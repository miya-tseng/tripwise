using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class TripTests : PageTest
{
    private const int MaxTripDays = 30;

    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    private static readonly DateOnly FirstDayOfMonth =
        new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private readonly List<string> _createdTripNames = [];

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [SetUp]
    public async Task OpenHomePage()
    {
        await Page.GotoAsync("/");
    }

    [TearDown]
    public async Task DeleteCreatedTrips()
    {
        if (_createdTripNames.Count == 0)
        {
            return;
        }

        var response = await Page.APIRequest.GetAsync("/api/trips");
        var trips = await response.JsonAsync();
        foreach (var trip in trips!.Value.EnumerateArray())
        {
            if (_createdTripNames.Contains(trip.GetProperty("name").GetString()!))
            {
                await Page.APIRequest.DeleteAsync($"/api/trips/{trip.GetProperty("id").GetString()}");
            }
        }
    }

    [Test]
    public async Task CreateTrip_ShowsTripInList()
    {
        var name = UniqueTripName();
        var start = FirstDayOfMonth.AddDays(9);

        await CreateTripAsync(name, start, start.AddDays(2));

        var trip = TripItem(name);
        await Expect(trip).ToBeVisibleAsync();
        await Expect(trip).ToContainTextAsync("3 days");
    }

    [Test]
    public async Task DeleteTrip_RemovesTripFromList()
    {
        var name = UniqueTripName();
        var start = FirstDayOfMonth.AddDays(4);
        await CreateTripAsync(name, start, start);
        var trip = TripItem(name);
        await Expect(trip).ToBeVisibleAsync();

        await trip.GetByTestId("delete-button").ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete trip?" });
        await Expect(dialog).ToContainTextAsync(name);
        await dialog.GetByTestId("confirm-button").ClickAsync();

        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trip).ToHaveCountAsync(0);
    }

    [Test]
    public async Task CancelDelete_KeepsTripInList()
    {
        var name = UniqueTripName();
        var start = FirstDayOfMonth.AddDays(14);
        await CreateTripAsync(name, start, start.AddDays(1));
        var trip = TripItem(name);

        await trip.GetByTestId("delete-button").ClickAsync();
        await Page.GetByTestId("cancel-button").ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Alertdialog)).ToBeHiddenAsync();
        await Expect(trip).ToBeVisibleAsync();
    }

    [Test]
    public async Task SelectDates_LongerThanMaxTripDays_IsNotAllowed()
    {
        var start = FirstDayOfMonth;
        var lastAllowedDay = start.AddDays(MaxTripDays - 1);
        var firstBlockedDay = start.AddDays(MaxTripDays);

        await Page.Locator("input[name='dates']").ClickAsync();
        await DayCell(start).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();

        await Expect(DayCell(firstBlockedDay)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(DayCell(lastAllowedDay)).Not.ToHaveAttributeAsync("aria-disabled", "true");
    }

    [Test]
    public async Task CreateButton_WithoutDates_IsDisabled()
    {
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync(UniqueTripName());

        await Expect(CreateButton()).ToBeDisabledAsync();
    }

    private async Task CreateTripAsync(string name, DateOnly start, DateOnly end)
    {
        await Page.GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync(name);
        await Page.Locator("input[name='dates']").ClickAsync();
        await DayCell(start).ClickAsync();
        await DayCell(end).ClickAsync();
        await Expect(Page.GetByTestId("dates-hint")).ToHaveTextAsync(DaysText(start, end));
        await CreateButton().ClickAsync();
        _createdTripNames.Add(name);
    }

    private ILocator CreateButton() => Page.GetByRole(AriaRole.Button, new() { Name = "Create", Exact = true });

    private ILocator DayCell(DateOnly date) =>
        Page.Locator($"[data-test-id='dp-{date:yyyy-MM-dd}']");

    private ILocator TripItem(string name) =>
        Page.GetByTestId("trip-item").Filter(new() { HasText = name });

    private static string DaysText(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        return days == 1 ? "1 day" : $"{days} days";
    }

    private static string UniqueTripName() => $"E2E {Guid.NewGuid():N}"[..16];
}
