import { CommonModule, DatePipe, NgStyle, TitleCasePipe, UpperCasePipe } from '@angular/common';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, OnDestroy, OnInit } from '@angular/core';
import {
  AbstractControl,
  FormsModule,
  ReactiveFormsModule,
  UntypedFormArray,
  UntypedFormBuilder,
  UntypedFormControl,
  UntypedFormGroup,
  ValidationErrors,
  ValidatorFn,
  Validators
} from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatCard, MatCardContent } from '@angular/material/card';
import { MatCheckbox, MatCheckboxChange } from '@angular/material/checkbox';
import { MatOption } from '@angular/material/core';
import {
  MatDatepicker,
  MatDatepickerInput,
  MatDatepickerInputEvent,
  MatDatepickerToggle
} from '@angular/material/datepicker';
import { MatDialog } from '@angular/material/dialog';
import { MatError, MatFormField, MatLabel, MatPrefix, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { MatSelect } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Router } from '@angular/router';
import * as moment from 'moment';
import { firstValueFrom, Subscription } from 'rxjs';
import { Support, SupportCategory, SupportStatus, SupportSubCategory } from 'src/app/core/api/models';
import { EvacuationFileHouseholdMember } from 'src/app/core/api/models/evacuation-file-household-member';
import { DialogContent } from 'src/app/core/models/dialog-content.model';
import { ComputeRulesService } from 'src/app/core/services/computeRules.service';
import { CustomValidationService } from 'src/app/core/services/customValidation.service';
import { EvacueeSessionService } from 'src/app/core/services/evacuee-session.service';
import { DateConversionService } from 'src/app/core/services/utility/dateConversion.service';
import { AlertService } from 'src/app/shared/components/alert/alert.service';
import { InformationDialogComponent } from 'src/app/shared/components/dialog-components/information-dialog/information-dialog.component';
import { DialogComponent } from 'src/app/shared/components/dialog/dialog.component';
import * as globalConst from '../../../../core/services/global-constants';
import { LoadEvacueeListService } from '../../../../core/services/load-evacuee-list.service';
import { AppLoaderComponent } from '../../../../shared/components/app-loader/app-loader.component';
import { ReferralCreationService } from '../../step-supports/referral-creation.service';
import { StepSupportsService } from '../../step-supports/step-supports.service';
import { ClothingComponent } from './details-type/clothing/clothing.component';
import { FoodGroceriesComponent } from './details-type/food-groceries/food-groceries.component';
import { FoodMealsComponent } from './details-type/food-meals/food-meals.component';
import { IncidentalsComponent } from './details-type/incidentals/incidentals.component';
import { LodgingBilletingComponent } from './details-type/lodging-billeting/lodging-billeting.component';
import { LodgingGroupComponent } from './details-type/lodging-group/lodging-group.component';
import { LodgingHotelMotelComponent } from './details-type/lodging-hotel-motel/lodging-hotel-motel.component';
import { OtherTransportationComponent } from './details-type/other-transportation/other-transportation.component';
import { ShelterAllowanceGroupComponent } from './details-type/shelter-allowance/shelter-allowance.component';
import { TaxiTransportationComponent } from './details-type/taxi-transportation/taxi-transportation.component';
import { SupportDetailsService } from './support-details.service';
import { UserService } from 'src/app/core/services/user.service';
import { DuplicateSupportModel } from 'src/app/core/models/duplicate-support.model';
import { ConflictMessageScenario } from 'src/app/core/api/models/conflict-message-scenario';
@Component({
  selector: 'app-support-details',
  templateUrl: './support-details.component.html',
  styleUrls: ['./support-details.component.scss'],
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    MatCard,
    MatCardContent,
    NgStyle,
    FormsModule,
    ReactiveFormsModule,
    MatFormField,
    MatInput,
    MatPrefix,
    MatError,
    AppLoaderComponent,
    MatLabel,
    MatDatepickerInput,
    MatDatepickerToggle,
    MatSuffix,
    MatDatepicker,
    MatSelect,
    MatOption,
    MatCheckbox,
    MatTooltipModule,
    ShelterAllowanceGroupComponent,
    FoodMealsComponent,
    FoodGroceriesComponent,
    TaxiTransportationComponent,
    OtherTransportationComponent,
    LodgingHotelMotelComponent,
    LodgingBilletingComponent,
    LodgingGroupComponent,
    IncidentalsComponent,
    ClothingComponent,
    MatButton,
    UpperCasePipe,
    TitleCasePipe,
    DatePipe,
    CommonModule
  ]
})
export class SupportDetailsComponent implements OnInit, OnDestroy {
  currentTime: string;
  now = Date.now();
  toggle = false;
  isVisible = true;
  supportDetailsForm: UntypedFormGroup;
  noOfDaysList = [];
  selectedStartDate: string;
  editFlag = false;
  cloneFlag = false;
  taskStartTime: string;
  showLoader = false;
  color = '#169BD5';
  originalSupport: Support;
  existingSupports: Support[];
  supportListSubscription: Subscription;
  supportLimits: any;
  supportLimitsSubscription: Subscription;
  userService: UserService;

