namespace TripWise.Api.Dtos;

public class ScheduleDto
{
    public Guid TripId { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<ScheduleDayDto> Days { get; set; } = [];
}

public class ScheduleDayDto
{
    public DateOnly Date { get; set; }
    public decimal Total { get; set; }
    public IReadOnlyList<ActivityDto> Activities { get; set; } = [];
}
