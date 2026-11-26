using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class StayTests : SchedulePageTest
{
    private ILocator StayDialog() => Page.GetByTestId("stay-dialog");

    private async Task TypeStayNameAsync(string name)
    {
        await Page.GetByTestId("use-custom-stay").ClickAsync();
        await Page.GetByLabel("Hotel or place").FillAsync(name);
    }

    private async Task ChooseAsync(string selectTestId, string label)
    {
        var select = Page.GetByTestId(selectTestId);
        await select.GetByTestId("select-trigger").ClickAsync();
        await select.GetByRole(AriaRole.Option).Filter(new() { HasText = label }).First.ClickAsync();
    }

    private async Task SaveStayAsync()
    {
        await Page.GetByTestId("save-stay").ClickAsync();
        await Expect(StayDialog()).ToHaveCountAsync(0);
    }

    [Test]
    public async Task AddStayFromADay_ShowsTonightOnEachNight()
    {
        await ScheduleDay(1).GetByTestId("day-add-stay").ClickAsync();
        await Expect(StayDialog()).ToBeVisibleAsync();
        await TypeStayNameAsync("Hotel Gracery");
        await ChooseAsync("check-out-select", "Fri, May 3");
        await Expect(Page.GetByTestId("stay-nights")).ToHaveTextAsync("2 nights");
        await Page.GetByLabel("Total cost").FillAsync("9000");
        await SaveStayAsync();

        await Expect(ScheduleDay(1).GetByTestId("day-stay")).ToContainTextAsync("Hotel Gracery");
        await Expect(ScheduleDay(1).GetByTestId("day-stay")).ToContainTextAsync("Night 1 of 2");
        await Expect(ScheduleDay(2).GetByTestId("day-stay")).ToContainTextAsync("Night 2 of 2");
        await Expect(ScheduleDay(3).GetByTestId("day-checkout")).ToContainTextAsync("Hotel Gracery");
        await Expect(ScheduleDay(3).GetByTestId("day-add-stay")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("stay-card-name")).ToHaveTextAsync(["🛏 Hotel Gracery"]);
        await Expect(Page.GetByTestId("nights-without-stay")).ToHaveTextAsync("1 night without a stay");
    }

    [Test]
    public async Task StayCost_IsSplitAcrossNightsAndAddedToTheBudget()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00", cost: "500");

        await Page.GetByTestId("add-stay").ClickAsync();
        await TypeStayNameAsync("Hotel Gracery");
        await ChooseAsync("check-out-select", "Fri, May 3");
        await Page.GetByLabel("Total cost").FillAsync("9000");
        await Expect(Page.GetByTestId("stay-cost-preview")).ToHaveTextAsync("NT$4,500.00 / night");
        await SaveStayAsync();

        await Expect(ScheduleDay(1).GetByTestId("day-total")).ToHaveTextAsync("NT$5,000.00");
        await Expect(ScheduleDay(2).GetByTestId("day-total")).ToHaveTextAsync("NT$4,500.00");
        await Expect(Page.GetByTestId("trip-total")).ToHaveTextAsync("NT$9,500.00");
    }

    [Test]
    public async Task OverlappingStay_ShowsTheErrorInTheDialog()
    {
        await Page.GetByTestId("add-stay").ClickAsync();
        await TypeStayNameAsync("Hotel Gracery");
        await ChooseAsync("check-out-select", "Fri, May 3");
        await SaveStayAsync();

        await ScheduleDay(2).GetByTestId("day-stay").ClickAsync();
        await Expect(StayDialog()).ToContainTextAsync("Edit \"Hotel Gracery\"");
        await Page.GetByTestId("close-stay-dialog").ClickAsync();

        await ScheduleDay(3).GetByTestId("day-add-stay").ClickAsync();
        await TypeStayNameAsync("Ryokan");
        await ChooseAsync("check-in-select", "Thu, May 2");
        await Page.GetByTestId("save-stay").ClickAsync();

        await Expect(StayDialog().GetByTestId("stay-error")).ToContainTextAsync("overlap with \"Hotel Gracery\"");
    }

    [Test]
    public async Task EditAndDeleteStay()
    {
        await Page.GetByTestId("add-stay").ClickAsync();
        await TypeStayNameAsync("Hotel");
        await SaveStayAsync();

        await Page.GetByTestId("stay-card").ClickAsync();
        await Page.GetByLabel("Hotel or place").FillAsync("Hotel Gracery");
        await ChooseAsync("stay-booking-select", "Booked");
        await SaveStayAsync();
        await Expect(Page.GetByTestId("stay-card")).ToContainTextAsync("Hotel Gracery");
        await Expect(Page.GetByTestId("stay-card")).ToContainTextAsync("Booked");

        await Page.ReloadAsync();
        await Page.GetByTestId("stay-card").ClickAsync();
        await Page.GetByTestId("delete-stay").ClickAsync();
        await Page.GetByTestId("confirm-button").ClickAsync();

        await Expect(Page.GetByTestId("stay-card")).ToHaveCountAsync(0);
        await Expect(ScheduleDay(1).GetByTestId("day-add-stay")).ToBeVisibleAsync();
    }

    [Test]
    public async Task AddStayFromAPlace_UsesThePlaceName()
    {
        await RequireFakePlacesAsync();
        await Page.GetByTestId("add-stay").ClickAsync();
        await Page.GetByLabel("Hotel or place").FillAsync("Tokyo Tower");
        await PlaceOption("Tokyo Tower").ClickAsync();
        await SaveStayAsync();

        await Expect(ScheduleDay(1).GetByTestId("day-stay-name")).ToHaveTextAsync("Tokyo Tower");
    }
}
