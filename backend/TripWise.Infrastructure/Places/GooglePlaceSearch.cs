using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripWise.Application;
using TripWise.Application.Places;
using TripWise.Domain;

namespace TripWise.Infrastructure.Places;

public class GooglePlaceSearch(
    HttpClient httpClient,
    IOptions<GooglePlacesOptions> options,
    ILogger<GooglePlaceSearch> logger) : IPlaceSearch
{
    private const string UnavailableMessage = "Place search is unavailable right now. Please try again later.";
    private const string AutocompleteFields = "suggestions.placePrediction.placeId,suggestions.placePrediction.structuredFormat";
    private const string DetailsFields = "id,displayName,formattedAddress,location,googleMapsUri";

    public async Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(
        string query,
        string? sessionToken,
        GeoPoint? near,
        CancellationToken cancellationToken = default)
    {
        var body = new AutocompleteRequest
        {
            Input = query,
            SessionToken = sessionToken,
            LanguageCode = options.Value.LanguageCode,
            LocationBias = near is null
                ? null
                : new LocationBias
                {
                    Circle = new Circle
                    {
                        Center = new LatLng { Latitude = near.Latitude, Longitude = near.Longitude },
                        Radius = options.Value.BiasRadiusMeters,
                    },
                },
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "./places:autocomplete")
        {
            Content = JsonContent.Create(body),
        };
        request.Headers.Add("X-Goog-FieldMask", AutocompleteFields);

        var response = await SendAsync<AutocompleteResponse>(request, cancellationToken);

        return (response?.Suggestions ?? [])
            .Select(suggestion => suggestion.PlacePrediction)
            .Where(prediction => prediction?.PlaceId is not null)
            .Select(prediction => new PlaceSuggestion(
                prediction!.PlaceId!,
                prediction.StructuredFormat?.MainText?.Text ?? string.Empty,
                prediction.StructuredFormat?.SecondaryText?.Text ?? string.Empty))
            .Where(suggestion => suggestion.Name.Length > 0)
            .Take(options.Value.MaxSuggestions)
            .ToList();
    }

    public async Task<Place?> GetDetailsAsync(
        string placeId,
        string? sessionToken,
        CancellationToken cancellationToken = default)
    {
        var query = $"languageCode={Uri.EscapeDataString(options.Value.LanguageCode)}";

        if (!string.IsNullOrWhiteSpace(sessionToken))
        {
            query += $"&sessionToken={Uri.EscapeDataString(sessionToken)}";
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"places/{Uri.EscapeDataString(placeId)}?{query}");
        request.Headers.Add("X-Goog-FieldMask", DetailsFields);

        var details = await SendAsync<PlaceDetails>(request, cancellationToken, notFoundAsNull: true);

        if (details?.Id is null || details.Location is null || details.GoogleMapsUri is null)
        {
            return null;
        }

        return new Place(
            details.Id,
            details.DisplayName?.Text ?? string.Empty,
            details.FormattedAddress ?? string.Empty,
            details.Location.Latitude,
            details.Location.Longitude,
            details.GoogleMapsUri);
    }

    private async Task<T?> SendAsync<T>(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        bool notFoundAsNull = false)
        where T : class
    {
        request.Headers.Add("X-Goog-Api-Key", options.Value.ApiKey);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (notFoundAsNull && response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.BadRequest)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Google Places returned {StatusCode}", (int)response.StatusCode);
                throw new ExternalServiceUnavailableException(UnavailableMessage);
            }

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            logger.LogWarning(exception, "Google Places could not be reached");
            throw new ExternalServiceUnavailableException(UnavailableMessage, exception);
        }
    }

    private sealed class AutocompleteRequest
    {
        [JsonPropertyName("input")]
        public string Input { get; set; } = string.Empty;

        [JsonPropertyName("sessionToken")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? SessionToken { get; set; }

        [JsonPropertyName("languageCode")]
        public string LanguageCode { get; set; } = string.Empty;

        [JsonPropertyName("locationBias")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public LocationBias? LocationBias { get; set; }
    }

    private sealed class LocationBias
    {
        [JsonPropertyName("circle")]
        public Circle Circle { get; set; } = new();
    }

    private sealed class Circle
    {
        [JsonPropertyName("center")]
        public LatLng Center { get; set; } = new();

        [JsonPropertyName("radius")]
        public double Radius { get; set; }
    }

    private sealed class LatLng
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }
    }

    private sealed class AutocompleteResponse
    {
        [JsonPropertyName("suggestions")]
        public List<Suggestion>? Suggestions { get; set; }
    }

    private sealed class Suggestion
    {
        [JsonPropertyName("placePrediction")]
        public PlacePrediction? PlacePrediction { get; set; }
    }

    private sealed class PlacePrediction
    {
        [JsonPropertyName("placeId")]
        public string? PlaceId { get; set; }

        [JsonPropertyName("structuredFormat")]
        public StructuredFormat? StructuredFormat { get; set; }
    }

    private sealed class StructuredFormat
    {
        [JsonPropertyName("mainText")]
        public LocalizedText? MainText { get; set; }

        [JsonPropertyName("secondaryText")]
        public LocalizedText? SecondaryText { get; set; }
    }

    private sealed class LocalizedText
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class PlaceDetails
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("displayName")]
        public LocalizedText? DisplayName { get; set; }

        [JsonPropertyName("formattedAddress")]
        public string? FormattedAddress { get; set; }

        [JsonPropertyName("location")]
        public LatLng? Location { get; set; }

        [JsonPropertyName("googleMapsUri")]
        public string? GoogleMapsUri { get; set; }
    }
}
