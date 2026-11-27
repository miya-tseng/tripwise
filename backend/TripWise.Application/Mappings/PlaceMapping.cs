using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Mappings;

public static class PlaceMapping
{
    public static PlaceDto ToDto(this Place place) => new()
    {
        PlaceId = place.PlaceId,
        Name = place.Name,
        Address = place.Address,
        Latitude = place.Latitude,
        Longitude = place.Longitude,
        GoogleMapsUri = place.GoogleMapsUri,
    };
}
