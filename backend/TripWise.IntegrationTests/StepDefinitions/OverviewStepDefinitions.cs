using System.Globalization;
using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class OverviewStepDefinitions(HttpClient client, ApiResponse response, ScenarioTrip existing)
{
    private TripOverviewDto? _overview;

    [Given("the trip has a {string} activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly} costing {decimal} {word} with booking {string}")]
    public async Task GivenTheTripHasACategorizedActivity(
        string category,
        string name,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        decimal cost,
        string currency,
        string booking)
    {
        var created = await client.PostAsJsonAsync($"/api/trips/{existing.Trip.Id}/activities", new SaveActivityDto
        {
            Name = name,
            Date = date,
            StartTime = start,
            EndTime = end,
            Cost = cost,
            Currency = currency,
            Category = Enum.Parse<ActivityCategory>(category),
            BookingStatus = Enum.Parse<BookingStatus>(booking),
        });
        created.EnsureSuccessStatusCode();
    }

    [When("I get the trip overview")]
    public async Task WhenIGetTheTripOverview()
    {
        response.Message = await client.GetAsync($"/api/trips/{existing.Trip.Id}/overview");
    }

    [When("I get the overview of an unknown trip")]
    public async Task WhenIGetTheOverviewOfAnUnknownTrip()
    {
        response.Message = await client.GetAsync($"/api/trips/{Guid.NewGuid()}/overview");
    }

    [Then("the overview has {int} days, {int} activities and {int} of {int} nights with a stay")]
    public async Task ThenTheOverviewHasCounts(int days, int activities, int nightsWithStay, int nights)
    {
        var overview = await OverviewAsync();

        Assert.Multiple(() =>
        {
            Assert.That(overview.Days, Is.EqualTo(days));
            Assert.That(overview.ActivityCount, Is.EqualTo(activities));
            Assert.That(overview.NightsWithStay, Is.EqualTo(nightsWithStay));
            Assert.That(overview.Nights, Is.EqualTo(nights));
        });
    }

    [Then("the overview spend is {decimal} in total, {decimal} on activities and {decimal} on stays")]
    public async Task ThenTheOverviewSpendIs(decimal total, decimal activities, decimal stays)
    {
        var overview = await OverviewAsync();

        Assert.Multiple(() =>
        {
            Assert.That(overview.TotalInTwd, Is.EqualTo(total));
            Assert.That(overview.ActivitiesInTwd, Is.EqualTo(activities));
            Assert.That(overview.StaysInTwd, Is.EqualTo(stays));
        });
    }

    [Then("the overview average per day is {decimal}")]
    public async Task ThenTheOverviewAveragePerDayIs(decimal average)
    {
        Assert.That((await OverviewAsync()).AveragePerDayInTwd, Is.EqualTo(average));
    }

    [Then("the overview spend by category is {string}")]
    public async Task ThenTheOverviewSpendByCategoryIs(string expected)
    {
        var overview = await OverviewAsync();
        var actual = overview.SpendByCategory.Select(spend =>
            $"{spend.Category} {spend.AmountInTwd?.ToString("0.##", CultureInfo.InvariantCulture)}");

        Assert.That(string.Join(", ", actual), Is.EqualTo(expected));
    }

    [Then("the overview day totals are {string}")]
    public async Task ThenTheOverviewDayTotalsAre(string expected)
    {
        var overview = await OverviewAsync();
        var actual = overview.SpendByDay.Select(day => day.TotalInTwd?.ToString("0.##", CultureInfo.InvariantCulture));

        Assert.That(string.Join(", ", actual), Is.EqualTo(expected));
    }

    [Then("the overview booking progress is {int} to book, {int} booked and {int} paid")]
    public async Task ThenTheOverviewBookingProgressIs(int toBook, int booked, int paid)
    {
        var booking = (await OverviewAsync()).Booking;

        Assert.That((booking.ToBook, booking.Booked, booking.Paid), Is.EqualTo((toBook, booked, paid)));
    }

    [Then("the overview top expenses are {string}")]
    public async Task ThenTheOverviewTopExpensesAre(string expected)
    {
        var overview = await OverviewAsync();

        Assert.That(string.Join(", ", overview.TopExpenses.Select(expense => expense.Name)), Is.EqualTo(expected));
    }

    [Then("the overview needs attention for {string}")]
    public async Task ThenTheOverviewNeedsAttentionFor(string expected)
    {
        var overview = await OverviewAsync();
        var actual = overview.Attention.Select(item =>
            $"{item.Kind} {item.Name ?? item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");

        Assert.That(string.Join(", ", actual), Is.EqualTo(expected));
    }

    [Then("the overview has no totals in TWD")]
    public async Task ThenTheOverviewHasNoTotalsInTwd()
    {
        var overview = await OverviewAsync();

        Assert.Multiple(() =>
        {
            Assert.That(overview.RatesAvailable, Is.False);
            Assert.That(overview.TotalInTwd, Is.Null);
            Assert.That(overview.SpendByCurrency.Single().Currency, Is.EqualTo("JPY"));
        });
    }

    private async Task<TripOverviewDto> OverviewAsync()
    {
        _overview ??= await response.Message.Content.ReadFromJsonAsync<TripOverviewDto>(ApiJson.Options);
        return _overview!;
    }
}
