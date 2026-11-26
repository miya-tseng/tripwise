using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class SuggestionTests : SchedulePageTest
{
    private const string FakeReasonEnding = "很多旅客會排在同一天。";

    [SetUp]
    public Task RequireFakePlaces() => RequireFakePlacesAsync();

    [Test]
    public async Task OpenIdeas_ShowsRealPlacesWithReasons()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 1, "10:00", "11:30");

        await OpenIdeasAsync("Senso-ji");

        await Expect(Panel().GetByTestId("suggestion-name")).ToHaveTextAsync(
            ["Tokyo Skytree", "Shibuya Sky", "Tsukiji Outer Market", "Ueno Park", "teamLab Planets TOKYO"]);
        await Expect(Panel().GetByRole(AriaRole.Heading)).ToHaveTextAsync("Near Senso-ji");
        await Expect(Suggestion("Tokyo Skytree").GetByTestId("suggestion-reason")).ToContainTextAsync("Senso-ji");
        await Expect(Suggestion("Tokyo Skytree").GetByTestId("suggestion-duration")).ToHaveTextAsync("~1 h");
        await Expect(Suggestion("Tokyo Skytree").GetByTestId("suggestion-map-link"))
            .ToHaveAttributeAsync("href", "https://www.google.com/maps/search/?api=1&query=35.710063,139.8107");
    }

    [Test]
    public async Task AddIdea_FillsTheFormRightAfterTheActivityAndAddsIt()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 2, "10:00", "11:30");
        await OpenIdeasAsync("Senso-ji");

        await Suggestion("Tokyo Skytree").GetByTestId("add-suggestion").ClickAsync();

        await Expect(Page.GetByTestId("selected-place-name")).ToHaveTextAsync("Tokyo Skytree");
        await Expect(Page.GetByTestId("day-select").GetByTestId("select-trigger")).ToContainTextAsync("Day 2");
        await Expect(TimeTrigger("Start")).ToHaveAccessibleNameAsync("Start: 11:30");
        await Expect(TimeTrigger("End")).ToHaveAccessibleNameAsync("End: 12:30");

        await SubmitButton("Add").ClickAsync();

        await Expect(ActivityNames(day: 2)).ToHaveTextAsync(["Senso-ji", "Tokyo Skytree"]);
        await Expect(ActivityItem("Tokyo Skytree").GetByTestId("activity-address"))
            .ToHaveTextAsync("1 Chome-1-2 Oshiage, Sumida City, Tokyo, Japan");
        await Expect(Suggestion("Tokyo Skytree")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task Ideas_ExcludePlacesAlreadyInTheTrip()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 1, "10:00", "11:30");
        await AddPlaceAsync("skytree", "Tokyo Skytree", day: 3, "10:00", "11:00");

        await OpenIdeasAsync("Senso-ji");

        await Expect(Panel().GetByTestId("suggestion-name")).ToHaveTextAsync(
            ["Shibuya Sky", "Tsukiji Outer Market", "Ueno Park", "teamLab Planets TOKYO", "Meiji Jingu"]);
    }

    [Test]
    public async Task CustomActivity_HasNoIdeasButton()
    {
        await AddActivityAsync("Free time", day: 1, "15:00", "16:00");

        await Expect(ActivityItem("Free time").GetByTestId("suggest-activity")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task CloseIdeas_HidesThePanel()
    {
        await AddPlaceAsync("senso", "Senso-ji", day: 1, "10:00", "11:30");
        await OpenIdeasAsync("Senso-ji");

        await Page.GetByTestId("close-suggestions").ClickAsync();

        await Expect(Panel()).ToHaveCountAsync(0);
    }

    private async Task OpenIdeasAsync(string activityName)
    {
        await ActivityItem(activityName).GetByTestId("suggest-activity").ClickAsync();
        await Expect(Panel().GetByTestId("suggestion").First).ToBeVisibleAsync();

        var reason = await Panel().GetByTestId("suggestion-reason").First.InnerTextAsync();

        if (!reason.EndsWith(FakeReasonEnding, StringComparison.Ordinal))
        {
            Assert.Ignore("Suggestion E2E tests need fake suggestions. Start the API with Gemini__Provider=Fake.");
        }
    }

    private ILocator Panel() => Page.GetByTestId("suggestions-panel");

    private ILocator Suggestion(string name) =>
        Panel().GetByTestId("suggestion").Filter(new() { HasText = name });

    private ILocator TimeTrigger(string label) =>
        Page.GetByRole(AriaRole.Button, new() { NameRegex = new System.Text.RegularExpressions.Regex($"^{label}:") });
}