  constructor(
    private router: Router,
    public stepSupportsService: StepSupportsService,
    private datePipe: DatePipe,
    private formBuilder: UntypedFormBuilder,
    private customValidation: CustomValidationService,
    private supportDetailsService: SupportDetailsService,
    private dialog: MatDialog,
    public evacueeSessionService: EvacueeSessionService,
    private alertService: AlertService,
    private referralCreationService: ReferralCreationService,
    private dateConversionService: DateConversionService,
    private computeState: ComputeRulesService,
    private cdr: ChangeDetectorRef,
    private loadEvacueeListService: LoadEvacueeListService
  ) {
    if (this.router.getCurrentNavigation() !== null) {
      if (this.router.getCurrentNavigation().extras.state !== undefined) {
        const state = this.router.getCurrentNavigation().extras.state;
        if (state?.action === 'edit') {
          this.editFlag = true;
        } else if (state?.action === 'clone') {
          this.cloneFlag = true;
          this.originalSupport = this.stepSupportsService.selectedSupportDetail;
        }
      }
    }
    this.currentTime = this.datePipe.transform(Date.now(), 'HH:mm');
  }

  compareTaskDateTimeValidator({ controlType, other }: { controlType: 'date' | 'time'; other: string }): ValidatorFn {
    return (control: AbstractControl): ValidationErrors | null => {
      let isValid = false;
      const error = controlType === 'date' ? { invalidTaskDate: true } : { invalidTaskTime: true };
      const otherControl = this.supportDetailsForm?.get(other);

      if (
        control?.value === null ||
        control?.value === '' ||
        control?.value === undefined ||
        otherControl?.value === null ||
        otherControl?.value === '' ||
        otherControl?.value === undefined
      ) {
        isValid = true;
      } else {
        let controlDate;
        if (controlType === 'date') {
          controlDate = this.dateConversionService.createDateTimeString(control.value, otherControl.value);
        } else {
          controlDate = this.dateConversionService.createDateTimeString(otherControl.value, control.value);
        }

        if (this.evacueeSessionService?.evacFile?.task?.from && this.evacueeSessionService?.evacFile?.task?.to) {
          const from = moment(this.evacueeSessionService?.evacFile?.task?.from);
          const to = moment(this.evacueeSessionService?.evacFile?.task?.to);
          const current = moment(controlDate);

          if (current.isSame(to, 'day') && current.isAfter(to)) {
            isValid = false;
          } else {
            isValid = current.isBetween(from, to, 'm', '[]');
          }
        } else isValid = true;
      }

      if (!isValid) {
        otherControl?.setErrors(error);
        return error;
      }

      otherControl?.setErrors(null);
      return null;
    };
  }

  private removeError(control: AbstractControl, errorKey: string): void {
    if (control && control.errors && control.errors[errorKey]) {
      const errors = { ...control.errors };
      delete errors[errorKey];
      if (Object.keys(errors).length === 0) {
        control.setErrors(null);
      } else {
        control.setErrors(errors);
      }
    }
  }

  private supportMapping: Map<number, SupportSubCategory | SupportCategory> = new Map<
    number,
    SupportSubCategory | SupportCategory
  >([
    [174360000, SupportSubCategory.Food_Groceries],
    [174360001, SupportSubCategory.Food_Restaurant],
    [174360002, SupportSubCategory.Lodging_Hotel],
    [174360003, SupportSubCategory.Lodging_Billeting],
    [174360004, SupportSubCategory.Lodging_Group],
    [174360005, SupportCategory.Incidentals],
    [174360006, SupportCategory.Clothing],
    [174360007, SupportSubCategory.Transportation_Taxi],
    [174360008, SupportSubCategory.Transportation_Other],
    [174360009, SupportSubCategory.Lodging_Allowance]
  ]);

  private inverseSupportMapping: Map<SupportSubCategory | SupportCategory, number> = new Map<
    SupportSubCategory | SupportCategory,
    number
  >(Array.from(this.supportMapping.entries()).map(([key, value]) => [value, key]));

  validDateFilter = (d: Date | null): boolean => {
    const date = d || new Date();
    return moment(date).isBetween(
      moment(this.evacueeSessionService?.evacFile?.task?.from),
      moment(this.evacueeSessionService?.evacFile?.task?.to),
      'D',
      '[]'
    );
    // return this.evacueeSessionService.isPaperBased
    //   ? moment(date).isBetween(
    //       moment(this.evacueeSessionService?.evacFile?.task?.from),
    //       moment(this.evacueeSessionService?.evacFile?.task?.to),
    //       'D',
    //       '[]'
    //     )
    //   : moment(date).isBetween(
    //       moment(new Date()),
    //       moment(this.evacueeSessionService?.evacFile?.task?.to),
    //       'D',
    //       '[]'
    //     );
  };

