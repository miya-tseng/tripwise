using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.Application.Mappings;

namespace TripWise.Application.Services;

public class StayService(
    ITripRepository repository,
    IExchangeRateProvider exchangeRateProvider,
    PlaceResolver placeResolver)
{
    public async Task<StayDto> AddAsync(Guid tripId, SaveStayDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var details = await ToDetailsAsync(dto, currentPlace: null, rates, cancellationToken);
        var stay = trip.AddStay(details);
        await repository.UpdateAsync(trip, cancellationToken);

        return stay.ToDto(rates);
    }

    public async Task<StayDto> UpdateAsync(Guid tripId, Guid stayId, SaveStayDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var currentPlace = trip.Stays.FirstOrDefault(stay => stay.Id == stayId)?.Place;
        var details = await ToDetailsAsync(dto, currentPlace, rates, cancellationToken);
        var stay = trip.UpdateStay(stayId, details);
        await repository.UpdateAsync(trip, cancellationToken);

        return stay.ToDto(rates);
    }

    public async Task DeleteAsync(Guid tripId, Guid stayId, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        trip.RemoveStay(stayId);
        await repository.UpdateAsync(trip, cancellationToken);
    }

    private async Task<StayDetails> ToDetailsAsync(
        SaveStayDto dto,
        Place? currentPlace,
        ExchangeRates? rates,
        CancellationToken cancellationToken)
    {
        var place = await placeResolver.ResolveAsync(dto.PlaceId, dto.PlaceSessionToken, currentPlace, cancellationToken);

        return new StayDetails(
            dto.Name,
            dto.CheckIn!.Value,
            dto.CheckOut!.Value,
            place,
            CostMapping.ToCost(dto.Cost, dto.Currency, rates),
            dto.BookingStatus ?? BookingStatus.NotNeeded,
            dto.ConfirmationCode);
    }
}
