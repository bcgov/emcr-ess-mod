using System;
using System.Collections.Generic;
using System.Linq;
using EMBC.ESS.Shared.Contracts.Events.SelfServe;

namespace EMBC.ESS.Engines.Supporting.SupportGeneration.SelfServe.SupportCreationStrategies;

internal class FoodRestraurantStrategy : ISelfServeSupportCreationStrategy<SelfServeFoodRestaurantSupport>
{
    private static SelfServeFoodRestaurantSupport Create(DateTime from, DateTime to, IEnumerable<SelfServeHouseholdMember> householdMembers)
    {
        var support = new SelfServeFoodRestaurantSupport
        {
            Meals = CalculateMealPlan(from, to),
            IncludedHouseholdMembers = householdMembers.Select(hm => hm.Id).ToList(),
            TotalAmount = 0d
        };
        support.TotalAmount = CalculateSelfServeSupportAmount(support);
        return support;
    }

    private static IEnumerable<SupportDayMeals> CalculateMealPlan(DateTime from, DateTime to)
    {
        var meals = new Dictionary<DateOnly, SupportDayMeals>();
        int maxBreakfasts = 3;
        int maxLunches = 3;
        int maxDinners = 3;

        var startingTime = from;
        var endingTime = to;    

        DateOnly firstDay = DateOnly.FromDateTime(startingTime);

        // allow previous meal if occurs in same first day
        if (startingTime.Hour >= 11 && startingTime.Hour < 15)
        {
            meals.Add(firstDay, new SupportDayMeals(firstDay) { Breakfast = true });
            maxBreakfasts++;
        }
        else if (startingTime.Hour >= 15 && startingTime.Hour <= 24 ||
                 startingTime.Hour >= 0 && startingTime.Hour < 1) 
        {
            meals.Add(firstDay, new SupportDayMeals(firstDay) { Lunch = true });
            maxLunches++;
        }

        while (startingTime < endingTime)
        {
            var day = DateOnly.FromDateTime(startingTime);
            var meal = meals.ContainsKey(day) ? meals[day] : new SupportDayMeals(day);
            switch (startingTime.Hour)
            {
                case >= 1 and < 11:
                    meal.Breakfast = meals.Values.Count(m => m.Breakfast == true) < maxBreakfasts;
                    startingTime = new DateTime(day, new TimeOnly(11, 1), DateTimeKind.Unspecified);
                    break;

                case >= 11 and < 15:
                    meal.Lunch = meals.Values.Count(m => m.Lunch == true) < maxLunches;
                    startingTime = new DateTime(day, new TimeOnly(15, 1), DateTimeKind.Unspecified);
                    break;

                case >= 15 and <= 24 or >= 0 and < 1:
                    meal.Dinner = meals.Values.Count(m => m.Dinner == true) < maxDinners;
                    startingTime = new DateTime(day.AddDays(1), new TimeOnly(1, 1), DateTimeKind.Unspecified);
                    break;
            }
            meals[day] = meal;
        }

        return meals.Values;
    }

    private static double CalculateSelfServeSupportAmount(SelfServeFoodRestaurantSupport support)
    {
        var numberOfHouseholdMembers = support.IncludedHouseholdMembers.Count();
        var amount = 0d;
        foreach (var meal in support.Meals)
        {
            if (meal.Breakfast == true) amount += 12.75d * numberOfHouseholdMembers;
            if (meal.Lunch == true) amount += 14.75d * numberOfHouseholdMembers;
            if (meal.Dinner == true) amount += 25.50d * numberOfHouseholdMembers;
        }
        return amount;
    }

    public SelfServeFoodRestaurantSupport Create(CreateSelfServeSupport cmd) => Create(cmd.From, cmd.To, cmd.HouseholdMembers);

    public SelfServeFoodRestaurantSupport Evaluate(SelfServeFoodRestaurantSupport support)
    {
        support.TotalAmount = CalculateSelfServeSupportAmount(support);
        return support;
    }

    public bool Validate(SelfServeFoodRestaurantSupport support) => support.TotalAmount > 0 && support.IncludedHouseholdMembers.Any() && support.Meals.Any();
}
