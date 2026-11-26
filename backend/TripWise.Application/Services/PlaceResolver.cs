using TripWise.Application.Places;
using TripWise.Domain;

namespace TripWise.Application.Services;

public class PlaceResolver(IPlaceSearch placeSearch)
{
    public async Task<Place?> ResolveAsync(
        string? placeId,
        string? sessionToken,
        Place? currentPlace,
        CancellationToken cancellationToken)
    {
        var trimmedId = placeId?.Trim();

        if (string.IsNullOrEmpty(trimmedId))
        {
            return null;
        }

        if (currentPlace?.PlaceId == trimmedId)
        {
            return currentPlace;
        }

        return await placeSearch.GetDetailsAsync(trimmedId, sessionToken, cancellationToken)
            ?? throw new DomainException("Place not found.");
    }
}
