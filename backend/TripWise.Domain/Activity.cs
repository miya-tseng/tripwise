namespace TripWise.Domain;

public class Activity
{
    public const int MaxNameLength = 100;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateOnly Date { get; private set; }
    public TimeSlot TimeSlot { get; private set; } = null!;
    public Money Cost { get; private set; } = Money.Zero;

    private Activity() { }

    internal static Activity Create(string name, DateOnly date, TimeSlot timeSlot, Money cost) => new()
    {
        Id = Guid.CreateVersion7(),
        Name = name,
        Date = date,
        TimeSlot = timeSlot,
        Cost = cost,
    };

    internal void Update(string name, DateOnly date, TimeSlot timeSlot, Money cost)
    {
        Name = name;
        Date = date;
        TimeSlot = timeSlot;
        Cost = cost;
    }

    internal bool ConflictsWith(DateOnly date, TimeSlot timeSlot) =>
        Date == date && TimeSlot.OverlapsWith(timeSlot);

    internal static string NormalizeName(string name)
    {
        var trimmedName = name?.Trim();

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
