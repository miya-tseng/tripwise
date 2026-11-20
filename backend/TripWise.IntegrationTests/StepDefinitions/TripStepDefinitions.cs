using System.Net;
using System.Net.Http.Json;
using Reqnroll;
using TripWise.Api.Dtos;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class TripStepDefinitions(HttpClient client)
{
    private HttpResponseMessage _response = null!;
    private TripDto _existingTrip = null!;
    private TripDto? _responseTrip;

    [Given("a trip {string} from {DateOnly} to {DateOnly} exists")]
    public async Task GivenATripExists(string name, DateOnly start, DateOnly end)
    {
        var response = await client.PostAsJsonAsync("/api/trips", NewTrip(name, start, end));
        response.EnsureSuccessStatusCode();
        _existingTrip = (await response.Content.ReadFromJsonAsync<TripDto>())!;
    }

    [When("I create a trip {string} from {DateOnly} to {DateOnly}")]
    public async Task WhenICreateATrip(string name, DateOnly start, DateOnly end)
    {
        _response = await client.PostAsJsonAsync("/api/trips", NewTrip(name, start, end));
    }

    [When("I create a trip without a name from {DateOnly} to {DateOnly}")]
    public async Task WhenICreateATripWithoutAName(DateOnly start, DateOnly end)
    {
        _response = await client.PostAsJsonAsync("/api/trips", new { StartDate = start, EndDate = end });
    }

    [When("I get the trip")]
    public async Task WhenIGetTheTrip()
    {
        _response = await client.GetAsync($"/api/trips/{_existingTrip.Id}");
    }

    [When("I get an unknown trip")]
    public async Task WhenIGetAnUnknownTrip()
    {
        _response = await client.GetAsync($"/api/trips/{Guid.NewGuid()}");
    }

    [When("I list all trips")]
    public async Task WhenIListAllTrips()
    {
        _response = await client.GetAsync("/api/trips");
    }

    [When("I delete the trip")]
    public async Task WhenIDeleteTheTrip()
    {
        _response = await client.DeleteAsync($"/api/trips/{_existingTrip.Id}");
    }

    [When("I delete an unknown trip")]
    public async Task WhenIDeleteAnUnknownTrip()
    {
        _response = await client.DeleteAsync($"/api/trips/{Guid.NewGuid()}");
    }

    [Then("the response status is {int}")]
    public void ThenTheResponseStatusIs(int statusCode)
    {
        Assert.That(_response.StatusCode, Is.EqualTo((HttpStatusCode)statusCode));
    }

    [Then("the response is a trip {string} from {DateOnly} to {DateOnly} with {int} days")]
    public async Task ThenTheResponseIsATrip(string name, DateOnly start, DateOnly end, int days)
    {
        var trip = await ReadResponseTripAsync();

        Assert.Multiple(() =>
        {
            Assert.That(trip.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(trip.Name, Is.EqualTo(name));
            Assert.That(trip.StartDate, Is.EqualTo(start));
            Assert.That(trip.EndDate, Is.EqualTo(end));
            Assert.That(trip.Days, Is.EqualTo(days));
        });
    }

    [Then("the response location points to the trip")]
    public async Task ThenTheResponseLocationPointsToTheTrip()
    {
        var trip = await ReadResponseTripAsync();

        Assert.That(_response.Headers.Location!.ToString(), Does.EndWith($"/api/trips/{trip.Id}"));
    }

    [Then("the error detail is {string}")]
    public async Task ThenTheErrorDetailIs(string detail)
    {
        var body = await _response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain(detail));
    }

    [Then("the list contains the trip")]
    public async Task ThenTheListContainsTheTrip()
    {
        var trips = await _response.Content.ReadFromJsonAsync<List<TripDto>>();

        Assert.That(trips!.Select(trip => trip.Id), Does.Contain(_existingTrip.Id));
    }

    [Then("the trip no longer exists")]
    public async Task ThenTheTripNoLongerExists()
    {
        var response = await client.GetAsync($"/api/trips/{_existingTrip.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private async Task<TripDto> ReadResponseTripAsync()
    {
        _responseTrip ??= await _response.Content.ReadFromJsonAsync<TripDto>();
        return _responseTrip!;
    }

    private static CreateTripDto NewTrip(string name, DateOnly start, DateOnly end)
    {
        return new CreateTripDto
        {
            Name = name,
            StartDate = start,
            EndDate = end,
        };
    }
}
