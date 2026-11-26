using System.Net;
using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class TripStepDefinitions(HttpClient client, ApiResponse response, ScenarioTrip existing)
{
    private TripDto? _responseTrip;

    [Given("a trip {string} from {DateOnly} to {DateOnly} exists")]
    public async Task GivenATripExists(string name, DateOnly start, DateOnly end)
    {
        var created = await client.PostAsJsonAsync("/api/trips", NewTrip(name, start, end));
        created.EnsureSuccessStatusCode();
        existing.Trip = (await created.Content.ReadFromJsonAsync<TripDto>())!;
    }

    [When("I create a trip {string} from {DateOnly} to {DateOnly}")]
    public async Task WhenICreateATrip(string name, DateOnly start, DateOnly end)
    {
        response.Message = await client.PostAsJsonAsync("/api/trips", NewTrip(name, start, end));
    }

    [When("I create a trip {string} from {DateOnly} to {DateOnly} with the destination {string}")]
    public async Task WhenICreateATripWithADestination(string name, DateOnly start, DateOnly end, string placeId)
    {
        var trip = NewTrip(name, start, end);
        trip.DestinationPlaceId = placeId;
        response.Message = await client.PostAsJsonAsync("/api/trips", trip);

        if (response.Message.IsSuccessStatusCode)
        {
            existing.Trip = await ReadResponseTripAsync();
        }
    }

    [When("I update the trip to {string} from {DateOnly} to {DateOnly} with the destination {string}")]
    public async Task WhenIUpdateTheTripWithADestination(string name, DateOnly start, DateOnly end, string placeId)
    {
        var trip = UpdatedTrip(name, start, end);
        trip.DestinationPlaceId = placeId;
        response.Message = await client.PutAsJsonAsync($"/api/trips/{existing.Trip.Id}", trip);
        response.Message.EnsureSuccessStatusCode();
    }

    [Then("the response trip destination is {string} in {string}")]
    public async Task ThenTheResponseTripDestinationIs(string name, string address)
    {
        var trip = await ReadResponseTripAsync();

        Assert.Multiple(() =>
        {
            Assert.That(trip.Destination!.Name, Is.EqualTo(name));
            Assert.That(trip.Destination.Address, Is.EqualTo(address));
        });
    }

    [Then("the stored trip destination is {string}")]
    public async Task ThenTheStoredTripDestinationIs(string name)
    {
        var trip = (await client.GetFromJsonAsync<TripDto>($"/api/trips/{existing.Trip.Id}"))!;

        Assert.That(trip.Destination?.Name, Is.EqualTo(name));
    }

    [Then("the stored trip has no destination")]
    public async Task ThenTheStoredTripHasNoDestination()
    {
        var trip = (await client.GetFromJsonAsync<TripDto>($"/api/trips/{existing.Trip.Id}"))!;

        Assert.That(trip.Destination, Is.Null);
    }

    [When("I create a trip without a name from {DateOnly} to {DateOnly}")]
    public async Task WhenICreateATripWithoutAName(DateOnly start, DateOnly end)
    {
        response.Message = await client.PostAsJsonAsync("/api/trips", new { StartDate = start, EndDate = end });
    }

    [When("I get the trip")]
    public async Task WhenIGetTheTrip()
    {
        response.Message = await client.GetAsync($"/api/trips/{existing.Trip.Id}");
    }

    [When("I get an unknown trip")]
    public async Task WhenIGetAnUnknownTrip()
    {
        response.Message = await client.GetAsync($"/api/trips/{Guid.NewGuid()}");
    }

    [When("I list all trips")]
    public async Task WhenIListAllTrips()
    {
        response.Message = await client.GetAsync("/api/trips");
    }

    [When("I update the trip to {string} from {DateOnly} to {DateOnly}")]
    public async Task WhenIUpdateTheTrip(string name, DateOnly start, DateOnly end)
    {
        response.Message = await client.PutAsJsonAsync($"/api/trips/{existing.Trip.Id}", UpdatedTrip(name, start, end));
    }

    [When("I update an unknown trip to {string} from {DateOnly} to {DateOnly}")]
    public async Task WhenIUpdateAnUnknownTrip(string name, DateOnly start, DateOnly end)
    {
        response.Message = await client.PutAsJsonAsync($"/api/trips/{Guid.NewGuid()}", UpdatedTrip(name, start, end));
    }

    [Then("the stored trip is {string} from {DateOnly} to {DateOnly}")]
    public async Task ThenTheStoredTripIs(string name, DateOnly start, DateOnly end)
    {
        var trip = (await client.GetFromJsonAsync<TripDto>($"/api/trips/{existing.Trip.Id}"))!;

        Assert.Multiple(() =>
        {
            Assert.That(trip.Name, Is.EqualTo(name));
            Assert.That(trip.StartDate, Is.EqualTo(start));
            Assert.That(trip.EndDate, Is.EqualTo(end));
        });
    }

    [When("I delete the trip")]
    public async Task WhenIDeleteTheTrip()
    {
        response.Message = await client.DeleteAsync($"/api/trips/{existing.Trip.Id}");
    }

    [When("I delete an unknown trip")]
    public async Task WhenIDeleteAnUnknownTrip()
    {
        response.Message = await client.DeleteAsync($"/api/trips/{Guid.NewGuid()}");
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

        Assert.That(response.Message.Headers.Location!.ToString(), Does.EndWith($"/api/trips/{trip.Id}"));
    }

    [Then("the list contains the trip")]
    public async Task ThenTheListContainsTheTrip()
    {
        var trips = await response.Message.Content.ReadFromJsonAsync<List<TripDto>>();

        Assert.That(trips!.Select(trip => trip.Id), Does.Contain(existing.Trip.Id));
    }

    [Then("the trip no longer exists")]
    public async Task ThenTheTripNoLongerExists()
    {
        var found = await client.GetAsync($"/api/trips/{existing.Trip.Id}");

        Assert.That(found.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    private async Task<TripDto> ReadResponseTripAsync()
    {
        _responseTrip ??= await response.Message.Content.ReadFromJsonAsync<TripDto>();
        return _responseTrip!;
    }

    private static UpdateTripDto UpdatedTrip(string name, DateOnly start, DateOnly end)
    {
        return new UpdateTripDto
        {
            Name = name,
            StartDate = start,
            EndDate = end,
        };
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
