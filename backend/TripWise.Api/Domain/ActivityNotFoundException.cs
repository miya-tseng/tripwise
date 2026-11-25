namespace TripWise.Api.Domain;

public class ActivityNotFoundException(Guid activityId)
    : DomainException($"Activity {activityId} was not found in this trip.");
