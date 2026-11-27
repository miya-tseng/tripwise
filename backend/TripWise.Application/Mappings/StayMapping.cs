using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Mappings;

public static class StayMapping
{
    public static StayDto ToDto(this Stay stay, ExchangeRates? rates) => new()
    {
        Id = stay.Id,
        Name = stay.Name,
        CheckIn = stay.CheckIn,
        CheckOut = stay.CheckOut,
        Nights = stay.Nights,
        Cost = stay.Cost.Amount,
        Currency = stay.Cost.Currency,
        CostInTwd = rates?.ToHomeCurrency(stay.Cost),
        Place = stay.Place?.ToDto(),
        BookingStatus = stay.BookingStatus,
        ConfirmationCode = stay.ConfirmationCode,
    };
}
