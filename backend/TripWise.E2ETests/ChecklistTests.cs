using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class ChecklistTests : SchedulePageTest
{
    [Test]
    public async Task AddAndTickItems_UpdatesTheProgress()
    {
        await OpenChecklistAsync();

        await QuickAddAsync("ToPack", "Passport");
        await QuickAddAsync("ToPack", "Charger");
        await QuickAddAsync("Other", "Exchange some yen");
        await Expect(Page.GetByTestId("checklist-progress")).ToHaveTextAsync("0 / 3 done");

        await Item("ToPack", "Passport").GetByTestId("item-checkbox").CheckAsync();
        await Expect(Page.GetByTestId("checklist-progress")).ToHaveTextAsync("1 / 3 done");
        await Expect(Section("ToPack").GetByTestId("section-count")).ToHaveTextAsync("1 / 2");

        await Page.ReloadAsync();
        await Expect(Item("ToPack", "Passport").GetByTestId("item-checkbox")).ToBeCheckedAsync();
        await Expect(Page.GetByTestId("checklist-progress")).ToHaveTextAsync("1 / 3 done");
    }

    [Test]
    public async Task Suggestion_AddsACommonItemOnce()
    {
        await OpenChecklistAsync();

        await Section("ToPack").GetByTestId("suggestion").Filter(new() { HasText = "Power adapter" }).ClickAsync();

        await Expect(Section("ToPack").GetByTestId("item-text")).ToHaveTextAsync(["Power adapter"]);
        await Expect(Section("ToPack").GetByTestId("suggestion").Filter(new() { HasText = "Power adapter" }))
            .ToHaveCountAsync(0);
    }

    [Test]
    public async Task EditItem_MovesItAndShowsItOverdue()
    {
        await OpenChecklistAsync();
        await QuickAddAsync("ToPack", "Passport");

        await Item("ToPack", "Passport").GetByTestId("edit-item").ClickAsync();
        await Page.GetByTestId("edit-item-text").FillAsync("Renew passport");
        var section = Page.GetByTestId("edit-item-section");
        await section.GetByTestId("select-trigger").ClickAsync();
        await section.GetByRole(AriaRole.Option, new() { Name = "To book", Exact = true }).ClickAsync();
        await Page.GetByTestId("edit-item-due").FillAsync("2020-01-01");
        await Page.GetByTestId("save-item").ClickAsync();

        var renewed = Item("ToBook", "Renew passport");
        await Expect(renewed).ToHaveClassAsync(new System.Text.RegularExpressions.Regex("overdue"));
        await Expect(renewed.GetByTestId("item-due")).ToHaveTextAsync("Overdue · Jan 1");
        await Expect(Section("ToPack").GetByTestId("checklist-item")).ToHaveCountAsync(0);
    }

    [Test]
    public async Task DeleteItem_RemovesIt()
    {
        await OpenChecklistAsync();
        await QuickAddAsync("Other", "Buy SIM card");

        await Item("Other", "Buy SIM card").GetByTestId("edit-item").ClickAsync();
        await Page.GetByTestId("delete-item").ClickAsync();

        await Expect(Section("Other").GetByTestId("checklist-item")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("checklist-progress")).ToHaveTextAsync("0 / 0 done");
    }

    [Test]
    public async Task DragAnItem_ChangesTheOrder()
    {
        await OpenChecklistAsync();
        await QuickAddAsync("ToPack", "Passport");
        await QuickAddAsync("ToPack", "Charger");
        await QuickAddAsync("ToPack", "Adapter");

        await Page.RunAndWaitForResponseAsync(
            () => DragAsync(Item("ToPack", "Adapter").GetByTestId("drag-handle"), Item("ToPack", "Passport")),
            response => response.Url.EndsWith("/checklist/order", StringComparison.Ordinal) && response.Ok);

        await Expect(Section("ToPack").GetByTestId("item-text")).ToHaveTextAsync(["Adapter", "Passport", "Charger"]);
        await Page.ReloadAsync();
        await Expect(Section("ToPack").GetByTestId("item-text")).ToHaveTextAsync(["Adapter", "Passport", "Charger"]);
    }

    [Test]
    public async Task ActivityToBook_AppearsAndIsTickedWhenBooked()
    {
        await TypeCustomActivityAsync("teamLab");
        await PickTimeAsync("Start", "10:00");
        await PickTimeAsync("End", "12:00");
        await ChooseBookingAsync("To book");
        await SubmitAsync("Add", "teamLab");

        await OpenChecklistAsync();
        var booking = Section("ToBook").GetByTestId("booking-item").Filter(new() { HasText = "teamLab" });
        await Expect(booking.GetByTestId("booking-checkbox")).Not.ToBeCheckedAsync();
        await Expect(booking.GetByTestId("booking-item-status")).ToHaveTextAsync("To book");

        await Page.GetByTestId("nav-itinerary").ClickAsync();
        await ActivityItem("teamLab").GetByTestId("edit-activity").ClickAsync();
        await ChooseBookingAsync("Booked");
        await SubmitButton("Save").ClickAsync();
        await Expect(ActivityItem("teamLab").GetByTestId("booking-badge")).ToHaveTextAsync("Booked");

        await OpenChecklistAsync();
        await Expect(booking.GetByTestId("booking-checkbox")).ToBeCheckedAsync();
        await Expect(booking.GetByTestId("booking-item-status")).ToHaveTextAsync("Booked");
    }

    [Test]
    public async Task TickABookingOnTheChecklist_MarksTheActivityBooked()
    {
        await TypeCustomActivityAsync("Sushi Dai");
        await PickTimeAsync("Start", "07:00");
        await PickTimeAsync("End", "08:00");
        await ChooseBookingAsync("To book");
        await SubmitAsync("Add", "Sushi Dai");

        await OpenChecklistAsync();
        await Section("ToBook").GetByTestId("booking-item").Filter(new() { HasText = "Sushi Dai" })
            .GetByTestId("booking-checkbox").CheckAsync();
        await Expect(Page.GetByTestId("checklist-progress")).ToHaveTextAsync("1 / 1 done");

        await Page.GetByTestId("nav-itinerary").ClickAsync();
        await Expect(ActivityItem("Sushi Dai").GetByTestId("booking-badge")).ToHaveTextAsync("Booked");
    }

    private async Task DragAsync(ILocator source, ILocator target)
    {
        var from = (await source.BoundingBoxAsync())!;
        var to = (await target.BoundingBoxAsync())!;

        await Page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(from.X + from.Width / 2, from.Y + from.Height / 2 - 10, new() { Steps = 3 });
        await Page.Mouse.MoveAsync(to.X + 40, to.Y + 4, new() { Steps = 10 });
        await Page.Mouse.UpAsync();
    }

    private ILocator Section(string section) => Page.GetByTestId($"section-{section}");

    private ILocator Item(string section, string text) =>
        Section(section).GetByTestId("checklist-item").Filter(new() { HasText = text });

    private async Task OpenChecklistAsync()
    {
        await Page.GetByTestId("nav-checklist").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Checklist", Level = 1 })).ToBeVisibleAsync();
    }

    private async Task QuickAddAsync(string section, string text)
    {
        var input = Section(section).GetByTestId("quick-add");
        await input.FillAsync(text);
        await input.PressAsync("Enter");
        await Expect(Item(section, text)).ToBeVisibleAsync();
    }

    private async Task ChooseBookingAsync(string label)
    {
        var select = Page.GetByTestId("booking-select");
        await select.GetByTestId("select-trigger").ClickAsync();
        await select.GetByRole(AriaRole.Option, new() { Name = label, Exact = true }).ClickAsync();
    }
}
