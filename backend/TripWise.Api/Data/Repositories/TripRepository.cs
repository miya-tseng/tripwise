using Microsoft.EntityFrameworkCore;
using TripWise.Api.Domain;

namespace TripWise.Api.Data.Repositories;

public class TripRepository(AppDbContext dbContext) : ITripRepository
{
    public async Task AddAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        dbContext.Trips.Add(trip);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return dbContext.Trips.FirstOrDefaultAsync(trip => trip.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Trip>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Trips
            .OrderBy(trip => trip.Period.Start)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Trip trip, CancellationToken cancellationToken = default)
    {
        dbContext.Trips.Remove(trip);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
