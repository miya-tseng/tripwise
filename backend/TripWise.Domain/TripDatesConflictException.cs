namespace TripWise.Domain;

public class TripDatesConflictException(int activitiesOutside)
    : DomainException(
        activitiesOutside == 1
            ? "1 activity falls outside the new dates. Move or delete it first."
            : $"{activitiesOutside} activities fall outside the new dates. Move or delete them first.")
{
    public int ActivitiesOutside { get; } = activitiesOutside;
}
