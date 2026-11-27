using System.Net.Http.Json;
using Reqnroll;
using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.StepDefinitions;

[Binding]
public class ChecklistStepDefinitions(HttpClient client, ApiResponse response, ScenarioTrip existing)
{
    private string ChecklistUrl => $"/api/trips/{existing.Trip.Id}/checklist";

    [Given("the checklist has {string} in {string}")]
    public async Task GivenTheChecklistHas(string text, string section)
    {
        var added = await client.PostAsJsonAsync(ChecklistUrl, NewItem(text, section, null));
        added.EnsureSuccessStatusCode();
    }

    [Given("the trip has a stay {string} from {DateOnly} to {DateOnly} booked")]
    public async Task GivenTheTripHasABookedStay(string name, DateOnly checkIn, DateOnly checkOut)
    {
        var added = await client.PostAsJsonAsync($"/api/trips/{existing.Trip.Id}/stays", new SaveStayDto
        {
            Name = name,
            CheckIn = checkIn,
            CheckOut = checkOut,
            BookingStatus = BookingStatus.Booked,
        });
        added.EnsureSuccessStatusCode();
    }

    [When("I add {string} to {string} on the checklist")]
    public async Task WhenIAdd(string text, string section)
    {
        await SendAsync(client.PostAsJsonAsync(ChecklistUrl, NewItem(text, section, null)));
    }

    [When("I add {string} to {string} on the checklist due {DateOnly}")]
    public async Task WhenIAddDue(string text, string section, DateOnly dueDate)
    {
        await SendAsync(client.PostAsJsonAsync(ChecklistUrl, NewItem(text, section, dueDate)));
    }

    [When("I tick {string} on the checklist")]
    public async Task WhenITick(string text)
    {
        var item = await ItemAsync(text);
        await SendAsync(client.PutAsJsonAsync($"{ChecklistUrl}/{item.Id}/done", new SetDoneDto { Done = true }));
    }

    [When("I untick {string} on the checklist")]
    public async Task WhenIUntick(string text)
    {
        var item = await ItemAsync(text);
        await SendAsync(client.PutAsJsonAsync($"{ChecklistUrl}/{item.Id}/done", new SetDoneDto { Done = false }));
    }

    [When("I change the checklist item {string} to {string} in {string}")]
    public async Task WhenIChangeTheItem(string text, string newText, string section)
    {
        var item = await ItemAsync(text);
        await SendAsync(client.PutAsJsonAsync($"{ChecklistUrl}/{item.Id}", NewItem(newText, section, null)));
    }

    [When("I reorder the checklist {string} section to {string}")]
    public async Task WhenIReorder(string section, string texts)
    {
        var checklist = await FetchAsync();
        var ids = Split(texts).Select(text => checklist.Items.First(item => item.Text == text).Id).ToList();

        await SendAsync(client.PutAsJsonAsync($"{ChecklistUrl}/order", new ReorderChecklistDto
        {
            Section = Enum.Parse<ChecklistSection>(section),
            ItemIds = ids,
        }));
    }

    [When("I delete {string} from the checklist")]
    public async Task WhenIDelete(string text)
    {
        var item = await ItemAsync(text);
        await SendAsync(client.DeleteAsync($"{ChecklistUrl}/{item.Id}"));
    }

    [When("I delete an unknown item from the checklist")]
    public async Task WhenIDeleteAnUnknownItem()
    {
        await SendAsync(client.DeleteAsync($"{ChecklistUrl}/{Guid.NewGuid()}"));
    }

    [When("I get the checklist")]
    public async Task WhenIGetTheChecklist()
    {
        await SendAsync(client.GetAsync(ChecklistUrl));
    }

    [When("I get the checklist of an unknown trip")]
    public async Task WhenIGetTheChecklistOfAnUnknownTrip()
    {
        await SendAsync(client.GetAsync($"/api/trips/{Guid.NewGuid()}/checklist"));
    }

