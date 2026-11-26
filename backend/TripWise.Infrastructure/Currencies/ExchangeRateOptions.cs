namespace TripWise.Infrastructure.Currencies;

public class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";
    public const string OpenErApiProvider = "OpenErApi";
    public const string FakeProvider = "Fake";

    public string Provider { get; set; } = OpenErApiProvider;
    public Uri BaseUrl { get; set; } = new("https://open.er-api.com/v6/");
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(6);
    public TimeSpan FailureCacheDuration { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
}
