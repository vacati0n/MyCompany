using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Reporting;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The analytics surface, composed from records the completed delivery waves write.
///
/// The demonstrations run the surface against a state in which NOTHING was observed, because that
/// is the state the company is actually in and the state in which a figure invented out of an
/// absence would do the damage.
/// </summary>
public sealed class AnalyticsSurfaceTests
{
    private static readonly DateOnly Period = new(2026, 10, 1);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(1);

    // -----------------------------------------------------------------------
    // Cost (plan task T-006)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The concrete defect, corrected. A period holding no recorded operation is UNMEASURED, and
    /// never a zero amount — which is what the delivered currency-returning member cannot say.
    /// </summary>
    [Fact]
    public void APeriodHoldingNoRecordedOperationIsUnmeasuredAndNotZero()
    {
        var model = AnalyticsComposers.Cost(new PeriodSummary(
            Period, Money.Zero(), new Money(77.41m), new Money(34.42m), new Money(42.99m),
            new Money(-77.41m), ContainsEstimates: false, Operations: 0));

        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(model.Cost);
        Assert.Equal(UnmeasuredReason.NoObservationExists, unmeasured.Reason);
        Assert.Contains("no operation is recorded", unmeasured.Detail, StringComparison.Ordinal);

        // The count behind the figure is itself a measured quantity, and it is an observed zero.
        Assert.IsType<MeasurementQuantity.ObservedZero>(model.Operations);

        // The two do not render alike, which is the property the surface exists to hold.
        Assert.NotEqual(model.Cost.Describe(), model.Operations.Describe());
    }

    /// <summary>
    /// A period holding records whose aggregation comes to zero is an OBSERVED ZERO. This is the
    /// other half of the same distinction, and the named zero-cost set is the real case: those
    /// operations are recorded and they cost nothing.
    /// </summary>
    [Fact]
    public void APeriodHoldingRecordsThatAggregateToZeroIsAnObservedZero()
    {
        var model = AnalyticsComposers.Cost(new PeriodSummary(
            Period, Money.Zero(), new Money(77.41m), new Money(34.42m), new Money(42.99m),
            new Money(-77.41m), ContainsEstimates: false, Operations: 4));

        Assert.IsType<MeasurementQuantity.ObservedZero>(model.Cost);
        Assert.Equal("4 operations", model.Operations.Describe());
    }

    /// <summary>Every cost figure states the number of records behind it and how its prices are dated.</summary>
    [Fact]
    public void ACostFigureStatesItsRecordCountAndItsPriceBasis()
    {
        var model = AnalyticsComposers.Cost(
            new ItemSummary(Item, new Money(1.25m), ContainsEstimates: true, Operations: 3));

        Assert.Equal("1.25 USD", model.Cost.Describe());
        Assert.Equal("3 operations", model.Operations.Describe());
        Assert.Contains("metered", model.PriceBasis, StringComparison.Ordinal);
        Assert.True(model.ContainsEstimates);

        var itemWithNoRecord = AnalyticsComposers.Cost(
            new ItemSummary(Item, Money.Zero(), ContainsEstimates: false, Operations: 0));

        Assert.IsType<MeasurementQuantity.Unmeasured>(itemWithNoRecord.Cost);
    }

    // -----------------------------------------------------------------------
    // Approval effort (plan task T-008)
    // -----------------------------------------------------------------------

    private static ApprovalExerciseLabel Label() =>
        new("wave-4 analytics read", 1, "a set of one; it establishes the mechanism and no series");

    /// <summary>
    /// Review, queue and rework are three separate quantities. A first pass records a measured
    /// zero rework, which renders differently from a component nobody measured.
    /// </summary>
    [Fact]
    public void ApprovalEffortIsThreeSeparateQuantitiesWithZeroDistinctFromUnmeasured()
    {
        var approval = new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved", Now.AddMinutes(-2), Now, queuedAt: Now.AddMinutes(-2));

        var model = AnalyticsComposers.ApprovalEffort(
            ApprovalMeasurement.Derive(approval, null, Label()));

        Assert.Equal("2 minutes", model.Review.Describe());
        Assert.IsType<MeasurementQuantity.ObservedZero>(model.Queue);
        Assert.IsType<MeasurementQuantity.ObservedZero>(model.Rework);
        Assert.NotEqual(model.Review.Describe(), model.Queue.Describe());
        Assert.Equal(Label().ClassLimits, model.Label.ClassLimits);
    }

