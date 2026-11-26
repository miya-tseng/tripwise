using TripWise.Domain;

namespace TripWise.Application.Places;

public interface IPlaceSearch
{
    Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(
        string query,
        string? sessionToken,
        GeoPoint? near,
        PlaceSearchKind kind = PlaceSearchKind.Any,
        CancellationToken cancellationToken = default);

    Task<Place?> GetDetailsAsync(string placeId, string? sessionToken, CancellationToken cancellationToken = default);

    Task<Place?> FindAsync(string text, GeoPoint? near, CancellationToken cancellationToken = default);
}
