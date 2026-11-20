using TripWise.Api.Domain;

namespace TripWise.UnitTests.Domain;

public class DateRangeTests
{
    private static readonly DateOnly Start = new(2021, 05, 01);

    [Test]
    public void Constructor_WithSameStartAndEnd_HasOneDay()
    {
        var range = new DateRange(Start, Start);

        Assert.That(range.Days, Is.EqualTo(1));
    }

    [TestCase(1, 2)]
    [TestCase(2, 3)]
    [TestCase(29, 30)]
    public void Days_CountsStartAndEndDate(int daysToAdd, int expectedDays)
    {
        var range = new DateRange(Start, Start.AddDays(daysToAdd));

        Assert.That(range.Days, Is.EqualTo(expectedDays));
    }

    [Test]
    public void Days_AcrossMonthAndYear_CountsCorrectly()
    {
        var range = new DateRange(new DateOnly(2026, 12, 30), new DateOnly(2027, 1, 2));

        Assert.That(range.Days, Is.EqualTo(4));
    }

    [Test]
    public void Constructor_WithEndBeforeStart_ThrowsDomainException()
    {
        var end = Start.AddDays(-1);

        var ex = Assert.Throws<DomainException>(() => new DateRange(Start, end));

        Assert.That(ex.Message, Is.EqualTo("End date must be on or after start date."));
    }

    [Test]
    public void Constructor_SetsStartAndEnd()
    {
        var end = Start.AddDays(2);

        var range = new DateRange(Start, end);

        Assert.Multiple(() =>
        {
            Assert.That(range.Start, Is.EqualTo(Start));
            Assert.That(range.End, Is.EqualTo(end));
        });
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    public void Contains_DateInsideRange_ReturnsTrue(int daysFromStart)
    {
        var range = new DateRange(Start, Start.AddDays(2));

        Assert.That(range.Contains(Start.AddDays(daysFromStart)), Is.True);
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void Contains_DateOutsideRange_ReturnsFalse(int daysFromStart)
    {
        var range = new DateRange(Start, Start.AddDays(2));

        Assert.That(range.Contains(Start.AddDays(daysFromStart)), Is.False);
    }

    [Test]
    public void Equals_WithSameDates_ReturnsTrue()
    {
        var a = new DateRange(Start, Start.AddDays(2));
        var b = new DateRange(Start, Start.AddDays(2));

        Assert.That(a, Is.EqualTo(b));
    }
}