  paperCompletedDateFilter = (d: Date | null): boolean => {
    const date = d || new Date();
    return moment(date).isBetween(
      moment(this.evacueeSessionService?.evacFile?.task?.from),
      moment(new Date()),
      'D',
      '[]'
    );
  };

  ngOnInit(): void {
    this.showLoader = true;
    this.supportLimitsSubscription = this.stepSupportsService.fetchSupportLimits().subscribe({
      next: (supportLimits) => {
        this.supportLimits = supportLimits;
        this.showLoader = false;
        this.cdr.detectChanges();
      },
      error: (error) => {
        this.showLoader = false;
        console.error('Error fetching support limits: ', error);
        this.alertService.clearAlert();
        this.alertService.setAlert('danger', globalConst.supportListerror);
        this.cdr.detectChanges();
      }
    });

    this.supportListSubscription = this.stepSupportsService.getExistingSupportList().subscribe({
      next: (supports) => {
        this.existingSupports = supports;
      },
      error: (error) => {
        this.alertService.clearAlert();
        this.alertService.setAlert('danger', globalConst.supportListerror);
      }
    });

    this.createSupportDetailsForm();
    this.supportDetailsForm.get('noOfDays').valueChanges.subscribe((value) => {
      this.updateValidToDate(value);
    });

    this.supportDetailsForm.get('fromDate').valueChanges.subscribe((value) => {
      if (value === null || value === '' || value === undefined) {
        this.supportDetailsForm.get('noOfDays').patchValue(1);
      }
    });

    this.supportDetailsForm.get('toDate').valueChanges.subscribe((value) => {
      if (value === null || value === '' || value === undefined) {
        this.supportDetailsForm.get('noOfDays').patchValue(1);
      }
    });

    this.addExistingMembers();

    if (this.stepSupportsService?.supportDetails?.noOfDays === undefined) {
      this.supportDetailsForm.get('noOfDays').patchValue(1);
    }

    this.calculateNoOfDays();

    if (this.cloneFlag) {
      this.supportDetailsForm.get('noOfDays').patchValue(1);
    }
  }

  ngOnDestroy(): void {
    this.supportListSubscription.unsubscribe();
    this.supportLimitsSubscription.unsubscribe();
  }

  isHouseholdMemberEligibleForSupport(member: EvacuationFileHouseholdMember): boolean {
    if (!this.supportLimits || this.supportLimits.length === 0) {
      return true;
    }
    const currentSupportType = this.stepSupportsService.supportTypeToAdd.description;
    const matchingSupportLimit = this.supportLimits.find(
      (limit) => this.mapSupportType(limit.supportType) === currentSupportType
    );
    if (!matchingSupportLimit) {
      return true;
    }
    if (matchingSupportLimit.extensionAvailable) {
      return true;
    }
    const supportLimitStartDate = moment(matchingSupportLimit.supportLimitStartDate);
    const supportLimitEndDate = moment(matchingSupportLimit.supportLimitEndDate);
    const hasReceivedSupport = this.existingSupports.some((support) => {
      const supportDate = moment(support.from);
      return (
        support.category === currentSupportType &&
        support.includedHouseholdMembers?.some((m) => m === member.id) &&
        support.status !== SupportStatus.Cancelled.toString() &&
        support.status !== SupportStatus.Void.toString() &&
        supportDate.isBetween(supportLimitStartDate, supportLimitEndDate, 'days', '[]')
      );
    });
    return !hasReceivedSupport;
  }

  allMembersEligible(): boolean {
    return this.evacueeSessionService?.evacFile?.needsAssessment?.householdMembers.every((member) =>
      this.isHouseholdMemberEligibleForSupport(member)
    );
  }

  checkDateRange(): boolean {
    const selectedFromDate = new Date(this.supportDetailsForm.get('fromDate').value);
    const updateFromDate = new Date(selectedFromDate.setDate(selectedFromDate.getDate() + 30));
    return moment(this.supportDetailsForm.get('toDate').value).isSameOrBefore(moment(updateFromDate));
  }

  calculateNoOfDays() {
    const taskStartDate = this.datePipe.transform(this.evacueeSessionService?.evacFile?.task?.from, 'MMM d, y');
    const taskEndDate = this.datePipe.transform(this.evacueeSessionService?.evacFile?.task?.to, 'MMM d, y');
    const dateDiff = new Date(taskEndDate).getTime() - new Date(taskStartDate).getTime();
    const noOfDaysCalc = dateDiff / (1000 * 60 * 60 * 24);

    const counter = noOfDaysCalc > 30 ? 30 : noOfDaysCalc;

    for (let i = 1; i <= counter; i++) {
      this.noOfDaysList.push(i);
    }
  }

  /**
   * Navigates to select-support page
   */
  back() {
    this.stepSupportsService
      .openDataLossPopup(globalConst.supportDataLossDialog)
      .afterClosed()
      .subscribe((event) => {
        if (event === 'confirm') {
          this.router.navigate(['/ess-wizard/add-supports/view']);
        }
      });
  }

