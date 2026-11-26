using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class TripPageFlowTests : SchedulePageTest
{
    [Test]
    public async Task AddFromADay_PreselectsThatDay()
    {
        await ScheduleDay(3).GetByTestId("add-to-day").ClickAsync();

        await Expect(Dialog()).ToContainTextAsync("Day 3 · Fri, May 3");
        await Expect(Page.GetByTestId("day-select").GetByTestId("select-trigger")).ToContainTextAsync("Day 3");

        await TypeCustomActivityAsync("Nikko day trip");
        await PickTimeAsync("Start", "08:00");
        await PickTimeAsync("End", "18:00");
        await SubmitAsync("Add", "Nikko day trip");

        await Expect(ActivityNames(day: 3)).ToHaveTextAsync(["Nikko day trip"]);
    }

    [Test]
    public async Task AddFromAnEmptyDay_OpensTheDialogForThatDay()
    {
        await ScheduleDay(2).GetByTestId("day-empty").ClickAsync();

        await Expect(Page.GetByTestId("day-select").GetByTestId("select-trigger")).ToContainTextAsync("Day 2");
    }

    [Test]
    public async Task AddFromABoardColumn_PreselectsThatDay()
    {
        await Page.GetByTestId("view-board").ClickAsync();

        await Page.GetByTestId("board-add").Nth(1).ClickAsync();

        await Expect(Page.GetByTestId("day-select").GetByTestId("select-trigger")).ToContainTextAsync("Day 2");
    }

    [Test]
    public async Task Header_StaysVisibleWhileScrolling()
    {
        for (var index = 0; index < 4; index++)
        {
            await AddActivityAsync($"Stop {index + 1}", day: 1, $"{9 + index * 2:00}:00", $"{10 + index * 2:00}:00");
        }

        await Page.Mouse.WheelAsync(0, 2000);

        var bar = Page.GetByTestId("trip-bar");
        await Expect(bar).ToBeInViewportAsync();
        var box = (await bar.BoundingBoxAsync())!;
        Assert.That(box.Y, Is.EqualTo(0).Within(1));
        await Expect(bar.GetByTestId("add-activity")).ToBeInViewportAsync();
    }

    [Test]
    public async Task DropdownsInTheDialog_StayInsideTheWindow()
    {
        await Page.SetViewportSizeAsync(1280, 700);
        await OpenAddDialogAsync();

        var select = Page.GetByTestId("booking-select");
        await select.GetByTestId("select-trigger").ClickAsync();

        var menu = select.GetByTestId("select-menu");
        await Expect(menu.GetByRole(AriaRole.Option)).ToHaveCountAsync(4);
        foreach (var option in await menu.GetByRole(AriaRole.Option).AllAsync())
        {
            await Expect(option).ToBeInViewportAsync(new() { Ratio = 1 });
        }
    }

    [Test]
    public async Task FailedSave_KeepsTheDialogOpenWithTheError()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");

        await AddActivityAsync("Ueno Park", day: 1, "10:30", "11:30");

        await Expect(Dialog()).ToBeVisibleAsync();
        await Expect(Dialog().GetByTestId("schedule-error")).ToContainTextAsync("overlaps with \"Senso-ji\"");

        await Page.GetByTestId("close-activity-dialog").ClickAsync();
        await Expect(Dialog()).ToHaveCountAsync(0);
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Senso-ji"]);
    }
}
