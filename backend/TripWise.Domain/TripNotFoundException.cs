namespace TripWise.Domain;

public class TripNotFoundException(Guid tripId)
    : DomainException($"Trip {tripId} was not found.");
