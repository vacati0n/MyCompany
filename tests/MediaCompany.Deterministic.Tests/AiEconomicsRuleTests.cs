using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Accounting;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The AI-economics rules as pure functions: the cost controller, the evidence selection, the tier rule
/// and its downgrade, the controller's restriction, the missing-price removal and the composers.
///
/// Every amount, utilisation, quality and observation below is a DEMONSTRATION PARAMETER chosen to sit on
/// a rule's boundary; none is a benchmark result, a budget amount or an observation of the company's work.
/// </summary>
public sealed class AiEconomicsRuleTests
{
    private static readonly DateOnly Month = new(2026, 10, 1);
    private static readonly DateTimeOffset Reserved = DateTimeOffset.Parse("2026-10-09T08:00:00Z");
    private static readonly ChannelId Channel = ChannelId.New();

    // -----------------------------------------------------------------------
    // The cost controller
    // -----------------------------------------------------------------------

    /// <summary>
    /// Each band yields exactly its mapped action and no other, on both sides of every boundary, and
    /// repeated evaluation of one reading yields one action.
    /// </summary>
    [Theory]
    [InlineData(0.0, null, ControllerAction.None)]
    [InlineData(49.9999, null, ControllerAction.None)]
    [InlineData(50.0, 50, ControllerAction.AlertOnly)]
    [InlineData(74.9999, 50, ControllerAction.AlertOnly)]
    [InlineData(75.0, 75, ControllerAction.Downgrade)]
    [InlineData(89.9999, 75, ControllerAction.Downgrade)]
    [InlineData(90.0, 90, ControllerAction.Defer)]
    [InlineData(99.9999, 90, ControllerAction.Defer)]
    [InlineData(100.0, 100, ControllerAction.Refuse)]
    [InlineData(250.0, 100, ControllerAction.Refuse)]
    public void EachBandYieldsExactlyItsAction(double percent, int? threshold, ControllerAction action)
    {
        var summary = Summary(channelAmount: new Money(1m), channelPercent: (decimal)percent, channelBooked: percent == 0 ? 0m : 0.5m);

        var decision = CostController.Decide(summary, Reserved);
        var again = CostController.Decide(summary, Reserved);

        var channel = decision.Readings.Single(r => r.Scope == GoverningScope.Channel);
        Assert.Equal(action, channel.Action);
        Assert.Equal(threshold, channel.Threshold is { } t ? (int)t : null);
        Assert.Equal(action, decision.Action);
        Assert.Equal(again.Action, decision.Action);
        Assert.Equal(again.Readings.Select(r => r.Statement), decision.Readings.Select(r => r.Statement));
        Assert.Equal(Month, decision.BookingMonth);
        Assert.Equal(Reserved, decision.DecidedAt);
    }

    /// <summary>
    /// Where the channel and company readings reach different thresholds the higher one's action applies,
    /// whichever scope reaches it, and the decision names that scope.
    /// </summary>
    [Fact]
    public void TheHigherThresholdOfTheTwoReadingsApplies()
    {
        var companyHigher = CostController.Decide(Summary(new Money(1m), 60m, 0.6m, companyPercent: 92m), Reserved);
        Assert.Equal(ControllerAction.Defer, companyHigher.Action);
        Assert.Equal(GoverningScope.Company, companyHigher.DecidedBy);
        Assert.Equal(RefusalReason.DeferredAtThreshold, companyHigher.Reason);

        var channelHigher = CostController.Decide(Summary(new Money(1m), 100m, 1m, companyPercent: 76m), Reserved);
        Assert.Equal(ControllerAction.Refuse, channelHigher.Action);
        Assert.Equal(GoverningScope.Channel, channelHigher.DecidedBy);
        Assert.Equal(RefusalReason.RefusedAtThreshold, channelHigher.Reason);
    }

