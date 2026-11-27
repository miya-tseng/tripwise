using NSubstitute;
using TripWise.Application.Places;
using TripWise.Application.Services;
using TripWise.Application.Suggestions;
using TripWise.Domain;

namespace TripWise.UnitTests.Services;

public class SuggestionServiceTests
{
    private static readonly DateOnly FirstDay = new(2021, 05, 01);
    private static readonly Place SensoJi = Place("senso-ji", "Senso-ji", 35.71, 139.79);
    private static readonly Place Skytree = Place("skytree", "Tokyo Skytree", 35.71, 139.81);

    private ITripRepository _repository = null!;
    private ISuggestionGenerator _generator = null!;
    private IPlaceSearch _placeSearch = null!;
    private SuggestionService _sut = null!;
    private Trip _trip = null!;
    private Activity _anchor = null!;

    private static Place Place(string id, string name, double latitude, double longitude) =>
        new(id, name, $"{name} address", latitude, longitude, $"https://maps.google.com/?cid={id}");

    private static TimeSlot Slot(string start, string end) =>
        new(TimeOnly.Parse(start), TimeOnly.Parse(end));

    private static SuggestionIdea Idea(string name, int minutes = 60) =>
        new(name, "東京都", $"{name} reason", minutes);

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ITripRepository>();
        _generator = Substitute.For<ISuggestionGenerator>();
        _placeSearch = Substitute.For<IPlaceSearch>();
        _sut = new SuggestionService(_repository, _generator, _placeSearch);

        _trip = Trip.Create("Tokyo", FirstDay, FirstDay.AddDays(2));
        _anchor = _trip.AddActivity("", FirstDay, Slot("10:00", "11:30"), place: SensoJi);
        _repository.GetByIdAsync(_trip.Id, Arg.Any<CancellationToken>()).Returns(_trip);

