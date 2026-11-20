namespace TripWise.Api.Domain;

public class Trip
{
    public const int MaxNameLength = 100;
    public const int MaxDays = 30;

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateRange Period { get; private set; } = null!;

    private Trip() { }

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
