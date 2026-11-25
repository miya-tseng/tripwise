using TripWise.Api.Domain;

namespace TripWise.UnitTests.Domain;

public class TripActivityTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);
    private static readonly DateOnly SecondDay = FirstDay.AddDays(1);
    private static readonly DateOnly LastDay = FirstDay.AddDays(2);

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, LastDay);
    }

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void NewTrip_HasNoActivities()
    {
        Assert.That(_trip.Activities, Is.Empty);
    }

    [Test]
    public void AddActivity_WithValidInput_AddsActivity()
    {
        var slot = Slot("10:00", "11:00");

        var activity = _trip.AddActivity("Senso-ji", FirstDay, slot);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(activity.Name, Is.EqualTo("Senso-ji"));
            Assert.That(activity.Date, Is.EqualTo(FirstDay));
            Assert.That(activity.TimeSlot, Is.EqualTo(slot));
            Assert.That(_trip.Activities, Is.EquivalentTo(new[] { activity }));
        });
    }

    [Test]
    public void AddActivity_WithSurroundingSpaces_TrimsName()
    {
        var activity = _trip.AddActivity("  Senso-ji  ", FirstDay, Slot("10:00", "11:00"));

        Assert.That(activity.Name, Is.EqualTo("Senso-ji"));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void AddActivity_WithMissingName_ThrowsDomainException(string? name)
    {
        var ex = Assert.Throws<DomainException>(() => _trip.AddActivity(name!, FirstDay, Slot("10:00", "11:00")));

        Assert.That(ex.Message, Is.EqualTo("Activity name is required."));
    }

    [Test]
    public void AddActivity_WithNameAtMaxLength_Succeeds()
    {
        var name = new string('a', Activity.MaxNameLength);

        var activity = _trip.AddActivity(name, FirstDay, Slot("10:00", "11:00"));

        Assert.That(activity.Name, Has.Length.EqualTo(Activity.MaxNameLength));
    }

    [Test]
    public void AddActivity_WithNameOverMaxLength_ThrowsDomainException()
    {
        var name = new string('a', Activity.MaxNameLength + 1);

        var ex = Assert.Throws<DomainException>(() => _trip.AddActivity(name, FirstDay, Slot("10:00", "11:00")));

        Assert.That(ex.Message, Is.EqualTo("Activity name cannot exceed 100 characters."));
    }

    [TestCase(0)]
    [TestCase(2)]
    public void AddActivity_OnFirstOrLastTripDay_Succeeds(int daysFromStart)
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay.AddDays(daysFromStart), Slot("10:00", "11:00"));

        Assert.That(_trip.Activities, Does.Contain(activity));
    }

    [TestCase(-1)]
    [TestCase(3)]
    public void AddActivity_OutsideTripDates_ThrowsDomainException(int daysFromStart)
    {
        var ex = Assert.Throws<DomainException>(() =>
            _trip.AddActivity("Senso-ji", FirstDay.AddDays(daysFromStart), Slot("10:00", "11:00")));

        Assert.That(ex.Message, Is.EqualTo("Activity date must be within the trip dates."));
    }

    [TestCase("10:00", "11:00")]
    [TestCase("10:30", "11:30")]
    [TestCase("09:30", "10:30")]
    [TestCase("10:15", "10:45")]
    [TestCase("09:00", "12:00")]
    public void AddActivity_OverlappingOnSameDay_ThrowsActivityOverlapException(string start, string end)
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        Assert.Throws<ActivityOverlapException>(() => _trip.AddActivity("Ueno Park", FirstDay, Slot(start, end)));
    }

    [Test]
    public void AddActivity_Overlapping_DoesNotAddActivity()
    {
        var existing = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        Assert.Throws<ActivityOverlapException>(() => _trip.AddActivity("Ueno Park", FirstDay, Slot("10:30", "11:30")));

        Assert.That(_trip.Activities, Is.EquivalentTo(new[] { existing }));
    }

    [Test]
    public void AddActivity_Overlapping_MessageNamesConflictingActivity()
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        var ex = Assert.Throws<ActivityOverlapException>(() =>
            _trip.AddActivity("Ueno Park", FirstDay, Slot("10:30", "11:30")));

        Assert.That(ex.Message, Is.EqualTo("The activity overlaps with \"Senso-ji\" (10:00-11:00) on 2021-05-01."));
    }

    [TestCase("09:00", "10:00")]
    [TestCase("11:00", "12:00")]
    public void AddActivity_BackToBackOnSameDay_Succeeds(string start, string end)
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        _trip.AddActivity("Ueno Park", FirstDay, Slot(start, end));

        Assert.That(_trip.Activities, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddActivity_SameTimeOnDifferentDay_Succeeds()
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        _trip.AddActivity("Ueno Park", SecondDay, Slot("10:00", "11:00"));

        Assert.That(_trip.Activities, Has.Count.EqualTo(2));
    }

    [Test]
    public void AddActivity_WithNullTimeSlot_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _trip.AddActivity("Senso-ji", FirstDay, null!));
    }

    [Test]
    public void UpdateActivity_WithValidInput_UpdatesActivity()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));
        var newSlot = Slot("14:00", "15:30");

        _trip.UpdateActivity(activity.Id, "  Tokyo Tower  ", SecondDay, newSlot);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Name, Is.EqualTo("Tokyo Tower"));
            Assert.That(activity.Date, Is.EqualTo(SecondDay));
            Assert.That(activity.TimeSlot, Is.EqualTo(newSlot));
        });
    }

    [Test]
    public void UpdateActivity_OverlappingOnlyItself_Succeeds()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        _trip.UpdateActivity(activity.Id, "Senso-ji", FirstDay, Slot("10:30", "11:30"));

        Assert.That(activity.TimeSlot, Is.EqualTo(Slot("10:30", "11:30")));
    }

    [Test]
    public void UpdateActivity_OverlappingAnotherActivity_ThrowsAndKeepsOriginalValues()
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));
        var activity = _trip.AddActivity("Ueno Park", FirstDay, Slot("13:00", "14:00"));

        Assert.Throws<ActivityOverlapException>(() =>
            _trip.UpdateActivity(activity.Id, "Ueno Zoo", FirstDay, Slot("10:30", "11:30")));

        Assert.Multiple(() =>
        {
            Assert.That(activity.Name, Is.EqualTo("Ueno Park"));
            Assert.That(activity.TimeSlot, Is.EqualTo(Slot("13:00", "14:00")));
        });
    }

    [Test]
    public void UpdateActivity_MovingToDayWithConflict_ThrowsActivityOverlapException()
    {
        _trip.AddActivity("Senso-ji", SecondDay, Slot("10:00", "11:00"));
        var activity = _trip.AddActivity("Ueno Park", FirstDay, Slot("10:00", "11:00"));

        Assert.Throws<ActivityOverlapException>(() =>
            _trip.UpdateActivity(activity.Id, "Ueno Park", SecondDay, Slot("10:00", "11:00")));
    }

    [Test]
    public void UpdateActivity_OutsideTripDates_ThrowsAndKeepsOriginalDate()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        var ex = Assert.Throws<DomainException>(() =>
            _trip.UpdateActivity(activity.Id, "Senso-ji", LastDay.AddDays(1), Slot("10:00", "11:00")));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("Activity date must be within the trip dates."));
            Assert.That(activity.Date, Is.EqualTo(FirstDay));
        });
    }

    [Test]
    public void UpdateActivity_WithMissingName_ThrowsAndKeepsOriginalName()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        Assert.Throws<DomainException>(() =>
            _trip.UpdateActivity(activity.Id, " ", FirstDay, Slot("12:00", "13:00")));

        Assert.Multiple(() =>
        {
            Assert.That(activity.Name, Is.EqualTo("Senso-ji"));
            Assert.That(activity.TimeSlot, Is.EqualTo(Slot("10:00", "11:00")));
        });
    }

    [Test]
    public void UpdateActivity_WithUnknownId_ThrowsActivityNotFoundException()
    {
        Assert.Throws<ActivityNotFoundException>(() =>
            _trip.UpdateActivity(Guid.NewGuid(), "Senso-ji", FirstDay, Slot("10:00", "11:00")));
    }

    [Test]
    public void RemoveActivity_RemovesOnlyThatActivity()
    {
        var removed = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));
        var kept = _trip.AddActivity("Ueno Park", FirstDay, Slot("13:00", "14:00"));

        _trip.RemoveActivity(removed.Id);

        Assert.That(_trip.Activities, Is.EquivalentTo(new[] { kept }));
    }

    [Test]
    public void RemoveActivity_FreesTimeSlotForNewActivity()
    {
        var removed = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));
        _trip.RemoveActivity(removed.Id);

        var added = _trip.AddActivity("Ueno Park", FirstDay, Slot("10:00", "11:00"));

        Assert.That(_trip.Activities, Is.EquivalentTo(new[] { added }));
    }

    [Test]
    public void RemoveActivity_WithUnknownId_ThrowsActivityNotFoundException()
    {
        Assert.Throws<ActivityNotFoundException>(() => _trip.RemoveActivity(Guid.NewGuid()));
    }
}
