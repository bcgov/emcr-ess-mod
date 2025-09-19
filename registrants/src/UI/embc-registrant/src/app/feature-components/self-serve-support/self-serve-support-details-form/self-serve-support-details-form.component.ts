import { CUSTOM_ELEMENTS_SCHEMA, Component, Input } from '@angular/core';
import { FormArray, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import {
  DraftSupportForm,
  SelfServeClothingSupportForm,
  SelfServeFoodGroceriesSupportForm,
  SelfServeFoodRestaurantSupportForm,
  SelfServeIncidentsSupportForm,
  SelfServeShelerAllowanceSupportForm,
  SelfServeSupportDayMealForm,
  SupportDateForm,
  SupportPersonForm
} from '../self-serve-support.model';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatCardModule } from '@angular/material/card';
import { MatRadioModule } from '@angular/material/radio';
import { MatFormFieldModule } from '@angular/material/form-field';
import {
  DraftSupports,
  SelfServeClothingSupport,
  SelfServeFoodGroceriesSupport,
  SelfServeFoodRestaurantSupport,
  SelfServeIncidentalsSupport,
  SelfServeShelterAllowanceSupport,
  SelfServeSupportEligibilityState,
  SelfServeSupportType,
  SupportDayMeals
} from 'src/app/core/api/models';
import * as moment from 'moment';
import { SelfServeSupportRestaurantMealsInfoDialogComponent } from '../self-serve-support-restaurant-meals-info-dialog/self-serve-support-restaurant-meals-info-dialog.component';
import { EvacuationFileDataService } from 'src/app/sharedModules/components/evacuation-file/evacuation-file-data.service';
import { NgClass } from '@angular/common';

@Component({
  selector: 'app-self-serve-support-details-form',
  standalone: true,
  imports: [
    NgClass,
    ReactiveFormsModule,
    MatButtonModule,
    MatCheckboxModule,
    MatCardModule,
    MatRadioModule,
    MatFormFieldModule
  ],
  templateUrl: './self-serve-support-details-form.component.html',
  styleUrls: ['../self-serve-support-form.component.scss', './self-serve-support-details-form.component.scss'],
  schemas: [CUSTOM_ELEMENTS_SCHEMA]
})
export class SelfServeSupportDetailsFormComponent {
  SelfServeSupportType = SelfServeSupportType;
  SelfServeSupportEligibilityState = SelfServeSupportEligibilityState;
  @Input() supportDraftForm: FormGroup<DraftSupportForm>;

  showSelfServeShelterAllowanceSupport = false;
  showSelfServeFoodSupport = false;
  hasSelfServeFoodGroceriesSupport = false;
  hasSelfServiceFoodRestaurantSupport = false;
  showSelfServeClothingSupport = false;
  showSelfServeIncidentsSupport = false;

  foodRestaurantDates: moment.Moment[] = [];

  _draftSupports: DraftSupports;

  supportEligibilityStateSettings: Partial<Record<SelfServeSupportType, SelfServeSupportEligibilityState>> = {};

  @Input()
  set draftSupports(draftSupports: DraftSupports) {
    this.supportEligibilityStateSettings[SelfServeSupportType.ShelterAllowance] = this.getEligibilityState(
      SelfServeSupportType.ShelterAllowance
    );

    this.supportEligibilityStateSettings[SelfServeSupportType.FoodGroceries] = this.getEligibilityState(
      SelfServeSupportType.FoodGroceries
    );

    this.supportEligibilityStateSettings[SelfServeSupportType.FoodRestaurant] = this.getEligibilityState(
      SelfServeSupportType.FoodRestaurant
    );

    this.supportEligibilityStateSettings[SelfServeSupportType.Clothing] = this.getEligibilityState(
      SelfServeSupportType.Clothing
    );

    this.supportEligibilityStateSettings[SelfServeSupportType.Incidentals] = this.getEligibilityState(
      SelfServeSupportType.Incidentals
    );

    draftSupports.items.forEach((support) => {
      switch (support.type) {
        case SelfServeSupportType.ShelterAllowance:
          this.createSelfServeShelterAllowanceSupportForm(support, this.supportDraftForm.controls.shelterAllowance);
          break;

        case SelfServeSupportType.FoodGroceries:
          this.createSelfServeFoodGroceriesSupportForm(support, this.supportDraftForm.controls.food.controls.groceries);
          break;

        case SelfServeSupportType.FoodRestaurant:
          this.createSelfServeFoodRestaurantSupportForm(
            support,
            this.supportDraftForm.controls.food.controls.restaurant
          );
          break;

        case SelfServeSupportType.Clothing:
          this.createSelfServeClothingSupportForm(support, this.supportDraftForm.controls.clothing);
          break;

        case SelfServeSupportType.Incidentals:
          this.createSelfServeIncidentsSupportForm(support, this.supportDraftForm.controls.incidents);
          break;

        default:
          break;
      }
    });

    this._draftSupports = draftSupports;
  }

