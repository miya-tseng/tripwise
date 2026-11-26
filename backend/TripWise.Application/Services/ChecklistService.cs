using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Services;

public class ChecklistService(ITripRepository repository)
{
    public async Task<ChecklistDto?> GetAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        return trip is null ? null : ToDto(trip);
    }

    public Task<ChecklistDto?> AddAsync(Guid tripId, SaveChecklistItemDto dto, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.AddChecklistItem(dto.Text, dto.Section!.Value, dto.DueDate), cancellationToken);

    public Task<ChecklistDto?> UpdateAsync(Guid tripId, Guid itemId, SaveChecklistItemDto dto, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.UpdateChecklistItem(itemId, dto.Text, dto.Section!.Value, dto.DueDate), cancellationToken);

    public Task<ChecklistDto?> SetDoneAsync(Guid tripId, Guid itemId, bool done, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.SetChecklistItemDone(itemId, done), cancellationToken);

    public Task<ChecklistDto?> DeleteAsync(Guid tripId, Guid itemId, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.RemoveChecklistItem(itemId), cancellationToken);

    public Task<ChecklistDto?> ReorderAsync(Guid tripId, ReorderChecklistDto dto, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.ReorderChecklist(dto.Section!.Value, dto.ItemIds), cancellationToken);

    public Task<ChecklistDto?> SetActivityBookedAsync(Guid tripId, Guid activityId, bool booked, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.SetActivityBooked(activityId, booked), cancellationToken);

    public Task<ChecklistDto?> SetStayBookedAsync(Guid tripId, Guid stayId, bool booked, CancellationToken cancellationToken = default) =>
        ChangeAsync(tripId, trip => trip.SetStayBooked(stayId, booked), cancellationToken);

    public static IReadOnlyList<BookingItemDto> BookingItems(Trip trip)
    {
        var activities = trip.Activities
            .Where(activity => activity.Details.BookingStatus != BookingStatus.NotNeeded)
            .Select(activity => (
                Start: activity.TimeSlot.Start,
                Item: ToBookingItem(ExpenseKind.Activity, activity.Id, activity.Name, activity.Date, activity.Details.BookingStatus)));
        var stays = trip.Stays
            .Where(stay => stay.BookingStatus != BookingStatus.NotNeeded)
            .Select(stay => (
                Start: TimeOnly.MinValue,
                Item: ToBookingItem(ExpenseKind.Stay, stay.Id, stay.Name, stay.CheckIn, stay.BookingStatus)));

        return activities.Concat(stays)
            .OrderBy(entry => entry.Item.Date)
            .ThenBy(entry => entry.Start)
            .Select(entry => entry.Item)
            .ToList();
    }

    private static BookingItemDto ToBookingItem(ExpenseKind kind, Guid id, string name, DateOnly date, BookingStatus status) => new()
    {
        Kind = kind,
        Id = id,
        Name = name,
        Date = date,
        BookingStatus = status,
        Done = status != BookingStatus.ToBook,
    };

    private async Task<ChecklistDto?> ChangeAsync(Guid tripId, Action<Trip> change, CancellationToken cancellationToken)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        change(trip);
        await repository.UpdateAsync(trip, cancellationToken);

        return ToDto(trip);
    }

    private static ChecklistDto ToDto(Trip trip)
    {
        var items = trip.Checklist
            .OrderBy(item => item.Section)
            .ThenBy(item => item.Position)
            .Select(item => new ChecklistItemDto
            {
                Id = item.Id,
                Text = item.Text,
                Section = item.Section,
                Done = item.Done,
                DueDate = item.DueDate,
                Position = item.Position,
            })
            .ToList();
        var bookings = BookingItems(trip);

        return new ChecklistDto
        {
            TripId = trip.Id,
            Items = items,
            Bookings = bookings,
            Done = items.Count(item => item.Done) + bookings.Count(item => item.Done),
            Total = items.Count + bookings.Count,
        };
    }
}
