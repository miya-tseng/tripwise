using TripWise.Application.Suggestions;
using TripWise.Infrastructure.Places;

namespace TripWise.Infrastructure.Suggestions;

public class FakeSuggestionGenerator : ISuggestionGenerator
{
    public Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(
        SuggestionContext context,
        CancellationToken cancellationToken = default)
    {
        var city = CityOf(context.PlaceAddress);

        IReadOnlyList<SuggestionIdea> ideas = FakePlaceSearch.Places
            .Where(place => place.PlaceId != context.PlaceId && CityOf(place.Address) == city)
            .Select((place, index) => new SuggestionIdea(
                place.Name,
                city,
                $"和「{context.PlaceName}」同一區，很多旅客會排在同一天。",
                60 + index % 3 * 30))
            .ToList();

        return Task.FromResult(ideas);
    }

    private static string CityOf(string address)
    {
        var parts = address.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[^2] : address;
    }
}
