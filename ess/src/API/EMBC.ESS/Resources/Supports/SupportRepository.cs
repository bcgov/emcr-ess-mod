using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AutoMapper;
using EMBC.ESS.Utilities.Dynamics;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;
using EMBC.Utilities.Extensions;
using Microsoft.OData.Client;
using EMBC.ESS.Resources.Reports;
using AutoMapper.Execution;
using EMBC.ESS.Shared.Contracts.Events;
using System.ComponentModel.DataAnnotations;

namespace EMBC.ESS.Resources.Supports
{
    internal class SupportRepository : ISupportRepository
    {
        private readonly IEssContextFactory essContextFactory;
        private readonly IMapper mapper;

        private static CancellationToken CreateCancellationToken() => new CancellationTokenSource().Token;

        private struct SupportQueue
        {
            public static readonly SupportQueue EAApproval = new SupportQueue { QueueId = new Guid("a4f0fbbe-89a1-ec11-b831-00505683fbf4") };
            public static readonly SupportQueue EAReview = new SupportQueue { QueueId = new Guid("e969aae7-8aa1-ec11-b831-00505683fbf4") };
            public static readonly SupportQueue QRReview = new SupportQueue { QueueId = new Guid("f132db1c-5eb4-ec11-b832-00505683fbf4") };

            public Guid QueueId { get; private set; }
        }


        public SupportRepository(IEssContextFactory essContextFactory, IMapper mapper)
        {
            this.essContextFactory = essContextFactory;
            this.mapper = mapper;
        }

        public async Task<ManageSupportCommandResult> Manage(ManageSupportCommand cmd)
        {
            var ct = CreateCancellationToken();
            return cmd switch
            {
                CreateNewSupportsCommand c => await Handle(c, ct),
                ChangeSupportStatusCommand c => await Handle(c, ct),
                SubmitSupportForApprovalCommand c => await Handle(c, ct),
                ApproveSupportCommand c => await Handle(c, ct),
                SubmitSupportForReviewCommand c => await Handle(c, ct),
                _ => throw new NotSupportedException($"{cmd.GetType().Name} is not supported")
            };
        }

        public async Task<SupportQueryResult> Query(SupportQuery query)
        {
            var ct = CreateCancellationToken();
            return query switch
            {
                SearchSupportsQuery q => await Handle(q, ct),

                _ => throw new NotSupportedException($"{query.GetType().Name} is not supported")
            };
        }

        public async Task<DuplicateSupportQueryResult> DuplicateQuery(SupportQuery query)
        {
            var ct = CreateCancellationToken();
            return query switch
            {
                PotentialDuplicateSupportsQuery q => await Handle(q, ct),

                _ => throw new NotSupportedException($"{query.GetType().Name} is not supported")
            };
        }

