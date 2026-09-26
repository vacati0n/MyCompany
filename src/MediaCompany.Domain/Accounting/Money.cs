namespace MediaCompany.Domain.Accounting;

/// <summary>
/// A currency amount. The authoritative arithmetic for recorded cost and for every rollup is
/// the datastore's exact decimal type, per decision D-006 and constraint C-005; this type
/// carries amounts across the application boundary and supports the pre-flight cost-ceiling
/// comparison of resolution step 4, which is an estimate and is labelled one wherever reported.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public const string DefaultCurrency = "USD";

    public Money(decimal amount, string currency = DefaultCurrency)
    {
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Currency is required.", nameof(currency));
        }

        Amount = amount;
        Currency = currency;
    }

    public decimal Amount { get; }
    public string Currency { get; }

    public static Money Zero(string currency = DefaultCurrency) => new(0m, currency);

    public int CompareTo(Money other)
    {
        RequireSameCurrency(other);
        return Amount.CompareTo(other.Amount);
    }

    public static bool operator <(Money left, Money right) => left.CompareTo(right) < 0;
    public static bool operator >(Money left, Money right) => left.CompareTo(right) > 0;
    public static bool operator <=(Money left, Money right) => left.CompareTo(right) <= 0;
    public static bool operator >=(Money left, Money right) => left.CompareTo(right) >= 0;

    public static Money operator +(Money left, Money right)
    {
        left.RequireSameCurrency(right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    private void RequireSameCurrency(Money other)
    {
        if (!string.Equals(Currency, other.Currency, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"Cannot compare {Currency} with {other.Currency}.");
        }
    }

    public override string ToString() => $"{Amount} {Currency}";
}

/// <summary>
/// Whether a recorded figure was measured from a provider's returned usage counts or estimated.
/// Assumption A-002 may prove false, and risk R-009 requires the report to state which.
/// </summary>
public enum CostBasis
{
    Measurement = 1,
    Estimate = 2,
}
