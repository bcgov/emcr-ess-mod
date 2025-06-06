using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EMBC.ESS.Shared.Contracts.Events;
using EMBC.ESS.Utilities.Dynamics;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;
using EMBC.Utilities.Extensions;
using Microsoft.OData.Client;
using System.Collections.Concurrent;
using EMBC.ESS.Resources.Reports;
using System.ComponentModel.DataAnnotations;

namespace EMBC.ESS.Engines.Supporting.SupportCompliance
{
    internal class DuplicateSupportComplianceCheck(IEssContextFactory essContextFactory) : ISupportComplianceCheck
    {
        public async Task<IEnumerable<SupportFlag>> CheckCompliance(Shared.Contracts.Events.Support support, CancellationToken ct)
        {
            if (support.Id == null) throw new ArgumentNullException(nameof(support.Id));

            var ctx = essContextFactory.CreateReadOnly();

            var checkedSupport = (await ((DataServiceQuery<era_evacueesupport>)ctx.era_evacueesupports.Where(s => s.era_name == support.Id)).SingleOrDefaultAsync(ct));
            if (checkedSupport == null) throw new ArgumentException($"Support {support.Id} not found", nameof(support));

            ctx.AttachTo(nameof(ctx.era_evacueesupports), checkedSupport);
            await ctx.LoadPropertyAsync(checkedSupport, nameof(era_evacueesupport.era_era_householdmember_era_evacueesupport), ct);
            ctx.Detach(checkedSupport);

            var from = checkedSupport.era_validfrom.Value;
            var to = checkedSupport.era_validto.Value;
            var type = checkedSupport.era_supporttype.Value;
            var householdMembers = checkedSupport.era_era_householdmember_era_evacueesupport.ToList();

            var similarSupportTypes = SimilarSupportTypes(Enum.Parse<SupportType>(type.ToString())).Cast<int>().ToArray();

            var duplicateSupports = (await householdMembers
                .SelectManyAsync(async hm => await GetDuplicateSupportsForHouseholdMember(ctx, support, hm, similarSupportTypes, from, to, ct)))
                .DistinctBy(s => s.supportId).ToList();

            var duplicates = duplicateSupports.Select(s => new DuplicateSupportFlag { DuplicatedSupportId = s.supportId }).ToList();

            ctx.DetachAll();

            return duplicates;
        }

