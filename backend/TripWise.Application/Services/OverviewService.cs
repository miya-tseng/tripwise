using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;
using TripWise.Application.Mappings;

namespace TripWise.Application.Services;

public class OverviewService(ITripRepository repository, IExchangeRateProvider exchangeRateProvider)
{
    public const int TopExpenseCount = 5;

    public async Task<TripOverviewDto> GetAsync(Guid tripId, CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);
        var overview = TripOverview.For(trip);
        var activityCosts = trip.Activities.Select(activity => activity.Cost).Where(IsPaid).ToList();
        var stayCosts = trip.Stays.Select(stay => stay.Cost).Where(IsPaid).ToList();
        var total = Total(activityCosts.Concat(stayCosts), rates);

        return new TripOverviewDto
        {
            TripId = trip.Id,
            Name = trip.Name,
            StartDate = trip.Period.Start,
            EndDate = trip.Period.End,
            Days = overview.Days,
            Nights = overview.Nights,
            NightsWithStay = overview.NightsWithStay,
            ActivityCount = overview.ActivityCount,
            PlaceCount = overview.PlaceCount,
            ChecklistDone = trip.Checklist.Count(item => item.Done),
            ChecklistTotal = trip.Checklist.Count,
            RatesAvailable = rates is not null,
            RatesUpdatedAt = rates?.UpdatedAt,
            TotalInTwd = total,
            ActivitiesInTwd = Total(activityCosts, rates),
            StaysInTwd = Total(stayCosts, rates),
            AveragePerDayInTwd = total is { } sum
                ? decimal.Round(sum / overview.Days, Money.DecimalPlaces, MidpointRounding.AwayFromZero)
                : null,
            SpendByCurrency = trip.GetTotals()
                .Where(IsPaid)
                .Select(money => new CurrencySpendDto
                {
                    Currency = money.Currency,
                    Amount = money.Amount,
                    AmountInTwd = rates?.ToHomeCurrency(money),
                })
                .OrderByDescending(spend => spend.AmountInTwd ?? 0)
                .ToList(),
            SpendByCategory = overview.SpendByCategory
                .Select(spend => new CategorySpendDto
                {
                    Category = spend.Category,
                    ActivityCount = spend.ActivityCount,
                    AmountInTwd = Total(spend.Costs, rates),
                })
                .OrderByDescending(spend => spend.AmountInTwd ?? 0)
                .ThenBy(spend => spend.Category)
                .ToList(),
            SpendByDay = overview.SpendByDay.Select(day => ToDto(day, rates)).ToList(),
            Booking = new BookingProgressDto
            {
                ToBook = overview.Booking.ToBook,
                Booked = overview.Booking.Booked,
                Paid = overview.Booking.Paid,
            },
            TopExpenses = overview.Expenses
                .Select(expense => new ExpenseDto
                {
                    Kind = expense.Kind,
                    Id = expense.Id,
                    Name = expense.Name,
                    Date = expense.Date,
                    Cost = CostMapping.ToMoneyDto(expense.Cost),
                    CostInTwd = rates?.ToHomeCurrency(expense.Cost),
                })
                .OrderByDescending(expense => expense.CostInTwd.HasValue)
                .ThenByDescending(expense => expense.CostInTwd)
                .ThenBy(expense => expense.Date)
                .Take(TopExpenseCount)
                .ToList(),
            Attention = AttentionItems(trip, overview),
        };
    }

    private static bool IsPaid(Money money) => money.Amount > 0;

    private static decimal? Total(IEnumerable<Money> costs, ExchangeRates? rates) =>
        CostMapping.TotalInHomeCurrency(costs, rates);

    private static DaySpendDto ToDto(DaySpend day, ExchangeRates? rates)
    {
        var activities = Total(day.ActivityCosts, rates);
        var stay = rates is null ? null : day.StayCost is { } cost ? rates.ToHomeCurrency(cost) : 0m;

        return new DaySpendDto
        {
            Date = day.Date,
            DayNumber = day.DayNumber,
            ActivityCount = day.ActivityCount,
            ActivitiesInTwd = activities,
            StayInTwd = stay,
            TotalInTwd = activities + stay,
        };
    }

    private static List<AttentionItemDto> AttentionItems(Trip trip, TripOverview overview) =>
        overview.ActivitiesToBook
            .Select(activity => new AttentionItemDto
            {
                Kind = AttentionKind.ActivityToBook,
                Date = activity.Date,
                ItemId = activity.Id,
                Name = activity.Name,
            })
            .Concat(overview.StaysToBook.Select(stay => new AttentionItemDto
            {
                Kind = AttentionKind.StayToBook,
                Date = stay.CheckIn,
                ItemId = stay.Id,
                Name = stay.Name,
            }))
            .Concat(ChecklistAttention(trip))
            .Concat(overview.NightsWithoutStay.Select(date => new AttentionItemDto
            {
                Kind = AttentionKind.NightWithoutStay,
                Date = date,
            }))
            .Concat(overview.EmptyDays.Select(date => new AttentionItemDto
            {
                Kind = AttentionKind.EmptyDay,
                Date = date,
            }))
            .ToList();

    private static IEnumerable<AttentionItemDto> ChecklistAttention(Trip trip)
    {
        var open = trip.Checklist.Count(item => !item.Done);

        if (open == 0)
        {
            yield break;
        }

        yield return new AttentionItemDto
        {
            Kind = AttentionKind.ChecklistOpen,
            Date = trip.Period.Start,
            Name = open == 1 ? "1 checklist item left" : $"{open} checklist items left",
        };
    }
}
