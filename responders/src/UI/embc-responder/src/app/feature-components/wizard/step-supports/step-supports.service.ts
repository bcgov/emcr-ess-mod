import { Injectable } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import {
  Code,
  Support,
  SupportSubCategory,
  Referral,
  SupportStatus,
  SupportMethod,
  SupportCategory,
  EvacuationFile,
  Interac
} from 'src/app/core/api/models';
import { SupplierListItem } from 'src/app/core/api/models/supplier-list-item';
import { RegistrationsService, TasksService } from 'src/app/core/api/services';
import { DialogContent } from 'src/app/core/models/dialog-content.model';
import { EvacuationFileModel } from 'src/app/core/models/evacuation-file.model';
import { SupplierListItemModel } from 'src/app/core/models/supplier-list-item.model';
import { SupportDeliveryModel, SupportDetailsModel } from 'src/app/core/models/support-details.model';
import { CacheService } from 'src/app/core/services/cache.service';
import { EssFileService } from 'src/app/core/services/ess-file.service';
import { LocationsService } from 'src/app/core/services/locations.service';
import { UserService } from 'src/app/core/services/user.service';
import { InformationDialogComponent } from 'src/app/shared/components/dialog-components/information-dialog/information-dialog.component';
import { DialogComponent } from 'src/app/shared/components/dialog/dialog.component';
import { ReferralCreationService } from './referral-creation.service';
import * as globalConst from '../../../core/services/global-constants';
import { DateConversionService } from 'src/app/core/services/utility/dateConversion.service';
import { ComputeRulesService } from 'src/app/core/services/computeRules.service';
import { AppBaseService } from 'src/app/core/services/helper/appBase.service';
import { EvacueeSessionService } from 'src/app/core/services/evacuee-session.service';
import { SupportLimit } from 'src/app/core/api/models/support-limit';
import { SupportsService } from 'src/app/core/api/services/supports.service';
import { DuplicateSupportModel } from 'src/app/core/models/duplicate-support.model';

@Injectable({ providedIn: 'root' })
export class StepSupportsService {
  private existingSupportListVal: BehaviorSubject<Support[]> = new BehaviorSubject<Support[]>([]);
  private existingSupportListVal$: Observable<Support[]> = this.existingSupportListVal.asObservable();
  private supportTypeToAddVal: Code;
  private supplierListVal: SupplierListItemModel[];
  private supportDetailsVal: SupportDetailsModel;
  private supportDeliveryVal: SupportDeliveryModel;
  private selectedSupportDetailVal: Support;
  private supportLimitsVal: BehaviorSubject<SupportLimit[]> = new BehaviorSubject<SupportLimit[]>([]);
  private supportLimitsVal$: Observable<SupportLimit[]> = this.supportLimitsVal.asObservable();

  constructor(
    private essFileService: EssFileService,
    private cacheService: CacheService,
    private taskService: TasksService,
    private supportService: SupportsService,
    private userService: UserService,
    private locationService: LocationsService,
    private dialog: MatDialog,
    private referralService: ReferralCreationService,
    private registrationsService: RegistrationsService,
    private dateConversionService: DateConversionService,
    private appBaseService: AppBaseService,
    private computeState: ComputeRulesService,
    private evacueeSessionService: EvacueeSessionService
  ) {}

  set selectedSupportDetail(selectedSupportDetailVal: Support) {
    this.selectedSupportDetailVal = selectedSupportDetailVal;
  }

  get selectedSupportDetail(): Support {
    return this.selectedSupportDetailVal;
  }

  set supportDetails(supportDetailsVal: SupportDetailsModel) {
    this.supportDetailsVal = supportDetailsVal;
  }

  get supportDetails(): SupportDetailsModel {
    return this.supportDetailsVal;
  }

  set supportDelivery(supportDeliveryVal: SupportDeliveryModel) {
    this.supportDeliveryVal = supportDeliveryVal;
  }

  get supportDelivery(): SupportDeliveryModel {
    return this.supportDeliveryVal;
  }

  setExistingSupportList(existingSupportListVal: Support[]) {
    this.existingSupportListVal.next(existingSupportListVal);
  }

