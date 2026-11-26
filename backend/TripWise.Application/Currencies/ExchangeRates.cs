using TripWise.Domain;

namespace TripWise.Application.Currencies;

public sealed class ExchangeRates
{
    public ExchangeRates(DateTimeOffset updatedAt, IReadOnlyDictionary<string, decimal> unitsPerHomeCurrency)
    {
        UpdatedAt = updatedAt;
        UnitsPerHomeCurrency = unitsPerHomeCurrency
            .Where(rate => rate.Value > 0)
            .ToDictionary(rate => rate.Key.ToUpperInvariant(), rate => rate.Value);
        UnitsPerHomeCurrency.TryAdd(Money.HomeCurrency, 1m);
    }

    public DateTimeOffset UpdatedAt { get; }

    public Dictionary<string, decimal> UnitsPerHomeCurrency { get; }

    public IReadOnlyList<string> Currencies =>
        UnitsPerHomeCurrency.Keys.Order(StringComparer.Ordinal).ToList();

    public bool Supports(string currency) => UnitsPerHomeCurrency.ContainsKey(currency.ToUpperInvariant());

    public decimal? ToHomeCurrency(Money money)
    {
        if (!UnitsPerHomeCurrency.TryGetValue(money.Currency, out var unitsPerHomeCurrency))
        {
            return null;
        }

        return money.Currency == Money.HomeCurrency
            ? money.Amount
            : decimal.Round(money.Amount / unitsPerHomeCurrency, Money.DecimalPlaces, MidpointRounding.AwayFromZero);
    }
}