        private async Task<PotentialDuplicateSupportsQueryResult> Handle(PotentialDuplicateSupportsQuery query, CancellationToken ct)
        {
            var ctx = essContextFactory.Create();

            if (!Enum.TryParse<SupportType>(query.Category, out var supportType))
            {
                throw new ArgumentException($"Invalid support type: {query.Category}");
            }

            if (!DateTime.TryParse(query.FromDate, out var fromDate))
            {
                throw new ArgumentException($"Invalid FromDate: {query.FromDate}");
            }

            if (!DateTime.TryParse(query.ToDate, out var toDate))
            {
                throw new ArgumentException($"Invalid ToDate: {query.ToDate}");
            }

            var toDateOffset = DateTime.SpecifyKind(toDate, DateTimeKind.Local).ToUniversalTime();
            var fromDateOffset = DateTime.SpecifyKind(fromDate, DateTimeKind.Local).ToUniversalTime();

            List<Guid> memberGuids = new List<Guid>();
            foreach (var id in query.Members)
            {
                if (Guid.TryParse(id, out var guid))
                {
                    memberGuids.Add(guid);
                }
                else
                {
                    throw new ArgumentException($"Invalid member ID: {id}");
                }
            }

            // Build a LINQ filter for the member GUIDs
            var filter = string.Join(" or ", memberGuids.Select(g => $"era_householdmemberid eq {g}"));
            // Fetch information about household members passed into the query
            var householdMembers = (await ((DataServiceQuery<era_householdmember>)ctx.era_householdmembers
                .AddQueryOption("$filter", filter))
                .GetAllPagesAsync(ct)).ToList();

            var relatedSupportTypes = SimilarSupportTypes(supportType).Select(t => (int)t).ToArray();
            var supportTypesFilter = string.Join(" or ", relatedSupportTypes.Select(t => $"era_supporttype eq {t}"));

            // Modified query to use string-based filter
            var supports = (await ((DataServiceQuery<era_evacueesupport>)ctx.era_evacueesupports
                .Expand(s => s.era_era_householdmember_era_evacueesupport)
                .Expand(s => s.era_EvacuationFileId)
                .Expand(s => s.era_Task)
                .Expand(s => s.era_IssuedById)
                .AddQueryOption("$filter", $"({supportTypesFilter}) and era_validfrom le {toDateOffset:yyyy-MM-ddTHH:mm:ss.fffZ} and era_validto ge {fromDateOffset:yyyy-MM-ddTHH:mm:ss.fffZ} and statuscode ne {(int)SupportStatus.Cancelled} and statuscode ne {(int)SupportStatus.Void}"))
                .GetAllPagesAsync(ct)).ToList();

            // Create a new ConcurrentBag to store potential duplicates
            var potentialDuplicates = new ConcurrentBag<era_evacueesupport>();
            var potentianExtendedDuplicateSupports = new ConcurrentBag<DuplicateSupportResult>();
            var lockObj = new object();

            //load the responder 
            var responder = ctx.era_essteamusers.Where(u => u.era_essteamuserid == query.IssuedBy).SingleOrDefault();

            //load the file 
            var file = await ctx.era_evacuationfiles
                .Expand(s => s.era_TaskId)
                .Expand(f => f.era_Registrant)
                .Expand(f => f.era_era_evacuationfile_era_evacueesupport_ESSFileId)
                .Where(f => f.era_name == query.FileId).SingleOrDefaultAsync(ct);

            if (file == null) throw new InvalidOperationException($"Evacuation file {query.FileId} not found");

            // Iterate through each of the supports that matched the query
            Parallel.ForEach(supports, support =>
            {
                // remove household members that are not in the support inside the temp support and cast tempSupport to a list in a warning
                var tempSupport = support;
                ConflictMessageScenario scenario = ConflictMessageScenario.ExactMatchSameFile;
                // Iterate through each household member in the support
                foreach (var supportMember in tempSupport.era_era_householdmember_era_evacueesupport.ToList())
                {
                    // Iterate through each household member passed into the query
                    foreach (var member in householdMembers)
                    {
                        // Check name similarity
                        var firstNameSimilarity = member.era_firstname.ToLower().CombinedSimilarity(supportMember.era_firstname);
                        var lastNameSimilarity = member.era_lastname.ToLower().CombinedSimilarity(supportMember.era_lastname);

                        // If the similarity is above 70%, consider the support a potential duplicate
                        var combinedScore = (firstNameSimilarity + lastNameSimilarity) / 2;
                        if (combinedScore >= 0.7 && member.era_dateofbirth == supportMember.era_dateofbirth)
                        {
                            lock (lockObj)
                            {
                                if (query.FileId == support.era_EvacuationFileId.era_name)
                                {
                                    scenario = ConflictMessageScenario.PartialMatchSameFile;

                                    if (string.Equals(member.era_firstname?.Trim(), supportMember.era_firstname?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(member.era_lastname?.Trim(), supportMember.era_lastname?.Trim(), StringComparison.OrdinalIgnoreCase))
                                        scenario = ConflictMessageScenario.ExactMatchSameFile;
                                }
                                else
                                {
                                    scenario = ConflictMessageScenario.PartialMatchOnDifferentEssFile;

                                    if (string.Equals(member.era_firstname?.Trim(), supportMember.era_firstname?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(member.era_lastname?.Trim(), supportMember.era_lastname?.Trim(), StringComparison.OrdinalIgnoreCase))
                                        scenario = ConflictMessageScenario.ExactMatchOnDifferentEssFile;
                                }
                                Guid guid = Guid.NewGuid();
                                era_supportconflictmessage eraConflictMessage = new era_supportconflictmessage
                                {
                                    era_supportconflictmessageid = guid,
                                    era_name = guid.ToString(),
                                    era_EvacueeSupport = support,
                                    era_ESSFile = file,
                                    era_ESSTask = file.era_TaskId,
                                    era_Registrant = file.era_Registrant,

                                    // era_Registrant = support.era_EvacuationFileId.era_Registrant,
                                    era_Responder = responder,

                                    era_evacueedob = member.era_dateofbirth,
                                    era_evacueename = $"{member.era_firstname} {member.era_lastname}",
                                    createdon = DateTime.Now,

                                    era_MatchedESSFile = support.era_EvacuationFileId,
                                    era_matchedname = $"{supportMember.era_firstname} {supportMember.era_lastname}",
                                    era_matcheddob = supportMember.era_dateofbirth,
                                    era_scenario = (int)scenario,
                                };

                                //CreateConflictMessage(ctx, eraConflictMessage, ct);

                                var supportTypeEnum = (SupportType)support.era_supporttype.Value;
                                var supportTypeDisplayName = supportTypeEnum.GetType()
                                    .GetField(supportTypeEnum.ToString())
                                    ?.GetCustomAttributes(typeof(DisplayAttribute), false)
                                    .Cast<DisplayAttribute>()
                                    .FirstOrDefault()?.Name;

                                if (string.IsNullOrEmpty(supportTypeDisplayName))
                                {
                                    supportTypeDisplayName = "None";
                                }

                                potentianExtendedDuplicateSupports.Add(
                                new DuplicateSupportResult
                                {
                                    duplicateSupportScenario = (int)scenario,
                                    essFileId = tempSupport.era_EvacuationFileId.era_name,
                                    supportStartDate = tempSupport.era_validfrom.Value.UtcDateTime.ToPST().ToString("MM-dd-yyyy"),
                                    supportEndDate = tempSupport.era_validto.Value.UtcDateTime.ToPST().ToString("MM-dd-yyyy"),
                                    supportStartTime = tempSupport.era_validfrom.Value.UtcDateTime.ToPST().ToString("hh:mm"),
                                    supportEndTime = tempSupport.era_validto.Value.UtcDateTime.ToPST().ToString("hh:mm"),
                                    householdMemberFirstName = $"{member.era_firstname}",
                                    householdMemberLastName = $"{member.era_lastname}",
                                    supportCategory = supportTypeDisplayName.ToString(),
                                    supportSubCategory = support.era_supporttype.Value.ToString(),
                                    supportMemberFirstName = $"{supportMember.era_firstname}",
                                    supportMemberLastName = $"{supportMember.era_lastname}",
                                    supportMemberDOB = supportMember.era_dateofbirth.Value.ToString(),
                                    householdMemberDOB = member.era_dateofbirth.Value.ToString()

                                });
                            }
                        }
                    }
                }
            });

            await ctx.SaveChangesAsync(ct);
            ctx.DetachAll();
            return new PotentialDuplicateSupportsQueryResult { DuplicateSupports = mapper.Map<IEnumerable<DuplicateSupportResult>>(potentianExtendedDuplicateSupports.OrderBy(t => t.duplicateSupportScenario)) };
        }

        private async Task<SubmitSupportForApprovalCommandResult> Handle(SubmitSupportForApprovalCommand cmd, CancellationToken ct)
        {
            var ctx = essContextFactory.Create();

            var support = await ctx.era_evacueesupports.Where(s => s.era_name == cmd.SupportId).SingleOrDefaultAsync();
            if (support == null) throw new InvalidOperationException($"Support {cmd.SupportId} not found");
            if (support.statuscode != (int)SupportStatus.PendingScan)
                throw new InvalidOperationException($"Support {cmd.SupportId} is in status {(SupportStatus)support.statuscode} and cannot be submitted for approval - expecting PendingScan status");

            // create flags
            var flagTypes = (await ctx.era_supportflagtypes.GetAllPagesAsync()).ToArray();
            foreach (var flag in cmd.Flags)
            {
                var supportFlag = mapper.Map<era_supportflag>(flag);
                ctx.AddToera_supportflags(supportFlag);
                ctx.SetLink(supportFlag, nameof(era_supportflag.era_FlagType), flagTypes.Single(t => t.era_supportflagtypeid == supportFlag._era_flagtype_value));
                ctx.SetLink(supportFlag, nameof(era_supportflag.era_EvacueeSupport), support);
                if (flag is DuplicateSupportFlag dup)
                {
                    var duplicateSupport = await ctx.era_evacueesupports.Where(s => s.era_name == dup.DuplicatedSupportId).SingleOrDefaultAsync();
                    if (duplicateSupport == null) throw new InvalidOperationException($"Support {dup.DuplicatedSupportId} not found");
                    ctx.SetLink(supportFlag, nameof(era_supportflag.era_SupportDuplicate), duplicateSupport);
                }
            }

            // assign to EA queue
            var queueId = cmd.Flags.Any() ? SupportQueue.EAReview.QueueId : SupportQueue.EAApproval.QueueId;
            var queue = await ctx.queues.Where(q => q.queueid == queueId).SingleOrDefaultAsync(ct);
            if (queue == null) throw new InvalidOperationException($"Error queue {queueId} not found");
            AssignSupportToQueue(ctx, support, queue);

            SetSupportStatus(ctx, support, SupportStatus.PendingApproval);
            ctx.UpdateObject(support);

            await ctx.SaveChangesAsync(ct);
            ctx.DetachAll();

            return new SubmitSupportForApprovalCommandResult();
        }

        private async Task<SubmitSupportForApprovalCommandResult> Handle(ApproveSupportCommand cmd, CancellationToken ct)
        {
            var ctx = essContextFactory.Create();

            var support = await ctx.era_evacueesupports.Where(s => s.era_name == cmd.SupportId).SingleOrDefaultAsync();
            if (support == null) throw new InvalidOperationException($"Support {cmd.SupportId} not found");
            if (support.statuscode != (int)SupportStatus.PendingApproval)
                throw new InvalidOperationException($"Support {cmd.SupportId} is in status {(SupportStatus)support.statuscode} and cannot be approved - expecting PendingApproval status");

            await ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.bpf_era_evacueesupport_era_essevacueeetransfersupport), ct);
            await ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.era_evacueesupport_QueueItems), ct);
            var bpf = support.bpf_era_evacueesupport_era_essevacueeetransfersupport.OrderByDescending(bpf => bpf.completedon).FirstOrDefault();
            if (bpf == null) throw new InvalidOperationException($"Business process flow for Support {cmd.SupportId} not found");

