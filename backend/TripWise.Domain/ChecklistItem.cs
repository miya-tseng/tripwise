namespace TripWise.Domain;

public class ChecklistItem
{
    public const int MaxTextLength = 200;

    public Guid Id { get; private set; }
    public string Text { get; private set; } = null!;
    public ChecklistSection Section { get; private set; }
    public bool Done { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public int Position { get; private set; }

    private ChecklistItem() { }

    public bool IsOverdue(DateOnly today) => !Done && DueDate is { } due && due < today;

    internal static ChecklistItem Create(string text, ChecklistSection section, DateOnly? dueDate, int position) => new()
    {
        Id = Guid.CreateVersion7(),
        Text = NormalizeText(text),
        Section = ValidSection(section),
        DueDate = dueDate,
        Position = position,
    };

    internal void Update(string text, ChecklistSection section, DateOnly? dueDate)
    {
        Text = NormalizeText(text);
        Section = ValidSection(section);
        DueDate = dueDate;
    }

    internal void MarkDone(bool done)
    {
        Done = done;
    }

    internal void MoveTo(ChecklistSection section, int position)
    {
        Section = section;
        Position = position;
    }

    private static string NormalizeText(string text)
    {
        var trimmed = text?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException("Checklist item text is required.");
        }

        if (trimmed.Length > MaxTextLength)
        {
            throw new DomainException($"Checklist item text cannot exceed {MaxTextLength} characters.");
        }

        return trimmed;
    }

    private static ChecklistSection ValidSection(ChecklistSection section) =>
        Enum.IsDefined(section) ? section : throw new DomainException("Unknown checklist section.");
}
