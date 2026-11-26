using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Application.Places;
using TripWise.Domain;

namespace TripWise.Application.Services;

public class ActivityService(
    ITripRepository repository,
    IExchangeRateProvider exchangeRateProvider,
    IPlaceSearch placeSearch)
{
    public async Task<ActivityDto?> AddAsync(Guid tripId, SaveActivityDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var place = await ResolvePlaceAsync(dto, currentPlace: null, cancellationToken);
        var activity = trip.AddActivity(
            dto.Name,
            dto.Date!.Value,
            ToTimeSlot(dto),
            ToCost(dto, rates),
            place,
            ToDetails(dto));
        await repository.UpdateAsync(trip, cancellationToken);

        return ToDto(activity, rates);
    }

    public async Task<ActivityDto?> UpdateAsync(Guid tripId, Guid activityId, SaveActivityDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var currentPlace = trip.Activities.FirstOrDefault(activity => activity.Id == activityId)?.Place;
        var place = await ResolvePlaceAsync(dto, currentPlace, cancellationToken);
        var activity = trip.UpdateActivity(
            activityId,
            dto.Name,
            dto.Date!.Value,
            ToTimeSlot(dto),
            ToCost(dto, rates),
            place,
            ToDetails(dto));
        await repository.UpdateAsync(trip, cancellationToken);

        return ToDto(activity, rates);
    }

    public async Task<ActivityDto?> MoveAsync(
        Guid tripId,
        Guid activityId,
        MoveActivityDto dto,
        CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var activity = trip.MoveActivity(activityId, dto.Date!.Value, dto.StartTime!.Value);
        await repository.UpdateAsync(trip, cancellationToken);
        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);

        return ToDto(activity, rates);
    }

    public async Task<bool> DeleteAsync(Guid tripId, Guid activityId, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return false;
        }

        trip.RemoveActivity(activityId);
        await repository.UpdateAsync(trip, cancellationToken);

        return true;
    }

    public async Task<ScheduleDto?> GetScheduleAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var activitiesByDate = trip.Activities.ToLookup(activity => activity.Date);

        var days = Enumerable.Range(0, trip.Period.Days)
            .Select(trip.Period.Start.AddDays)
            .Select(date =>
            {
                var activities = activitiesByDate[date].OrderBy(activity => activity.TimeSlot.Start).ToList();

                return new ScheduleDayDto
                {
                    Date = date,
                    TotalInTwd = TotalInHomeCurrency(activities, rates),
                    Totals = trip.GetDailyTotals(date).Select(ToMoneyDto).ToList(),
                    Activities = activities.Select(activity => ToDto(activity, rates)).ToList(),
                };
            })
            .ToList();

        return new ScheduleDto
        {
            TripId = trip.Id,
            RatesAvailable = rates is not null,
            RatesUpdatedAt = rates?.UpdatedAt,
            TotalInTwd = TotalInHomeCurrency(trip.Activities, rates),
            Totals = trip.GetTotals().Select(ToMoneyDto).ToList(),
            Days = days,
        };
    }

    private static decimal? TotalInHomeCurrency(IEnumerable<Activity> activities, ExchangeRates? rates)
    {
        if (rates is null)
        {
            return null;
        }

        var converted = activities.Select(activity => rates.ToHomeCurrency(activity.Cost)).ToList();

        return converted.Any(amount => amount is null) ? null : converted.Sum(amount => amount!.Value);
    }

    private async Task<Place?> ResolvePlaceAsync(
        SaveActivityDto dto,
        Place? currentPlace,
        CancellationToken cancellationToken)
    {
        var placeId = dto.PlaceId?.Trim();

        if (string.IsNullOrEmpty(placeId))
        {
            return null;
        }

        if (currentPlace?.PlaceId == placeId)
        {
            return currentPlace;
        }

        return await placeSearch.GetDetailsAsync(placeId, dto.PlaceSessionToken, cancellationToken)
            ?? throw new DomainException("Place not found.");
    }

    private static ActivityDetails ToDetails(SaveActivityDto dto) => new(
        dto.Category ?? ActivityCategory.Other,
        dto.BookingStatus ?? BookingStatus.NotNeeded,
        dto.Notes,
        dto.ConfirmationCode,
        dto.Link);

    private static TimeSlot ToTimeSlot(SaveActivityDto dto) =>
        new(dto.StartTime!.Value, dto.EndTime!.Value);

    private static Money? ToCost(SaveActivityDto dto, ExchangeRates? rates)
    {
        if (dto.Cost is not { } amount)
        {
            return null;
        }

        var cost = new Money(amount, dto.Currency ?? Money.HomeCurrency);

        if (rates is not null && !rates.Supports(cost.Currency))
        {
            throw new DomainException($"Currency {cost.Currency} is not supported.");
        }

        return cost;
    }

    private static MoneyDto ToMoneyDto(Money money) => new()
    {
        Amount = money.Amount,
        Currency = money.Currency,
    };

    private static ActivityDto ToDto(Activity activity, ExchangeRates? rates)
    {
        return new ActivityDto
        {
            Id = activity.Id,
            Name = activity.Name,
            Date = activity.Date,
            StartTime = activity.TimeSlot.Start,
            EndTime = activity.TimeSlot.End,
            Cost = activity.Cost.Amount,
            Currency = activity.Cost.Currency,
            CostInTwd = rates?.ToHomeCurrency(activity.Cost),
            Place = activity.Place is null ? null : PlaceService.ToDto(activity.Place),
            Category = activity.Details.Category,
            BookingStatus = activity.Details.BookingStatus,
            Notes = activity.Details.Notes,
            ConfirmationCode = activity.Details.ConfirmationCode,
            Link = activity.Details.Link,
        };
    }
}
