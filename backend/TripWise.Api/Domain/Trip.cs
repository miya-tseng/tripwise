namespace TripWise.Api.Domain;

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

    public Activity AddActivity(string name, DateOnly date, TimeSlot timeSlot)
    {
        var normalizedName = Activity.NormalizeName(name);
        EnsureActivityFits(date, timeSlot, excludedActivityId: null);

        var activity = Activity.Create(normalizedName, date, timeSlot);
        _activities.Add(activity);
        return activity;
    }

    public Activity UpdateActivity(Guid activityId, string name, DateOnly date, TimeSlot timeSlot)
    {
        var activity = FindActivity(activityId);
        var normalizedName = Activity.NormalizeName(name);
        EnsureActivityFits(date, timeSlot, excludedActivityId: activityId);

        activity.Update(normalizedName, date, timeSlot);
        return activity;
    }

    public void RemoveActivity(Guid activityId)
    {
        _activities.Remove(FindActivity(activityId));
    }

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
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            throw new DomainException("Trip name is required.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Trip name cannot exceed {MaxNameLength} characters.");
        }

        var period = new DateRange(start, end);

        if (period.Days > MaxDays)
        {
            throw new DomainException($"A trip cannot be longer than {MaxDays} days.");
        }

        return new Trip
        {
            Id = Guid.CreateVersion7(),
            Name = trimmedName,
            Period = period,
        };
    }
}
