using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using TripWise.Application.Suggestions;
using TripWise.Infrastructure.Suggestions;

namespace TripWise.IntegrationTests.Suggestions;

public class CachedSuggestionGeneratorTests
{
    private sealed class CountingGenerator : ISuggestionGenerator
    {
        public int Calls { get; private set; }

        public Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(SuggestionContext context, CancellationToken cancellationToken = default)
        {
            Calls++;
            IReadOnlyList<SuggestionIdea> ideas = [new SuggestionIdea($"{context.PlaceName} idea {Calls}", "", "", 60)];
            return Task.FromResult(ideas);
        }
    }

    private static SuggestionContext Context(string placeId) =>
        new(placeId, placeId, "", new TimeOnly(10, 0), []);

    [Test]
    public async Task GenerateAsync_CachesIdeasPerPlace()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var inner = new CountingGenerator();
        var generator = new CachedSuggestionGenerator(inner, cache, Options.Create(new GeminiOptions()));

        var first = await generator.GenerateAsync(Context("senso-ji"));
        var second = await generator.GenerateAsync(Context("senso-ji"));
        await generator.GenerateAsync(Context("skytree"));

        Assert.Multiple(() =>
        {
            Assert.That(second, Is.SameAs(first));
            Assert.That(inner.Calls, Is.EqualTo(2));
        });
    }

    [Test]
    public void GenerateAsync_DoesNotCacheFailures()
    {
        using var cache = new MemoryCache(new MemoryCacheOptions());
        var calls = 0;
        var failing = new DelegatingGenerator(() =>
        {
            calls++;
            throw new SuggestionsUnavailableException();
        });
        var generator = new CachedSuggestionGenerator(failing, cache, Options.Create(new GeminiOptions()));

        Assert.ThrowsAsync<SuggestionsUnavailableException>(() => generator.GenerateAsync(Context("senso-ji")));
        Assert.ThrowsAsync<SuggestionsUnavailableException>(() => generator.GenerateAsync(Context("senso-ji")));

        Assert.That(calls, Is.EqualTo(2));
    }

    private sealed class DelegatingGenerator(Func<IReadOnlyList<SuggestionIdea>> generate) : ISuggestionGenerator
    {
        public Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(SuggestionContext context, CancellationToken cancellationToken = default) =>
            Task.FromResult(generate());
    }
}
