using TripWise.Application.Suggestions;

namespace TripWise.UnitTests.Suggestions;

public class SuggestionPromptTests
{
    private static SuggestionContext Context(params string[] planned) =>
        new("senso-ji-id", "Senso-ji", "2 Chome-3-1 Asakusa, Taito City, Tokyo", new TimeOnly(11, 30), planned);

    [Test]
    public void Build_DescribesThePlaceAndWhenItEnds()
    {
        var prompt = SuggestionPrompt.Build(Context());

        Assert.Multiple(() =>
        {
            Assert.That(prompt, Does.Contain("「Senso-ji」"));
            Assert.That(prompt, Does.Contain("2 Chome-3-1 Asakusa, Taito City, Tokyo"));
            Assert.That(prompt, Does.Contain("11:30"));
        });
    }

    [Test]
    public void Build_AsksForTheConfiguredNumberOfRealPlaces()
    {
        var prompt = SuggestionPrompt.Build(Context());

        Assert.Multiple(() =>
        {
            Assert.That(prompt, Does.Contain($"請列出 {SuggestionPrompt.IdeaCount} 個"));
            Assert.That(prompt, Does.Contain("不要編造"));
            Assert.That(prompt, Does.Contain("繁體中文"));
        });
    }

    [Test]
    public void Build_ListsWhatIsAlreadyPlannedThatDay()
    {
        var prompt = SuggestionPrompt.Build(Context("Tsukiji", "Ueno Park"));

        Assert.That(prompt, Does.Contain("當天已經安排：Tsukiji、Ueno Park。"));
    }

    [Test]
    public void Build_WithNothingElsePlanned_OmitsThePlannedLine()
    {
        Assert.That(SuggestionPrompt.Build(Context()), Does.Not.Contain("當天已經安排："));
    }
}
