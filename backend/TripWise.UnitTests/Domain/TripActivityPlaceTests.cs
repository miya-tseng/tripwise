using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class TripActivityPlaceTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);

    private static readonly Place SensoJi = new(
        "senso-ji-id",
        "Senso-ji",
        "2 Chome-3-1 Asakusa, Taito City, Tokyo",
        35.7148,
        139.7967,
        "https://maps.google.com/?cid=1");

    private Trip _trip = null!;

    [SetUp]
    public void CreateTrip()
    {
        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
    }

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    [Test]
    public void AddActivity_WithoutPlace_HasNoPlace()
    {
        var activity = _trip.AddActivity("Free time", FirstDay, Slot("10:00", "11:00"));

        Assert.That(activity.Place, Is.Null);
    }

    [Test]
    public void AddActivity_WithPlace_KeepsPlace()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        Assert.That(activity.Place, Is.EqualTo(SensoJi));
    }

    [TestCase("")]
    [TestCase("   ")]
    public void AddActivity_WithPlaceAndNoName_UsesPlaceName(string name)
    {
        var activity = _trip.AddActivity(name, FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        Assert.That(activity.Name, Is.EqualTo("Senso-ji"));
    }

    [Test]
    public void AddActivity_WithPlaceAndCustomName_KeepsCustomName()
    {
        var activity = _trip.AddActivity("Morning at the temple", FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        Assert.That(activity.Name, Is.EqualTo("Morning at the temple"));
    }

    [Test]
    public void AddActivity_WithLongPlaceName_TruncatesTheDefaultName()
    {
        var longPlace = new Place(
            SensoJi.PlaceId,
            new string('a', 150),
            SensoJi.Address,
            SensoJi.Latitude,
            SensoJi.Longitude,
            SensoJi.GoogleMapsUri);

        var activity = _trip.AddActivity("", FirstDay, Slot("10:00", "11:00"), place: longPlace);

        Assert.That(activity.Name, Has.Length.EqualTo(Activity.MaxNameLength));
    }

    [Test]
    public void AddActivity_WithoutPlaceAndNoName_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => _trip.AddActivity("", FirstDay, Slot("10:00", "11:00")));

        Assert.That(ex.Message, Is.EqualTo("Activity name is required."));
    }

    [Test]
    public void UpdateActivity_ChangesPlace()
    {
        var activity = _trip.AddActivity("Free time", FirstDay, Slot("10:00", "11:00"));

        _trip.UpdateActivity(activity.Id, "", FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        Assert.Multiple(() =>
        {
            Assert.That(activity.Place, Is.EqualTo(SensoJi));
            Assert.That(activity.Name, Is.EqualTo("Senso-ji"));
        });
    }

    [Test]
    public void UpdateActivity_WithoutPlace_RemovesPlace()
    {
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        _trip.UpdateActivity(activity.Id, "Free time", FirstDay, Slot("10:00", "11:00"));

        Assert.That(activity.Place, Is.Null);
    }

    [Test]
    public void UpdateActivity_RejectedByOverlap_KeepsOriginalPlace()
    {
        _trip.AddActivity("Ueno Park", FirstDay, Slot("13:00", "14:00"));
        var activity = _trip.AddActivity("Senso-ji", FirstDay, Slot("10:00", "11:00"), place: SensoJi);

        Assert.Throws<ActivityOverlapException>(() =>
            _trip.UpdateActivity(activity.Id, "Free time", FirstDay, Slot("13:30", "14:30")));

        Assert.That(activity.Place, Is.EqualTo(SensoJi));
    }
}
