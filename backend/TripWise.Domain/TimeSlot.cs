namespace TripWise.Domain;

public sealed record TimeSlot
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeSlot(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
        {
            throw new DomainException("End time must be later than start time.");
        }

        Start = start;
        End = end;
    }

    public bool OverlapsWith(TimeSlot other) => Start < other.End && other.Start < End;
}
