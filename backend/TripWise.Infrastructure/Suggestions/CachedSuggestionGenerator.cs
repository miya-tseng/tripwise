using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TripWise.Application.Suggestions;

namespace TripWise.Infrastructure.Suggestions;

public class CachedSuggestionGenerator(
    ISuggestionGenerator inner,
    IMemoryCache cache,
    IOptions<GeminiOptions> options) : ISuggestionGenerator
{
    public async Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(
        SuggestionContext context,
        CancellationToken cancellationToken = default)
    {
        var key = $"suggestions:{context.PlaceId}";

        if (cache.TryGetValue(key, out IReadOnlyList<SuggestionIdea>? cached) && cached is not null)
        {
            return cached;
        }

        var ideas = await inner.GenerateAsync(context, cancellationToken);
        cache.Set(key, ideas, options.Value.CacheDuration);
        return ideas;
    }
}
