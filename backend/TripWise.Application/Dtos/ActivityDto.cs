using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class ActivityDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? CostInTwd { get; set; }
    public PlaceDto? Place { get; set; }

    public ActivityCategory Category { get; set; }

    public BookingStatus BookingStatus { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
    public string? Link { get; set; }
}
