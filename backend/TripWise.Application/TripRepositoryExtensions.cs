using TripWise.Domain;

namespace TripWise.Application;

public static class TripRepositoryExtensions
{
    public static async Task<Trip> GetRequiredAsync(
        this ITripRepository repository,
        Guid tripId,
        CancellationToken cancellationToken = default) =>
        await repository.GetByIdAsync(tripId, cancellationToken)
        ?? throw new TripNotFoundException(tripId);
}
