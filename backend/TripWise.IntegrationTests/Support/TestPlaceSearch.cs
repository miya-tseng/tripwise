using TripWise.Application;
using TripWise.Application.Places;
using TripWise.Domain;
using TripWise.Infrastructure.Places;

namespace TripWise.IntegrationTests.Support;

public class TestPlaceSearch : IPlaceSearch
{
    private readonly FakePlaceSearch _fake = new();

    public bool Available { get; set; } = true;
    public int DetailsRequests { get; private set; }

    public void Reset()
    {
        Available = true;
        DetailsRequests = 0;
    }

    public Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(
        string query,
        string? sessionToken,
        GeoPoint? near,
        PlaceSearchKind kind = PlaceSearchKind.Any,
        CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        return _fake.AutocompleteAsync(query, sessionToken, near, kind, cancellationToken);
    }

    public Task<Place?> GetDetailsAsync(string placeId, string? sessionToken, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        DetailsRequests++;
        return _fake.GetDetailsAsync(placeId, sessionToken, cancellationToken);
    }

    public Task<Place?> FindAsync(string text, GeoPoint? near, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        return _fake.FindAsync(text, near, cancellationToken);
    }

    private void EnsureAvailable()
    {
        if (!Available)
        {
            throw new ExternalServiceUnavailableException("Place search is unavailable right now. Please try again later.");
        }
    }
}