  /**
   * Toggles the details section
   */
  nextDetails() {
    this.isVisible = !this.isVisible;
  }

  addExistingMembers() {
    if (this.stepSupportsService?.supportDetails?.members) {
      const members = this.supportDetailsForm.get('members') as UntypedFormArray;
      this.stepSupportsService?.supportDetails?.members.forEach((member) => {
        members.push(new UntypedFormControl(member));
      });
      this.nextDetails();
    }
  }

  /**
   * Checks if the given task date is in the past
   *
   * @param fromDate
   * @param toDate
   * @returns
   */
  validTaskDate(fromDate: string, toDate: string): boolean {
    if (moment(toDate).isBefore(fromDate)) {
      return false;
    } else {
      return true;
    }
  }

  /**
   * Returns the control of the form
   */
  get supportDetailsFormControl(): { [key: string]: AbstractControl } {
    return this.supportDetailsForm.controls;
  }

  /**
   * Sets and removes members from form array
   *
   * @param $event checkbox event
   * @param member selected household member
   */
  onChange($event: MatCheckboxChange, member: EvacuationFileHouseholdMember) {
    const members = this.supportDetailsForm.get('members') as UntypedFormArray;

    if ($event.checked) {
      members.push(new UntypedFormControl(member));
    } else {
      const i = members.controls.findIndex((x) => x.value === member);
      members.removeAt(i);
    }
  }

  /**
   * Selects all the checkbox
   *
   * @param $event checkbox event
   */
  selectAll($event: MatCheckboxChange) {
    const members = this.supportDetailsForm.get('members') as UntypedFormArray;
    if ($event.checked) {
      members.clear();
      this.evacueeSessionService?.evacFile?.needsAssessment?.householdMembers.forEach((member) => {
        if (this.isHouseholdMemberEligibleForSupport(member)) {
          members.push(new UntypedFormControl(member));
        }
      });
    } else {
      members.clear();
    }
  }

  /**
   * Checks if the member already exists
   *
   * @param member selected household member
   * @returns true/false
   */
  exists(member: EvacuationFileHouseholdMember) {
    const existingList: EvacuationFileHouseholdMember[] = this.supportDetailsForm.get('members').value;
    return existingList.findIndex((value) => value.id === member.id) > -1;
  }

  isIndeterminate() {
    return this.supportDetailsForm.get('members').value.length > 0 && !this.isChecked();
  }

  isChecked() {
    return (
      this.supportDetailsForm.get('members').value.length ===
      this.evacueeSessionService?.evacFile?.needsAssessment?.householdMembers.length
    );
  }

  hideRateSheet(): boolean {
    return this.stepSupportsService?.supportTypeToAdd?.value !== SupportSubCategory.Lodging_Group;
  }

  /**
   * Updates date field based on number of days to be added
   */
  updateValidToDate(days?: number): void {
    const currentVal = this.supportDetailsForm.get('fromDate').value;
    const currentSupportType = this.stepSupportsService.supportTypeToAdd.value;
    if (days !== null && currentVal !== '') {
      const date = new Date(currentVal);
      const fromTime = this.supportDetailsForm.get('fromTime').value;
      const afterMidnightBeforeSix = this.isTimeBetween(fromTime, '00:00:00', '06:00:00');
      let totalDays = days;
      if (
        afterMidnightBeforeSix &&
        (currentSupportType === SupportSubCategory.Lodging_Hotel ||
          currentSupportType === SupportSubCategory.Lodging_Allowance ||
          currentSupportType === SupportSubCategory.Lodging_Group)
      ) {
        totalDays -= 1;
      }
      const finalValue = this.datePipe.transform(date.setDate(date.getDate() + totalDays), 'MM/dd/yyyy');
      this.supportDetailsForm.get('toDate').patchValue(new Date(finalValue));
    }
  }

  /**
   * Updates To Date based of no of days
   *
   * @param event
   */
  updateToDate(event: MatDatepickerInputEvent<Date>) {
    const days = this.supportDetailsForm.get('noOfDays').value;
    const currentVal = this.supportDetailsForm.get('fromDate').value;
    const fromTime = this.supportDetailsForm.get('fromTime').value;
    const currentSupportType = this.stepSupportsService.supportTypeToAdd.value;
    const afterMidnightBeforeSix = this.isTimeBetween(fromTime, '00:00:00', '06:00:00');
    const date = new Date(currentVal);
    let finalValue = this.datePipe.transform(date.setDate(date.getDate() + days), 'MM/dd/yyyy');
    if (
      afterMidnightBeforeSix &&
      (currentSupportType === SupportSubCategory.Lodging_Hotel ||
        currentSupportType === SupportSubCategory.Lodging_Allowance ||
        currentSupportType === SupportSubCategory.Lodging_Group)
    ) {
      finalValue = this.datePipe.transform(date.setDate(date.getDate() + days - 1), 'MM/dd/yyyy');
      this.supportDetailsForm.get('toDate').patchValue(new Date(finalValue));
    } else {
      this.supportDetailsForm.get('toDate').patchValue(new Date(finalValue));
    }
  }

