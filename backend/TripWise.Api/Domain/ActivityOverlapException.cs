namespace TripWise.Api.Domain;

public class ActivityOverlapException(Activity conflictingActivity)
    : DomainException(
        $"The activity overlaps with \"{conflictingActivity.Name}\" " +
        $"({conflictingActivity.TimeSlot.Start:HH\\:mm}-{conflictingActivity.TimeSlot.End:HH\\:mm}) " +
        $"on {conflictingActivity.Date:yyyy-MM-dd}.");
