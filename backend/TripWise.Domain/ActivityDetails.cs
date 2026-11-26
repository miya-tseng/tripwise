namespace TripWise.Domain;

public sealed record ActivityDetails
{
    public const int MaxNotesLength = 2000;
    public const int MaxConfirmationCodeLength = 100;
    public const int MaxLinkLength = 500;

    public static readonly ActivityDetails Default = new();

    public ActivityCategory Category { get; }
    public BookingStatus BookingStatus { get; }
    public string Notes { get; }
    public string ConfirmationCode { get; }
    public string? Link { get; }

    public ActivityDetails(
        ActivityCategory category = ActivityCategory.Other,
        BookingStatus bookingStatus = BookingStatus.NotNeeded,
        string? notes = null,
        string? confirmationCode = null,
        string? link = null)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException("Unknown activity category.");
        }

        if (!Enum.IsDefined(bookingStatus))
        {
            throw new DomainException("Unknown booking status.");
        }

        Category = category;
        BookingStatus = bookingStatus;
        Notes = Limited(notes, MaxNotesLength, "Notes");
        ConfirmationCode = Limited(confirmationCode, MaxConfirmationCodeLength, "Confirmation code");
        Link = NormalizeLink(link);
    }

    public bool NeedsBooking => BookingStatus == BookingStatus.ToBook;

    private static string Limited(string? value, int maxLength, string field)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    private static string? NormalizeLink(string? link)
    {
        var trimmed = link?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > MaxLinkLength
            || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new DomainException("Link must be an https URL.");
        }

        return trimmed;
    }
}