  get draftSupports() {
    return this._draftSupports;
  }

  constructor(
    private dialog: MatDialog,
    public evacuationFileDataService: EvacuationFileDataService
  ) { }

  private createSelfServeShelterAllowanceSupportForm(
    selfServeSupport: SelfServeShelterAllowanceSupport,
    selfServeSupportFormGroup: FormGroup<SelfServeShelerAllowanceSupportForm>
  ) {
    this.createSupportPersonFormArray(
      selfServeSupport.includedHouseholdMembers,
      selfServeSupportFormGroup.controls.includedHouseholdMembers
    );

    this.createSupportDateFormArray(selfServeSupport.nights, selfServeSupportFormGroup.controls.nights);

    selfServeSupportFormGroup.controls.totalAmount.setValue(selfServeSupport.totalAmount ?? 0);

    this.showSelfServeShelterAllowanceSupport = true;
  }

  getEligibilityState(type: SelfServeSupportType) {
    return this.evacuationFileDataService.selfServeEligibilityCheck.supportSettings.find((s) => s.type === type)?.state;
  }

  private createSelfServeFoodGroceriesSupportForm(
    selfServeSupport: SelfServeFoodGroceriesSupport,
    selfServeSupportFormGroup: FormGroup<SelfServeFoodGroceriesSupportForm>
  ) {
    this.createSupportPersonFormArray(
      selfServeSupport.includedHouseholdMembers,
      selfServeSupportFormGroup.controls.includedHouseholdMembers
    );

    this.createSupportDateFormArray(selfServeSupport.nights, selfServeSupportFormGroup.controls.nights);

    selfServeSupportFormGroup.controls.totalAmount.setValue(selfServeSupport.totalAmount ?? 0);

    this.showSelfServeFoodSupport = true;
    this.hasSelfServeFoodGroceriesSupport = true;

    if (
      this.supportEligibilityStateSettings[SelfServeSupportType.FoodGroceries] ===
      SelfServeSupportEligibilityState.UnavailableOneTimeUsed
    ) {
      this.hasSelfServeFoodGroceriesSupport = false;
    }
  }

