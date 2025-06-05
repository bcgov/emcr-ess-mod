using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using EMBC.ESS.Utilities.Dynamics.Microsoft.Dynamics.CRM;

namespace EMBC.ESS.Resources.Supports
{
    public class Mappings : Profile
    {
        public Mappings()
        {
            CreateMap<IEnumerable<era_evacueesupport>, IEnumerable<Support>>()
                .ConvertUsing<SupportTypeConverter>();

            CreateMap<IEnumerable<Support>, IEnumerable<era_evacueesupport>>()
                .ConvertUsing<SupportTypeConverter>();

            CreateMap<era_evacueesupport, Support>()
                .IgnoreAllPropertiesWithAnInaccessibleSetter()
                .IncludeAllDerived()
                .ForMember(d => d.Id, opts => opts.MapFrom(s => s.era_name))
                .ForMember(d => d.FileId, opts => opts.MapFrom(s => s.era_EvacuationFileId.era_name))
                .ForMember(d => d.TaskId, opts => opts.MapFrom(s => s.era_EvacuationFileId.era_TaskId.era_name))
                .ForMember(d => d.CreatedOn, opts => opts.MapFrom(s => s.createdon.Value.UtcDateTime))
                .ForMember(d => d.CreatedByTeamMemberId, opts => opts.MapFrom(s => s._era_issuedbyid_value))
                .ForMember(d => d.IssuedOn, opts => opts.MapFrom(s => s.era_paperreferralcompletedon.HasValue
                    ? s.era_paperreferralcompletedon.Value.UtcDateTime
                    : s.createdon.Value.UtcDateTime))
                .ForMember(d => d.OriginatingNeedsAssessmentId, opts => opts.MapFrom(s => s._era_needsassessmentid_value))
                .ForMember(d => d.From, opts => opts.MapFrom(s => s.era_validfrom.HasValue ? s.era_validfrom.Value.UtcDateTime : DateTime.MinValue))
                .ForMember(d => d.To, opts => opts.MapFrom(s => s.era_validto.HasValue ? s.era_validto.Value.UtcDateTime : DateTime.MinValue))
                .ForMember(d => d.Status, opts => opts.MapFrom(s => s.statuscode))
                .ForMember(d => d.IncludedHouseholdMembers, opts => opts.MapFrom(s => s.era_era_householdmember_era_evacueesupport.Select(m => m.era_householdmemberid)))
                .ForMember(d => d.HouseholdMembers, opts => opts.MapFrom(s => s.era_era_householdmember_era_evacueesupport.Select(h => new HouseholdMemberDto
                {
                    DateOfBirth = h.era_dateofbirth.ToString(),
                    FirstName = h.era_firstname,
                    LastName = h.era_lastname
                })))

                .ForMember(d => d.SupportDelivery, opts => opts.MapFrom(s => s))
                .ForMember(d => d.Flags, opts => opts.MapFrom(s => s.era_era_evacueesupport_era_supportflag_EvacueeSupport))
                .ForMember(d => d.IsSelfServe, opts => opts.MapFrom(s => s.era_selfservesupport))
                ;

            Func<SupportDelivery, SupportMethod?> resolveSupportDelieveryType = sd => sd switch
            {
                Referral => SupportMethod.Referral,
                Interac => SupportMethod.ETransfer,

                _ => null
            };

            CreateMap<Support, era_evacueesupport>(MemberList.Source)
                .IgnoreAllSourcePropertiesWithAnInaccessibleSetter()
                .IncludeAllDerived()
                // this is a trick to include support delivery flattening mappings when mapping to Dynamics
                // more support deliveries should be also included here
                .IncludeMembers(s => s.SupportDelivery as Referral, s => s.SupportDelivery as Interac)
                // support delivery must be mapped at this level, can't be at the included mapping
                .ForMember(d => d.era_supportdeliverytype, opts => opts.MapFrom(s => resolveSupportDelieveryType(s.SupportDelivery)))
                .ForSourceMember(s => s.HouseholdMembers, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.SupportDelivery, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.IncludedHouseholdMembers, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.Status, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.FileId, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.OriginatingNeedsAssessmentId, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.IssuedOn, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.Flags, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.TaskId, opts => opts.DoNotValidate())

                .ForMember(s => s.era_selfservesupport, opts => opts.MapFrom(s => s.IsSelfServe))
                .ForMember(d => d.era_name, opts => opts.MapFrom(s => s.Id))
                .ForMember(d => d.era_validfrom, opts => opts.MapFrom(s => s.From))
                .ForMember(d => d.era_validto, opts => opts.MapFrom(s => s.To))
                .ForMember(d => d._era_issuedbyid_value, opts => opts.MapFrom(s => s.CreatedByTeamMemberId))
                .ForMember(d => d.era_paperreferralcompletedon, opts => opts.MapFrom(s => s.IsPaperReferral ? s.IssuedOn : null))
              ;

            CreateMap<era_evacueesupport, SupportDelivery>()
                .ConvertUsing<SupportDeliveryTypeConverter>();

            CreateMap<era_evacueesupport, Referral>()
                .ForMember(d => d.IssuedToPersonName, opts => opts.MapFrom(s => s.era_purchaserofgoods))
                .ForMember(d => d.SupplierId, opts => opts.MapFrom(s => s._era_supplierid_value))
                .ForMember(d => d.SupplierNotes, opts => opts.MapFrom(s => s.era_suppliernote))
                .ForMember(d => d.ManualReferralId, opts => opts.MapFrom(s => s.era_manualsupport))
                .ForMember(d => d.IssuedByDisplayName, opts => opts.MapFrom(s => s.era_paperissuedby))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .ForSourceMember(s => s.IssuedByDisplayName, opts => opts.DoNotValidate())
                .ForMember(d => d._era_supplierid_value, opts => opts.MapFrom(s => s.SupplierId))
                .ForMember(d => d.era_suppliernote, opts => opts.MapFrom(s => s.SupplierNotes))
                .ForMember(d => d.era_manualsupport, opts => opts.MapFrom(s => s.ManualReferralId))
                .ForMember(d => d.era_paperissuedby, opts => opts.MapFrom(s => s.ManualReferralId != null ? s.IssuedByDisplayName : null))
                ;

            CreateMap<era_evacueesupport, Interac>()
                .ForMember(d => d.NotificationEmail, opts => opts.MapFrom(s => s.era_notificationemailaddress))
                .ForMember(d => d.NotificationMobile, opts => opts.MapFrom(s => s.era_notificationphonenumber))
                .ForMember(d => d.ReceivingRegistrantId, opts => opts.MapFrom(s => s._era_payeeid_value))
                .ForMember(d => d.RecipientFirstName, opts => opts.MapFrom(s => s.era_PayeeId == null ? null : s.era_PayeeId.firstname))
                .ForMember(d => d.RecipientLastName, opts => opts.MapFrom(s => s.era_PayeeId == null ? null : s.era_PayeeId.lastname))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .ForSourceMember(s => s.RecipientFirstName, opts => opts.DoNotValidate())
                .ForSourceMember(s => s.RecipientLastName, opts => opts.DoNotValidate())
                .ForMember(d => d.era_notificationemailaddress, opts => opts.MapFrom(s => s.NotificationEmail))
                .ForMember(d => d.era_notificationphonenumber, opts => opts.MapFrom(s => s.NotificationMobile))
                .ForMember(d => d._era_payeeid_value, opts => opts.MapFrom(s => s.ReceivingRegistrantId))
                ;

            CreateMap<era_evacueesupport, ClothingSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ForMember(d => d.ExtremeWeatherConditionsEnabled, opts => opts.MapFrom(s => s.era_extremewinterconditions == (int)EraTwoOptions.Yes))
                .ForMember(d => d.ApproverName, opts => opts.MapFrom(s => s.era_supportoverrideauthority))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForSourceMember(s => s.ExtremeWeatherConditionsEnabled, opts => opts.DoNotValidate())
                .ForMember(d => d.era_extremewinterconditions, opts => opts.MapFrom(s => s.ExtremeWeatherConditionsEnabled ? EraTwoOptions.Yes : EraTwoOptions.No))
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.Clothing))
                .ForMember(d => d.era_amountoverride, opts => opts.MapFrom(s => !string.IsNullOrEmpty(s.ApproverName)))
                ;

