using System.ComponentModel.DataAnnotations;
using TripWise.Domain;

namespace TripWise.Application.Dtos;

public class ChecklistDto
{
    public Guid TripId { get; set; }
    public int Done { get; set; }
    public int Total { get; set; }
    public IReadOnlyList<ChecklistItemDto> Items { get; set; } = [];
    public IReadOnlyList<BookingItemDto> Bookings { get; set; } = [];
}

public class ChecklistItemDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;

    public ChecklistSection Section { get; set; }

    public bool Done { get; set; }
    public DateOnly? DueDate { get; set; }
    public int Position { get; set; }
}

public class BookingItemDto
{
    public ExpenseKind Kind { get; set; }

    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly Date { get; set; }

    public BookingStatus BookingStatus { get; set; }

    public bool Done { get; set; }
}

public class SaveChecklistItemDto
{
    [Required]
    [StringLength(ChecklistItem.MaxTextLength)]
    public string Text { get; set; } = string.Empty;

    [Required]
    public ChecklistSection? Section { get; set; }

    public DateOnly? DueDate { get; set; }
}

public class SetDoneDto
{
    [Required]
    public bool? Done { get; set; }
}

public class ReorderChecklistDto
{
    [Required]
    public ChecklistSection? Section { get; set; }

    [Required]
    public IReadOnlyList<Guid> ItemIds { get; set; } = [];
}
