using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class ActivityDetailsStepDefinitions(HttpClient client, ApiResponse response, ScenarioTrip existing)
{
    private readonly Dictionary<string, ActivityDto> _added = [];

    private string ActivitiesUrl => $"/api/trips/{existing.Trip.Id}/activities";

    [When("I add an activity {string} on {DateOnly} with details:")]
    public async Task WhenIAddAnActivityWithDetails(string name, DateOnly date, DataTable table)
    {
        var row = table.Rows.Single();
        var dto = Activity(name, date);
        dto.Category = Enum.Parse<ActivityCategory>(row["category"]);
        dto.BookingStatus = Enum.Parse<BookingStatus>(row["bookingStatus"]);
        dto.Notes = row["notes"];
        dto.ConfirmationCode = row["confirmationCode"];
        dto.Link = row["link"];

        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, dto);

        if (response.Message.IsSuccessStatusCode)
        {
            _added[name] = (await response.Message.Content.ReadFromJsonAsync<ActivityDto>(ApiJson.Options))!;
        }
    }

    [When("I add an activity {string} on {DateOnly} with category {string}")]
    public async Task WhenIAddAnActivityWithCategory(string name, DateOnly date, string category)
    {
        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, new
        {
            Name = name,
            Date = date,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(11, 0),
            Category = category,
        });
    }

    [When("I add an activity {string} on {DateOnly} with {int} characters of notes")]
    public async Task WhenIAddAnActivityWithLongNotes(string name, DateOnly date, int length)
    {
        var dto = Activity(name, date);
        dto.Notes = new string('a', length);

        response.Message = await client.PostAsJsonAsync(ActivitiesUrl, dto);
    }

    [When("I mark the activity {string} as {string} with confirmation {string}")]
    public async Task WhenIMarkTheActivityAs(string name, string status, string confirmation)
    {
        var activity = _added[name];
        var dto = new SaveActivityDto
        {
            Name = activity.Name,
            Date = activity.Date,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            Category = activity.Category,
            BookingStatus = Enum.Parse<BookingStatus>(status),
            Notes = activity.Notes,
            ConfirmationCode = confirmation,
            Link = activity.Link,
        };

        response.Message = await client.PutAsJsonAsync($"{ActivitiesUrl}/{activity.Id}", dto);
    }

    [When("I move the added activity {string} to {DateOnly} at {TimeOnly}")]
    public async Task WhenIMoveTheAddedActivity(string name, DateOnly date, TimeOnly start)
    {
        response.Message = await client.PatchAsJsonAsync(
            $"{ActivitiesUrl}/{_added[name].Id}/move",
            new MoveActivityDto { Date = date, StartTime = start });
    }

    [Then("the activity {string} in the schedule has details:")]
    public async Task ThenTheActivityInTheScheduleHasDetails(string name, DataTable table)
    {
        var row = table.Rows.Single();
        var schedule = (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule", ApiJson.Options))!;
        var activity = schedule.Days.SelectMany(day => day.Activities).Single(item => item.Name == name);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Category.ToString(), Is.EqualTo(row["category"]));
            Assert.That(activity.BookingStatus.ToString(), Is.EqualTo(row["bookingStatus"]));
            Assert.That(activity.Notes, Is.EqualTo(row["notes"]));
            Assert.That(activity.ConfirmationCode, Is.EqualTo(row["confirmationCode"]));
            Assert.That(activity.Link ?? string.Empty, Is.EqualTo(row["link"]));
        });
    }

    private static SaveActivityDto Activity(string name, DateOnly date) => new()
    {
        Name = name,
        Date = date,
        StartTime = new TimeOnly(10, 0),
        EndTime = new TimeOnly(11, 0),
    };
}
