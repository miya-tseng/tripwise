using System.ComponentModel.DataAnnotations;
using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class SaveStayDto
{
    [StringLength(Stay.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? CheckIn { get; set; }

    [Required]
    public DateOnly? CheckOut { get; set; }

    public decimal? Cost { get; set; }

    public string? Currency { get; set; }

    [StringLength(Place.MaxPlaceIdLength)]
    public string? PlaceId { get; set; }

    [StringLength(100)]
    public string? PlaceSessionToken { get; set; }

    public BookingStatus? BookingStatus { get; set; }

    [StringLength(ActivityDetails.MaxConfirmationCodeLength)]
    public string? ConfirmationCode { get; set; }
}
