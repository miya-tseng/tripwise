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
        await Page.GotoAsync("/");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "TripWise" })).ToBeVisibleAsync();
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

        var trip = UpcomingSection().GetByTestId("trip-item").Filter(new() { HasText = name });
        await Expect(trip).ToBeVisibleAsync();
        await Expect(trip.GetByTestId("trip-countdown")).ToHaveTextAsync(new Regex("^In "));
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

        await Expect(TripItem(name).GetByTestId("trip-destination")).ToHaveTextAsync("Osaka, Japan");
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
    }

    [Test]
    public async Task PastTrips_AreCollapsedUntilOpened()
    {
        await CreateTripThroughApiAsync(FirstDayOfNextMonth.AddDays(1), FirstDayOfNextMonth.AddDays(2));
        var name = await CreateTripThroughApiAsync(Today.AddYears(-1), Today.AddYears(-1).AddDays(2));
        await Page.ReloadAsync();

        var pastTrip = Page.GetByTestId("past-section").GetByTestId("trip-item").Filter(new() { HasText = name });
        await Expect(pastTrip).ToHaveCountAsync(0);

        await Page.GetByTestId("toggle-past").ClickAsync();

        await Expect(pastTrip).ToBeVisibleAsync();
        await Expect(pastTrip).ToContainTextAsync("3 days");
    }

    [Test]
    public async Task DeleteTrip_FromTheMenu_RemovesIt()
    {
        var name = await CreateTripThroughApiAsync(FirstDayOfNextMonth.AddDays(4), FirstDayOfNextMonth.AddDays(4));
        await Page.ReloadAsync();
        var trip = TripItem(name);

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
        await Page.ReloadAsync();
        var trip = TripItem(name);

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

    private ILocator TripItem(string name) =>
        Page.GetByTestId("trip-item").Filter(new() { HasText = name });

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
}
