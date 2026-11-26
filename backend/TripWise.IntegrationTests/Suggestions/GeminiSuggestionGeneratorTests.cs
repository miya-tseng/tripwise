using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TripWise.Application.Suggestions;
using TripWise.Infrastructure.Suggestions;
using TripWise.IntegrationTests.Support;

namespace TripWise.IntegrationTests.Suggestions;

public class GeminiSuggestionGeneratorTests
{
    private static readonly SuggestionContext Context =
        new("senso-ji-id", "Senso-ji", "Asakusa, Tokyo", new TimeOnly(11, 30), ["Tsukiji"]);

    private static string SuccessBody(string json) => JsonSerializer.Serialize(new
    {
        candidates = new[] { new { content = new { role = "model", parts = new[] { new { text = json } } } } },
    });

    private const string IdeasJson = """
        { "suggestions": [ { "name": "Tokyo Skytree", "area": "東京都墨田區", "reason": "附近地標", "suggestedMinutes": 90 } ] }
        """;

    private StubHttpHandler _handler = null!;
    private GeminiSuggestionGenerator _generator = null!;

    [SetUp]
    public void SetUp()
    {
        _handler = new StubHttpHandler();
        var options = new GeminiOptions
        {
            ApiKey = "gemini-key",
            Model = "primary-model",
            FallbackModels = ["fallback-model"],
            RetryDelay = TimeSpan.Zero,
        };
        var httpClient = new HttpClient(_handler) { BaseAddress = options.BaseUrl };
        _generator = new GeminiSuggestionGenerator(httpClient, Options.Create(options), NullLogger<GeminiSuggestionGenerator>.Instance);
    }

    [TearDown]
    public void TearDown() => _handler.Dispose();

    [Test]
    public async Task GenerateAsync_PostsThePromptWithJsonSchemaToTheModel()
    {
        _handler.RespondAlways(HttpStatusCode.OK, SuccessBody(IdeasJson));

        await _generator.GenerateAsync(Context);

        var request = _handler.Requests.Single();
        var body = JsonDocument.Parse(request.Body!).RootElement;
        var config = body.GetProperty("generationConfig");
        Assert.Multiple(() =>
        {
            Assert.That(request.Uri, Is.EqualTo("https://generativelanguage.googleapis.com/v1beta/models/primary-model:generateContent"));
            Assert.That(request.Headers["x-goog-api-key"], Is.EqualTo("gemini-key"));
            Assert.That(body.GetProperty("contents")[0].GetProperty("parts")[0].GetProperty("text").GetString(), Is.EqualTo(SuggestionPrompt.Build(Context)));
            Assert.That(config.GetProperty("responseMimeType").GetString(), Is.EqualTo("application/json"));
            Assert.That(config.GetProperty("responseSchema").GetProperty("required")[0].GetString(), Is.EqualTo("suggestions"));
            Assert.That(config.GetProperty("thinkingConfig").GetProperty("thinkingLevel").GetString(), Is.EqualTo("minimal"));
        });
    }

    [Test]
    public async Task GenerateAsync_ParsesTheSuggestions()
    {
        _handler.RespondAlways(HttpStatusCode.OK, SuccessBody(IdeasJson));

        var ideas = await _generator.GenerateAsync(Context);

        Assert.That(ideas, Is.EqualTo(new[] { new SuggestionIdea("Tokyo Skytree", "東京都墨田區", "附近地標", 90) }));
    }

    [Test]
    public async Task GenerateAsync_RetriesTransientErrorsOnTheSameModel()
    {
        _handler.RespondOnce(HttpStatusCode.ServiceUnavailable, "{}");
        _handler.RespondOnce(HttpStatusCode.TooManyRequests, "{}");
        _handler.RespondAlways(HttpStatusCode.OK, SuccessBody(IdeasJson));

        var ideas = await _generator.GenerateAsync(Context);

        Assert.Multiple(() =>
        {
            Assert.That(ideas, Has.Count.EqualTo(1));
            Assert.That(_handler.Requests.Select(request => request.Uri), Is.All.Contains("primary-model"));
            Assert.That(_handler.Requests, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public async Task GenerateAsync_FallsBackToTheNextModelWhenThePrimaryKeepsFailing()
    {
        _handler.RespondOnce(HttpStatusCode.InternalServerError, "{}");
        _handler.RespondAlways(HttpStatusCode.OK, SuccessBody(IdeasJson));

        await _generator.GenerateAsync(Context);

        Assert.That(_handler.Requests.Select(request => request.Uri.Contains("fallback-model")), Is.EqualTo(new[] { false, true }));
    }

    [Test]
    public async Task GenerateAsync_FallsBackWhenTheModelReturnsNoContent()
    {
        _handler.RespondOnce(HttpStatusCode.OK, """{ "candidates": [] }""");
        _handler.RespondAlways(HttpStatusCode.OK, SuccessBody(IdeasJson));

        var ideas = await _generator.GenerateAsync(Context);

        Assert.That(ideas, Has.Count.EqualTo(1));
    }

    [Test]
    public void GenerateAsync_DoesNotFallBackOnAnInvalidKey()
    {
        _handler.RespondAlways(HttpStatusCode.Forbidden, "{}");

        Assert.ThrowsAsync<SuggestionsUnavailableException>(() => _generator.GenerateAsync(Context));
        Assert.That(_handler.Requests, Has.Count.EqualTo(1));
    }

    [Test]
    public void GenerateAsync_WhenEveryModelFails_ThrowsUnavailable()
    {
        _handler.RespondAlways(HttpStatusCode.InternalServerError, "{}");

        var ex = Assert.ThrowsAsync<SuggestionsUnavailableException>(() => _generator.GenerateAsync(Context));

        Assert.That(ex!.Message, Is.EqualTo("Suggestions are unavailable right now. Please try again later."));
    }

    [Test]
    public void GenerateAsync_WhenGeminiCannotBeReached_ThrowsUnavailable()
    {
        _handler.ThrowAlways(new HttpRequestException("connection refused"));

        Assert.ThrowsAsync<SuggestionsUnavailableException>(() => _generator.GenerateAsync(Context));
    }
}
