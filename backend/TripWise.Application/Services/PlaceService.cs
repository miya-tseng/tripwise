using TripWise.Application.Dtos;
using TripWise.Application.Places;
using TripWise.Domain;
using TripWise.Application.Mappings;

namespace TripWise.Application.Services;

public class PlaceService(IPlaceSearch placeSearch)
{
    public const int MaxQueryLength = 100;

    public async Task<IReadOnlyList<PlaceSuggestionDto>> AutocompleteAsync(
        string? query,
        string? sessionToken,
        double? latitude,
        double? longitude,
        PlaceSearchKind kind = PlaceSearchKind.Any,
        CancellationToken cancellationToken = default)
    {
        var trimmedQuery = query?.Trim() ?? string.Empty;

        if (trimmedQuery.Length == 0)
        {
            return [];
        }

        if (trimmedQuery.Length > MaxQueryLength)
        {
            throw new DomainException($"Search text cannot exceed {MaxQueryLength} characters.");
        }

        var near = latitude is { } lat && longitude is { } lng ? new GeoPoint(lat, lng) : null;
        var suggestions = await placeSearch.AutocompleteAsync(trimmedQuery, sessionToken, near, kind, cancellationToken);

        return suggestions
            .Select(suggestion => new PlaceSuggestionDto
            {
                PlaceId = suggestion.PlaceId,
                Name = suggestion.Name,
                Description = suggestion.Description,
            })
            .ToList();
    }

    public async Task<PlaceDto?> GetDetailsAsync(
        string placeId,
        string? sessionToken,
        CancellationToken cancellationToken = default)
    {
        var place = await placeSearch.GetDetailsAsync(placeId, sessionToken, cancellationToken);

        return place?.ToDto();
    }
}
