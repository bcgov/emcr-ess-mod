using EMBC.ESS.Resources.Reports;
using EMBC.ESS.Resources.Tasks;
using EMBC.ESS.Shared.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EMBC.ESS.Resources.Supports
{
    public interface ISupportRepository
    {
        public Task<ManageSupportCommandResult> Manage(ManageSupportCommand cmd);

        public Task<SupportQueryResult> Query(SupportQuery query);
        public Task<DuplicateSupportQueryResult> DuplicateQuery(SupportQuery query);

    }

    public abstract class ManageSupportCommand
    {
    }

    public abstract class ManageSupportCommandResult
    {
    }

    public abstract class SupportQuery
    {
    }

    public abstract class SupportQueryResult
    {
    }

    public abstract class DuplicateSupportQueryResult
    {
    }

    public class CreateNewSupportsCommand : ManageSupportCommand
    {
        public string FileId { get; set; }
        public IEnumerable<Support> Supports { get; set; }
    }

    public class CreateNewSupportsCommandResult : ManageSupportCommandResult
    {
        public IEnumerable<Support> Supports { get; set; }
    }

    public class ChangeSupportStatusCommand : ManageSupportCommand
    {
        public IEnumerable<SupportStatusTransition> Items { get; set; }
    }

    public class ChangeSupportStatusCommandResult : ManageSupportCommandResult
    {
        public IEnumerable<string> Ids { get; set; }
    }

    public class SubmitSupportForApprovalCommand : ManageSupportCommand
    {
        public string SupportId { get; set; }

        public IEnumerable<SupportFlag> Flags { get; set; } = Array.Empty<SupportFlag>();
    }

    public class ApproveSupportCommand : ManageSupportCommand
    {
        public string SupportId { get; set; }
    }

    public class SubmitSupportForApprovalCommandResult : ManageSupportCommandResult
    { }

    public class SubmitSupportForReviewCommand : ManageSupportCommand
    {
        public string SupportId { get; set; }
    }

    public class SubmitSupportForReviewCommandResult : ManageSupportCommandResult
    { }


    public class CreateSupportConflictCommand : ManageSupportCommand
    {
        public ConflictMessage ConflictMessage { get; set; }
        public string SupportId { get; set; }
        public string FileId { get; set; }
        public Guid IssuedBy { get; set; }
    }

    public class CreateSupportConflictCommandResult : ManageSupportCommandResult
    {
        public Guid Id { get; set; }
    }


    public class SupportStatusTransition
    {
        public static SupportStatusTransition VoidSupport(string supportId, SupportVoidReason reason) => new SupportStatusTransition
        {
            SupportId = supportId,
            Reason = reason.ToString(),
            ToStatus = SupportStatus.Void
        };

        public string SupportId { get; set; }
        public SupportStatus ToStatus { get; set; }
        public string Reason { get; set; }
    }

    public class SearchSupportsQuery : SupportQuery
    {
        public string ById { get; set; }
        public string ByManualReferralId { get; set; }
        public string ByEvacuationFileId { get; set; }
        public SupportStatus? ByStatus { get; set; }
        public int? LimitNumberOfResults { get; set; }
    }

    public class SearchSupportQueryResult : SupportQueryResult
    {
        public IEnumerable<Support> Items { get; set; }
    }

    public class PotentialDuplicateSupportsQuery : SupportQuery
    {
        public string Category { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string[] Members { get; set; }
        public string FileId { get; set; }
        public Guid? IssuedBy { get; set; }

    }

    public class PotentialDuplicateSupportsQueryResult : DuplicateSupportQueryResult
    {
        public IEnumerable<DuplicateSupportResult> DuplicateSupports { get; set; }
    }

    public abstract record Support
    {
        public string Id { get; set; }
        public string FileId { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedByTeamMemberId { get; set; }
        public DateTime? IssuedOn { get; set; }
        public string OriginatingNeedsAssessmentId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public SupportStatus Status { get; set; } = SupportStatus.Active;
        public IEnumerable<string> IncludedHouseholdMembers { get; set; } = Array.Empty<string>();
        public IEnumerable<HouseholdMemberDto> HouseholdMembers { get; set; } = Array.Empty<HouseholdMemberDto>();
        public SupportDelivery SupportDelivery { get; set; }
        public bool IsPaperReferral => SupportDelivery is Referral r && r.ManualReferralId != null;
        public bool IsReferral => SupportDelivery is Referral;
        public bool IsEtransfer => SupportDelivery is ETransfer;
        public IEnumerable<SupportFlag> Flags { get; set; }
        public string TaskId { get; set; }
        public bool IsSelfServe { get; set; }
    }

    public record HouseholdMemberDto
    {
        public string DateOfBirth { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    public abstract record SupportDelivery
    { }

    public record Referral : SupportDelivery
    {
        public string? SupplierId { get; set; }
        public string? SupplierNotes { get; set; }
        public string IssuedToPersonName { get; set; }
        public string? ManualReferralId { get; set; }
        public string? IssuedByDisplayName { get; set; }
    }

    public abstract record ETransfer : SupportDelivery
    { }

    public record Interac : ETransfer
    {
        public string? NotificationEmail { get; set; }
        public string? NotificationMobile { get; set; }
        public string ReceivingRegistrantId { get; set; }
        public string RecipientFirstName { get; set; }
        public string RecipientLastName { get; set; }
    }

    public record ClothingSupport : Support
    {
        public bool ExtremeWeatherConditionsEnabled { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ApproverName { get; set; }
    }

    public record IncidentalsSupport : Support
    {
        public string ApprovedItems { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ApproverName { get; set; }
    }

    public record FoodGroceriesSupport : Support
    {
        public int NumberOfDays { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ApproverName { get; set; }
    }

    public record FoodRestaurantSupport : Support
    {
        public int NumberOfBreakfastsPerPerson { get; set; }
        public int NumberOfLunchesPerPerson { get; set; }
        public int NumberOfDinnersPerPerson { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public record ShelterHotelSupport : Support
    {
        public int NumberOfNights { get; set; }
        public int NumberOfRooms { get; set; }
    }

    public record ShelterBilletingSupport : Support
    {
        public int NumberOfNights { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public string HostCity { get; set; }
        public string HostEmail { get; set; }
        public string HostPhone { get; set; }
    }

    public record ShelterGroupSupport : Support
    {
        public int NumberOfNights { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string FacilityCity { get; set; }
        public string FacilityCommunityCode { get; set; }
        public string FacilityContactPhone { get; set; }
    }

    public record ShelterAllowanceSupport : Support
    {
        public int NumberOfNights { get; set; }
        public decimal TotalAmount { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
    }

    public record TransportationTaxiSupport : Support
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
    }

    public record TransportationOtherSupport : Support
    {
        public decimal TotalAmount { get; set; }
        public string TransportMode { get; set; }
    }



    public record ConflictMessage
    {
        public EssTask EssTask { get; set; }
        // public EvacuationFile EvacuationFile { get; set; }
        // public Reports.Evacuee Evacuee { get; set; }
        public string EvacueeName { get; set; }
        public string EvacueeDOB { get; set; }
        public Teams.TeamMember Responder { get; set; }
        public ConflictMessageScenario Scenario { get; set; }
        //public EvacuationFile MatchedEvacuationFile { get; set; }
        public string MatchedName { get; set; }
        public string MatchedDOB { get; set; }
        //   public decimal NameMatchScore { get; set; }
        public Reports.Support MatchedEvacueeSupport { get; set; }
    }


#pragma warning disable CA1008 // Enums should have zero value

    public enum SupportStatus
    {
        // referrals

        Active = 1,
        Expired = 174360000,
        Void = 2,

        // e-transfers

        PendingApproval = 174360001,
        Approved = 174360002,
        Paid = 174360003,
        Cancelled = 174360004,
        UnderReview = 174360005,
        PendingScan = 174360006,
        Issued = 174360007,
    }

    public enum SupportVoidReason
    {
        ErrorOnPrintedReferral = 174360000,
        NewSupplierRequired = 174360001,
        SupplierCouldNotMeetNeed = 174360002
    }

    public enum EraTwoOptions
    {
        Yes = 174360000,
        No = 174360001
    }

    public enum SupportMethod
    {
        Referral = 174360000,
        ETransfer = 174360001
    }

    public enum SupportType
    {
        FoodGroceries = 174360000,
        FoodRestaurant = 174360001,
        ShelterHotel = 174360002,
        ShelterBilleting = 174360003,
        ShelterGroup = 174360004,
        Incidentals = 174360005,
        Clothing = 174360006,
        TransporationTaxi = 174360007,
        TransportationOther = 174360008,
        ShelterAllowance = 174360009,
    }

#pragma warning restore CA1008 // Enums should have zero value

    public abstract class SupportFlag
    {
    }

    public class AmountOverridenSupportFlag : SupportFlag
    {
        internal const string FlagTypeId = "5a844ee3-b8ab-ec11-b831-00505683fbf4";
        public string Approver { get; set; }
    }

    public class DuplicateSupportFlag : SupportFlag
    {
        internal const string FlagTypeId = "8f3487d4-b8ab-ec11-b831-00505683fbf4";
        public string DuplicatedSupportId { get; set; }
    }

}
