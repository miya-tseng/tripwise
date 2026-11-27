using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Mappings;

public static class ChecklistMapping
{
    public static ChecklistDto ToChecklistDto(this Trip trip)
    {
        var items = trip.Checklist
            .OrderBy(item => item.Section)
            .ThenBy(item => item.Position)
            .Select(ToDto)
            .ToList();
        var bookings = trip.ToBookingItems();

        return new ChecklistDto
        {
            TripId = trip.Id,
            Items = items,
            Bookings = bookings,
            Done = items.Count(item => item.Done) + bookings.Count(item => item.Done),
            Total = items.Count + bookings.Count,
        };
    }

    public static IReadOnlyList<BookingItemDto> ToBookingItems(this Trip trip)
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

    private static ChecklistItemDto ToDto(ChecklistItem item) => new()
    {
        Id = item.Id,
        Text = item.Text,
        Section = item.Section,
        Done = item.Done,
        DueDate = item.DueDate,
        Position = item.Position,
    };

    private static BookingItemDto ToBookingItem(ExpenseKind kind, Guid id, string name, DateOnly date, BookingStatus status) => new()
    {
        Kind = kind,
        Id = id,
        Name = name,
        Date = date,
        BookingStatus = status,
        Done = status != BookingStatus.ToBook,
    };
}
