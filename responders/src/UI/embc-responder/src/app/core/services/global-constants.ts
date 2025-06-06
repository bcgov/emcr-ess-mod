import { IMask, IMaskFactory } from 'angular-imask';
import { DashboardBanner, DialogContent } from '../models/dialog-content.model';

export const phoneMask = '000-000-0000';

export const dateMask: Parameters<IMaskFactory['create']>[1] = {
  mask: 'mm/dd/yyyy',
  lazy: false,
  blocks: {
    yyyy: {
      mask: IMask.MaskedRange,
      placeholderChar: 'y',
      from: 1900,
      to: 2999
    },
    mm: {
      mask: IMask.MaskedRange,
      placeholderChar: 'm',
      from: 1,
      to: 12
    },
    dd: {
      mask: IMask.MaskedRange,
      placeholderChar: 'd',
      from: 1,
      to: 31
    }
  }
};

export const etransferLimt = 10000;

export const mealRate = {
  total: 53,
  breakfast: 12.75,
  lunch: 14.75,
  dinner: 25.5
};

export const groceriesRate = {
  rate: 22.5
};

export const incidentals = {
  rate: 50
};

export const extremeConditions = {
  rate: 200
};

export const normalConditions = {
  rate: 150
};

export const billeting = {
  rate: 30,
  adult: 10,
  child: 5
};

export const shelterAllowanceRate = {
  rate: 200,
  rateOverMaxMembers: 400,
  maxMembers: 5
};

export const noOfRooms = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];

export const supportNoOfDays = [
  1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30
];
export const mainApplicant = 'Main Applicant';
export const insuranceOptions = [
  { name: 'Yes', value: 'Yes' },
  {
    name: 'Yes, but I am unsure if I have coverage for this event.',
    value: 'Unsure'
  },
  { name: 'No', value: 'No' },
  { name: "I don't know", value: 'Unknown' }
];

export const radioButtonOptions = [
  { name: 'Yes', value: 'Yes', apiValue: true },
  { name: 'No', value: 'No', apiValue: false }
];

export const mealRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Meals</span></b>',
  text: `<div class="row primary"><div class="col-md-2">Breakfast</div><div class="col-md-2 bold">\$${mealRate.breakfast.toFixed(
    2
  )}</div></div><div class="row primary"><div class="col-md-2">Lunch</div><div class="col-md-2 bold">\$${mealRate.lunch.toFixed(
    2
  )}</div></div><div class="row primary"><div class="col-md-2">Dinner</div><div class="col-md-2 bold">\$${mealRate.dinner.toFixed(
    2
  )}</div></div><div class="row primary pt-3"><div class="col-md-2">Total</div><div class="col-md-4 bold">\$${mealRate.total.toFixed(
    2
  )} </div></div><div class="pt-3">NOTE:</div><div>Alcohol, tobacco and gratuities are not eligible expenses</div>`,
  cancelButton: 'Close'
};

export const groceriesRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Groceries</span></b>',
  text: `<p class="primary">Daily rate per person <span class="bold">- \$${groceriesRate.rate.toFixed(
    2
  )} (incl. GST/PST)</span></p> <div>NOTE:</div><p>Alcohol, tobacco and gratuities are not eligible expenses</p>`,
  cancelButton: 'Close'
};

export const incidentalsRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Incidentals</span></b>',
  text: `<p>To be issued when evacuees have been unable to pack necessities</p><p class="primary">Adults, youth & children - <span class="bold">up to \$${incidentals.rate.toFixed(
    2
  )} maximum per person (incl. PST)</span></p><div>NOTE:</div><p>May include miscellaneous items such as personal hygiene products, laundry supplies, pet food and shelter, medication for a 3 day period, and other immediate needs as required. The Ministry of Emergency Management and Climate Readiness (EMCR) Emergency Coordination Centre must be consulted when extraordinary requirements are needed to provide for immediate needs 1-800-663-3456</p>`,
  cancelButton: 'Close'
};

