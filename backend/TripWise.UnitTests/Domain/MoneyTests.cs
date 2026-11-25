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
}
