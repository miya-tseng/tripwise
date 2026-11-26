using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class ViewTests : SchedulePageTest
{
    [Test]
    public async Task SwitchToTable_ShowsTheSameActivitiesAndSurvivesReload()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await AddActivityAsync("Tokyo Tower", day: 2, "18:00", "19:00");

        await Page.GetByTestId("view-table").ClickAsync();

        await Expect(Page).ToHaveURLAsync(new Regex(@"\?view=table$"));
        await Expect(TableNames()).ToHaveTextAsync(["Senso-ji", "Tokyo Tower"]);
        await Expect(Page.GetByTestId("table-day")).ToHaveCountAsync(2);

        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("view-table")).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(TableNames()).ToHaveTextAsync(["Senso-ji", "Tokyo Tower"]);

        await Page.GetByTestId("view-board").ClickAsync();
        await Expect(Page.GetByTestId("board-card-name")).ToHaveTextAsync(["Senso-ji", "Tokyo Tower"]);
        await Expect(Page).ToHaveURLAsync(new Regex(@"\?view=board$"));
    }

    [Test]
    public async Task SortByCost_ShowsTheMostExpensiveFirst()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00", cost: "100");
        await AddActivityAsync("Tokyo Tower", day: 2, "18:00", "19:00", cost: "1200");
        await AddActivityAsync("Ueno Park", day: 1, "13:00", "14:00", cost: "500");
        await Page.GetByTestId("view-table").ClickAsync();

        await Page.GetByTestId("sort-cost").ClickAsync();

        await Expect(TableNames()).ToHaveTextAsync(["Tokyo Tower", "Ueno Park", "Senso-ji"]);
        await Expect(Page.GetByTestId("table-total")).ToHaveTextAsync("NT$1,800.00");

        await Page.GetByTestId("sort-name").ClickAsync();
        await Expect(TableNames()).ToHaveTextAsync(["Senso-ji", "Tokyo Tower", "Ueno Park"]);
    }

    [Test]
    public async Task ClickARow_OpensTheActivityToEdit()
    {
        await AddActivityAsync("Senso-ji", day: 1, "10:00", "11:00");
        await Page.GetByTestId("view-table").ClickAsync();

        await Page.GetByTestId("table-row").Filter(new() { HasText = "Senso-ji" }).ClickAsync();

        await Expect(Dialog()).ToBeVisibleAsync();
        await Expect(Page.GetByRole(AriaRole.Textbox, new() { Name = "Activity" })).ToHaveValueAsync("Senso-ji");
    }

    private ILocator TableNames() => Page.GetByTestId("table-name");
}
