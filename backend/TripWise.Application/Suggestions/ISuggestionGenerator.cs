namespace TripWise.Application.Suggestions;

public interface ISuggestionGenerator
{
    Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(SuggestionContext context, CancellationToken cancellationToken = default);
}