  /**
   * Updates No of days based on To Date selection
   *
   * @param event
   */
  updateNoOfDays(event: MatDatepickerInputEvent<Date>) {
    const fromDate = this.datePipe.transform(this.supportDetailsForm.get('fromDate').value, 'dd-MMM-yyyy');
    const fromTime = this.supportDetailsForm.get('fromTime').value;
    const toDate = this.datePipe.transform(event.value, 'dd-MMM-yyyy');
    const dateDiff = new Date(toDate).getTime() - new Date(fromDate).getTime();
    const currentSupportType = this.stepSupportsService.supportTypeToAdd.value;
    let days = dateDiff / (1000 * 60 * 60 * 24);

    const afterMidnightBeforeSix = this.isTimeBetween(fromTime, '00:00:00', '06:00:00');

    if (days > 30) {
      this.supportDetailsForm.get('noOfDays').patchValue(null);
    } else {
      if (
        afterMidnightBeforeSix &&
        (currentSupportType === SupportSubCategory.Lodging_Hotel ||
          currentSupportType === SupportSubCategory.Lodging_Allowance ||
          currentSupportType === SupportSubCategory.Lodging_Group)
      ) {
        days = days + 1;
      }
      this.supportDetailsForm.get('noOfDays').patchValue(days);
    }
  }

  async validateDelivery() {
    if (!this.supportDetailsForm.valid) {
      this.supportDetailsForm.markAllAsTouched();
      return;
    }

    let existingSupports = this.existingSupports.filter((x) => x.status !== SupportStatus.Cancelled.toString());

    if (this.editFlag) {
      existingSupports = existingSupports.filter((s) => s !== this.stepSupportsService.selectedSupportDetail);
    }

    const thisSupport = this.supportDetailsForm.getRawValue();
    const from = moment(this.dateConversionService.createDateTimeString(thisSupport.fromDate, thisSupport.fromTime));
    const to = moment(this.dateConversionService.createDateTimeString(thisSupport.toDate, thisSupport.toTime));
    const members: EvacuationFileHouseholdMember[] = this.supportDetailsForm.get('members').value.map((m) => m.id);

    const supportCategory =
      SupportSubCategory[this.stepSupportsService.supportTypeToAdd.value] ||
      SupportCategory[this.stepSupportsService.supportTypeToAdd.value];
    const duplicateSupportRequest = {
      members,
      toDate: to.toISOString(),
      fromDate: from.toISOString(),
      category: this.mapSupportTypeInverse(supportCategory),
      fileId: this.evacueeSessionService?.evacFile?.id,
      issuedBy: this.userService?.currentProfile?.userName
    };

    try {
      // Get potential duplicates based on fuzzy search from API
      const potentialDuplicateSupports = await firstValueFrom(
        this.stepSupportsService.checkPossibleDuplicateSupports(duplicateSupportRequest)
      );

      // If there are potential duplicates, show a dialog to confirm
      if (potentialDuplicateSupports.length > 0) {
        const message: DialogContent = this.generateDuplicateSupportDialog(potentialDuplicateSupports);
        this.dialog
          .open(DialogComponent, {
            data: {
              component: InformationDialogComponent,
              content: message
            },
            width: '720px'
          })
          .afterClosed()
          .subscribe((event) => {
            if (event === 'confirm') {
              // If confirmed, add the delivery
              this.addDelivery();
            }
          });
        // If there are no potential duplicates found, add the delivery
      } else {
        this.addDelivery();
      }
    } catch (error) {
      console.error('Error fetching duplicate supports: ', error);
      return;
    }
  }

  generateDuplicateSupportDialog(potentialDuplicateSupports: DuplicateSupportModel[]): DialogContent {
    if (potentialDuplicateSupports.length > 0) {
      const evacueeDetails = potentialDuplicateSupports
        .map(
          (support) => `
        <ul>
          <li><strong>Name:</strong> ${support.supportMemberFirstName} ${support.supportMemberLastName}</li>
          <li><strong>Date of Birth:</strong> ${support.supportMemberDOB}</li>
          <li><strong>ESS File Number:</strong> ${support.essFileId}</li>
          <li><strong>Support Period:</strong> [Start Date ${support.supportStartDate}] ${support.supportStartTime} - [${support.supportEndDate}] ${support.supportEndTime}</li>
        </ul>`
        )
        .join('');

      return {
        title: '<span class="bold field-error">Duplicate Support Detected</span>',
        text:
          'The support you are attempting to issue overlaps with previously issued supports. Evacuees are not entitled to supports of the same type during overlapping dates and times. Overlapping supports may be denied by EMCR if not properly justified and are subject to review.' +
          '<br/>The following evacuee(s) have already received the same support type during the overlapping period.' +
          `<br/><br/><strong>Evacuee Details:</strong><br/>${evacueeDetails}` +
          '<strong>Next Steps:</strong><br/>' +
          `<li>Review the evacuee’s ESS files and previously issued supports to determine eligibility</li>` +
          `<li>Edit the support to avoid an overlapping time period</li>` +
          `<li>If proceeding with the overlapping support, provide justification in an ESS File Note</li>`,

        cancelButton: 'Edit Support',
        confirmButton: 'Continue and Issue Overlapping Support'
      };
    }

    return {
      title: '<span class="bold field-error">Potential Duplicate Support Detected</span>',
      text: 'The support you are trying to add may have conflicts with previously issued supports.<br><br>Do you want to continue?',

      confirmButton: 'Yes, Continue',
      cancelButton: 'No, Cancel'
    };
  }

