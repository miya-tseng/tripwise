namespace TripWise.Application.Currencies;

public interface IExchangeRateProvider
{
    Task<ExchangeRates?> GetLatestAsync(CancellationToken cancellationToken = default);
}
