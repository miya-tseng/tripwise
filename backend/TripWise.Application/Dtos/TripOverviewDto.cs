using TripWise.Domain;

namespace TripWise.Application.Dtos;

public enum AttentionKind
{
    ActivityToBook,
    StayToBook,
    ChecklistOpen,
    NightWithoutStay,
    EmptyDay,
}

public class TripOverviewDto
{
    public Guid TripId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int Days { get; set; }
    public int Nights { get; set; }
    public int NightsWithStay { get; set; }
    public int ActivityCount { get; set; }
    public int PlaceCount { get; set; }
    public int ChecklistDone { get; set; }
    public int ChecklistTotal { get; set; }
    public bool RatesAvailable { get; set; }
    public DateTimeOffset? RatesUpdatedAt { get; set; }
    public decimal? TotalInTwd { get; set; }
    public decimal? ActivitiesInTwd { get; set; }
    public decimal? StaysInTwd { get; set; }
    public decimal? AveragePerDayInTwd { get; set; }
    public IReadOnlyList<CurrencySpendDto> SpendByCurrency { get; set; } = [];
    public IReadOnlyList<CategorySpendDto> SpendByCategory { get; set; } = [];
    public IReadOnlyList<DaySpendDto> SpendByDay { get; set; } = [];
    public BookingProgressDto Booking { get; set; } = new();
    public IReadOnlyList<ExpenseDto> TopExpenses { get; set; } = [];
    public IReadOnlyList<AttentionItemDto> Attention { get; set; } = [];
}

public class CurrencySpendDto
{
    public string Currency { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal? AmountInTwd { get; set; }
}

public class CategorySpendDto
{
    public ActivityCategory Category { get; set; }
    public int ActivityCount { get; set; }
    public decimal? AmountInTwd { get; set; }
}

public class DaySpendDto
{
    public DateOnly Date { get; set; }
    public int DayNumber { get; set; }
    public int ActivityCount { get; set; }
    public decimal? ActivitiesInTwd { get; set; }
    public decimal? StayInTwd { get; set; }
    public decimal? TotalInTwd { get; set; }
}

public class BookingProgressDto
{
    public int ToBook { get; set; }
    public int Booked { get; set; }
    public int Paid { get; set; }
}

public class ExpenseDto
{
    public ExpenseKind Kind { get; set; }
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public MoneyDto Cost { get; set; } = new();
    public decimal? CostInTwd { get; set; }
}

public class AttentionItemDto
{
    public AttentionKind Kind { get; set; }
    public DateOnly Date { get; set; }
    public Guid? ItemId { get; set; }
    public string? Name { get; set; }
}
