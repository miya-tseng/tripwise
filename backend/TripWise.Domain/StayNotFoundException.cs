namespace TripWise.Domain;

public class StayNotFoundException(Guid stayId)
    : DomainException($"Stay {stayId} was not found in this trip.");
