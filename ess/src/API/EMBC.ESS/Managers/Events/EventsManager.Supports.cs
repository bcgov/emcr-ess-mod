using System;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EMBC.ESS.Engines.Search;
using EMBC.ESS.Engines.Supporting;
using EMBC.ESS.Managers.Events.Notifications;
using EMBC.ESS.Resources.Evacuees;
using EMBC.ESS.Resources.Payments;
using EMBC.ESS.Resources.Print;
using EMBC.ESS.Resources.Supports;
using EMBC.ESS.Resources.Tasks;
using EMBC.ESS.Shared.Contracts;
using EMBC.ESS.Shared.Contracts.Events;
using EMBC.ESS.Shared.Contracts.Events.SelfServe;
using EMBC.Utilities.Extensions;
using EMBC.Utilities.Telemetry;
using Microsoft.Extensions.Hosting;

namespace EMBC.ESS.Managers.Events;

public partial class EventsManager
{
    public async Task<string> Handle(ProcessSupportsCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.FileId)) throw new ArgumentNullException(nameof(cmd.FileId));
        if (string.IsNullOrEmpty(cmd.RequestingUserId)) throw new ArgumentNullException(nameof(cmd.RequestingUserId));

        var requestingUser = (await teamRepository.GetMembers(userId: cmd.RequestingUserId, includeStatuses: activeOnlyStatus)).SingleOrDefault();
        if (requestingUser == null) throw new BusinessValidationException($"User {cmd.RequestingUserId} not found");

        foreach (var support in cmd.Supports)
        {
            support.CreatedBy = new TeamMember { Id = requestingUser.Id };
            support.CreatedOn = DateTime.UtcNow;
        }

        var validationResponse = (DigitalSupportsValidationResponse)await supportingEngine.Validate(new DigitalSupportsValidationRequest
        {
            FileId = cmd.FileId,
            Supports = cmd.Supports
        });
        if (!validationResponse.IsValid) throw new BusinessValidationException(string.Join(',', validationResponse.Errors));

        var response = (ProcessDigitalSupportsResponse)await supportingEngine.Process(new ProcessDigitalSupportsRequest
        {
            FileId = cmd.FileId,
            Supports = cmd.Supports,
            RequestingUserId = requestingUser.Id,
            IncludeSummaryInReferralsPrintout = cmd.IncludeSummaryInReferralsPrintout
        });

        return response.PrintRequestId;
    }

    public async System.Threading.Tasks.Task Handle(ProcessPaperSupportsCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.FileId)) throw new ArgumentNullException(nameof(cmd.FileId));
        if (string.IsNullOrEmpty(cmd.RequestingUserId)) throw new ArgumentNullException(nameof(cmd.RequestingUserId));

        var requestingUser = (await teamRepository.GetMembers(userId: cmd.RequestingUserId, includeStatuses: activeOnlyStatus)).SingleOrDefault();
        if (requestingUser == null) throw new BusinessValidationException($"User {cmd.RequestingUserId} not found");

        foreach (var referral in cmd.Supports)
        {
            referral.CreatedBy = new TeamMember { Id = requestingUser.Id };
            referral.CreatedOn = DateTime.UtcNow;
        }

        var validationResponse = (PaperSupportsValidationResponse)await supportingEngine.Validate(new PaperSupportsValidationRequest
        {
            FileId = cmd.FileId,
            Supports = cmd.Supports
        });
        if (!validationResponse.IsValid) throw new BusinessValidationException(string.Join(',', validationResponse.Errors));

        await supportingEngine.Process(new ProcessPaperSupportsRequest { FileId = cmd.FileId, Supports = cmd.Supports });
    }

    public async System.Threading.Tasks.Task Handle(ProcessSelfServeSupportsCommand cmd)
    {
        var file = (await evacuationRepository.Query(new Resources.Evacuations.EvacuationFilesQuery { FileId = cmd.EvacuationFileNumber })).Items.SingleOrDefault();
        if (file == null) throw new NotFoundException("file not found", cmd.EvacuationFileNumber);
        if (string.IsNullOrEmpty(cmd.RegistrantUserId) || file.PrimaryRegistrantUserId != cmd.RegistrantUserId) throw new InvalidOperationException($"Registrant {cmd.RegistrantUserId} does not match file {cmd.EvacuationFileNumber} primary registrant which is {file.PrimaryRegistrantUserId}");
        if (file.NeedsAssessment.EligibilityCheck == null || !file.NeedsAssessment.EligibilityCheck.Eligible) throw new BusinessLogicException($"File {cmd.EvacuationFileNumber} latest needs assessment doesn't have a valid eligibility check");

        var response = (GenerateSelfServeSupportsResponse)await supportingEngine.Generate(new CalculateSelfServeSupports(cmd.Supports, file.NeedsAssessment.HouseholdMembers.Select(hm => new SelfServeHouseholdMember(hm.Id, hm.IsMinor))));

        var supports = ((GenerateSelfServeETransferSupportsResponse)await supportingEngine.Generate(
            new GenerateSelfServeETransferSupports(file.Id, file.PrimaryRegistrantId, response.Supports, cmd.ETransferDetails, file.NeedsAssessment.EligibilityCheck.From.Value, file.NeedsAssessment.EligibilityCheck.To.Value))).Supports;
        var validationResponse = (DigitalSupportsValidationResponse)await supportingEngine.Validate(new DigitalSupportsValidationRequest
        {
            FileId = cmd.EvacuationFileNumber,
            Supports = supports
        });
        if (!validationResponse.IsValid) throw new BusinessValidationException(string.Join(',', validationResponse.Errors));

        // Create a needs assessment and associate with the task
        await evacuationRepository.Manage(new Resources.Evacuations.AssignFileToTask { EvacuationFileNumber = file.Id, TaskNumber = file.NeedsAssessment.EligibilityCheck.TaskNumber });

        // Process the supports
        await supportingEngine.Process(new ProcessDigitalSupportsRequest
        {
            FileId = cmd.EvacuationFileNumber,
            Supports = supports,
            RequestingUserId = null,
            IncludeSummaryInReferralsPrintout = false,
            PrintReferrals = false
        });

        await SendEmailConfirmation(cmd.ETransferDetails, file.PrimaryRegistrantId, supports);
    }

    public async Task<string> Handle(VoidSupportCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.FileId)) throw new ArgumentNullException("FileId is required");
        if (string.IsNullOrEmpty(cmd.SupportId)) throw new ArgumentNullException("SupportId is required");

        return ((ChangeSupportStatusCommandResult)await supportRepository.Manage(new ChangeSupportStatusCommand
        {
            Items = [SupportStatusTransition.VoidSupport(cmd.SupportId, Enum.Parse<Resources.Supports.SupportVoidReason>(cmd.VoidReason.ToString()))]
        })).Ids.SingleOrDefault();
    }

    public async Task<string> Handle(CancelSupportCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.FileId)) throw new ArgumentNullException("FileId is required");
        if (string.IsNullOrEmpty(cmd.SupportId)) throw new ArgumentNullException("SupportId is required");

        // check invariants
        var support = ((SearchSupportQueryResult)await supportRepository.Query(new Resources.Supports.SearchSupportsQuery { ById = cmd.SupportId })).Items.SingleOrDefault();
        if (support == null) throw new NotFoundException($"Support {cmd.SupportId} not found");
        if (support.SupportDelivery is not Resources.Supports.ETransfer) throw new BusinessValidationException($"Support {cmd.SupportId} is not an etransfer and cannot be cancelled");
        var relatedPayments = ((SearchPaymentResponse)await paymentRepository.Query(new SearchPaymentRequest { ByLinkedSupportId = cmd.SupportId })).Items.Where(p => p.Status != PaymentStatus.Cancelled);
        if (relatedPayments.Any()) throw new BusinessValidationException($"Support {cmd.SupportId} already has associated payments");

        var id = ((ChangeSupportStatusCommandResult)await supportRepository.Manage(new ChangeSupportStatusCommand
        {
            Items = [new SupportStatusTransition { SupportId = cmd.SupportId, ToStatus = Resources.Supports.SupportStatus.Cancelled, Reason = cmd.Reason }]
        })).Ids.SingleOrDefault();
        return id;
    }

    public async Task<PrintRequestQueryResult> Handle(PrintRequestQuery query)
    {
        if (string.IsNullOrEmpty(query.PrintRequestId)) throw new ArgumentNullException(nameof(query.PrintRequestId));
        if (string.IsNullOrEmpty(query.RequestingUserId)) throw new ArgumentNullException(nameof(query.RequestingUserId));

        //get the print request
        var printRequest = (await printingRepository.Query(new QueryPrintRequests { ById = query.PrintRequestId })).Cast<ReferralPrintRequest>().SingleOrDefault();
        if (printRequest == null) throw new NotFoundException("print request not found", query.PrintRequestId);

        //get requesting user
        if (printRequest.RequestingUserId != query.RequestingUserId) throw new BusinessLogicException($"User {query.RequestingUserId} cannot query print for another user ({printRequest.RequestingUserId})");
        var requestingUser = mapper.Map<TeamMember>((await teamRepository.GetMembers(userId: printRequest.RequestingUserId, includeStatuses: activeOnlyStatus)).SingleOrDefault());
        if (requestingUser == null) throw new NotFoundException($"User {printRequest.RequestingUserId} not found", printRequest.RequestingUserId);

        //load the file
        var file = mapper.Map<Shared.Contracts.Events.EvacuationFile>((await evacuationRepository.Query(new Resources.Evacuations.EvacuationFilesQuery
        {
            FileId = printRequest.FileId
        })).Items.SingleOrDefault());
        if (file == null) throw new NotFoundException($"Evacuation file {printRequest.FileId} not found", printRequest.Id);

        var registrant = mapper.Map<RegistrantProfile>((await evacueesRepository.Query(new EvacueeQuery { EvacueeId = file.PrimaryRegistrantId })).Items.SingleOrDefault());

        if (registrant == null) throw new NotFoundException($"Registrant not found '{file.PrimaryRegistrantId}'", file.PrimaryRegistrantId);

        var ct = new CancellationTokenSource().Token;
        await evacuationFileLoader.Load(file, ct);

        //Find referrals to print
        var referrals = file.Supports.Where(s => printRequest.SupportIds.Contains(s.Id)).ToArray();
        if (referrals.Length != printRequest.SupportIds.Count())
            throw new BusinessLogicException($"Print request {printRequest.Id} has {printRequest.SupportIds.Count()} linked supports, but evacuation file {printRequest.FileId} doesn't have all of them");

        var isProduction = env.IsProduction();

        //convert referrals to html
        var generatedReferrals = (GenerateReferralsResponse)await supportingEngine.Generate(new GenerateReferralsRequest()
        {
            File = file,
            Supports = referrals,
            AddSummary = printRequest.IncludeSummary,
            AddWatermark = !isProduction,
            PrintingMember = requestingUser,
            Evacuee = registrant
        });

        var content = generatedReferrals.Content;
        var contentType = "text/html";

        await printingRepository.Manage(new MarkPrintRequestAsComplete { PrintRequestId = printRequest.Id });

        return new PrintRequestQueryResult
        {
            Content = content,
            ContentType = contentType,
            PrintedOn = printRequest.CreatedOn
        };
    }

    public async Task<string> Handle(ReprintSupportCommand cmd)
    {
        if (string.IsNullOrEmpty(cmd.FileId)) throw new ArgumentNullException(nameof(cmd.FileId));
        if (string.IsNullOrEmpty(cmd.RequestingUserId)) throw new ArgumentNullException(nameof(cmd.RequestingUserId));
        if (string.IsNullOrEmpty(cmd.SupportId)) throw new ArgumentNullException(nameof(cmd.SupportId));

        var requestingUser = (await teamRepository.GetMembers(userId: cmd.RequestingUserId, includeStatuses: activeOnlyStatus)).Single();

        var now = DateTime.UtcNow;
        var referralPrintId = await printingRepository.Manage(new SavePrintRequest
        {
            PrintRequest = new ReferralPrintRequest
            {
                FileId = cmd.FileId,
                SupportIds = [cmd.SupportId],
                IncludeSummary = cmd.IncludeSummary,
                RequestingUserId = requestingUser.Id,
                Type = ReferralPrintType.Reprint,
                Comments = cmd.ReprintReason,
                Title = $"referral-{cmd.SupportId}-{now:yyyyMMddhhmmss:R}"
            }
        });

        return referralPrintId;
    }

    public async System.Threading.Tasks.Task Handle(ProcessPendingSupportsCommand _)
    {
        var logger = telemetryProvider.Get(nameof(ProcessPendingSupportsCommand));
        var ct = new CancellationTokenSource().Token;

        var foundSupports = true;
        //handle limited number of pending support at a time
        while (foundSupports)
        {
            // get all pending scan supports
            var pendingScanSupports = ((SearchSupportQueryResult)await supportRepository.Query(new Resources.Supports.SearchSupportsQuery
            {
                ByStatus = Resources.Supports.SupportStatus.PendingScan,
                LimitNumberOfResults = 20
            })).Items.ToArray();

            foundSupports = pendingScanSupports.Any();

            if (foundSupports)
            {
                logger.LogInformation("Found {0} pending scan supports", pendingScanSupports.Length);
                // scan and get flags
                var response = (CheckSupportComplianceResponse)await supportingEngine.Validate(new CheckSupportComplianceRequest
                {
                    Supports = mapper.Map<IEnumerable<Shared.Contracts.Events.Support>>(pendingScanSupports)
                });

                await Parallel.ForEachAsync(response.Flags, ct, async (sf, ct) =>
                {
                    try
                    {
                        var currentSupport = ((SearchSupportQueryResult)await supportRepository.Query(new Resources.Supports.SearchSupportsQuery { ById = sf.Key.Id })).Items.SingleOrDefault();
                        if (currentSupport == null) throw new InvalidOperationException($"Couldn't find support {sf.Key.Id}");
                        var task = (EssTask)(await taskRepository.QueryTask(new TaskQuery { ById = currentSupport.TaskId })).Items.SingleOrDefault();
                        if (task == null) throw new InvalidOperationException($"Couldn't find task {currentSupport.TaskId}");

                        var flags = mapper.Map<IEnumerable<Resources.Supports.SupportFlag>>(sf.Value);

                        await supportRepository.Manage(new SubmitSupportForApprovalCommand
                        {
                            SupportId = currentSupport.Id,
                            Flags = flags
                        });

                        if (!flags.Any() && task.AutoApprovedEnabled)
                        {
                            await supportRepository.Manage(new ApproveSupportCommand
                            {
                                SupportId = currentSupport.Id
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        logger.LogError(e, "Failed to process pending support {0}", sf.Key.Id);
                    }
                });
            }
        }
    }

    public async System.Threading.Tasks.Task Handle(ProcessApprovedSupportsCommand _)
    {
        var logger = telemetryProvider.Get(nameof(ProcessApprovedSupportsCommand));
        var foundSupports = true;
        while (foundSupports)
        {
            var approvedSupports = ((PendingPaymentSupportSearchResponse)await searchEngine.Search(new PendingPaymentSupportSearchRequest())).Supports.ToArray();

            foundSupports = approvedSupports.Any();

            if (foundSupports)
            {
                logger.LogInformation("Found {0} approved supports", approvedSupports.Length);
                var payments = ((GeneratePaymentsResponse)await supportingEngine.Generate(new GeneratePaymentsRequest
                {
                    Supports = approvedSupports.Select(s => new Engines.Supporting.PayableSupport
                    {
                        Amount = s.Amount,
                        FileId = s.FileId,
                        SupportId = s.SupportId,
                        PayeeId = s.PayeeId,
                        Delivery = s.Delivery is PayableSupportInteracDelivery d
                            ? new PaymentDelivery
                            {
                                NotificationEmail = d.NotificationEmail,
                                NotificationPhone = d.NotificationPhone,
                                RecipientFirstName = d.RecipientFirstName,
                                RecipientLastName = d.RecipientLastName
                            }
                            : null
                    }).ToArray()
                })).Payments.ToArray();

                logger.LogInformation("Generating {0} payments", payments.Length);

                foreach (var payment in payments)
                {
                    await paymentRepository.Manage(new CreatePaymentRequest { Payment = payment });
                }
            }
        }
    }

    public async System.Threading.Tasks.Task Handle(OptOutSelfServeCommand cmd)
    {
        var file = (await evacuationRepository.Query(new Resources.Evacuations.EvacuationFilesQuery { FileId = cmd.EvacuationFileNumber })).Items.SingleOrDefault();
        if (file == null) throw new NotFoundException("file not found", cmd.EvacuationFileNumber);
        if (string.IsNullOrEmpty(cmd.RegistrantUserId) || file.PrimaryRegistrantUserId != cmd.RegistrantUserId) throw new InvalidOperationException($"Registrant {cmd.RegistrantUserId} does not match file {cmd.EvacuationFileNumber} primary registrant which is {file.PrimaryRegistrantUserId}");

        await evacuationRepository.Manage(new Resources.Evacuations.OptoutSelfServe { EvacuationFileNumber = cmd.EvacuationFileNumber });
    }

    public async Task<DraftSelfServeSupportQueryResponse> Handle(DraftSelfServeSupportQuery query)
    {
        var file = (await evacuationRepository.Query(new Resources.Evacuations.EvacuationFilesQuery { FileId = query.EvacuationFileNumber })).Items.SingleOrDefault();
        if (file == null) throw new NotFoundException("file not found", query.EvacuationFileNumber);
        if (file.PrimaryRegistrantUserId != query.RegistrantUserId) throw new InvalidOperationException($"Registrant {query.RegistrantUserId} does not match file {query.EvacuationFileNumber} primary registrant which is {file.PrimaryRegistrantUserId}");
        if (file.NeedsAssessment.EligibilityCheck == null || !file.NeedsAssessment.EligibilityCheck.Eligible) throw new BusinessValidationException($"File {query.EvacuationFileNumber} is not eligible for self serve");

        var task = (await taskRepository.QueryTask(new TaskQuery { ById = file.NeedsAssessment.EligibilityCheck.TaskNumber, ByStatus = [Resources.Tasks.TaskStatus.Active] })).Items.SingleOrDefault() as EssTask;
        if (task == null) throw new NotFoundException("task not found", file.NeedsAssessment.EligibilityCheck.TaskNumber);

        IEnumerable<SelfServeSupport> supports;
        if (query.Items == null)
        {
            //generate the supports based on the needs assessment
            var response = (GenerateSelfServeSupportsResponse)await supportingEngine.Generate(new GenerateSelfServeSupports(
                mapper.Map<IEnumerable<SelfServeSupportType>>(file.NeedsAssessment.EligibilityCheck.SupportSettings.Where(s => s.State == Resources.Evacuations.SelfServeSupportEligibilityState.Available).Select(s => s.Type)),
                file.NeedsAssessment.EligibilityCheck.From.Value.ToPST(),
                file.NeedsAssessment.EligibilityCheck.To.Value.ToPST(),
                file.NeedsAssessment.HouseholdMembers.Select(hm => new SelfServeHouseholdMember(hm.Id, hm.IsMinor)).ToList()));
            supports = response.Supports;
        }
        else
        {
            //recalculate the totals and return as is
            var response = (GenerateSelfServeSupportsResponse)await supportingEngine.Generate(new CalculateSelfServeSupports(query.Items, file.NeedsAssessment.HouseholdMembers.Select(hm => new SelfServeHouseholdMember(hm.Id, hm.IsMinor))));
            supports = response.Supports;
        }
        return new DraftSelfServeSupportQueryResponse
        {
            Items = supports,
            HouseholdMembers = mapper.Map<IEnumerable<HouseholdMember>>(file.NeedsAssessment.HouseholdMembers)
        };
    }

    public async System.Threading.Tasks.Task<string> Handle(CheckEligibileForSelfServeCommand cmd)
    {
        var file = (await evacuationRepository.Query(new Resources.Evacuations.EvacuationFilesQuery { FileId = cmd.EvacuationFileNumber })).Items.SingleOrDefault();
        if (file == null) throw new NotFoundException("file not found", cmd.EvacuationFileNumber);
        if (string.IsNullOrEmpty(cmd.RegistrantUserId) || file.PrimaryRegistrantUserId != cmd.RegistrantUserId) throw new InvalidOperationException($"Registrant {cmd.RegistrantUserId} does not match file {cmd.EvacuationFileNumber} primary registrant which is {file.PrimaryRegistrantUserId}");

        var eligibilityResult = ((ValidateSelfServeSupportsEligibilityResponse)await supportingEngine.Validate(new ValidateSelfServeSupportsEligibility(cmd.EvacuationFileNumber), default)).Eligibility;

        await evacuationRepository.Manage(new Resources.Evacuations.AddEligibilityCheck
        {
            EvacuationFileNumber = cmd.EvacuationFileNumber,
            Eligible = eligibilityResult.Eligible,
            TaskNumber = eligibilityResult.TaskNumber,
            HomeAddressReferenceId = eligibilityResult.HomeAddressReferenceId,
            From = eligibilityResult.From,
            To = eligibilityResult.To,
            Reason = eligibilityResult.Reason,
            EligibleSupports = eligibilityResult.EligibleSupportTypes
                .Select(s => new Resources.Evacuations.SelfServeSupportSetting(mapper.Map<Resources.Evacuations.SelfServeSupportType>(s), Resources.Evacuations.SelfServeSupportEligibilityState.Available))
                .Concat(eligibilityResult.OneTimePastSupportTypes.Select(s => new Resources.Evacuations.SelfServeSupportSetting(mapper.Map<Resources.Evacuations.SelfServeSupportType>(s), Resources.Evacuations.SelfServeSupportEligibilityState.NotAvailableOneTimeUsed)))
        });

        if (eligibilityResult.Eligible && !eligibilityResult.EligibleSupportTypes.Any())
        {
            // no supports are required, associate the file to the task
            await evacuationRepository.Manage(new Resources.Evacuations.AssignFileToTask { EvacuationFileNumber = file.Id, TaskNumber = eligibilityResult.TaskNumber });
        }

        return cmd.EvacuationFileNumber;
    }

    public async Task<DuplicateSupportsQueryResult> Handle(DuplicateSupportsQuery query)
    {
        var supports = (PotentialDuplicateSupportsQueryResult)await supportRepository.DuplicateQuery(new PotentialDuplicateSupportsQuery
        {
            Category = query.Category,
            FromDate = query.FromDate,
            ToDate = query.ToDate,
            Members = query.Members,
            FileId = query.FileId,
            IssuedBy = string.IsNullOrEmpty(query.IssuedBy) ? (Guid?)null : Guid.Parse(query.IssuedBy)
        });

        return new DuplicateSupportsQueryResult
        {
            DuplicateSupports = mapper.Map<IEnumerable<DuplicateSupportResult>>(supports.DuplicateSupports)
        };
    }

    public async Task<EligibilityCheckQueryResponse> Handle(EligibilityCheckQuery query)
    {
        var file = (await evacuationRepository.Query(new Resources.Evacuations.EvacuationFilesQuery { FileId = query.EvacuationFileNumber })).Items.SingleOrDefault();
        if (file == null) throw new NotFoundException("file not found", query.EvacuationFileNumber);
        if (string.IsNullOrEmpty(query.RegistrantUserId) || file.PrimaryRegistrantUserId != query.RegistrantUserId) throw new InvalidOperationException($"Registrant {query.RegistrantUserId} does not match file {query.EvacuationFileNumber} primary registrant which is {file.PrimaryRegistrantUserId}");

        var eligibility = file.NeedsAssessment.EligibilityCheck?.Eligible == true
            ? new SupportEligibility
            {
                IsEligible = true,
                TaskNumber = file.NeedsAssessment.EligibilityCheck?.TaskNumber,
                From = file.NeedsAssessment.EligibilityCheck?.From,
                To = file.NeedsAssessment.EligibilityCheck?.To,
                SupportSettings = mapper.Map<IEnumerable<SelfServeSupportSetting>>(file.NeedsAssessment.EligibilityCheck.SupportSettings)
            }
            : new SupportEligibility
            {
                IsEligible = false
            };

        return new EligibilityCheckQueryResponse
        {
            Eligibility = eligibility
        };
    }

    private async System.Threading.Tasks.Task SendEmailConfirmation(ETransferDetails eTransferDetails, string primaryRegistrantId, IEnumerable<Shared.Contracts.Events.Support> supports)
    {
        var evacuee = (await evacueesRepository.Query(new EvacueeQuery { EvacueeId = primaryRegistrantId })).Items.SingleOrDefault() ?? throw new InvalidOperationException($"registrants {primaryRegistrantId} not found");
        var emailAddress = evacuee.Email ?? eTransferDetails.ContactEmail;
        if (string.IsNullOrWhiteSpace(emailAddress)) return;

        decimal clothingAmount = 0, incidentalsAmount = 0, groceryAmount = 0, restaurantAmount = 0, shelterAllowanceAmount = 0;

        foreach (var item in supports)
        {
            switch (item)
            {
                case Shared.Contracts.Events.ClothingSupport clothingSupport:
                    clothingAmount += clothingSupport.TotalAmount;
                    break;

                case Shared.Contracts.Events.FoodGroceriesSupport groceriesSupport:
                    groceryAmount += groceriesSupport.TotalAmount;
                    break;

                case Shared.Contracts.Events.FoodRestaurantSupport restaurantSupport:
                    restaurantAmount += restaurantSupport.TotalAmount;
                    break;

                case Shared.Contracts.Events.IncidentalsSupport incidentalsSupport:
                    incidentalsAmount += incidentalsSupport.TotalAmount;
                    break;

                case Shared.Contracts.Events.ShelterAllowanceSupport shelterSupport:
                    shelterAllowanceAmount += shelterSupport.TotalAmount;
                    break;
            }
        }
        var totalAmount = clothingAmount + incidentalsAmount + groceryAmount + restaurantAmount + shelterAllowanceAmount;
        var endDate = supports.Count() > 0 ? supports.First().To.ToPST() : DateTime.UtcNow.AddDays(3).ToPST();
        string logoImage = this.ConvertImageToBase64(emcrLogoImageFileName);

        await SendEmailNotification(
            SubmissionTemplateType.ETransferConfirmation,
            emailAddress,
            $"{evacuee.LastName}, {evacuee.FirstName}",
            [
                KeyValuePair.Create("totalAmount", totalAmount.ToString("0.00")),
                KeyValuePair.Create("clothingAmount", (clothingAmount > 0) ? clothingAmount.ToString("0.00") : null),
                KeyValuePair.Create("incidentalsAmount", (incidentalsAmount > 0) ? incidentalsAmount.ToString("0.00") : null),
                KeyValuePair.Create("groceryAmount", (groceryAmount > 0) ? groceryAmount.ToString("0.00") : null),
                KeyValuePair.Create("restaurantAmount", (restaurantAmount > 0) ? restaurantAmount.ToString("0.00") : null),
                KeyValuePair.Create("shelterAllowanceAmount", (shelterAllowanceAmount > 0) ? shelterAllowanceAmount.ToString("0.00") : null),
                KeyValuePair.Create("recipientName", eTransferDetails.RecipientName),
                KeyValuePair.Create("notificationEmail", eTransferDetails.ETransferEmail),
                KeyValuePair.Create("endDate", endDate.ToString("MMMM d, yyyy")),
                KeyValuePair.Create("endTime", endDate.ToString("h:mm tt")),
                KeyValuePair.Create("emcrLogoImage", logoImage)
            ]);
    }
}
