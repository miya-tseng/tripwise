using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TripWise.Application;
using TripWise.Application.Places;
using TripWise.Infrastructure.Places;

namespace TripWise.IntegrationTests.Places;

public class GooglePlaceSearchTests
{
    private const string AutocompleteBody = """
        {
          "suggestions": [
            {
              "placePrediction": {
                "placeId": "ChIJ8T1GpMGOGGARDYGSgpooDWw",
                "structuredFormat": {
                  "mainText": { "text": "淺草寺" },
                  "secondaryText": { "text": "日本東京都台東區淺草" }
                }
              }
            },
            { "queryPrediction": { "text": { "text": "senso ji restaurants" } } }
          ]
        }
        """;

    private const string DetailsBody = """
        {
          "id": "ChIJ8T1GpMGOGGARDYGSgpooDWw",
          "displayName": { "text": "淺草寺", "languageCode": "zh-TW" },
          "formattedAddress": "2 Chome-3-1 Asakusa, Taito City, Tokyo 111-0032日本",
          "location": { "latitude": 35.7147651, "longitude": 139.7966553 },
          "googleMapsUri": "https://maps.google.com/?cid=7787212617349009677"
        }
        """;

    private StubHandler _handler = null!;
    private GooglePlaceSearch _search = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new StubHandler();
        var options = new GooglePlacesOptions { ApiKey = "test-key" };
        var httpClient = new HttpClient(_handler) { BaseAddress = options.BaseUrl };
        _search = new GooglePlaceSearch(httpClient, Options.Create(options), NullLogger<GooglePlaceSearch>.Instance);
    }

    [TearDown]
    public void TearDown() => _handler.Dispose();

    [Test]
    public async Task AutocompleteAsync_PostsTheQueryWithKeyFieldMaskAndSession()
    {
        _handler.Respond(HttpStatusCode.OK, AutocompleteBody);

        await _search.AutocompleteAsync("senso", "session-1", new GeoPoint(35.7, 139.8));

        var request = _handler.Requests.Single();
        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Post));
            Assert.That(request.Uri, Is.EqualTo("https://places.googleapis.com/v1/places:autocomplete"));
            Assert.That(request.Headers["X-Goog-Api-Key"], Is.EqualTo("test-key"));
            Assert.That(request.Headers["X-Goog-FieldMask"], Does.Contain("suggestions.placePrediction.placeId"));
            Assert.That(body.GetProperty("input").GetString(), Is.EqualTo("senso"));
            Assert.That(body.GetProperty("sessionToken").GetString(), Is.EqualTo("session-1"));
            Assert.That(body.GetProperty("languageCode").GetString(), Is.EqualTo("zh-TW"));
            Assert.That(body.GetProperty("locationBias").GetProperty("circle").GetProperty("center").GetProperty("latitude").GetDouble(), Is.EqualTo(35.7));
            Assert.That(body.GetProperty("locationBias").GetProperty("circle").GetProperty("radius").GetDouble(), Is.EqualTo(50_000));
        });
    }

    [Test]
    public async Task AutocompleteAsync_WithoutLocationOrSession_OmitsThem()
    {
        _handler.Respond(HttpStatusCode.OK, AutocompleteBody);

        await _search.AutocompleteAsync("senso", null, null);

        var body = JsonDocument.Parse(_handler.Requests.Single().Body!).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(body.TryGetProperty("locationBias", out _), Is.False);
            Assert.That(body.TryGetProperty("sessionToken", out _), Is.False);
            Assert.That(body.TryGetProperty("includedPrimaryTypes", out _), Is.False);
        });
    }

    [Test]
    public async Task AutocompleteAsync_ForDestinations_LimitsResultsToRegions()
    {
        _handler.Respond(HttpStatusCode.OK, AutocompleteBody);

        await _search.AutocompleteAsync("tok", null, null, PlaceSearchKind.Destination);

        var body = JsonDocument.Parse(_handler.Requests.Single().Body!).RootElement;
        Assert.That(
            body.GetProperty("includedPrimaryTypes").EnumerateArray().Select(type => type.GetString()),
            Is.EqualTo(new[] { "(regions)" }));
    }

    [Test]
    public async Task AutocompleteAsync_ReturnsPlacePredictionsOnly()
    {
        _handler.Respond(HttpStatusCode.OK, AutocompleteBody);

        var suggestions = await _search.AutocompleteAsync("senso", null, null);

        Assert.That(suggestions, Is.EqualTo(new[]
        {
            new PlaceSuggestion("ChIJ8T1GpMGOGGARDYGSgpooDWw", "淺草寺", "日本東京都台東區淺草"),
        }));
    }

    [Test]
    public async Task AutocompleteAsync_WithNoSuggestions_ReturnsEmpty()
    {
        _handler.Respond(HttpStatusCode.OK, "{}");

        Assert.That(await _search.AutocompleteAsync("zzzz", null, null), Is.Empty);
    }

    [Test]
    public async Task GetDetailsAsync_RequestsTheFieldsWithLanguageAndSession()
    {
        _handler.Respond(HttpStatusCode.OK, DetailsBody);

        await _search.GetDetailsAsync("ChIJ8T1GpMGOGGARDYGSgpooDWw", "session-1");

        var request = _handler.Requests.Single();
        Assert.Multiple(() =>
        {
            Assert.That(request.Method, Is.EqualTo(HttpMethod.Get));
            Assert.That(request.Uri, Is.EqualTo("https://places.googleapis.com/v1/places/ChIJ8T1GpMGOGGARDYGSgpooDWw?languageCode=zh-TW&sessionToken=session-1"));
            Assert.That(request.Headers["X-Goog-FieldMask"], Is.EqualTo("id,displayName,formattedAddress,location,googleMapsUri"));
            Assert.That(request.Headers["X-Goog-Api-Key"], Is.EqualTo("test-key"));
        });
    }

    [Test]
    public async Task GetDetailsAsync_MapsThePlace()
    {
        _handler.Respond(HttpStatusCode.OK, DetailsBody);

        var place = await _search.GetDetailsAsync("ChIJ8T1GpMGOGGARDYGSgpooDWw", null);

        Assert.Multiple(() =>
        {
            Assert.That(place!.PlaceId, Is.EqualTo("ChIJ8T1GpMGOGGARDYGSgpooDWw"));
            Assert.That(place.Name, Is.EqualTo("淺草寺"));
            Assert.That(place.Address, Does.StartWith("2 Chome-3-1 Asakusa"));
            Assert.That(place.Latitude, Is.EqualTo(35.7147651));
            Assert.That(place.Longitude, Is.EqualTo(139.7966553));
            Assert.That(place.GoogleMapsUri, Is.EqualTo("https://maps.google.com/?cid=7787212617349009677"));
        });
    }

    [TestCase(HttpStatusCode.NotFound)]
    [TestCase(HttpStatusCode.BadRequest)]
    public async Task GetDetailsAsync_ForUnknownPlace_ReturnsNull(HttpStatusCode status)
    {
        _handler.Respond(status, """{ "error": { "status": "NOT_FOUND" } }""");

        Assert.That(await _search.GetDetailsAsync("unknown", null), Is.Null);
    }

    [TestCase(HttpStatusCode.InternalServerError)]
    [TestCase(HttpStatusCode.Forbidden)]
    [TestCase(HttpStatusCode.TooManyRequests)]
    public void AutocompleteAsync_WhenGoogleFails_ThrowsUnavailable(HttpStatusCode status)
    {
        _handler.Respond(status, "{}");

        Assert.ThrowsAsync<ExternalServiceUnavailableException>(() => _search.AutocompleteAsync("senso", null, null));
    }

    [Test]
    public void GetDetailsAsync_WhenGoogleCannotBeReached_ThrowsUnavailable()
    {
        _handler.Throw(new HttpRequestException("connection refused"));

        Assert.ThrowsAsync<ExternalServiceUnavailableException>(() => _search.GetDetailsAsync("id", null));
    }

    [Test]
    public async Task FindAsync_PostsATextSearchForOnePlaceNearTheLocation()
    {
        _handler.Respond(HttpStatusCode.OK, $$"""{ "places": [ {{DetailsBody}} ] }""");

        var place = await _search.FindAsync("Tokyo Skytree 東京都墨田區", new GeoPoint(35.71, 139.79));

        var request = _handler.Requests.Single();
        var body = JsonDocument.Parse(request.Body!).RootElement;
        Assert.Multiple(() =>
        {
            Assert.That(request.Uri, Is.EqualTo("https://places.googleapis.com/v1/places:searchText"));
            Assert.That(request.Headers["X-Goog-FieldMask"], Does.Contain("places.googleMapsUri"));
            Assert.That(body.GetProperty("textQuery").GetString(), Is.EqualTo("Tokyo Skytree 東京都墨田區"));
            Assert.That(body.GetProperty("pageSize").GetInt32(), Is.EqualTo(1));
            Assert.That(body.GetProperty("locationBias").GetProperty("circle").GetProperty("center").GetProperty("longitude").GetDouble(), Is.EqualTo(139.79));
            Assert.That(place!.Name, Is.EqualTo("淺草寺"));
        });
    }

    [Test]
    public async Task FindAsync_WithNoResults_ReturnsNull()
    {
        _handler.Respond(HttpStatusCode.OK, "{}");

        Assert.That(await _search.FindAsync("nowhere", null), Is.Null);
    }

    private sealed record RecordedRequest(HttpMethod Method, string Uri, Dictionary<string, string> Headers, string? Body);

    private sealed class StubHandler : HttpMessageHandler
    {
        private Func<HttpResponseMessage> _respond = () => new HttpResponseMessage(HttpStatusCode.NotFound);

        public List<RecordedRequest> Requests { get; } = [];

        public void Respond(HttpStatusCode status, string body) =>
            _respond = () => new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };

        public void Throw(Exception exception) => _respond = () => throw exception;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            var headers = request.Headers.ToDictionary(header => header.Key, header => string.Join(",", header.Value));
            Requests.Add(new RecordedRequest(request.Method, request.RequestUri!.ToString(), headers, body));
            return _respond();
        }
    }
}
