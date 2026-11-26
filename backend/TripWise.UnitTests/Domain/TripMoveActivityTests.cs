using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripMoveActivityTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);
    private static readonly DateOnly SecondDay = FirstDay.AddDays(1);

    private static readonly Place SensoJi = new(
        "senso-ji-id", "Senso-ji", "Asakusa", 35.71, 139.79, "https://maps.google.com/?cid=1");

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
    }

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void MoveActivity_ToAnotherDay_KeepsTimeAndDuration()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"));

        _trip.MoveActivity(activity.Id, SecondDay, new TimeOnly(10, 0));

        Assert.Multiple(() =>
        {
            Assert.That(activity.Date, Is.EqualTo(SecondDay));
            Assert.That(activity.TimeSlot, Is.EqualTo(Slot("10:00", "11:30")));
        });
    }

    [Test]
    public void MoveActivity_ToAnotherTime_KeepsDuration()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"));

        _trip.MoveActivity(activity.Id, FirstDay, new TimeOnly(14, 15));

        Assert.That(activity.TimeSlot, Is.EqualTo(Slot("14:15", "15:45")));
    }

    [Test]
    public void MoveActivity_KeepsNameCostAndPlace()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), new Money(500m, "JPY"), SensoJi);

        _trip.MoveActivity(activity.Id, SecondDay, new TimeOnly(9, 0));

        Assert.Multiple(() =>
        {
            Assert.That(activity.Name, Is.EqualTo("Senso-ji"));
            Assert.That(activity.Cost, Is.EqualTo(new Money(500m, "JPY")));
            Assert.That(activity.Place, Is.EqualTo(SensoJi));
        });
    }

    [Test]
    public void MoveActivity_OverlappingAnother_ThrowsAndKeepsOriginal()
    {
        _trip.AddActivity("Ueno Park", SecondDay, Slot("10:00", "11:00"));
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"));

        Assert.Throws<ActivityOverlapException>(() => _trip.MoveActivity(activity.Id, SecondDay, new TimeOnly(9, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(activity.Date, Is.EqualTo(FirstDay));
            Assert.That(activity.TimeSlot, Is.EqualTo(Slot("10:00", "11:30")));
        });
    }

    [Test]
    public void MoveActivity_OverlappingOnlyItsOldTime_Succeeds()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "12:00"));

        _trip.MoveActivity(activity.Id, FirstDay, new TimeOnly(11, 0));

        Assert.That(activity.TimeSlot, Is.EqualTo(Slot("11:00", "13:00")));
    }

    [Test]
    public void MoveActivity_RightAfterAnother_IsAllowed()
    {
        _trip.AddActivity("Ueno Park", SecondDay, Slot("09:00", "10:00"));
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        _trip.MoveActivity(activity.Id, SecondDay, new TimeOnly(10, 0));

        Assert.That(activity.Date, Is.EqualTo(SecondDay));
    }

    [Test]
    public void MoveActivity_OutsideTripDates_Throws()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        var ex = Assert.Throws<DomainException>(() => _trip.MoveActivity(activity.Id, FirstDay.AddDays(5), new TimeOnly(10, 0)));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("Activity date must be within the trip dates."));
            Assert.That(activity.Date, Is.EqualTo(FirstDay));
        });
    }

    [TestCase("23:00")]
    [TestCase("22:31")]
    public void MoveActivity_EndingAfterMidnight_Throws(string start)
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"));

        var ex = Assert.Throws<DomainException>(() => _trip.MoveActivity(activity.Id, FirstDay, TimeOnly.Parse(start)));

        Assert.Multiple(() =>
        {
            Assert.That(ex.Message, Is.EqualTo("The activity would end after midnight."));
            Assert.That(activity.TimeSlot, Is.EqualTo(Slot("10:00", "11:30")));
        });
    }

    [Test]
    public void MoveActivity_EndingJustBeforeMidnight_IsAllowed()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:30"));

        _trip.MoveActivity(activity.Id, FirstDay, new TimeOnly(22, 29));

        Assert.That(activity.TimeSlot.End, Is.EqualTo(new TimeOnly(23, 59)));
    }

    [Test]
    public void MoveActivity_WithUnknownId_ThrowsNotFound()
    {
        Assert.Throws<ActivityNotFoundException>(() => _trip.MoveActivity(Guid.NewGuid(), FirstDay, new TimeOnly(10, 0)));
    }

    [Test]
    public void TimeSlot_Duration_IsEndMinusStart()
    {
        Assert.That(Slot("10:15", "12:00").Duration, Is.EqualTo(TimeSpan.FromMinutes(105)));
    }
}
