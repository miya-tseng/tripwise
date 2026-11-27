namespace TripWise.Domain;

public partial class Trip
{
    public const int MaxNameLength = 100;
    public const int MaxDays = 30;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;
    public Place? Destination { get; private set; }

    private readonly List<Activity> _activities = [];
    public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();

    private readonly List<Stay> _stays = [];
    public IReadOnlyCollection<Stay> Stays => _stays.AsReadOnly();

    private readonly List<ChecklistItem> _checklist = [];
    public IReadOnlyCollection<ChecklistItem> Checklist => _checklist.AsReadOnly();

    public DateOnly LastCheckOut => Period.End.AddDays(1);

    public IReadOnlyList<DateOnly> Nights =>
        Enumerable.Range(0, Period.Days).Select(Period.Start.AddDays).ToList();

    private Trip() { }

    public static Trip Create(string name, DateOnly start, DateOnly end, Place? destination = null)
    {
        return new Trip
        {
            Id = Guid.CreateVersion7(),
            Name = NormalizeName(name),
            Period = CreatePeriod(start, end),
            Destination = destination,
        };
    }

    public void ChangeDestination(Place? destination)
    {
        Destination = destination;
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
