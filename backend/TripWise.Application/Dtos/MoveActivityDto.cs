using System.ComponentModel.DataAnnotations;

namespace TripWise.Application.Dtos;

public class MoveActivityDto
{
    [Required]
    public DateOnly? Date { get; set; }

    [Required]
    public TimeOnly? StartTime { get; set; }
}
