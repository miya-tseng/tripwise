using TripWise.Application.Currencies;

namespace TripWise.Infrastructure.Currencies;

public class FakeExchangeRateProvider : IExchangeRateProvider
{
    public static readonly DateTimeOffset UpdatedAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static readonly IReadOnlyDictionary<string, decimal> UnitsPerTwd = new Dictionary<string, decimal>
    {
        ["TWD"] = 1m,
        ["JPY"] = 4.8m,
        ["USD"] = 0.032m,
        ["EUR"] = 0.029m,
        ["KRW"] = 42m,
        ["THB"] = 1.1m,
        ["HKD"] = 0.25m,
        ["CNY"] = 0.23m,
        ["GBP"] = 0.025m,
        ["SGD"] = 0.043m,
    };

    public Task<ExchangeRates?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<ExchangeRates?>(new ExchangeRates(UpdatedAt, UnitsPerTwd));
}