        private async Task<IEnumerable<DuplicateSupportResult>> GetDuplicateSupportsForHouseholdMember(EssContext ctx, Shared.Contracts.Events.Support support, era_householdmember hm, int[] similarSupportTypes, DateTimeOffset from, DateTimeOffset to, CancellationToken ct)
        {

            var relatedSupportTypes = similarSupportTypes.Select(t => (int)t).ToArray();
            var supportTypesFilter = string.Join(" or ", relatedSupportTypes.Select(t => $"era_supporttype eq {t}"));

            // Modified query to use string-based filter
            var supports = (await ((DataServiceQuery<era_evacueesupport>)ctx.era_evacueesupports
                .Expand(s => s.era_era_householdmember_era_evacueesupport)
                .Expand(s => s.era_EvacuationFileId)
                .Expand(s => s.era_Task)
                .Expand(s => s.era_IssuedById)
                .AddQueryOption("$filter", $"({supportTypesFilter}) and era_name ne '{support.Id}' and era_validfrom le {to:yyyy-MM-ddTHH:mm:ss.fffZ} and era_validto ge {from:yyyy-MM-ddTHH:mm:ss.fffZ} and statuscode ne {(int)SupportStatus.Cancelled} and statuscode ne {(int)SupportStatus.Void}"))
                .GetAllPagesAsync(ct)).ToList();
  
            // Create a new ConcurrentBag to store potential duplicates
            var potentialDuplicates = new ConcurrentBag<era_evacueesupport>();
            var potentianExtendedDuplicateSupports = new ConcurrentBag<DuplicateSupportResult>();


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
                    {
                        // Check name similarity
                        var firstNameSimilarity = hm.era_firstname.ToLower().CombinedSimilarity(supportMember.era_firstname);
                        var lastNameSimilarity = hm.era_lastname.ToLower().CombinedSimilarity(supportMember.era_lastname);

                        // If the similarity is above 70%, consider the support a potential duplicate
                        var combinedScore = (firstNameSimilarity + lastNameSimilarity) / 2;
                        if (combinedScore >= 0.7 && hm.era_dateofbirth == supportMember.era_dateofbirth)
                        {
                            bool includeInResult = false;
                            if (tempSupport.era_EvacuationFileId.era_interviewername == support.era_EvacuationFileId.era_name)
                            {
                                scenario = ConflictMessageScenario.PartialMatchSameFile;

                                if (string.Equals(hm.era_firstname?.Trim(), supportMember.era_firstname?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(hm.era_lastname?.Trim(), supportMember.era_lastname?.Trim(), StringComparison.OrdinalIgnoreCase))
                                    scenario = ConflictMessageScenario.ExactMatchSameFile;
                            }
                            else
                            {
                                scenario = ConflictMessageScenario.PartialMatchOnDifferentEssFile;

                                if (string.Equals(hm.era_firstname?.Trim(), supportMember.era_firstname?.Trim(), StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(hm.era_lastname?.Trim(), supportMember.era_lastname?.Trim(), StringComparison.OrdinalIgnoreCase))
                                    scenario = ConflictMessageScenario.ExactMatchOnDifferentEssFile;
                            }

                            if(scenario == ConflictMessageScenario.ExactMatchOnDifferentEssFile || scenario == ConflictMessageScenario.ExactMatchSameFile)
                                includeInResult = true;

                            Guid guid = Guid.NewGuid();


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
                            if (includeInResult)
                            {
                                potentianExtendedDuplicateSupports.Add(
                                new DuplicateSupportResult
                                {
                                    duplicateSupportScenario = (int)scenario,
                                    essFileId = tempSupport.era_EvacuationFileId.era_name,
                                    supportStartDate = tempSupport.era_validfrom.Value.UtcDateTime.ToPST().ToString("MM-dd-yyyy"),
                                    supportEndDate = tempSupport.era_validto.Value.UtcDateTime.ToPST().ToString("MM-dd-yyyy"),
                                    supportStartTime = tempSupport.era_validfrom.Value.UtcDateTime.ToPST().ToString("hh:mm"),
                                    supportEndTime = tempSupport.era_validto.Value.UtcDateTime.ToPST().ToString("hh:mm"),
                                    householdMemberFirstName = $"{hm.era_firstname}",
                                    householdMemberLastName = $"{hm.era_lastname}",
                                    supportCategory = supportTypeDisplayName.ToString(),
                                    supportSubCategory = support.era_supporttype.Value.ToString(),
                                    supportMemberFirstName = $"{supportMember.era_firstname}",
                                    supportMemberLastName = $"{supportMember.era_lastname}",
                                    supportMemberDOB = supportMember.era_dateofbirth.Value.ToString(),
                                    householdMemberDOB = hm.era_dateofbirth.Value.ToString(),
                                    supportId = tempSupport.era_name.ToString(),
                                });
                            }
                        }
                    }
                }
            });
 

            return potentianExtendedDuplicateSupports;
        }

        private static SupportType[] SimilarSupportTypes(SupportType type) =>
            type switch
            {
                SupportType.Food_Groceries or SupportType.Food_Restaurant => [SupportType.Food_Groceries, SupportType.Food_Restaurant],
                SupportType.Lodging_Group or SupportType.Lodging_Billeting or SupportType.Lodging_Hotel or SupportType.Lodging_Shelter => [SupportType.Lodging_Group, SupportType.Lodging_Billeting, SupportType.Lodging_Hotel, SupportType.Lodging_Shelter],
                SupportType.Transportation_Other or SupportType.Transportation_Taxi => [SupportType.Transportation_Other, SupportType.Transportation_Taxi],

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
