namespace TripWise.Domain;

public partial class Trip
{
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
}
