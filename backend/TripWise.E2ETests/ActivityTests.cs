using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class ActivityTests : PageTest
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    private static readonly DateOnly FirstDay = new(2030, 5, 1);

    private string _tripName = null!;
    private string _tripId = null!;

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [SetUp]
    public async Task CreateTripAndOpenSchedule()
    {
        _tripName = $"E2E {Guid.NewGuid():N}"[..16];

        var response = await Page.APIRequest.PostAsync("/api/trips", new()
        {
            DataObject = new
            {
                name = _tripName,
                startDate = FirstDay.ToString("yyyy-MM-dd"),
                endDate = FirstDay.AddDays(2).ToString("yyyy-MM-dd"),
            },
        });
        Assert.That(response.Ok, Is.True, await response.TextAsync());
        var trip = await response.JsonAsync();
        _tripId = trip!.Value.GetProperty("id").GetString()!;

        await Page.GotoAsync($"/trips/{_tripId}");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = _tripName })).ToBeVisibleAsync();
    }

    [TearDown]
    public async Task DeleteTrip()
    {
        await Page.APIRequest.DeleteAsync($"/api/trips/{_tripId}");
    }

    [Test]
    public async Task OpenTripFromList_ShowsEveryDayOfTheTrip()
    {
        await Page.GotoAsync("/");

        await Page.GetByTestId("trip-item").Filter(new() { HasText = _tripName }).ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex($"/trips/{_tripId}$"));
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
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Add activity" })).ToBeVisibleAsync();
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

    private async Task AddActivityAsync(string name, int day, string start, string end)
    {
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Activity" }).FillAsync(name);
        await ChooseDayAsync(day);
        await PickTimeAsync("Start", start);
        await PickTimeAsync("End", end);
        await SubmitButton("Add").ClickAsync();

        var added = ActivityItem(name);
        var rejected = Page.GetByTestId("schedule-error");
        await Expect(added.Or(rejected)).ToBeVisibleAsync();
    }

    private async Task ChooseDayAsync(int day)
    {
        await Page.GetByTestId("select-trigger").ClickAsync();
        await Page.GetByRole(AriaRole.Option).Nth(day - 1).ClickAsync();
        await Expect(Page.GetByTestId("select-trigger")).ToContainTextAsync($"Day {day}");
    }

    private async Task PickTimeAsync(string label, string time)
    {
        var parts = time.Split(':');
        var trigger = Page.GetByRole(AriaRole.Button, new() { NameRegex = new Regex($"^{label}:") });
        await trigger.ClickAsync();

        var panel = Page.GetByRole(AriaRole.Dialog, new() { Name = $"Choose {label.ToLowerInvariant()} time" });
        await panel.GetByRole(AriaRole.Group, new() { Name = "Hour" })
            .GetByRole(AriaRole.Button, new() { Name = parts[0], Exact = true }).ClickAsync();
        await panel.GetByRole(AriaRole.Group, new() { Name = "Minute" })
            .GetByRole(AriaRole.Button, new() { Name = parts[1], Exact = true }).ClickAsync();

        await Expect(trigger).ToHaveAccessibleNameAsync($"{label}: {time}");
    }

    private ILocator SubmitButton(string name) =>
        Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    private ILocator ScheduleDay(int day) => Page.GetByTestId("schedule-day").Nth(day - 1);

    private ILocator ActivityNames(int day) => ScheduleDay(day).GetByTestId("activity-name");

    private ILocator ActivityItem(string name) =>
        Page.GetByTestId("activity-item").Filter(new() { HasText = name });
}