    /// <summary>
    /// A component with no recorded mark is unmeasured and never zero minutes, and the unmeasured
    /// rendering names what was missing.
    /// </summary>
    [Fact]
    public void AnApprovalComponentWithNoRecordedMarkIsUnmeasured()
    {
        var approval = new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved", Now.AddMinutes(-2), Now);

        var model = AnalyticsComposers.ApprovalEffort(
            ApprovalMeasurement.Derive(approval, null, Label()));

        var queue = Assert.IsType<MeasurementQuantity.Unmeasured>(model.Queue);
        Assert.Contains("no queued mark", queue.Detail, StringComparison.Ordinal);

        // The review component is derived from its own two marks, both recorded, so it reports
        // what was recorded rather than inheriting the queue component's absence.
        Assert.Equal("2 minutes", model.Review.Describe());

        // And no threshold, target or pass line is stated anywhere in the output.
        foreach (var word in new[] { "threshold", "target", "pass line", "should be", "acceptable" })
        {
            Assert.DoesNotContain(word, model.Review.Describe(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, model.Label.ClassLimits, StringComparison.OrdinalIgnoreCase);
        }
    }

    // -----------------------------------------------------------------------
    // Production-path stage outcomes and the composition step (T-009, decision D-004)
    // -----------------------------------------------------------------------

    private static ItemDossier EmptyDossier() => new() { Item = Item, Version = Version };

    /// <summary>
    /// All twelve stages appear whatever was recorded, and a stage with nothing recorded reads as
    /// unmeasured rather than as absent from the surface or inferred from a later stage.
    /// </summary>
    [Fact]
    public void EveryStageOfTheClosedSetAppearsAndAnUnrecordedOneIsUnmeasured()
    {
        var surface = AnalyticsComposers.StageOutcomes(
            EmptyDossier(), PublishReadyVerdict.Pass(), FirstPublicationConditionRegister.Empty);

        Assert.Equal(ProductionStageSet.Count, surface.Stages.Count);
        Assert.Equal(ProductionStageSet.All, surface.Stages.Select(s => s.Stage).ToArray());

        Assert.All(surface.Stages, stage =>
        {
            Assert.Null(stage.RecordedOutcome);
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(stage.RecordedOutcomes);
            Assert.Equal(UnmeasuredReason.NoObservationExists, unmeasured.Reason);
            Assert.Contains("nothing is inferred from any later stage", unmeasured.Detail, StringComparison.Ordinal);
        });
    }

    /// <summary>A recorded stage outcome reads as recorded, and states what it recorded.</summary>
    [Fact]
    public void ARecordedStageOutcomeIsReadableIncludingAFailure()
    {
        var dossier = EmptyDossier() with
        {
            Stages =
            [
                new StageEvidence
                {
                    Stage = ProductionStage.Research,
                    Outcome = StageOutcome.Failed,
                    Summary = "the research stage failed",
                    RecordedAt = Now,
                },
            ],
        };

        var surface = AnalyticsComposers.StageOutcomes(
            dossier, PublishReadyVerdict.Pass(), FirstPublicationConditionRegister.Empty);

        var research = surface.Stages.Single(s => s.Stage == ProductionStage.Research);

        Assert.Equal(StageOutcome.Failed, research.RecordedOutcome);
        Assert.Equal("1 recorded outcomes", research.RecordedOutcomes.Describe());
        Assert.Contains("Failed", research.Statement, StringComparison.Ordinal);

        // The stage after it stays unmeasured: nothing is inferred from a neighbour.
        Assert.IsType<MeasurementQuantity.Unmeasured>(
            surface.Stages.Single(s => s.Stage == ProductionStage.Script).RecordedOutcomes);
    }

    /// <summary>
    /// Decision D-004. Nothing is recorded at the composition gate transition, the step reads as
    /// unmeasured, and the records that DO exist are named rather than a gate state being inferred
    /// from them.
    /// </summary>
    [Fact]
    public void TheCompositionStepReadsAsUnmeasuredAndNamesTheRecordsThatDoExist()
    {
        var step = AnalyticsComposers.CompositionStep();

        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(step.GateTransitions);
        Assert.Equal(UnmeasuredReason.NoObservationExists, unmeasured.Reason);
        Assert.Equal("no gate transition was recorded at composition", unmeasured.Detail);

        Assert.Contains("dispatch record", step.DispatchRecordEvidence, StringComparison.Ordinal);
        Assert.Contains("audit entry", step.AuditEntryEvidence, StringComparison.Ordinal);
        Assert.Contains("no gate state is inferred", step.Statement, StringComparison.Ordinal);
    }

    /// <summary>
    /// A refusal from a precondition whose recorded state is UNKNOWN keeps the unknown visible: it
    /// refuses, and it does not read as a satisfied answer or as an absent record.
    /// </summary>
    [Fact]
    public void AnUnknownPreconditionStaysVisibleInTheRefusal()
    {
        var register = new FirstPublicationConditionRegister(
        [
            new ConditionObservation(
                FirstPublicationCondition.TwoStepVerification,
                ConditionState.Unknown,
                "the setting page could not be read",
                new DateOnly(2026, 9, 30)),
        ]);

        var refusals = AnalyticsComposers.Refusals(EmptyDossier(), PublishReadyVerdict.Pass(), register);

        var unknown = refusals.Single(r => r.Precondition == nameof(FirstPublicationCondition.TwoStepVerification));
        Assert.Equal(nameof(ConditionResolution.Unknown), unknown.RecordedState);
        Assert.Contains("UNKNOWN", unknown.Detail, StringComparison.Ordinal);

        // The other two refuse as ABSENT, which is a different recorded state and stays its own.
        var absent = refusals.Single(r => r.Precondition == nameof(FirstPublicationCondition.PaymentAccount));
        Assert.Equal(nameof(ConditionResolution.Absent), absent.RecordedState);
        Assert.NotEqual(unknown.RecordedState, absent.RecordedState);

        Assert.All(refusals, r => Assert.False(string.IsNullOrWhiteSpace(r.RecordedState)));
    }

    /// <summary>Every publish-ready refusal is readable and names the precondition that refused.</summary>
    [Fact]
    public void EveryPublishReadyRefusalNamesThePreconditionAndTheStateItRefusedFrom()
    {
        var verdict = PublishReadyVerdict.Refuse(
        [
            new PublishReadyRefusalDetail(
                PublishReadyRefusal.RuntimeNotRecorded, "the finished runtime is not recorded"),
        ]);

        var refusal = AnalyticsComposers
            .Refusals(EmptyDossier(), verdict, FirstPublicationConditionRegister.Empty)
            .First();

        Assert.Equal(nameof(PublishReadyRefusal.RuntimeNotRecorded), refusal.Precondition);
        Assert.Equal("absent", refusal.RecordedState);
    }

    // -----------------------------------------------------------------------
    // Supply audit (plan task T-010)
    // -----------------------------------------------------------------------

    /// <summary>
    /// A subject whose clip count was never established shows as unmeasured and carries NO NUMBER
    /// in its place, while a library that answered literally and reported none is an observed zero.
    /// </summary>
    [Fact]
    public void AnUnestablishedClipCountIsUnmeasuredAndALiteralNoneIsAnObservedZero()
    {
        var models = AnalyticsComposers.SupplyAudit(
        [
            new SupplyAuditEntry
            {
                RequestedTerm = "saola",
                Library = "committed library",
                Fidelity = TermFidelity.Substituted,
                AuditedAt = Now,
                TermAnswered = "saona",
                UnobtainedReason = CountUnobtainedReason.TermNotAnsweredLiterally,
                WhatWouldObtainIt = "a literal single-token answer from the library",
            },
            new SupplyAuditEntry
            {
                RequestedTerm = "pangolin",
                Library = "committed library",
                Fidelity = TermFidelity.LiteralSingleToken,
                AuditedAt = Now,
                TermAnswered = "pangolin",
                UnobtainedReason = CountUnobtainedReason.LibraryReportsNone,
                WhatWouldObtainIt = "nothing further; the library answered literally",
            },
        ]);

        var unestablished = models.Single(m => m.Subject == "saola");
        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(unestablished.ClipCount);
        Assert.Equal(UnmeasuredReason.SourceCannotStateOne, unmeasured.Reason);
        Assert.Contains("saona", unmeasured.Detail, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(unestablished.WhatWouldObtainIt));

        var none = models.Single(m => m.Subject == "pangolin");
        Assert.IsType<MeasurementQuantity.ObservedZero>(none.ClipCount);
        Assert.NotEqual(unestablished.ClipCount.Describe(), none.ClipCount.Describe());
    }

    /// <summary>
    /// No grouping or label in the supply output names the current subject, so the surface stays
    /// re-pointable: the key is whatever term the audit recorded.
    /// </summary>
    [Fact]
    public void TheSupplyOutputGroupsOnTheRecordedSubjectAndAssumesNoPermanentOne()
    {
        var models = AnalyticsComposers.SupplyAudit(
        [
            new SupplyAuditEntry
            {
                RequestedTerm = "a wholly different subject",
                Library = "another library",
                Fidelity = TermFidelity.LiteralSingleToken,
                AuditedAt = Now,
                Count = 7,
            },
        ]);

        var model = Assert.Single(models);
        Assert.Equal("a wholly different subject", model.Subject);
        Assert.Equal("7 clips", model.ClipCount.Describe());
    }

    // -----------------------------------------------------------------------
    // Compliance determinations (plan task T-011)
    // -----------------------------------------------------------------------

    /// <summary>
    /// One entry per member of the closed five-member set. A determination with no recorded
    /// outcome is unmeasured rather than a negative outcome, and no outcome is inferred from
    /// another determination's.
    /// </summary>
    [Fact]
    public void ADeterminationWithNoRecordedOutcomeIsUnmeasuredRatherThanNegative()
    {
        var models = AnalyticsComposers.Determinations(
        [
            new DeterminationResolution
            {
                Determination = ComplianceDetermination.DuplicateDetection,
                Outcome = DeterminationOutcome.Evidenced,
                ResolvedAt = Now,
                Evidence = "a recorded distance",
            },
        ]);

        Assert.Equal(Enum.GetValues<ComplianceDetermination>().Length, models.Count);

        var recorded = models.Single(m => m.Determination == ComplianceDetermination.DuplicateDetection);
        Assert.Equal(DeterminationOutcome.Evidenced, recorded.RecordedOutcome);

        var absent = models.Single(m => m.Determination == ComplianceDetermination.AdvertiserSuitability);
        Assert.Null(absent.RecordedOutcome);
        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(absent.RecordedResolutions);
        Assert.Contains("nothing is inferred from any other determination", unmeasured.Detail, StringComparison.Ordinal);

        // Unmeasured is not the same reading as a failed determination.
        Assert.DoesNotContain("Failed", absent.Statement, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The served tier and the single-record caveat (plan tasks T-012, T-014)
    // -----------------------------------------------------------------------

    private static ServedTierRecord Record(ReasoningTier? requested, ReasoningTier? served) => new()
    {
        Operation = OperationId.New(),
        Period = Period,
        Requested = requested,
        Served = served,
    };

    /// <summary>
    /// Every output reporting a served tier carries the caveat, and a record whose route stated no
    /// tier carries the explicit absence marker with no tier value in its place.
    /// </summary>
    [Fact]
    public void EveryServedTierOutputCarriesTheCaveatAndAnAbsentTierCarriesNoValue()
    {
        var models = AnalyticsComposers.ServedTiers(
        [
            Record(ReasoningTier.Deep, ReasoningTier.Light),
            Record(ReasoningTier.Deep, null),
        ]);

        // One period, ONE served-tier count, across every output that carries the caveat. Two of
        // the two records are in the period and one of them carries a served tier, and the caveat
        // counts served-tier records, so both outputs say one.
        Assert.All(models, m =>
        {
            Assert.Contains("does not settle", m.Caveat.Statement, StringComparison.Ordinal);
            Assert.Equal("1 served-tier records", m.Caveat.ServedTierRecords.Describe());
        });

        var ratioOverTheSameRecords = AnalyticsComposers.TierRatio(Period,
        [
            Record(ReasoningTier.Deep, ReasoningTier.Light),
            Record(ReasoningTier.Deep, null),
        ]);

        Assert.Equal(
            models[0].Caveat.ServedTierRecords.Describe(),
            ratioOverTheSameRecords.Caveat.ServedTierRecords.Describe());

        var observed = models.Single(m => m.Served is not null);
        Assert.Equal(ReasoningTier.Light, observed.Served);
        Assert.NotEqual(observed.Requested, observed.Served);

        var absent = models.Single(m => m.Served is null);
        Assert.Contains("absent", absent.ServedStatement, StringComparison.Ordinal);
        Assert.DoesNotContain("Deep", absent.ServedStatement, StringComparison.Ordinal);
        Assert.DoesNotContain("Light", absent.ServedStatement, StringComparison.Ordinal);
    }

    /// <summary>
    /// Decision D-007. A period holding fewer than two served-tier records presents NO ratio: the
    /// figure is unmeasured and states why, and the caveat travels with it either way.
    /// </summary>
    [Fact]
    public void APeriodHoldingFewerThanTwoServedTierRecordsPresentsNoRatio()
    {
        foreach (var records in new IReadOnlyList<ServedTierRecord>[]
                 {
                     [],
                     [Record(ReasoningTier.Deep, null)],
                     [Record(ReasoningTier.Deep, ReasoningTier.Deep)],
                 })
        {
            var ratio = AnalyticsComposers.TierRatio(Period, records);

            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(ratio.Ratio);
            Assert.Contains("fewer than two served-tier records", unmeasured.Detail, StringComparison.Ordinal);
            Assert.Contains("assumption", ratio.AssumptionLabel, StringComparison.Ordinal);
            Assert.Contains("not been measured", ratio.AssumptionLabel, StringComparison.Ordinal);
        }
    }

    /// <summary>Two served-tier records make the ratio readable, still labelled and still caveated.</summary>
    [Fact]
    public void TwoServedTierRecordsMakeTheRatioReadable()
    {
        var ratio = AnalyticsComposers.TierRatio(Period,
        [
            Record(ReasoningTier.Deep, ReasoningTier.Deep),
            Record(ReasoningTier.Deep, ReasoningTier.Light),
        ]);

        Assert.Equal("50 percent", ratio.Ratio.Describe());
        Assert.Equal("2 served-tier records", ratio.Caveat.ServedTierRecords.Describe());
    }

    // -----------------------------------------------------------------------
    // The six revenue-derived figures (plan task T-007)
    // -----------------------------------------------------------------------

    /// <summary>
    /// With no revenue parameter recorded, NO figure is visible and none yields a value. The
    /// absence is the default state rather than a filtered one: there is no admitted parameter, so
    /// the admission function has no argument and is not callable.
    /// </summary>
    [Fact]
    public void NoRevenueDerivedFigureIsVisibleWhileNoParameterIsRecorded()
    {
        Assert.Empty(AnalyticsComposers.Visible([]));

        // A parameter recorded WITHOUT a source observation lights none of the six either.
        Assert.Empty(AnalyticsComposers.Visible(
        [
            new RevenueParameterRecord { Observation = MeasurementQuantity.Observed(2.50m, "USD") },
        ]));
    }

    /// <summary>
    /// All six are present in the delivered surface as deferred declarations carrying no value
    /// field at all, so none can be rendered as zero, a dash or any placeholder.
    /// </summary>
    [Fact]
    public void AllSixRevenueDerivedFiguresArePresentInTheCatalogueAndCarryNoValue()
    {
        Assert.Equal(6, AnalyticsComposers.RevenueDerived.Count);

        foreach (var figure in AnalyticsComposers.RevenueDerived)
        {
            var declaration = MeasureCatalogue.DeclarationFor(figure);

            Assert.Contains(declaration, MeasureCatalogue.Deferred);
            Assert.Contains("not yet available", declaration.Display, StringComparison.Ordinal);
            Assert.DoesNotContain(
                "Value",
                typeof(Measure.Deferred).GetProperties().Select(p => p.Name));
        }
    }

    /// <summary>
    /// A figure becomes visible only against a parameter recorded with its source observation, and
    /// the recorded source is shown alongside it. The five that need an aggregation nobody has
    /// recorded are visible AND unmeasured, which is a different statement from a value.
    /// </summary>
    [Fact]
    public void AFigureIsVisibleOnlyAgainstAnObservedParameterAndShowsItsRecordedSource()
    {
        var visible = AnalyticsComposers.Visible(
        [
            new RevenueParameterRecord
            {
                Observation = MeasurementQuantity.Observed(2.50m, "USD"),
                SourceObservation = "a platform revenue statement",
                ObservedOn = new DateOnly(2026, 10, 1),
            },
        ]);

        Assert.Equal(6, visible.Count);
        Assert.All(visible, f => Assert.Equal("a platform revenue statement", f.RecordedSource));

        var revenue = visible.Single(f => f.Figure == RevenueDerivedFigure.Revenue);
        Assert.Equal("2.50 USD", revenue.Quantity.Describe());

        foreach (var derived in visible.Where(f => f.Figure != RevenueDerivedFigure.Revenue))
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(derived.Quantity);
            Assert.Equal(UnmeasuredReason.SourceCannotStateOne, unmeasured.Reason);
            Assert.Contains("no money arithmetic", unmeasured.Detail, StringComparison.Ordinal);
        }
    }

    // -----------------------------------------------------------------------
    // The measurable-now report (the free-form value slot, closed)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The reported measure carries a measurement quantity and no free-form value slot, so a
    /// string a caller composed can no longer reach a reader unclassified through this surface.
    /// </summary>
    [Fact]
    public void AReportedMeasureCarriesAMeasurementQuantityAndNoFreeFormValueSlot()
    {
        var members = typeof(ReportedMeasure).GetProperties().Select(p => p.Name).ToArray();

        Assert.Contains("Quantity", members);
        Assert.DoesNotContain("Value", members);
        Assert.Equal(typeof(MeasurementQuantity), typeof(ReportedMeasure).GetProperty("Quantity")!.PropertyType);
    }

    /// <summary>
    /// A period holding no recorded operation reports every money figure as unmeasured, so the
    /// surface that a reader actually sees carries the same distinction the representation does.
    /// </summary>
    [Fact]
    public async Task TheMeasurableNowReportIsUnmeasuredThroughoutForAPeriodWithNoRecordedOperation()
    {
        var service = new ReportingService(new StubPartitions(), new StubClock());

        var lines = await service.MeasurableNowAsync(Period, CancellationToken.None);

        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.IsType<MeasurementQuantity.Unmeasured>(line.Quantity));
        Assert.False(ReportingService.ReportedSetContainsRevenueMeasure(lines));
    }

    /// <summary>With records behind them the same figures are observed, and the set stays revenue-free.</summary>
    [Fact]
    public async Task TheMeasurableNowReportIsObservedForAPeriodHoldingRecordedOperations()
    {
        // The report reads one source, the channel partition, whose company row carries the operations.
        var service = new ReportingService(
            new StubPartitions
            {
                Horizon = DateTimeOffset.UnixEpoch,
                EarliestEntry = DateTimeOffset.UnixEpoch,
                CompanyOperations = StubPartitions.Empty with { Operations = 2, Cost = new Money(1.25m) },
                CompanyVariance = new Money(-76.16m),
            },
            new StubClock());

        var lines = await service.MeasurableNowAsync(Period, CancellationToken.None);

        Assert.Contains(lines, l => l.Name == "monthly-cost-total"
            && l.Quantity is MeasurementQuantity.ObservedValue);
        Assert.False(ReportingService.ReportedSetContainsRevenueMeasure(lines));

        // The deferred set names the parameter each measure waits on and renders no value.
        Assert.All(service.DeferredMeasures(), d => Assert.Contains("not yet available", d, StringComparison.Ordinal));
    }

    /// <summary>A cost rollup reader over a period whose recorded operation count is supplied.</summary>
    private sealed class StubCosts(long operations) : ICostRollupReader
    {
        // The retired members are gone from the port (the AI-economics change); the period summary now
        // carries its month closure from the read that produced it.
        public Task<MonthReading<PeriodSummary>> PeriodSummaryAsync(DateOnly period, CancellationToken ct) =>
            Task.FromResult(new MonthReading<PeriodSummary>(
                new MonthClosure(period, ClosedByThisRead: true, Now, null),
                new PeriodSummary(
                    period, new Money(1.25m), new Money(77.41m), new Money(34.42m), new Money(42.99m),
                    new Money(-76.16m), ContainsEstimates: false, Operations: operations)));

        public Task<ItemSummary> ItemSummaryAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult(new ItemSummary(item, new Money(1.25m), ContainsEstimates: false, Operations: operations));
    }

    private sealed class StubClock : IClock
    {
        public DateTimeOffset UtcNow => Now;
    }

    // -----------------------------------------------------------------------
    // The whole surface with nothing observed
    // -----------------------------------------------------------------------

    /// <summary>
    /// The surface populated with NO observation at all.
    ///
    /// The claim this decides, stated exactly: every quantity resolves to the unmeasured case,
    /// every one names which of the two closed reasons applies, and NO RENDERING CARRIES A VALUE —
    /// the rendering is the type's own unmeasured prefix followed by the stated detail and nothing
    /// else, and the prefix carries no amount, no unit and no digit of any kind.
    ///
    /// The detail MAY carry digits, by design: it states what was looked for and where, and that
    /// statement legitimately names a record count or a period. Those digits belong to the stated
    /// detail and never to a value, and the assertion below draws the line where the type does
    /// rather than asserting an absence of digits the surface does not actually hold. The earlier
    /// shape of this check asserted only the prefix and a non-blank reason, which decided rather
    /// less than the claim it was offered for.
    /// </summary>
    [Fact]
    public void TheWholeSurfaceWithNoObservationCarriesNoValueInAnyRendering()
    {
        var quantities = new List<MeasurementQuantity>();

        var surface = AnalyticsComposers.StageOutcomes(
            EmptyDossier(), PublishReadyVerdict.Pass(), FirstPublicationConditionRegister.Empty);
        quantities.AddRange(surface.Stages.Select(s => s.RecordedOutcomes));
        quantities.Add(surface.CompositionStep.GateTransitions);

        quantities.Add(AnalyticsComposers.Cost(new PeriodSummary(
            Period, Money.Zero(), new Money(77.41m), new Money(34.42m), new Money(42.99m),
            new Money(-77.41m), ContainsEstimates: false, Operations: 0)).Cost);

        quantities.Add(AnalyticsComposers.Cost(
            new ItemSummary(Item, Money.Zero(), ContainsEstimates: false, Operations: 0)).Cost);

        quantities.AddRange(AnalyticsComposers
            .Determinations([])
            .Select(d => d.RecordedResolutions));

        quantities.Add(AnalyticsComposers.TierRatio(Period, []).Ratio);

        Assert.NotEmpty(quantities);

        var someDetailCarriesADigit = false;

        foreach (var quantity in quantities)
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);

            Assert.Contains(unmeasured.Reason, Enum.GetValues<UnmeasuredReason>());
            Assert.False(string.IsNullOrWhiteSpace(unmeasured.Detail));

            var prefix = MeasurementQuantity.UnmeasuredPrefix(unmeasured.Reason);
            var rendered = unmeasured.Describe();

            // The whole rendering is the prefix and the stated detail, and nothing else: there is
            // no third part in which an amount or a unit could appear.
            Assert.Equal(prefix + unmeasured.Detail, rendered);

            // The part the type composes carries no digit at all, so no figure is rendered.
            Assert.DoesNotContain(prefix, char.IsDigit);

            someDetailCarriesADigit |= unmeasured.Detail.Any(char.IsDigit);
        }

        // Stated rather than implied: a detail does carry digits, and this is the exemption the
        // claim above draws the line around. A check that asserted no digit anywhere would fail
        // here, and narrowing the surface to make it pass would cost the reader the count and the
        // period that make an absence diagnosable.
        Assert.True(
            someDetailCarriesADigit,
            "no unmeasured detail carried a digit, so the exemption this check states is untested");
    }

    /// <summary>
    /// The unmeasured prefix is the boundary between what the type says and what the detail says,
    /// and it carries no digit for either reason. The check above rests on that.
    /// </summary>
    [Theory]
    [InlineData(UnmeasuredReason.NoObservationExists)]
    [InlineData(UnmeasuredReason.SourceCannotStateOne)]
    public void TheUnmeasuredPrefixCarriesNoDigitForEitherReason(UnmeasuredReason reason)
    {
        var prefix = MeasurementQuantity.UnmeasuredPrefix(reason);

        Assert.DoesNotContain(prefix, char.IsDigit);
        Assert.StartsWith("unmeasured (", prefix, StringComparison.Ordinal);
        Assert.EndsWith(": ", prefix, StringComparison.Ordinal);
    }
}
