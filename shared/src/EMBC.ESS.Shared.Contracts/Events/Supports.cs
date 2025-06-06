using System;
using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Text.Json.Serialization;

namespace EMBC.ESS.Shared.Contracts.Events
{
    /// <summary>
    /// Proccess supports for a file, return a print request id
    /// </summary>
    public class ProcessSupportsCommand : Command
    {
        public string RequestingUserId { get; set; }
        public bool IncludeSummaryInReferralsPrintout { get; set; }
        public string FileId { get; set; }
        public IEnumerable<Support> Supports { get; set; }
    }

    /// <summary>
    /// Proccess paper supports for a file without printing them
    /// </summary>
    public class ProcessPaperSupportsCommand : Command
    {
        public string RequestingUserId { get; set; }
        public string FileId { get; set; }
        public IEnumerable<Support> Supports { get; set; }
    }

    /// <summary>
    /// void a support in a file by id
    /// </summary>
    public class VoidSupportCommand : Command
    {
        public string FileId { get; set; }
        public string SupportId { get; set; }
        public SupportVoidReason VoidReason { get; set; }
    }

    /// <summary>
    /// void a support in a file by id
    /// </summary>
    public class CancelSupportCommand : Command
    {
        public string FileId { get; set; }
        public string SupportId { get; set; }
        public string Reason { get; set; }
    }

    public class CreateSupportConflictCommandRequest : Command
    {
        public string[] Members { get; set; }
        public string SupportId { get; set; }
        public string FileId { get; set; }
        public string IssuedBy { get; set; }
    }

    public class CreateSupportConflictCommandResult
    {
        public Guid Id { get; set; }
    }

    /// <summary>
    /// query a print request's content
    /// </summary>
    public class PrintRequestQuery : Query<PrintRequestQueryResult>
    {
        public string PrintRequestId { get; set; }
        public string RequestingUserId { get; set; }
    }

