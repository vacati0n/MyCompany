using MediaCompany.Deterministic.Production;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// Demonstrations and measurements for the item's metered ledger (criteria A-015 to A-017) and for
/// the two contract changes the ledger rests on: the required price re-fetch date and the
/// reasoning-tier attributes on the operation record.
/// </summary>
public sealed class ItemLedgerTests
{
    private static readonly DateOnly OperationDay = new(2026, 9, 26);

    // ------------------------------------------------------------------
    // A-017 — a price without a re-fetch date is absent, not current
    // ------------------------------------------------------------------

    [Fact]
    public void APriceWithNoRefetchDateIsAbsentRatherThanCurrent()
    {
        var line = Line(quantity: 10_000, refetchedOn: null);

        Assert.False(line.PriceIsApplicable);
        Assert.Null(line.Cost);
    }

    [Fact]
    public void APriceRefetchedBeforeTheOperationItPricedIsNotApplicable()
    {
        var line = Line(quantity: 10_000, refetchedOn: OperationDay.AddDays(-1));

        Assert.False(line.PriceIsApplicable);
        Assert.Null(line.Cost);
    }

    [Fact]
    public void APriceRefetchedOnTheDayOfTheOperationIsApplicable()
    {
        var line = Line(quantity: 10_000, refetchedOn: OperationDay);

        Assert.True(line.PriceIsApplicable);
        Assert.Equal(0.50m, line.Cost);
    }

    [Fact]
    public void AModelPriceCarriesItsRefetchDateAndRefusesAnEarlierOne()
    {
        var price = NewPrice(refetchedOn: OperationDay);

        Assert.True(price.IsApplicableTo(OperationDay));
        Assert.False(price.IsApplicableTo(OperationDay.AddDays(1)));
    }

    [Fact]
    public void AModelPriceWithNoRefetchDateIsApplicableToNothing()
    {
        var price = NewPrice(refetchedOn: null);

        Assert.False(price.IsApplicableTo(OperationDay));
        Assert.False(price.IsApplicableTo(OperationDay.AddDays(-30)));
    }

    // ------------------------------------------------------------------
    // A-015 — the four quantities, and lines with absent prices named rather than dropped
    // ------------------------------------------------------------------

    [Fact]
    public void TheLedgerNamesTheFourMeteredQuantities()
    {
        Assert.Equal(4, Enum.GetValues<MeteredQuantityKind>().Length);
    }

    [Fact]
    public void ALineWithAnAbsentPriceIsReportedRatherThanSilentlyExcluded()
    {
        var total = ItemLedger.Total(
        [
            Line(quantity: 10_000, refetchedOn: OperationDay),
            Line(quantity: 5_000, refetchedOn: null),
        ]);

        Assert.False(total.Complete);
        Assert.Single(total.LinesWithAbsentPrice);
        Assert.Equal(0.50m, total.Cost);
    }

    [Fact]
    public void ALedgerWhoseEveryPriceIsApplicableIsComplete()
    {
        var total = ItemLedger.Total(
        [
            Line(quantity: 10_000, refetchedOn: OperationDay),
            Line(quantity: 2_000, refetchedOn: OperationDay),
        ]);

        Assert.True(total.Complete);
        Assert.Empty(total.LinesWithAbsentPrice);
        Assert.Equal(0.60m, total.Cost);
    }

    [Fact]
    public void AZeroQuantityLinePricesToZeroRatherThanToAnAbsence()
    {
        var line = new LedgerLine
        {
            Kind = MeteredQuantityKind.GeneratedSeconds,
            Quantity = 0,
            UnitPrice = 0.05m,
            PricePerUnits = 1,
            Basis = CostBasis.Measurement,
            PriceRefetchedOn = OperationDay,
            OperationOn = OperationDay,
        };

        Assert.True(line.PriceIsApplicable);
        Assert.Equal(0m, line.Cost);
    }

    // ------------------------------------------------------------------
    // A-016 — the comparison, against the approved figure and not the superseded one
    // ------------------------------------------------------------------

