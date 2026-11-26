using TripWise.Application.Currencies;
using TripWise.Infrastructure.Currencies;

namespace TripWise.IntegrationTests.Support;

public class TestExchangeRateProvider : IExchangeRateProvider
{
    public bool Available { get; set; } = true;

    public Task<ExchangeRates?> GetLatestAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult(Available
            ? new ExchangeRates(FakeExchangeRateProvider.UpdatedAt, FakeExchangeRateProvider.UnitsPerTwd)
            : null);
}
