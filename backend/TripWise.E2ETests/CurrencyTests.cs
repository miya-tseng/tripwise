using System.Globalization;
using Microsoft.Playwright;

namespace TripWise.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class CurrencyTests : SchedulePageTest
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    private Dictionary<string, decimal> _unitsPerTwd = null!;

    [SetUp]
    public async Task LoadExchangeRates()
    {
        var response = await Page.APIRequest.GetAsync("/api/exchange-rates");
        var rates = (await response.JsonAsync())!.Value;

        if (!rates.GetProperty("available").GetBoolean())
        {
            Assert.Ignore("Exchange rates are unavailable, so TWD totals cannot be verified.");
        }

        _unitsPerTwd = rates.GetProperty("unitsPerBaseCurrency")
            .EnumerateObject()
            .ToDictionary(rate => rate.Name, rate => rate.Value.GetDecimal());
    }

    [Test]
    public async Task AddCostsInSeveralCurrencies_ShowsOriginalAmountsAndTwdTotals()
    {
        await AddActivityInCurrencyAsync("Tsukiji Market", day: 1, "07:30", "08:30", cost: "3000", currency: "JPY");
        await AddActivityInCurrencyAsync("Senso-ji", day: 1, "10:00", "11:00", cost: "150", currency: "TWD");
        await AddActivityInCurrencyAsync("Airport bus", day: 2, "13:00", "15:00", cost: "12.5", currency: "USD");

        var tsukiji = ToTwd(3000m, "JPY");
        var bus = ToTwd(12.5m, "USD");

        await Expect(ScheduleDay(1).GetByTestId("activity-cost")).ToHaveTextAsync(["¥3,000", "NT$150.00"]);
        await Expect(ScheduleDay(2).GetByTestId("activity-cost")).ToHaveTextAsync(["$12.50"]);
        await Expect(ScheduleDay(1).GetByTestId("activity-cost-twd")).ToHaveTextAsync([$"≈ {Twd(tsukiji)}"]);
        await Expect(ScheduleDay(2).GetByTestId("activity-cost-twd")).ToHaveTextAsync([$"≈ {Twd(bus)}"]);
        await Expect(Page.GetByTestId("day-total"))
            .ToHaveTextAsync([Twd(tsukiji + 150m), Twd(bus), Twd(0m)]);
        await Expect(Page.GetByTestId("trip-total")).ToHaveTextAsync(Twd(tsukiji + 150m + bus));

        await Page.ReloadAsync();
        await Expect(Page.GetByTestId("trip-total")).ToHaveTextAsync(Twd(tsukiji + 150m + bus));
        await Expect(ScheduleDay(1).GetByTestId("activity-cost")).ToHaveTextAsync(["¥3,000", "NT$150.00"]);
    }

    [Test]
    public async Task ChangeActivityCurrency_UpdatesTwdTotal()
    {
        await AddActivityInCurrencyAsync("Shibuya Sky", day: 1, "17:00", "18:00", cost: "2200", currency: "JPY");

        await ActivityItem("Shibuya Sky").GetByTestId("edit-activity").ClickAsync();
        await CostInput().FillAsync("20");
        await ChooseCurrencyAsync("USD");
        await SubmitButton("Save").ClickAsync();

        await Expect(ActivityItem("Shibuya Sky").GetByTestId("activity-cost")).ToHaveTextAsync("$20.00");
        await Expect(Page.GetByTestId("trip-total")).ToHaveTextAsync(Twd(ToTwd(20m, "USD")));
    }

    [Test]
    public async Task ForeignCost_IsPreviewedInTwdBeforeSaving()
    {
        await OpenAddDialogAsync();
        await CostInput().FillAsync("2200");
        await ChooseCurrencyAsync("JPY");

        await Expect(Page.GetByTestId("cost-preview")).ToHaveTextAsync($"≈ {Twd(ToTwd(2200m, "JPY"))}");
    }

    [Test]
    public async Task LastUsedCurrency_IsRememberedForTheTrip()
    {
        await AddActivityInCurrencyAsync("Shibuya Sky", day: 1, "17:00", "18:00", cost: "2200", currency: "JPY");

        await OpenAddDialogAsync();
        await Expect(CurrencyTrigger()).ToContainTextAsync("JPY");

        await Page.ReloadAsync();
        await OpenAddDialogAsync();
        await Expect(CurrencyTrigger()).ToContainTextAsync("JPY");
    }

    [Test]
    public async Task RatesSource_IsShownWithTheSchedule()
    {
        await Expect(Page.GetByTestId("rates-note")).ToContainTextAsync("Rates by Exchange Rate API");
    }

    private async Task AddActivityInCurrencyAsync(string name, int day, string start, string end, string cost, string currency)
    {
        await TypeCustomActivityAsync(name);
        await ChooseDayAsync(day);
        await PickTimeAsync("Start", start);
        await PickTimeAsync("End", end);
        await CostInput().FillAsync(cost);
        await ChooseCurrencyAsync(currency);
        await SubmitAsync("Add", name);
    }

    private async Task ChooseCurrencyAsync(string currency)
    {
        await CurrencyTrigger().ClickAsync();
        await Page.GetByTestId("currency-select").GetByRole(AriaRole.Option)
            .Filter(new() { HasText = currency }).ClickAsync();
        await Expect(CurrencyTrigger()).ToContainTextAsync(currency);
    }

    private ILocator CurrencyTrigger() => Page.GetByTestId("currency-select").GetByTestId("select-trigger");

    private decimal ToTwd(decimal amount, string currency) =>
        currency == "TWD"
            ? amount
            : decimal.Round(amount / _unitsPerTwd[currency], 2, MidpointRounding.AwayFromZero);

    private static string Twd(decimal amount) => "NT$" + amount.ToString("#,##0.00", English);
}
