using System;
using System.Linq;
using AutoMapper;
using EMBC.ESS.Resources.Supports;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;
using EMBC.Utilities.Extensions;

namespace EMBC.ESS.Resources.Reports
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            CreateMap<era_householdmember, Evacuee>()
                .ForMember(d => d.FileId, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_paperbasedessfile ?? s.era_EvacuationFileid.era_name))
                .ForMember(d => d.RegistrationCompleted, opts => opts.Ignore())
                .ForMember(d => d.TaskNumber, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_TaskId == null ? null : s.era_EvacuationFileid.era_TaskId.era_name))
                .ForMember(d => d.TaskStartDate, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_TaskId == null ? null : s.era_EvacuationFileid.era_TaskId.era_taskstartdate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd")))
                .ForMember(d => d.TaskStartTime, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_TaskId == null ? null : s.era_EvacuationFileid.era_TaskId.era_taskstartdate.Value.UtcDateTime.ToPST().ToString("hh:mm tt")))
                .ForMember(d => d.TaskEndDate, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_TaskId == null ? null : s.era_EvacuationFileid.era_TaskId.era_taskenddate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd")))
                .ForMember(d => d.TaskEndTime, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_TaskId == null ? null : s.era_EvacuationFileid.era_TaskId.era_taskenddate.Value.UtcDateTime.ToPST().ToString("hh:mm tt")))
                .ForMember(d => d.EvacuationFileStatus, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_essfilestatus))
                .ForMember(d => d.EvacuatedTo, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_TaskId == null ? null : s.era_EvacuationFileid.era_TaskId.era_JurisdictionID == null ? null : s.era_EvacuationFileid.era_TaskId.era_JurisdictionID.era_jurisdictionname))
                .ForMember(d => d.EvacuatedFrom, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_EvacuatedFromID == null ? null : s.era_EvacuationFileid.era_EvacuatedFromID.era_jurisdictionname))
                .ForMember(d => d.FacilityName, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid == null ? null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_registrationlocation))
                .ForMember(d => d.SelfRegistrationDate, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_selfregistrationdate.HasValue ? s.era_EvacuationFileid.era_selfregistrationdate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd") : null))
                .ForMember(d => d.SelfRegistrationTime, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_selfregistrationdate.HasValue ? s.era_EvacuationFileid.era_selfregistrationdate.Value.UtcDateTime.ToPST().ToString("hh:mm tt") : null))
                .ForMember(d => d.RegistrationCompletedDate, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_registrationcompleteddate.HasValue ? s.era_EvacuationFileid.era_registrationcompleteddate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd") : null))
                .ForMember(d => d.RegistrationCompletedTime, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? null : s.era_EvacuationFileid.era_registrationcompleteddate.HasValue ? s.era_EvacuationFileid.era_registrationcompleteddate.Value.UtcDateTime.ToPST().ToString("hh:mm tt") : null))
                .ForMember(d => d.IsHeadOfHousehold, opts => opts.MapFrom(s => s.era_isprimaryregistrant))
                .ForMember(d => d.LastName, opts => opts.MapFrom(s => s.era_lastname))
                .ForMember(d => d.FirstName, opts => opts.MapFrom(s => s.era_firstname))
                .ForMember(d => d.DateOfBirth, opts => opts.MapFrom(s => s.era_dateofbirth))
                .ForMember(d => d.Gender, opts => opts.ConvertUsing<GenderConverter, int?>(s => s.era_gender))
                .ForMember(d => d.PreferredName, opts => opts.MapFrom(s => string.Empty)) //the field in the wiki doesn't seem to exist
                .ForMember(d => d.Initials, opts => opts.MapFrom(s => s.era_initials))
                .ForMember(d => d.AddressLine1, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address1_line1))
                .ForMember(d => d.AddressLine2, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address1_line2))
                .ForMember(d => d.Community, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.era_City == null ? s.era_Registrant.address1_city : s.era_Registrant.era_City.era_jurisdictionname))
                .ForMember(d => d.Province, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.era_ProvinceState == null ? s.era_Registrant.address1_stateorprovince : s.era_Registrant.era_ProvinceState.era_code))
                .ForMember(d => d.PostalCode, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address1_postalcode))
                .ForMember(d => d.Country, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.era_Country == null ? s.era_Registrant.address1_country : s.era_Registrant.era_Country.era_countrycode))
                .ForMember(d => d.Phone, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address1_telephone1))
                .ForMember(d => d.Email, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.emailaddress1))
                .ForMember(d => d.MailingAddressLine1, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address2_line1))
                .ForMember(d => d.MailingAddressLine2, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address2_line2))
                .ForMember(d => d.MailingCommunity, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.era_MailingCity == null ? s.era_Registrant.address2_city : s.era_Registrant.era_MailingCity.era_jurisdictionname))
                .ForMember(d => d.MailingProvince, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.era_MailingProvinceState == null ? s.era_Registrant.address2_stateorprovince : s.era_Registrant.era_MailingProvinceState.era_code))
                .ForMember(d => d.MailingPostal, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.address2_postalcode))
                .ForMember(d => d.MailingCountry, opts => opts.MapFrom(s => s.era_Registrant == null ? null : s.era_Registrant.era_MailingCountry == null ? s.era_Registrant.address2_country : s.era_Registrant.era_MailingCountry.era_countrycode))
                .ForMember(d => d.Insurance, opts => opts.MapFrom(s => s.era_EvacuationFileid == null
                    ? InsuranceOption.Unknown
                    : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid == null
                        ? InsuranceOption.Unknown
                        : Enum.Parse<InsuranceOption>(((InsuranceOptionOptionSet)s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_insurancecoverage).ToString())))
                .ForMember(d => d.NumberOfPets, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? 0 : s.era_EvacuationFileid.era_era_evacuationfile_era_animal_ESSFileid.Count))
                .ForMember(d => d.RequiresClothing, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? (bool?)null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_canevacueeprovideclothing.GetValueOrDefault(0) == (int)NeedTrueFalseOptionSet.False))
                .ForMember(d => d.RequiresFood, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? (bool?)null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_canevacueeprovidefood.GetValueOrDefault(0) == (int)NeedTrueFalseOptionSet.False))
                .ForMember(d => d.RequiresIncidentals, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? (bool?)null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_canevacueeprovideincidentals.GetValueOrDefault(0) == (int)NeedTrueFalseOptionSet.False))
                .ForMember(d => d.RequiresTransportation, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? (bool?)null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_canevacueeprovidetransportation.GetValueOrDefault(0) == (int)NeedTrueFalseOptionSet.False))
                .ForMember(d => d.RequiresShelterReferral, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? (bool?)null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_shelteroptions.GetValueOrDefault(0) == (int)ShelterOptionSet.Referral))
                .ForMember(d => d.RequiresShelterAllowance, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? (bool?)null : s.era_EvacuationFileid.era_CurrentNeedsAssessmentid.era_shelteroptions.GetValueOrDefault(0) == (int)ShelterOptionSet.Allowance))
                .ForMember(d => d.NumberOfSupports, opts => opts.MapFrom(s => s.era_EvacuationFileid == null ? 0 : s.era_EvacuationFileid.era_era_evacuationfile_era_evacueesupport_ESSFileId.Count))
                .ForMember(d => d.SupportsTotalAmount, opts => opts.Ignore())
                .AfterMap((s, d) =>
                {
                    d.RegistrationCompleted = !string.IsNullOrEmpty(d.TaskNumber);
                    d.SupportsTotalAmount = s.era_EvacuationFileid.era_era_evacuationfile_era_evacueesupport_ESSFileId.Select(s => s.era_totalamount ?? 0).Sum();
                });

            Func<int?, string> resolveSupportType = s =>
            {
                var res = string.Empty;
                switch (s)
                {
                    case (int)SupportType.Food_Groceries:
                    case (int)SupportType.Food_Restaurant:
                        res = "Food";
                        break;

                    case (int)SupportType.Lodging_Hotel:
                    case (int)SupportType.Lodging_Billeting:
                    case (int)SupportType.Lodging_Group:
                    case (int)SupportType.Lodging_Shelter:
                        res = "Shelter";
                        break;

                    case (int)SupportType.Incidentals:
                        res = "Incidentals";
                        break;

                    case (int)SupportType.Clothing:
                        res = "Clothing";
                        break;

                    case (int)SupportType.Transportation_Taxi:
                    case (int)SupportType.Transportation_Other:
                        res = "Transportation";
                        break;

                    default:
                        break;
                }
                return res;
            };

            Func<int?, string> resolveSupportSubType = s =>
            {
                var res = string.Empty;
                switch (s)
                {
                    case (int)SupportType.Food_Groceries:
                        res = "Groceries";
                        break;

                    case (int)SupportType.Food_Restaurant:
                        res = "Restaurant";
                        break;

                    case (int)SupportType.Lodging_Hotel:
                        res = "Hotel";
                        break;

                    case (int)SupportType.Lodging_Billeting:
                        res = "Billeting";
                        break;

                    case (int)SupportType.Lodging_Group:
                        res = "Group";
                        break;

                    case (int)SupportType.Lodging_Shelter:
                        res = "Shelter Allowance";
                        break;

                    case (int)SupportType.Transportation_Taxi:
                        res = "Taxi";
                        break;

                    case (int)SupportType.Transportation_Other:
                        res = "Other";
                        break;

                    default:
                        break;
                }
                return res;
            };

            Func<int?, string> resolveSupportStatus = s =>
            {
                var res = string.Empty;
                switch (s)
                {
                    case (int)SupportStatus.Void:
                        res = "Void";
                        break;
                    case (int)SupportStatus.Paid:
                        res = "Paid";
                        break;
                    case (int)SupportStatus.Issued:
                        res = "Issued";
                        break;
                    case (int)SupportStatus.PendingScan:
                        res = "Pending Scan";
                        break;
                    case (int)SupportStatus.Expired:
                        res = "Expired";
                        break;
                    case (int)SupportStatus.UnderReview:
                        res = "Under Review";
                        break;
                    case (int)SupportStatus.Active:
                        res = "Active";
                        break;
                    case (int)SupportStatus.Approved:
                        res = "Approved";
                        break;
                    case (int)SupportStatus.Cancelled:
                        res = "Cancelled";
                        break;
                    case (int)SupportStatus.PendingApproval:
                        res = "Pending Approval";
                        break;
                    default:
                        break;
                }
                return res;
            };

            Func<int?, string> resolveSupportVoidReasonStatus = s =>
            {
                var res = string.Empty;
                switch (s)
                {
                    case (int)SupportVoidReason.SupplierCouldNotMeetNeed:
                        res = "Supplier Could Not Meet Need";
                        break;
                    case (int)SupportVoidReason.NewSupplierRequired:
                        res = "New Supplier Required";
                        break;
                    case (int)SupportVoidReason.ErrorOnPrintedReferral:
                        res = "Error On Printed Referral";
                        break;
                }
                return res;
            };

            Func<era_evacueesupport?, string> resolveSupportDeliveryType = s =>
            {
                var res = string.Empty;
                switch (s.era_supportdeliverytype)
                {
                    case (int)SupportMethod.ETransfer:
                        res = "e-Transfer";
                        break;

                    case (int)SupportMethod.Referral:
                        res = string.IsNullOrEmpty(s.era_manualsupport) ? "Digital Referral" : "Paper Referral";
                        break;

                    default:
                        break;
                }
                return res;
            };

            CreateMap<era_evacueesupport, Support>()
                .ForMember(d => d.FileId, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_paperbasedessfile ?? s.era_EvacuationFileId.era_name))
                .ForMember(d => d.TaskNumber, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_TaskId == null ? null : s.era_EvacuationFileId.era_TaskId.era_name))
                .ForMember(d => d.TaskStartDate, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_TaskId == null ? null : s.era_EvacuationFileId.era_TaskId.era_taskstartdate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd")))
                .ForMember(d => d.TaskStartTime, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_TaskId == null ? null : s.era_EvacuationFileId.era_TaskId.era_taskstartdate.Value.UtcDateTime.ToPST().ToString("hh:mm tt")))
                .ForMember(d => d.TaskEndDate, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_TaskId == null ? null : s.era_EvacuationFileId.era_TaskId.era_taskenddate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd")))
                .ForMember(d => d.TaskEndTime, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_TaskId == null ? null : s.era_EvacuationFileId.era_TaskId.era_taskenddate.Value.UtcDateTime.ToPST().ToString("hh:mm tt")))
                .ForMember(d => d.EvacuationFileStatus, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_essfilestatus))
                .ForMember(d => d.EvacuatedTo, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_TaskId == null ? null : s.era_EvacuationFileId.era_TaskId.era_JurisdictionID == null ? null : s.era_EvacuationFileId.era_TaskId.era_JurisdictionID.era_jurisdictionname))
                .ForMember(d => d.EvacuatedFrom, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_EvacuatedFromID == null ? null : s.era_EvacuationFileId.era_EvacuatedFromID.era_jurisdictionname))
                .ForMember(d => d.FacilityName, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_CurrentNeedsAssessmentid == null ? null : s.era_EvacuationFileId.era_CurrentNeedsAssessmentid.era_registrationlocation))
                .ForMember(d => d.SelfRegistrationDate, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_selfregistrationdate.HasValue ? s.era_EvacuationFileId.era_selfregistrationdate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd") : null))
                .ForMember(d => d.SelfRegistrationTime, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_selfregistrationdate.HasValue ? s.era_EvacuationFileId.era_selfregistrationdate.Value.UtcDateTime.ToPST().ToString("hh:mm tt") : null))
                .ForMember(d => d.RegistrationCompletedDate, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_registrationcompleteddate.HasValue ? s.era_EvacuationFileId.era_registrationcompleteddate.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd") : null))
                .ForMember(d => d.RegistrationCompletedTime, opts => opts.MapFrom(s => s.era_EvacuationFileId == null ? null : s.era_EvacuationFileId.era_registrationcompleteddate.HasValue ? s.era_EvacuationFileId.era_registrationcompleteddate.Value.UtcDateTime.ToPST().ToString("hh:mm tt") : null))
                .ForMember(d => d.PurchaserOfGoods, opts => opts.MapFrom(s => s.era_purchaserofgoods))
                .ForMember(d => d.SupportNumber, opts => opts.MapFrom(s => s.era_manualsupport ?? s.era_name))
                .ForMember(d => d.SupportStatus, opts => opts.MapFrom(s => resolveSupportStatus(s.statuscode)))
                .ForMember(d => d.SupportStatusReason, opts => opts.MapFrom(s => resolveSupportVoidReasonStatus(s.era_voidreason)))
                .ForMember(d => d.SupportType, opts => opts.MapFrom(s => resolveSupportType(s.era_supporttype)))
                .ForMember(d => d.SubSupportType, opts => opts.MapFrom(s => resolveSupportSubType(s.era_supporttype)))
                .ForMember(d => d.SupportDeliveryType, opts => opts.MapFrom(s => resolveSupportDeliveryType(s)))
                .ForMember(d => d.ValidFromDate, opts => opts.MapFrom(s => s.era_validfrom.HasValue ? s.era_validfrom.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd") : null))
                .ForMember(d => d.ValidFromTime, opts => opts.MapFrom(s => s.era_validfrom.HasValue ? s.era_validfrom.Value.UtcDateTime.ToPST().ToString("hh:mm tt") : null))
                .ForMember(d => d.NumberOfDays, opts => opts.MapFrom(s => s.era_validfrom.HasValue && s.era_validto.HasValue ? (s.era_validto.Value.DateTime - s.era_validfrom.Value.DateTime).Days : 0))
                .ForMember(d => d.ValidToDate, opts => opts.MapFrom(s => s.era_validto.HasValue ? s.era_validto.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd") : null))
                .ForMember(d => d.ValidToTime, opts => opts.MapFrom(s => s.era_validto.HasValue ? s.era_validto.Value.UtcDateTime.ToPST().ToString("hh:mm tt") : null))
                .ForMember(d => d.NumberOfEvacuees, opts => opts.MapFrom(s => s.era_era_householdmember_era_evacueesupport.Count))
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ForMember(d => d.Payee, opts => opts.MapFrom(s => s._era_payeeid_value == null || s.era_PayeeId == null ? null : $"{s.era_PayeeId.firstname} {s.era_PayeeId.lastname}"))
                .ForMember(d => d.Breakfasts, opts => opts.MapFrom(s => s.era_numberofbreakfasts))
                .ForMember(d => d.Lunches, opts => opts.MapFrom(s => s.era_numberoflunches))
                .ForMember(d => d.Dinners, opts => opts.MapFrom(s => s.era_numberofdinners))
                .ForMember(d => d.NumberOfRooms, opts => opts.MapFrom(s => s.era_numberofrooms))
                .ForMember(d => d.NumberOfNights, opts => opts.MapFrom(s => s.era_numberofnights))
                .ForMember(d => d.SupportCreatedDate, opts => opts.MapFrom(s => s.createdon.Value.UtcDateTime.ToPST().ToString("yyyy/MM/dd")))
                .ForMember(d => d.SupportCreatedTime, opts => opts.MapFrom(s => s.createdon.Value.UtcDateTime.ToPST().ToString("hh:mm tt")))
                .ForMember(d => d.ExtremeWeatherConditionsEnabled, opts => opts.MapFrom(s => s.era_extremewinterconditions == (int)EraTwoOptions.Yes))
                .ForMember(d => d.NumberOfMeals, opts => opts.MapFrom(s => s.era_numberofmeals))
                .ForMember(d => d.SupplierLegalName, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_name : null))
                .ForMember(d => d.SupplierName, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_suppliername : null))
                .ForMember(d => d.AddressLine1, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_addressline1 : null))
                .ForMember(d => d.AddressLine2, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_addressline2 : null))
                .ForMember(d => d.City, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_RelatedCity != null ? s.era_SupplierId.era_RelatedCity.era_jurisdictionname : null : null))
                .ForMember(d => d.PostalCode, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_postalcode : null))
                .ForMember(d => d.Phone, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_telephone : null))
                .ForMember(d => d.Fax, opts => opts.MapFrom(s => s.era_SupplierId != null ? s.era_SupplierId.era_fax : null))
                .ForMember(d => d.LodgingName, opts => opts.MapFrom(s => s.era_lodgingname))
                .ForMember(d => d.LodgingAddress, opts => opts.MapFrom(s => s.era_lodgingaddress))
                .ForMember(d => d.BilletingCity, opts => opts.MapFrom(s => s.era_lodgingcity))
                .ForMember(d => d.GroupLodgingCity, opts => opts.MapFrom(s => s.era_GroupLodgingCityID != null ? s.era_GroupLodgingCityID.era_jurisdictionname : null))
                .ForMember(d => d.LodgingContactNumber, opts => opts.MapFrom(s => s.era_lodgingcontactnumber))
                .ForMember(d => d.LodgingEmail, opts => opts.MapFrom(s => s.era_lodgingemailaddress))
                ;



            // New mapping for era_supportconflictmessage to ConflictMessage
            CreateMap<era_supportconflictmessage, ConflictMessage>()
            .ForMember(d => d.EssTask, opts => opts.MapFrom(s => s.era_ESSTask))
            //.ForMember(d => d.EvacuationFile, opts => opts.MapFrom(s => s.era_ESSFile))
            .ForMember(d => d.EvacueeSupport, opts => opts.MapFrom(s => s.era_EvacueeSupport))
            //.ForMember(d => d.MatchedEvacuationFile, opts => opts.MapFrom(s => s.era_MatchedESSFile))
            //.ForMember(d => d.Evacuee, opts => opts.MapFrom(s => s.era_Registrant))
            .ForMember(d => d.Responder, opts => opts.MapFrom(s => s.era_Responder))
            .ForMember(d => d.EvacueeDOB, opts => opts.MapFrom(s => s.era_evacueedob))
            .ForMember(d => d.EvacueeName, opts => opts.MapFrom(s => s.era_evacueename))
            .ForMember(d => d.MatchedDOB, opts => opts.MapFrom(s => s.era_matcheddob))
            .ForMember(d => d.MatchedName, opts => opts.MapFrom(s => s.era_matchedname))
            // .ForMember(d => d.nam, opts => opts.MapFrom(s => s.era_name))

            .ForMember(d => d.Scenario, opts => opts.MapFrom(s => s.era_scenario))
            .ReverseMap();
        }
    }

#pragma warning disable CA1008 // Enums should have zero value

    public enum InsuranceOptionOptionSet
    {
        No = 174360000,
        Yes = 174360001,
        Unsure = 174360002,
        Unknown = 174360003
    }

    public enum EraTwoOptions
    {
        Yes = 174360000,
        No = 174360001
    }

    public enum NeedTrueFalseOptionSet
    {
        True = 174360000,
        False = 174360001
    }

    public enum ShelterOptionSet
    {
        Allowance = 174360000,
        Referral = 174360001
    }

#pragma warning restore CA1008 // Enums should have zero value

    public class GenderConverter : IValueConverter<string, int?>, IValueConverter<int?, string>
    {
        public int? Convert(string sourceMember, ResolutionContext context) => sourceMember?.ToLowerInvariant() switch
        {
            "male" => 1,
            "female" => 2,
            "x" => 3,
            _ => null
        };

        public string Convert(int? sourceMember, ResolutionContext context) => sourceMember switch
        {
            1 => "Male",
            2 => "Female",
            3 => "X",
            _ => null
        };
    }
}
