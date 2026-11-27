namespace TripWise.Domain;

public partial class Trip
{
    public Activity SetActivityBooked(Guid activityId, bool booked)
    {
        var activity = FindActivity(activityId);
        EnsureNeedsBooking(activity.Details.BookingStatus);
        activity.ChangeBookingStatus(NextBookingStatus(activity.Details.BookingStatus, booked));
        return activity;
    }

    public Stay SetStayBooked(Guid stayId, bool booked)
    {
        var stay = FindStay(stayId);
        EnsureNeedsBooking(stay.BookingStatus);
        stay.ChangeBookingStatus(NextBookingStatus(stay.BookingStatus, booked));
        return stay;
    }

    private static void EnsureNeedsBooking(BookingStatus status)
    {
        if (status == BookingStatus.NotNeeded)
        {
            throw new DomainException("This item does not need booking.");
        }
    }

    private static BookingStatus NextBookingStatus(BookingStatus current, bool booked) => booked
        ? current == BookingStatus.ToBook ? BookingStatus.Booked : current
        : BookingStatus.ToBook;
}
