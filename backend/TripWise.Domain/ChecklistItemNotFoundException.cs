namespace TripWise.Domain;

public class ChecklistItemNotFoundException(Guid itemId)
    : DomainException($"Checklist item {itemId} was not found in this trip.");
