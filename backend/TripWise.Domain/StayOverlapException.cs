namespace TripWise.Domain;

public class StayOverlapException(Stay conflictingStay)
    : DomainException(
        $"These nights overlap with \"{conflictingStay.Name}\" " +
        $"({conflictingStay.CheckIn:yyyy-MM-dd} to {conflictingStay.CheckOut:yyyy-MM-dd}).");
