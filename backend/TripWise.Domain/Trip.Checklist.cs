namespace TripWise.Domain;

public partial class Trip
{
    public ChecklistItem AddChecklistItem(string text, ChecklistSection section, DateOnly? dueDate = null)
    {
        var position = _checklist.Where(item => item.Section == section).Select(item => item.Position + 1).DefaultIfEmpty(0).Max();
        var item = ChecklistItem.Create(text, section, dueDate, position);
        _checklist.Add(item);
        return item;
    }

    public ChecklistItem UpdateChecklistItem(Guid itemId, string text, ChecklistSection section, DateOnly? dueDate)
    {
        var item = FindChecklistItem(itemId);
        var previousSection = item.Section;
        item.Update(text, section, dueDate);

        if (previousSection != section)
        {
            item.MoveTo(section, ItemsIn(section).Where(other => other.Id != itemId).Select(other => other.Position + 1).DefaultIfEmpty(0).Max());
            Renumber(previousSection);
        }

        return item;
    }

    public ChecklistItem SetChecklistItemDone(Guid itemId, bool done)
    {
        var item = FindChecklistItem(itemId);
        item.MarkDone(done);
        return item;
    }

    public void RemoveChecklistItem(Guid itemId)
    {
        var item = FindChecklistItem(itemId);
        _checklist.Remove(item);
        Renumber(item.Section);
    }

    public void ReorderChecklist(ChecklistSection section, IReadOnlyList<Guid> itemIds)
    {
        ArgumentNullException.ThrowIfNull(itemIds);
        var items = ItemsIn(section);

        if (itemIds.Count != items.Count || itemIds.Distinct().Count() != items.Count || items.Any(item => !itemIds.Contains(item.Id)))
        {
            throw new DomainException("The new order must list every item of the section once.");
        }

        for (var index = 0; index < itemIds.Count; index++)
        {
            items.First(item => item.Id == itemIds[index]).MoveTo(section, index);
        }
    }

    private List<ChecklistItem> ItemsIn(ChecklistSection section) =>
        _checklist.Where(item => item.Section == section).OrderBy(item => item.Position).ToList();

    private void Renumber(ChecklistSection section)
    {
        var items = ItemsIn(section);

        for (var index = 0; index < items.Count; index++)
        {
            items[index].MoveTo(section, index);
        }
    }

    private ChecklistItem FindChecklistItem(Guid itemId) =>
        _checklist.FirstOrDefault(item => item.Id == itemId)
        ?? throw new ChecklistItemNotFoundException(itemId);
}
