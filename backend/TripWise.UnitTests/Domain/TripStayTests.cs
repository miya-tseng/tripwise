using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripStayTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);
    private static readonly DateOnly LastDay = FirstDay.AddDays(3);

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, LastDay);
    }

    private static StayDetails Details(
        string name,
        int checkInDay,
        int checkOutDay,
        Money? cost = null,
        BookingStatus bookingStatus = BookingStatus.NotNeeded) =>
        new(name, FirstDay.AddDays(checkInDay), FirstDay.AddDays(checkOutDay), cost: cost, bookingStatus: bookingStatus);

    [Test]
    public void NewTrip_HasNoStays()
    {
        Assert.That(_trip.Stays, Is.Empty);
    }

    [Test]
    public void Nights_ListsEveryTripDayAsANight()
    {
        Assert.That(_trip.Nights, Is.EqualTo(new[] { FirstDay, FirstDay.AddDays(1), FirstDay.AddDays(2), LastDay }));
    }

    [Test]
    public void AddStay_WithValidDetails_AddsStay()
    {
        var stay = _trip.AddStay(Details("Hotel Gracery", 0, 2, new Money(9000m), BookingStatus.Booked));

        Assert.Multiple(() =>
        {
            Assert.That(stay.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(stay.Name, Is.EqualTo("Hotel Gracery"));
            Assert.That(stay.CheckIn, Is.EqualTo(FirstDay));
            Assert.That(stay.CheckOut, Is.EqualTo(FirstDay.AddDays(2)));
            Assert.That(stay.Nights, Is.EqualTo(2));
            Assert.That(stay.BookingStatus, Is.EqualTo(BookingStatus.Booked));
            Assert.That(_trip.Stays, Is.EquivalentTo(new[] { stay }));
        });
    }

    [Test]
    public void AddStay_CheckingOutTheDayAfterTheTrip_IsAllowed()
    {
        var stay = _trip.AddStay(Details("Airport hotel", 3, 4));

        Assert.That(stay.CheckOut, Is.EqualTo(_trip.LastCheckOut));
    }

    [TestCase(-1, 1)]
    [TestCase(3, 5)]
    public void AddStay_OutsideTheTrip_ThrowsDomainException(int checkInDay, int checkOutDay)
    {
        var ex = Assert.Throws<DomainException>(() => _trip.AddStay(Details("Hotel", checkInDay, checkOutDay)));

        Assert.That(ex.Message, Is.EqualTo("Stay dates must be within the trip dates."));
    }

    [TestCase(1, 1)]
    [TestCase(2, 1)]
    public void StayDetails_WithCheckOutNotAfterCheckIn_ThrowsDomainException(int checkInDay, int checkOutDay)
    {
        var ex = Assert.Throws<DomainException>(() => Details("Hotel", checkInDay, checkOutDay));

        Assert.That(ex.Message, Is.EqualTo("Check-out must be after check-in."));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void StayDetails_WithoutNameOrPlace_ThrowsDomainException(string? name)
    {
        var ex = Assert.Throws<DomainException>(() => Details(name!, 0, 1));

        Assert.That(ex.Message, Is.EqualTo("Stay name is required."));
    }

    [Test]
    public void StayDetails_WithoutNameButWithPlace_UsesThePlaceName()
    {
        var place = new Place("place-1", "Hotel Gracery Shinjuku", "Kabukicho, Tokyo", 35.69, 139.70, "https://maps.google.com/?cid=1");

        var details = new StayDetails("", FirstDay, FirstDay.AddDays(1), place);

        Assert.That(details.Name, Is.EqualTo("Hotel Gracery Shinjuku"));
    }

    [Test]
    public void StayDetails_TrimsNameAndConfirmationCode()
    {
        var details = new StayDetails("  Hotel  ", FirstDay, FirstDay.AddDays(1), confirmationCode: "  AB12  ");

        Assert.Multiple(() =>
        {
            Assert.That(details.Name, Is.EqualTo("Hotel"));
            Assert.That(details.ConfirmationCode, Is.EqualTo("AB12"));
        });
    }

    [Test]
    public void StayDetails_WithTooLongConfirmationCode_ThrowsDomainException()
    {
        var code = new string('A', ActivityDetails.MaxConfirmationCodeLength + 1);

        Assert.Throws<DomainException>(() =>
            new StayDetails("Hotel", FirstDay, FirstDay.AddDays(1), confirmationCode: code));
    }

    [Test]
    public void StayDetails_WithUnknownBookingStatus_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new StayDetails("Hotel", FirstDay, FirstDay.AddDays(1), bookingStatus: (BookingStatus)99));
    }

    [TestCase(0, 2)]
    [TestCase(1, 3)]
    [TestCase(0, 4)]
    public void AddStay_OverlappingAnotherStay_ThrowsStayOverlapException(int checkInDay, int checkOutDay)
    {
        _trip.AddStay(Details("Hotel Gracery", 1, 2));

        var ex = Assert.Throws<StayOverlapException>(() => _trip.AddStay(Details("Ryokan", checkInDay, checkOutDay)));

        Assert.That(ex.Message, Does.Contain("overlap with \"Hotel Gracery\""));
    }

    [Test]
    public void AddStay_CheckingInOnAnotherStaysCheckOutDay_IsAllowed()
    {
        _trip.AddStay(Details("Hotel Gracery", 0, 2));

        var ryokan = _trip.AddStay(Details("Ryokan", 2, 4));

        Assert.That(_trip.Stays, Has.Count.EqualTo(2).And.Contains(ryokan));
    }

    [Test]
    public void UpdateStay_ChangesDetails()
    {
        var stay = _trip.AddStay(Details("Hotel", 0, 1));

        _trip.UpdateStay(stay.Id, Details("Hotel Gracery", 0, 3, new Money(15000m)));

        Assert.Multiple(() =>
        {
            Assert.That(stay.Name, Is.EqualTo("Hotel Gracery"));
            Assert.That(stay.Nights, Is.EqualTo(3));
            Assert.That(stay.Cost.Amount, Is.EqualTo(15000m));
        });
    }

    [Test]
    public void UpdateStay_DoesNotConflictWithItself()
    {
        var stay = _trip.AddStay(Details("Hotel", 0, 2));

        Assert.DoesNotThrow(() => _trip.UpdateStay(stay.Id, Details("Hotel", 1, 3)));
    }

    [Test]
    public void UpdateStay_IntoAnotherStay_ThrowsStayOverlapException()
    {
        _trip.AddStay(Details("Hotel", 0, 2));
        var ryokan = _trip.AddStay(Details("Ryokan", 2, 4));

        Assert.Throws<StayOverlapException>(() => _trip.UpdateStay(ryokan.Id, Details("Ryokan", 1, 4)));
    }

    [Test]
    public void UpdateStay_WithUnknownId_ThrowsStayNotFoundException()
    {
        Assert.Throws<StayNotFoundException>(() => _trip.UpdateStay(Guid.NewGuid(), Details("Hotel", 0, 1)));
    }

    [Test]
    public void RemoveStay_RemovesIt()
    {
        var stay = _trip.AddStay(Details("Hotel", 0, 1));

        _trip.RemoveStay(stay.Id);

        Assert.That(_trip.Stays, Is.Empty);
    }

    [Test]
    public void RemoveStay_WithUnknownId_ThrowsStayNotFoundException()
    {
        Assert.Throws<StayNotFoundException>(() => _trip.RemoveStay(Guid.NewGuid()));
    }

    [Test]
    public void StayForNight_FindsTheStayCoveringThatNight()
    {
        var hotel = _trip.AddStay(Details("Hotel", 0, 2));

        Assert.Multiple(() =>
        {
            Assert.That(_trip.StayForNight(FirstDay), Is.EqualTo(hotel));
            Assert.That(_trip.StayForNight(FirstDay.AddDays(1)), Is.EqualTo(hotel));
            Assert.That(_trip.StayForNight(FirstDay.AddDays(2)), Is.Null);
        });
    }

    [Test]
    public void CostForNight_SplitsTheCostAndGivesTheRemainderToTheLastNight()
    {
        var stay = _trip.AddStay(Details("Hotel", 0, 3, new Money(100m, "JPY")));

        Assert.Multiple(() =>
        {
            Assert.That(stay.CostForNight(FirstDay), Is.EqualTo(new Money(33.33m, "JPY")));
            Assert.That(stay.CostForNight(FirstDay.AddDays(1)), Is.EqualTo(new Money(33.33m, "JPY")));
            Assert.That(stay.CostForNight(FirstDay.AddDays(2)), Is.EqualTo(new Money(33.34m, "JPY")));
            Assert.That(stay.CostForNight(FirstDay.AddDays(3)), Is.EqualTo(Money.ZeroIn("JPY")));
        });
    }

    [Test]
    public void GetTotals_IncludesStayCosts()
    {
        _trip.AddActivity("Senso-ji", FirstDay, new TimeSlot(new(10, 0), new(11, 0)), new Money(500m));
        _trip.AddStay(Details("Hotel", 0, 2, new Money(9000m)));
        _trip.AddStay(Details("Ryokan", 2, 3, new Money(20000m, "JPY")));

        Assert.That(_trip.GetTotals(), Is.EqualTo(new[] { new Money(9500m), new Money(20000m, "JPY") }));
    }

    [Test]
    public void GetDailyTotals_IncludesTheNightlyShareOfAStay()
    {
        _trip.AddActivity("Senso-ji", FirstDay, new TimeSlot(new(10, 0), new(11, 0)), new Money(500m));
        _trip.AddStay(Details("Hotel", 0, 2, new Money(9000m)));

        Assert.Multiple(() =>
        {
            Assert.That(_trip.GetDailyTotals(FirstDay), Is.EqualTo(new[] { new Money(5000m) }));
            Assert.That(_trip.GetDailyTotals(FirstDay.AddDays(1)), Is.EqualTo(new[] { new Money(4500m) }));
            Assert.That(_trip.GetDailyTotals(FirstDay.AddDays(2)), Is.Empty);
        });
    }

    [Test]
    public void GetTotals_IgnoresFreeStays()
    {
        _trip.AddStay(Details("Friend's place", 0, 2));

        Assert.That(_trip.GetTotals(), Is.Empty);
    }

    [Test]
    public void Reschedule_LeavingAStayOutside_ThrowsDomainException()
    {
        _trip.AddStay(Details("Ryokan", 3, 4));

        var ex = Assert.Throws<DomainException>(() => _trip.Reschedule(FirstDay, FirstDay.AddDays(1)));

        Assert.That(ex.Message, Is.EqualTo("A stay falls outside the new dates. Change or delete it first."));
    }

    [Test]
    public void Reschedule_KeepingStaysInside_Succeeds()
    {
        _trip.AddStay(Details("Hotel", 0, 2));

        Assert.DoesNotThrow(() => _trip.Reschedule(FirstDay, FirstDay.AddDays(1)));
    }
}
