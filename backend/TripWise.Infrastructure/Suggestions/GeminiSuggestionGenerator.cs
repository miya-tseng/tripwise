using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripWise.Application.Suggestions;

namespace TripWise.Infrastructure.Suggestions;

public class GeminiSuggestionGenerator(
    HttpClient httpClient,
    IOptions<GeminiOptions> options,
    ILogger<GeminiSuggestionGenerator> logger) : ISuggestionGenerator
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly object ResponseSchema = new
    {
        type = "OBJECT",
        properties = new
        {
            suggestions = new
            {
                type = "ARRAY",
                items = new
                {
                    type = "OBJECT",
                    properties = new
                    {
                        name = new { type = "STRING" },
                        area = new { type = "STRING" },
                        reason = new { type = "STRING" },
                        suggestedMinutes = new { type = "INTEGER" },
                    },
                    required = new[] { "name", "area", "reason", "suggestedMinutes" },
                },
            },
        },
        required = new[] { "suggestions" },
    };

    public async Task<IReadOnlyList<SuggestionIdea>> GenerateAsync(
        SuggestionContext context,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        var body = BuildRequest(SuggestionPrompt.Build(context), settings);

        foreach (var model in settings.FallbackModels.Prepend(settings.Model).Distinct())
        {
            var (json, canTryNextModel) = await TryModelAsync(model, body, settings, cancellationToken);

            if (json is not null)
            {
                return SuggestionIdeaParser.Parse(json);
            }

            if (!canTryNextModel)
            {
                break;
            }
        }

        throw new SuggestionsUnavailableException();
    }

    private async Task<(string? Json, bool CanTryNextModel)> TryModelAsync(
        string model,
        GenerateContentRequest body,
        GeminiOptions settings,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await SendWithRetryAsync(model, body, settings, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Gemini {Model} returned {StatusCode}", model, (int)response.StatusCode);
                return (null, CanFallBackFrom(response.StatusCode));
            }

            var content = await response.Content.ReadFromJsonAsync<GenerateContentResponse>(JsonOptions, cancellationToken);
            var json = content?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(json))
            {
                logger.LogWarning("Gemini {Model} returned no content", model);
                return (null, true);
            }

            return (json, false);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            logger.LogWarning(exception, "Gemini {Model} could not be reached", model);
            return (null, true);
        }
    }

    private async Task<HttpResponseMessage> SendWithRetryAsync(
        string model,
        GenerateContentRequest body,
        GeminiOptions settings,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Post,
                $"models/{Uri.EscapeDataString(model)}:generateContent")
            {
                Content = JsonContent.Create(body, options: JsonOptions),
            };
            request.Headers.Add("x-goog-api-key", settings.ApiKey);

            var response = await httpClient.SendAsync(request, cancellationToken);

            if (!IsTransient(response.StatusCode) || attempt > settings.MaxRetries)
            {
                return response;
            }

            logger.LogWarning("Gemini {Model} returned {StatusCode}, retrying (attempt {Attempt})", model, (int)response.StatusCode, attempt);
            response.Dispose();
            await Task.Delay(settings.RetryDelay * attempt, cancellationToken);
        }
    }

    private static bool IsTransient(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;

    private static bool CanFallBackFrom(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.NotFound or HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests
        || (int)statusCode >= 500;

    private static GenerateContentRequest BuildRequest(string prompt, GeminiOptions settings)
    {
        var thinkingConfig = string.IsNullOrWhiteSpace(settings.ThinkingLevel)
            ? null
            : new ThinkingConfig(settings.ThinkingLevel);

        return new GenerateContentRequest(
            [new Content("user", [new Part(prompt)])],
            new GenerationConfig("application/json", ResponseSchema, thinkingConfig));
    }

    private sealed record GenerateContentRequest(
        [property: JsonPropertyName("contents")] IReadOnlyList<Content> Contents,
        [property: JsonPropertyName("generationConfig")] GenerationConfig GenerationConfig);

    private sealed record GenerationConfig(
        [property: JsonPropertyName("responseMimeType")] string ResponseMimeType,
        [property: JsonPropertyName("responseSchema")] object ResponseSchema,
        [property: JsonPropertyName("thinkingConfig")] ThinkingConfig? ThinkingConfig);

    private sealed record ThinkingConfig(
        [property: JsonPropertyName("thinkingLevel")] string ThinkingLevel);

    private sealed record Content(
        [property: JsonPropertyName("role")] string? Role,
        [property: JsonPropertyName("parts")] IReadOnlyList<Part>? Parts);

    private sealed record Part(
        [property: JsonPropertyName("text")] string? Text);

    private sealed record GenerateContentResponse(
        [property: JsonPropertyName("candidates")] IReadOnlyList<Candidate>? Candidates);

    private sealed record Candidate(
        [property: JsonPropertyName("content")] Content? Content);
}
