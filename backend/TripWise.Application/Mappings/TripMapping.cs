using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Mappings;

public static class TripMapping
{
    public static TripDto ToDto(this Trip trip) => new()
    {
        Id = trip.Id,
        Name = trip.Name,
        StartDate = trip.Period.Start,
        EndDate = trip.Period.End,
        Days = trip.Period.Days,
        Destination = trip.Destination?.ToDto(),
    };
}
