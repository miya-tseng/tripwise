using TripWise.Application.Currencies;
using TripWise.Domain;

namespace TripWise.UnitTests.Currencies;

public class ExchangeRatesTests
{
    private static ExchangeRates Rates(params (string Currency, decimal UnitsPerTwd)[] rates) =>
        new(DateTimeOffset.UnixEpoch, rates.ToDictionary(rate => rate.Currency, rate => rate.UnitsPerTwd));

    [Test]
    public void ToHomeCurrency_ForTwd_ReturnsSameAmount()
    {
        Assert.That(Rates().ToHomeCurrency(new Money(123.45m)), Is.EqualTo(123.45m));
    }

    [TestCase("3000", "4.8", "625")]
    [TestCase("2200", "4.8", "458.33")]
    [TestCase("10", "0.032", "312.5")]
    [TestCase("0.01", "4.8", "0")]
    [TestCase("1", "3", "0.33")]
    [TestCase("2", "3", "0.67")]
    public void ToHomeCurrency_DividesByRateAndRoundsToTwoDecimals(string amount, string unitsPerTwd, string expected)
    {
        var rates = Rates(("JPY", decimal.Parse(unitsPerTwd)));

        Assert.That(rates.ToHomeCurrency(new Money(decimal.Parse(amount), "JPY")), Is.EqualTo(decimal.Parse(expected)));
    }

    [Test]
    public void ToHomeCurrency_RoundsMidpointAwayFromZero()
    {
        var rates = Rates(("USD", 0.4m));

        Assert.That(rates.ToHomeCurrency(new Money(0.01m, "USD")), Is.EqualTo(0.03m));
    }

    [Test]
    public void ToHomeCurrency_ForUnknownCurrency_ReturnsNull()
    {
        Assert.That(Rates(("JPY", 4.8m)).ToHomeCurrency(new Money(10m, "EUR")), Is.Null);
    }

    [Test]
    public void Supports_HomeCurrencyEvenWhenNotListed()
    {
        Assert.That(Rates().Supports("TWD"), Is.True);
    }

    [TestCase("JPY", true)]
    [TestCase("jpy", true)]
    [TestCase("EUR", false)]
    public void Supports_ListedCurrencies(string currency, bool expected)
    {
        Assert.That(Rates(("JPY", 4.8m)).Supports(currency), Is.EqualTo(expected));
    }

    [Test]
    public void Constructor_IgnoresNonPositiveRates()
    {
        var rates = Rates(("JPY", 0m), ("USD", -1m), ("EUR", 0.029m));

        Assert.That(rates.Currencies, Is.EqualTo(new[] { "EUR", "TWD" }));
    }
}
