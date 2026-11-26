using TripWise.Application.Currencies;
using TripWise.Application.Dtos;
using TripWise.Domain;

namespace TripWise.Application.Services;

internal static class CostMapping
{
    public static Money? ToCost(decimal? amount, string? currency, ExchangeRates? rates)
    {
        if (amount is not { } value)
        {
            return null;
        }

        var cost = new Money(value, currency ?? Money.HomeCurrency);

        if (rates is not null && !rates.Supports(cost.Currency))
        {
            throw new DomainException($"Currency {cost.Currency} is not supported.");
        }

        return cost;
    }

    public static MoneyDto ToMoneyDto(Money money) => new()
    {
        Amount = money.Amount,
        Currency = money.Currency,
    };

    public static decimal? TotalInHomeCurrency(IEnumerable<Money> costs, ExchangeRates? rates)
    {
        if (rates is null)
        {
            return null;
        }

        var converted = costs.Select(rates.ToHomeCurrency).ToList();

        return converted.Any(amount => amount is null) ? null : converted.Sum(amount => amount!.Value);
    }
}
