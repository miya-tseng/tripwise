namespace TripWise.Domain;

public partial class Trip
{
    public Stay AddStay(StayDetails details)
    {
        EnsureStayFits(details, excludedStayId: null);

        var stay = Stay.Create(details);
        _stays.Add(stay);
        return stay;
    }

    public Stay UpdateStay(Guid stayId, StayDetails details)
    {
        var stay = FindStay(stayId);
        EnsureStayFits(details, excludedStayId: stayId);

        stay.Update(details);
        return stay;
    }

    public void RemoveStay(Guid stayId)
    {
        _stays.Remove(FindStay(stayId));
    }

    public Stay? StayForNight(DateOnly night) => _stays.FirstOrDefault(stay => stay.CoversNight(night));

    private Stay FindStay(Guid stayId) =>
        _stays.FirstOrDefault(stay => stay.Id == stayId)
        ?? throw new StayNotFoundException(stayId);

    private void EnsureStayFits(StayDetails details, Guid? excludedStayId)
    {
        if (details.CheckIn < Period.Start || details.CheckOut > LastCheckOut)
        {
            throw new DomainException("Stay dates must be within the trip dates.");
        }

        var conflictingStay = _stays.FirstOrDefault(stay =>
            stay.Id != excludedStayId && stay.OverlapsWith(details.CheckIn, details.CheckOut));

        if (conflictingStay is not null)
        {
            throw new StayOverlapException(conflictingStay);
        }
    }
}
