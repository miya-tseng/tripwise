using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.Application.Mappings;

namespace TripWise.Application.Services;

public class ChecklistService(ITripRepository repository)
{
    public async Task<ChecklistDto> GetAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        return trip.ToChecklistDto();
    }

    public Task<ChecklistDto> AddAsync(Guid tripId, SaveChecklistItemDto dto, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.AddChecklistItem(dto.Text, dto.Section!.Value, dto.DueDate), cancellationToken);

    public Task<ChecklistDto> UpdateAsync(Guid tripId, Guid itemId, SaveChecklistItemDto dto, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.UpdateChecklistItem(itemId, dto.Text, dto.Section!.Value, dto.DueDate), cancellationToken);

    public Task<ChecklistDto> SetDoneAsync(Guid tripId, Guid itemId, bool done, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.SetChecklistItemDone(itemId, done), cancellationToken);

    public Task<ChecklistDto> DeleteAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.RemoveChecklistItem(itemId), cancellationToken);

    public Task<ChecklistDto> ReorderAsync(Guid tripId, ReorderChecklistDto dto, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.ReorderChecklist(dto.Section!.Value, dto.ItemIds), cancellationToken);

    public Task<ChecklistDto> SetActivityBookedAsync(Guid tripId, Guid activityId, bool booked, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.SetActivityBooked(activityId, booked), cancellationToken);

    public Task<ChecklistDto> SetStayBookedAsync(Guid tripId, Guid stayId, bool booked, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.SetStayBooked(stayId, booked), cancellationToken);

    private async Task<ChecklistDto> ChangeAsync(Guid tripId, Action<Trip> change, CancellationToken cancellationToken)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        change(trip);
        await repository.UpdateAsync(trip, cancellationToken);

        return trip.ToChecklistDto();
    }
}
