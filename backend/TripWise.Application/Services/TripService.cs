using TripWise.Domain;
using TripWise.Application.Dtos;
using TripWise.Application.Mappings;

namespace TripWise.Application.Services;

public class TripService(ITripRepository repository, PlaceResolver placeResolver)
{
    public async Task<TripDto> CreateAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
    {
        var destination = await placeResolver.ResolveAsync(
            dto.DestinationPlaceId,
            dto.PlaceSessionToken,
            currentPlace: null,
            cancellationToken);
        var trip = Trip.Create(dto.Name, dto.StartDate!.Value, dto.EndDate!.Value, destination);

        await repository.AddAsync(trip, cancellationToken);

        return trip.ToDto();
    }

    public async Task<IReadOnlyList<TripDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var trips = await repository.GetAllAsync(cancellationToken);

        return trips.Select(TripMapping.ToDto).ToList();
    }

    public async Task<TripDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(id, cancellationToken);

        return trip.ToDto();
    }

    public async Task<TripDto> UpdateAsync(Guid id, UpdateTripDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(id, cancellationToken);

        var destination = await placeResolver.ResolveAsync(
            dto.DestinationPlaceId,
            dto.PlaceSessionToken,
            trip.Destination,
            cancellationToken);

        trip.Reschedule(dto.StartDate!.Value, dto.EndDate!.Value);
        trip.Rename(dto.Name);
        trip.ChangeDestination(destination);
        await repository.UpdateAsync(trip, cancellationToken);

        return trip.ToDto();
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(id, cancellationToken);

        await repository.DeleteAsync(trip, cancellationToken);
    }
}
