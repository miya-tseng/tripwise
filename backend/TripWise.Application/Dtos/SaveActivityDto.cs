using System.ComponentModel.DataAnnotations;
using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class SaveActivityDto
{
    [StringLength(Activity.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? Date { get; set; }

    [Required]
    public TimeOnly? StartTime { get; set; }

    [Required]
    public TimeOnly? EndTime { get; set; }

    public decimal? Cost { get; set; }

    public string? Currency { get; set; }

    [StringLength(Place.MaxPlaceIdLength)]
    public string? PlaceId { get; set; }

    [StringLength(100)]
    public string? PlaceSessionToken { get; set; }
}
