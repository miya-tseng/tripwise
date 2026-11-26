using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Services;

public class ActivityService(
    ITripRepository repository,
    IExchangeRateProvider exchangeRateProvider,
    PlaceResolver placeResolver)
{
    public async Task<ActivityDto?> AddAsync(Guid tripId, SaveActivityDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var place = await placeResolver.ResolveAsync(dto.PlaceId, dto.PlaceSessionToken, currentPlace: null, cancellationToken);
        var activity = trip.AddActivity(
            dto.Name,
            dto.Date!.Value,
            ToTimeSlot(dto),
            CostMapping.ToCost(dto.Cost, dto.Currency, rates),
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
        var place = await placeResolver.ResolveAsync(dto.PlaceId, dto.PlaceSessionToken, currentPlace, cancellationToken);
        var activity = trip.UpdateActivity(
            activityId,
            dto.Name,
            dto.Date!.Value,
            ToTimeSlot(dto),
            CostMapping.ToCost(dto.Cost, dto.Currency, rates),
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
        var stays = trip.Stays.OrderBy(stay => stay.CheckIn).Select(stay => StayService.ToDto(stay, rates)).ToList();

        var days = Enumerable.Range(0, trip.Period.Days)
            .Select(trip.Period.Start.AddDays)
            .Select(date =>
            {
                var activities = activitiesByDate[date].OrderBy(activity => activity.TimeSlot.Start).ToList();

                return new ScheduleDayDto
                {
                    Date = date,
                    TotalInTwd = CostMapping.TotalInHomeCurrency(trip.GetDailyCosts(date), rates),
                    Totals = trip.GetDailyTotals(date).Select(CostMapping.ToMoneyDto).ToList(),
                    Activities = activities.Select(activity => ToDto(activity, rates)).ToList(),
                    Stay = stays.FirstOrDefault(stay => stay.CheckIn <= date && date < stay.CheckOut),
                    CheckingOut = stays.FirstOrDefault(stay => stay.CheckOut == date),
                };
            })
            .ToList();

        return new ScheduleDto
        {
            TripId = trip.Id,
            RatesAvailable = rates is not null,
            RatesUpdatedAt = rates?.UpdatedAt,
            TotalInTwd = CostMapping.TotalInHomeCurrency(trip.GetAllCosts(), rates),
            Totals = trip.GetTotals().Select(CostMapping.ToMoneyDto).ToList(),
            Stays = stays,
            Days = days,
        };
    }

    private static ActivityDetails ToDetails(SaveActivityDto dto) => new(
        dto.Category ?? ActivityCategory.Other,
        dto.BookingStatus ?? BookingStatus.NotNeeded,
        dto.Notes,
        dto.ConfirmationCode,
        dto.Link);

    private static TimeSlot ToTimeSlot(SaveActivityDto dto) =>
        new(dto.StartTime!.Value, dto.EndTime!.Value);

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
