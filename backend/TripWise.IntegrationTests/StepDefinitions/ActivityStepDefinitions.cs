using System.Globalization;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reqnroll;
using TripWise.Infrastructure.Data;
using TripWise.Infrastructure.Data.Configurations;
using TripWise.Domain;
using TripWise.Application.Dtos;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class ActivityStepDefinitions(
    HttpClient client,
    ApiResponse response,
    ScenarioTrip existing,
    TripWiseApiFactory factory)
{
    private readonly Dictionary<string, ActivityDto> _activitiesByName = [];

    private string ActivitiesUrl => $"/api/trips/{existing.Trip.Id}/activities";

    [Given("the trip has an activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task GivenTheTripHasAnActivity(string name, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var created = await client.PostAsJsonAsync(ActivitiesUrl, NewActivity(name, date, start, end));
        created.EnsureSuccessStatusCode();
        _activitiesByName[name] = (await created.Content.ReadFromJsonAsync<ActivityDto>())!;
    }

    [Given("the trip has an activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly} costing {decimal}")]
    public async Task GivenTheTripHasAnActivityCosting(string name, DateOnly date, TimeOnly start, TimeOnly end, decimal cost)
    {
        var created = await client.PostAsJsonAsync(ActivitiesUrl, NewActivity(name, date, start, end, cost));
        created.EnsureSuccessStatusCode();
        _activitiesByName[name] = (await created.Content.ReadFromJsonAsync<ActivityDto>())!;
    }

    [When("I add an activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly} costing {decimal}")]
    public async Task WhenIAddAnActivityCosting(string name, DateOnly date, TimeOnly start, TimeOnly end, decimal cost)
    {
        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, NewActivity(name, date, start, end, cost));
    }

    [When("I change the cost of {string} to {decimal}")]
    public async Task WhenIChangeTheCostOf(string name, decimal cost)
    {
        var activity = _activitiesByName[name];

        response.Message = await client.PutAsJsonAsync(
            $"{ActivitiesUrl}/{activity.Id}",
            NewActivity(activity.Name, activity.Date, activity.StartTime, activity.EndTime, cost));
    }

    [Then("the response activity costs {decimal}")]
    public async Task ThenTheResponseActivityCosts(decimal cost)
    {
        var activity = (await response.Message.Content.ReadFromJsonAsync<ActivityDto>())!;

        Assert.That(activity.Cost, Is.EqualTo(cost));
    }

    [Then("the activity {string} costs {decimal} in the schedule")]
    public async Task ThenTheActivityCostsInTheSchedule(string name, decimal cost)
    {
        var schedule = await GetScheduleAsync();
        var activity = schedule.Days.SelectMany(day => day.Activities).Single(item => item.Name == name);

        Assert.That(activity.Cost, Is.EqualTo(cost));
    }

    [Then("the schedule total is {decimal}")]
    public async Task ThenTheScheduleTotalIs(decimal total)
    {
        var schedule = await GetScheduleAsync();

        Assert.That(schedule.Total, Is.EqualTo(total));
    }

    [Then("the schedule total on {DateOnly} is {decimal}")]
    public async Task ThenTheScheduleTotalOnDateIs(DateOnly date, decimal total)
    {
        var schedule = await GetScheduleAsync();

        Assert.That(schedule.Days.Single(day => day.Date == date).Total, Is.EqualTo(total));
    }

    [When("I add an activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task WhenIAddAnActivity(string name, DateOnly date, TimeOnly start, TimeOnly end)
    {
        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, NewActivity(name, date, start, end));
    }

    [When("I add an activity {string} on {DateOnly} with raw times {string} and {string}")]
    public async Task WhenIAddAnActivityWithRawTimes(string name, DateOnly date, string start, string end)
    {
        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, new { Name = name, Date = date, StartTime = start, EndTime = end });
    }

    [When("I add an activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly} to an unknown trip")]
    public async Task WhenIAddAnActivityToAnUnknownTrip(string name, DateOnly date, TimeOnly start, TimeOnly end)
    {
        response.Message = await client.PostAsJsonAsync(
            $"/api/trips/{Guid.NewGuid()}/activities",
            NewActivity(name, date, start, end));
    }

    [When("I add an activity {string} on {DateOnly} without times")]
    public async Task WhenIAddAnActivityWithoutTimes(string name, DateOnly date)
    {
        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, new { Name = name, Date = date });
    }

    [When("I update the activity {string} to {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task WhenIUpdateTheActivity(string currentName, string newName, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var activityId = _activitiesByName[currentName].Id;

        response.Message = await client.PutAsJsonAsync(
            $"{ActivitiesUrl}/{activityId}",
            NewActivity(newName, date, start, end));
    }

    [When("I update an unknown activity to {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task WhenIUpdateAnUnknownActivity(string name, DateOnly date, TimeOnly start, TimeOnly end)
    {
        response.Message = await client.PutAsJsonAsync(
            $"{ActivitiesUrl}/{Guid.NewGuid()}",
            NewActivity(name, date, start, end));
    }

    [When("I delete the activity {string}")]
    public async Task WhenIDeleteTheActivity(string name)
    {
        response.Message = await client.DeleteAsync($"{ActivitiesUrl}/{_activitiesByName[name].Id}");
    }

    [When("I delete an unknown activity")]
    public async Task WhenIDeleteAnUnknownActivity()
    {
        response.Message = await client.DeleteAsync($"{ActivitiesUrl}/{Guid.NewGuid()}");
    }

    [When("I get the schedule")]
    public async Task WhenIGetTheSchedule()
    {
        response.Message = await client.GetAsync($"/api/trips/{existing.Trip.Id}/schedule");
    }

    [When("I get the schedule of an unknown trip")]
    public async Task WhenIGetTheScheduleOfAnUnknownTrip()
    {
        response.Message = await client.GetAsync($"/api/trips/{Guid.NewGuid()}/schedule");
    }

    [Then("the response is an activity {string} on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task ThenTheResponseIsAnActivity(string name, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var activity = (await response.Message.Content.ReadFromJsonAsync<ActivityDto>())!;

        Assert.Multiple(() =>
        {
            Assert.That(activity.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(activity.Name, Is.EqualTo(name));
            Assert.That(activity.Date, Is.EqualTo(date));
            Assert.That(activity.StartTime, Is.EqualTo(start));
            Assert.That(activity.EndTime, Is.EqualTo(end));
        });
    }

    [Then("the response location points to the schedule")]
    public void ThenTheResponseLocationPointsToTheSchedule()
    {
        Assert.That(
            response.Message.Headers.Location!.ToString(),
            Does.EndWith($"/api/trips/{existing.Trip.Id}/schedule"));
    }

    [Then("the schedule on {DateOnly} is {string}")]
    public async Task ThenTheScheduleOnDateIs(DateOnly date, string expectedNames)
    {
        var schedule = await GetScheduleAsync();
        var day = schedule.Days.Single(scheduleDay => scheduleDay.Date == date);

        Assert.That(day.Activities.Select(activity => activity.Name), Is.EqualTo(SplitList(expectedNames)));
    }

    [Then(@"^the schedule has the days (.*)$")]
    public async Task ThenTheScheduleHasTheDays(string expectedDates)
    {
        var schedule = (await response.Message.Content.ReadFromJsonAsync<ScheduleDto>())!;
        var expected = SplitList(expectedDates)
            .Select(date => DateOnly.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture));

        Assert.That(schedule.Days.Select(day => day.Date), Is.EqualTo(expected));
    }

    [Then("the activity {string} is still on {DateOnly} from {TimeOnly} to {TimeOnly}")]
    public async Task ThenTheActivityIsStillOn(string name, DateOnly date, TimeOnly start, TimeOnly end)
    {
        var schedule = await GetScheduleAsync();
        var activity = schedule.Days
            .SelectMany(day => day.Activities)
            .Single(scheduled => scheduled.Id == _activitiesByName[name].Id);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Name, Is.EqualTo(name));
            Assert.That(activity.Date, Is.EqualTo(date));
            Assert.That(activity.StartTime, Is.EqualTo(start));
            Assert.That(activity.EndTime, Is.EqualTo(end));
        });
    }

    [Then("no activities of the trip remain in the database")]
    public async Task ThenNoActivitiesOfTheTripRemainInTheDatabase()
    {
        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var remaining = await dbContext.Set<Activity>()
            .CountAsync(activity => EF.Property<Guid>(activity, ActivityConfiguration.TripIdColumn) == existing.Trip.Id);

        Assert.That(remaining, Is.Zero);
    }

    private async Task<ScheduleDto> GetScheduleAsync()
    {
        return (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule"))!;
    }

    private static string[] SplitList(string value) =>
        value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static SaveActivityDto NewActivity(
        string name,
        DateOnly date,
        TimeOnly start,
        TimeOnly end,
        decimal? cost = null)
    {
        return new SaveActivityDto
        {
            Name = name,
            Date = date,
            StartTime = start,
            EndTime = end,
            Cost = cost,
        };
    }
}
