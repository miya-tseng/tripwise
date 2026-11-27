using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class StayDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Nights { get; set; }
    public decimal Cost { get; set; }
    public string Currency { get; set; } = string.Empty;
    public decimal? CostInTwd { get; set; }
    public PlaceDto? Place { get; set; }

    public BookingStatus BookingStatus { get; set; }

    public string ConfirmationCode { get; set; } = string.Empty;
}