export const clothingRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Clothing</span></b>',
  text: `<p>To be issued when evacuees have been unable to pack necessities</p><p class="primary">Adults, youth & children - <span class="bold">up to \$${normalConditions.rate.toFixed(
    2
  )} maximum per person (incl. PST)*</span></p><p>* Where <span class="bold">extreme winter conditions</span> apply at the time of the incident, and on a needs basis, amount maybe increased to <span class="bold">\$${extremeConditions.rate.toFixed(
    2
  )} per person.</span></p><div>NOTE:</div><p>Clothing is provided as needed to preserve health and modesty. This is not wardrobe replacement. Clothing may include footwear or special needs items such as baby diapers.</p>`,
  cancelButton: 'Close'
};

export const taxiRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Transportation</span></b>',
  text: '<p>Transportation necessary to meet immediate needs (e.g. taxis, 3 day bus pass, gasoline)</p>',
  cancelButton: 'Close'
};

export const otherRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Transportation</span></b>',
  text: '<p>Transportation necessary to meet immediate needs (e.g. taxis, 3 day bus pass, gasoline)</p>',
  cancelButton: 'Close'
};

export const hotelRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Shelter</span></b>',
  text: '<p class="primary bold">Hotel/Motel/Campground</p><p>Emergency Support Services is eligible for approved Provincial Government Rates from commercial accommodations supplier listed in the Ministry of Labour and Citizens Services Business Travel Accommodation Listing for government travel.</p><p class="bold">Only the cost of the room is covered.</p><p>The evacuee is responsible for all other charges (e.g. video rentals, damages, parking, local and long distance calls).</p>',
  cancelButton: 'Close'
};

export const needsShelterAllowanceRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Shelter Allowance</span></b>',
  text: '<p>To be issued when evacuees want to source their own shelter whilst evacuated.</p></br><p><span class="bold">$200 per night</span> per household of 1 to 5 members</p><p><span class="bold">$400 per night</span> per household of 6 or more members</p>',
  cancelButton: 'Close'
};

export const billetingRateSheet: DialogContent = {
  title: '<b>ESS Rate Sheet - <span class="dialog-title">Shelter</span></b>',
  text: `<p class="primary bold">Billeting in Private Homes</p><p>The referral form for billeting is issued to the billeting host (supplier). Billeting rate does not include meals.</p><p>\$${billeting.rate.toFixed(
    2
  )} per night based on single occupancy (add \$${billeting.adult.toFixed(
    2
  )} for each additional adult and youth and \$${billeting.child.toFixed(2)} for each additional child.</p>`,
  cancelButton: 'Close'
};

export const voidMessage: DialogContent = {
  text: '<p>Support successfully voided.</p>',
  cancelButton: 'Close'
};

export const deleteDraftMessage: DialogContent = {
  title: 'Delete Support',
  text: '<p>Are you sure you want to delete this support?</p>',
  confirmButton: 'Yes, Delete Support',
  cancelButton: 'No, Cancel'
};

export const extendSupportMessage: DialogContent = {
  title: 'Clone & Extend Support',
  text: '<p>Are you sure you want to clone this support? Once the support has been cloned, you will be able to edit and extend the support details.</p>',
  confirmButton: 'Yes, Clone & Extend',
  cancelButton: 'No, Cancel'
};

export const duplicateSupportMessage: DialogContent = {
  title: 'Possible Support Conflict',
  text: '<p>There is already a support associated with this ESS File of the same support type with the same or overlapping support period. Do you wish to continue?</p>',
  confirmButton: 'Yes, Continue',
  cancelButton: 'No, Cancel'
};

export const needsShelterAllowanceMessage: DialogContent = {
  title: 'Shelter',
  text: '<p>A shelter allowance of $200 per night per household of 1 to 5 members.</p><p>A shelter allowance of $400 per night per household of 6 or more members.</p></br><p>The shelter allowance can be provided to eligible evacuees via e-Transfer.</p>'
};

