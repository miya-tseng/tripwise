namespace TripWise.Domain;

public sealed record StayDetails
{
    public string Name { get; }
    public Place? Place { get; }
    public DateOnly CheckIn { get; }
    public DateOnly CheckOut { get; }
    public Money Cost { get; }
    public BookingStatus BookingStatus { get; }
    public string ConfirmationCode { get; }

    public StayDetails(
        string name,
        DateOnly checkIn,
        DateOnly checkOut,
        Place? place = null,
        Money? cost = null,
        BookingStatus bookingStatus = BookingStatus.NotNeeded,
        string? confirmationCode = null)
    {
        var trimmedName = name?.Trim();

        if (string.IsNullOrEmpty(trimmedName))
        {
            trimmedName = place?.Name;
        }

        if (string.IsNullOrEmpty(trimmedName))
        {
            throw new DomainException("Stay name is required.");
        }

        if (trimmedName.Length > Stay.MaxNameLength)
        {
            trimmedName = trimmedName[..Stay.MaxNameLength].TrimEnd();
        }

        if (checkOut <= checkIn)
        {
            throw new DomainException("Check-out must be after check-in.");
        }

        if (!Enum.IsDefined(bookingStatus))
        {
            throw new DomainException("Unknown booking status.");
        }

        var trimmedCode = confirmationCode?.Trim() ?? string.Empty;

        if (trimmedCode.Length > ActivityDetails.MaxConfirmationCodeLength)
        {
            throw new DomainException($"Confirmation code cannot exceed {ActivityDetails.MaxConfirmationCodeLength} characters.");
        }

        Name = trimmedName;
        Place = place;
        CheckIn = checkIn;
        CheckOut = checkOut;
        Cost = cost ?? Money.Zero;
        BookingStatus = bookingStatus;
        ConfirmationCode = trimmedCode;
    }
}
