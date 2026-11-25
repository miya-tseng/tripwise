using TripWise.Domain;
using TripWise.Application.Dtos;

namespace TripWise.Application.Services;

public class TripService(ITripRepository repository)
{
    public async Task<TripDto> CreateAsync(CreateTripDto dto, CancellationToken cancellationToken = default)
    {
        var trip = Trip.Create(dto.Name, dto.StartDate!.Value, dto.EndDate!.Value);

        await repository.AddAsync(trip, cancellationToken);

        return ToDto(trip);
    }

    public async Task<IReadOnlyList<TripDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var trips = await repository.GetAllAsync(cancellationToken);

        return trips.Select(ToDto).ToList();
    }

    public async Task<TripDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(id, cancellationToken);

        return trip is null ? null : ToDto(trip);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(id, cancellationToken);

        if (trip is null)
        {
            return false;
        }

        await repository.DeleteAsync(trip, cancellationToken);

        return true;
    }

    private static TripDto ToDto(Trip trip)
    {
        return new TripDto
        {
            Id = trip.Id,
            Name = trip.Name,
            StartDate = trip.Period.Start,
            EndDate = trip.Period.End,
            Days = trip.Period.Days,
        };
    }
}
