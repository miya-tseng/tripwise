using System.ComponentModel.DataAnnotations;
using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class UpdateTripDto
{
    [Required]
    [StringLength(Trip.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }

    [StringLength(Place.MaxPlaceIdLength)]
    public string? DestinationPlaceId { get; set; }

    public string? PlaceSessionToken { get; set; }
}
