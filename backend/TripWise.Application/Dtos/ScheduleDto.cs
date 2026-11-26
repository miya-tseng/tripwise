namespace TripWise.Application.Dtos;

public class ScheduleDto
{
    public Guid TripId { get; set; }
    public bool RatesAvailable { get; set; }
    public DateTimeOffset? RatesUpdatedAt { get; set; }
    public decimal? TotalInTwd { get; set; }
    public IReadOnlyList<MoneyDto> Totals { get; set; } = [];
    public IReadOnlyList<ScheduleDayDto> Days { get; set; } = [];
}

public class ScheduleDayDto
{
    public DateOnly Date { get; set; }
    public decimal? TotalInTwd { get; set; }
    public IReadOnlyList<MoneyDto> Totals { get; set; } = [];
    public IReadOnlyList<ActivityDto> Activities { get; set; } = [];
}
