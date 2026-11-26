using TripWise.Domain;

namespace TripWise.UnitTests.Domain;

public class PlaceTests
{
    private const string MapsUri = "https://maps.google.com/?cid=123";

    private static Place Valid(
        string placeId = "ChIJ8T1GpMGOGGARDYGSgpooDWw",
        string name = "Senso-ji",
        string address = "2 Chome-3-1 Asakusa, Taito City, Tokyo",
        double latitude = 35.7148,
        double longitude = 139.7967,
        string googleMapsUri = MapsUri) =>
        new(placeId, name, address, latitude, longitude, googleMapsUri);

    [Test]
    public void Constructor_WithValidValues_KeepsThem()
    {
        var place = Valid(name: "  Senso-ji  ");

        Assert.Multiple(() =>
        {
            Assert.That(place.PlaceId, Is.EqualTo("ChIJ8T1GpMGOGGARDYGSgpooDWw"));
            Assert.That(place.Name, Is.EqualTo("Senso-ji"));
            Assert.That(place.Address, Is.EqualTo("2 Chome-3-1 Asakusa, Taito City, Tokyo"));
            Assert.That(place.Latitude, Is.EqualTo(35.7148));
            Assert.That(place.Longitude, Is.EqualTo(139.7967));
            Assert.That(place.GoogleMapsUri, Is.EqualTo(MapsUri));
        });
    }

    [TestCase("")]
    [TestCase("  ")]
    public void Constructor_WithoutPlaceId_Throws(string placeId)
    {
        var ex = Assert.Throws<DomainException>(() => Valid(placeId: placeId));

        Assert.That(ex.Message, Is.EqualTo("Place id is required."));
    }

    [Test]
    public void Constructor_WithoutName_Throws()
    {
        var ex = Assert.Throws<DomainException>(() => Valid(name: " "));

        Assert.That(ex.Message, Is.EqualTo("Place name is required."));
    }

    [Test]
    public void Constructor_WithoutAddress_IsAllowed()
    {
        Assert.That(Valid(address: null!).Address, Is.Empty);
    }

    [TestCase(-90.0001)]
    [TestCase(90.0001)]
    [TestCase(double.NaN)]
    public void Constructor_WithInvalidLatitude_Throws(double latitude)
    {
        var ex = Assert.Throws<DomainException>(() => Valid(latitude: latitude));

        Assert.That(ex.Message, Is.EqualTo("Latitude must be between -90 and 90."));
    }

    [TestCase(-180.0001)]
    [TestCase(180.0001)]
    public void Constructor_WithInvalidLongitude_Throws(double longitude)
    {
        var ex = Assert.Throws<DomainException>(() => Valid(longitude: longitude));

        Assert.That(ex.Message, Is.EqualTo("Longitude must be between -180 and 180."));
    }

    [TestCase(-90, -180)]
    [TestCase(90, 180)]
    public void Constructor_WithBoundaryCoordinates_IsAllowed(double latitude, double longitude)
    {
        Assert.DoesNotThrow(() => Valid(latitude: latitude, longitude: longitude));
    }

    [TestCase("not a url")]
    [TestCase("http://maps.google.com/?cid=1")]
    [TestCase("javascript:alert(1)")]
    public void Constructor_WithInvalidMapsLink_Throws(string uri)
    {
        var ex = Assert.Throws<DomainException>(() => Valid(googleMapsUri: uri));

        Assert.That(ex.Message, Is.EqualTo("Google Maps link must be an https URL."));
    }

    [Test]
    public void Equals_WithSameValues_ReturnsTrue()
    {
        Assert.That(Valid(), Is.EqualTo(Valid()));
    }
}