export const needsShelterReferralMessage: DialogContent = {
  title: 'Referral',
  text: '<p>A paper form provided by an ESS responder, directing evacuated individuals to specific suppliers or facilities for essential needs like food, shelter, clothing, or other necessary items during the evacuation.</p>'
};

export const needsIncidentalMessage: DialogContent = {
  title: 'Incidentals',
  text: '<p>Incidentals could include miscellaneous items such as personal hygiene products such as toothpaste, laundry soap and/or pet food.</p>'
};

export const saveMessage: DialogContent = {
  text: '<p>Support successfully saved.</p>',
  cancelButton: 'Close'
};

export const supportDeleteMessage: DialogContent = {
  text: '<p>Support successfully deleted.</p>',
  cancelButton: 'Close'
};

export const supportEditMessage: DialogContent = {
  text: '<p>Support successfully updated.</p>',
  cancelButton: 'Close'
};

export const supportCancelMessage: DialogContent = {
  text: '<p>Support successfully updated.</p>',
  cancelButton: 'Close'
};

export const deleteMessage: DialogContent = {
  text: '<p>User has been successfully deleted from ERA.</p>',
  cancelButton: 'Close'
};
export const editMessage: DialogContent = {
  text: '<p>User has been edited successfully.</p>',
  cancelButton: 'Close'
};
export const cancelEtransferMessage: DialogContent = {
  text: '<p>e-Transfer support successfully cancelled.</p>',
  cancelButton: 'Close'
};
export const addMessage: DialogContent = {
  text: '<p>Team member has been <b>saved</b> & added successfully.</p>',
  cancelButton: 'Close'
};
export const profileLinkMessage: DialogContent = {
  text: '<p>Profile Successfully Linked</p>',
  cancelButton: 'Close'
};
export const profileLinkErrorMessage: DialogContent = {
  text: '<p>Error while linking the profile. Please try again later</p>',
  cancelButton: 'Close'
};
export const incompleteProfileMessage: DialogContent = {
  title: 'Profile Incomplete',
  text: '<p>Please complete the evacuee profile.</p>',
  cancelButton: 'Close'
};
export const essFileLinkMessage: DialogContent = {
  text: '<p>ESS File Successfully Linked</p>',
  cancelButton: 'Close'
};
export const essFileLinkErrorMessage: DialogContent = {
  text: '<p>Error while linking the ESS File. Please try again later</p>',
  cancelButton: 'Close'
};
export const deleteSupplierMessage: DialogContent = {
  text: '<p>Supplier was successfully removed.</p>',
  cancelButton: 'Close'
};
export const editSupplierMessage: DialogContent = {
  text: '<p>Supplier was successfully edited.</p>',
  cancelButton: 'Close'
};
export const addSupplierMessage: DialogContent = {
  text: '<p>Supplier was successfully added.</p>',
  cancelButton: 'Close'
};
export const disabledESSFileMessage: DialogContent = {
  title: 'Unable to access this ESS File',
  text: '<p>Responders doing data entry for a paper ESS File can only access ESS Files that match the paper ESS File number used during the search process.</p><p>If you entered the incorrect paper ESS File Number, go back and start a new search with the correct details.</p>',
  cancelButton: 'Close'
};
export const unableAccessFileMessage: DialogContent = {
  title: 'Unable to access this File',
  text: '<p>This file can only be viewed if the evacuee presented govenment-issued identification.</p>',
  cancelButton: 'Close'
};

export const tier2Notes = 'Notes regarding Tier 2 Supervisor';
export const tier3Notes = 'Notes regarding Tier 3 ESS Director/ Manager';
export const tier1Notes = 'Notes regarding Tier 1 Responder';
export const defaultRole = {
  code: 'Tier1',
  description: 'Tier 1 - Responder (default)'
};

