using MediaCompany.Domain;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Domain.Tests;

/// <summary>
/// The representations the sustained-rate capability adds to the domain: the tier ratio's
/// required definition, the throughput reading's required statement, the lifecycle action names
/// declared once, and approval effort resolved per component at the one derivation site.
/// </summary>
public sealed class SustainedRateRepresentationTests
{
    private static readonly DateTimeOffset Presented = DateTimeOffset.Parse("2026-09-01T10:00:00Z");
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(2);

    private static ApprovalExerciseLabel Label() =>
        new("wave-5 component exercise", 1, "a constructed approval; it establishes the derivation rule and no series");

    // -----------------------------------------------------------------------
    // Construction invariants
    // -----------------------------------------------------------------------

    /// <summary>
    /// A tier ratio refuses to exist without its definition, exactly as it refuses to exist without
    /// its caveat and its assumption label, so no output can present the ratio without saying what
    /// it is.
    /// </summary>
    [Fact]
    public void ATierRatioRefusesABlankDefinition()
    {
        var caveat = SingleRecordCaveat.For(MeasurementQuantity.Count(0, "served-tier records"));
        var unmeasured = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, "none recorded");

        Assert.Throws<ArgumentException>(
            () => new TierRatioReadModel(new DateOnly(2026, 9, 1), unmeasured, caveat, "an assumption", "  "));

