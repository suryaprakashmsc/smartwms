namespace Inventory.Domain.ValueObjects;

/// <summary>
/// Represents a monetary amount with currency.
/// Immutable value object — equality is based on value, not reference.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; } = string.Empty;

    private Money() { } // EF Core

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    // -- Factory methods ----------------------------------------------------

    public static Money Of(decimal amount, string currency = "INR")
    {
        if (amount < 0)
            throw new ArgumentException("Money amount cannot be negative.", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency code is required.", nameof(currency));

        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code (e.g. INR, USD).", nameof(currency));

        return new Money(Math.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "INR") => Of(0, currency);

    // -- Arithmetic ---------------------------------------------------------

    public Money Add(Money other)
    {
        GuardSameCurrency(other);
        return Of(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        GuardSameCurrency(other);
        return Of(Amount - other.Amount, Currency);
    }

    public Money Multiply(int quantity)
    {
        if (quantity < 0)
            throw new ArgumentException("Quantity multiplier cannot be negative.", nameof(quantity));
        return Of(Amount * quantity, Currency);
    }

    public Money Multiply(decimal factor)
    {
        if (factor < 0)
            throw new ArgumentException("Factor cannot be negative.", nameof(factor));
        return Of(Amount * factor, Currency);
    }

    // -- Comparison ---------------------------------------------------------

    public bool IsGreaterThan(Money other)
    {
        GuardSameCurrency(other);
        return Amount > other.Amount;
    }

    public bool IsZero() => Amount == 0;

    // -- Equality -----------------------------------------------------------

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right) =>
        left?.Equals(right) ?? right is null;

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    // -- Display ------------------------------------------------------------

    public override string ToString() => $"{Currency} {Amount:N2}";

    // -- Guards -------------------------------------------------------------

    private void GuardSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {Currency} vs {other.Currency}");
    }
}