export const currencyPattern = '^([0-9]+(((,[0-9]{3}){1})?([.][0-9]{0,2})?))$';
export const postalPattern = '^[A-Za-z][0-9][A-Za-z][ ]?[0-9][A-Za-z][0-9]$';
export const gstFirstField = '^-?([0-9]\\d*)?$';
export const gstSecondField = '^(?!0{4})[0-9]{4}$';
export const defaultProvince = { code: 'BC', name: 'British Columbia' };
export const defaultCountry = { code: 'CAN', name: 'Canada' };
export const usDefaultObject = {
  code: 'USA',
  name: 'United States of America'
};
export const zipCodePattern = '^([0-9]{5}-[0-9]{4}|[0-9]{5})$';
export const securityQuestionAnswerPattern = '^[a-zA-Z0-9 ]+$';
export const securityPhrasePattern = securityQuestionAnswerPattern;
export const petsQuantityPattern = '^([1-9][0-9]{0,2})$';
export const supportNumberPattern = '^([0-9]{6,10})$';

export const gender = [
  { name: 'Male', value: 'Male' },
  { name: 'Female', value: 'Female' },
  { name: 'X', value: 'X' }
];

export const editExistingSupplierMessage: DialogContent = {
  title: 'Editing this Supplier is Disabled',
  text: '<p>Supplier details cannot be edited as this supplier has an agreement with more than one ESS Team. If you are required to edit the supplier information please send an email to:</p><p class="supplier-link">essmodernization@gov.bc.ca</p>',
  cancelButton: 'Close'
};

export const supportDataLossDialog: DialogContent = {
  text: '<p><b>Warning:</b> Any unsaved data will be lost. Do you wish to continue?</p>',
  confirmButton: 'Yes, Continue',
  cancelButton: 'No, Cancel'
};

export const wizardProfileMessage: DialogContent = {
  title: 'Complete all steps',
  text: '<p>Please complete all sections of the Evacuee Profile prior to submitting.</p>',
  cancelButton: 'Close'
};
export const wizardESSFileMessage: DialogContent = {
  title: 'Complete all steps',
  text: '<p>Please complete all sections of the ESS File prior to submitting.</p>',
  cancelButton: 'Close'
};

export const newRegWizardProfileCreatedMessage: DialogContent = {
  title: 'Evacuee Profile Saved',
  text: '<p>Evacuee profile has been successfully created.</p>',
  confirmButton: 'Proceed to Next Step'
};
export const newRegWizardEssFileCreatedMessage: DialogContent = {
  title: 'ESS File Saved',
  text: '<p>ESS File has been successfully created and saved.</p>',
  confirmButton: 'Proceed to Next Step',
  exitLink: 'Exit Wizard'
};

export const addCaseNotesMessage: DialogContent = {
  text: '<p>Would you like to <strong>add case notes</strong> before exiting the wizard?</p>',
  confirmButton: 'Yes, Add Case Notes',
  cancelButton: 'No, Exit Wizard'
};

export const newRegWizardProfileUpdatedMessage: DialogContent = {
  title: 'Evacuee Profile Updated',
  text: '<p>Evacuee profile has been successfully updated.</p>',
  confirmButton: 'Proceed to Next Step'
};

export const editRegWizardProfileCreatedMessage: DialogContent = {
  title: 'Evacuee Profile Saved',
  text: '<p>Evacuee profile has been successfully updated.</p>',
  confirmButton: 'Proceed to Next Step'
};

export const evacueeProfileUpdatedMessage: DialogContent = {
  title: 'Evacuee Profile Updated',
  text: '<p>Evacuee profile has been successfully updated.</p>',
  cancelButton: 'Close'
};

export const memberProfileCreateMessage: DialogContent = {
  title: 'Profile Created Successfully',
  text: '<p>Evacuee profile has been successfully created.</p>',
  cancelButton: 'Close'
};

export const petDeleteDialog: DialogContent = {
  title: 'Remove Pet',
  text: '<p>Are you sure you want to remove this pet from your ESS file?</p>',
  confirmButton: 'Yes, Remove Pet',
  cancelButton: 'No, Cancel'
};

