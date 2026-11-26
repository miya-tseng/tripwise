using TripWise.Application.Suggestions;
using TripWise.Infrastructure.Suggestions;

namespace TripWise.IntegrationTests.Support;

public class TestSuggestionGenerator : ISuggestionGenerator
{
    private readonly FakeSuggestionGenerator _fake = new();

    public bool Available { get; set; } = true;
    public List<SuggestionContext> Requests { get; } = [];

    public void Reset()
    {
        Available = true;
        Requests.Clear();
    }

    public Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(SuggestionContext context, CancellationToken cancellationToken = default)
    {
        Requests.Add(context);

        if (!Available)
        {
            throw new SuggestionsUnavailableException();
        }

        return _fake.GenerateAsync(context, cancellationToken);
    }
}