  getExistingSupportList(): Observable<Support[]> {
    return this.existingSupportListVal$;
  }

  setStoredSupportLimits(supportLimits: SupportLimit[]): void {
    this.supportLimitsVal.next(supportLimits);
  }

  getStoredSupportLimits(): Observable<SupportLimit[]> {
    return this.supportLimitsVal$;
  }

  fetchSupportLimits(): Observable<SupportLimit[]> {
    return this.taskService
      .tasksGetTask({
        taskId: this.userService?.currentProfile?.taskNumber
      })
      .pipe(
        map((task) => {
          const supportLimits: SupportLimit[] = task.supportLimits.map((supportLimit) => {
            return {
              supportLimitStartDate: supportLimit.supportLimitStartDate,
              supportLimitEndDate: supportLimit.supportLimitEndDate,
              extensionAvailable: supportLimit.extensionAvailable,
              supportType: supportLimit.supportType
            };
          });

          this.setStoredSupportLimits(supportLimits);
          return supportLimits;
        })
      );
  }

  set supportTypeToAdd(supportTypeToAddVal: Code) {
    this.supportTypeToAddVal = supportTypeToAddVal;
    this.cacheService.set('supportType', JSON.stringify(supportTypeToAddVal));
    this.appBaseService.appModel = {
      supportProperties: { selectedSupport: supportTypeToAddVal }
    };
    this.computeState.triggerEvent();
  }

  get supportTypeToAdd(): Code {
    return this.supportTypeToAddVal ? this.supportTypeToAddVal : JSON.parse(this.cacheService.get('supportType'));
  }

  set supplierList(supplierListVal: SupplierListItemModel[]) {
    this.supplierListVal = supplierListVal;
  }

  get supplierList(): SupplierListItemModel[] {
    return this.supplierListVal;
  }

  public getEvacFile(essFileNumber: string): Observable<EvacuationFileModel> {
    return this.essFileService.getFileFromId(essFileNumber); //'100963'
  }

  public getSupports(essFileNumber: string, externalReferenceId?: string): Observable<Support[]> {
    return this.essFileService.getSupports(essFileNumber, externalReferenceId);
  }

  public getSupplierList(): Observable<Array<SupplierListItemModel>> {
    return this.taskService
      .tasksGetSuppliersList({
        taskId: this.userService?.currentProfile?.taskNumber
      })
      .pipe(
        map((supplierList: Array<SupplierListItem>) => {
          return supplierList.map((item: SupplierListItemModel) => {
            return {
              ...item,
              address: this.locationService.getAddressModelFromAddress(item.address)
            };
          });
        })
      );
  }

  checkPossibleDuplicateSupports(supportDetails: any): Observable<DuplicateSupportModel[]> {
    return this.supportService
      .supportsGetDuplicateSupports({
        members: supportDetails.members,
        toDate: supportDetails.toDate,
        fromDate: supportDetails.fromDate,
        category: supportDetails.category,
        fileId: supportDetails.fileId,
        issuedBy: this.userService?.currentProfile?.id
      })
      .pipe(
        map((supports: DuplicateSupportModel[]) => {
          return supports;
        })
      );
  }

  public openDataLossPopup(content: DialogContent): MatDialogRef<DialogComponent, string> {
    return this.dialog.open(DialogComponent, {
      data: {
        component: InformationDialogComponent,
        content
      },
      width: '530px'
    });
  }

  public editDraft(method: SupportMethod) {
    this.referralService.updateDraftSupports(this.selectedSupportDetail);
    this.saveAsDraft(method);
  }