  mapSubCategoryToCategory(subCategory: SupportSubCategory): SupportCategory {
    switch (subCategory) {
      case SupportSubCategory.Food_Groceries:
        return SupportCategory.Food;
      case SupportSubCategory.Food_Restaurant:
        return SupportCategory.Food;
      case SupportSubCategory.Lodging_Hotel:
        return SupportCategory.Lodging;
      case SupportSubCategory.Lodging_Billeting:
        return SupportCategory.Lodging;
      case SupportSubCategory.Lodging_Group:
        return SupportCategory.Lodging;
      case SupportSubCategory.Lodging_Allowance:
        return SupportCategory.Lodging;
      case SupportSubCategory.Transportation_Taxi:
        return SupportCategory.Transportation;
      case SupportSubCategory.Transportation_Other:
        return SupportCategory.Transportation;

      default:
        return SupportCategory.Unknown;
    }
  }

  /**
   * Navigates to support delivery page
   */
  addDelivery() {
    this.stepSupportsService.supportDetails = this.supportDetailsForm.getRawValue();
    this.computeState.triggerEvent();
    if (this.evacueeSessionService.isPaperBased) {
      this.mapPaperFields();
    }
    const action = this.editFlag ? 'edit' : this.cloneFlag ? 'clone' : '';
    if (action) {
      this.router.navigate(['/ess-wizard/add-supports/delivery'], {
        state: { action }
      });
    } else {
      this.router.navigate(['/ess-wizard/add-supports/delivery']);
    }
  }

  mapPaperFields() {
    this.stepSupportsService.supportDetails.externalReferenceId =
      this.supportDetailsForm.get('paperSupportNumber').value;
    this.stepSupportsService.supportDetails.issuedBy =
      this.supportDetailsForm.get('paperIssuedBy.firstName').value +
      ' ' +
      this.supportDetailsForm.get('paperIssuedBy.lastNameInitial').value;
    this.stepSupportsService.supportDetails.issuedOn = this.supportDetailsForm.get('paperCompletedOn').value;
  }

  /**
   * Open support rate sheet
   */
  openRateSheet() {
    this.dialog.open(DialogComponent, {
      data: {
        component: InformationDialogComponent,
        content: this.stepSupportsService.getRateSheetContent()
      },
      width: '720px'
    });
  }

  /**
   * Navigates back to the View Details page
   */
  backToEdit() {
    this.router.navigate(['/ess-wizard/add-supports/view-detail']);
  }

  checkReferralNumber($event): void {
    this.showLoader = !this.showLoader;
    this.supportDetailsService.checkUniqueReferralNumber('R' + $event.target.value).subscribe({
      next: (value) => {
        this.showLoader = !this.showLoader;
        const draftList = this.referralCreationService.getDraftSupport();
        const duplicateReferralList: Support[] = [...value, ...draftList];
        const filteredReferrals = duplicateReferralList.filter(
          (referrals) =>
            referrals.category === this.stepSupportsService?.supportTypeToAdd?.value ||
            referrals.subCategory === this.stepSupportsService?.supportTypeToAdd?.value
        );
        this.updateFormValidations(filteredReferrals);
        if (value) {
          this.supportDetailsForm.get('paperSupportNumber').updateValueAndValidity();
        } else {
          this.supportDetailsForm.updateValueAndValidity();
        }
      },
      error: (error) => {
        this.showLoader = !this.showLoader;
        this.alertService.clearAlert();
        this.alertService.setAlert('danger', globalConst.referralCheckerror);
      }
    });
  }

  private setFromDate() {
    if (this.evacueeSessionService.isPaperBased) {
      return this.stepSupportsService?.supportDetails?.fromDate
        ? this.stepSupportsService?.supportDetails?.fromDate
        : '';
    } else {
      return this.stepSupportsService?.supportDetails?.fromDate && !this.cloneFlag
        ? this.stepSupportsService?.supportDetails?.fromDate
        : this.createFromDate();
    }
  }

  private setFromTime() {
    if (this.evacueeSessionService.isPaperBased) {
      return this.stepSupportsService?.supportDetails?.fromTime
        ? this.stepSupportsService?.supportDetails?.fromTime
        : '';
    } else {
      return this.stepSupportsService?.supportDetails?.fromTime && !this.cloneFlag
        ? this.stepSupportsService?.supportDetails?.fromTime
        : this.setDefaultTimes();
    }
  }

