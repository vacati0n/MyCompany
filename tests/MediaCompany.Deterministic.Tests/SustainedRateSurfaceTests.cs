using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Reporting;
using MediaCompany.Domain;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The composing sites the sustained-rate capability adds, each exercised in every case it
/// decides: the tier ratio over records carrying both tiers, the throughput coverage rule, the
/// four record-source readings, and approval effort resolved per component.
/// </summary>
public sealed class SustainedRateSurfaceTests
{
    private static readonly DateOnly Period = new(2026, 9, 1);
    private static readonly DateTimeOffset Start = DateTimeOffset.Parse("2026-09-01T00:00:00Z");
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(3);

    private static ServedTierRecord Record(ReasoningTier? requested, ReasoningTier? served) => new()
    {
        Operation = OperationId.New(),
        Period = Period,
        Requested = requested,
        Served = served,
    };

    // -----------------------------------------------------------------------
    // The tier ratio
    // -----------------------------------------------------------------------

    /// <summary>
    /// The corrected denominator. A record carrying a served tier and NO requested tier has
    /// nothing to agree or disagree with, so it no longer counts as a mismatch: over one agreeing
    /// record, one disagreeing record and one with no requested tier, the share is half, not a third.
    /// </summary>
    [Fact]
    public void ARecordWithNoRequestedTierIsNotCountedAsAMismatch()
    {
        var ratio = AnalyticsComposers.TierRatio(Period,
        [
            Record(ReasoningTier.Deep, ReasoningTier.Deep),
            Record(ReasoningTier.Deep, ReasoningTier.Light),
            Record(null, ReasoningTier.Light),
        ]);

        Assert.Equal("50 percent", ratio.Ratio.Describe());
        Assert.Equal(AnalyticsComposers.TierRatioDefinition, ratio.Definition);
    }

    /// <summary>
    /// Two served-tier records of which only one also carries a requested tier present NO ratio:
    /// the rule counts records carrying both, and the definition and the assumption label travel
    /// with the unmeasured output as they do with a measured one.
    /// </summary>
    [Fact]
    public void FewerThanTwoRecordsCarryingBothTiersPresentNoRatio()
    {
        var ratio = AnalyticsComposers.TierRatio(Period,
        [
            Record(ReasoningTier.Deep, ReasoningTier.Deep),
            Record(null, ReasoningTier.Light),
        ]);

        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(ratio.Ratio);
        Assert.Contains("(1 recorded)", unmeasured.Detail, StringComparison.Ordinal);
        Assert.Equal("2 served-tier records", ratio.Caveat.ServedTierRecords.Describe());
        Assert.Equal(AnalyticsComposers.TierRatioDefinition, ratio.Definition);
        Assert.Contains("not been measured", ratio.AssumptionLabel, StringComparison.Ordinal);
    }

