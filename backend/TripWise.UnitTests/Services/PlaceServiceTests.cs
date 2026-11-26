using NSubstitute;
using TripWise.Application.Places;
using TripWise.Application.Services;
using TripWise.Domain;

namespace TripWise.UnitTests.Services;

public class PlaceServiceTests
{
    private IPlaceSearch _placeSearch = null!;
    private PlaceService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _placeSearch = Substitute.For<IPlaceSearch>();
        _sut = new PlaceService(_placeSearch);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task AutocompleteAsync_WithBlankQuery_ReturnsNothingWithoutSearching(string? query)
    {
        var result = await _sut.AutocompleteAsync(query, "token", null, null);

        Assert.That(result, Is.Empty);
        await _placeSearch.DidNotReceiveWithAnyArgs().AutocompleteAsync(default!, default, default, default);
    }

    [Test]
    public void AutocompleteAsync_WithTooLongQuery_ThrowsDomainException()
    {
        var ex = Assert.ThrowsAsync<DomainException>(() =>
            _sut.AutocompleteAsync(new string('a', PlaceService.MaxQueryLength + 1), null, null, null));

        Assert.That(ex!.Message, Is.EqualTo("Search text cannot exceed 100 characters."));
    }

    [Test]
    public async Task AutocompleteAsync_TrimsQueryAndPassesSessionAndLocation()
    {
        _placeSearch.AutocompleteAsync("senso", "token", new GeoPoint(35.7, 139.8), PlaceSearchKind.Any, Arg.Any<CancellationToken>())
            .Returns([new PlaceSuggestion("senso-ji-id", "Senso-ji", "Asakusa, Tokyo")]);

        var result = await _sut.AutocompleteAsync("  senso  ", "token", 35.7, 139.8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Single().PlaceId, Is.EqualTo("senso-ji-id"));
            Assert.That(result.Single().Name, Is.EqualTo("Senso-ji"));
            Assert.That(result.Single().Description, Is.EqualTo("Asakusa, Tokyo"));
        });
    }

    [Test]
    public async Task AutocompleteAsync_WithOnlyOneCoordinate_SearchesWithoutLocation()
    {
        await _sut.AutocompleteAsync("senso", null, 35.7, null);

        await _placeSearch.Received(1).AutocompleteAsync("senso", null, null, PlaceSearchKind.Any, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AutocompleteAsync_ForDestinations_PassesTheKind()
    {
        await _sut.AutocompleteAsync("tok", null, null, null, PlaceSearchKind.Destination);

        await _placeSearch.Received(1).AutocompleteAsync("tok", null, null, PlaceSearchKind.Destination, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetDetailsAsync_WithUnknownPlace_ReturnsNull()
    {
        Assert.That(await _sut.GetDetailsAsync("unknown", null), Is.Null);
    }

    [Test]
    public async Task GetDetailsAsync_MapsThePlace()
    {
        var place = new Place("id", "Senso-ji", "Asakusa", 35.7, 139.8, "https://maps.google.com/?cid=1");
        _placeSearch.GetDetailsAsync("id", "token", Arg.Any<CancellationToken>()).Returns(place);

        var result = await _sut.GetDetailsAsync("id", "token");

        Assert.Multiple(() =>
        {
            Assert.That(result!.Name, Is.EqualTo("Senso-ji"));
            Assert.That(result.Longitude, Is.EqualTo(139.8));
            Assert.That(result.GoogleMapsUri, Is.EqualTo("https://maps.google.com/?cid=1"));
        });
    }
}
