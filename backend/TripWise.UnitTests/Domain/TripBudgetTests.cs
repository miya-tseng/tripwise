using TripWise.Api.Domain;

namespace TripWise.UnitTests.Domain;

public class TripBudgetTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);
    private static readonly DateOnly SecondDay = FirstDay.AddDays(1);

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
    }

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void AddActivity_WithoutCost_DefaultsToZero()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));

        Assert.That(activity.Cost, Is.EqualTo(Money.Zero));
    }

    [Test]
    public void AddActivity_WithCost_KeepsCost()
    {
        var activity = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));

        Assert.That(activity.Cost.Amount, Is.EqualTo(2200m));
    }

    [Test]
    public void UpdateActivity_ChangesCost()
    {
        var activity = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));

        _trip.UpdateActivity(activity.Id, "Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2500m));

        Assert.That(activity.Cost.Amount, Is.EqualTo(2500m));
    }

    [Test]
    public void UpdateActivity_WithoutCost_ResetsCostToZero()
    {
        var activity = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));

        _trip.UpdateActivity(activity.Id, "Shibuya Sky", FirstDay, Slot("17:00", "18:00"));

        Assert.That(activity.Cost, Is.EqualTo(Money.Zero));
    }

    [Test]
    public void UpdateActivity_RejectedByOverlap_KeepsOriginalCost()
    {
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));
        var activity = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));

        Assert.Throws<ActivityOverlapException>(() =>
            _trip.UpdateActivity(activity.Id, "Shibuya Sky", FirstDay, Slot("10:30", "11:30"), new Money(1m)));

        Assert.That(activity.Cost.Amount, Is.EqualTo(2200m));
    }

    [Test]
    public void Totals_WithNoActivities_AreZero()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_trip.GetDailyTotal(FirstDay), Is.EqualTo(Money.Zero));
            Assert.That(_trip.GetTotal(), Is.EqualTo(Money.Zero));
        });
    }

    [Test]
    public void Totals_WithOneActivity_EqualItsCost()
    {
        _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));

        Assert.Multiple(() =>
        {
            Assert.That(_trip.GetDailyTotal(FirstDay).Amount, Is.EqualTo(2200m));
            Assert.That(_trip.GetTotal().Amount, Is.EqualTo(2200m));
        });
    }

    [Test]
    public void Totals_WithManyActivities_AddUpPerDayAndOverall()
    {
        _trip.AddActivity("Tsukiji", FirstDay, Slot("07:00", "08:00"), new Money(1500.50m));
        _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"));
        _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));
        _trip.AddActivity("teamLab", SecondDay, Slot("13:00", "15:00"), new Money(3800.25m));

        Assert.Multiple(() =>
        {
            Assert.That(_trip.GetDailyTotal(FirstDay).Amount, Is.EqualTo(3700.50m));
            Assert.That(_trip.GetDailyTotal(SecondDay).Amount, Is.EqualTo(3800.25m));
            Assert.That(_trip.GetDailyTotal(FirstDay.AddDays(2)), Is.EqualTo(Money.Zero));
            Assert.That(_trip.GetTotal().Amount, Is.EqualTo(7500.75m));
        });
    }

    [Test]
    public void Totals_KeepDecimalPrecision()
    {
        _trip.AddActivity("Coffee", FirstDay, Slot("08:00", "08:30"), new Money(0.1m));
        _trip.AddActivity("Snack", FirstDay, Slot("09:00", "09:30"), new Money(0.2m));

        Assert.That(_trip.GetDailyTotal(FirstDay).Amount, Is.EqualTo(0.3m));
    }

    [Test]
    public void Totals_ExcludeRemovedActivities()
    {
        var removed = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));
        _trip.AddActivity("teamLab", SecondDay, Slot("13:00", "15:00"), new Money(3800m));

        _trip.RemoveActivity(removed.Id);

        Assert.Multiple(() =>
        {
            Assert.That(_trip.GetDailyTotal(FirstDay), Is.EqualTo(Money.Zero));
            Assert.That(_trip.GetTotal().Amount, Is.EqualTo(3800m));
        });
    }

    [Test]
    public void Totals_FollowAnActivityMovedToAnotherDay()
    {
        var activity = _trip.AddActivity("Shibuya Sky", FirstDay, Slot("17:00", "18:00"), new Money(2200m));

        _trip.UpdateActivity(activity.Id, "Shibuya Sky", SecondDay, Slot("17:00", "18:00"), new Money(2200m));

        Assert.Multiple(() =>
        {
            Assert.That(_trip.GetDailyTotal(FirstDay), Is.EqualTo(Money.Zero));
            Assert.That(_trip.GetDailyTotal(SecondDay).Amount, Is.EqualTo(2200m));
        });
    }
}
