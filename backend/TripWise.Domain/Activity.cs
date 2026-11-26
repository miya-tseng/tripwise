namespace TripWise.Domain;

public class Activity
{
    public const int MaxNameLength = 100;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public Money Cost { get; private set; } = Money.Zero;
    public Place? Place { get; private set; }

    private Activity() { }

    internal static Activity Create(string name, DateOnly date, TimeSlot timeSlot, Money cost, Place? place) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Date = date,
        TimeSlot = timeSlot,
        Cost = cost,
        Place = place,
    };

    internal void Update(string name, DateOnly date, TimeSlot timeSlot, Money cost, Place? place)
    {
        Name = name;
        Date = date;
        TimeSlot = timeSlot;
        Cost = cost;
        Place = place;
    }

    internal bool ConflictsWith(DateOnly date, TimeSlot timeSlot) =>
        Date == date && TimeSlot.OverlapsWith(timeSlot);

    internal static string NormalizeName(string name, Place? place = null)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName) && place is not null)
        {
            trimmedName = place.Name.Length > MaxNameLength ? place.Name[..MaxNameLength].TrimEnd() : place.Name;
        }

        if (string.IsNullOrEmpty(trimmedName))
        {
            throw new DomainException("Activity name is required.");
        }

        if (trimmedName.Length > MaxNameLength)
        {
            throw new DomainException($"Activity name cannot exceed {MaxNameLength} characters.");
        }

        return trimmedName;
    }
}