export const noPetsDialog: DialogContent = {
  title: 'Alert',
  text: '<p>By changing your selection to "No" all previously added pets will be removed</p>',
  confirmButton: 'Yes, Remove Pets',
  cancelButton: 'No, Cancel'
};

export const householdMemberDeleteDialog: DialogContent = {
  title: 'Remove Household Member',
  text: '<p>Are you sure you want to <b>remove</b> this household member from your ESS File?</p>',
  confirmButton: 'Yes, Remove Household Member',
  cancelButton: 'No, Cancel'
};

export const exitWizardDialog: DialogContent = {
  text: '<p>Are you sure you want to exit the wizard?</p><p>Any information that has not been <b>submitted</b> will be lost.</p>',
  confirmButton: 'Yes, Exit Wizard',
  cancelButton: 'No, Cancel'
};

export const paperProcessSupports: DialogContent = {
  text: '<p>By clicking "Proceed", the supports will be processed & no longer editable.</p>',
  confirmButton: 'Proceed',
  cancelButton: 'Cancel'
};

export const evacueeProfileStepIncompleteMessage: DialogContent = {
  text: '<p>Please <strong>complete the Evacuee Profile</strong> prior to proceeding to the next steps.</p>',
  cancelButton: 'Close'
};

export const essFileStepIncompleteMessage: DialogContent = {
  text: '<p>Please <strong>complete the ESS File</strong> prior to proceeding to the next steps.</p>',
  cancelButton: 'Close'
};

export const supportIncompleteMessage: DialogContent = {
  text: '<p>Please process the <strong>DRAFT</strong> Supports prior to accessing this step.</p>',
  cancelButton: 'Close'
};

export const supportInProgressMessage: DialogContent = {
  text: '<p><span class="bold field-error">WARNING: </span>You have a Support that has not been completed. This record will be lost if you continue. Do you wish to continue?</p>',
  confirmButton: 'Yes',
  cancelButton: 'No, Cancel'
};

export const stepIncompleteMessage: DialogContent = {
  text: '<p>Please <strong>complete the current step</strong> prior to proceeding to the next steps.</p>',
  cancelButton: 'Close'
};

export const successfulVerification: DialogContent = {
  title: 'Profile Successfully Verified',
  text: '<p>Evacuee profile has been successfully verified.</p>',
  cancelButton: 'Close'
};

export const verifyEvacueeProfile: DialogContent = {
  subtitle: 'Verify Evacuee Profile',
  text: '<div class="row"><div class="col-md-12"><p id="verified-radio-group-label" class="bold">To verify this profile please confirm that you have seen government issue identification for this evacuee.</p></div></div>',
  cancelButton: 'Cancel',
  confirmButton: 'Verify Profile'
};

export const unlockFieldsProfile: DialogContent = {
  subtitle: 'Edit Evacuee Details',
  text: '<div class="row"><div class="col-md-12"><p id="verified-radio-group-label" class="bold">To unlock these fields, please confirm that you have seen government issue identification to support these changes.</p></div></div>',
  cancelButton: 'Cancel',
  confirmButton: 'Unlock'
};

export const dashboardViewProfile: DialogContent = {
  subtitle: 'Identity Verification',
  text: '<div class="row"><div class="col-md-12"><p id="verified-radio-group-label" class="bold">Did you see government issued identification from this individual?</p></div></div>',
  cancelButton: 'Cancel',
  confirmButton: 'Next'
};

export const hideNote: DialogContent = {
  title: '<p class="dialog-title">Hide Case Note<p>',
  text: '<div class="row"><div class="col-md-12"><p>Are you sure you want to hide this Case Note?</p><p>Hidden Case Notes can only be viewed by Managers (third user access tier) and above.</p></div></div>',
  cancelButton: 'No, Cancel',
  confirmButton: 'Yes, Hide Note'
};

export const showNote: DialogContent = {
  title: '<p class="dialog-title">Show Case Note<p>',
  text: '<div class="row"><div class="col-md-12"><p>Are you sure you want to show this Case Note?</p><p>This Case Note can now be viewed by all users (all user access tiers).</p></div></div>',
  cancelButton: 'No, Cancel',
  confirmButton: 'Yes, Show Note'
};