  private createSelfServeFoodRestaurantSupportForm(
    selfServeSupport: SelfServeFoodRestaurantSupport,
    selfServeSupportFormGroup: FormGroup<SelfServeFoodRestaurantSupportForm>
  ) {
    const dates = selfServeSupport.meals.map((m) => moment(m.date, 'YYYY-MM-DD'));
    var tooManyMeals = false;
    var maxMeals = 0;
    selfServeSupport.meals.forEach((meal) => {
      if (meal.breakfast) maxMeals++;
      if (meal.lunch) maxMeals++;
      if (meal.dinner) maxMeals++;
    });
    if (maxMeals > 9) {
      tooManyMeals = true;
      maxMeals = 9;
    }

    selfServeSupport.includedHouseholdMembers.forEach((p) => {
      selfServeSupportFormGroup.controls.includedHouseholdMembers.push(this.createSupportPersonFormGroup(p));
    });

    const originalMealSupportsDraft: Record<string, SupportDayMeals> = {};

    selfServeSupportFormGroup.controls.mealTypes.valueChanges.subscribe({
      next: () => {
        // get all unchecked and undisabled control
        const unCheckedControls: Record<string, FormControl[]> = {
          breakfast: [],
          lunch: [],
          dinner: []
        };

        const checkedCount: Record<string, number> = {
          breakfast: 0,
          lunch: 0,
          dinner: 0,
          total: 0
        };

        selfServeSupportFormGroup.controls.mealTypes.controls.forEach((meal) => {
          if (meal.controls.breakfast.value === true && tooManyMeals === false) {
            checkedCount.breakfast++;
            checkedCount.total++;
          }
          else if (
            originalMealSupportsDraft[meal.controls.date.value.format('YYYY-MM-DD')].breakfast === true
          ) {
            if (tooManyMeals) {
              meal.controls.breakfast.setValue(false, { emitEvent: false });
              tooManyMeals = false;
            }
            unCheckedControls.breakfast.push(meal.controls.breakfast);
          }

          if (meal.controls.lunch.value === true && tooManyMeals === false) {
            checkedCount.lunch++;
            checkedCount.total++;
          }
          else if (
            originalMealSupportsDraft[meal.controls.date.value.format('YYYY-MM-DD')].lunch === true
          ) {
            if (tooManyMeals) {
              meal.controls.breakfast.setValue(false, { emitEvent: false });
              tooManyMeals = false;
            }
            unCheckedControls.lunch.push(meal.controls.lunch);
          }

          if (meal.controls.dinner.value === true && tooManyMeals === false) {
            checkedCount.dinner++;
            checkedCount.total++;
          }
          else if (
            originalMealSupportsDraft[meal.controls.date.value.format('YYYY-MM-DD')].dinner === true
          ) {
            if (tooManyMeals) {
              meal.controls.breakfast.setValue(false, { emitEvent: false });
              tooManyMeals = false;
            }
            unCheckedControls.dinner.push(meal.controls.dinner);
          }
        });

        // @Note: Only 3 dates can be checked per meal type
        // check count of selected checkboxes are 3 then disable the first unchecked
        if (checkedCount.breakfast >= 3 || checkedCount.total >= maxMeals) {
          unCheckedControls.breakfast.forEach((c) => c.disable({ emitEvent: false }));
        } else {
          unCheckedControls.breakfast.forEach((c) => c.enable({ emitEvent: false }));
        }

        if (checkedCount.lunch >= 3 || checkedCount.total >= maxMeals) {
          unCheckedControls.lunch.forEach((c) => c.disable({ emitEvent: false }));
        } else {
          unCheckedControls.lunch.forEach((c) => c.enable({ emitEvent: false }));
        }

        if (checkedCount.dinner >= 3 || checkedCount.total >= maxMeals) {
          unCheckedControls.dinner.forEach((c) => c.disable({ emitEvent: false }));
        } else {
          unCheckedControls.dinner.forEach((c) => c.enable({ emitEvent: false }));
        }
      }
    });

    selfServeSupport.meals.forEach((m) => {
      const date = moment(m.date, 'YYYY-MM-DD');

      originalMealSupportsDraft[date.format('YYYY-MM-DD')] = m;

      selfServeSupportFormGroup.controls.mealTypes.push(
        new FormGroup<SelfServeSupportDayMealForm>({
          date: new FormControl(date),
          breakfast: new FormControl({ value: m.breakfast, disabled: m.breakfast !== true }),
          lunch: new FormControl({ value: m.lunch, disabled: m.lunch !== true }),
          dinner: new FormControl({ value: m.dinner, disabled: m.dinner !== true })
        })
      );
    });

    selfServeSupportFormGroup.controls.includedHouseholdMembers.valueChanges.subscribe({
      next: (includedHouseholdMembers) => {
        if (includedHouseholdMembers.every((m) => !m.isSelected)) {
          selfServeSupportFormGroup.controls.mealTypes.controls.forEach((m) => {
            if (m.controls.breakfast.value === true) {
              m.controls.breakfast.setValue(false, { emitEvent: false });
            }
            if (m.controls.lunch.value === true) {
              m.controls.lunch.setValue(false, { emitEvent: false });
            }
            if (m.controls.dinner.value === true) {
              m.controls.dinner.setValue(false, { emitEvent: false });
            }
            // disalbe all fields
            m.controls.breakfast.disable({ emitEvent: false });
            m.controls.lunch.disable({ emitEvent: false });
            m.controls.dinner.disable({ emitEvent: false });
          });
        } else {
          selfServeSupportFormGroup.controls.mealTypes.controls.forEach((meal) => {
            if (
              originalMealSupportsDraft[meal.controls.date.value.format('YYYY-MM-DD')].breakfast === true
            ) {
              meal.controls.breakfast.enable({ emitEvent: false });
            }

            if (
              originalMealSupportsDraft[meal.controls.date.value.format('YYYY-MM-DD')].lunch === true
            ) {
              meal.controls.lunch.enable({ emitEvent: false });
            }

            if (
              originalMealSupportsDraft[meal.controls.date.value.format('YYYY-MM-DD')].dinner === true
            ) {
              meal.controls.dinner.enable({ emitEvent: false });
            }
          });
        }
      }
    });

    selfServeSupportFormGroup.controls.totalAmount.setValue(selfServeSupport.totalAmount ?? 0);

    this.foodRestaurantDates = [...dates];

    this.showSelfServeFoodSupport = true;
    this.hasSelfServiceFoodRestaurantSupport = true;

    if (
      this.supportEligibilityStateSettings[SelfServeSupportType.FoodRestaurant] ===
      SelfServeSupportEligibilityState.UnavailableOneTimeUsed
    ) {
      this.hasSelfServiceFoodRestaurantSupport = false;
    }
  }

