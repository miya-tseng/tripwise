using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripTests
{
    private static readonly DateOnly Start = new(2021, 05, 01);

    [Test]
    public void Create_WithValidInput_CreatesTrip()
    {
        var end = Start.AddDays(4);

        var trip = Trip.Create("Tokyo", Start, end);

        Assert.Multiple(() =>
        {
            Assert.That(trip.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(trip.Name, Is.EqualTo("Tokyo"));
            Assert.That(trip.Period, Is.EqualTo(new DateRange(Start, end)));
        });
    }

    [Test]
    public void Create_TwoTrips_HaveDifferentIds()
    {
        var first = Trip.Create("Tokyo", Start, Start);
        var second = Trip.Create("Tokyo", Start, Start);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithMissingName_ThrowsDomainException(string? name)
    {
        var ex = Assert.Throws<DomainException>(() => Trip.Create(name!, Start, Start));

        Assert.That(ex.Message, Is.EqualTo("Trip name is required."));
    }

    [Test]
    public void Create_WithSurroundingSpaces_TrimsName()
    {
        var trip = Trip.Create("  Tokyo  ", Start, Start);

        Assert.That(trip.Name, Is.EqualTo("Tokyo"));
    }

    [Test]
    public void Create_WithNameAtMaxLength_Succeeds()
    {
        var name = new string('a', Trip.MaxNameLength);

        var trip = Trip.Create(name, Start, Start);

        Assert.That(trip.Name, Has.Length.EqualTo(Trip.MaxNameLength));
    }

    [Test]
    public void Create_WithNameOverMaxLength_ThrowsDomainException()
    {
        var name = new string('a', Trip.MaxNameLength + 1);

        var ex = Assert.Throws<DomainException>(() => Trip.Create(name, Start, Start));

        Assert.That(ex.Message, Is.EqualTo("Trip name cannot exceed 100 characters."));
    }

    [Test]
    public void Create_WithNameAtMaxLengthAfterTrimming_Succeeds()
    {
        var name = $"  {new string('a', Trip.MaxNameLength)}  ";

        var trip = Trip.Create(name, Start, Start);

        Assert.That(trip.Name, Has.Length.EqualTo(Trip.MaxNameLength));
    }

    [Test]
    public void Create_SameDayTrip_HasOneDay()
    {
        var trip = Trip.Create("Day trip", Start, Start);

        Assert.That(trip.Period.Days, Is.EqualTo(1));
    }

    [Test]
    public void Create_WithMaxDays_Succeeds()
    {
        var end = Start.AddDays(Trip.MaxDays - 1);

        var trip = Trip.Create("Tokyo", Start, end);

        Assert.That(trip.Period.Days, Is.EqualTo(Trip.MaxDays));
    }

    [Test]
    public void Create_WithMoreThanMaxDays_ThrowsDomainException()
    {
        var end = Start.AddDays(Trip.MaxDays);

        var ex = Assert.Throws<DomainException>(() => Trip.Create("Tokyo", Start, end));

        Assert.That(ex.Message, Is.EqualTo("A trip cannot be longer than 30 days."));
    }

    [Test]
    public void Create_WithEndBeforeStart_ThrowsDomainException()
    {
        var end = Start.AddDays(-1);

        Assert.Throws<DomainException>(() => Trip.Create("Tokyo", Start, end));
    }
}