export const deleteSupplierFromList: DialogContent = {
  title: 'Remove Supplier from Supplier List',
  text: '<p>Are you sure you want to remove this supplier from your supplier list? Any ESS Teams who have this supplier on their list due to a mutual aid agreement will also have this supplier removed from their list.</p>',
  cancelButton: 'No, Cancel',
  confirmButton: 'Yes, Remove Supplier'
};

export const rescindSupplierFromList: DialogContent = {
  title: 'Rescind',
  text: '<p>Are you sure you want to permanently remove this ESS Team from accessing this supplier? They will no longer have access to use this supplier.</p>',
  cancelButton: 'No, Cancel',
  confirmButton: 'Yes, Remove ESS Team'
};

export const updateSupplierStatus: DialogContent = {
  title: 'Update Supplier Status',
  text: '<p>This supplier is connected to another ESS Team via a LEP mutual aid agreement. Any status changes will affect access to any shared suppliers.</p>',
  cancelButton: 'Cancel',
  confirmButton: 'Continue'
};

export const supplierStatusDefinition: DialogContent = {
  title: 'Status Definitions',
  text: '<p class="green-info"><b>Active</b></p><p>Supplier will appear within your supplier list and can be selected on a referral.</p><p class="red-alert"><b>Deactivated</b></p><p>Supplier will NOT appear within your supplier list and cannot be selected on a referral.</b></p><p><b>All status changes will affect ESS Team access to any suppliers shared via LEP mutual aid agreements.</b></p>',
  cancelButton: 'Close'
};

export const caseProfileNotesWarning: DialogContent = {
  title: 'Important Case Notes',
  text: '<p><strong>This file contains important case notes.</strong></p><p>Review these notes carefully to understand key details and promptly address any necessary actions or follow-ups.</p>',
  cancelButton: 'Close',
  confirmButton: 'View Case Notes'
};

export const systemError = 'The service is temporarily unavailable. Please try again later';
export const accessError = 'Access Denied';
export const genericError = 'An error occurred while loading this page. Please refresh and try again.';
export const agreementError = 'Unable to submit electronic access agreement at this time. Please try again later';
export const teamMemberListError = 'Unable to retrieve team members at this time. Please try again later';
export const teamMemberDeleteError = 'Unable to remove team members at this time. Please try again later';
export const activateTeamMemberError = 'Unable to activate team member at this time. Please try again later';
export const deActivateTeamMemberError = 'Unable to deactivate team member at this time. Please try again later';
export const saveTeamMemberError = 'Unable to add team member at this time. Please try again later';
export const updateTeamMemberError = 'Unable to update team member at this time. Please try again later';
export const usernameCheckerror = 'Unable to check username at this time. Please try again later';
export const referralCheckerror = 'Unable to check paper referral number at this time. Please try again later';
export const communityListError = 'Unable to retrieve assigned communities at this time. Please try again later';
export const addCommunityListError = 'Unable to retrieve community list at this time. Please try again later';
export const saveCommunityListError = 'Unable to add communities at this time. Please try again later';
export const removeCommunityListError = 'Unable to remove communities at this time. Please try again later';
export const createRegProfileError = 'Unable to create evacuee profile at this time. Please try again later';
export const editProfileError = 'Unable to update your user profile at this time. Please try again later';
export const createEssFileError = 'Unable to create ESS file at this time. Please try again later';
export const findEssFileError = 'Unable to find ESS file at this time. Please try again later';
export const editEssFileError = 'Unable to update the ESS file at this time. Please try again later';
export const securityQuestionError = 'Unable to load security questions. Please try again later';
export const taskSearchError = 'Unable to retrieve this task number. Please try again later';
export const taskSignInError = 'Unable to signIn into task. Please try again later';
export const evacueeSearchError = 'Unable to complete the search at this time. Please try again later';
export const notesListError = 'Unable to retrieve notes at this time. Please try again later';
export const addNotesError = 'Unable to add notes at this time. Please try again later';
export const hideNoteError = 'Unable to hide note at this time. Please try again later';
export const showNoteError = 'Unable to show note at this time. Please try again later';
export const editNotesError = 'Unable to edit note at this time. Please try again later';
export const verifyRegistrantProfileError =
  'Unable to verify the Registrant Profile at this time. Please try again later';
