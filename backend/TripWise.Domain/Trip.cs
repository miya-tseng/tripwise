namespace TripWise.Domain;

public class Trip
{
    public const int MaxNameLength = 100;
    public const int MaxDays = 30;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;

    private readonly List<Activity> _activities = [];
    public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();

    private readonly List<Stay> _stays = [];
    public IReadOnlyCollection<Stay> Stays => _stays.AsReadOnly();

    public DateOnly LastCheckOut => Period.End.AddDays(1);

    public IReadOnlyList<DateOnly> Nights =>
        Enumerable.Range(0, Period.Days).Select(Period.Start.AddDays).ToList();

    private Trip() { }

    public Activity AddActivity(
        string name,
        DateOnly date,
        TimeSlot timeSlot,
        Money? cost = null,
        Place? place = null,
        ActivityDetails? details = null)
    {
        var normalizedName = Activity.NormalizeName(name, place);
        EnsureActivityFits(date, timeSlot, excludedActivityId: null);

        var activity = Activity.Create(
            normalizedName,
            date,
            timeSlot,
            cost ?? Money.Zero,
            place,
            details ?? ActivityDetails.Default);
        _activities.Add(activity);
        return activity;
    }

    public Activity UpdateActivity(
        Guid activityId,
        string name,
        DateOnly date,
        TimeSlot timeSlot,
        Money? cost = null,
        Place? place = null,
        ActivityDetails? details = null)
    {
        var activity = FindActivity(activityId);
        var normalizedName = Activity.NormalizeName(name, place);
        EnsureActivityFits(date, timeSlot, excludedActivityId: activityId);

        activity.Update(normalizedName, date, timeSlot, cost ?? Money.Zero, place, details ?? ActivityDetails.Default);
        return activity;
    }

    public Activity MoveActivity(Guid activityId, DateOnly date, TimeOnly startTime)
    {
        var activity = FindActivity(activityId);
        var timeSlot = activity.TimeSlot.StartingAt(startTime);
        EnsureActivityFits(date, timeSlot, excludedActivityId: activityId);

        activity.MoveTo(date, timeSlot);
        return activity;
    }

    public Stay AddStay(StayDetails details)
    {
        EnsureStayFits(details, excludedStayId: null);

        var stay = Stay.Create(details);
        _stays.Add(stay);
        return stay;
    }

    public Stay UpdateStay(Guid stayId, StayDetails details)
    {
        var stay = FindStay(stayId);
        EnsureStayFits(details, excludedStayId: stayId);

        stay.Update(details);
        return stay;
    }

    public void RemoveStay(Guid stayId)
    {
        _stays.Remove(FindStay(stayId));
    }

    public Stay? StayForNight(DateOnly night) => _stays.FirstOrDefault(stay => stay.CoversNight(night));

    private Stay FindStay(Guid stayId) =>
        _stays.FirstOrDefault(stay => stay.Id == stayId)
        ?? throw new StayNotFoundException(stayId);

    private void EnsureStayFits(StayDetails details, Guid? excludedStayId)
    {
        if (details.CheckIn < Period.Start || details.CheckOut > LastCheckOut)
        {
            throw new DomainException("Stay dates must be within the trip dates.");
        }

        var conflictingStay = _stays.FirstOrDefault(stay =>
            stay.Id != excludedStayId && stay.OverlapsWith(details.CheckIn, details.CheckOut));

        if (conflictingStay is not null)
        {
            throw new StayOverlapException(conflictingStay);
        }
    }

    public void RemoveActivity(Guid activityId)
    {
        _activities.Remove(FindActivity(activityId));
    }

    public IReadOnlyList<Money> GetDailyTotals(DateOnly date) =>
        Money.SumByCurrency(
            _activities
                .Where(activity => activity.Date == date)
                .Select(activity => activity.Cost)
                .Concat(PaidStays.Where(stay => stay.CoversNight(date)).Select(stay => stay.CostForNight(date))));

    public IReadOnlyList<Money> GetTotals() =>
        Money.SumByCurrency(_activities.Select(activity => activity.Cost).Concat(PaidStays.Select(stay => stay.Cost)));

    private IEnumerable<Stay> PaidStays => _stays.Where(stay => stay.Cost.Amount > 0);

    private Activity FindActivity(Guid activityId) =>
        _activities.FirstOrDefault(activity => activity.Id == activityId)
        ?? throw new ActivityNotFoundException(activityId);

    private void EnsureActivityFits(DateOnly date, TimeSlot timeSlot, Guid? excludedActivityId)
    {
        ArgumentNullException.ThrowIfNull(timeSlot);

        if (!Period.Contains(date))
        {
            throw new DomainException("Activity date must be within the trip dates.");
        }

        var conflictingActivity = _activities.FirstOrDefault(activity =>
            activity.Id != excludedActivityId && activity.ConflictsWith(date, timeSlot));

        if (conflictingActivity is not null)
        {
            throw new ActivityOverlapException(conflictingActivity);
        }
    }

    public static Trip Create(string name, DateOnly start, DateOnly end)
    {
        return new Trip
        {
            Id = Guid.CreateVersion7(),
            Name = NormalizeName(name),
            Period = CreatePeriod(start, end),
        };
    }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Reschedule(DateOnly start, DateOnly end)
    {
        var period = CreatePeriod(start, end);
        var activitiesOutside = _activities.Count(activity => !period.Contains(activity.Date));

        if (activitiesOutside > 0)
        {
            throw new TripDatesConflictException(activitiesOutside);
        }

        if (_stays.Any(stay => stay.CheckIn < period.Start || stay.CheckOut > period.End.AddDays(1)))
        {
            throw new DomainException("A stay falls outside the new dates. Change or delete it first.");
        }

        Period = period;
    }

    private static string NormalizeName(string name)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            throw new DomainException("Trip name is required.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Trip name cannot exceed {MaxNameLength} characters.");
        }

        return trimmedName;
    }

    private static DateRange CreatePeriod(DateOnly start, DateOnly end)
    {
        var period = new DateRange(start, end);

        if (period.Days > MaxDays)
        {
            throw new DomainException($"A trip cannot be longer than {MaxDays} days.");
        }

        return period;
    }
}
