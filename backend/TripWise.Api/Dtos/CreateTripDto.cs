using System.ComponentModel.DataAnnotations;
using TripWise.Api.Domain;

namespace TripWise.Api.Dtos;

public class CreateTripDto
{
    [Required]
    [StringLength(Trip.MaxNameLength)]
    public string Name { get; set; } = string.Empty;

    [Required]
    public DateOnly? StartDate { get; set; }

    [Required]
    public DateOnly? EndDate { get; set; }
}