export const bcscInviteError = 'Unable to send BC Services Card invitation at this time. Please try again later';
export const getProfileEssFilesError =
  'Unable to retrieve ESS Files associated to the registrant at this time. Please try again later';
export const getPossibleEssfileMatchError =
  'Unable to retrieve possible ESS Files matches associated to the registrant at this time. Please try again later';
export const fileDashboardError = 'Unable to load ESS File at this time. Please try again later';
export const supplierCheckerror = 'Unable to check supplier at this time. Please try again later';
export const mainSuppliersListError = 'Unable to retrieve suppliers list at this time. Please try again later';
export const mutualAidListError = 'Unable to retrieve mutual aid suppliers list at this time. Please try again later';
export const activateSupplierError = 'Unable to activate supplier at this time. Please try again later';
export const deActivateSupplierError = 'Unable to deactivate supplier at this time. Please try again later';
export const supplierRefresherror = 'Unable to refresh supplier list at this time. Please try again later';
export const supportListerror = 'Unable to retrieve support list at this time. Please try again later';
export const supportNeedsAssessmentError = 'Unable to retrieve needs assessment at this time. Please try again later';
export const processSupportDraftsError = 'Unable to process draft supports at this time. Please try again later';
export const supportCategoryListError = 'Unable to retrieve support categories at this time. Please try again later';
export const supportStatusListError = 'Unable to retrieve support status at this time. Please try again later';
export const supportMethodListError = 'Unable to retrieve support payment methods at this time. Please try again later';
export const supportVoidReasonsError = 'Unable to retrieve support void reasons at this time. Please try again later';
export const supportReprintReasonsError =
  'Unable to retrieve support reprint reasons at this time. Please try again later';
export const communityTypesError = 'Unable to retrieve community types at this time. Please try again later';
export const identifiedNeedsError = 'Unable to retrieve identified needs at this time. Please try again later';
export const securityPhraseError = 'Unable to retrieve security word at this time. Please try again later';
export const verifySecurityPhraseError = 'Unable to verify security word at this time. Please try again later';
export const linkProfileError = 'Unable to link to ESS File. Please try again later';
export const securityQuestionsError = 'Unable to retrieve security questions at this time. Please try again later';
export const verifySecurityQuestionError = 'Unable to verify security questions at this time. Please try again later';
export const getProfileError = 'Unable to retrieve profile at this time. Please try again later';
export const getEssFileError = 'Unable to retrieve ESS file at this time. Please try again later';
export const voidReferralError = 'Unable to void referral at this time. Please try again later';
export const cancelEtransferError = 'Unable to cancel transfer at this time. Please try again later';
export const reprintReferralError = 'Unable to reprint referral at this time. Please try again later';
export const getSupportByIdError = 'Unable to retrieve support at this time. Please try again later';
export const claimSupplierError = 'Unable to claim supplier at this time. Please try again later';
export const rescindSupplierError = 'Unable to rescind mutual aid supplier at this time. Please try again later';
export const addSupplierError = 'Unable to add mutual aid supplier at this time. Please try again later';
export const deleteSupplierError = 'Unable to remove supplier at this time. Please try again later';
export const evacueeReportError = 'Unable to retrieve an Evacuee Report at this time. Please try again later';
export const supportReportError = 'Unable to retrieve a Support Report at this time. Please try again later';

export const paperIdQuestion =
  'Did the evacuee present any <b>government-issued photo ID</b> when the paper ESS File was completed?';