        var ratio = new TierRatioReadModel(new DateOnly(2026, 9, 1), unmeasured, caveat, "an assumption", "the agreement share");
        Assert.Equal("the agreement share", ratio.Definition);
    }

    /// <summary>
    /// A throughput reading refuses to exist without its statement of what its counts do and do
    /// not establish, so no count travels without saying it is not a rate.
    /// </summary>
    [Fact]
    public void AThroughputReadingRefusesABlankStatement()
    {
        var zero = MeasurementQuantity.Zero("claims");

        Assert.Throws<ArgumentException>(() => new ThroughputReadModel(
            Presented, Presented.AddHours(1), zero, zero, zero, zero, zero, " "));
    }

    /// <summary>
    /// The throughput reading carries exactly five quantities and a statement: no rate, no depth
    /// and no concurrency member exists on it to be filled.
    /// </summary>
    [Fact]
    public void AThroughputReadingDeclaresNoSizingMember()
    {
        var members = typeof(ThroughputReadModel).GetProperties().Select(p => p.Name).ToArray();

        Assert.Equal(
            new[] { "PeriodStart", "PeriodEnd", "Waiting", "Claimed", "Retried", "Escalated", "Completed", "Statement" },
            members);

        foreach (var sizing in new[] { "Rate", "Depth", "Concurrency", "Capacity", "Target", "Threshold" })
        {
            Assert.DoesNotContain(members, m => m.Contains(sizing, StringComparison.OrdinalIgnoreCase));
        }
    }

    // -----------------------------------------------------------------------
    // The lifecycle action names, declared once
    // -----------------------------------------------------------------------

    /// <summary>
    /// The six lifecycle actions are declared once, distinct, and in the lifecycle's own namespace
    /// of names, so a writer and the reader cannot drift apart on one of them.
    /// </summary>
    [Fact]
    public void TheLifecycleActionNamesAreDeclaredOnceAndDistinct()
    {
        Assert.Equal(6, LifecycleActions.All.Count);
        Assert.Equal(LifecycleActions.All.Count, LifecycleActions.All.Distinct(StringComparer.Ordinal).Count());
        Assert.All(LifecycleActions.All, a => Assert.StartsWith("job.", a, StringComparison.Ordinal));
        Assert.Contains(LifecycleActions.Completed, LifecycleActions.All);
    }

    // -----------------------------------------------------------------------
    // Approval effort, per component, at the one derivation site
    // -----------------------------------------------------------------------

    /// <summary>
    /// With the queued mark missing, ONLY the queue component is undeterminable, and it names the
    /// mark. Review and rework are derived from their own marks. The whole-row members keep their
    /// delivered meaning exactly: the row resolves unmeasured, all three intervals are null, and
    /// the missing-quantity statement is the delivered text.
    /// </summary>
    [Fact]
    public void AMissingQueuedMarkMakesOnlyTheQueueComponentUndeterminable()
    {
        var approval = new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved", Presented, Presented.AddMinutes(7));

        var measurement = ApprovalMeasurement.Derive(approval, null, Label());

        Assert.Equal(ApprovalComponentState.MarkAbsent, measurement.QueueComponent.State);
        Assert.Null(measurement.QueueComponent.Interval);
        Assert.Contains("no queued mark", measurement.QueueComponent.Statement, StringComparison.Ordinal);

        Assert.Equal(ApprovalComponentState.Derived, measurement.ReviewComponent.State);
        Assert.Equal(TimeSpan.FromMinutes(7), measurement.ReviewComponent.Interval);
        Assert.Equal(ApprovalComponentState.Derived, measurement.ReworkComponent.State);
        Assert.Equal(TimeSpan.Zero, measurement.ReworkComponent.Interval);

        // The delivered whole-row reading, unchanged.
        Assert.Equal(ApprovalMeasurementResolution.Unmeasured, measurement.Resolution);
        Assert.Null(measurement.ReviewTime);
        Assert.Null(measurement.QueueTime);
        Assert.Null(measurement.ReworkTime);
        Assert.Equal("queue time (no queued mark was recorded)", measurement.UnmeasuredReason);
    }

    /// <summary>
    /// With the rework predecessor missing, ONLY the rework component is undeterminable, and it
    /// names the mark; review and queue are derived.
    /// </summary>
    [Fact]
    public void AMissingReworkPredecessorMakesOnlyTheReworkComponentUndeterminable()
    {
        var approval = new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved on re-presentation", Presented, Presented.AddMinutes(4),
            queuedAt: Presented.AddMinutes(-3), reworkOf: Presented.AddDays(-1));

        var measurement = ApprovalMeasurement.Derive(approval, reworkPredecessor: null, Label());

        Assert.Equal(ApprovalComponentState.MarkAbsent, measurement.ReworkComponent.State);
        Assert.Contains("predecessor that was not supplied", measurement.ReworkComponent.Statement, StringComparison.Ordinal);
        Assert.Equal(TimeSpan.FromMinutes(4), measurement.ReviewComponent.Interval);
        Assert.Equal(TimeSpan.FromMinutes(3), measurement.QueueComponent.Interval);
        Assert.Equal(ApprovalMeasurementResolution.Unmeasured, measurement.Resolution);
    }

    /// <summary>
    /// A predecessor decided after this approval was presented makes the rework marks
    /// INCONSISTENT rather than absent, which is a different fact, and still touches no other
    /// component.
    /// </summary>
    [Fact]
    public void InconsistentReworkMarksAreDistinctFromAnAbsentMark()
    {
        var predecessor = new Approval(
            Item, new ItemVersion(1), "publication", WorkforceRole.Owner, ApprovalVerdict.SentBack,
            "sent back", Presented.AddHours(-1), Presented.AddMinutes(30));

        var approval = new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved", Presented, Presented.AddMinutes(2),
            queuedAt: Presented, reworkOf: predecessor.PresentedAt);

        var measurement = ApprovalMeasurement.Derive(approval, predecessor, Label());

        Assert.Equal(ApprovalComponentState.MarksInconsistent, measurement.ReworkComponent.State);
        Assert.Null(measurement.ReworkComponent.Interval);
        Assert.Equal(ApprovalComponentState.Derived, measurement.ReviewComponent.State);
        Assert.Equal(ApprovalComponentState.Derived, measurement.QueueComponent.State);
        Assert.Equal(TimeSpan.Zero, measurement.QueueComponent.Interval);
    }

    /// <summary>
    /// With every mark recorded, all three components are derived and agree with the delivered
    /// whole-row intervals, because both readings come from the same component results.
    /// </summary>
    [Fact]
    public void WithEveryMarkRecordedTheComponentsAgreeWithTheWholeRow()
    {
        var predecessor = new Approval(
            Item, new ItemVersion(1), "publication", WorkforceRole.Owner, ApprovalVerdict.SentBack,
            "sent back", Presented.AddHours(-3), Presented.AddHours(-2));

        var approval = new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved", Presented, Presented.AddMinutes(5),
            queuedAt: Presented.AddMinutes(-10), reworkOf: predecessor.PresentedAt);

        var measurement = ApprovalMeasurement.Derive(approval, predecessor, Label());

        Assert.Equal(ApprovalMeasurementResolution.Measured, measurement.Resolution);
        Assert.Equal(measurement.ReviewTime, measurement.ReviewComponent.Interval);
        Assert.Equal(measurement.QueueTime, measurement.QueueComponent.Interval);
        Assert.Equal(measurement.ReworkTime, measurement.ReworkComponent.Interval);
        Assert.Equal(TimeSpan.FromHours(2), measurement.ReworkComponent.Interval);
    }

    /// <summary>
    /// The REVIEW component's missing-mark case is shown as a REFUSED CONSTRUCTION, because both of
    /// its marks are mandatory: the presentation and decision instants are non-nullable parameters
    /// of the only constructor, so an approval without either cannot be expressed, and a decision
    /// recorded before its presentation is refused outright. There is therefore no approval whose
    /// review component could be missing a mark, and none is reported as one.
    /// </summary>
    [Fact]
    public void AnApprovalWithoutItsReviewMarksIsARefusedConstruction()
    {
        var constructor = Assert.Single(typeof(Approval).GetConstructors());
        var parameters = constructor.GetParameters().ToDictionary(p => p.Name!, p => p.ParameterType);

        Assert.Equal(typeof(DateTimeOffset), parameters["presentedAt"]);
        Assert.Equal(typeof(DateTimeOffset), parameters["decidedAt"]);
        Assert.Equal(typeof(DateTimeOffset?), parameters["queuedAt"]);

        Assert.Throws<ArgumentException>(() => new Approval(
            Item, Version, "publication", WorkforceRole.Owner, ApprovalVerdict.Approved,
            "approved", Presented, Presented.AddMinutes(-1)));
    }
}
