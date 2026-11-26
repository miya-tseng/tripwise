namespace TripWise.Domain;

public enum ExpenseKind
{
    Activity,
    Stay,
}

public sealed record Expense(ExpenseKind Kind, Guid Id, string Name, DateOnly Date, Money Cost);

public sealed record CategorySpend(ActivityCategory Category, int ActivityCount, IReadOnlyList<Money> Costs);

public sealed record DaySpend(DateOnly Date, int DayNumber, int ActivityCount, IReadOnlyList<Money> ActivityCosts, Money? StayCost);

public sealed record BookingProgress(int ToBook, int Booked, int Paid)
{
    public int NeedingBooking => ToBook + Booked + Paid;
    public int Done => Booked + Paid;
}

public sealed class TripOverview
{
    public required int Days { get; init; }
    public required int Nights { get; init; }
    public required int ActivityCount { get; init; }
    public required int PlaceCount { get; init; }
    public required int NightsWithStay { get; init; }
    public required BookingProgress Booking { get; init; }
    public required IReadOnlyList<Activity> ActivitiesToBook { get; init; }
    public required IReadOnlyList<Stay> StaysToBook { get; init; }
    public required IReadOnlyList<DateOnly> NightsWithoutStay { get; init; }
    public required IReadOnlyList<DateOnly> EmptyDays { get; init; }
    public required IReadOnlyList<CategorySpend> SpendByCategory { get; init; }
    public required IReadOnlyList<DaySpend> SpendByDay { get; init; }
    public required IReadOnlyList<Expense> Expenses { get; init; }

    public static TripOverview For(Trip trip)
    {
        ArgumentNullException.ThrowIfNull(trip);

        var activities = trip.Activities.OrderBy(activity => activity.Date).ThenBy(activity => activity.TimeSlot.Start).ToList();
        var stays = trip.Stays.OrderBy(stay => stay.CheckIn).ToList();
        var dates = trip.Nights;
        var statuses = activities.Select(activity => activity.Details.BookingStatus)
            .Concat(stays.Select(stay => stay.BookingStatus))
            .ToList();

        return new TripOverview
        {
            Days = trip.Period.Days,
            Nights = dates.Count,
            ActivityCount = activities.Count,
            PlaceCount = activities
                .Where(activity => activity.Place is not null)
                .Select(activity => activity.Place!.PlaceId)
                .Distinct()
                .Count(),
            NightsWithStay = dates.Count(date => stays.Any(stay => stay.CoversNight(date))),
            Booking = new BookingProgress(
                statuses.Count(status => status == BookingStatus.ToBook),
                statuses.Count(status => status == BookingStatus.Booked),
                statuses.Count(status => status == BookingStatus.Paid)),
            ActivitiesToBook = activities.Where(activity => activity.Details.NeedsBooking).ToList(),
            StaysToBook = stays.Where(stay => stay.BookingStatus == BookingStatus.ToBook).ToList(),
            NightsWithoutStay = dates.Where(date => !stays.Any(stay => stay.CoversNight(date))).ToList(),
            EmptyDays = dates.Where(date => activities.All(activity => activity.Date != date)).ToList(),
            SpendByCategory = activities
                .GroupBy(activity => activity.Details.Category)
                .Select(group => new CategorySpend(
                    group.Key,
                    group.Count(),
                    group.Select(activity => activity.Cost).Where(cost => cost.Amount > 0).ToList()))
                .OrderBy(spend => spend.Category)
                .ToList(),
            SpendByDay = dates
                .Select((date, index) =>
                {
                    var dayActivities = activities.Where(activity => activity.Date == date).ToList();
                    var stay = stays.FirstOrDefault(candidate => candidate.CoversNight(date) && candidate.Cost.Amount > 0);

                    return new DaySpend(
                        date,
                        index + 1,
                        dayActivities.Count,
                        dayActivities.Select(activity => activity.Cost).Where(cost => cost.Amount > 0).ToList(),
                        stay?.CostForNight(date));
                })
                .ToList(),
            Expenses = activities
                .Where(activity => activity.Cost.Amount > 0)
                .Select(activity => new Expense(ExpenseKind.Activity, activity.Id, activity.Name, activity.Date, activity.Cost))
                .Concat(stays
                    .Where(stay => stay.Cost.Amount > 0)
                    .Select(stay => new Expense(ExpenseKind.Stay, stay.Id, stay.Name, stay.CheckIn, stay.Cost)))
                .ToList(),
        };
    }
}
