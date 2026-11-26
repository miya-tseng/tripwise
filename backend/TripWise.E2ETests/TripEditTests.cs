using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class TripEditTests : SchedulePageTest
{
    [Test]
    public async Task EditTrip_RenamesAndExtendsDates()
    {
        var newName = $"{TripName} edited"[..20];

        await OpenEditDialogAsync();
        await TripDialog().GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync(newName);
        await SelectDatesAsync(FirstDay, FirstDay.AddDays(4));
        await Expect(TripDialog().GetByTestId("edit-dates-hint")).ToHaveTextAsync("5 days");
        await TripDialog().GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(TripDialog()).ToBeHiddenAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = newName, Level = 1 })).ToBeVisibleAsync();
        await Expect(Page.GetByTestId("trip-days")).ToHaveTextAsync("5");
        await Expect(Page.GetByTestId("schedule-day")).ToHaveCountAsync(5);

        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("trip-dates")).ToHaveTextAsync("May 1 – 5, 2030");
    }

    [Test]
    public async Task EditTrip_ShrinkingDatesWithActivityOutside_ShowsConflict()
    {
        await AddActivityAsync("Tokyo Tower", day: 3, "18:00", "19:00");

        await OpenEditDialogAsync();
        await SelectDatesAsync(FirstDay, FirstDay.AddDays(1));
        await TripDialog().GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(TripDialog().GetByTestId("edit-trip-error"))
            .ToHaveTextAsync("1 activity falls outside the new dates. Move or delete it first.");
        await TripDialog().GetByRole(AriaRole.Button, new() { Name = "Cancel" }).ClickAsync();

        await Expect(Page.GetByTestId("schedule-day")).ToHaveCountAsync(3);
        await Expect(ActivityNames(day: 3)).ToHaveTextAsync(["Tokyo Tower"]);
    }

    [Test]
    public async Task EditTrip_ShrinkingDatesAfterMovingActivity_Succeeds()
    {
        await AddActivityAsync("Tokyo Tower", day: 3, "18:00", "19:00");
        await ActivityItem("Tokyo Tower").GetByTestId("edit-activity").ClickAsync();
        await ChooseDayAsync(1);
        await SubmitButton("Save").ClickAsync();
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Tokyo Tower"]);

        await OpenEditDialogAsync();
        await SelectDatesAsync(FirstDay, FirstDay.AddDays(1));
        await TripDialog().GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();

        await Expect(TripDialog()).ToBeHiddenAsync();
        await Expect(Page.GetByTestId("schedule-day")).ToHaveCountAsync(2);
        await Expect(ActivityNames(day: 1)).ToHaveTextAsync(["Tokyo Tower"]);
    }

    [Test]
    public async Task EditTrip_Cancel_KeepsOriginalDetails()
    {
        await OpenEditDialogAsync();
        await TripDialog().GetByRole(AriaRole.Textbox, new() { Name = "Name" }).FillAsync("Not saved");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(TripDialog()).ToBeHiddenAsync();
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = TripName, Level = 1 })).ToBeVisibleAsync();
    }

    [Test]
    public async Task EditTrip_SetsTheDestination()
    {
        await RequireFakePlacesAsync();

        await OpenEditDialogAsync();
        await TripDialog().GetByRole(AriaRole.Combobox, new() { Name = "Destination" }).FillAsync("Tokyo");
        await Page.GetByRole(AriaRole.Option).Filter(new() { HasText = "Japan" }).First.ClickAsync();
        await TripDialog().GetByRole(AriaRole.Button, new() { Name = "Save" }).ClickAsync();
        await Expect(TripDialog()).ToBeHiddenAsync();

        await Page.ReloadAsync();
        await OpenEditDialogAsync();
        await Expect(TripDialog().GetByTestId("selected-place-name")).ToHaveTextAsync("Tokyo");
    }

    private async Task OpenEditDialogAsync()
    {
        await Page.GetByTestId("edit-trip").ClickAsync();
        await Expect(TripDialog()).ToBeVisibleAsync();
    }

    private async Task SelectDatesAsync(DateOnly start, DateOnly end)
    {
        await TripDialog().Locator("input[name='edit-dates']").ClickAsync();
        await DayCell(start).ClickAsync();
        await DayCell(end).ClickAsync();
    }

    private ILocator TripDialog() => Page.GetByRole(AriaRole.Dialog, new() { Name = "Edit trip" });

    private ILocator DayCell(DateOnly date) => Page.Locator($"[data-test-id='dp-{date:yyyy-MM-dd}']");
}
