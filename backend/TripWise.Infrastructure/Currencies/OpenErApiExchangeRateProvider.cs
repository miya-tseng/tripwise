using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripWise.Application.Currencies;
using TripWise.Domain;

namespace TripWise.Infrastructure.Currencies;

public class OpenErApiExchangeRateProvider(
    HttpClient httpClient,
    IMemoryCache cache,
    IOptions<ExchangeRateOptions> options,
    ILogger<OpenErApiExchangeRateProvider> logger) : IExchangeRateProvider
{
    private const string CacheKey = "exchange-rates:open-er-api";

    public async Task<ExchangeRates?> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out ExchangeRates? cached))
        {
            return cached;
        }

        var rates = await FetchAsync(cancellationToken);
        var duration = rates is null ? options.Value.FailureCacheDuration : options.Value.CacheDuration;
        cache.Set(CacheKey, rates, duration);

        return rates;
    }

    private async Task<ExchangeRates?> FetchAsync(CancellationToken cancellationToken)
    {
        try
        {
            var response = await httpClient.GetFromJsonAsync<OpenErApiResponse>(
                $"latest/{Money.HomeCurrency}",
                cancellationToken);

            if (response is not { Result: "success", Rates.Count: > 0 })
            {
                logger.LogWarning("Exchange rate API returned {Result}", response?.Result ?? "an empty response");
                return null;
            }

            return new ExchangeRates(
                DateTimeOffset.FromUnixTimeSeconds(response.TimeLastUpdateUnix),
                response.Rates);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                throw;
            }

            logger.LogWarning(exception, "Exchange rates are unavailable");
            return null;
        }
    }

    private sealed class OpenErApiResponse
    {
        [JsonPropertyName("result")]
        public string? Result { get; set; }

        [JsonPropertyName("time_last_update_unix")]
        public long TimeLastUpdateUnix { get; set; }

        [JsonPropertyName("rates")]
        public Dictionary<string, decimal> Rates { get; set; } = [];
    }
}
