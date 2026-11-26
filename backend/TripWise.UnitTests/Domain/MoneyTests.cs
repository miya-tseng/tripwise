using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class MoneyTests
{
    [TestCase("0")]
    [TestCase("0.01")]
    [TestCase("1500")]
    [TestCase("1234.5")]
    [TestCase("9999999999.99")]
    public void Constructor_WithValidAmount_KeepsAmount(string value)
    {
        var amount = decimal.Parse(value);

        Assert.That(new Money(amount).Amount, Is.EqualTo(amount));
    }

    [TestCase("-0.01")]
    [TestCase("-100")]
    public void Constructor_WithNegativeAmount_ThrowsDomainException(string value)
    {
        var ex = Assert.Throws<DomainException>(() => new Money(decimal.Parse(value)));

        Assert.That(ex.Message, Is.EqualTo("Amount cannot be negative."));
    }

    [TestCase("0.001")]
    [TestCase("12.345")]
    public void Constructor_WithMoreThanTwoDecimalPlaces_ThrowsDomainException(string value)
    {
        var ex = Assert.Throws<DomainException>(() => new Money(decimal.Parse(value)));

        Assert.That(ex.Message, Is.EqualTo("Amount cannot have more than 2 decimal places."));
    }

    [Test]
    public void Constructor_WithTrailingZeroDecimals_IsAllowed()
    {
        Assert.That(new Money(12.500m).Amount, Is.EqualTo(12.5m));
    }

    [Test]
    public void Constructor_OverMaxAmount_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => new Money(Money.MaxAmount + 0.01m));

        Assert.That(ex.Message, Does.StartWith("Amount cannot exceed"));
    }

    [Test]
    public void Zero_HasZeroAmount()
    {
        Assert.That(Money.Zero.Amount, Is.Zero);
    }

    [Test]
    public void Add_ReturnsSum()
    {
        Assert.That((new Money(10.25m) + new Money(4.75m)).Amount, Is.EqualTo(15m));
    }

    [Test]
    public void Add_DecimalsWithoutFloatingPointError()
    {
        Assert.That((new Money(0.1m) + new Money(0.2m)).Amount, Is.EqualTo(0.3m));
    }

    [Test]
    public void Sum_OfNothing_IsZero()
    {
        Assert.That(Money.Sum([]), Is.EqualTo(Money.Zero));
    }

    [Test]
    public void Sum_AddsEveryAmount()
    {
        var total = Money.Sum([new Money(0.1m), new Money(0.2m), new Money(1999.99m)]);

        Assert.That(total.Amount, Is.EqualTo(2000.29m));
    }

    [Test]
    public void Equals_WithSameAmount_ReturnsTrue()
    {
        var a = new Money(12.5m);
        var b = new Money(12.50m);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void Constructor_WithoutCurrency_UsesHomeCurrency()
    {
        Assert.That(new Money(10m).Currency, Is.EqualTo("TWD"));
    }

    [TestCase("JPY", "JPY")]
    [TestCase("usd", "USD")]
    [TestCase(" eur ", "EUR")]
    public void Constructor_WithCurrency_NormalizesCode(string currency, string expected)
    {
        Assert.That(new Money(10m, currency).Currency, Is.EqualTo(expected));
    }

    [TestCase("")]
    [TestCase("JP")]
    [TestCase("JPYY")]
    [TestCase("J1Y")]
    [TestCase(null)]
    public void Constructor_WithInvalidCurrency_ThrowsDomainException(string? currency)
    {
        var ex = Assert.Throws<DomainException>(() => new Money(10m, currency!));

        Assert.That(ex.Message, Is.EqualTo("Currency must be a 3-letter ISO 4217 code."));
    }

    [Test]
    public void Add_SameCurrency_KeepsCurrency()
    {
        var total = new Money(1000m, "JPY") + new Money(500m, "JPY");

        Assert.That(total, Is.EqualTo(new Money(1500m, "JPY")));
    }

    [Test]
    public void Add_DifferentCurrencies_ThrowsDomainException()
    {
        var ex = Assert.Throws<DomainException>(() => _ = new Money(1000m, "JPY") + new Money(10m));

        Assert.That(ex.Message, Is.EqualTo("Cannot add amounts in different currencies."));
    }

    [Test]
    public void Sum_OfForeignCurrency_KeepsThatCurrency()
    {
        Assert.That(Money.Sum([new Money(1000m, "JPY"), new Money(2000m, "JPY")]), Is.EqualTo(new Money(3000m, "JPY")));
    }

    [Test]
    public void SumByCurrency_GroupsAmountsWithHomeCurrencyFirstThenAlphabetical()
    {
        var totals = Money.SumByCurrency(
        [
            new Money(10m, "USD"),
            new Money(1000m, "JPY"),
            new Money(100m),
            new Money(2000m, "JPY"),
            new Money(5m, "EUR"),
        ]);

        Assert.That(totals, Is.EqualTo(new[]
        {
            new Money(100m),
            new Money(5m, "EUR"),
            new Money(3000m, "JPY"),
            new Money(10m, "USD"),
        }));
    }

    [Test]
    public void SumByCurrency_OfNothing_IsEmpty()
    {
        Assert.That(Money.SumByCurrency([]), Is.Empty);
    }

    [Test]
    public void Equals_WithDifferentCurrency_ReturnsFalse()
    {
        Assert.That(new Money(10m, "USD"), Is.Not.EqualTo(new Money(10m)));
    }
}
