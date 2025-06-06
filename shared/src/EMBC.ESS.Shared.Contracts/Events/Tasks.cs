using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace EMBC.ESS.Shared.Contracts.Events
{
    /// <summary>
    /// search suppliers list by task
    /// </summary>
    public class TasksSearchQuery : Query<TasksSearchQueryResult>
    {
        public string TaskId { get; set; }
    }

    public class TasksSearchQueryResult
    {
        public IEnumerable<IncidentTask> Items { get; set; }
    }

    /// <summary>
    /// query the supplier list for a task
    /// </summary>
    public class SuppliersListQuery : Query<SuppliersListQueryResponse>
    {
        public string TaskId { get; set; }
    }

    public class SuppliersListQueryResponse
    {
        public IEnumerable<SupplierDetails> Items { get; set; }
    }

    public class IncidentTask
    {
        public string Id { get; set; } //task number
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string CommunityCode { get; set; }
        public string Description { get; set; }
        public IncidentTaskStatus Status { get; set; }
        public bool RemoteExtensionsEnabled { get; set; }
        public bool SelfServeEnabled { get; set; }
        public IEnumerable<SupportLimits> SupportLimits { get; set; }
        public bool ExtremeWeatherConditionsEnabled { get; set; }
    }

    public enum IncidentTaskStatus
    {
        Active,
        Expired
    }

    public record SupportLimits
    {
        public SupportType SupportType { get; set; }
        public DateTime SupportLimitStartDate { get; set; }
        public DateTime SupportLimitEndDate { get; set; }
        public bool ExtensionAvailable { get; set; }
    }

    public enum SupportType
    {
        [Display(Name = "Food Groceries")]
        FoodGroceries = 174360000,
        [Display(Name = "Food Restaurant")]
        FoodRestaurant = 174360001,
        [Display(Name = "Shelter Hotel")]
        ShelterHotel = 174360002,
        [Display(Name = "Shelter Billeting")]
        ShelterBilleting = 174360003,
        [Display(Name = "Shelter Group")]
        ShelterGroup = 174360004,
        [Display(Name = "Incidentals")]
        Incidentals = 174360005,
        [Display(Name = "Clothing")]
        Clothing = 174360006,
        [Display(Name = "Transportation Taxi")]
        TransporationTaxi = 174360007,
        [Display(Name = "Transportation Other")]
        TransportationOther = 174360008,
        [Display(Name = "Shelter Allowance")]
        ShelterAllowance = 174360009
    }
}
