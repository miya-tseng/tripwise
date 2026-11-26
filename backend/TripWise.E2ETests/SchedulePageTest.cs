using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace TripWise.E2ETests;

public abstract class SchedulePageTest : PageTest
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    protected static readonly DateOnly FirstDay = new(2030, 5, 1);

    protected string TripName { get; private set; } = null!;
    protected string TripId { get; private set; } = null!;

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [SetUp]
    public async Task CreateTripAndOpenSchedule()
    {
        TripName = $"E2E {Guid.NewGuid():N}"[..16];

        var response = await Page.APIRequest.PostAsync("/api/trips", new()
        {
            DataObject = new
            {
                name = TripName,
                startDate = FirstDay.ToString("yyyy-MM-dd"),
                endDate = FirstDay.AddDays(2).ToString("yyyy-MM-dd"),
            },
        });
        Assert.That(response.Ok, Is.True, await response.TextAsync());
        var trip = await response.JsonAsync();
        TripId = trip!.Value.GetProperty("id").GetString()!;

        await Page.GotoAsync($"/trips/{TripId}");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = TripName })).ToBeVisibleAsync();
    }

    [TearDown]
    public async Task DeleteTrip()
    {
        await Page.APIRequest.DeleteAsync($"/api/trips/{TripId}");
    }

    protected async Task AddActivityAsync(string name, int day, string start, string end, string? cost = null)
    {
        await TypeCustomActivityAsync(name);
        await ChooseDayAsync(day);
        await PickTimeAsync("Start", start);
        await PickTimeAsync("End", end);
        if (cost is not null)
        {
            await CostInput().FillAsync(cost);
        }
        await SubmitButton("Add").ClickAsync();

        var added = ActivityItem(name);
        var rejected = Page.GetByTestId("schedule-error");
        await Expect(added.Or(rejected)).ToBeVisibleAsync();
    }

    protected async Task TypeCustomActivityAsync(string name)
    {
        var useCustom = Page.GetByTestId("use-custom-activity");

        if (await useCustom.IsVisibleAsync())
        {
            await useCustom.ClickAsync();
        }

        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Activity" }).FillAsync(name);
    }

    protected async Task ChooseDayAsync(int day)
    {
        var daySelect = Page.GetByTestId("day-select");
        await daySelect.GetByTestId("select-trigger").ClickAsync();
        await daySelect.GetByRole(AriaRole.Option).Nth(day - 1).ClickAsync();
        await Expect(daySelect.GetByTestId("select-trigger")).ToContainTextAsync($"Day {day}");
    }

    protected async Task PickTimeAsync(string label, string time)
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

    protected ILocator CostInput() => Page.GetByRole(AriaRole.Textbox, new() { Name = "Cost" });

    protected ILocator SubmitButton(string name) =>
        Page.GetByRole(AriaRole.Button, new() { Name = name, Exact = true });

    protected ILocator ScheduleDay(int day) => Page.GetByTestId("schedule-day").Nth(day - 1);

    protected ILocator ActivityNames(int day) => ScheduleDay(day).GetByTestId("activity-name");

    protected ILocator ActivityItem(string name) =>
        Page.GetByTestId("activity-item").Filter(new() { HasText = name });
}
