namespace TripWise.Application.Suggestions;

public sealed record SuggestionContext(
    string PlaceId,
    string PlaceName,
    string PlaceAddress,
    TimeOnly EndsAt,
    IReadOnlyList<string> PlannedThatDay);
