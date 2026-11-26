using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class PlaceStepDefinitions(
    HttpClient client,
    ApiResponse response,
    ScenarioTrip existing,
    TripWiseApiFactory factory)
{
    private readonly Dictionary<string, ActivityDto> _activitiesByName = [];

    private string ActivitiesUrl => $"/api/trips/{existing.Trip.Id}/activities";

    [Given("place search is unavailable")]
    public void GivenPlaceSearchIsUnavailable()
    {
        factory.PlaceSearch.Available = false;
    }

    [Given("the trip has the place {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task GivenTheTripHasThePlace(string placeId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var created = await client.PostAsJsonAsync(ActivitiesUrl, PlaceActivity(placeId, date, start, end));
        created.EnsureSuccessStatusCode();
        var activity = (await created.Content.ReadFromJsonAsync<ActivityDto>())!;
        _activitiesByName[activity.Name] = activity;
    }

    [When("I search places for {string}")]
    public async Task WhenISearchPlacesFor(string query)
    {
        response.Message = await client.GetAsync($"/api/places/autocomplete?query={Uri.EscapeDataString(query)}&sessionToken=test-session");
    }

    [When("I search places with {int} characters")]
    public async Task WhenISearchPlacesWithCharacters(int length)
    {
        response.Message = await client.GetAsync($"/api/places/autocomplete?query={new string('a', length)}");
    }

    [When("I get the place {string}")]
    public async Task WhenIGetThePlace(string placeId)
    {
        response.Message = await client.GetAsync($"/api/places/{Uri.EscapeDataString(placeId)}");
    }

    [When("I add the place {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task WhenIAddThePlace(string placeId, DateOnly date, TimeOnly start, TimeOnly end)
    {
        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, PlaceActivity(placeId, date, start, end));
    }

    [When("I move the place activity {string} to {TimeOnly} to {TimeOnly}")]
    public async Task WhenIMoveThePlaceActivity(string name, TimeOnly start, TimeOnly end)
    {
        var activity = _activitiesByName[name];
        var dto = PlaceActivity(activity.Place!.PlaceId, activity.Date, start, end);
        dto.Name = activity.Name;

        response.Message = await client.PutAsJsonAsync($"{ActivitiesUrl}/{activity.Id}", dto);
    }

    [Then("the place suggestions are {string}")]
    public async Task ThenThePlaceSuggestionsAre(string names)
    {
        var suggestions = (await response.Message.Content.ReadFromJsonAsync<List<PlaceSuggestionDto>>())!;

        Assert.Multiple(() =>
        {
            Assert.That(suggestions.Select(suggestion => suggestion.Name), Is.EqualTo(SplitList(names)));
            Assert.That(suggestions.Select(suggestion => suggestion.PlaceId), Is.All.Not.Empty);
            Assert.That(suggestions.Select(suggestion => suggestion.Description), Is.All.Not.Empty);
        });
    }

    [Then("there are no place suggestions")]
    public async Task ThenThereAreNoPlaceSuggestions()
    {
        var suggestions = (await response.Message.Content.ReadFromJsonAsync<List<PlaceSuggestionDto>>())!;

        Assert.That(suggestions, Is.Empty);
    }

    [Then("the place is {string} at {string}")]
    public async Task ThenThePlaceIs(string name, string address)
    {
        var place = (await response.Message.Content.ReadFromJsonAsync<PlaceDto>())!;

        Assert.Multiple(() =>
        {
            Assert.That(place.Name, Is.EqualTo(name));
            Assert.That(place.Address, Is.EqualTo(address));
            Assert.That(place.Latitude, Is.Not.Zero);
            Assert.That(place.Longitude, Is.Not.Zero);
            Assert.That(place.GoogleMapsUri, Does.StartWith("https://"));
        });
    }

    [Then("the response activity is {string} at the place {string}")]
    public async Task ThenTheResponseActivityIsAtThePlace(string name, string placeId)
    {
        var activity = (await response.Message.Content.ReadFromJsonAsync<ActivityDto>())!;

        Assert.Multiple(() =>
        {
            Assert.That(activity.Name, Is.EqualTo(name));
            Assert.That(activity.Place?.PlaceId, Is.EqualTo(placeId));
        });
    }

    [Then("the activity {string} is at the place {string} in the schedule")]
    public async Task ThenTheActivityIsAtThePlaceInTheSchedule(string name, string placeId)
    {
        var activity = await ScheduledActivityAsync(name);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Place?.PlaceId, Is.EqualTo(placeId));
            Assert.That(activity.Place?.GoogleMapsUri, Does.StartWith("https://"));
        });
    }

    [Then("the activity {string} has no place in the schedule")]
    public async Task ThenTheActivityHasNoPlaceInTheSchedule(string name)
    {
        Assert.That((await ScheduledActivityAsync(name)).Place, Is.Null);
    }

    [Then("place details were requested {int} time(s)")]
    public void ThenPlaceDetailsWereRequested(int times)
    {
        Assert.That(factory.PlaceSearch.DetailsRequests, Is.EqualTo(times));
    }

    private async Task<ActivityDto> ScheduledActivityAsync(string name)
    {
        var schedule = (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule"))!;
        return schedule.Days.SelectMany(day => day.Activities).Single(activity => activity.Name == name);
    }

    private static string[] SplitList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static SaveActivityDto PlaceActivity(string placeId, DateOnly date, TimeOnly start, TimeOnly end) => new()
    {
        Name = string.Empty,
        Date = date,
        StartTime = start,
        EndTime = end,
        PlaceId = placeId,
        PlaceSessionToken = "test-session",
    };
}
