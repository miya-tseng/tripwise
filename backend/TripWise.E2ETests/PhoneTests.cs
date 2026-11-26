using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class PhoneTests : SchedulePageTest
{
    public override BrowserNewContextOptions ContextOptions()
    {
        var options = base.ContextOptions();
        options.ViewportSize = new() { Width = 390, Height = 844 };
        options.IsMobile = true;
        options.HasTouch = true;
        return options;
    }

    [Test]
    public async Task FirstDay_IsVisibleWithoutScrolling()
    {
        await Expect(ScheduleDay(1).GetByTestId("day-date")).ToBeInViewportAsync();
        await Expect(Page.GetByTestId("add-activity")).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("fab")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("bottom-nav")).ToBeVisibleAsync();
    }

    [Test]
    public async Task BottomNav_MovesBetweenSectionsAndHome()
    {
        await Page.GetByTestId("nav-overview").TapAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Overview", Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("nav-overview")).ToHaveAttributeAsync("aria-current", "page");

        await Page.GetByTestId("nav-checklist").TapAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Checklist", Level = 1 })).ToBeVisibleAsync();

        await Page.GetByTestId("nav-itinerary").TapAsync();
        await Expect(Page).ToHaveURLAsync(new Regex($"/trips/{TripId}$"));
        await Expect(ScheduleDay(1)).ToBeVisibleAsync();

        await Page.GetByTestId("nav-trips").TapAsync();
        await Expect(Page.GetByTestId("app-bar")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("bottom-nav")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DayStrip_JumpsToTheDay()
    {
        await AddFromFabAsync("Senso-ji", "10:00", "11:00");

        await Page.GetByTestId("day-chip").Nth(2).TapAsync();

        await Expect(ScheduleDay(3).GetByTestId("day-date")).ToBeInViewportAsync();
        await Expect(Page.GetByTestId("day-chip").Nth(2)).ToHaveClassAsync(new Regex("active"));
    }

    [Test]
    public async Task FloatingButton_AddsToTheDayInView()
    {
        await Page.GetByTestId("day-chip").Nth(1).TapAsync();
        await Expect(Page.GetByTestId("day-chip").Nth(1)).ToHaveClassAsync(new Regex("active"));

        await AddFromFabAsync("Tokyo Tower", "18:00", "19:00");

        await Expect(ActivityNames(day: 2)).ToHaveTextAsync(["Tokyo Tower"]);
    }

    [Test]
    public async Task DialogButtons_StayVisibleOnAShortScreen()
    {
        await Page.GetByTestId("fab").TapAsync();
        await Page.GetByTestId("fab-add-activity").TapAsync();

        await Expect(SubmitButton("Add")).ToBeInViewportAsync();
        await Expect(SubmitButton("Cancel")).ToBeInViewportAsync();
    }

    [Test]
    public async Task TapAnActivity_OpensItAndCanDeleteIt()
    {
        await AddFromFabAsync("Senso-ji", "10:00", "11:00");

        await ActivityItem("Senso-ji").GetByTestId("activity-name").TapAsync();
        await Expect(Dialog()).ToContainTextAsync("Edit \"Senso-ji\"");

        await Page.GetByTestId("dialog-delete-activity").TapAsync();
        await Page.GetByTestId("confirm-button").TapAsync();

        await Expect(Dialog()).ToHaveCountAsync(0);
        await Expect(ActivityItem("Senso-ji")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Stays_AreSummarizedUntilOpened()
    {
        await Page.GetByTestId("fab").TapAsync();
        await Page.GetByTestId("fab-add-stay").TapAsync();
        await Page.GetByTestId("use-custom-stay").TapAsync();
        await Page.GetByLabel("Hotel or place").FillAsync("Hotel Gracery");
        await Page.GetByTestId("save-stay").TapAsync();
        await Expect(Page.GetByTestId("stay-dialog")).ToHaveCountAsync(0);

        var toggle = Page.GetByTestId("stays-toggle");
        await Expect(toggle).ToContainTextAsync("1 stay");
        await Expect(Page.GetByTestId("stay-card")).ToBeHiddenAsync();

        await toggle.TapAsync();

        await Expect(Page.GetByTestId("stay-card")).ToBeVisibleAsync();
    }

    [Test]
    public async Task Itinerary_ShowsOneDayAtATime()
    {
        await Expect(ScheduleDay(1)).ToBeVisibleAsync();
        await Expect(ScheduleDay(2)).ToBeHiddenAsync();
        await Expect(ScheduleDay(3)).ToBeHiddenAsync();

        await Page.GetByTestId("next-day").TapAsync();
        await Expect(ScheduleDay(2)).ToBeVisibleAsync();
        await Expect(ScheduleDay(1)).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("day-chip").Nth(1)).ToHaveClassAsync(new Regex("active"));

        await Page.GetByTestId("previous-day").TapAsync();
        await Expect(ScheduleDay(1)).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("previous-day")).ToBeDisabledAsync();
    }

    [Test]
    public async Task OverviewDayLink_OpensThatDay()
    {
        await Page.GotoAsync($"/trips/{TripId}?view=timeline&day=3");

        await Expect(ScheduleDay(3)).ToBeVisibleAsync();
        await Expect(ScheduleDay(1)).ToBeHiddenAsync();
    }

    private async Task AddFromFabAsync(string name, string start, string end)
    {
        await Page.GetByTestId("fab").TapAsync();
        await Page.GetByTestId("fab-add-activity").TapAsync();
        await TypeCustomActivityAsync(name);
        await PickTimeAsync("Start", start);
        await PickTimeAsync("End", end);
        await SubmitAsync("Add", name);
    }
}