    [When("I mark the activity {string} as booked on the checklist")]
    public async Task WhenIMarkTheActivityAsBooked(string name)
    {
        var activity = await ActivityAsync(name);
        await SendAsync(client.PutAsJsonAsync(
            $"{ChecklistUrl}/bookings/activities/{activity.Id}",
            new SetDoneDto { Done = true }));
    }

    [When("I mark the stay {string} as not booked on the checklist")]
    public async Task WhenIMarkTheStayAsNotBooked(string name)
    {
        var schedule = (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule", ApiJson.Options))!;
        var stay = schedule.Stays.First(candidate => candidate.Name == name);
        await SendAsync(client.PutAsJsonAsync(
            $"{ChecklistUrl}/bookings/stays/{stay.Id}",
            new SetDoneDto { Done = false }));
    }

    [When("I change the booking status of {string} to {string}")]
    public async Task WhenIChangeTheBookingStatus(string name, string status)
    {
        var activity = await ActivityAsync(name);
        var updated = await client.PutAsJsonAsync($"/api/trips/{existing.Trip.Id}/activities/{activity.Id}", new SaveActivityDto
        {
            Name = activity.Name,
            Date = activity.Date,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            Cost = activity.Cost,
            Currency = activity.Currency,
            Category = activity.Category,
            BookingStatus = Enum.Parse<BookingStatus>(status),
            Notes = activity.Notes,
            ConfirmationCode = activity.ConfirmationCode,
            Link = activity.Link,
        });
        updated.EnsureSuccessStatusCode();
    }

    [Then("the checklist {string} section is {string}")]
    public async Task ThenTheSectionIs(string section, string texts)
    {
        var checklist = await FetchAsync();
        var actual = checklist.Items
            .Where(item => item.Section == Enum.Parse<ChecklistSection>(section))
            .OrderBy(item => item.Position)
            .Select(item => item.Text);

        Assert.That(actual, Is.EqualTo(Split(texts)));
    }

    [Then("the checklist item {string} is due {DateOnly}")]
    public async Task ThenTheItemIsDue(string text, DateOnly dueDate)
    {
        Assert.That((await ItemAsync(text)).DueDate, Is.EqualTo(dueDate));
    }

    [Then("the checklist progress is {int} of {int}")]
    public async Task ThenTheProgressIs(int done, int total)
    {
        var checklist = await FetchAsync();

        Assert.That((checklist.Done, checklist.Total), Is.EqualTo((done, total)));
    }

    [Then("the checklist bookings are {string}")]
    public async Task ThenTheBookingsAre(string expected)
    {
        var checklist = await FetchAsync();
        var actual = checklist.Bookings.Select(item => $"{item.Name} {(item.Done ? "done" : "open")}");

        Assert.That(string.Join(", ", actual), Is.EqualTo(expected));
    }

    [Then("the activity {string} has booking status {string} in the schedule")]
    public async Task ThenTheActivityHasBookingStatus(string name, string status)
    {
        Assert.That((await ActivityAsync(name)).BookingStatus, Is.EqualTo(Enum.Parse<BookingStatus>(status)));
    }

    private async Task SendAsync(Task<HttpResponseMessage> request)
    {
        response.Message = await request;
    }

    private async Task<ChecklistDto> FetchAsync() =>
        (await client.GetFromJsonAsync<ChecklistDto>(ChecklistUrl, ApiJson.Options))!;

    private async Task<ChecklistItemDto> ItemAsync(string text)
    {
        return (await FetchAsync()).Items.First(item => item.Text == text);
    }

    private async Task<ActivityDto> ActivityAsync(string name)
    {
        var schedule = (await client.GetFromJsonAsync<ScheduleDto>($"/api/trips/{existing.Trip.Id}/schedule", ApiJson.Options))!;
        return schedule.Days.SelectMany(day => day.Activities).First(activity => activity.Name == name);
    }

    private static SaveChecklistItemDto NewItem(string text, string section, DateOnly? dueDate) => new()
    {
        Text = text,
        Section = Enum.Parse<ChecklistSection>(section),
        DueDate = dueDate,
    };

    private static string[] Split(string texts) =>
        texts.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
}
