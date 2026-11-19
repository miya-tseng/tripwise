namespace TripWise.Api.Domain;

public sealed record DateRange
{
    public DateOnly Start { get; }
    public DateOnly End { get; }

    public DateRange(DateOnly start, DateOnly end)
    {
        if (start > end)
        {
            throw new DomainException("End date must be on or after start date.");
        }

        Start = start;
        End = end;
    }

    public int Days => End.DayNumber - Start.DayNumber + 1;

    public bool Contains(DateOnly date) => Start <= date && End >= date;
}
