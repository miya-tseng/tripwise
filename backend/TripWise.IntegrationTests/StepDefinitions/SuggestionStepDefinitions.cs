using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class SuggestionStepDefinitions(
    HttpClient client,
    ApiResponse response,
    ScenarioTrip existing,
    TripWiseApiFactory factory)
{
    [Given("suggestions are unavailable")]
    public void GivenSuggestionsAreUnavailable()
    {
        factory.Suggestions.Available = false;
    }

    [When("I get suggestions for {string}")]
    public async Task WhenIGetSuggestionsFor(string activityName)
    {
        var schedule = (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule"))!;
        var activity = schedule.Days.SelectMany(day => day.Activities).Single(item => item.Name == activityName);

        response.Message = await client.GetAsync($"/api/trips/{existing.Trip.Id}/activities/{activity.Id}/suggestions");
    }

    [When("I get suggestions for an unknown activity")]
    public async Task WhenIGetSuggestionsForAnUnknownActivity()
    {
        response.Message = await client.GetAsync($"/api/trips/{existing.Trip.Id}/activities/{Guid.NewGuid()}/suggestions");
    }

    [When("I get suggestions for an unknown trip")]
    public async Task WhenIGetSuggestionsForAnUnknownTrip()
    {
        response.Message = await client.GetAsync($"/api/trips/{Guid.NewGuid()}/activities/{Guid.NewGuid()}/suggestions");
    }

    [Then("the suggested places are {string}")]
    public async Task ThenTheSuggestedPlacesAre(string names)
    {
        var suggestions = await ReadSuggestionsAsync();

        Assert.That(
            suggestions.Select(suggestion => suggestion.Place.Name),
            Is.EqualTo(names.Split(',', StringSplitOptions.TrimEntries)));
    }

    [Then("every suggestion has a reason, a duration and a Google Maps link")]
    public async Task ThenEverySuggestionHasDetails()
    {
        var suggestions = await ReadSuggestionsAsync();

        Assert.Multiple(() =>
        {
            Assert.That(suggestions, Is.Not.Empty);
            Assert.That(suggestions.Select(suggestion => suggestion.Reason), Is.All.Not.Empty);
            Assert.That(suggestions.Select(suggestion => suggestion.SuggestedMinutes), Is.All.InRange(15, 240));
            Assert.That(suggestions.Select(suggestion => suggestion.Place.GoogleMapsUri), Is.All.StartsWith("https://"));
            Assert.That(suggestions.Select(suggestion => suggestion.Place.PlaceId), Is.Unique);
        });
    }

    [Then("the suggestions were asked for {string} ending at {TimeOnly} with {string} planned")]
    public void ThenTheSuggestionsWereAskedFor(string placeName, TimeOnly endsAt, string planned)
    {
        var request = factory.Suggestions.Requests.Single();

        Assert.Multiple(() =>
        {
            Assert.That(request.PlaceName, Is.EqualTo(placeName));
            Assert.That(request.EndsAt, Is.EqualTo(endsAt));
            Assert.That(request.PlannedThatDay, Is.EqualTo(planned.Split(',', StringSplitOptions.TrimEntries)));
        });
    }

    private List<SuggestionDto>? _suggestions;

    private async Task<List<SuggestionDto>> ReadSuggestionsAsync() =>
        _suggestions ??= (await response.Message.Content.ReadFromJsonAsync<List<SuggestionDto>>())!;
}
