namespace MediaCompany.Domain.Accounting;

public enum BudgetScopeKind
{
    Department = 1,
    Channel = 2,
}

/// <summary>
/// A budget for one department or one channel over one period. Utilization is tracked at 50, 75,
/// 90 and 100 per cent and each crossing raises one alert (constraint C-018); the threshold set
/// is closed so that a crossing cannot be silently removed by configuration.
/// </summary>
public sealed record Budget
{
    /// <summary>The four utilization thresholds constraint C-018 fixes, in ascending order.</summary>
    public static readonly IReadOnlyList<int> Thresholds = [50, 75, 90, 100];

    public required BudgetId Id { get; init; }
    public required BudgetScopeKind ScopeKind { get; init; }

    /// <summary>The department or channel identifier this budget governs.</summary>
    public required Guid ScopeId { get; init; }

    /// <summary>The accounting period, as the first day of the month.</summary>
    public required DateOnly Period { get; init; }

    public required Money Amount { get; init; }
}

/// <summary>
/// One threshold crossing. Identity is the budget, the period and the threshold, so the alert is
/// idempotent per crossing and a re-evaluation cannot raise it twice.
/// </summary>
public sealed record BudgetAlert(
    BudgetId Budget,
    DateOnly Period,
    int Threshold,
    decimal UtilizationPercent,
    Money Utilized,
    Money BudgetAmount,
    DateTimeOffset RaisedAt);

/// <summary>
/// The approved monthly envelope, recorded at design fact F-009. Every figure presented against
/// it is an estimate over unit prices no first-hand verification has confirmed, and risk RK-002
/// requires the unit prices to be re-fetched immediately before any spend.
/// </summary>
public static class ApprovedEnvelope
{
    public static Money MonthlyTotal { get; } = new(77.41m);
    public static Money Metered { get; } = new(34.42m);
    public static Money Standing { get; } = new(42.99m);
}