    /// <summary>
    /// The definition says what the ratio is and what it is not, and it is the same text on every
    /// ratio: it names the agreement share and disclaims being the split the cost model assumes.
    /// </summary>
    [Fact]
    public void TheRatioDefinitionNamesTheAgreementShareAndDisclaimsTheAssumedSplit()
    {
        Assert.Contains("agreement share", AnalyticsComposers.TierRatioDefinition, StringComparison.Ordinal);
        Assert.Contains("both a requested and a served", AnalyticsComposers.TierRatioDefinition, StringComparison.Ordinal);
        Assert.Contains("not the reasoning-tier split", AnalyticsComposers.TierRatioDefinition, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // Throughput: the coverage rule at its one composing site
    // -----------------------------------------------------------------------

    private static ThroughputSummary Summary(
        DateTimeOffset start,
        DateTimeOffset end,
        DateTimeOffset? earliest,
        DateTimeOffset horizon,
        long enqueued = 0,
        long succeeded = 0,
        long retriedToClose = 0,
        long claimedToClose = 0,
        long claimed = 0,
        long retried = 0,
        long escalated = 0,
        long completed = 0) =>
        new(start, end, earliest, Quiet: true, horizon, enqueued, succeeded, retriedToClose, claimedToClose,
            claimed, retried, escalated, completed);

    /// <summary>
    /// An elapsed period lying wholly after the record's first entry and holding no transition is
    /// an OBSERVED ZERO in every quantity; the same counts over a period that began before the
    /// first entry are UNMEASURED, naming the coverage bounds. The two render differently.
    /// </summary>
    [Fact]
    public void AnObservedZeroPeriodAndAnUnmeasuredPeriodRenderApart()
    {
        var earliest = Start;
        var horizon = Start.AddDays(10);

        var observed = AnalyticsComposers.Throughput(Summary(Start.AddDays(2), Start.AddDays(3), earliest, horizon));
        var before = AnalyticsComposers.Throughput(Summary(Start.AddDays(-1), Start.AddDays(3), earliest, horizon));

        foreach (var quantity in new[] { observed.Waiting, observed.Claimed, observed.Retried, observed.Escalated, observed.Completed })
        {
            Assert.IsType<MeasurementQuantity.ObservedZero>(quantity);
        }

        foreach (var quantity in new[] { before.Waiting, before.Claimed, before.Retried, before.Escalated, before.Completed })
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
            Assert.Equal(UnmeasuredReason.NoObservationExists, unmeasured.Reason);
            Assert.Contains("begins before the append-only record's earliest entry", unmeasured.Detail, StringComparison.Ordinal);
        }

        Assert.NotEqual(observed.Claimed.Describe(), before.Claimed.Describe());
    }

    /// <summary>A period not yet elapsed when the store was read is unmeasured, and says so.</summary>
    [Fact]
    public void APeriodNotYetElapsedIsUnmeasured()
    {
        var reading = AnalyticsComposers.Throughput(
            Summary(Start.AddDays(1), Start.AddDays(5), Start, horizon: Start.AddDays(4), claimed: 3, claimedToClose: 3, enqueued: 3));

        var claimed = Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Claimed);
        Assert.Contains("ends after the record horizon", claimed.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// A read that never found a quiet instant to close the record takes no count: every quantity
    /// reads unmeasured because the source cannot state one, naming the in-flight transitions,
    /// whatever its period.
    /// </summary>
    [Fact]
    public void AReadThatFoundTransitionsInFlightReadsUnmeasuredThroughout()
    {
        var reading = AnalyticsComposers.Throughput(new ThroughputSummary(
            Start.AddDays(1), Start.AddDays(2), Start, Quiet: false, Horizon: null, 0, 0, 0, 0, 0, 0, 0, 0));

        foreach (var quantity in new[] { reading.Waiting, reading.Claimed, reading.Retried, reading.Escalated, reading.Completed })
        {
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
            Assert.Equal(UnmeasuredReason.SourceCannotStateOne, unmeasured.Reason);
            Assert.Contains("in flight", unmeasured.Detail, StringComparison.Ordinal);
        }
    }

    /// <summary>A record holding no entry at all measures no period.</summary>
    [Fact]
    public void AnEmptyRecordMeasuresNoPeriod()
    {
        var reading = AnalyticsComposers.Throughput(Summary(Start, Start.AddDays(1), earliest: null, Start.AddDays(2)));

        var waiting = Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Waiting);
        Assert.Contains("holds no entry", waiting.Detail, StringComparison.Ordinal);
    }

    /// <summary>
    /// The counts of a covered period, and the waiting count reconstructed at its close as the
    /// arrivals into the ready queue less the claims out of it. The unit counts here are
    /// DEMONSTRATION PARAMETERS chosen to exercise the arithmetic; they are no observation of the
    /// company's work and no threshold is derived from them.
    /// </summary>
    [Fact]
    public void ACoveredPeriodReportsItsCountsAndTheWaitingUnitsAtItsClose()
    {
        var reading = AnalyticsComposers.Throughput(Summary(
            Start.AddDays(1), Start.AddDays(2), Start, Start.AddDays(3),
            enqueued: 4, succeeded: 6, retriedToClose: 1, claimedToClose: 9,
            claimed: 5, retried: 1, escalated: 1, completed: 2));

        Assert.Equal("2 units waiting", reading.Waiting.Describe());
        Assert.Equal("5 claims", reading.Claimed.Describe());
        Assert.Equal("1 retried stage attempts", reading.Retried.Describe());
        Assert.Equal("1 escalated units", reading.Escalated.Describe());
        Assert.Equal("2 completed units", reading.Completed.Describe());
    }

    /// <summary>
    /// More claims than arrivals means a claim state was changed outside the services: the waiting
    /// count cannot be reconstructed and reads unmeasured because the source cannot state one,
    /// rather than as a negative number or a clamped zero.
    /// </summary>
    [Fact]
    public void AWaitingCountBelowZeroIsUnmeasured()
    {
        var reading = AnalyticsComposers.Throughput(Summary(
            Start.AddDays(1), Start.AddDays(2), Start, Start.AddDays(3), enqueued: 1, claimedToClose: 2, claimed: 2));

        var waiting = Assert.IsType<MeasurementQuantity.Unmeasured>(reading.Waiting);
        Assert.Equal(UnmeasuredReason.SourceCannotStateOne, waiting.Reason);
        Assert.Equal("2 claims", reading.Claimed.Describe());
    }

    /// <summary>
    /// Every reading states that its counts establish no rate, no depth and no concurrency figure,
    /// and names a demonstration store's counts as demonstration parameters.
    /// </summary>
    [Fact]
    public void EveryThroughputReadingStatesWhatItDoesNotEstablish()
    {
        var reading = AnalyticsComposers.Throughput(Summary(Start.AddDays(1), Start.AddDays(2), Start, Start.AddDays(3)));

        Assert.Contains("no sustainable rate", reading.Statement, StringComparison.Ordinal);
        Assert.Contains("no required buffer depth", reading.Statement, StringComparison.Ordinal);
        Assert.Contains("no concurrency figure", reading.Statement, StringComparison.Ordinal);
        Assert.Contains("demonstration parameter", reading.Statement, StringComparison.Ordinal);
    }

    /// <summary>A period that does not close after it starts is refused rather than read.</summary>
    [Fact]
    public void AnEmptyOrInvertedPeriodIsRefused()
    {
        Assert.Throws<ArgumentException>(() => AnalyticsComposers.Throughput(
            Summary(Start.AddDays(2), Start.AddDays(2), Start, Start.AddDays(3))));
    }

    // -----------------------------------------------------------------------
    // The four record sources
    // -----------------------------------------------------------------------

    /// <summary>
    /// With no dossier opened every component reads unmeasured naming the register and the item
    /// version; with one opened and empty every component is an observed zero. The two render
    /// differently, and neither carries a placeholder.
    /// </summary>
    [Fact]
    public void ADossierReadsUnmeasuredWhereNoneIsOpenedAndZeroWhereOneIsEmpty()
    {
        var none = AnalyticsComposers.DossierCounts(Item, Version, recorded: null);
        var empty = AnalyticsComposers.DossierCounts(
            Item, Version, new RecordedDossier(new ItemDossier { Item = Item, Version = Version }, ChannelId.New(), Start));

        var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(none.Stages);
        Assert.Contains("item dossier register", unmeasured.Detail, StringComparison.Ordinal);
        Assert.Contains($"item {Item} version {Version}", unmeasured.Detail, StringComparison.Ordinal);

        foreach (var quantity in Quantities(none))
        {
            Assert.IsType<MeasurementQuantity.Unmeasured>(quantity);
        }

        foreach (var quantity in Quantities(empty))
        {
            Assert.IsType<MeasurementQuantity.ObservedZero>(quantity);
        }

        Assert.NotEqual(none.SupplyAuditEntries.Describe(), empty.SupplyAuditEntries.Describe());
        Assert.Contains("none of them is a clip count", empty.Statement, StringComparison.Ordinal);
    }

    private static IEnumerable<MeasurementQuantity> Quantities(ItemDossierReadModel model) =>
    [
        model.Stages, model.TreatmentVerdicts, model.AudienceDesignations, model.Visuals,
        model.ClipOriginAssessments, model.ClaimAttributions, model.SupplyAuditEntries,
        model.DeterminationResolutions, model.RuntimeRecords, model.MetadataRecords, model.OriginalityAssessments,
    ];

    /// <summary>
    /// With no dossier opened the stage surface lists every stage of the closed set as unmeasured
    /// and lists NO refusal, because no predicate is evaluated over a dossier that does not exist.
    /// </summary>
    [Fact]
    public void WithNoDossierEveryStageIsUnmeasuredAndNoRefusalIsListed()
    {
        var surface = AnalyticsComposers.StageOutcomesUnrecorded(Item, Version);

        Assert.Equal(ProductionStageSet.Count, surface.Stages.Count);
        Assert.All(surface.Stages, s =>
        {
            Assert.Null(s.RecordedOutcome);
            var unmeasured = Assert.IsType<MeasurementQuantity.Unmeasured>(s.RecordedOutcomes);
            Assert.Contains("item dossier register", unmeasured.Detail, StringComparison.Ordinal);
        });
        Assert.Empty(surface.Refusals);
    }

    /// <summary>
    /// The supply-audit reading: with no dossier the entry count is unmeasured and no entry is
    /// listed; with an opened dossier holding none it is an observed zero; and each recorded entry
    /// keeps the case its count was recorded in, a zero as a zero and an absent count as no number.
    /// </summary>
    [Fact]
    public void TheSupplyAuditReadingKeepsEachEntryInTheCaseItWasRecordedIn()
    {
        var none = AnalyticsComposers.SupplyAuditRecorded(Item, Version, entries: null);
        Assert.IsType<MeasurementQuantity.Unmeasured>(none.RecordedEntries);
        Assert.Empty(none.Entries);

        var empty = AnalyticsComposers.SupplyAuditRecorded(Item, Version, []);
        Assert.IsType<MeasurementQuantity.ObservedZero>(empty.RecordedEntries);

        var recorded = AnalyticsComposers.SupplyAuditRecorded(Item, Version,
        [
            new SupplyAuditEntry
            {
                RequestedTerm = "fixture-term-literal-none",
                Library = "library-one",
                Fidelity = TermFidelity.LiteralSingleToken,
                AuditedAt = Start,
                Count = 0,
            },
            new SupplyAuditEntry
            {
                RequestedTerm = "fixture-term-unanswered",
                Library = "library-one",
                Fidelity = TermFidelity.Substituted,
                AuditedAt = Start,
                UnobtainedReason = CountUnobtainedReason.TermNotAnsweredLiterally,
                WhatWouldObtainIt = "a literal query",
            },
        ]);

        Assert.Equal("2 supply audit entries", recorded.RecordedEntries.Describe());
        Assert.IsType<MeasurementQuantity.ObservedZero>(recorded.Entries[0].ClipCount);
        Assert.IsType<MeasurementQuantity.Unmeasured>(recorded.Entries[1].ClipCount);
        Assert.Equal("fixture-term-unanswered", recorded.Entries[1].Subject);
    }

    /// <summary>
    /// The determination reading enumerates the closed set whatever was recorded; with no dossier
    /// the resolution count is unmeasured, and a recorded outcome is reported as recorded.
    /// </summary>
    [Fact]
    public void TheDeterminationReadingEnumeratesTheClosedSet()
    {
        var none = AnalyticsComposers.DeterminationsRecorded(Item, Version, resolutions: null);
        Assert.IsType<MeasurementQuantity.Unmeasured>(none.RecordedResolutions);
        Assert.Equal(5, none.Determinations.Count);

        var recorded = AnalyticsComposers.DeterminationsRecorded(Item, Version,
        [
            new DeterminationResolution
            {
                Determination = ComplianceDetermination.DuplicateDetection,
                Outcome = DeterminationOutcome.Evidenced,
                ResolvedAt = Start,
                Evidence = "no duplicate at the recorded distance",
            },
        ]);

        Assert.Equal("1 determination resolutions", recorded.RecordedResolutions.Describe());
        Assert.Equal(
            DeterminationOutcome.Evidenced,
            recorded.Determinations.Single(d => d.Determination == ComplianceDetermination.DuplicateDetection).RecordedOutcome);
        Assert.All(
            recorded.Determinations.Where(d => d.Determination != ComplianceDetermination.DuplicateDetection),
            d => Assert.IsType<MeasurementQuantity.Unmeasured>(d.RecordedResolutions));
    }

    // -----------------------------------------------------------------------
    // Approval effort, each component from its own marks
    // -----------------------------------------------------------------------

    private static ApprovalExerciseLabel Label() =>
        new("wave-5 component exercise", 1, "a constructed approval; it establishes the mapping and no series");

    /// <summary>
    /// Each of the three marks missing in turn. The queued mark missing makes ONLY queue
    /// unmeasured, naming the mark; the rework predecessor missing makes ONLY rework unmeasured;
    /// the review marks cannot be missing, which the domain demonstrates as a refused construction.
    /// The components not derived from the missing mark report what was recorded.
    /// </summary>
    [Fact]
    public void EachMissingMarkMakesOnlyItsOwnComponentUnmeasured()
    {
        var presented = Start.AddHours(9);

        var noQueueMark = AnalyticsComposers.ApprovalEffort(ApprovalMeasurement.Derive(
            new Approval(Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
                "approved", presented, presented.AddMinutes(6)),
            null, Label()));

        var queue = Assert.IsType<MeasurementQuantity.Unmeasured>(noQueueMark.Queue);
        Assert.Equal(UnmeasuredReason.NoObservationExists, queue.Reason);
        Assert.Contains("no queued mark", queue.Detail, StringComparison.Ordinal);
        Assert.Equal("6 minutes", noQueueMark.Review.Describe());
        Assert.IsType<MeasurementQuantity.ObservedZero>(noQueueMark.Rework);

        var noPredecessor = AnalyticsComposers.ApprovalEffort(ApprovalMeasurement.Derive(
            new Approval(Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
                "approved", presented, presented.AddMinutes(6), queuedAt: presented.AddMinutes(-4),
                reworkOf: presented.AddDays(-1)),
            null, Label()));

        var rework = Assert.IsType<MeasurementQuantity.Unmeasured>(noPredecessor.Rework);
        Assert.Contains("predecessor that was not supplied", rework.Detail, StringComparison.Ordinal);
        Assert.Equal("6 minutes", noPredecessor.Review.Describe());
        Assert.Equal("4 minutes", noPredecessor.Queue.Describe());
    }

    /// <summary>
    /// Inconsistent marks read unmeasured because the source cannot state the interval, which is a
    /// different closed reason from an absent mark.
    /// </summary>
    [Fact]
    public void InconsistentMarksReadUnmeasuredBecauseTheSourceCannotStateOne()
    {
        var presented = Start.AddHours(9);
        var predecessor = new Approval(Item, new ItemVersion(2), "publication", WorkforceRole.Owner,
            ApprovalVerdict.SentBack, "sent back", presented.AddHours(-1), presented.AddHours(1));

        var model = AnalyticsComposers.ApprovalEffort(ApprovalMeasurement.Derive(
            new Approval(Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
                "approved", presented, presented.AddMinutes(1), queuedAt: presented,
                reworkOf: predecessor.PresentedAt),
            predecessor, Label()));

        var rework = Assert.IsType<MeasurementQuantity.Unmeasured>(model.Rework);
        Assert.Equal(UnmeasuredReason.SourceCannotStateOne, rework.Reason);
        Assert.IsType<MeasurementQuantity.ObservedZero>(model.Queue);
        Assert.Equal("1 minutes", model.Review.Describe());
    }

    /// <summary>
    /// No component is derived from another and no combined figure exists: the read model carries
    /// exactly the three components and the label, and no threshold, target or pass line appears.
    /// </summary>
    [Fact]
    public void TheApprovalReadingCarriesNoCombinedFigureAndNoThreshold()
    {
        var members = typeof(ApprovalEffortReadModel).GetProperties().Select(p => p.Name).Order().ToArray();
        Assert.Equal(new[] { "Item", "Label", "Queue", "Review", "Rework", "Version" }, members);

        var presented = Start.AddHours(9);
        var model = AnalyticsComposers.ApprovalEffort(ApprovalMeasurement.Derive(
            new Approval(Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
                "approved", presented, presented.AddMinutes(6)),
            null, Label()));

        foreach (var word in new[] { "threshold", "target", "pass line", "should be", "acceptable" })
        {
            Assert.DoesNotContain(word, model.Review.Describe(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, model.Queue.Describe(), StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(word, model.Rework.Describe(), StringComparison.OrdinalIgnoreCase);
        }
    }

    // -----------------------------------------------------------------------
    // The deferred rate measures
    // -----------------------------------------------------------------------

    /// <summary>
    /// Cycle time, failure rate and rework rate are DEFERRED measures carrying no value field and
    /// naming the production series each waits on, and no sizing quantity is declared anywhere in
    /// the catalogue, deferred or measurable.
    /// </summary>
    [Fact]
    public void TheRateMeasuresAreDeferredAndNoSizingQuantityIsDeclared()
    {
        foreach (var name in new[] { "cycle-time", "failure-rate", "rework-rate" })
        {
            var deferred = Assert.Single(MeasureCatalogue.Deferred, d => d.Name == name);
            Assert.Contains("production series", deferred.AwaitingParameter, StringComparison.Ordinal);
            Assert.DoesNotContain(MeasureCatalogue.MeasurableNow, m => m.Name == name);
        }

        foreach (var sizing in new[] { "buffer", "depth", "concurrency", "sustainable", "target-rate", "pass-line" })
        {
            Assert.DoesNotContain(MeasureCatalogue.All, m => m.Name.Contains(sizing, StringComparison.OrdinalIgnoreCase));
        }
    }
}