            CreateMap<era_evacueesupport, IncidentalsSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ForMember(d => d.ApprovedItems, opts => opts.MapFrom(s => s.era_approveditems))
                .ForMember(d => d.ApproverName, opts => opts.MapFrom(s => s.era_supportoverrideauthority))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.Incidentals))
                .ForMember(d => d.era_amountoverride, opts => opts.MapFrom(s => !string.IsNullOrEmpty(s.ApproverName)))
                ;

            CreateMap<era_evacueesupport, FoodGroceriesSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ForMember(d => d.NumberOfDays, opts => opts.MapFrom(s => s.era_numberofmeals))
                .ForMember(d => d.ApproverName, opts => opts.MapFrom(s => s.era_supportoverrideauthority))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.FoodGroceries))
                .ForMember(d => d.era_amountoverride, opts => opts.MapFrom(s => !string.IsNullOrEmpty(s.ApproverName)))
                ;

            CreateMap<era_evacueesupport, FoodRestaurantSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ForMember(d => d.NumberOfBreakfastsPerPerson, opts => opts.MapFrom(s => s.era_numberofbreakfasts))
                .ForMember(d => d.NumberOfLunchesPerPerson, opts => opts.MapFrom(s => s.era_numberoflunches))
                .ForMember(d => d.NumberOfDinnersPerPerson, opts => opts.MapFrom(s => s.era_numberofdinners))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.FoodRestaurant))
                ;

            CreateMap<era_evacueesupport, ShelterBilletingSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.NumberOfNights, opts => opts.MapFrom(s => s.era_numberofnights))
                .ForMember(d => d.HostAddress, opts => opts.MapFrom(s => s.era_lodgingaddress))
                .ForMember(d => d.HostCity, opts => opts.MapFrom(s => s.era_lodgingcity))
                .ForMember(d => d.HostEmail, opts => opts.MapFrom(s => s.era_lodgingemailaddress))
                .ForMember(d => d.HostPhone, opts => opts.MapFrom(s => s.era_lodgingcontactnumber))
                .ForMember(d => d.HostName, opts => opts.MapFrom(s => s.era_lodgingname))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.ShelterBilleting))
                ;

            CreateMap<era_evacueesupport, ShelterGroupSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.NumberOfNights, opts => opts.MapFrom(s => s.era_numberofnights))
                .ForMember(d => d.FacilityAddress, opts => opts.MapFrom(s => s.era_lodgingaddress))
                .ForMember(d => d.FacilityCity, opts => opts.MapFrom(s => s.era_lodgingcity))
                .ForMember(d => d.FacilityCommunityCode, opts => opts.MapFrom(s => s._era_grouplodgingcityid_value))
                .ForMember(d => d.FacilityContactPhone, opts => opts.MapFrom(s => s.era_lodgingcontactnumber))
                .ForMember(d => d.FacilityName, opts => opts.MapFrom(s => s.era_lodgingname))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.ShelterGroup))
                ;

            CreateMap<era_evacueesupport, ShelterHotelSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.NumberOfNights, opts => opts.MapFrom(s => s.era_numberofnights))
                .ForMember(d => d.NumberOfRooms, opts => opts.MapFrom(s => s.era_numberofrooms))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.ShelterHotel))
                ;

            CreateMap<era_evacueesupport, ShelterAllowanceSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.NumberOfNights, opts => opts.MapFrom(s => s.era_numberofnights))
                .ForMember(d => d.ContactEmail, opts => opts.MapFrom(s => s.era_lodgingemailaddress))
                .ForMember(d => d.ContactPhone, opts => opts.MapFrom(s => s.era_lodgingcontactnumber))
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.ShelterAllowance))
                ;

            CreateMap<era_evacueesupport, TransportationOtherSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.TotalAmount, opts => opts.MapFrom(s => s.era_totalamount))
                .ForMember(d => d.TransportMode, opts => opts.MapFrom(s => s.era_transportmode))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.TransportationOther))
               ;

            CreateMap<era_evacueesupport, TransportationTaxiSupport>()
                .IncludeBase<era_evacueesupport, Support>()
                .ForMember(d => d.FromAddress, opts => opts.MapFrom(s => s.era_fromaddress))
                .ForMember(d => d.ToAddress, opts => opts.MapFrom(s => s.era_toaddress))
                .ReverseMap()
                .IncludeBase<Support, era_evacueesupport>()
                .ForMember(d => d.era_supporttype, opts => opts.MapFrom(s => SupportType.TransporationTaxi))
                ;

            CreateMap<SupportFlag, era_supportflag>()
                .ConvertUsing<SupportFlagTypeConverter>()
                ;

            CreateMap<era_supportflag, SupportFlag>(MemberList.Destination)
                .ConvertUsing<SupportFlagTypeConverter>()
                ;

            CreateMap<era_supportflag, AmountOverridenSupportFlag>()
                .ForMember(d => d.Approver, opts => opts.MapFrom(s => s.era_amountoverrider))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .ForMember(d => d._era_flagtype_value, opts => opts.MapFrom(s => AmountOverridenSupportFlag.FlagTypeId))
                .ForMember(d => d.era_amountoverrider, opts => opts.MapFrom(s => s.Approver))
                ;

            CreateMap<era_supportflag, DuplicateSupportFlag>()
                .ForMember(d => d.DuplicatedSupportId, opts => opts.MapFrom(s => s.era_SupportDuplicate == null ? null : s.era_SupportDuplicate.era_name))
                .ReverseMap()
                .ValidateMemberList(MemberList.Source)
                .ForSourceMember(s => s.DuplicatedSupportId, opts => opts.DoNotValidate())
                .ForMember(d => d._era_flagtype_value, opts => opts.MapFrom(s => DuplicateSupportFlag.FlagTypeId))
                ;

            // New mapping for era_supportconflictmessage to ConflictMessage
            CreateMap<era_supportconflictmessage, ConflictMessage>()
                .ForMember(d => d.EssTask, opts => opts.MapFrom(s => s.era_ESSTask))
                //.ForMember(d => d.EvacuationFile, opts => opts.MapFrom(s => s.era_ESSFile))
                .ForMember(d => d.MatchedEvacueeSupport, opts => opts.MapFrom(s => s.era_EvacueeSupport))
                //.ForMember(d => d.MatchedEvacuationFile, opts => opts.MapFrom(s => s.era_MatchedESSFile))
                //.ForMember(d => d.Evacuee, opts => opts.MapFrom(s => s.era_Registrant))
                .ForMember(d => d.Responder, opts => opts.MapFrom(s => s.era_Responder))
                .ForMember(d => d.EvacueeDOB, opts => opts.MapFrom(s => s.era_evacueedob))
                .ForMember(d => d.EvacueeName, opts => opts.MapFrom(s => s.era_evacueename))
                .ForMember(d => d.MatchedDOB, opts => opts.MapFrom(s => s.era_matcheddob))
                .ForMember(d => d.MatchedName, opts => opts.MapFrom(s => s.era_matchedname))
                .ForMember(d => d.EvacueeName, opts => opts.MapFrom(s => s.era_name))
                .ForMember(d => d.Scenario, opts => opts.MapFrom(s => s.era_scenario))
                //.ForMember(d => d.NameMatchScore, opts => opts.MapFrom(s => s.))

                .ReverseMap()
                ;
        }
    }

    /// <summary>
    /// Automapper converter to transform support from Dynamics era_evcueesupport entity
    /// </summary>
    public class SupportTypeConverter :
        ITypeConverter<era_evacueesupport, Support>,
        ITypeConverter<Support, era_evacueesupport>,
        ITypeConverter<IEnumerable<era_evacueesupport>, IEnumerable<Support>>,
        ITypeConverter<IEnumerable<Support>, IEnumerable<era_evacueesupport>>
    {
        public IEnumerable<era_evacueesupport> Convert(IEnumerable<Support> source, IEnumerable<era_evacueesupport> destination, ResolutionContext context) =>
            source.Select(s => Convert(s, null, context));

        public IEnumerable<Support> Convert(IEnumerable<era_evacueesupport> source, IEnumerable<Support> destination, ResolutionContext context) =>
            source.Select(s => Convert(s, null, context));

        public Support Convert(era_evacueesupport source, Support destination, ResolutionContext context) =>
            (SupportType?)source.era_supporttype switch
            {
                SupportType.Clothing => context.Mapper.Map<ClothingSupport>(source),
                SupportType.Incidentals => context.Mapper.Map<IncidentalsSupport>(source),
                SupportType.FoodGroceries => context.Mapper.Map<FoodGroceriesSupport>(source),
                SupportType.FoodRestaurant => context.Mapper.Map<FoodRestaurantSupport>(source),
                SupportType.ShelterBilleting => context.Mapper.Map<ShelterBilletingSupport>(source),
                SupportType.ShelterGroup => context.Mapper.Map<ShelterGroupSupport>(source),
                SupportType.ShelterHotel => context.Mapper.Map<ShelterHotelSupport>(source),
                SupportType.TransporationTaxi => context.Mapper.Map<TransportationTaxiSupport>(source),
                SupportType.TransportationOther => context.Mapper.Map<TransportationOtherSupport>(source),
                SupportType.ShelterAllowance => context.Mapper.Map<ShelterAllowanceSupport>(source),

                _ => throw new NotImplementedException($"No known type for SupportType value '{source.era_supporttype}'")
            };

        public era_evacueesupport Convert(Support source, era_evacueesupport destination, ResolutionContext context) =>
            source switch
            {
                ClothingSupport s => context.Mapper.Map<era_evacueesupport>(s),
                IncidentalsSupport s => context.Mapper.Map<era_evacueesupport>(s),
                FoodGroceriesSupport s => context.Mapper.Map<era_evacueesupport>(s),
                FoodRestaurantSupport s => context.Mapper.Map<era_evacueesupport>(s),
                ShelterBilletingSupport s => context.Mapper.Map<era_evacueesupport>(s),
                ShelterGroupSupport s => context.Mapper.Map<era_evacueesupport>(s),
                ShelterHotelSupport s => context.Mapper.Map<era_evacueesupport>(s),
                TransportationTaxiSupport s => context.Mapper.Map<era_evacueesupport>(s),
                TransportationOtherSupport s => context.Mapper.Map<era_evacueesupport>(s),
                ShelterAllowanceSupport s => context.Mapper.Map<era_evacueesupport>(s),

                _ => throw new NotImplementedException($"Unhandled support type '{source.GetType().Name}'")
            };
    }

    /// <summary>
    /// Automapper converter to transform support delivery from Dynamics era_evcueesupport entity
    /// </summary>
    public class SupportDeliveryTypeConverter :
        ITypeConverter<era_evacueesupport, SupportDelivery>,
        ITypeConverter<SupportDelivery, era_evacueesupport>
    {
        public SupportDelivery Convert(era_evacueesupport source, SupportDelivery destination, ResolutionContext context) =>
            (SupportMethod)source.era_supportdeliverytype.Value switch
            {
                SupportMethod.Referral => context.Mapper.Map<Referral>(source),
                SupportMethod.ETransfer => context.Mapper.Map<Interac>(source),

                _ => throw new NotImplementedException($"No known type for SupportMethod value {source.era_supportdeliverytype.Value}")
            };

        public era_evacueesupport Convert(SupportDelivery source, era_evacueesupport destination, ResolutionContext context) =>
            source switch
            {
                Referral r => context.Mapper.Map<era_evacueesupport>(r),
                Interac e => context.Mapper.Map<era_evacueesupport>(e),

                _ => throw new NotImplementedException($"No known type for SupportDeliver type of {source.GetType().Name}")
            };
    }

    /// <summary>
    /// Automapper converter to transform support flag from Dynamics era_supportflag entity
    /// </summary>
    public class SupportFlagTypeConverter :
        ITypeConverter<era_supportflag, SupportFlag>,
        ITypeConverter<SupportFlag, era_supportflag>
    {
        public SupportFlag Convert(era_supportflag source, SupportFlag destination, ResolutionContext context) =>
            source._era_flagtype_value.GetValueOrDefault().ToString() switch
            {
                DuplicateSupportFlag.FlagTypeId => context.Mapper.Map<DuplicateSupportFlag>(source),
                AmountOverridenSupportFlag.FlagTypeId => context.Mapper.Map<AmountOverridenSupportFlag>(source),

                _ => throw new NotImplementedException($"Support flag {source.era_name}: unknown flag type id '{source._era_flagtype_value}'")
            };

        public era_supportflag Convert(SupportFlag source, era_supportflag destination, ResolutionContext context) =>
            source switch
            {
                DuplicateSupportFlag f => context.Mapper.Map<era_supportflag>(f),
                AmountOverridenSupportFlag f => context.Mapper.Map<era_supportflag>(f),

                _ => throw new NotImplementedException($"Unknown support flag type {source.GetType().Name}")
            };
    }
}
