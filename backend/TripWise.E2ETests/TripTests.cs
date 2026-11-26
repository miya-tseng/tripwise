using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Microsoft.Playwright.NUnit;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class TripTests : PageTest
{
    private const int MaxTripDays = 30;

    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("E2E_BASE_URL") ?? "http://localhost:5173";

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.Today);

    private static readonly DateOnly FirstDayOfNextMonth =
        new DateOnly(Today.Year, Today.Month, 1).AddMonths(1);

    private readonly List<string> _createdTripNames = [];

    public override BrowserNewContextOptions ContextOptions() => new() { BaseURL = BaseUrl };

    [SetUp]
    public async Task OpenHomePage()
    {
        await DevToolsOverlay.HideAsync(Context);
        await Page.GotoAsync("/");
        await Expect(Page.GetByTestId("app-bar")).ToBeVisibleAsync();
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
    public async Task CreateTrip_ShowsTripUnderUpcoming()
    {
        var name = UniqueTripName();
        var start = FirstDayOfNextMonth.AddDays(9);

        await OpenNewTripDialogAsync();
        await NameInput().FillAsync(name);
        await SelectDatesAsync(start, start.AddDays(2));
        await CreateAsync(name);

        var row = await FindInAllTripsAsync(name);
        await Expect(row.GetByTestId("trip-row-status")).ToHaveTextAsync(new Regex("^In "));
    }

    [Test]
    public async Task CreateTrip_FromADestination_FillsTheNameAndShowsThePlace()
    {
        await RequireFakePlacesAsync();
        var start = FirstDayOfNextMonth.AddDays(3);

        await OpenNewTripDialogAsync();
        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Where to?" }).FillAsync("Osa");
        await Page.GetByRole(AriaRole.Option).Filter(new() { HasText = "Osaka" }).ClickAsync();
        await Expect(NameInput()).ToHaveValueAsync("Osaka");
        await Expect(NewTripDialog().GetByTestId("name-hint")).ToHaveTextAsync("From the destination");

        var name = UniqueTripName();
        await NameInput().FillAsync(name);
        await SelectDatesAsync(start, start.AddDays(1));
        await CreateAsync(name);

        var row = await FindInAllTripsAsync(name);
        await Expect(row.GetByTestId("trip-row-meta")).ToContainTextAsync("Osaka");
        await Expect(row).ToContainTextAsync("🇯🇵");
    }

    [Test]
    public async Task OngoingTrip_IsShownFirstAndOpensToday()
    {
        var name = await CreateTripThroughApiAsync(Today.AddDays(-1), Today.AddDays(1));
        await Page.ReloadAsync();

        var trip = Page.GetByTestId("ongoing-section").GetByTestId("trip-item").Filter(new() { HasText = name });
        await Expect(trip.GetByTestId("trip-progress")).ToHaveTextAsync("Day 2 of 3");

        await trip.GetByTestId("open-today").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"/trips/[^/?]+\?day=2$"));
        await Expect(Page.GetByTestId("schedule-day").Nth(1)).ToBeInViewportAsync();
        await Expect(Page.GetByTestId("schedule-day").Nth(1).GetByTestId("today-badge")).ToHaveTextAsync("Today");
        await Expect(Page.GetByTestId("today-badge")).ToHaveCountAsync(1);
    }

    [Test]
    public async Task PastTrips_AreOnTheAllTripsPage()
    {
        var name = await CreateTripThroughApiAsync(Today.AddYears(-1), Today.AddYears(-1).AddDays(2));
        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("trip-item").Filter(new() { HasText = name })).ToHaveCountAsync(0);

        await Page.GetByTestId("all-trips-link").ClickAsync();
        await Page.GetByTestId("trip-search").FillAsync(name);
        await Expect(Page.GetByTestId("no-trips")).ToBeVisibleAsync();
        await Page.GetByTestId("tab-past").ClickAsync();

        var row = Page.GetByTestId("trip-row").Filter(new() { HasText = name });
        await Expect(row.GetByTestId("trip-row-status")).ToHaveTextAsync("3 days");
    }

    [Test]
    public async Task Home_ShowsOnlyTheNextThreeTrips()
    {
        for (var index = 0; index < 4; index++)
        {
            await CreateTripThroughApiAsync(Today.AddDays(1 + index), Today.AddDays(1 + index));
        }
        await Page.ReloadAsync();

        await Expect(UpcomingSection().GetByTestId("trip-item")).ToHaveCountAsync(3);
        await Expect(UpcomingSection().GetByTestId("see-all-upcoming")).ToBeVisibleAsync();

        await UpcomingSection().GetByTestId("see-all-upcoming").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "All trips", Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task DeleteTrip_FromTheMenu_RemovesIt()
    {
        var name = await CreateTripThroughApiAsync(FirstDayOfNextMonth.AddDays(4), FirstDayOfNextMonth.AddDays(4));
        var trip = await FindInAllTripsAsync(name);

        await trip.GetByTestId("trip-menu").ClickAsync();
        await trip.GetByRole(AriaRole.Menuitem, new() { Name = "Delete trip" }).ClickAsync();
        var dialog = Page.GetByRole(AriaRole.Alertdialog, new() { Name = "Delete trip?" });
        await Expect(dialog).ToContainTextAsync(name);
        await dialog.GetByTestId("confirm-button").ClickAsync();

        await Expect(dialog).ToBeHiddenAsync();
        await Expect(trip).ToHaveCountAsync(0);
    }

    [Test]
    public async Task CancelDelete_KeepsTheTrip()
    {
        var name = await CreateTripThroughApiAsync(FirstDayOfNextMonth.AddDays(14), FirstDayOfNextMonth.AddDays(15));
        var trip = await FindInAllTripsAsync(name);

        await trip.GetByTestId("trip-menu").ClickAsync();
        await trip.GetByTestId("delete-button").ClickAsync();
        await Page.GetByTestId("cancel-button").ClickAsync();

        await Expect(Page.GetByRole(AriaRole.Alertdialog)).ToBeHiddenAsync();
        await Expect(trip).ToBeVisibleAsync();
    }

    [Test]
    public async Task SelectDates_LongerThanMaxTripDays_IsNotAllowed()
    {
        var start = FirstDayOfNextMonth;
        var lastAllowedDay = start.AddDays(MaxTripDays - 1);
        var firstBlockedDay = start.AddDays(MaxTripDays);

        await OpenNewTripDialogAsync();
        await OpenDatePickerAtAsync(start);
        await DayCell(start).ClickAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();

        await Expect(DayCell(firstBlockedDay)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(DayCell(lastAllowedDay)).Not.ToHaveAttributeAsync("aria-disabled", "true");
    }

    [Test]
    public async Task CreateButton_WithoutDates_IsDisabled()
    {
        await OpenNewTripDialogAsync();
        await NameInput().FillAsync(UniqueTripName());

        await Expect(NewTripDialog().GetByTestId("create-trip")).ToBeDisabledAsync();
    }

    private ILocator NewTripDialog() => Page.GetByTestId("new-trip-dialog");

    private ILocator NameInput() => NewTripDialog().GetByRole(AriaRole.Textbox, new() { Name = "Name" });

    private ILocator UpcomingSection() => Page.GetByTestId("upcoming-section");

    private async Task<ILocator> FindInAllTripsAsync(string name)
    {
        await Page.GotoAsync("/trips");
        await Page.GetByTestId("trip-search").FillAsync(name);
        var row = Page.GetByTestId("trip-row").Filter(new() { HasText = name });
        await Expect(row).ToHaveCountAsync(1);
        return row;
    }


    private ILocator DayCell(DateOnly date) =>
        Page.Locator($"[data-test-id='dp-{date:yyyy-MM-dd}']");

    private async Task OpenNewTripDialogAsync()
    {
        await Page.GetByTestId("new-trip").ClickAsync();
        await Expect(NewTripDialog()).ToBeVisibleAsync();
    }

    private async Task OpenDatePickerAtAsync(DateOnly date)
    {
        await NewTripDialog().Locator("input[name='dates']").ClickAsync();
        var monthsAhead = (date.Year - Today.Year) * 12 + date.Month - Today.Month;

        for (var month = 0; month < monthsAhead; month++)
        {
            await Page.GetByRole(AriaRole.Button, new() { Name = "Next month" }).ClickAsync();
        }
    }

    private async Task SelectDatesAsync(DateOnly start, DateOnly end)
    {
        await OpenDatePickerAtAsync(start);
        await DayCell(start).ClickAsync();
        await DayCell(end).ClickAsync();
        await Expect(NewTripDialog().GetByTestId("dates-hint")).ToHaveTextAsync(DaysText(start, end));
    }

    private async Task CreateAsync(string name)
    {
        _createdTripNames.Add(name);
        await NewTripDialog().GetByTestId("create-trip").ClickAsync();
        await Expect(NewTripDialog()).ToBeHiddenAsync();
    }

    private async Task<string> CreateTripThroughApiAsync(DateOnly start, DateOnly end)
    {
        var name = UniqueTripName();
        _createdTripNames.Add(name);

        var response = await Page.APIRequest.PostAsync("/api/trips", new()
        {
            DataObject = new
            {
                name,
                startDate = start.ToString("yyyy-MM-dd"),
                endDate = end.ToString("yyyy-MM-dd"),
            },
        });
        Assert.That(response.Ok, Is.True, await response.TextAsync());
        return name;
    }

    private async Task RequireFakePlacesAsync()
    {
        var response = await Page.APIRequest.GetAsync("/api/places/autocomplete?query=Osaka&kind=destination");
        var suggestions = (await response.JsonAsync())!.Value;
        var firstPlaceId = suggestions.GetArrayLength() > 0
            ? suggestions[0].GetProperty("placeId").GetString()
            : null;

        if (firstPlaceId?.StartsWith("fake-", StringComparison.Ordinal) != true)
        {
            Assert.Ignore("Place E2E tests need fake places. Start the API with GooglePlaces__Provider=Fake.");
        }
    }

    private static string DaysText(DateOnly start, DateOnly end)
    {
        var days = end.DayNumber - start.DayNumber + 1;
        return days == 1 ? "1 day" : $"{days} days";
    }

    private static string UniqueTripName() => $"E2E {Guid.NewGuid():N}"[..16];

    [Test]
    public async Task AppBar_StaysAtTheTopWhileScrolling()
    {
        for (var index = 0; index < 4; index++)
        {
            await CreateTripThroughApiAsync(FirstDayOfNextMonth.AddDays(index * 3), FirstDayOfNextMonth.AddDays(index * 3 + 1));
        }
        await Page.SetViewportSizeAsync(1280, 600);
        await Page.ReloadAsync();

        await Page.Mouse.WheelAsync(0, 1500);

        var bar = Page.GetByTestId("app-bar");
        await Expect(bar).ToBeInViewportAsync();
        Assert.That((await bar.BoundingBoxAsync())!.Y, Is.EqualTo(0).Within(1));
        await Expect(bar.GetByTestId("new-trip")).ToBeVisibleAsync();
    }
}