    public class PrintRequestQueryResult
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }
        public DateTime PrintedOn { get; set; }
    }


    public class DuplicateSupportsQuery : Query<DuplicateSupportsQueryResult>
    {
        public string Category { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public string[] Members { get; set; }
        public string FileId { get; set; }
        public string IssuedBy { get; set; }
    }

    public class DuplicateSupportResult
    {
        public string essFileId { get; set; }
        public string supportStartDate { get; set; }
        public string supportEndDate { get; set; }
        public string supportStartTime { get; set; }
        public string supportEndTime { get; set; }
        public int duplicateSupportScenario { get; set; }
        public string householdMemberFirstName { get; set; }
        public string householdMemberLastName { get; set; }
        public string supportMemberDOB { get; set; }
        public string supportCategory { get; set; }
        public string supportSubCategory { get; set; }
        public string supportMemberFirstName { get; set; }
        public string supportMemberLastName { get; set; }
        public string householdMemberDOB { get; set; }
    }

    public class DuplicateSupportsQueryResult
    {
        public IEnumerable<DuplicateSupportResult> DuplicateSupports { get; set; }
    }

    /// <summary>
    /// create a new support reprint request, returns the new print request id
    /// </summary>
    public class ReprintSupportCommand : Command
    {
        public string FileId { get; set; }
        public string RequestingUserId { get; set; }
        public string SupportId { get; set; }
        public string ReprintReason { get; set; }
        public bool IncludeSummary { get; set; }
    }

    /// <summary>
    /// process new supports
    /// </summary>
    public class ProcessPendingSupportsCommand : Command
    { }

    public class ProcessApprovedSupportsCommand : Command
    { }

    public class ProcessPendingPaymentsCommand : Command
    { }

    public class ReconcilePaymentsCommand : Command
    { }

    public class ReconcileSupplierInfoCommand : Command
    { }

    public class FullReconcilePaymentsCommand : Command
    { }

    [JsonConverter(typeof(PolymorphicJsonConverter<Support>))]
    public abstract class Support
    {
        public string Id { get; set; }
        public string FileId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public TeamMember CreatedBy { get; set; }
        public DateTime? IssuedOn { get; set; }
        public TeamMember IssuedBy { get; set; }
        public string OriginatingNeedsAssessmentId { get; set; }
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public SupportStatus Status { get; set; } = SupportStatus.Active;
        public IEnumerable<string> IncludedHouseholdMembers { get; set; } = Array.Empty<string>();
        public IEnumerable<HouseholdMemberDto> HouseholdMembers { get; set; } = Array.Empty<HouseholdMemberDto>();
        public SupportDelivery SupportDelivery { get; set; }
        public IEnumerable<SupportFlag> Flags { get; set; } = Array.Empty<SupportFlag>();
        public bool IsSelfServe { get; set; }
    }

    public record HouseholdMemberDto
    {
        public string DateOfBirth { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
    }

    [JsonConverter(typeof(PolymorphicJsonConverter<SupportDelivery>))]
    public abstract class SupportDelivery
    {
    }

    public class Referral : SupportDelivery
    {
        public string SupplierNotes { get; set; }
        public string IssuedToPersonName { get; set; }
        public SupplierDetails SupplierDetails { get; set; }
        public string ManualReferralId { get; set; }
        public bool IsPaperReferral => !string.IsNullOrEmpty(ManualReferralId);
    }

    public abstract class ETransfer : SupportDelivery
    { }

    public class Interac : ETransfer
    {
        public string NotificationEmail { get; set; }
        public string NotificationMobile { get; set; }
        public string ReceivingRegistrantId { get; set; }
        public string RecipientFirstName { get; set; }
        public string RecipientLastName { get; set; }
        public string SecurityQuestion { get; set; }
        public string SecurityAnswer { get; set; }
        public string RelatedPaymentId { get; set; }
    }

    public class SupplierDetails
    {
        public string Id { get; set; }
        public Address Address { get; set; }
        public string LegalName { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
    }

    public class ClothingSupport : Support
    {
        public decimal TotalAmount { get; set; }
        public string ApproverName { get; set; }
        public bool ExtremeWeatherConditionsEnabled { get; set; }
    }

    public class IncidentalsSupport : Support
    {
        public string ApprovedItems { get; set; }
        public decimal TotalAmount { get; set; }
        public string ApproverName { get; set; }
    }

    public class FoodGroceriesSupport : Support
    {
        public int NumberOfDays { get; set; }
        public decimal TotalAmount { get; set; }
        public string ApproverName { get; set; }
    }

    public class FoodRestaurantSupport : Support
    {
        public int NumberOfBreakfastsPerPerson { get; set; }
        public int NumberOfLunchesPerPerson { get; set; }
        public int NumberOfDinnersPerPerson { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class ShelterHotelSupport : Support
    {
        public int NumberOfNights { get; set; }
        public int NumberOfRooms { get; set; }
    }

    public class ShelterBilletingSupport : Support
    {
        public int NumberOfNights { get; set; }
        public string HostName { get; set; }
        public string HostAddress { get; set; }
        public string HostCity { get; set; }
        public string HostEmail { get; set; }
        public string HostPhone { get; set; }
    }

    public class ShelterGroupSupport : Support
    {
        public int NumberOfNights { get; set; }
        public string FacilityName { get; set; }
        public string FacilityAddress { get; set; }
        public string FacilityCity { get; set; }
        public string FacilityCommunityCode { get; set; }
        public string FacilityContactPhone { get; set; }
    }

    public class ShelterAllowanceSupport : Support
    {
        public int NumberOfNights { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransportationTaxiSupport : Support
    {
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
    }

    public class TransportationOtherSupport : Support
    {
        public decimal TotalAmount { get; set; }
        public string TransportMode { get; set; }
    }

    public enum SupportStatus
    {
        Active,
        Expired,
        Void,
        PendingApproval,
        Approved,
        Paid,
        Cancelled,
        UnderReview,
        Issued
    }

    public enum SupportVoidReason
    {
        ErrorOnPrintedReferral,
        NewSupplierRequired,
        SupplierCouldNotMeetNeed,
        UserInitiatedCancellation
    }

    [JsonConverter(typeof(PolymorphicJsonConverter<SupportFlag>))]
    public abstract class SupportFlag
    {
    }

    public class DuplicateSupportFlag : SupportFlag
    {
        public string DuplicatedSupportId { get; set; }
    }

    public class AmountExceededSupportFlag : SupportFlag
    {
        public string Approver { get; set; }
    }
}
