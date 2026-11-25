using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class BudgetTests : SchedulePageTest
{
    [Test]
    public async Task NewTrip_HasZeroBudget()
    {
        await Expect(TripTotal()).ToHaveTextAsync("0.00");
        await Expect(DayTotals()).ToHaveTextAsync(["0.00", "0.00", "0.00"]);
    }

    [Test]
    public async Task AddActivitiesWithCosts_ShowsDailySubtotalsAndTripTotal()
    {
        await AddActivityAsync("Tsukiji Market", day: 1, "07:30", "08:30", cost: "1500.5");
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await AddActivityAsync("Shibuya Sky", day: 1, "17:00", "18:00", cost: "2,200");
        await AddActivityAsync("teamLab Planets", day: 3, "13:00", "15:00", cost: "3800.25");

        await Expect(ScheduleDay(1).GetByTestId("activity-cost"))
            .ToHaveTextAsync(["1,500.50", "Free", "2,200.00"]);
        await Expect(DayTotals()).ToHaveTextAsync(["3,700.50", "0.00", "3,800.25"]);
        await Expect(TripTotal()).ToHaveTextAsync("7,500.75");

        await Page.ReloadAsync();
        await Expect(DayTotals()).ToHaveTextAsync(["3,700.50", "0.00", "3,800.25"]);
        await Expect(TripTotal()).ToHaveTextAsync("7,500.75");
    }

    [Test]
    public async Task Totals_KeepDecimalPrecision()
    {
        await AddActivityAsync("Coffee", day: 2, "08:00", "08:30", cost: "0.10");
        await AddActivityAsync("Snack", day: 2, "09:00", "09:30", cost: "0.20");

        await Expect(ScheduleDay(2).GetByTestId("day-total")).ToHaveTextAsync("0.30");
        await Expect(TripTotal()).ToHaveTextAsync("0.30");
    }

    [Test]
    public async Task EditActivityCost_UpdatesTotals()
    {
        await AddActivityAsync("Shibuya Sky", day: 1, "17:00", "18:00", cost: "2200");
        await AddActivityAsync("teamLab Planets", day: 3, "13:00", "15:00", cost: "3800");

        await ActivityItem("Shibuya Sky").GetByTestId("edit-activity").ClickAsync();
        await Expect(CostInput()).ToHaveValueAsync("2200.00");
        await CostInput().FillAsync("2500");
        await SubmitButton("Save").ClickAsync();

        await Expect(ScheduleDay(1).GetByTestId("day-total")).ToHaveTextAsync("2,500.00");
        await Expect(TripTotal()).ToHaveTextAsync("6,300.00");
    }

    [Test]
    public async Task MoveActivityToAnotherDay_MovesItsCost()
    {
        await AddActivityAsync("Shibuya Sky", day: 1, "17:00", "18:00", cost: "2200");

        await ActivityItem("Shibuya Sky").GetByTestId("edit-activity").ClickAsync();
        await ChooseDayAsync(2);
        await SubmitButton("Save").ClickAsync();

        await Expect(DayTotals()).ToHaveTextAsync(["0.00", "2,200.00", "0.00"]);
        await Expect(TripTotal()).ToHaveTextAsync("2,200.00");
    }

    [Test]
    public async Task DeleteActivity_RemovesItsCostFromTotals()
    {
        await AddActivityAsync("Shibuya Sky", day: 1, "17:00", "18:00", cost: "2200");
        await AddActivityAsync("teamLab Planets", day: 3, "13:00", "15:00", cost: "3800");

        await ActivityItem("Shibuya Sky").GetByTestId("delete-activity").ClickAsync();
        await Page.GetByRole(AriaRole.Alertdialog).GetByTestId("confirm-button").ClickAsync();

        await Expect(DayTotals()).ToHaveTextAsync(["0.00", "0.00", "3,800.00"]);
        await Expect(TripTotal()).ToHaveTextAsync("3,800.00");
    }

    [Test]
    public async Task InvalidCost_IsRejectedBeforeSaving()
    {
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Activity" }).FillAsync("Shibuya Sky");
        await PickTimeAsync("Start", "17:00");
        await PickTimeAsync("End", "18:00");

        await CostInput().FillAsync("12.345");

        await Expect(Page.GetByTestId("cost-error"))
            .ToHaveTextAsync("Cost must be a positive amount with up to 2 decimal places.");
        await Expect(SubmitButton("Add")).ToBeDisabledAsync();
        await Expect(TripTotal()).ToHaveTextAsync("0.00");
    }

    private ILocator TripTotal() => Page.GetByTestId("trip-total");

    private ILocator DayTotals() => Page.GetByTestId("day-total");
}
