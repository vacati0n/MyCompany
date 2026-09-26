using MediaCompany.Domain.Accounting;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// The four quantities the item's ledger states (criterion A-015). Each is metered in its own
/// unit kind, which is why they are named rather than summed into one number.
/// </summary>
public enum MeteredQuantityKind
{
    Tokens = 1,
    Characters = 2,
    Images = 3,
    GeneratedSeconds = 4,
}

/// <summary>
/// One priced line of the item's ledger.
///
/// <see cref="PriceRefetchedOn"/> is required and nullable at once, deliberately. A price lacking
/// a re-fetch date resolves as ABSENT rather than as current (decision D-010, criterion A-017), so
/// the absence must be representable in order to be refused; a type that made the date mandatory
/// at construction would hide the very condition the criterion measures.
/// </summary>
public sealed record LedgerLine
{
    public required MeteredQuantityKind Kind { get; init; }
    public required long Quantity { get; init; }
    public required decimal UnitPrice { get; init; }

    /// <summary>The unit the price is quoted per — 1,000 characters, one image, one second.</summary>
    public required long PricePerUnits { get; init; }

    public required CostBasis Basis { get; init; }

    /// <summary>Null means the price carries no re-fetch date and is therefore absent, not current.</summary>
    public DateOnly? PriceRefetchedOn { get; init; }

    /// <summary>The date of the operation this price was applied to, for the ordering comparison.</summary>
    public DateOnly? OperationOn { get; init; }

    /// <summary>
    /// A price is applicable only when it carries a re-fetch date no earlier than the operation it
    /// priced. Both halves matter: a missing date and a stale date are the same failure.
    /// </summary>
    public bool PriceIsApplicable =>
        PriceRefetchedOn is not null &&
        (OperationOn is null || PriceRefetchedOn.Value >= OperationOn.Value);

    /// <summary>
    /// The line's cost. Null when the price is not applicable — an inapplicable price produces no
    /// figure rather than a figure nobody can defend.
    /// </summary>
    public decimal? Cost =>
        PriceIsApplicable && PricePerUnits > 0
            ? decimal.Round(Quantity / (decimal)PricePerUnits * UnitPrice, 6, MidpointRounding.ToEven)
            : null;
}

/// <summary>Whether a reported figure was measured or estimated. Every figure in the comparison carries one.</summary>
public sealed record LabelledFigure(decimal Amount, CostBasis Basis, string Label);

/// <summary>
/// The item's ledger and its comparison against the approved per-item figure and the monthly
/// envelope (criteria A-015, A-016, A-017), a member of the rule-determined set under
/// <see cref="DeterministicTaskRegistry.ItemLedgerArithmetic"/>.
///
/// The comparison basis is the APPROVED per-item ledger, not the superseded narrower figure. The
/// narrower figure is carried alongside, labelled superseded and with its scope named, because
/// comparing against it would report an overrun that exists only because the wrong target was
/// quoted.
/// </summary>
public static class ItemLedger
{
    /// <summary>
    /// Sums the applicable lines. Lines whose price is not applicable are excluded from the total
    /// and returned separately, so an absent price shows up as a named gap rather than as a
    /// silently smaller number.
    /// </summary>
    public static LedgerTotal Total(IReadOnlyList<LedgerLine> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var applicable = lines.Where(l => l.PriceIsApplicable).ToArray();
        var inapplicable = lines.Where(l => !l.PriceIsApplicable).ToArray();

        var sum = applicable.Sum(l => l.Cost ?? 0m);

        return new LedgerTotal
        {
            Lines = lines,
            LinesWithAbsentPrice = inapplicable,
            Cost = decimal.Round(sum, 6, MidpointRounding.ToEven),
            Complete = inapplicable.Length == 0,
        };
    }

    /// <summary>
    /// Builds the comparison. Every figure carries its label, and the variance is stated against
    /// the approved figure rather than the superseded one.
    /// </summary>
    public static LedgerComparison Compare(
        LedgerTotal measured,
        decimal approvedPerItem,
        decimal monthlyEnvelope,
        decimal supersededPerItem,
        string supersededScope)
    {
        ArgumentNullException.ThrowIfNull(measured);

        return new LedgerComparison
        {
            Measured = new LabelledFigure(measured.Cost, CostBasis.Measurement, "measured, this item"),
            ApprovedPerItem = new LabelledFigure(approvedPerItem, CostBasis.Estimate, "approved per-item ledger"),
            MonthlyEnvelope = new LabelledFigure(monthlyEnvelope, CostBasis.Estimate, "monthly envelope"),
            Superseded = new LabelledFigure(supersededPerItem, CostBasis.Estimate, "superseded: " + supersededScope),
            VarianceAgainstApproved = decimal.Round(measured.Cost - approvedPerItem, 6, MidpointRounding.ToEven),
            Complete = measured.Complete,
        };
    }
}

/// <summary>The summed ledger, with the lines whose price was absent named rather than dropped.</summary>
public sealed record LedgerTotal
{
    public required IReadOnlyList<LedgerLine> Lines { get; init; }
    public required IReadOnlyList<LedgerLine> LinesWithAbsentPrice { get; init; }
    public required decimal Cost { get; init; }

    /// <summary>False when any line's price was absent, so the total is a floor rather than a total.</summary>
    public required bool Complete { get; init; }
}

/// <summary>The comparison, with every figure labelled a measurement or an estimate.</summary>
public sealed record LedgerComparison
{
    public required LabelledFigure Measured { get; init; }
    public required LabelledFigure ApprovedPerItem { get; init; }
    public required LabelledFigure MonthlyEnvelope { get; init; }
    public required LabelledFigure Superseded { get; init; }
    public required decimal VarianceAgainstApproved { get; init; }
    public required bool Complete { get; init; }
}
