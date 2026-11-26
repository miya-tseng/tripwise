using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripEditTests
{
    private static readonly DateOnly Start = new(2021, 05, 01);

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", Start, Start.AddDays(2));
    }

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void Rename_WithValidName_ChangesNameAndTrims()
    {
        _trip.Rename("  Osaka  ");

        Assert.That(_trip.Name, Is.EqualTo("Osaka"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Rename_WithMissingName_ThrowsAndKeepsName(string? name)
    {
        var ex = Assert.Throws<DomainException>(() => _trip.Rename(name!));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("Trip name is required."));
            Assert.That(_trip.Name, Is.EqualTo("Tokyo"));
        });
    }

    [Test]
    public void Rename_WithNameOverMaxLength_ThrowsAndKeepsName()
    {
        var ex = Assert.Throws<DomainException>(() => _trip.Rename(new string('a', Trip.MaxNameLength + 1)));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("Trip name cannot exceed 100 characters."));
            Assert.That(_trip.Name, Is.EqualTo("Tokyo"));
        });
    }

    [Test]
    public void Reschedule_ExtendingDates_ChangesPeriod()
    {
        _trip.AddActivity("Senso-ji", Start, Slot("10:00", "11:00"));

        _trip.Reschedule(Start.AddDays(-1), Start.AddDays(4));

        Assert.That(_trip.Period, Is.EqualTo(new DateRange(Start.AddDays(-1), Start.AddDays(4))));
    }

    [Test]
    public void Reschedule_ShrinkingDatesWithoutActivitiesOutside_ChangesPeriod()
    {
        _trip.AddActivity("Senso-ji", Start.AddDays(1), Slot("10:00", "11:00"));

        _trip.Reschedule(Start.AddDays(1), Start.AddDays(1));

        Assert.That(_trip.Period.Days, Is.EqualTo(1));
    }

    [Test]
    public void Reschedule_MovingToOtherDatesWithoutActivities_ChangesPeriod()
    {
        var later = Start.AddMonths(1);

        _trip.Reschedule(later, later.AddDays(5));

        Assert.That(_trip.Period, Is.EqualTo(new DateRange(later, later.AddDays(5))));
    }

    [Test]
    public void Reschedule_LeavingOneActivityOutside_ThrowsAndKeepsPeriod()
    {
        _trip.AddActivity("Senso-ji", Start, Slot("10:00", "11:00"));
        _trip.AddActivity("Tokyo Tower", Start.AddDays(2), Slot("18:00", "19:00"));

        var ex = Assert.Throws<TripDatesConflictException>(() => _trip.Reschedule(Start, Start.AddDays(1)));

        Assert.Multiple(() =>
        {
            Assert.That(ex.ActivitiesOutside, Is.EqualTo(1));
            Assert.That(ex.Message, Is.EqualTo("1 activity falls outside the new dates. Move or delete it first."));
            Assert.That(_trip.Period, Is.EqualTo(new DateRange(Start, Start.AddDays(2))));
        });
    }

    [Test]
    public void Reschedule_LeavingSeveralActivitiesOutside_CountsThem()
    {
        _trip.AddActivity("Tsukiji", Start, Slot("07:00", "08:00"));
        _trip.AddActivity("Senso-ji", Start, Slot("10:00", "11:00"));
        _trip.AddActivity("Shibuya", Start.AddDays(1), Slot("17:00", "18:00"));

        var ex = Assert.Throws<TripDatesConflictException>(() => _trip.Reschedule(Start.AddDays(2), Start.AddDays(2)));

        Assert.Multiple(() =>
        {
            Assert.That(ex.ActivitiesOutside, Is.EqualTo(3));
            Assert.That(ex.Message, Is.EqualTo("3 activities fall outside the new dates. Move or delete them first."));
        });
    }

    [Test]
    public void Reschedule_LongerThanMaxDays_ThrowsAndKeepsPeriod()
    {
        var ex = Assert.Throws<DomainException>(() => _trip.Reschedule(Start, Start.AddDays(Trip.MaxDays)));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("A trip cannot be longer than 30 days."));
            Assert.That(_trip.Period.Days, Is.EqualTo(3));
        });
    }

    [Test]
    public void Reschedule_WithEndBeforeStart_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => _trip.Reschedule(Start, Start.AddDays(-1)));
    }

    [Test]
    public void Reschedule_ThenAddActivity_UsesNewDates()
    {
        _trip.Reschedule(Start, Start.AddDays(5));

        var activity = _trip.AddActivity("Nikko", Start.AddDays(5), Slot("09:00", "17:00"));

        Assert.That(_trip.Activities, Does.Contain(activity));
    }

    [Test]
    public void TripDatesConflictException_IsADomainException()
    {
        Assert.That(new TripDatesConflictException(2), Is.InstanceOf<DomainException>());
    }
}
