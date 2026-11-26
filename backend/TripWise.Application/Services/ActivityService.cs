using TripWise.Domain;
using TripWise.Application.Dtos;

namespace TripWise.Application.Services;

public class ActivityService(ITripRepository repository)
{
    public async Task<ActivityDto?> AddAsync(Guid tripId, SaveActivityDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var activity = trip.AddActivity(dto.Name, dto.Date!.Value, ToTimeSlot(dto), ToCost(dto));
        await repository.UpdateAsync(trip, cancellationToken);

        return ToDto(activity);
    }

    public async Task<ActivityDto?> UpdateAsync(Guid tripId, Guid activityId, SaveActivityDto dto, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetByIdAsync(tripId, cancellationToken);

        if (trip is null)
        {
            return null;
        }

        var activity = trip.UpdateActivity(activityId, dto.Name, dto.Date!.Value, ToTimeSlot(dto), ToCost(dto));
        await repository.UpdateAsync(trip, cancellationToken);

        return ToDto(activity);
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

        var activitiesByDate = trip.Activities.ToLookup(activity => activity.Date);

        var days = Enumerable.Range(0, trip.Period.Days)
            .Select(trip.Period.Start.AddDays)
            .Select(date => new ScheduleDayDto
            {
                Date = date,
                Total = HomeCurrencyAmount(trip.GetDailyTotals(date)),
                Activities = activitiesByDate[date]
                    .OrderBy(activity => activity.TimeSlot.Start)
                    .Select(ToDto)
                    .ToList(),
            })
            .ToList();

        return new ScheduleDto
        {
            TripId = trip.Id,
            Total = HomeCurrencyAmount(trip.GetTotals()),
            Days = days,
        };
    }

    private static decimal HomeCurrencyAmount(IEnumerable<Money> totals) =>
        totals.Where(total => total.Currency == Money.HomeCurrency).Sum(total => total.Amount);

    private static TimeSlot ToTimeSlot(SaveActivityDto dto) =>
        new(dto.StartTime!.Value, dto.EndTime!.Value);

    private static Money? ToCost(SaveActivityDto dto) =>
        dto.Cost is { } amount ? new Money(amount) : null;

    private static ActivityDto ToDto(Activity activity)
    {
        return new ActivityDto
        {
            Id = activity.Id,
            Name = activity.Name,
            Date = activity.Date,
            StartTime = activity.TimeSlot.Start,
            EndTime = activity.TimeSlot.End,
            Cost = activity.Cost.Amount,
        };
    }
}
