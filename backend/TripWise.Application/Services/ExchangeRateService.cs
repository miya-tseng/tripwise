using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Services;

public class ExchangeRateService(IExchangeRateProvider exchangeRateProvider)
{
    public async Task<ExchangeRatesDto> GetLatestAsync(CancellationToken cancellationToken = default)
    {
        var rates = await exchangeRateProvider.GetLatestAsync(cancellationToken);

        return new ExchangeRatesDto
        {
            BaseCurrency = Money.HomeCurrency,
            Available = rates is not null,
            UpdatedAt = rates?.UpdatedAt,
            Currencies = rates?.Currencies ?? [Money.HomeCurrency],
            UnitsPerBaseCurrency = rates?.UnitsPerHomeCurrency
                ?? new Dictionary<string, decimal> { [Money.HomeCurrency] = 1m },
        };
    }
}
