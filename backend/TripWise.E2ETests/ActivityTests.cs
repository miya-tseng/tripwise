using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class ActivityTests : SchedulePageTest
{
    [Test]
    public async Task OpenTripFromList_ShowsEveryDayOfTheTrip()
    {
        await Page.GotoAsync("/trips");
        await Page.GetByTestId("trip-search").FillAsync(TripName);

        await Page.GetByTestId("trip-row-link").Filter(new() { HasText = TripName }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"/trips/{TripId}$"));
        await Expect(Page.GetByTestId("schedule-day")).ToHaveCountAsync(3);
        await Expect(ScheduleDay(1)).ToContainTextAsync("Nothing planned yet.");
    }

    [Test]
    public async Task AddActivities_ShowsThemSortedByStartTime()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await AddActivityAsync("Tsukiji Market", day: 1, "07:30", "08:30");

        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Tsukiji Market", "Senso-ji"]);
        await Expect(ScheduleDay(1).GetByTestId("activity-time"))
            .ToHaveTextAsync(["07:30 – 08:30", "10:00 – 11:00"]);
        await Expect(Page.GetByTestId("activity-count")).ToHaveTextAsync("2");
    }

    [Test]
    public async Task AddActivity_OnAnotherDay_ShowsItUnderThatDay()
    {
        await AddActivityAsync("teamLab Planets", day: 3, "13:00", "15:00");

        await Expect(ActivityNames(day: 3)).ToHaveTextAsync(["teamLab Planets"]);
        await Expect(ActivityNames(day: 1)).ToHaveCountAsync(0);
    }

    [Test]
    public async Task AddOverlappingActivity_ShowsErrorAndKeepsSchedule()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");

        await AddActivityAsync("Ueno Park", day: 1, "10:30", "11:30");

        await Expect(Page.GetByTestId("schedule-error"))
            .ToContainTextAsync("The activity overlaps with \"Senso-ji\" (10:00-11:00)");
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Senso-ji"]);

        await Page.ReloadAsync();
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Senso-ji"]);
    }

    [Test]
    public async Task AddBackToBackActivity_IsAllowed()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");

        await AddActivityAsync("Ueno Park", day: 1, "11:00", "12:00");

        await Expect(Page.GetByTestId("schedule-error")).ToHaveCountAsync(0);
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Senso-ji", "Ueno Park"]);
    }

    [Test]
    public async Task DismissOverlapError_HidesIt()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await AddActivityAsync("Ueno Park", day: 1, "10:30", "11:30");
        await Expect(Page.GetByTestId("schedule-error")).ToBeVisibleAsync();

        await Page.GetByTestId("dismiss-error").ClickAsync();

        await Expect(Page.GetByTestId("schedule-error")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditActivity_IntoAnOverlap_ShowsErrorAndKeepsOriginal()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await AddActivityAsync("Ueno Park", day: 1, "13:00", "14:00");

        await ActivityItem("Ueno Park").GetByTestId("edit-activity").ClickAsync();
        await PickTimeAsync("Start", "10:30");
        await PickTimeAsync("End", "11:30");
        await SubmitButton("Save").ClickAsync();

        await Expect(Page.GetByTestId("schedule-error")).ToContainTextAsync("overlaps with \"Senso-ji\"");
        await Page.ReloadAsync();
        await Expect(ActivityItem("Ueno Park").GetByTestId("activity-time")).ToHaveTextAsync("13:00 – 14:00");
    }

    [Test]
    public async Task EditActivity_MovesItToTheNewDayAndTime()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");

        await ActivityItem("Senso-ji").GetByTestId("edit-activity").ClickAsync();
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Activity" }).FillAsync("Tokyo Tower");
        await ChooseDayAsync(2);
        await PickTimeAsync("Start", "18:00");
        await PickTimeAsync("End", "19:30");
        await SubmitButton("Save").ClickAsync();

        await Expect(ActivityNames(day: 2)).ToHaveTextAsync(["Tokyo Tower"]);
        await Expect(ActivityNames(day: 1)).ToHaveCountAsync(0);
        await Expect(Dialog()).ToBeHiddenAsync();
    }

    [Test]
    public async Task DeleteActivity_RemovesItAfterConfirmation()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");

        await ActivityItem("Senso-ji").GetByTestId("delete-activity").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete activity?" });
        await dialog.GetByTestId("confirm-button").ClickAsync();

        await Expect(ActivityNames(day: 1)).ToHaveCountAsync(0);
        await Page.ReloadAsync();
        await Expect(ScheduleDay(1)).ToContainTextAsync("Nothing planned yet.");
    }
}