    [Fact]
    public void TheComparisonIsStatedAgainstTheApprovedFigureWithTheSupersededOneLabelled()
    {
        var comparison = ItemLedger.Compare(
            ItemLedger.Total([Line(quantity: 10_000, refetchedOn: OperationDay)]),
            approvedPerItem: 2.647440m,
            monthlyEnvelope: 77.41m,
            supersededPerItem: 1.5786m,
            supersededScope: "research, scripting, fact-check, SEO and QC only");

        Assert.Equal(CostBasis.Measurement, comparison.Measured.Basis);
        Assert.Equal(CostBasis.Estimate, comparison.ApprovedPerItem.Basis);
        Assert.Equal(CostBasis.Estimate, comparison.Superseded.Basis);
        Assert.Contains("superseded", comparison.Superseded.Label, StringComparison.OrdinalIgnoreCase);

        // The variance is measured against the approved figure, not the superseded one. Comparing
        // against $1.58 would report an overrun that exists only because the wrong target was quoted.
        Assert.Equal(0.50m - 2.647440m, comparison.VarianceAgainstApproved);
    }

    [Fact]
    public void EveryFigureInTheComparisonCarriesItsLabel()
    {
        var comparison = ItemLedger.Compare(
            ItemLedger.Total([Line(quantity: 1_000, refetchedOn: OperationDay)]),
            2.647440m, 77.41m, 1.5786m, "narrower scope");

        foreach (var figure in new[]
                 {
                     comparison.Measured, comparison.ApprovedPerItem,
                     comparison.MonthlyEnvelope, comparison.Superseded,
                 })
        {
            Assert.False(string.IsNullOrWhiteSpace(figure.Label));
        }
    }

    [Fact]
    public void AnIncompleteLedgerCarriesItsIncompletenessIntoTheComparison()
    {
        var comparison = ItemLedger.Compare(
            ItemLedger.Total([Line(quantity: 1_000, refetchedOn: null)]),
            2.647440m, 77.41m, 1.5786m, "narrower scope");

        Assert.False(comparison.Complete);
    }

    // ------------------------------------------------------------------
    // D-006 — the reasoning tier, with an explicit absence marker
    // ------------------------------------------------------------------

    [Fact]
    public void AnOperationRecordCarriesTheTierRequestedAndTheTierServed()
    {
        var record = NewOperation() with
        {
            ReasoningTierRequested = ReasoningTier.Deep,
            ReasoningTierServed = ReasoningTier.Deep,
        };

        Assert.True(record.CarriesTierEvidence);
    }

    /// <summary>
    /// A route that cannot state a tier records an explicit absence. The consumer reads an absence
    /// rather than an inferred value, which is the difference between "untestable and we know it"
    /// and "untestable and we do not".
    /// </summary>
    [Fact]
    public void ARouteThatCannotStateATierRecordsAnExplicitAbsence()
    {
        var record = NewOperation() with
        {
            ReasoningTierRequested = ReasoningTier.Standard,
            ReasoningTierServed = null,
        };

        Assert.False(record.CarriesTierEvidence);
        Assert.Null(record.ReasoningTierServed);
    }

    [Fact]
    public void AnOperationRecordedWithNoTierAtAllCarriesNoTierEvidence()
    {
        Assert.False(NewOperation().CarriesTierEvidence);
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private static LedgerLine Line(long quantity, DateOnly? refetchedOn) => new()
    {
        Kind = MeteredQuantityKind.Characters,
        Quantity = quantity,
        UnitPrice = 0.05m,
        PricePerUnits = 1_000,
        Basis = CostBasis.Measurement,
        PriceRefetchedOn = refetchedOn,
        OperationOn = OperationDay,
    };

    private static ModelPrice NewPrice(DateOnly? refetchedOn) => new(
        ModelPriceId.New(),
        new ModelId("narration"),
        PriceUnitKind.OutputUnit,
        0.05m,
        "USD",
        "first-party pricing page",
        OperationDay,
        DateTimeOffset.UtcNow.AddDays(-1),
        null)
    {
        RefetchedOn = refetchedOn,
    };

    private static OperationRecord NewOperation() => new()
    {
        Id = OperationId.New(),
        Run = RunId.New(),
        Attribution = new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()),
        Capability = CapabilityClass.Narration,
        Units = UnitCounts.None,
        CostBasis = CostBasis.Measurement,
        Duration = TimeSpan.FromSeconds(1),
        Outcome = OperationOutcome.Succeeded,
        OccurredAt = DateTimeOffset.UtcNow,
        Attempt = 1,
    };
}
