using System.Text.RegularExpressions;

namespace TripWise.Domain;

public sealed partial record Money
{
    public const int DecimalPlaces = 2;
    public const decimal MaxAmount = 9_999_999_999.99m;
    public const string HomeCurrency = "TWD";

    public static readonly Money Zero = new(0m);

    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = HomeCurrency)
    {
        if (amount < 0)
        {
            throw new DomainException("Amount cannot be negative.");
        }

        if (amount > MaxAmount)
        {
            throw new DomainException($"Amount cannot exceed {MaxAmount:N2}.");
        }

        if (decimal.Round(amount, DecimalPlaces) != amount)
        {
            throw new DomainException($"Amount cannot have more than {DecimalPlaces} decimal places.");
        }

        var normalizedCurrency = currency?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!CurrencyCodePattern().IsMatch(normalizedCurrency))
        {
            throw new DomainException("Currency must be a 3-letter ISO 4217 code.");
        }

        Amount = amount;
        Currency = normalizedCurrency;
    }

    public static Money ZeroIn(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new DomainException("Cannot add amounts in different currencies.");
        }

        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money Sum(IEnumerable<Money> amounts)
    {
        var list = amounts.ToList();

        return list.Count == 0
            ? Zero
            : list.Aggregate(ZeroIn(list[0].Currency), (total, amount) => total + amount);
    }

    public static IReadOnlyList<Money> SumByCurrency(IEnumerable<Money> amounts) =>
        amounts
            .GroupBy(amount => amount.Currency)
            .OrderBy(group => group.Key == HomeCurrency ? 0 : 1)
            .ThenBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => Sum(group))
            .ToList();

    [GeneratedRegex("^[A-Z]{3}$")]
    private static partial Regex CurrencyCodePattern();
}
