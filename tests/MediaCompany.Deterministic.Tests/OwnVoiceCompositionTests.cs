using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The own-voice change's reporting items: the demonstration label wording of the controller decisions (decision D-014 of
/// its design) and the produced-item lines of an own-source production (decisions D-010 and D-012).
/// </summary>
public sealed class OwnVoiceCompositionTests
{
    private static RecordedControllerDecision Decision() => new()
    {
        Operation = OperationId.New(),
        Channel = SnapshotFixture.ChannelOne,
        DecidedAt = SnapshotFixture.Instant.AddHours(-1),
        BookingMonth = SnapshotFixture.October,
        Action = ControllerAction.Defer,
        Basis = SelectionBasis.Configured,
        BasisStatement = "fixture",
        TierStatement = "fixture",
        CompanyBasisStatement = "fixture",
        ReservationStatement = "fixture",
        ObservationsRankedOn = MeasurementQuantity.Count(0, "observations ranked on"),
        Readings =
        [
            new RecordedDecisionReading
            {
                Scope = GoverningScope.Channel,
                Amount = RecordedAmount.Of(new Money(1m), "the budget register"),
                BookedSpend = MeasurementQuantity.Observed(0.95m, "USD"),
                Utilisation = MeasurementQuantity.Observed(95m, "percent"),
                Threshold = BudgetThreshold.Ninety,
                Action = ControllerAction.Defer,
            },
        ],
        Candidates =
        [
            new RecordedDecisionCandidate
            {
                Route = RouteId.New(),
                ConfiguredRating = new QualityRating(3),
                ObservedQuality = MeasurementQuantity.Observed(4m, "rating"),
                ObservedCost = MeasurementQuantity.Observed(0.0123m, "USD"),
                Observations = MeasurementQuantity.Count(1, "observations"),
            },
        ],
    };

    /// <summary>
    /// IN A DEMONSTRATION STORE no controller label embeds an observed reading: each embedded quantity is described through the
    /// demonstration figure's rendering, and the candidate names "quality" and "cost"; the lines' own figures are tagged as
    /// delivered (demonstration). IN A COMPANY STORE every label reads as delivered.
    /// </summary>
    [Fact]
    public void ControllerLabelsReadAsDemonstrationInADemonstrationStoreAndAsDeliveredInACompanyStore()
    {
        var decision = Decision();
        var demonstration = ManagementComposers.Compose(SnapshotFixture.Build(decisions: [decision]) with { Designation = StoreDesignation.Demonstration })
            .Lines.Where(l => l.Key == ReportLineKey.ControllerReading).ToArray();
        var company = ManagementComposers.Compose(SnapshotFixture.Build(decisions: [decision]) with { Designation = StoreDesignation.Company })
            .Lines.Where(l => l.Key == ReportLineKey.ControllerReading).ToArray();

        Assert.Equal(2, demonstration.Length);
        Assert.All(demonstration, l => Assert.DoesNotContain(", observed quality ", l.Label, StringComparison.Ordinal));
        Assert.All(demonstration, l => Assert.DoesNotContain(", observed cost ", l.Label, StringComparison.Ordinal));
        Assert.Contains("booked spend a DEMONSTRATION figure of 0.95 USD", demonstration[0].Label, StringComparison.Ordinal);
        Assert.Contains("quality a DEMONSTRATION figure of 4 rating", demonstration[1].Label, StringComparison.Ordinal);
        Assert.Contains("cost a DEMONSTRATION figure of 0.0123 USD", demonstration[1].Label, StringComparison.Ordinal);
        Assert.All(demonstration, l => Assert.Equal(FigureCase.Demonstration, l.Figure.Case));

        Assert.Contains("booked spend 0.95 USD, threshold", company[0].Label, StringComparison.Ordinal);
        Assert.Contains("observed quality 4 rating, observed cost 0.0123 USD", company[1].Label, StringComparison.Ordinal);
        Assert.All(company, l => Assert.Equal(FigureCase.Observed, l.Figure.Case));
    }

    /// <summary>
    /// An OWN-MODE production's lines name its narration source and say it booked nothing, local compute unmeasured; a version
    /// recorded before the tenth resource reads its mode's source and says so.
    /// </summary>
    [Fact]
    public void AnOwnModeProductionsLinesNameItsSourceAndSayItBookedNothing()
    {
        ProducedItemReading Item(string mode, string? source, int version) => new()
        {
            Item = ItemId.New(),
            Version = new ItemVersion(version),
            Mode = mode,
            Designation = "Company",
            OpenedAt = SnapshotFixture.Instant.AddHours(-2),
            Stages = [],
            Operations = 1,
            UnstatedOperations = 0,
            Booked = new Money(0.007695m),
            OpenReservations = Money.Zero(),
            OpenReservationCount = 0,
            NarrationSource = source,
        };

        var own = Item("Own", "InHouseModel", 3);
        var before = Item("Metered", null, 2);
        var lines = ManagementComposers.Compose(SnapshotFixture.Build() with { Designation = StoreDesignation.Company, ProducedItems = [own, before] }).Lines;

        var ownOperations = Assert.Single(lines, l => l.Key == ReportLineKey.ProducedItemOperations && l.Label.Contains($"item {own.Item}", StringComparison.Ordinal));
        Assert.Contains("Own mode, narration source InHouseModel, Company store", ownOperations.Label, StringComparison.Ordinal);
        Assert.Contains("narrated from its own source and booked none, no metered spend; local compute unmeasured", ownOperations.RecordedText, StringComparison.Ordinal);

        var earlier = Assert.Single(lines, l => l.Key == ReportLineKey.ProducedItemOperations && l.Label.Contains($"item {before.Item}", StringComparison.Ordinal));
        Assert.Contains("narration source Vendor, read from the mode: recorded before the tenth resource", earlier.Label, StringComparison.Ordinal);
        Assert.Equal("1 operations", earlier.RecordedText);
    }
}
