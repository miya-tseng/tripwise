namespace TripWise.Infrastructure.Places;

public class GooglePlacesOptions
{
    public const string SectionName = "GooglePlaces";
    public const string GoogleProvider = "Google";
    public const string FakeProvider = "Fake";

    public string? Provider { get; set; }
    public string? ApiKey { get; set; }
    public Uri BaseUrl { get; set; } = new("https://places.googleapis.com/v1/");
    public string LanguageCode { get; set; } = "zh-TW";
    public double BiasRadiusMeters { get; set; } = 50_000;
    public int MaxSuggestions { get; set; } = 6;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);

    public bool UsesGoogle =>
        Provider != FakeProvider && !string.IsNullOrWhiteSpace(ApiKey);
}