export const digitalIdQuestion = 'Can you present any <b>government-issued photo ID</b> to verify your identity?';

export const newRegistrationTipText =
  '<p class="tips-info">If you need to update either first name, last name or date of birth, please start a new search using the correct infomation.</p>';
export const otherRegistrationTipText =
  '<p class="tips-info">If an Evacuee Profile has been connected to a BC Services Card, the first name, last name and date of birth will be locked.</p><p class="tips-info">If these details need to be updated, the evacuee can log into their BC Services Card and update this information.</p>';
export const reviewMembersTipText =
  '<p class="bold no-margin">Confirm the following household members should be included in this ESS File.</p><p class="no-margin">Add any household members that are missing from the list.</p><p class="no-margin requiredField">Any household member not selected will not receive supports.</p>';
export const completeMembersTipText =
  '<p class="bold no-margin">Confirm the following household members should be included in this ESS File.</p><p class="no-margin">Add any household members that are missing from the list.</p><p class="no-margin requiredField">Any household member not selected will not receive supports.</p>';
export const pendingStatusText: DashboardBanner = {
  heading: 'Pending:',
  buttonText: 'Edit ESS File',
  content: 'Complete ESS File and add supports if required.'
};

export const expiredStatusText: DashboardBanner = {
  heading: 'Expired:',
  content: 'Reactivate and complete ESS File and add supports if required.',
  buttonText: 'Edit ESS File'
};

export const paperCompletedStatusText: DashboardBanner = {
  heading: 'Complete:',
  buttonText: 'Edit ESS File',
  content: 'Task number end date has expired and ESS File is closed.'
};

export const completedStatusText: DashboardBanner = {
  heading: 'Complete:',
  content:
    'Task number end date has expired and ESS File is closed. To extend or to add new supports, task number must be extended.'
};

export const activeStatusText: DashboardBanner = {
  heading: 'Active:',
  buttonText: 'Edit ESS File',
  content: 'Review, extend or add new supports to the current ESS File.'
};

export const remoteActiveStatusText: DashboardBanner = {
  heading: 'Active:',
  buttonText: 'Extend or Reprint Supports',
  content: 'Extend Supports associated with the Current ESS File.'
};

export const caseNotesActiveText: DashboardBanner = {
  heading: 'Active:',
  buttonText: 'Add Notes',
  content: 'Add or Edit Case Notes associated with the Current ESS File.'
};

export const caseNotesCompleteText: DashboardBanner = {
  heading: 'Complete:',
  buttonText: 'Add Notes',
  content: 'Task number end date has expired. Add or Edit Case Notes associated with the Current ESS File.'
};

export const shelterAllowanceNeedDialog: DialogContent = {
  title: 'Shelter',
  text: '<p>A shelter allowance of $200 per night per household of 1 to 5 members.</p><p>A shelter allowance of $400 per night per household of 6 or more members.</p> <br/>The shelter allowance can be provided to eligible evacuees via e-Transfer.'
};

export const shelterReferralNeedDialog: DialogContent = {
  title: 'Referrals',
  text: 'Provided by an ESS responder to direct evacuated individuals to specific suppliers or facilities for essential needs like food, shelter, clothing, or other necessary items during the evacuation.'
};

export const incidentalsNeedDialog: DialogContent = {
  title: 'Incidentals',
  text: 'Incidentals could include items such as personal hygiene products like toothpaste, laundry soap and/or pet food.'
};

export const duplicateHouseholdMemberWarning: DialogContent = {
  title: '<b>Duplicate Household Member Detected</b>',
  text: `<p>It looks like the member you are trying to add already exists in the household.</p>
  <p>Please review the existing members before proceeding.</p>`,
  cancelButton: 'Close'
};

export const noAssistanceRequired = 'Household currently does not require assistance.';
export const confirmDuplicateSupportMessage =
  'I have reviewed the evacuee details and confirm that they are not the same person';
export const DuplicateSupportPopupProceedMessage = 'Continue and Issue Overlapping Support';