            // approve the support
            support.era_etransferapproved = 174360001; //No
            SetSupportStatus(ctx, support, SupportStatus.Approved);
            ctx.UpdateObject(support);

            // push BPF to correct stage
            const string processStageName = "Send Support For Payment";
            var processStage = await ctx.processstages.Where(ps => ps.stagename == processStageName).SingleOrDefaultAsync(ct);
            if (processStage == null) throw new InvalidOperationException($"Business process flow for Support {cmd.SupportId} not found");
            ctx.SetLink(bpf, nameof(era_essevacueeetransfersupport.activestageid), processStage);

            await ctx.SaveChangesAsync(ct);

            // deactivate BPF, must happen in a separate tx
            ctx.DetachAll();
            support = await ctx.era_evacueesupports.Where(s => s.era_name == cmd.SupportId).SingleOrDefaultAsync();
            await ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.bpf_era_evacueesupport_era_essevacueeetransfersupport), ct);
            bpf = support.bpf_era_evacueesupport_era_essevacueeetransfersupport.OrderByDescending(bpf => bpf.completedon).FirstOrDefault();
            if (bpf == null) throw new InvalidOperationException($"Business process flow for Support {cmd.SupportId} not found");
            ctx.DeactivateObject(bpf, 2);

            await ctx.SaveChangesAsync(ct);
            ctx.DetachAll();

            return new SubmitSupportForApprovalCommandResult();
        }

        private async Task<SubmitSupportForReviewCommandResult> Handle(SubmitSupportForReviewCommand cmd, CancellationToken ct)
        {
            var ctx = essContextFactory.Create();

            var support = await ctx.era_evacueesupports.Where(s => s.era_name == cmd.SupportId).SingleOrDefaultAsync(ct);
            if (support == null) throw new InvalidOperationException($"Support {cmd.SupportId} not found");

            // guard on current status
            if (!new[] { SupportStatus.Approved, SupportStatus.Issued }.Contains((SupportStatus)support.statuscode))
                throw new InvalidOperationException($"Support {cmd.SupportId} is in status {(SupportStatus)support.statuscode} and cannot be submitted for review - expecting Approved or Issued statuses");

            await ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.bpf_era_evacueesupport_era_essevacueeetransfersupport), ct);

            // assign to QR review queue
            var queue = await ctx.queues.Where(q => q.queueid == SupportQueue.QRReview.QueueId).SingleOrDefaultAsync(ct);
            if (queue == null) throw new InvalidOperationException($"Error queue {SupportQueue.QRReview.QueueId} not found");
            AssignSupportToQueue(ctx, support, queue);

            SetSupportStatus(ctx, support, SupportStatus.UnderReview);
            support.era_etransferapproved = 174360000; //yes per https://justice.gov.bc.ca/jira/browse/EMBCESSMOD-4041
            support.era_etransfertransactioncreated = false;
            support.ownerid = await ctx.GetCurrentSystemUser();
            ctx.UpdateObject(support);

            var bpf = support.bpf_era_evacueesupport_era_essevacueeetransfersupport.OrderByDescending(bpf => bpf.completedon).FirstOrDefault();
            if (bpf != null)
            {
                //activate the business workflow to a specific stage
                ctx.ActivateObject(bpf, 1);
                var processStage = await ctx.processstages.Where(ps => ps.stagename == "Internal Qualified Receiver Review").SingleOrDefaultAsync(ct);
                if (processStage != null) ctx.SetLink(bpf, nameof(era_essevacueeetransfersupport.activestageid), processStage);
            }

            await ctx.SaveChangesAsync(ct);

            return new SubmitSupportForReviewCommandResult();
        }

        private static void AssignSupportToQueue(EssContext ctx, era_evacueesupport support, queue queue)
        {
            var queueItem = new queueitem
            {
                queueitemid = Guid.NewGuid(),
                objecttypecode = 10056 //support type pick list value
            };

            // create queue item
            ctx.AddToqueueitems(queueItem);
            ctx.SetLink(queueItem, nameof(queueItem.queueid), queue);
            ctx.SetLink(queueItem, nameof(queueItem.objectid_era_evacueesupport), support);
        }

        private async Task<ManageSupportCommandResult> Handle(ChangeSupportStatusCommand cmd, CancellationToken ct)
        {
            var ctx = essContextFactory.Create();
            var changesSupportIds = new List<string>();
            foreach (var item in cmd.Items)
            {
                var support = await ctx.era_evacueesupports.Where(s => s.era_name == item.SupportId).SingleOrDefaultAsync(ct);
                if (support == null) throw new InvalidOperationException($"Support {item.SupportId} not found, can't update its status");
                SetSupportStatus(ctx, support, item.ToStatus, item.Reason);
                ctx.UpdateObject(support);
                changesSupportIds.Add(item.SupportId);
            }

            await ctx.SaveChangesAsync(ct);
            ctx.DetachAll();
            return new ChangeSupportStatusCommandResult { Ids = changesSupportIds.ToArray() };
        }

        private static void SetSupportStatus(EssContext ctx, era_evacueesupport support, SupportStatus status, string? changeReason = null)
        {
            var supportDeliveryType = (SupportMethod)support.era_supportdeliverytype;

            switch (status)
            {
                case SupportStatus.Void when supportDeliveryType == SupportMethod.Referral:
                    support.era_voidreason = (int)Enum.Parse<SupportVoidReason>(changeReason);
                    ctx.DeactivateObject(support, (int)status);
                    break;

                case SupportStatus.Cancelled when supportDeliveryType == SupportMethod.ETransfer:
                case SupportStatus.Paid when supportDeliveryType == SupportMethod.ETransfer:
                    ctx.DeactivateObject(support, (int)status);
                    break;

                default:
                    support.statuscode = (int)status;
                    break;
            }
        }

        private async Task<SearchSupportQueryResult> Handle(SearchSupportsQuery query, CancellationToken ct)
        {
            var ctx = essContextFactory.CreateReadOnly();
            var supports = (await Search(ctx, query, ct)).ToArray();
            await Parallel.ForEachAsync(supports, ct, async (s, ct) => await LoadSupportDetails(ctx, s, ct));

            var results = new SearchSupportQueryResult { Items = mapper.Map<IEnumerable<Support>>(supports).ToArray() };

            ctx.DetachAll();

            return results;
        }

        private static async Task<IEnumerable<era_evacueesupport>> Search(EssContext ctx, SearchSupportsQuery query, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(query.ById) &&
                string.IsNullOrEmpty(query.ByManualReferralId) &&
                string.IsNullOrEmpty(query.ByEvacuationFileId) &&
                !query.ByStatus.HasValue)
                throw new ArgumentException("Supports query must have at least one criteria", nameof(query));

            IQueryable<era_evacueesupport> supportsQuery = ctx.era_evacueesupports
              .Expand(s => s.era_EvacuationFileId)
              .Expand(s => s.era_era_householdmember_era_evacueesupport)
              .Expand(s => s.era_era_evacueesupport_era_supportflag_EvacueeSupport)
              .Expand(s => s.era_era_etransfertransaction_era_evacueesuppo)
              .Expand(s => s.era_NeedsAssessmentID);

            // search a specific file
            if (!string.IsNullOrEmpty(query.ByEvacuationFileId))
            {
                var file = (await ((DataServiceQuery<era_evacuationfile>)ctx.era_evacuationfiles
                    .Where(f => f.era_name == query.ByEvacuationFileId))
                    .ExecuteAsync(ct))
                    .SingleOrDefault();
                if (file == null) return Array.Empty<era_evacueesupport>();

                supportsQuery = supportsQuery.Where(s => s._era_evacuationfileid_value == file.era_evacuationfileid);
            }

            if (!string.IsNullOrEmpty(query.ById)) supportsQuery = supportsQuery.Where(s => s.era_name == query.ById);
            if (!string.IsNullOrEmpty(query.ByManualReferralId)) supportsQuery = supportsQuery.Where(s => s.era_manualsupport == query.ByManualReferralId);
            if (query.ByStatus.HasValue) supportsQuery = supportsQuery.Where(s => s.statuscode == (int)query.ByStatus.Value);
            supportsQuery = supportsQuery.OrderBy(s => s.createdon);
            if (query.LimitNumberOfResults.HasValue) supportsQuery = supportsQuery.Take(query.LimitNumberOfResults.Value);

            return await ((DataServiceQuery<era_evacueesupport>)supportsQuery).GetAllPagesAsync(ct);
        }

        private static async Task LoadSupportDetails(EssContext ctx, era_evacueesupport support, CancellationToken ct)
        {
            ctx.AttachTo(nameof(EssContext.era_evacueesupports), support);
            var tasks = new List<Task>();

            if (support.era_EvacuationFileId == null) tasks.Add(ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.era_EvacuationFileId), ct));
            if (support.era_NeedsAssessmentID == null) tasks.Add(ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.era_NeedsAssessmentID), ct));
            if (support.era_era_householdmember_era_evacueesupport == null) tasks.Add(ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.era_era_householdmember_era_evacueesupport), ct));
            if (support.era_era_evacueesupport_era_supportflag_EvacueeSupport == null) tasks.Add(ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.era_era_evacueesupport_era_supportflag_EvacueeSupport), ct));
            if (support.era_era_etransfertransaction_era_evacueesuppo == null) tasks.Add(ctx.LoadPropertyAsync(support, nameof(era_evacueesupport.era_era_etransfertransaction_era_evacueesuppo), ct));

            await Task.WhenAll(tasks);

            if (support.era_NeedsAssessmentID?._era_tasknumber_value == null) throw new InvalidOperationException($"Support {support.era_name} needs assessment has no task");
            var task = await ctx.era_tasks.ByKey(support.era_NeedsAssessmentID._era_tasknumber_value).GetValueAsync(ct);
            if (task == null) throw new InvalidOperationException($"Support {support.era_name} has no task");

            support.era_EvacuationFileId.era_TaskId = task;

            foreach (var flag in support.era_era_evacueesupport_era_supportflag_EvacueeSupport)
            {
                if (flag._era_supportduplicate_value.HasValue)
                {
                    ctx.AttachTo(nameof(EssContext.era_supportflags), flag);
                    await ctx.LoadPropertyAsync(flag, nameof(era_supportflag.era_SupportDuplicate), ct);
                }
            }
        }

        private async Task<CreateNewSupportsCommandResult> Handle(CreateNewSupportsCommand cmd, CancellationToken ct)
        {
            var ctx = essContextFactory.Create();
            var file = (await ((DataServiceQuery<era_evacuationfile>)ctx.era_evacuationfiles
                .Expand(f => f.era_CurrentNeedsAssessmentid)
                .Where(f => f.era_name == cmd.FileId))
                .ExecuteAsync(ct))
                .SingleOrDefault();

            if (file == null) throw new ArgumentException($"Evacuation file {cmd.FileId} not found");

            await ctx.LoadPropertyAsync(file, nameof(era_evacuationfile.era_era_evacuationfile_era_householdmember_EvacuationFileid), ct);

            var supports = new List<era_evacueesupport>();

            foreach (var s in cmd.Supports)
            {
                var support = mapper.Map<era_evacueesupport>(s);
                var supportHouseholdMembersIds = s.IncludedHouseholdMembers.Select(m => Guid.Parse(m)).ToArray();

                // map tracked household members
                support.era_era_householdmember_era_evacueesupport = new Collection<era_householdmember>(
                    file.era_era_evacuationfile_era_householdmember_EvacuationFileid.Where(m => supportHouseholdMembersIds.Contains(m.era_householdmemberid.Value)).ToArray());

                // check all household members are accounted for
                if (support.era_era_householdmember_era_evacueesupport.Count != supportHouseholdMembersIds.Length)
                    throw new InvalidOperationException($"Support has household members which do not exist in evacuation file {cmd.FileId}");

                await CreateSupport(ctx, file, support, ct);

                supports.Add(support);
            }

            await ctx.SaveChangesAsync(ct);

            ctx.DetachAll();

            // load the generated support ids
            await Parallel.ForEachAsync(supports, ct,
                async (s, ct) => s.era_name = await ctx.era_evacueesupports.ByKey(s.era_evacueesupportid).Select(s => s.era_name).GetValueAsync(ct));

            ctx.DetachAll();

            return new CreateNewSupportsCommandResult { Supports = mapper.Map<IEnumerable<Support>>(supports) };
        }

        private static async Task CreateSupport(EssContext ctx, era_evacuationfile file, era_evacueesupport support, CancellationToken ct)
        {
            var validationErrors = ValidateSupportInvariants(support).ToArray();
            if (validationErrors.Any()) throw new InvalidOperationException($"Failed to create a support in file {file.era_name}:  {string.Join(';', validationErrors)}");

            support.era_evacueesupportid = Guid.NewGuid();
            if (support.era_supportdeliverytype == (int)SupportMethod.ETransfer)
            {
                SetSupportStatus(ctx, support, SupportStatus.PendingScan);
            }
            else
            {
                SetSupportStatus(ctx, support, support.era_validto <= DateTime.UtcNow ? SupportStatus.Expired : SupportStatus.Active);
            }

            ctx.AddToera_evacueesupports(support);
            ctx.AddLink(file, nameof(era_evacuationfile.era_era_evacuationfile_era_evacueesupport_ESSFileId), support);
            ctx.AddLink(file.era_CurrentNeedsAssessmentid, nameof(era_needassessment.era_era_needassessment_era_evacueesupport_NeedsAssessmentID), support);
            ctx.SetLink(support, nameof(era_evacueesupport.era_EvacuationFileId), file);
            ctx.SetLink(support, nameof(era_evacueesupport.era_NeedsAssessmentID), file.era_CurrentNeedsAssessmentid);
            if (support._era_grouplodgingcityid_value.HasValue)
                ctx.SetLink(support, nameof(era_evacueesupport.era_GroupLodgingCityID), ctx.LookupJurisdictionByCode(support._era_grouplodgingcityid_value?.ToString()));

            AssignHouseholdMembersToSupport(ctx, support, support.era_era_householdmember_era_evacueesupport);
            if (support.era_selfservesupport == false) await AssignTeamMemberToSupport(ctx, support, ct);
            await AssignSupplierToSupport(ctx, support, ct);
            await AssignETransferRecipientToSupport(ctx, support, ct);
        }

        private static IEnumerable<string> ValidateSupportInvariants(era_evacueesupport support)
        {
            if (!support.era_era_householdmember_era_evacueesupport.Any()) yield return "No household members associated";
            if (!support.era_selfservesupport == true && !support._era_issuedbyid_value.HasValue) yield return "No issuing team member";
        }

        private static void AssignHouseholdMembersToSupport(EssContext ctx, era_evacueesupport support, IEnumerable<era_householdmember> householdMembers)
        {
            foreach (var member in householdMembers)
            {
                ctx.AddLink(member, nameof(era_householdmember.era_era_householdmember_era_evacueesupport), support);
            }
        }

        private static async Task AssignSupplierToSupport(EssContext ctx, era_evacueesupport support, CancellationToken ct)
        {
            if (support._era_supplierid_value.HasValue)
            {
                var supplier = await ctx.era_suppliers.ByKey(support._era_supplierid_value).GetValueAsync(ct);
                if (supplier == null || supplier.statecode != (int)EntityState.Active) throw new ArgumentException($"Supplier id {support._era_supplierid_value} not found or is not active");
                ctx.SetLink(support, nameof(era_evacueesupport.era_SupplierId), supplier);
            }
        }

        private static async Task AssignETransferRecipientToSupport(EssContext ctx, era_evacueesupport support, CancellationToken ct)
        {
            if (support._era_payeeid_value.HasValue)
            {
                var registrant = await ctx.contacts.ByKey(support._era_payeeid_value).GetValueAsync(ct);
                if (registrant == null || registrant.statecode != (int)EntityState.Active) throw new ArgumentException($"Registrant id {support._era_payeeid_value} not found or is not active");
                ctx.SetLink(support, nameof(era_evacueesupport.era_PayeeId), registrant);
            }
        }

        private static async Task AssignTeamMemberToSupport(EssContext ctx, era_evacueesupport support, CancellationToken ct)
        {
            var teamMember = await ctx.era_essteamusers.ByKey(support._era_issuedbyid_value).GetValueAsync(ct);
            if (teamMember == null || teamMember.statecode != (int)EntityState.Active) throw new InvalidOperationException($"team member {support._era_issuedbyid_value} not found or is not active");
            ctx.SetLink(support, nameof(era_evacueesupport.era_IssuedById), teamMember);
        }

        private static SupportType[] SimilarSupportTypes(SupportType type) =>
            type switch
            {
                SupportType.Food_Groceries or SupportType.Food_Restaurant => 
                    [SupportType.Food_Groceries, SupportType.Food_Restaurant],
                
                SupportType.Lodging_Group or SupportType.Lodging_Billeting or 
                SupportType.Lodging_Hotel or SupportType.Lodging_Shelter => 
                    [SupportType.Lodging_Group, SupportType.Lodging_Billeting, 
                     SupportType.Lodging_Hotel, SupportType.Lodging_Shelter],
                
                SupportType.Transportation_Other or SupportType.Transportation_Taxi => 
                    [SupportType.Transportation_Other, SupportType.Transportation_Taxi],

                _ => [type]
            };

        private enum SupportType
        {
            Food_Groceries = 174360000,
            Food_Restaurant = 174360001,
            Lodging_Hotel = 174360002,
            Lodging_Billeting = 174360003,
            Lodging_Group = 174360004,
            Incidentals = 174360005,
            Clothing = 174360006,
            Transportation_Taxi = 174360007,
            Transportation_Other = 174360008,
            Lodging_Shelter = 174360009,
        }
    }
}