  private createSelfServeClothingSupportForm(
    selfServeSupport: SelfServeClothingSupport,
    selfServeSupportFormGroup: FormGroup<SelfServeClothingSupportForm>
  ) {
    selfServeSupport.includedHouseholdMembers.forEach((p) => {
      selfServeSupportFormGroup.controls.includedHouseholdMembers.push(this.createSupportPersonFormGroup(p));
    });

    selfServeSupportFormGroup.controls.totalAmount.setValue(selfServeSupport.totalAmount ?? 0);

    this.showSelfServeClothingSupport = true;
  }

  private createSelfServeIncidentsSupportForm(
    selfServeSupport: SelfServeIncidentalsSupport,
    selfServeSupportFormGroup: FormGroup<SelfServeIncidentsSupportForm>
  ) {
    selfServeSupport.includedHouseholdMembers.forEach((p) => {
      selfServeSupportFormGroup.controls.includedHouseholdMembers.push(this.createSupportPersonFormGroup(p));
    });

    selfServeSupportFormGroup.controls.totalAmount.setValue(selfServeSupport.totalAmount ?? 0);

    this.showSelfServeIncidentsSupport = true;
  }

  createSupportPersonFormArray(persons: string[], personFormArray: FormArray<FormGroup<SupportPersonForm>>) {
    persons.forEach((p) => personFormArray.push(this.createSupportPersonFormGroup(p)));
  }

  createSupportPersonFormGroup(id: string, isSelected: boolean = true): FormGroup<SupportPersonForm> {
    return new FormGroup<SupportPersonForm>({ personId: new FormControl(id), isSelected: new FormControl(isSelected) });
  }

  createSupportDateFormArray(dates: string[], datesFormArray: FormArray<FormGroup<SupportDateForm>>) {
    dates.forEach((d) => datesFormArray.push(this.createSupportDateFormGroup(moment(d, 'YYYY-MM-DD'))));
  }

  createSupportDateFormGroup(date: moment.Moment, isSelected: boolean = true): FormGroup<SupportDateForm> {
    return new FormGroup<SupportDateForm>({ date: new FormControl(date), isSelected: new FormControl(isSelected) });
  }

  getPersonName(id: string) {
    const personDetails = this.draftSupports.householdMembers.find((p) => p.id == id)?.details;
    return `${personDetails?.firstName ?? ''}`;
  }

  openRestaurantMealsInfoDialog() {
    this.dialog
      .open(SelfServeSupportRestaurantMealsInfoDialogComponent, {
        data: {},
        maxWidth: '400px'
      })
      .afterClosed()
      .subscribe();
  }
}
