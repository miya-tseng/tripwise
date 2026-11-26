using System.ComponentModel.DataAnnotations;
using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class SaveActivityDto
{
    [Required]
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
}
