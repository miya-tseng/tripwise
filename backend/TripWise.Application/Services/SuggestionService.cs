using TripWise.Application.Dtos;
using TripWise.Application.Places;
using TripWise.Application.Suggestions;
using TripWise.Domain;
using TripWise.Application.Mappings;

namespace TripWise.Application.Services;

public class SuggestionService(
    ITripRepository repository,
    ISuggestionGenerator generator,
    IPlaceSearch placeSearch)
{
    public const int MaxSuggestions = 5;

    public async Task<IReadOnlyList<SuggestionDto>> GetSuggestionsAsync(
        Guid tripId,
        Guid activityId,
        CancellationToken cancellationToken = default)
    {
        var trip = await repository.GetRequiredAsync(tripId, cancellationToken);

        var activity = trip.Activities.FirstOrDefault(item => item.Id == activityId)
            ?? throw new ActivityNotFoundException(activityId);
        var anchor = activity.Place
            ?? throw new DomainException("Suggestions need an activity with a place.");

        var plannedThatDay = trip.Activities
            .Where(item => item.Date == activity.Date && item.Id != activity.Id)
            .OrderBy(item => item.TimeSlot.Start)
            .Select(item => item.Name)
            .ToList();
        var context = new SuggestionContext(
            anchor.PlaceId,
            anchor.Name,
            anchor.Address,
            activity.TimeSlot.End,
            plannedThatDay);

        var ideas = await generator.GenerateAsync(context, cancellationToken);
        var near = new GeoPoint(anchor.Latitude, anchor.Longitude);
        var excludedPlaceIds = trip.Activities
            .Select(item => item.Place?.PlaceId)
            .OfType<string>()
            .ToHashSet(StringComparer.Ordinal);
        var excludedNames = trip.Activities
            .SelectMany(item => new[] { item.Name, item.Place?.Name })
            .OfType<string>()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var suggestions = new List<SuggestionDto>();

        foreach (var idea in ideas.Where(idea => !excludedNames.Contains(idea.Name)))
        {
            if (suggestions.Count == MaxSuggestions)
            {
                break;
            }

            var query = string.IsNullOrWhiteSpace(idea.Area) ? idea.Name : $"{idea.Name} {idea.Area}";
            var place = await placeSearch.FindAsync(query, near, cancellationToken);

            if (place is null || !excludedPlaceIds.Add(place.PlaceId))
            {
                continue;
            }

            suggestions.Add(new SuggestionDto
            {
                Place = place.ToDto(),
                Reason = idea.Reason,
                SuggestedMinutes = idea.SuggestedMinutes,
            });
        }

        return suggestions;
    }
}