  public saveAsDraft(method: SupportMethod) {
    const members: Array<string> = this.supportDetails.members.map((value) => {
      return value.id;
    });
    const referral: Referral | Interac =
      method === SupportMethod.Referral
        ? {
            manualReferralId:
              this.supportDetails.externalReferenceId !== undefined
                ? 'R' + this.supportDetails.externalReferenceId
                : '',
            issuedToPersonName:
              this.supportTypeToAdd.value === SupportSubCategory.Lodging_Allowance
                ? this.supportDelivery.details.hostName
                : (this.supportDelivery.issuedTo as any) !== 'Someone else'
                  ? this.supportDelivery.issuedTo.lastName + ', ' + this.supportDelivery.issuedTo.firstName
                  : this.supportDelivery.name,

            supplierAddress: this.supportDelivery.supplier.address,
            supplierId: this.supportDelivery.supplier.id,
            supplierName: this.supportDelivery.supplier.name,
            supplierNotes: this.supportDelivery.supplierNote,
            method: SupportMethod.Referral
          }
        : {
            method: SupportMethod.ETransfer,
            notificationEmail: this.supportDelivery.notificationEmail,
            notificationMobile: this.supportDelivery.notificationMobile,
            receivingRegistrantId: this.appBaseService?.appModel?.selectedProfile?.selectedEvacueeInContext.id
          };
    const support: Support = {
      issuedBy: this.supportDetails.issuedBy,
      issuedOn: this.supportDetails.issuedOn,
      from: this.dateConversionService.createDateTimeString(this.supportDetails.fromDate, this.supportDetails.fromTime),
      includedHouseholdMembers: members,
      // needsAssessmentId: this.evacueeSessionService?.evacFile.id,
      status: SupportStatus.Draft,
      to: this.dateConversionService.createDateTimeString(this.supportDetails.toDate, this.supportDetails.toTime),
      category: null,
      method,
      subCategory: null,
      supportDelivery: referral
    };

    if (this.supportTypeToAdd.value === SupportSubCategory.Food_Restaurant) {
      this.referralService.createMealReferral(support, this.supportDetails);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Food_Groceries) {
      this.referralService.createGroceriesReferral(support, this.supportDetails);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Transportation_Taxi) {
      this.referralService.createTaxiReferral(support, this.supportDetails);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Transportation_Other) {
      this.referralService.createOtherReferral(support, this.supportDetails);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Billeting) {
      this.referralService.createBilletingReferral(support, this.supportDetails, this.supportDelivery);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Allowance) {
      this.referralService.createShelterAllowanceReferral(support, this.supportDetails, this.supportDelivery);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Group) {
      this.referralService.createGroupLodgingReferral(support, this.supportDetails, this.supportDelivery);
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Hotel) {
      this.referralService.createHotelMotelReferral(support, this.supportDetails);
    } else if (this.supportTypeToAdd.value === SupportCategory.Incidentals) {
      this.referralService.createIncidentalsReferral(support, this.supportDetails);
    } else if (this.supportTypeToAdd.value === SupportCategory.Clothing) {
      this.referralService.createClothingReferral(support, this.supportDetails);
    }
  }

  getRateSheetContent(): DialogContent {
    if (this.supportTypeToAdd.value === SupportSubCategory.Food_Restaurant) {
      return globalConst.mealRateSheet;
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Food_Groceries) {
      return globalConst.groceriesRateSheet;
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Transportation_Taxi) {
      return globalConst.taxiRateSheet;
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Transportation_Other) {
      return globalConst.otherRateSheet;
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Billeting) {
      return globalConst.billetingRateSheet;
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Hotel) {
      return globalConst.hotelRateSheet;
    } else if (this.supportTypeToAdd.value === SupportSubCategory.Lodging_Allowance) {
      return globalConst.needsShelterAllowanceRateSheet;
    } else if (this.supportTypeToAdd.value === SupportCategory.Incidentals) {
      return globalConst.incidentalsRateSheet;
    } else if (this.supportTypeToAdd.value === SupportCategory.Clothing) {
      return globalConst.clothingRateSheet;
    }
  }

  getNeedsAssessmentInfo(fileId: string, needsAssessmentId: string): Observable<EvacuationFileModel> {
    return this.registrationsService
      .registrationsGetFile({
        fileId,
        needsAssessmentId
      })
      .pipe(
        map((file: EvacuationFile): EvacuationFileModel => {
          return {
            ...file,
            evacuatedFromAddress: this.locationService.getAddressModelFromAddress(file.evacuatedFromAddress),
            assignedTaskCommunity: this.locationService.mapCommunityFromCode(file?.task?.communityCode)
          };
        })
      );
  }
}
