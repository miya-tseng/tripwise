using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class BoardTests : SchedulePageTest
{
    [Test]
    public async Task DragCardToAnotherDay_KeepsItsTime()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:30");
        await OpenBoardAsync();

        await BoardCard("Senso-ji").DragToAsync(ColumnHeader(2));

        await Expect(CardNames(column: 2)).ToHaveTextAsync(["Senso-ji"]);
        await Expect(CardNames(column: 1)).ToHaveCountAsync(0);
        await Expect(BoardCard("Senso-ji").GetByTestId("board-card-time")).ToHaveTextAsync("10:00 – 11:30");
    }

    [Test]
    public async Task DragCardBelowAnother_StartsRightAfterIt()
    {
        await AddActivityAsync("Senso-ji", day: 1, "09:00", "10:30");
        await AddActivityAsync("Tokyo Tower", day: 2, "08:00", "09:00");
        await OpenBoardAsync();

        var target = BoardCard("Tokyo Tower");
        var box = (await target.BoundingBoxAsync())!;
        await BoardCard("Senso-ji").DragToAsync(Column(2).GetByTestId("board-cards"), new()
        {
            TargetPosition = new() { X = box.Width / 2, Y = box.Height + 20 },
        });

        await Expect(CardNames(column: 2)).ToHaveTextAsync(["Tokyo Tower", "Senso-ji"]);
        await Expect(BoardCard("Senso-ji").GetByTestId("board-card-time")).ToHaveTextAsync("09:00 – 10:30");
    }

    [Test]
    public async Task DragCardIntoAnOverlap_ShowsErrorAndKeepsIt()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:30");
        await AddActivityAsync("Ueno Park", day: 2, "10:30", "11:00");
        await OpenBoardAsync();

        await BoardCard("Senso-ji").DragToAsync(ColumnHeader(2));

        await Expect(Page.GetByTestId("schedule-error")).ToContainTextAsync("overlaps with \"Ueno Park\"");
        await Expect(CardNames(column: 1)).ToHaveTextAsync(["Senso-ji"]);

        await Page.ReloadAsync();
        await OpenBoardAsync();
        await Expect(CardNames(column: 1)).ToHaveTextAsync(["Senso-ji"]);
    }

    [Test]
    public async Task MoveWithKeyboardMenu_ChangesDayAndTime()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:30");
        await OpenBoardAsync();

        await BoardCard("Senso-ji").GetByRole(AriaRole.Button, new() { Name = "Move Senso-ji" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "Next day" }).ClickAsync();
        await Expect(CardNames(column: 2)).ToHaveTextAsync(["Senso-ji"]);

        await BoardCard("Senso-ji").GetByRole(AriaRole.Button, new() { Name = "Move Senso-ji" }).ClickAsync();
        await Page.GetByRole(AriaRole.Menuitem, new() { Name = "30 min later" }).ClickAsync();
        await Expect(BoardCard("Senso-ji").GetByTestId("board-card-time")).ToHaveTextAsync("10:30 – 12:00");

        await Page.GetByTestId("view-timeline").ClickAsync();
        await Expect(ActivityNames(day: 2)).ToHaveTextAsync(["Senso-ji"]);
    }

    private async Task OpenBoardAsync()
    {
        await Page.GetByTestId("view-board").ClickAsync();
        await Expect(Page.GetByTestId("board-view")).ToBeVisibleAsync();
    }

    private ILocator Column(int day) => Page.GetByTestId("board-column").Nth(day - 1);

    private ILocator ColumnHeader(int day) => Column(day).GetByTestId("board-column-header");

    private ILocator CardNames(int column) => Column(column).GetByTestId("board-card-name");

    private ILocator BoardCard(string name) =>
        Page.GetByTestId("board-card").Filter(new() { HasText = name });
}
