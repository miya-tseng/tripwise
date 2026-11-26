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

    private Trip() { }

    public Activity AddActivity(
        string name,
        DateOnly date,
        TimeSlot timeSlot,
        Money? cost = null,
        Place? place = null)
    {
        var normalizedName = Activity.NormalizeName(name, place);
        EnsureActivityFits(date, timeSlot, excludedActivityId: null);

        var activity = Activity.Create(normalizedName, date, timeSlot, cost ?? Money.Zero, place);
        _activities.Add(activity);
        return activity;
    }

    public Activity UpdateActivity(
        Guid activityId,
        string name,
        DateOnly date,
        TimeSlot timeSlot,
        Money? cost = null,
        Place? place = null)
    {
        var activity = FindActivity(activityId);
        var normalizedName = Activity.NormalizeName(name, place);
        EnsureActivityFits(date, timeSlot, excludedActivityId: activityId);

        activity.Update(normalizedName, date, timeSlot, cost ?? Money.Zero, place);
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

    public void RemoveActivity(Guid activityId)
    {
        _activities.Remove(FindActivity(activityId));
    }

    public IReadOnlyList<Money> GetDailyTotals(DateOnly date) =>
        Money.SumByCurrency(_activities.Where(activity => activity.Date == date).Select(activity => activity.Cost));

    public IReadOnlyList<Money> GetTotals() => Money.SumByCurrency(_activities.Select(activity => activity.Cost));

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
