using System.ComponentModel.DataAnnotations;
using TripWise.Api.Domain;

namespace TripWise.Api.Dtos;

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
}
