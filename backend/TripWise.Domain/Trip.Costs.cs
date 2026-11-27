namespace TripWise.Domain;

public partial class Trip
{
    public IReadOnlyList<Money> GetDailyTotals(DateOnly date) => Money.SumByCurrency(GetDailyCosts(date));

    public IReadOnlyList<Money> GetTotals() => Money.SumByCurrency(GetAllCosts());

    public IEnumerable<Money> GetDailyCosts(DateOnly date) =>
        _activities
            .Where(activity => activity.Date == date)
            .Select(activity => activity.Cost)
            .Concat(PaidStays.Where(stay => stay.CoversNight(date)).Select(stay => stay.CostForNight(date)));

    public IEnumerable<Money> GetAllCosts() =>
        _activities.Select(activity => activity.Cost).Concat(PaidStays.Select(stay => stay.Cost));

    private IEnumerable<Stay> PaidStays => _stays.Where(stay => stay.Cost.Amount > 0);
}
