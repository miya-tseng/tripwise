namespace TripWise.Infrastructure.Suggestions;

public class GeminiOptions
{
    public const string SectionName = "Gemini";
    public const string FakeProvider = "Fake";

    public string? Provider { get; set; }
    public string? ApiKey { get; set; }
    public Uri BaseUrl { get; set; } = new("https://generativelanguage.googleapis.com/v1beta/");
    public string Model { get; set; } = "gemini-3.5-flash-lite";
    public string[] FallbackModels { get; set; } = ["gemini-3.1-flash-lite"];
    public string? ThinkingLevel { get; set; } = "minimal";
    public int MaxRetries { get; set; } = 2;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(20);
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(24);

    public bool UsesGemini => Provider != FakeProvider && !string.IsNullOrWhiteSpace(ApiKey);
}
