using TripWise.Application.Suggestions;

namespace TripWise.UnitTests.Suggestions;

public class SuggestionIdeaParserTests
{
    [Test]
    public void Parse_ReadsEverySuggestion()
    {
        var ideas = SuggestionIdeaParser.Parse("""
            {
              "suggestions": [
                { "name": "Tokyo Skytree", "area": "東京都墨田區", "reason": "步行可到的地標", "suggestedMinutes": 90 },
                { "name": "  Ueno Park ", "area": " 東京都台東區 ", "reason": " 散步 ", "suggestedMinutes": 60 }
              ]
            }
            """);

        Assert.That(ideas, Is.EqualTo(new[]
        {
            new SuggestionIdea("Tokyo Skytree", "東京都墨田區", "步行可到的地標", 90),
            new SuggestionIdea("Ueno Park", "東京都台東區", "散步", 60),
        }));
    }

    [Test]
    public void Parse_SkipsSuggestionsWithoutAName()
    {
        var ideas = SuggestionIdeaParser.Parse("""
            { "suggestions": [ { "name": "", "reason": "x" }, { "reason": "y" }, { "name": "Tokyo Tower" } ] }
            """);

        Assert.That(ideas.Select(idea => idea.Name), Is.EqualTo(new[] { "Tokyo Tower" }));
    }

    [Test]
    public void Parse_RemovesDuplicateNames()
    {
        var ideas = SuggestionIdeaParser.Parse("""
            { "suggestions": [ { "name": "Tokyo Tower" }, { "name": "tokyo tower" } ] }
            """);

        Assert.That(ideas, Has.Count.EqualTo(1));
    }

    [TestCase(5, 15)]
    [TestCase(15, 15)]
    [TestCase(90, 90)]
    [TestCase(600, 240)]
    public void Parse_KeepsMinutesWithinRange(int minutes, int expected)
    {
        var ideas = SuggestionIdeaParser.Parse($$"""
            { "suggestions": [ { "name": "Tokyo Tower", "suggestedMinutes": {{minutes}} } ] }
            """);

        Assert.That(ideas.Single().SuggestedMinutes, Is.EqualTo(expected));
    }

    [Test]
    public void Parse_UsesDefaultMinutesWhenMissing()
    {
        var ideas = SuggestionIdeaParser.Parse("""{ "suggestions": [ { "name": "Tokyo Tower" } ] }""");

        Assert.Multiple(() =>
        {
            Assert.That(ideas.Single().SuggestedMinutes, Is.EqualTo(SuggestionIdeaParser.DefaultMinutes));
            Assert.That(ideas.Single().Area, Is.Empty);
            Assert.That(ideas.Single().Reason, Is.Empty);
        });
    }

    [Test]
    public void Parse_TruncatesLongReasons()
    {
        var reason = new string('很', 200);

        var ideas = SuggestionIdeaParser.Parse($$"""{ "suggestions": [ { "name": "Tokyo Tower", "reason": "{{reason}}" } ] }""");

        Assert.That(ideas.Single().Reason, Has.Length.EqualTo(SuggestionIdeaParser.MaxReasonLength + 1));
        Assert.That(ideas.Single().Reason, Does.EndWith("…"));
    }

    [TestCase("{}")]
    [TestCase("""{ "suggestions": [] }""")]
    [TestCase("null")]
    public void Parse_WithNoSuggestions_ReturnsEmpty(string json)
    {
        Assert.That(SuggestionIdeaParser.Parse(json), Is.Empty);
    }

    [TestCase("not json")]
    [TestCase("""{ "suggestions": "oops" }""")]
    public void Parse_WithInvalidOutput_ThrowsUnavailable(string json)
    {
        Assert.Throws<SuggestionsUnavailableException>(() => SuggestionIdeaParser.Parse(json));
    }
}
