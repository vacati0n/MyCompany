using MediaCompany.Deterministic.Analytics;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Production;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The demonstration rule over the delivered reports (the production change, decision D-005 of its design, as the
/// orchestrator ruled on the implementation phase's objection): in a store designated demonstration no figure of any
/// report, brief line or tile is observed or observed zero; the fake operations' spend is a DEMONSTRATION figure,
/// rendered apart from observed zero and from unmeasured, and every rule reading it abstains. A store not designated
/// demonstration renders exactly as delivered.
/// </summary>
public sealed class DemonstrationFigureTests
{
    [Fact]
    public void ADemonstrationStoresSpendIsNeverObservedInAnyReportBriefLineOrTile()
    {
        var snapshot = SnapshotFixture.Build(operations: 14, cost: 0m) with { Designation = StoreDesignation.Demonstration };

        var read = ManagementReportService.Compose(snapshot);

        Assert.DoesNotContain(read.Reports.Lines, l => l.Figure.Case is FigureCase.Observed or FigureCase.ObservedZero);
        var weekCost = read.Reports.Lines.First(l => l.Key == ReportLineKey.WeekCost);
        Assert.Equal(FigureCase.Demonstration, weekCost.Figure.Case);
        Assert.False(weekCost.Figure.CarriesAValue);
        Assert.Contains("DEMONSTRATION", weekCost.Figure.Describe(), StringComparison.Ordinal);
        Assert.DoesNotContain(read.Tiles, t => t.Case is FigureCase.Observed or FigureCase.ObservedZero);
        Assert.Contains(read.Tiles, t => t.Rendering.Contains("[demonstration]", StringComparison.Ordinal));
        Assert.DoesNotContain(read.Tiles, t => t.Rendering.Contains("[observed", StringComparison.Ordinal));
        Assert.All(read.Reports.Outcomes, o => Assert.IsType<RuleOutcome.Abstained>(o));
    }

    [Fact]
    public void ObservedZeroUnmeasuredAndDemonstrationRenderDistinguishably()
    {
        var words = Enum.GetValues<FigureCase>().Select(LineFigure.CaseWord).ToArray();
        Assert.Equal(words.Length, words.Distinct(StringComparer.Ordinal).Count());

        var zero = LineFigure.Of(MeasurementQuantity.Zero("USD"));
        var unmeasured = LineFigure.Of(MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "nothing booked"));
        var demonstration = LineFigure.Demonstrated(MeasurementQuantity.Zero("USD"));
        var described = new[] { zero, unmeasured, demonstration }.Select(f => $"{f.Describe()} [{LineFigure.CaseWord(f.Case)}]").ToArray();
        Assert.Equal(3, described.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal("demonstration", LineFigure.CaseWord(demonstration.Case));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(StoreDesignation.Company)]
    public void AStoreNotDesignatedDemonstrationRendersAsDelivered(StoreDesignation? designation)
    {
        var delivered = ManagementReportService.Compose(SnapshotFixture.Build(operations: 14, cost: 0m));
        var read = ManagementReportService.Compose(SnapshotFixture.Build(operations: 14, cost: 0m) with { Designation = designation });

        Assert.Equal(delivered.Tiles.Select(t => t.Rendering), read.Tiles.Select(t => t.Rendering));
        Assert.Equal(FigureCase.ObservedZero, read.Reports.Lines.First(l => l.Key == ReportLineKey.WeekCost).Figure.Case);
    }
}
