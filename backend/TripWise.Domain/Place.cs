namespace TripWise.Domain;

public sealed record Place
{
    public const int MaxPlaceIdLength = 300;
    public const int MaxNameLength = 300;
    public const int MaxAddressLength = 500;
    public const int MaxUriLength = 500;

    public string PlaceId { get; }
    public string Name { get; }
    public string Address { get; }
    public double Latitude { get; }
    public double Longitude { get; }
    public string GoogleMapsUri { get; }

    public Place(string placeId, string name, string address, double latitude, double longitude, string googleMapsUri)
    {
        PlaceId = Required(placeId, "Place id", MaxPlaceIdLength);
        Name = Required(name, "Place name", MaxNameLength);
        Address = (address ?? string.Empty).Trim();

        if (Address.Length > MaxAddressLength)
        {
            throw new DomainException($"Place address cannot exceed {MaxAddressLength} characters.");
        }

        if (latitude is < -90 or > 90 || double.IsNaN(latitude))
        {
            throw new DomainException("Latitude must be between -90 and 90.");
        }

        if (longitude is < -180 or > 180 || double.IsNaN(longitude))
        {
            throw new DomainException("Longitude must be between -180 and 180.");
        }

        if (!Uri.TryCreate(googleMapsUri, UriKind.Absolute, out var uri)
            || uri.Scheme != Uri.UriSchemeHttps
            || googleMapsUri.Length > MaxUriLength)
        {
            throw new DomainException("Google Maps link must be an https URL.");
        }

        Latitude = latitude;
        Longitude = longitude;
        GoogleMapsUri = googleMapsUri;
    }

    private static string Required(string value, string field, int maxLength)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
        {
            throw new DomainException($"{field} is required.");
        }

        if (trimmed.Length > maxLength)
        {
            throw new DomainException($"{field} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }
}
