using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EMBC.ESS.Engines.Supporting;
using EMBC.ESS.Engines.Supporting.SupportGeneration.SelfServe;
using EMBC.ESS.Engines.Supporting.SupportGeneration.SelfServe.SupportCreationStrategies;
using EMBC.ESS.Managers.Events.Notifications;
using EMBC.ESS.Shared.Contracts.Events.SelfServe;
using EMBC.Utilities.Extensions;
using EMBC.Utilities.Transformation;
using HandlebarsDotNet;
using Shouldly;
using Xunit;

namespace EMBC.Tests.Unit.ESS.Engines;

public class SelfServeSupportGenerationTests
{
    private readonly SelfServeSupportGenerator strategy;
    private readonly DateTime startDate;
    private readonly DateTime endDate;
    private readonly IEnumerable<SelfServeHouseholdMember> householdMembers;
    private readonly DateOnly[] expectedDays;
    private readonly IEnumerable<string> expectedHouseholdMemberIds;

    public SelfServeSupportGenerationTests()
    {
        strategy = new SelfServeSupportGenerator(new SelfServeSupportCreationStrategy());

        startDate = new DateTime(2024, 5, 1, 12, 0, 0, DateTimeKind.Utc).ToPST();
        endDate = startDate.AddHours(72);
        householdMembers = Enumerable.Range(1, 5).Select(i => new SelfServeHouseholdMember(i.ToString(), false));
        expectedDays = [DateOnly.FromDateTime(startDate), DateOnly.FromDateTime(startDate.AddHours(24)), DateOnly.FromDateTime(startDate.AddHours(48))];
        expectedHouseholdMemberIds = householdMembers.Select(hm => hm.Id).ToList();
    }

