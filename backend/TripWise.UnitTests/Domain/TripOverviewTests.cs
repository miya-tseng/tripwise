using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripOverviewTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private static readonly Place SensoJi =
        new("senso-ji", "Senso-ji", "Asakusa", 35.71, 139.79, "https://maps.google.com/?cid=1");

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
    }

    private static TimeSlot Slot(int startHour) => new(new TimeOnly(startHour, 0), new TimeOnly(startHour + 1, 0));

    private Activity Add(
        string name,
        int day,
        int startHour,
        Money? cost = null,
        ActivityCategory category = ActivityCategory.Other,
        BookingStatus booking = BookingStatus.NotNeeded,
        Place? place = null) =>
        _trip.AddActivity(name, FirstDay.AddDays(day), Slot(startHour), cost, place, new ActivityDetails(category, booking));

    private Stay AddStay(string name, int checkInDay, int checkOutDay, Money? cost = null, BookingStatus booking = BookingStatus.NotNeeded) =>
        _trip.AddStay(new StayDetails(name, FirstDay.AddDays(checkInDay), FirstDay.AddDays(checkOutDay), cost: cost, bookingStatus: booking));

    [Test]
    public void EmptyTrip_NeedsAStayEveryNightAndHasNothingPlanned()
    {
        var overview = TripOverview.For(_trip);

        Assert.Multiple(() =>
        {
            Assert.That(overview.Days, Is.EqualTo(3));
            Assert.That(overview.Nights, Is.EqualTo(3));
            Assert.That(overview.ActivityCount, Is.Zero);
            Assert.That(overview.PlaceCount, Is.Zero);
            Assert.That(overview.NightsWithStay, Is.Zero);
            Assert.That(overview.NightsWithoutStay, Has.Count.EqualTo(3));
            Assert.That(overview.EmptyDays, Has.Count.EqualTo(3));
            Assert.That(overview.Booking, Is.EqualTo(new BookingProgress(0, 0, 0)));
            Assert.That(overview.SpendByCategory, Is.Empty);
            Assert.That(overview.Expenses, Is.Empty);
            Assert.That(overview.SpendByDay.Select(day => day.DayNumber), Is.EqualTo(new[] { 1, 2, 3 }));
        });
    }

    [Test]
    public void Attention_ListsWhatIsStillToBookAndWhatIsMissing()
    {
        var teamLab = Add("teamLab", 0, 10, booking: BookingStatus.ToBook);
        Add("Senso-ji", 0, 13, booking: BookingStatus.Booked);
        var inn = AddStay("Asakusa Inn", 0, 1, booking: BookingStatus.ToBook);

        var overview = TripOverview.For(_trip);

        Assert.Multiple(() =>
        {
            Assert.That(overview.ActivitiesToBook, Is.EqualTo(new[] { teamLab }));
            Assert.That(overview.StaysToBook, Is.EqualTo(new[] { inn }));
            Assert.That(overview.NightsWithoutStay, Is.EqualTo(new[] { FirstDay.AddDays(1), FirstDay.AddDays(2) }));
            Assert.That(overview.EmptyDays, Is.EqualTo(new[] { FirstDay.AddDays(1), FirstDay.AddDays(2) }));
            Assert.That(overview.NightsWithStay, Is.EqualTo(1));
        });
    }

    [Test]
    public void Booking_CountsActivitiesAndStaysThatNeedBooking()
    {
        Add("teamLab", 0, 10, booking: BookingStatus.ToBook);
        Add("Sushi", 0, 12, booking: BookingStatus.Paid);
        Add("Walk", 0, 14);
        AddStay("Hotel", 0, 3, booking: BookingStatus.Booked);

        var booking = TripOverview.For(_trip).Booking;

        Assert.Multiple(() =>
        {
            Assert.That(booking, Is.EqualTo(new BookingProgress(1, 1, 1)));
            Assert.That(booking.NeedingBooking, Is.EqualTo(3));
            Assert.That(booking.Done, Is.EqualTo(2));
        });
    }

    [Test]
    public void PlaceCount_CountsEachPlaceOnce()
    {
        Add("Senso-ji morning", 0, 9, place: SensoJi);
        Add("Senso-ji again", 1, 9, place: SensoJi);
        Add("Lunch", 1, 12);

        Assert.That(TripOverview.For(_trip).PlaceCount, Is.EqualTo(1));
    }

    [Test]
    public void SpendByCategory_GroupsActivityCostsAndCounts()
    {
        Add("Sushi", 0, 7, new Money(3000, "JPY"), ActivityCategory.Food);
        Add("Ramen", 1, 12, new Money(200, "TWD"), ActivityCategory.Food);
        Add("Senso-ji", 0, 9, category: ActivityCategory.Sightseeing);

        var spend = TripOverview.For(_trip).SpendByCategory;

        Assert.Multiple(() =>
        {
            Assert.That(spend.Select(item => item.Category), Is.EqualTo(new[] { ActivityCategory.Sightseeing, ActivityCategory.Food }));
            Assert.That(spend[0].ActivityCount, Is.EqualTo(1));
            Assert.That(spend[0].Costs, Is.Empty);
            Assert.That(spend[1].ActivityCount, Is.EqualTo(2));
            Assert.That(spend[1].Costs, Is.EqualTo(new[] { new Money(3000, "JPY"), new Money(200, "TWD") }));
        });
    }

    [Test]
    public void SpendByDay_SplitsActivitiesAndTheNightlyStayCost()
    {
        Add("Sushi", 0, 7, new Money(500, "TWD"));
        AddStay("Hotel", 0, 2, new Money(9000, "TWD"));
        AddStay("Free couch", 2, 3);

        var days = TripOverview.For(_trip).SpendByDay;

        Assert.Multiple(() =>
        {
            Assert.That(days[0].ActivityCosts, Is.EqualTo(new[] { new Money(500, "TWD") }));
            Assert.That(days[0].ActivityCount, Is.EqualTo(1));
            Assert.That(days[0].StayCost, Is.EqualTo(new Money(4500, "TWD")));
            Assert.That(days[1].ActivityCosts, Is.Empty);
            Assert.That(days[1].StayCost, Is.EqualTo(new Money(4500, "TWD")));
            Assert.That(days[2].StayCost, Is.Null);
        });
    }

    [Test]
    public void Expenses_IncludeEveryPaidActivityAndStay()
    {
        var sushi = Add("Sushi", 1, 7, new Money(500, "TWD"));
        Add("Walk", 1, 9);
        var hotel = AddStay("Hotel", 0, 2, new Money(9000, "TWD"));

        var expenses = TripOverview.For(_trip).Expenses;

        Assert.That(expenses, Is.EquivalentTo(new[]
        {
            new Expense(ExpenseKind.Activity, sushi.Id, "Sushi", FirstDay.AddDays(1), new Money(500, "TWD")),
            new Expense(ExpenseKind.Stay, hotel.Id, "Hotel", FirstDay, new Money(9000, "TWD")),
        }));
    }
}
