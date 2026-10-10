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


    // -----------------------------------------------------------------------
    // The measurable-now report (correction CR-001): the same rule as the weekly reports and the dashboard
    // -----------------------------------------------------------------------

    /// <summary>
    /// The report command's figures in a DEMONSTRATION store: the monthly cost total, the variance against the envelope,
    /// the cost by capability and every channel line render as demonstration, none as observed or observed zero, and an
    /// unmeasured figure stays unmeasured.
    /// </summary>
    [Fact]
    public async Task TheReportRendersADemonstrationStoresFiguresAsDemonstration()
    {
        var lines = await new MediaCompany.Deterministic.Services.ReportingService(
            Partitions(), new NoCompanyRecords(), new FixedDesignation(StoreDesignation.Demonstration))
            .MeasurableNowAsync(new DateOnly(2026, 10, 1), CancellationToken.None);

        var valued = lines.Where(l => l.Quantity is MeasurementQuantity.ObservedValue or MeasurementQuantity.ObservedZero).ToArray();
        Assert.Contains(valued, l => l.Name == "monthly-cost-total");
        Assert.Contains(valued, l => l.Name == "cost-variance-against-envelope");
        Assert.All(valued, l =>
        {
            Assert.True(l.Demonstration);
            Assert.Equal(FigureCase.Demonstration, l.Case());
            Assert.Contains("DEMONSTRATION", l.Describe(), StringComparison.Ordinal);
        });
        Assert.DoesNotContain(lines, l => l.Case() is FigureCase.Observed or FigureCase.ObservedZero);
        Assert.All(lines.Where(l => l.Quantity is MeasurementQuantity.Unmeasured), l => Assert.Equal(FigureCase.Unmeasured, l.Case()));
    }

    /// <summary>A store not designated demonstration, and a report built without the designation reader, render as delivered.</summary>
    [Theory]
    [InlineData(null)]
    [InlineData(StoreDesignation.Company)]
    public async Task TheReportOfAStoreNotDesignatedDemonstrationRendersAsDelivered(StoreDesignation? designation)
    {
        var period = new DateOnly(2026, 10, 1);
        var delivered = await new MediaCompany.Deterministic.Services.ReportingService(Partitions(), new NoCompanyRecords())
            .MeasurableNowAsync(period, CancellationToken.None);
        var read = await new MediaCompany.Deterministic.Services.ReportingService(Partitions(), new NoCompanyRecords(), new FixedDesignation(designation))
            .MeasurableNowAsync(period, CancellationToken.None);

        Assert.Equal(delivered.Select(l => l.Quantity.Describe()), read.Select(l => l.Describe()));
        Assert.DoesNotContain(read, l => l.Demonstration);
        Assert.Equal(FigureCase.ObservedZero, read.First(l => l.Name == "monthly-cost-total").Case());
    }

    private static StubPartitions Partitions() => new()
    {
        Horizon = DateTimeOffset.UnixEpoch,
        EarliestEntry = DateTimeOffset.UnixEpoch,
        CompanyOperations = StubPartitions.Empty with { Operations = 14, Cost = MediaCompany.Domain.Accounting.Money.Zero() },
    };

    private sealed class FixedDesignation(StoreDesignation? designation) : MediaCompany.Application.Ports.IStoreDesignationReader
    {
        public Task<MediaCompany.Application.Ports.StoreIdentity> ReadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new MediaCompany.Application.Ports.StoreIdentity(designation, "mediacompany_demo", null));
    }
}