    [Fact]
    public async Task Generate_ShelterAllowance_Created()
    {
        var support = await GenerateSelfServeSupports<SelfServeShelterAllowanceSupport>(SelfServeSupportType.ShelterAllowance);
        support.Nights.ShouldBe(expectedDays);
        support.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);
        support.TotalAmount.ShouldBe(600d);
    }

    [Fact]
    public async Task Generate_ShelterAllowanceWithMinors_Created()
    {
        var householdMembersWithMinors = householdMembers.ToList();
        householdMembersWithMinors[4] = new SelfServeHouseholdMember(householdMembersWithMinors[4].Id, true);
        var support = await GenerateSelfServeSupports<SelfServeShelterAllowanceSupport>(SelfServeSupportType.ShelterAllowance, householdMembersWithMinors);
        support.Nights.ShouldBe(expectedDays);
        support.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);
        support.TotalAmount.ShouldBe(600d);
    }

    [Fact]
    public async Task Generate_ShelterAllowanceNoHouseholdMembers_NotCreated()
    {
        ((GenerateSelfServeSupportsResponse)await strategy.Generate(new GenerateSelfServeSupports([SelfServeSupportType.ShelterAllowance], startDate, endDate, []), default)).Supports.ShouldBeEmpty();
    }

    [Fact]
    public async Task Generate_ShelterAllowanceSingleDay_Created()
    {
        var fromDate = DateTime.Today;
        var support = await GenerateSelfServeSupports<SelfServeShelterAllowanceSupport>(SelfServeSupportType.ShelterAllowance, overrideStartDate: fromDate, overrideEndDate: fromDate.AddHours(23));
        support.Nights.ShouldBe([DateOnly.FromDateTime(fromDate.AddDays(-1))]); /* Start time < 5am - Should be the night before. */
        support.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);
        support.TotalAmount.ShouldBe(200d);
    }

    [Fact]
    public async Task Generate_Incidentals_Created()
    {
        var support = await GenerateSelfServeSupports<SelfServeIncidentalsSupport>(SelfServeSupportType.Incidentals);
        support.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);
        support.TotalAmount.ShouldBe(250d);
    }

    [Fact]
    public async Task Generate_Clothing_Created()
    {
        var support = await GenerateSelfServeSupports<SelfServeClothingSupport>(SelfServeSupportType.Clothing);
        support.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);
        support.TotalAmount.ShouldBe(750d);
    }

    [Fact]
    public async Task Generate_FoodGroceries_Created()
    {
        var response = (GenerateSelfServeSupportsResponse)await strategy.Generate(new GenerateSelfServeSupports([SelfServeSupportType.FoodGroceries, SelfServeSupportType.FoodRestaurant], startDate, endDate, householdMembers), default);
        response.Supports.Count().ShouldBe(2);

        var groceries = (SelfServeFoodGroceriesSupport)response.Supports.Single(s => s is SelfServeFoodGroceriesSupport);
        groceries.Nights.ShouldBe(expectedDays);
        groceries.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);

        groceries.TotalAmount.ShouldBe(337.5d);

        var restaurant = (SelfServeFoodRestaurantSupport)response.Supports.Single(s => s is SelfServeFoodRestaurantSupport);
        restaurant.IncludedHouseholdMembers.ShouldBe(expectedHouseholdMemberIds);
        // restaurant support days are different
        IEnumerable<DateOnly> mealsExpectedDays = [DateOnly.FromDateTime(startDate), DateOnly.FromDateTime(startDate.AddHours(24)), DateOnly.FromDateTime(startDate.AddHours(48)), DateOnly.FromDateTime(startDate.AddHours(72))];
        // No longer true: mealsExpectedDays = startDate.Hour >= 11 ? mealsExpectedDays : mealsExpectedDays.Prepend(DateOnly.FromDateTime(startDate.AddDays(-1)));
        restaurant.Meals.Select(m => m.Date).ShouldBe(mealsExpectedDays);
        var numberOfBreakfasts = restaurant.Meals.Aggregate(0, (n, m) => n + (m.Breakfast == true ? 1 : 0));
        var numberOfLunches = restaurant.Meals.Aggregate(0, (n, m) => n + (m.Lunch == true ? 1 : 0));
        var numberOfDinners = restaurant.Meals.Aggregate(0, (n, m) => n + (m.Dinner == true ? 1 : 0));
        numberOfBreakfasts.ShouldBe(3);
        numberOfLunches.ShouldBe(3);
        numberOfDinners.ShouldBe(3);
        restaurant.TotalAmount.ShouldBe(795d);
    }

    private async Task<T> GenerateSelfServeSupports<T>(SelfServeSupportType type, IEnumerable<SelfServeHouseholdMember>? overrideHouseholdMembers = null, DateTime? overrideStartDate = null, DateTime? overrideEndDate = null) where T : SelfServeSupport
    {
        var response = (GenerateSelfServeSupportsResponse)await strategy.Generate(new GenerateSelfServeSupports([type], overrideStartDate ?? startDate, overrideEndDate ?? endDate, overrideHouseholdMembers ?? householdMembers), default);
        return response.Supports.ShouldHaveSingleItem().ShouldBeOfType<T>();
    }

    [Fact]
    public async Task CreateSelfServeSupportsETransferEmailContentFoodOnly()
    {
        var endDate = DateTime.UtcNow.AddDays(3).ToPST();
        IEnumerable<KeyValuePair<string, string>> tokens = new[]
        {
                KeyValuePair.Create("totalAmount", "120"),
                KeyValuePair.Create("groceryAmount", "30"),
                KeyValuePair.Create("restaurantAmount", "90"),
                KeyValuePair.Create("recipientName","John Doe - Food Only"),
                KeyValuePair.Create("notificationEmail","John.Doe@example.com"),
                KeyValuePair.Create("endDate", endDate.ToString("MMMM d, yyyy")),
                KeyValuePair.Create("endTime", endDate.ToString("h:mm tt")),
        };

        EmailTemplateProvider etp = new EmailTemplateProvider();
        var emailTemplate = (EmailTemplate)await etp.Get(SubmissionTemplateType.ETransferConfirmation);

        string emailContent = "";
        if (!string.IsNullOrWhiteSpace(emailTemplate.Content))
        {
            var transformationData = new TransformationData
            {
                Template = emailTemplate.Content,
                Tokens = new Dictionary<string, string>(tokens)
            };
            var template = Handlebars.Compile(transformationData.Template);

            var taskResult = await Task.FromResult(new TransformationResult { Content = template(transformationData.Tokens) });
            emailContent = taskResult.Content;
        }

        emailContent.ShouldNotBeNullOrEmpty();
        await File.WriteAllTextAsync("./eTransferEmailConfirmationFoodOnly.html", emailContent);
    }

    [Fact]
    public async Task CreateSelfServeSupportsETransferEmailContentFoodExcluded()
    {
        var endDate = DateTime.UtcNow.AddDays(3).ToPST();
        IEnumerable<KeyValuePair<string, string>> tokens = new[]
        {
                KeyValuePair.Create("totalAmount", "160"),
                KeyValuePair.Create("clothingAmount", "30"),
                KeyValuePair.Create("incidentalsAmount", "90"),
                KeyValuePair.Create("shelterAllowanceAmount", "40"),
                KeyValuePair.Create("recipientName","John Doe - Food Excluded"),
                KeyValuePair.Create("notificationEmail","John.Doe@example.com"),
                KeyValuePair.Create("endDate", endDate.ToString("MMMM d, yyyy")),
                KeyValuePair.Create("endTime", endDate.ToString("h:mm tt")),
        };

        EmailTemplateProvider etp = new EmailTemplateProvider();
        var emailTemplate = (EmailTemplate)await etp.Get(SubmissionTemplateType.ETransferConfirmation);

        string emailContent = "";
        if (!string.IsNullOrWhiteSpace(emailTemplate.Content))
        {
            var transformationData = new TransformationData
            {
                Template = emailTemplate.Content,
                Tokens = new Dictionary<string, string>(tokens)
            };
            var template = Handlebars.Compile(transformationData.Template);

            var taskResult = await Task.FromResult(new TransformationResult { Content = template(transformationData.Tokens) });
            emailContent = taskResult.Content;
        }

        emailContent.ShouldNotBeNullOrEmpty();
        await File.WriteAllTextAsync("./eTransferEmailConfirmationFoodExcluded.html", emailContent);
    }
}
