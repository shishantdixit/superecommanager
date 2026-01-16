namespace SuperEcomManager.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary values with currency.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    public static Money Zero => new(0, "INR");
    public static Money ZeroUSD => new(0, "USD");

    // Required for EF Core
    private Money()
    {
        Currency = "INR";
    }

    public Money(decimal amount, string currency = "INR")
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency cannot be empty", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Subtract(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount - other.Amount, Currency);
    }

    public Money Multiply(decimal factor)
    {
        return new Money(Amount * factor, Currency);
    }

    public static Money operator +(Money a, Money b) => a.Add(b);
    public static Money operator -(Money a, Money b) => a.Subtract(b);
    public static Money operator *(Money a, decimal factor) => a.Multiply(factor);

    public static bool operator >(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount >= b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        a.EnsureSameCurrency(b);
        return a.Amount <= b.Amount;
    }

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot operate on different currencies: {Currency} and {other.Currency}");
    }

    public bool Equals(Money? other)
    {
        if (other is null) return false;
        return Amount == other.Amount && Currency == other.Currency;
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() => $"{Currency} {Amount:N2}";

    public static bool operator ==(Money? a, Money? b)
    {
        if (a is null && b is null) return true;
        if (a is null || b is null) return false;
        return a.Equals(b);
    }

    public static bool operator !=(Money? a, Money? b) => !(a == b);
}