  private createFromDate() {
    let existingSupports = this.existingSupports.filter(
      (x) => x.status !== SupportStatus.Cancelled.toString() && x.status !== SupportStatus.Void.toString()
    );

    const category: SupportCategory =
      SupportCategory[this.stepSupportsService.supportTypeToAdd.value] ||
      this.mapSubCategoryToCategory(SupportSubCategory[this.stepSupportsService.supportTypeToAdd.value]);

    let largestTo = existingSupports
      .filter((support) => support.category === category)
      .reduce(
        (maxTo, support) => {
          const supportTo = moment(support.to);
          return supportTo.isAfter(maxTo) ? new Date(supportTo.toDate().getTime() + 60000) : maxTo;
        },
        new Date(this.dateConversionService.convertStringToDate(this.datePipe.transform(Date.now(), 'dd-MMM-yyyy')))
      );
    const taskTo = moment(this.evacueeSessionService?.evacFile?.task?.to);

    if (moment(largestTo).isAfter(taskTo)) {
      largestTo = taskTo.toDate();
    }

    return largestTo;
  }

  private setDefaultTimes() {
    let existingSupports = this.existingSupports.filter(
      (x) => x.status !== SupportStatus.Cancelled.toString() && x.status !== SupportStatus.Void.toString()
    );

    const category: SupportCategory =
      SupportCategory[this.stepSupportsService.supportTypeToAdd.value] ||
      this.mapSubCategoryToCategory(SupportSubCategory[this.stepSupportsService.supportTypeToAdd.value]);
    const maxToDate = existingSupports
      .filter((support) => support.category === category)
      .map((support) => new Date(support.to)) // Convert to Date objects
      .reduce((max, date) => (date > max ? date : max), new Date(-8640000000000000)); // Compare dates and get the max

    // Add one minute to the maxToDate
    const maxToDatePlusOneMinute = new Date(maxToDate.getTime() + 60000);

    // Get the current time
    const currentDateTime = new Date();
    currentDateTime.setSeconds(0);

    // Compare and get the later time between maxToDatePlusOneMinute and currentTime
    let finalTime = maxToDatePlusOneMinute > currentDateTime ? maxToDatePlusOneMinute : currentDateTime;
    const taskTo = moment(this.evacueeSessionService?.evacFile?.task?.to);
    if (moment(finalTime).isAfter(taskTo)) {
      finalTime = taskTo.toDate();
      finalTime.setSeconds(0);
    }

    const largestToTime = finalTime.toTimeString().split(' ')[0];
    return largestToTime;
  }

  private mapSupportType(supportType: number): SupportSubCategory | SupportCategory {
    switch (supportType) {
      case 174360000:
        return SupportSubCategory.Food_Groceries;
      case 174360001:
        return SupportSubCategory.Food_Restaurant;
      case 174360002:
        return SupportSubCategory.Lodging_Hotel;
      case 174360003:
        return SupportSubCategory.Lodging_Billeting;
      case 174360004:
        return SupportSubCategory.Lodging_Group;
      case 174360005:
        return SupportCategory.Incidentals;
      case 174360006:
        return SupportCategory.Clothing;
      case 174360007:
        return SupportSubCategory.Transportation_Taxi;
      case 174360008:
        return SupportSubCategory.Transportation_Other;
      case 174360009:
        return SupportSubCategory.Lodging_Allowance;
      default:
        return SupportCategory.Unknown;
    }
  }

  private mapSupportTypeInverse(support: SupportSubCategory | SupportCategory): number {
    switch (support) {
      case SupportSubCategory.Food_Groceries:
        return 174360000;
      case SupportSubCategory.Food_Restaurant:
        return 174360001;
      case SupportSubCategory.Lodging_Hotel:
        return 174360002;
      case SupportSubCategory.Lodging_Billeting:
        return 174360003;
      case SupportSubCategory.Lodging_Group:
        return 174360004;
      case SupportCategory.Incidentals:
        return 174360005;
      case SupportCategory.Clothing:
        return 174360006;
      case SupportSubCategory.Transportation_Taxi:
        return 174360007;
      case SupportSubCategory.Transportation_Other:
        return 174360008;
      case SupportSubCategory.Lodging_Allowance:
        return 174360009;
      default:
        throw new Error(`Unknown SupportSubCategory or SupportCategory: ${support}`);
    }
  }

  private setToTime() {
    if (this.evacueeSessionService.isPaperBased) {
      return this.stepSupportsService?.supportDetails?.toTime ? this.stepSupportsService?.supportDetails?.toTime : '';
    } else {
      return this.stepSupportsService?.supportDetails?.toTime && !this.cloneFlag
        ? this.stepSupportsService?.supportDetails?.toTime
        : this.setDefaultTimes();
    }
  }