        _placeSearch.FindAsync(Arg.Any<string>(), Arg.Any<GeoPoint?>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var name = call.ArgAt<string>(0).Replace(" 東京都", string.Empty);
                return Place(name.ToLowerInvariant().Replace(' ', '-'), name, 35.7, 139.8);
            });
    }

    [Test]
    public void GetSuggestionsAsync_WithUnknownTrip_Throws()
    {
        Assert.ThrowsAsync<TripNotFoundException>(() => _sut.GetSuggestionsAsync(Guid.NewGuid(), _anchor.Id));
    }

    [Test]
    public void GetSuggestionsAsync_WithUnknownActivity_ThrowsNotFound()
    {
        Assert.ThrowsAsync<ActivityNotFoundException>(() => _sut.GetSuggestionsAsync(_trip.Id, Guid.NewGuid()));
    }

    [Test]
    public void GetSuggestionsAsync_ForActivityWithoutPlace_ThrowsDomainException()
    {
        var freeTime = _trip.AddActivity("Free time", FirstDay, Slot("13:00", "14:00"));

        var ex = Assert.ThrowsAsync<DomainException>(() => _sut.GetSuggestionsAsync(_trip.Id, freeTime.Id));

        Assert.That(ex!.Message, Is.EqualTo("Suggestions need an activity with a place."));
    }

    [Test]
    public async Task GetSuggestionsAsync_PassesThePlaceAndDayToTheGenerator()
    {
        _trip.AddActivity("Tsukiji", FirstDay, Slot("07:00", "08:00"));
        _trip.AddActivity("Tokyo Tower", FirstDay, Slot("18:00", "19:00"));
        _trip.AddActivity("Nikko", FirstDay.AddDays(1), Slot("09:00", "17:00"));
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>()).Returns([]);

        await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        await _generator.Received(1).GenerateAsync(
            Arg.Is<SuggestionContext>(context =>
                context.PlaceId == "senso-ji"
                && context.PlaceName == "Senso-ji"
                && context.PlaceAddress == "Senso-ji address"
                && context.EndsAt == new TimeOnly(11, 30)
                && context.PlannedThatDay.SequenceEqual(new[] { "Tsukiji", "Tokyo Tower" })),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSuggestionsAsync_ResolvesIdeasToRealPlacesNearTheActivity()
    {
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns([Idea("Ueno Park", 90)]);

        var suggestions = await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        await _placeSearch.Received(1).FindAsync("Ueno Park 東京都", new GeoPoint(35.71, 139.79), Arg.Any<CancellationToken>());
        var suggestion = suggestions!.Single();
        Assert.Multiple(() =>
        {
            Assert.That(suggestion.Place.Name, Is.EqualTo("Ueno Park"));
            Assert.That(suggestion.Place.GoogleMapsUri, Does.StartWith("https://"));
            Assert.That(suggestion.Reason, Is.EqualTo("Ueno Park reason"));
            Assert.That(suggestion.SuggestedMinutes, Is.EqualTo(90));
        });
    }

    [Test]
    public async Task GetSuggestionsAsync_DropsIdeasThatAreNotRealPlaces()
    {
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns([Idea("Imaginary Castle"), Idea("Ueno Park")]);
        _placeSearch.FindAsync(Arg.Is<string>(text => text.StartsWith("Imaginary")), Arg.Any<GeoPoint?>(), Arg.Any<CancellationToken>())
            .Returns((Place?)null);

        var suggestions = await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        Assert.That(suggestions!.Select(item => item.Place.Name), Is.EqualTo(new[] { "Ueno Park" }));
    }

    [Test]
    public async Task GetSuggestionsAsync_ExcludesPlacesAlreadyInTheTrip()
    {
        _trip.AddActivity("", FirstDay.AddDays(1), Slot("10:00", "11:00"), place: Skytree);
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns([Idea("Senso-ji"), Idea("Skytree Observation"), Idea("Ueno Park")]);
        _placeSearch.FindAsync(Arg.Is<string>(text => text.StartsWith("Skytree")), Arg.Any<GeoPoint?>(), Arg.Any<CancellationToken>())
            .Returns(Skytree);

        var suggestions = await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        Assert.That(suggestions!.Select(item => item.Place.Name), Is.EqualTo(new[] { "Ueno Park" }));
    }

    [Test]
    public async Task GetSuggestionsAsync_SkipsIdeasResolvingToTheSamePlace()
    {
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns([Idea("Ueno Park"), Idea("Ueno Onshi Park")]);
        _placeSearch.FindAsync(Arg.Is<string>(text => text.StartsWith("Ueno")), Arg.Any<GeoPoint?>(), Arg.Any<CancellationToken>())
            .Returns(Place("ueno-park", "Ueno Park", 35.71, 139.77));

        var suggestions = await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        Assert.That(suggestions, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task GetSuggestionsAsync_ReturnsAtMostFiveAndStopsSearching()
    {
        var ideas = Enumerable.Range(1, 8).Select(index => Idea($"Place {index}")).ToList();
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>()).Returns(ideas);

        var suggestions = await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        Assert.That(suggestions, Has.Count.EqualTo(SuggestionService.MaxSuggestions));
        await _placeSearch.Received(5).FindAsync(Arg.Any<string>(), Arg.Any<GeoPoint?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task GetSuggestionsAsync_WithoutArea_SearchesByNameOnly()
    {
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns([new SuggestionIdea("Ueno Park", "", "reason", 60)]);

        await _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id);

        await _placeSearch.Received(1).FindAsync("Ueno Park", Arg.Any<GeoPoint?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public void GetSuggestionsAsync_WhenTheGeneratorFails_Throws()
    {
        _generator.GenerateAsync(Arg.Any<SuggestionContext>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<SuggestionIdea>>(_ => throw new SuggestionsUnavailableException());

        Assert.ThrowsAsync<SuggestionsUnavailableException>(() => _sut.GetSuggestionsAsync(_trip.Id, _anchor.Id));
    }
}
