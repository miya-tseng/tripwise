using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripWise.Application.Suggestions;

public static class SuggestionIdeaParser
{
    public const int MinMinutes = 15;
    public const int MaxMinutes = 240;
    public const int DefaultMinutes = 60;
    public const int MaxReasonLength = 120;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<SuggestionIdea> Parse(string json)
    {
        SuggestionsPayload? payload;

        try
        {
            payload = JsonSerializer.Deserialize<SuggestionsPayload>(json, JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new SuggestionsUnavailableException(exception);
        }

        return (payload?.Suggestions ?? [])
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item => new SuggestionIdea(
                item.Name!.Trim(),
                item.Area?.Trim() ?? string.Empty,
                Truncate(item.Reason?.Trim() ?? string.Empty),
                NormalizeMinutes(item.SuggestedMinutes)))
            .DistinctBy(idea => idea.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static int NormalizeMinutes(int? minutes) =>
        minutes is { } value ? Math.Clamp(value, MinMinutes, MaxMinutes) : DefaultMinutes;

    private static string Truncate(string reason) =>
        reason.Length <= MaxReasonLength ? reason : reason[..MaxReasonLength].TrimEnd() + "…";

    private sealed class SuggestionsPayload
    {
        [JsonPropertyName("suggestions")]
        public List<SuggestionItem>? Suggestions { get; set; }
    }

    private sealed class SuggestionItem
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("area")]
        public string? Area { get; set; }

        [JsonPropertyName("reason")]
        public string? Reason { get; set; }

        [JsonPropertyName("suggestedMinutes")]
        public int? SuggestedMinutes { get; set; }
    }
}