  /**
   * Support details form
   */
  private createSupportDetailsForm(): void {
    this.supportDetailsForm = this.formBuilder.group({
      fromDate: [
        this.setFromDate(),
        [
          this.customValidation.validDateValidator(),
          Validators.required,
          this.compareTaskDateTimeValidator({
            controlType: 'date',
            other: 'fromTime'
          })
        ]
      ],
      fromTime: [
        this.setFromTime(),
        [Validators.required, this.compareTaskDateTimeValidator({ controlType: 'time', other: 'fromDate' })]
      ],
      noOfDays: [
        this.stepSupportsService?.supportDetails?.noOfDays ? this.stepSupportsService?.supportDetails?.noOfDays : '',
        [Validators.required]
      ],
      toDate: [
        this.stepSupportsService?.supportDetails?.toDate ? this.stepSupportsService?.supportDetails?.toDate : '',
        [
          this.customValidation.validDateValidator(),
          Validators.required,
          this.compareTaskDateTimeValidator({
            controlType: 'date',
            other: 'toTime'
          })
        ]
      ],
      toTime: [
        this.setToTime(),
        [Validators.required, this.compareTaskDateTimeValidator({ controlType: 'time', other: 'toDate' })]
      ],
      members: this.formBuilder.array([], [this.customValidation.memberCheckboxValidator()]),
      referral: this.supportDetailsService.generateDynamicForm(this.stepSupportsService?.supportTypeToAdd?.value),
      paperSupportNumber: [
        this.stepSupportsService?.supportDetails?.externalReferenceId
          ? this.stepSupportsService?.supportDetails?.externalReferenceId
          : '',
        [
          this.customValidation
            .conditionalValidation(
              () => this.evacueeSessionService.isPaperBased,
              this.customValidation.whitespaceValidator()
            )
            .bind(this.customValidation),

          this.customValidation
            .conditionalValidation(() => this.evacueeSessionService.isPaperBased, Validators.minLength(6))
            .bind(this.customValidation),
          this.customValidation
            .conditionalValidation(
              () => this.evacueeSessionService.isPaperBased,
              Validators.pattern(globalConst.supportNumberPattern)
            )
            .bind(this.customValidation)
        ]
      ],
      paperIssuedBy: this.formBuilder.group({
        firstName: [
          this.stepSupportsService?.supportDetails?.issuedBy?.split(' ')[0]
            ? this.stepSupportsService?.supportDetails?.issuedBy?.split(' ')[0]
            : '',
          [
            this.customValidation
              .conditionalValidation(
                () => this.evacueeSessionService.isPaperBased,
                this.customValidation.whitespaceValidator()
              )
              .bind(this.customValidation)
          ]
        ],
        lastNameInitial: [
          this.stepSupportsService?.supportDetails?.issuedBy?.split(' ')[1]
            ? this.stepSupportsService?.supportDetails?.issuedBy?.split(' ')[1]
            : '',
          [
            this.customValidation
              .conditionalValidation(
                () => this.evacueeSessionService.isPaperBased,
                this.customValidation.whitespaceValidator()
              )
              .bind(this.customValidation)
          ]
        ]
      }),
      paperCompletedOn: [
        this.stepSupportsService?.supportDetails?.issuedOn ? this.stepSupportsService?.supportDetails?.issuedOn : '',
        [
          this.customValidation
            .conditionalValidation(() => this.evacueeSessionService.isPaperBased, Validators.required)
            .bind(this.customValidation),
          this.customValidation
            .conditionalValidation(
              () => this.evacueeSessionService.isPaperBased,
              this.customValidation.validDateValidator()
            )
            .bind(this.customValidation)
        ]
      ]
    });
  }

  private updateFormValidations(filteredReferrals) {
    this.supportDetailsForm
      .get('paperSupportNumber')
      .setValidators([
        this.customValidation
          .conditionalValidation(
            () => this.evacueeSessionService.isPaperBased,
            this.customValidation.whitespaceValidator()
          )
          .bind(this.customValidation),

        this.customValidation
          .conditionalValidation(() => this.evacueeSessionService.isPaperBased, Validators.minLength(6))
          .bind(this.customValidation),
        this.customValidation
          .conditionalValidation(
            () => this.evacueeSessionService.isPaperBased,
            Validators.pattern(globalConst.supportNumberPattern)
          )
          .bind(this.customValidation)
      ]);
    this.supportDetailsForm.get('paperSupportNumber').updateValueAndValidity();
  }

  private isTimeBetween(time, from, to) {
    // Split the times into hours, minutes, and seconds
    const [hours, minutes, seconds] = time.split(':').map(Number);
    const [fromHours, fromMinutes, fromSeconds] = from.split(':').map(Number);
    const [toHours, toMinutes, toSeconds] = to.split(':').map(Number);
    // Create Date objects for both times
    const imputTime = new Date();
    imputTime.setHours(hours, minutes, seconds ? seconds : 0);
    const fromTime = new Date();
    fromTime.setHours(fromHours, fromMinutes, fromSeconds);
    const toTime = new Date();
    toTime.setHours(toHours, toMinutes, toSeconds);
    //Compare the times
    return imputTime >= fromTime && imputTime <= toTime;
  }
}
