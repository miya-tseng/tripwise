using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class StayStepDefinitions(HttpClient client, ApiResponse response, ScenarioTrip existing)
{
    private readonly Dictionary<string, StayDto> _staysByName = [];

    private string StaysUrl => $"/api/trips/{existing.Trip.Id}/stays";

    [Given("the trip has a stay {string} from {DateOnly} to {DateOnly} costing {decimal} {word}")]
    public async Task GivenTheTripHasAStay(string name, DateOnly checkIn, DateOnly checkOut, decimal cost, string currency)
    {
        var created = await client.PostAsJsonAsync(StaysUrl, NewStay(name, checkIn, checkOut, cost, currency));
        created.EnsureSuccessStatusCode();
        _staysByName[name] = (await created.Content.ReadFromJsonAsync<StayDto>())!;
    }

    [When("I add a stay {string} from {DateOnly} to {DateOnly} costing {decimal} {word}")]
    public async Task WhenIAddAStay(string name, DateOnly checkIn, DateOnly checkOut, decimal cost, string currency)
    {
        response.Message = await client.PostAsJsonAsync(StaysUrl, NewStay(name, checkIn, checkOut, cost, currency));
    }

    [When("I add a stay at the place {string} from {DateOnly} to {DateOnly}")]
    public async Task WhenIAddAStayAtThePlace(string placeId, DateOnly checkIn, DateOnly checkOut)
    {
        response.Message = await client.PostAsJsonAsync(StaysUrl, new SaveStayDto
        {
            CheckIn = checkIn,
            CheckOut = checkOut,
            PlaceId = placeId,
        });
    }

    [When("I change the stay {string} to {string} from {DateOnly} to {DateOnly} as {string}")]
    public async Task WhenIChangeTheStay(string name, string newName, DateOnly checkIn, DateOnly checkOut, string status)
    {
        var dto = NewStay(newName, checkIn, checkOut, 0m, Money.HomeCurrency);
        dto.BookingStatus = Enum.Parse<BookingStatus>(status);

        response.Message = await client.PutAsJsonAsync($"{StaysUrl}/{_staysByName[name].Id}", dto);
    }

    [When("I delete the stay {string}")]
    public async Task WhenIDeleteTheStay(string name)
    {
        response.Message = await client.DeleteAsync($"{StaysUrl}/{_staysByName[name].Id}");
    }

    [When("I delete an unknown stay")]
    public async Task WhenIDeleteAnUnknownStay()
    {
        response.Message = await client.DeleteAsync($"{StaysUrl}/{Guid.NewGuid()}");
    }

    [Then("the response stay {string} has {int} nights")]
    public async Task ThenTheResponseStayHasNights(string name, int nights)
    {
        var stay = (await response.Message.Content.ReadFromJsonAsync<StayDto>())!;

        Assert.Multiple(() =>
        {
            Assert.That(stay.Name, Is.EqualTo(name));
            Assert.That(stay.Nights, Is.EqualTo(nights));
        });
    }

    [Then("the nights in the schedule are:")]
    public async Task ThenTheNightsInTheScheduleAre(DataTable table)
    {
        var schedule = await GetScheduleAsync();
        var expected = table.Rows.Select(row => (row["date"], row["stay"], row["checkingOut"])).ToList();
        var actual = schedule.Days
            .Select(day => (day.Date.ToString("yyyy-MM-dd"), day.Stay?.Name ?? string.Empty, day.CheckingOut?.Name ?? string.Empty))
            .ToList();

        Assert.That(actual, Is.EqualTo(expected));
    }

    [Then("the schedule lists the stay {string} costing {decimal} {word} converted to TWD")]
    public async Task ThenTheScheduleListsTheStayCosting(string name, decimal cost, string currency)
    {
        var stay = (await GetScheduleAsync()).Stays.Single(item => item.Name == name);

        Assert.Multiple(() =>
        {
            Assert.That(stay.Cost, Is.EqualTo(cost));
            Assert.That(stay.Currency, Is.EqualTo(currency));
            Assert.That(stay.CostInTwd, Is.GreaterThan(0));
        });
    }

    [Then("the schedule lists the stay {string} at {string}")]
    public async Task ThenTheScheduleListsTheStayAt(string name, string address)
    {
        var stay = (await GetScheduleAsync()).Stays.Single(item => item.Name == name);

        Assert.That(stay.Place?.Address, Is.EqualTo(address));
    }

    [Then("the schedule lists the stay {string} as {string}")]
    public async Task ThenTheScheduleListsTheStayAs(string name, string status)
    {
        var stay = (await GetScheduleAsync()).Stays.Single(item => item.Name == name);

        Assert.That(stay.BookingStatus.ToString(), Is.EqualTo(status));
    }

    [Then("the schedule has no stays")]
    public async Task ThenTheScheduleHasNoStays()
    {
        var schedule = await GetScheduleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(schedule.Stays, Is.Empty);
            Assert.That(schedule.Days.Select(day => day.Stay), Is.All.Null);
        });
    }

    private async Task<ScheduleDto> GetScheduleAsync() =>
        (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule"))!;

    private static SaveStayDto NewStay(string name, DateOnly checkIn, DateOnly checkOut, decimal cost, string currency) => new()
    {
        Name = name,
        CheckIn = checkIn,
        CheckOut = checkOut,
        Cost = cost,
        Currency = currency,
    };
}
