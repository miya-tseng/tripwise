using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class OverviewTests : SchedulePageTest
{
    [Test]
    public async Task Overview_ReflectsAddingAStayAndBookingAnActivity()
    {
        await TypeCustomActivityAsync("teamLab");
        await PickTimeAsync("Start", "10:00");
        await PickTimeAsync("End", "12:00");
        await CostInput().FillAsync("1200");
        await ChooseBookingAsync("To book");
        await SubmitAsync("Add", "teamLab");

        await OpenOverviewAsync();
        await Expect(Page.GetByTestId("total-spend")).ToContainTextAsync("NT$1,200.00");
        await Expect(Page.GetByTestId("nights-covered")).ToContainTextAsync("0 / 3");
        await Expect(AttentionItem("Book teamLab")).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("attention-item").Filter(new() { HasText = "No stay for the night" }))
            .ToHaveCountAsync(3);

        await Page.GetByTestId("back-to-trip").ClickAsync();
        await Page.GetByTestId("add-stay").ClickAsync();
        await Page.GetByTestId("use-custom-stay").ClickAsync();
        await Page.GetByLabel("Hotel or place").FillAsync("Hotel Gracery");
        await Page.GetByTestId("stay-dialog").GetByTestId("quick-check-out").Filter(new() { HasText = "All 3 nights" }).ClickAsync();
        await Page.GetByLabel("Total cost").FillAsync("9000");
        await Page.GetByTestId("save-stay").ClickAsync();
        await Expect(Page.GetByTestId("stay-dialog")).ToHaveCountAsync(0);

        await ActivityItem("teamLab").GetByTestId("edit-activity").ClickAsync();
        await ChooseBookingAsync("Booked");
        await SubmitButton("Save").ClickAsync();
        await Expect(ActivityItem("teamLab").GetByTestId("booking-badge")).ToHaveTextAsync("Booked");

        await OpenOverviewAsync();
        await Expect(Page.GetByTestId("total-spend")).ToContainTextAsync("NT$10,200.00");
        await Expect(Page.GetByTestId("nights-covered")).ToContainTextAsync("3 / 3");
        await Expect(Page.GetByTestId("booking-progress")).ToContainTextAsync("1 / 1");
        await Expect(Page.GetByTestId("split-stays")).ToHaveTextAsync("NT$9,000.00");
        await Expect(Page.GetByTestId("top-expense").First).ToContainTextAsync("Hotel Gracery");
        await Expect(AttentionItem("Book teamLab")).ToHaveCountAsync(0);
        await Expect(Page.GetByTestId("attention-item").Filter(new() { HasText = "No stay for the night" }))
            .ToHaveCountAsync(0);
    }

    [Test]
    public async Task AttentionItem_OpensTheDayInTheTimeline()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await OpenOverviewAsync();

        await Page.GetByTestId("attention-item").Filter(new() { HasText = "Nothing planned" }).First.ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"view=timeline&day=2$"));
        await Expect(ScheduleDay(2)).ToBeInViewportAsync();
    }

    [Test]
    public async Task DayBar_ShowsTheBreakdownAndOpensTheDay()
    {
        await AddActivityAsync("Senso-ji", day: 2, "10:00", "11:00", cost: "500");
        await OpenOverviewAsync();

        var bar = Page.GetByTestId("day-bar").Nth(1);
        await bar.HoverAsync();
        await Expect(Page.GetByTestId("day-tooltip")).ToContainTextAsync("NT$500.00");

        await bar.ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex(@"day=2$"));
    }

    private ILocator AttentionItem(string title) =>
        Page.GetByTestId("attention-item").Filter(new() { HasText = title });

    private async Task OpenOverviewAsync()
    {
        await Page.GetByTestId("open-overview").ClickAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Spend by day" })).ToBeVisibleAsync();
    }

    private async Task ChooseBookingAsync(string label)
    {
        var select = Page.GetByTestId("booking-select");
        await select.GetByTestId("select-trigger").ClickAsync();
        await select.GetByRole(AriaRole.Option, new() { Name = label, Exact = true }).ClickAsync();
    }
}
