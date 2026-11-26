using TripWise.Application.Places;
using TripWise.Domain;

namespace TripWise.Infrastructure.Places;

public class FakePlaceSearch : IPlaceSearch
{
    public static readonly IReadOnlyList<Place> Places =
    [
        Create("fake-senso-ji", "Senso-ji", "2 Chome-3-1 Asakusa, Taito City, Tokyo, Japan", 35.714765, 139.796655),
        Create("fake-tokyo-skytree", "Tokyo Skytree", "1 Chome-1-2 Oshiage, Sumida City, Tokyo, Japan", 35.710063, 139.810700),
        Create("fake-shibuya-sky", "Shibuya Sky", "2 Chome-24-12 Shibuya, Shibuya City, Tokyo, Japan", 35.658450, 139.702240),
        Create("fake-tsukiji-outer-market", "Tsukiji Outer Market", "4 Chome-16-2 Tsukiji, Chuo City, Tokyo, Japan", 35.665498, 139.770677),
        Create("fake-ueno-park", "Ueno Park", "Uenokoen, Taito City, Tokyo, Japan", 35.714765, 139.773987),
        Create("fake-teamlab-planets", "teamLab Planets TOKYO", "6 Chome-1-16 Toyosu, Koto City, Tokyo, Japan", 35.649222, 139.789792),
        Create("fake-meiji-jingu", "Meiji Jingu", "1-1 Yoyogikamizonocho, Shibuya City, Tokyo, Japan", 35.676398, 139.699326),
        Create("fake-tokyo-tower", "Tokyo Tower", "4 Chome-2-8 Shibakoen, Minato City, Tokyo, Japan", 35.658581, 139.745433),
        Create("fake-fushimi-inari", "Fushimi Inari Taisha", "68 Fukakusa Yabunouchicho, Fushimi Ward, Kyoto, Japan", 34.967140, 135.772672),
        Create("fake-kiyomizu-dera", "Kiyomizu-dera", "1 Chome-294 Kiyomizu, Higashiyama Ward, Kyoto, Japan", 34.994856, 135.785046),
        Create("fake-taipei-101", "Taipei 101", "No. 7, Section 5, Xinyi Road, Xinyi District, Taipei City, Taiwan", 25.033964, 121.564468),
        Create("fake-jiufen", "Jiufen Old Street", "Jishan Street, Ruifang District, New Taipei City, Taiwan", 25.109650, 121.845116),
    ];

    public Task<IReadOnlyList<PlaceSuggestion>> AutocompleteAsync(
        string query,
        string? sessionToken,
        GeoPoint? near,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PlaceSuggestion> suggestions = Places
            .Where(place =>
                place.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || place.Address.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Select(place => new PlaceSuggestion(place.PlaceId, place.Name, place.Address))
            .Take(6)
            .ToList();

        return Task.FromResult(suggestions);
    }

    public Task<Place?> GetDetailsAsync(
        string placeId,
        string? sessionToken,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Places.FirstOrDefault(place => place.PlaceId == placeId));

    private static Place Create(string placeId, string name, string address, double latitude, double longitude) =>
        new(
            placeId,
            name,
            address,
            latitude,
            longitude,
            $"https://www.google.com/maps/search/?api=1&query={latitude},{longitude}");
}