    /// <summary>
    /// A channel with no budget amount recorded refuses metered work under its OWN reason, naming the
    /// channel and the month: the amount is a recorded amount that was not recorded — no value, no zero —
    /// and no utilisation is computed against it. It is never the exceeded reason.
    /// </summary>
    [Fact]
    public void AMissingAmountRefusesUnderItsOwnReasonAndIsNeverZeroOrHeadroom()
    {
        var decision = CostController.Decide(Summary(channelAmount: null, channelPercent: null, channelBooked: 0m), Reserved);

        var channel = decision.Readings.Single(r => r.Scope == GoverningScope.Channel);
        var missing = Assert.IsType<RecordedAmount.NotRecorded>(channel.Amount);
        Assert.Contains(Channel.ToString(), missing.LookedFor, StringComparison.Ordinal);
        Assert.Contains("2026-10", missing.LookedFor, StringComparison.Ordinal);
        Assert.IsType<MeasurementQuantity.Unmeasured>(channel.Utilisation);
        Assert.Equal(ControllerAction.Refuse, decision.Action);
        Assert.Equal(RefusalReason.BudgetAmountNotRecorded, decision.Reason);
        Assert.NotEqual(RefusalReason.CostCeilingOrBudgetExceeded, decision.Reason);
        Assert.Null(channel.Threshold);
    }

    /// <summary>
    /// A scope whose booked spend cannot be stated — an operation booked into the month carries an unstated
    /// cost — reads its spend UNMEASURED, naming how many, never zero, and refuses under its own reason.
    /// </summary>
    [Fact]
    public void UnmeasuredSpendRefusesUnderItsOwnReasonAndIsNeverReadAsZero()
    {
        var summary = Summary(new Money(1m), 1.05m, 0.0105m) with
        {
            ChannelSpend = new ScopeSpend(3, 1, new Money(0.0105m)),
        };

        var decision = CostController.Decide(summary, Reserved);

        var channel = decision.Readings.Single(r => r.Scope == GoverningScope.Channel);
        var spend = Assert.IsType<MeasurementQuantity.Unmeasured>(channel.BookedSpend);
        Assert.Contains("1 of the 3 operations", spend.Detail, StringComparison.Ordinal);
        Assert.IsType<MeasurementQuantity.Unmeasured>(channel.Utilisation);
        Assert.Equal(RefusalReason.SpendUnmeasured, decision.Reason);
        Assert.Equal(ControllerAction.Refuse, decision.Action);
    }

    /// <summary>
    /// THE FIRST METERED OPERATION OF A MONTH: with a recorded amount and nothing booked, booked spend is an
    /// OBSERVED ZERO and the utilisation an observed zero, so no action is demanded; the company reading is
    /// governed against the recorded metered allotment, a recorded amount, and every decision states it.
    /// </summary>
    [Fact]
    public void AnEmptyMonthReadsObservedZeroAndAdmitsTheFirstOperation()
    {
        var decision = CostController.Decide(Summary(new Money(1m), 0m, 0m) with { ChannelSpend = new ScopeSpend(0, 0, Money.Zero()) }, Reserved);

        var channel = decision.Readings.Single(r => r.Scope == GoverningScope.Channel);
        Assert.IsType<MeasurementQuantity.ObservedZero>(channel.BookedSpend);
        Assert.IsType<MeasurementQuantity.ObservedZero>(channel.Utilisation);
        Assert.Equal(ControllerAction.None, decision.Action);
        Assert.Null(decision.DecidedBy);

        var company = decision.Readings.Single(r => r.Scope == GoverningScope.Company);
        var basis = Assert.IsType<RecordedAmount.Recorded>(company.Amount);
        Assert.Equal(34.42m, basis.Amount.Amount);
        Assert.Contains("recorded metered allotment of 34.42", decision.CompanyBasisStatement, StringComparison.Ordinal);
        Assert.Contains("never observations", decision.CompanyBasisStatement, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The controller's restriction and the missing price, in resolution
    // -----------------------------------------------------------------------

    /// <summary>
    /// Under a refusal, a deferral or an unmeasured reading only the targets that reach no provider stay
    /// admissible: a substitute is served; where none survives, a refusal is refused naming its reason and
    /// scope and a deferral is held under its own reason escalating from the booking instant.
    /// </summary>
    [Fact]
    public void UnderARefusalOrADeferralOnlyZeroCostTargetsStayAdmissible()
    {
        var provider = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var substitute = Fixture.Substitute(RouteTier.Emergency, DeterministicTaskRegistry.MetadataTemplatePopulation, 80);
        var refusing = CostController.Decide(Summary(null, null, 0m), Fixture.Now);
        var deferring = CostController.Decide(Summary(new Money(1m), 95m, 0.95m), Fixture.Now);

        var served = Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(
            Fixture.Request(), Fixture.Inputs([provider, substitute]) with { Controller = refusing }));
        Assert.Equal(substitute.Id, served.Route.Id);

        var refused = Assert.IsType<CapabilityResolution.Refused>(RouteResolver.Resolve(
            Fixture.Request(), Fixture.Inputs([provider]) with { Controller = refusing }));
        Assert.Equal(RefusalReason.BudgetAmountNotRecorded, refused.Reason);
        Assert.Contains(Channel.ToString(), refused.Detail, StringComparison.Ordinal);

        var held = Assert.IsType<CapabilityResolution.Held>(RouteResolver.Resolve(
            Fixture.Request(), Fixture.Inputs([provider]) with { Controller = deferring }));
        Assert.Equal(RefusalReason.DeferredAtThreshold, held.Reason);
        Assert.Equal(Fixture.Now + TimeSpan.FromHours(4), held.EscalatesAt);

        var hold = Fixture.HoldAndEscalate(RouteTier.Secondary);
        Assert.IsType<RouteTarget.HoldAndEscalate>(Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(
            Fixture.Request(), Fixture.Inputs([provider, hold]) with { Controller = deferring })).Route.Target);
    }

