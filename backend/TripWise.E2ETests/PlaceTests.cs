using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class PlaceTests : SchedulePageTest
{
    [SetUp]
    public async Task RequireFakePlaces()
    {
        var response = await Page.APIRequest.GetAsync("/api/places/autocomplete?query=Senso-ji");
        var suggestions = (await response.JsonAsync())!.Value;
        var firstPlaceId = suggestions.GetArrayLength() > 0
            ? suggestions[0].GetProperty("placeId").GetString()
            : null;

        if (firstPlaceId?.StartsWith("fake-", StringComparison.Ordinal) != true)
        {
            Assert.Ignore("Place E2E tests need fake places. Start the API with GooglePlaces__Provider=Fake.");
        }
    }

    [Test]
    public async Task SearchAndAddPlace_ShowsItInTheScheduleWithAddress()
    {
        await SearchPlaceAsync("senso");
        await PlaceOption("Senso-ji").ClickAsync();
        await Expect(Page.GetByTestId("selected-place-name")).ToHaveTextAsync("Senso-ji");

        await PickTimeAsync("Start", "10:00");
        await PickTimeAsync("End", "11:30");
        await SubmitButton("Add").ClickAsync();

        var activity = ActivityItem("Senso-ji");
        await Expect(activity.GetByTestId("activity-address"))
            .ToHaveTextAsync("2 Chome-3-1 Asakusa, Taito City, Tokyo, Japan");
        await Expect(Page.GetByTestId("selected-place")).ToHaveCountAsync(0);

        await Page.ReloadAsync();
        await Expect(ActivityItem("Senso-ji").GetByTestId("activity-address"))
            .ToHaveTextAsync("2 Chome-3-1 Asakusa, Taito City, Tokyo, Japan");
    }

    [Test]
    public async Task SearchPlace_CanBeChosenWithTheKeyboard()
    {
        await SearchPlaceAsync("tokyo");
        await Expect(PlaceOption("Tokyo Skytree")).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Page.GetByTestId("selected-place-name")).ToHaveTextAsync("Tokyo Skytree");
    }

    [Test]
    public async Task SearchPlace_WithNoMatches_SaysNothingWasFound()
    {
        await SearchPlaceAsync("zzzz");

        await Expect(Page.GetByTestId("place-search-empty")).ToHaveTextAsync("No places found.");
    }

    [Test]
    public async Task ChangePlace_BeforeAdding_SearchesAgain()
    {
        await SearchPlaceAsync("senso");
        await PlaceOption("Senso-ji").ClickAsync();

        await Page.GetByTestId("change-place").ClickAsync();
        await SearchPlaceAsync("skytree");
        await PlaceOption("Tokyo Skytree").ClickAsync();

        await Expect(Page.GetByTestId("selected-place-name")).ToHaveTextAsync("Tokyo Skytree");
    }

    [Test]
    public async Task EditPlaceActivity_KeepsThePlace()
    {
        await SearchPlaceAsync("shibuya");
        await PlaceOption("Shibuya Sky").ClickAsync();
        await PickTimeAsync("Start", "17:00");
        await PickTimeAsync("End", "18:00");
        await SubmitButton("Add").ClickAsync();
        await Expect(ActivityItem("Shibuya Sky")).ToBeVisibleAsync();

        await ActivityItem("Shibuya Sky").GetByTestId("edit-activity").ClickAsync();
        await Expect(Page.GetByTestId("selected-place-name")).ToHaveTextAsync("Shibuya Sky");
        await ChooseDayAsync(2);
        await SubmitButton("Save").ClickAsync();

        await Expect(ActivityNames(day: 2)).ToHaveTextAsync(["Shibuya Sky"]);
        await Expect(ActivityItem("Shibuya Sky").GetByTestId("activity-address"))
            .ToHaveTextAsync("2 Chome-24-12 Shibuya, Shibuya City, Tokyo, Japan");
    }

    private async Task SearchPlaceAsync(string query)
    {
        await Page.GetByRole(AriaRole.Combobox, new() { Name = "Activity" }).FillAsync(query);
    }

    private ILocator PlaceOption(string name) =>
        Page.GetByRole(AriaRole.Option).Filter(new() { HasText = name }).First;
}
