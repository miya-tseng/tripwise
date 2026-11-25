namespace TripWise.Api.Domain;

public sealed record Money
{
    public const int DecimalPlaces = 2;
    public const decimal MaxAmount = 9_999_999_999.99m;

    public static readonly Money Zero = new(0m);

    public decimal Amount { get; }

    public Money(decimal amount)
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

        Amount = amount;
    }

    public static Money operator +(Money left, Money right) => new(left.Amount + right.Amount);

    public static Money Sum(IEnumerable<Money> amounts) =>
        amounts.Aggregate(Zero, (total, amount) => total + amount);
}
