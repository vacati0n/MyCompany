using MediaCompany.Domain.Accounting;

namespace MediaCompany.Deterministic.Accounting;

/// <summary>
/// Threshold crossing detection (module M-006, plan task T-026), under the name
/// <see cref="DeterministicTaskRegistry.QuotaAndSpendAccounting"/>.
///
/// The utilization percentage itself is computed by the datastore from the recorded operations,
/// because constraint C-005 puts the money arithmetic in the exact decimal type rather than in
/// application code. What this type decides is which of the four fixed thresholds a move from one
/// utilization to another crossed — an ordering question over integers, not an arithmetic one
/// over money.
/// </summary>
public static class BudgetThresholds
{
    /// <summary>
    /// The thresholds crossed by moving from <paramref name="previousPercent"/> to
    /// <paramref name="currentPercent"/>, in ascending order. A threshold already crossed is not
    /// crossed again, which is what makes the alert idempotent per budget, period and threshold.
    /// </summary>
    public static IReadOnlyList<int> Crossed(decimal previousPercent, decimal currentPercent)
    {
        if (currentPercent <= previousPercent)
        {
            return [];
        }

        return Budget.Thresholds
            .Where(t => previousPercent < t && currentPercent >= t)
            .ToArray();
    }

    /// <summary>The highest threshold reached at a utilization, or null below the lowest.</summary>
    public static int? HighestReached(decimal percent)
    {
        int? highest = null;
        foreach (var threshold in Budget.Thresholds)
        {
            if (percent >= threshold)
            {
                highest = threshold;
            }
        }

        return highest;
    }
}
