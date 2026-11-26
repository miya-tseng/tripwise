using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class ActivityDetailsTests : SchedulePageTest
{
    [Test]
    public async Task AddActivityWithDetails_ShowsCategoryStatusNotesAndLink()
    {
        await TypeCustomActivityAsync("Sushi Dai");
        await PickTimeAsync("Start", "07:00");
        await PickTimeAsync("End", "08:00");
        await Page.GetByRole(AriaRole.Radio, new() { Name = "Food" }).ClickAsync();
        await ChooseBookingAsync("To book");
        await OpenDetailsAsync();
        await Page.GetByLabel("Notes").FillAsync("Queue opens at 5am\nCash only");
        await Page.GetByLabel("Confirmation code").FillAsync("SD-12");
        await Page.GetByLabel("Link").FillAsync("https://example.com/sushi-dai");
        await SubmitButton("Add").ClickAsync();

        var item = ActivityItem("Sushi Dai");
        await Expect(item.GetByTestId("activity-category")).ToHaveTextAsync("🍜");
        await Expect(item.GetByTestId("booking-badge")).ToHaveTextAsync("To book");
        await Expect(item.GetByTestId("activity-notes")).ToHaveTextAsync("Queue opens at 5am");
        await Expect(item.GetByTestId("activity-link")).ToHaveTextAsync("Booking SD-12 ↗");
        await Expect(item.GetByTestId("activity-link")).ToHaveAttributeAsync("href", "https://example.com/sushi-dai");

        await Page.ReloadAsync();
        await Expect(ActivityItem("Sushi Dai").GetByTestId("booking-badge")).ToHaveTextAsync("To book");
    }

    [Test]
    public async Task EditBookingStatus_UpdatesTheBadge()
    {
        await TypeCustomActivityAsync("teamLab");
        await PickTimeAsync("Start", "10:00");
        await PickTimeAsync("End", "12:00");
        await ChooseBookingAsync("To book");
        await SubmitButton("Add").ClickAsync();
        await Expect(ActivityItem("teamLab").GetByTestId("booking-badge")).ToHaveTextAsync("To book");

        await ActivityItem("teamLab").GetByTestId("edit-activity").ClickAsync();
        await ChooseBookingAsync("Paid");
        await SubmitButton("Save").ClickAsync();

        await Expect(ActivityItem("teamLab").GetByTestId("booking-badge")).ToHaveTextAsync("Paid");
    }

    [Test]
    public async Task InvalidLink_IsRejectedBeforeSaving()
    {
        await TypeCustomActivityAsync("Shibuya Sky");
        await PickTimeAsync("Start", "17:00");
        await PickTimeAsync("End", "18:00");
        await OpenDetailsAsync();
        await Page.GetByLabel("Link").FillAsync("http://example.com");

        await Expect(Page.GetByTestId("link-error")).ToHaveTextAsync("Link must start with https://");
        await Expect(SubmitButton("Add")).ToBeDisabledAsync();
    }

    [Test]
    public async Task FilterByCategory_ShowsOnlyThatCategoryInEveryView()
    {
        await AddCategorizedAsync("Senso-ji", "Sightseeing", "10:00", "11:00");
        await AddCategorizedAsync("Sushi Dai", "Food", "07:00", "08:00");

        await Page.GetByTestId("filter-Food").ClickAsync();
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Sushi Dai"]);

        await Page.GetByTestId("view-board").ClickAsync();
        await Expect(Page.GetByTestId("board-card-name")).ToHaveTextAsync(["Sushi Dai"]);

        await Page.GetByTestId("filter-all").ClickAsync();
        await Expect(Page.GetByTestId("board-card-name")).ToHaveTextAsync(["Sushi Dai", "Senso-ji"]);
    }

    private async Task AddCategorizedAsync(string name, string category, string start, string end)
    {
        await TypeCustomActivityAsync(name);
        await PickTimeAsync("Start", start);
        await PickTimeAsync("End", end);
        await Page.GetByRole(AriaRole.Radio, new() { Name = category }).ClickAsync();
        await SubmitButton("Add").ClickAsync();
        await Expect(ActivityItem(name)).ToBeVisibleAsync();
    }

    private async Task ChooseBookingAsync(string label)
    {
        var select = Page.GetByTestId("booking-select");
        await select.GetByTestId("select-trigger").ClickAsync();
        await select.GetByRole(AriaRole.Option, new() { Name = label, Exact = true }).ClickAsync();
        await Expect(select.GetByTestId("select-trigger")).ToContainTextAsync(label);
    }

    private async Task OpenDetailsAsync()
    {
        var toggle = Page.GetByTestId("toggle-details");

        if (await toggle.GetAttributeAsync("aria-expanded") != "true")
        {
            await toggle.ClickAsync();
        }
    }
}