    /// <summary>
    /// A provider route whose model has no price in force is REMOVED with its own reason, never estimated
    /// at zero: alone it is held as unpriced, beside a priced route the priced one is served, and the
    /// estimate for it is absent rather than zero.
    /// </summary>
    [Fact]
    public void AMissingPriceIsRemovedAndNeverReadAsZero()
    {
        var unpriced = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, new ModelId("unpriced-model"), 95);
        var priced = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 90);

        var alone = Assert.IsType<CapabilityResolution.Held>(RouteResolver.Resolve(Fixture.Request(), Fixture.Inputs([unpriced])));
        Assert.Equal(RefusalReason.PriceNotInForce, alone.Reason);

        var served = Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(Fixture.Request(), Fixture.Inputs([unpriced, priced])));
        Assert.Equal(priced.Id, served.Route.Id);

        Assert.Null(RouteResolver.EstimateCost(unpriced, Fixture.Request(), Fixture.Inputs([unpriced])));
        Assert.NotNull(RouteResolver.EstimateCost(priced, Fixture.Request(), Fixture.Inputs([priced])));
    }

    // -----------------------------------------------------------------------
    // The tier rule and the downgrade
    // -----------------------------------------------------------------------

    /// <summary>
    /// Below the downgrade action the classes are the requested tier, then a higher one, then untiered, and
    /// a route stating a LOWER tier is never served: where only lower-stated routes survive the request is
    /// held under its own reason. Every resolved outcome carries its tier outcome.
    /// </summary>
    [Fact]
    public void BelowTheDowngradeActionNoLowerStatedTierIsServed()
    {
        var light = Tiered(RouteTier.Primary, ReasoningTier.Light);
        var standard = Tiered(RouteTier.Secondary, ReasoningTier.Standard);
        var deep = Tiered(RouteTier.Emergency, ReasoningTier.Deep);
        var untiered = Fixture.Substitute(RouteTier.Emergency, DeterministicTaskRegistry.MetadataTemplatePopulation, 80);

        var at = Resolved([light, standard, deep]);
        Assert.Equal(standard.Id, at.Route.Id);
        Assert.Equal(TierOutcome.AtRequestedTier, at.TierOutcome);

        var higher = Resolved([light, deep]);
        Assert.Equal(deep.Id, higher.Route.Id);
        Assert.Equal(TierOutcome.HigherTier, higher.TierOutcome);

        var none = Resolved([light, untiered]);
        Assert.Equal(untiered.Id, none.Route.Id);
        Assert.Equal(TierOutcome.Untiered, none.TierOutcome);

        var lower = Assert.IsType<CapabilityResolution.Held>(RouteResolver.Resolve(Fixture.Request(), Fixture.Inputs([light])));
        Assert.Equal(RefusalReason.NoRouteAtRequestedTier, lower.Reason);
    }

    /// <summary>
    /// Under the downgrade action a ROUTINE request is served the highest stated tier below the requested
    /// one, recorded as a downgrade, never below the floor and never onto an untiered route; a CRITICAL
    /// request is not downgraded; and where no eligible downgrade exists the record says none was available
    /// and why.
    /// </summary>
    [Fact]
    public void TheDowngradeServesALowerStatedTierOnlyWherePermittedAndRecordsWhenNoneWasAvailable()
    {
        var downgrade = CostController.Decide(Summary(new Money(1m), 80m, 0.8m), Fixture.Now);
        Assert.Equal(ControllerAction.Downgrade, downgrade.Action);

        var light = Tiered(RouteTier.Primary, ReasoningTier.Light);
        var standard = Tiered(RouteTier.Secondary, ReasoningTier.Standard);
        var lowFloorLight = Tiered(RouteTier.Primary, ReasoningTier.Light, quality: 50);
        var untiered = Fixture.Substitute(RouteTier.Emergency, DeterministicTaskRegistry.MetadataTemplatePopulation, 80);

        var routine = RouteResolver.ResolveWithRecord(Fixture.Request(), Fixture.Inputs([light, standard]) with { Controller = downgrade });
        var downgraded = Assert.IsType<CapabilityResolution.Resolved>(routine.Resolution);
        Assert.Equal(light.Id, downgraded.Route.Id);
        Assert.Equal(TierOutcome.Downgraded, downgraded.TierOutcome);
        Assert.Contains("downgraded from Standard to Light", routine.TierStatement, StringComparison.Ordinal);

        var critical = RouteResolver.ResolveWithRecord(
            Fixture.Request(criticality: Criticality.Critical), Fixture.Inputs([light, standard]) with { Controller = downgrade });
        Assert.Equal(standard.Id, Assert.IsType<CapabilityResolution.Resolved>(critical.Resolution).Route.Id);
        Assert.Contains("the request is critical", critical.TierStatement, StringComparison.Ordinal);

        var belowFloor = RouteResolver.ResolveWithRecord(Fixture.Request(floor: 70), Fixture.Inputs([lowFloorLight, standard]) with { Controller = downgrade });
        Assert.Equal(standard.Id, Assert.IsType<CapabilityResolution.Resolved>(belowFloor.Resolution).Route.Id);
        Assert.Contains("none was available", belowFloor.TierStatement, StringComparison.Ordinal);

        var onlyUntiered = RouteResolver.ResolveWithRecord(Fixture.Request(), Fixture.Inputs([untiered]) with { Controller = downgrade });
        var served = Assert.IsType<CapabilityResolution.Resolved>(onlyUntiered.Resolution);
        Assert.Equal(TierOutcome.Untiered, served.TierOutcome);
        Assert.Contains("a route stating no tier is never a downgrade", onlyUntiered.TierStatement, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Evidence selection
    // -----------------------------------------------------------------------

    /// <summary>
    /// Over candidates whose every compared quantity is observed in one unit, selection takes the CHEAPEST
    /// candidate meeting the floor — not the configured first — labelled evidence, with every observation it
    /// ranked on and their count; an observed-zero cost ranks as zero; and where no candidate meets the floor
    /// on observation nothing is selected and the request is held under the floor reason.
    /// </summary>
    [Fact]
    public void FullyObservedCandidatesAreRankedOnEvidence()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);
        var evidence = new[]
        {
            Observation(primary.Id, 92m, 0.0105m),
            Observation(primary.Id, 95m, 0.0090m),
            Observation(secondary.Id, 88m, 0m),
        };

        var outcome = EvidenceSelection.Select([primary, secondary], TaskClass.ScriptPass, new QualityRating(80), "USD", evidence);

        Assert.Equal(secondary.Id, outcome.Selected!.Id);
        Assert.Equal(SelectionBasis.Evidence, outcome.Record.Basis);
        Assert.Equal("3 observations ranked on", outcome.Record.ObservationCount.Describe());
        Assert.Equal(evidence.Select(o => o.Id.Value).Order(), outcome.Record.ObservationsRankedOn.Select(o => o.Value).Order());
        Assert.Equal(92m, Assert.IsType<MeasurementQuantity.ObservedValue>(outcome.Record.Candidates.Single(c => c.Route == primary.Id).ObservedQuality).Amount);
        Assert.Equal(0.0105m, Assert.IsType<MeasurementQuantity.ObservedValue>(outcome.Record.Candidates.Single(c => c.Route == primary.Id).ObservedCost).Amount);
        Assert.IsType<MeasurementQuantity.ObservedZero>(outcome.Record.Candidates.Single(c => c.Route == secondary.Id).ObservedCost);
        Assert.Equal(new QualityRating(90), outcome.Record.Candidates.Single(c => c.Route == primary.Id).ConfiguredRating);

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(
            Fixture.Request(floor: 80) with { TaskClass = TaskClass.ScriptPass }, Fixture.Inputs([primary, secondary]) with { Evidence = evidence }));
        Assert.Equal(secondary.Id, resolved.Route.Id);
        Assert.Equal(SelectionBasis.Evidence, resolved.Basis);

        var belowFloor = EvidenceSelection.Select([primary, secondary], TaskClass.ScriptPass, new QualityRating(96), "USD", evidence);
        Assert.Null(belowFloor.Selected);
        var held = Assert.IsType<CapabilityResolution.Held>(RouteResolver.Resolve(
            Fixture.Request(floor: 85) with { TaskClass = TaskClass.ScriptPass },
            Fixture.Inputs([primary, secondary]) with { Evidence = evidence.Select(o => o with { Quality = MeasurementQuantity.Observed(84m, BenchmarkObservation.QualityRatingUnit) }).ToArray() }));
        Assert.Equal(RefusalReason.NoRouteAtOrAboveFloor, held.Reason);
    }

    /// <summary>
    /// SELECTION REFUSES TO RANK ON AN UNMEASURED QUANTITY. Making each compared quantity of each candidate
    /// unmeasured in turn — or observed in another unit, or the task class absent, or a candidate without
    /// any observation — yields the configured ordering EXACTLY, labelled configured, naming the quantity;
    /// nothing is ranked on and the count is an observed zero.
    /// </summary>
    [Fact]
    public void AnyQuantityNotObservedInOneUnitYieldsTheConfiguredOrderingNamingIt()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);
        var baseline = new[] { Observation(primary.Id, 92m, 0.0105m), Observation(secondary.Id, 88m, 0.0035m) };
        var gap = MeasurementQuantity.NotMeasured(UnmeasuredReason.SourceCannotStateOne, "fixture: not stated");

        var variants = new (string Named, BenchmarkObservation[] Evidence, TaskClass? Task)[]
        {
            ("the quality of route " + primary.Id, [baseline[0] with { Quality = gap }, baseline[1]], TaskClass.ScriptPass),
            ("the quality of route " + secondary.Id, [baseline[0], baseline[1] with { Quality = gap }], TaskClass.ScriptPass),
            ("the cost of route " + primary.Id, [baseline[0] with { Cost = gap }, baseline[1]], TaskClass.ScriptPass),
            ("the cost of route " + secondary.Id, [baseline[0], baseline[1] with { Cost = gap }], TaskClass.ScriptPass),
            ("not in rating on the 0 to 100 scale", [baseline[0] with { Quality = MeasurementQuantity.Observed(4m, "stars") }, baseline[1]], TaskClass.ScriptPass),
            ("not in USD", [baseline[0], baseline[1] with { Cost = MeasurementQuantity.Observed(0.003m, "EUR") }], TaskClass.ScriptPass),
            ($"route {secondary.Id} has no observation", [baseline[0]], TaskClass.ScriptPass),
            ("names no task class", baseline, null),
        };

        foreach (var (named, evidence, task) in variants)
        {
            var outcome = EvidenceSelection.Select([secondary, primary], task, new QualityRating(80), "USD", evidence);

            Assert.Equal(primary.Id, outcome.Selected!.Id);
            Assert.Equal(SelectionBasis.Configured, outcome.Record.Basis);
            Assert.Equal(new[] { primary.Id, secondary.Id }, outcome.Record.Candidates.Select(c => c.Route));
            Assert.Empty(outcome.Record.ObservationsRankedOn);
            Assert.IsType<MeasurementQuantity.ObservedZero>(outcome.Record.ObservationCount);
            Assert.Contains(named, outcome.Record.Statement, StringComparison.Ordinal);
            Assert.Contains("never presented as an observed quality", outcome.Record.Statement, StringComparison.Ordinal);
        }
    }

    // -----------------------------------------------------------------------
    // The composers
    // -----------------------------------------------------------------------

    /// <summary>
    /// The tier distribution over a month holding no operation reads unmeasured at every tier, naming what
    /// was looked for; over recorded parts every observed part sums exactly to the total, an unstated cost
    /// makes its part's cost and the total's unmeasured, and the assumed split travels labelled.
    /// </summary>
    [Fact]
    public async Task TheTierDistributionComposesEachPartInItsCase()
    {
        var partitions = new StubPartitions();
        var empty = EconomicsComposers.TierDistribution(await partitions.TierDistributionAsync(Month, CancellationToken.None));
        Assert.All(empty.Parts.Append(empty.Total), p => Assert.IsType<MeasurementQuantity.Unmeasured>(p.Operations));
        Assert.IsType<MeasurementQuantity.Unmeasured>(empty.TierEvidence);

        partitions.TierParts =
        [
            new TierPartTotals(ReasoningTier.Light, 2, 3_000, 500, 0, new Money(0.0105m), 1),
            new TierPartTotals(ReasoningTier.Deep, 1, 3_000, 1_000, 0, new Money(0.024m), 0),
            new TierPartTotals(null, 1, 100, 100, 0, new Money(0.0018m), 0),
        ];
        var reading = EconomicsComposers.TierDistribution(await partitions.TierDistributionAsync(Month, CancellationToken.None));

        Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Parts.Single(p => p.Tier == ReasoningTier.Light).Cost);
        Assert.IsType<MeasurementQuantity.ObservedZero>(reading.Parts.Single(p => p.Tier == ReasoningTier.Standard).Operations);
        Assert.Equal(0.024m, Assert.IsType<MeasurementQuantity.ObservedValue>(reading.Parts.Single(p => p.Tier == ReasoningTier.Deep).Cost).Amount);
        Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Total.Cost);
        Assert.Equal("4 operations", reading.Total.Operations.Describe());
        Assert.Equal("3 operations carrying a served tier", reading.TierEvidence.Describe());
        Assert.Equal(EconomicsComposers.AssumedSplit, reading.AssumedSplit);
        Assert.StartsWith("assumed, not measured", reading.AssumedSplit, StringComparison.Ordinal);
    }

    /// <summary>
    /// A recorded amount is refused where it would carry a zero or no register, and a not-recorded amount
    /// is refused without saying what was looked for; the two render apart and the not-recorded one carries
    /// no amount.
    /// </summary>
    [Fact]
    public void ARecordedAmountIsNeverZeroAndANotRecordedOneCarriesNoValue()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecordedAmount.Of(Money.Zero(), "the budget register"));
        Assert.Throws<ArgumentException>(() => RecordedAmount.Of(new Money(1m), " "));
        Assert.Throws<ArgumentException>(() => RecordedAmount.Missing(" "));

        var missing = RecordedAmount.Missing("no budget amount is recorded for the fixture channel");
        Assert.DoesNotContain(missing.Describe(), c => char.IsDigit(c));
        Assert.DoesNotContain(typeof(RecordedAmount.NotRecorded).GetProperties(), p => p.PropertyType == typeof(Money));
        Assert.StartsWith("recorded 1", RecordedAmount.Of(new Money(1m), "the budget register").Describe(), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------

    private static CapabilityResolution.Resolved Resolved(IReadOnlyList<Route> routes) =>
        Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(Fixture.Request(), Fixture.Inputs(routes)));

    private static Route Tiered(RouteTier tier, ReasoningTier stated, int quality = 90) =>
        new(RouteId.New(),
            CapabilityClass.EditorialReasoning,
            tier,
            new RouteTarget.HoldAndEscalate("a demonstration route stating a tier"),
            new QualityRating(quality),
            new ContextCapacity(int.MaxValue),
            "no provider; a demonstration route",
            Fixture.Today.AddDays(-10),
            stated);

    private static BenchmarkObservation Observation(RouteId route, decimal quality, decimal cost) => new()
    {
        Id = BenchmarkObservationId.New(),
        Entry = CorpusEntryId.New(),
        TaskClass = TaskClass.ScriptPass,
        Route = route,
        Model = Fixture.PrimaryModel,
        Operation = OperationId.New(),
        Quality = MeasurementQuantity.Observed(quality, BenchmarkObservation.QualityRatingUnit),
        Cost = MeasurementQuantity.Observed(cost, "USD"),
        Latency = MeasurementQuantity.Observed(1_000m, BenchmarkObservation.LatencyUnit),
        ObservedAt = Fixture.Now.AddDays(-1),
        Period = Fixture.Today,
    };

    private static GoverningReadingsSummary Summary(
        Money? channelAmount,
        decimal? channelPercent,
        decimal channelBooked,
        decimal companyPercent = 0m) =>
        new(Month,
            Channel,
            channelAmount,
            new ScopeSpend(channelBooked == 0m ? 0 : 1, 0, new Money(channelBooked)),
            channelPercent,
            CompanyId.New(),
            new ScopeSpend(companyPercent == 0m ? 0 : 1, 0, new Money(companyPercent * 0.3442m)),
            companyPercent,
            new Money(100m));
}
