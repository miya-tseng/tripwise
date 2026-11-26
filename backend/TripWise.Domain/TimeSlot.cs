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

    public TimeSpan Duration => End - Start;

    public TimeSlot StartingAt(TimeOnly start)
    {
        if (start.ToTimeSpan() + Duration >= TimeSpan.FromDays(1))
        {
            throw new DomainException("The activity would end after midnight.");
        }

        return new TimeSlot(start, start.Add(Duration));
    }

    public bool OverlapsWith(TimeSlot other) => Start < other.End && other.Start < End;
}
