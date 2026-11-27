using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Mappings;

public static class ActivityMapping
{
    public static ActivityDto ToDto(this Activity activity, ExchangeRates? rates) => new()
    {
        Id = activity.Id,
        Name = activity.Name,
        Date = activity.Date,
        StartTime = activity.TimeSlot.Start,
        EndTime = activity.TimeSlot.End,
        Cost = activity.Cost.Amount,
        Currency = activity.Cost.Currency,
        CostInTwd = rates?.ToHomeCurrency(activity.Cost),
        Place = activity.Place?.ToDto(),
        Category = activity.Details.Category,
        BookingStatus = activity.Details.BookingStatus,
        Notes = activity.Details.Notes,
        ConfirmationCode = activity.Details.ConfirmationCode,
        Link = activity.Details.Link,
    };
}
