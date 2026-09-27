using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Publication;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The publishing capability, built and exercised short of upload.
///
/// Every test here exercises the machinery to its own edge and asserts what it CANNOT do. The
/// wave builds the complete path and stops immediately before the effecting step, so the
/// demonstrations are mostly demonstrations of absence: a refusal naming a structural reason, a
/// union with no case to return, a position set with no successor.
/// </summary>
public sealed class FirstPublicationConditionTests
{
    private static readonly DateOnly Observed = new(2026, 9, 27);

    private static ConditionObservation At(FirstPublicationCondition condition, ConditionState state) =>
        new(condition, state, $"observation of {condition}", Observed);

    /// <summary>
    /// The register is TOTAL over the closed three-member set. An empty register yields three
    /// standings, not zero, so a missing observation is something that refuses rather than nothing
    /// to check.
    /// </summary>
    [Fact]
    public void AnEmptyRegisterStillEvaluatesAllThreeConditions()
    {
        var standings = FirstPublicationConditionRegister.Empty.Evaluate();

        Assert.Equal(3, standings.Count);
        Assert.All(standings, s => Assert.Equal(ConditionResolution.Absent, s.Resolution));
        Assert.All(standings, s => Assert.False(s.IsSatisfied));
    }

    /// <summary>An absent record folds to not satisfied, and says so rather than saying nothing.</summary>
    [Fact]
    public void AnAbsentObservationFoldsToNotSatisfiedAndNamesItselfAbsent()
    {
        var standing = FirstPublicationConditionRegister.Empty.Evaluate()
            .Single(s => s.Condition == FirstPublicationCondition.PaymentAccount);

        Assert.False(standing.IsSatisfied);
        Assert.Contains("NO observation is recorded", standing.Describe());
        Assert.Contains("never read as nothing to check", standing.Describe());
    }

    /// <summary>
    /// Each of the three, set to UNKNOWN in turn: it resolves as not satisfied, and the refusal
    /// still says the state was unknown rather than false.
    /// </summary>
    [Theory]
    [InlineData(FirstPublicationCondition.LibraryRegistration)]
    [InlineData(FirstPublicationCondition.PaymentAccount)]
    [InlineData(FirstPublicationCondition.TwoStepVerification)]
    public void AnUnknownConditionResolvesAsNotSatisfiedAndStaysVisibleAsUnknown(
        FirstPublicationCondition condition)
    {
        var register = new FirstPublicationConditionRegister([At(condition, ConditionState.Unknown)]);
        var standing = register.Evaluate().Single(s => s.Condition == condition);

        Assert.Equal(ConditionResolution.Unknown, standing.Resolution);
        Assert.False(standing.IsSatisfied);
        Assert.Contains("UNKNOWN", standing.Describe());
        Assert.Contains("NOT satisfied", standing.Describe());
    }

    /// <summary>
    /// Each of the three, set to FALSE in turn. It refuses like the unknown case, and the refusal
    /// distinguishes the two: they are discharged by different acts, so a reader must be able to
    /// tell them apart.
    /// </summary>
    [Theory]
    [InlineData(FirstPublicationCondition.LibraryRegistration)]
    [InlineData(FirstPublicationCondition.PaymentAccount)]
    [InlineData(FirstPublicationCondition.TwoStepVerification)]
    public void AFalseConditionRefusesAndIsDistinguishableFromAnUnknownOne(
        FirstPublicationCondition condition)
    {
        var no = new FirstPublicationConditionRegister([At(condition, ConditionState.NotSatisfied)])
            .Evaluate().Single(s => s.Condition == condition);
        var unknown = new FirstPublicationConditionRegister([At(condition, ConditionState.Unknown)])
            .Evaluate().Single(s => s.Condition == condition);

        Assert.False(no.IsSatisfied);
        Assert.False(unknown.IsSatisfied);
        Assert.NotEqual(no.Resolution, unknown.Resolution);
        Assert.NotEqual(no.Describe(), unknown.Describe());
        Assert.Contains("observed false", no.Describe());
    }

    /// <summary>Only an observation recording satisfied satisfies. All three, or the set refuses.</summary>
    [Fact]
    public void AllThreeMustBeSatisfiedForTheRegisterToHold()
    {
        var twoOfThree = new FirstPublicationConditionRegister(
        [
            At(FirstPublicationCondition.LibraryRegistration, ConditionState.Satisfied),
            At(FirstPublicationCondition.PaymentAccount, ConditionState.Satisfied),
            At(FirstPublicationCondition.TwoStepVerification, ConditionState.Unknown),
        ]);

        Assert.False(twoOfThree.AllSatisfied());
        Assert.Single(twoOfThree.Unsatisfied());
        Assert.Equal(
            FirstPublicationCondition.TwoStepVerification,
            twoOfThree.Unsatisfied()[0].Condition);
    }

    /// <summary>
    /// The real recorded position, as the supplied ticket states it: library registration is NOT
    /// DONE, the payment account DOES NOT EXIST, and two-step verification is UNCONFIRMED rather
    /// than false. All three refuse, and none of them is discharged by this change.
    /// </summary>
    [Fact]
    public void TheRealRecordedStateRefusesAllThreeConditions()
    {
        var register = new FirstPublicationConditionRegister(
        [
            new ConditionObservation(
                FirstPublicationCondition.LibraryRegistration,
                ConditionState.NotSatisfied,
                "the channel is not registered on any music or stock library",
                Observed),
            new ConditionObservation(
                FirstPublicationCondition.PaymentAccount,
                ConditionState.NotSatisfied,
                "the payee position is settled but the payment account does not yet exist",
                Observed),
            new ConditionObservation(
                FirstPublicationCondition.TwoStepVerification,
                ConditionState.Unknown,
                "two-step verification is unconfirmed; no strike stands",
                Observed),
        ]);

        Assert.False(register.AllSatisfied());
        Assert.Equal(3, register.Unsatisfied().Count);

        // The unconfirmed one is recorded UNKNOWN, not false: nobody observed it to be off.
        var twoStep = register.Evaluate()
            .Single(s => s.Condition == FirstPublicationCondition.TwoStepVerification);
        Assert.Equal(ConditionResolution.Unknown, twoStep.Resolution);
    }

    /// <summary>An observation with no evidence is refused at construction.</summary>
    [Fact]
    public void AnUnevidencedObservationIsNotConstructible()
    {
        Assert.Throws<ArgumentException>(() => new ConditionObservation(
            FirstPublicationCondition.PaymentAccount, ConditionState.Satisfied, "   ", Observed));
    }

    /// <summary>The gate refuses by the condition's own name, and names all three in the detail.</summary>
    [Fact]
    public void TheGateRefusesNamingTheConditionThatFailed()
    {
        var item = ItemId.New();
        var version = new ItemVersion(3);
        var now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

        var approval = new Approval(
            item, version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
            ApprovalVerdict.Approved, "approved", now.AddMinutes(-30), now.AddMinutes(-10));

        var verdict = GatePredicates.EvaluatePublish(
            item, version, MediaCompany.Domain.Publication.GateState.Approved,
            WorkforceRole.Publisher, [], [approval], rightsPreconditionMet: true, now,
            new FirstPublicationConditionRegister(
                [At(FirstPublicationCondition.PaymentAccount, ConditionState.NotSatisfied)]));

        var refused = Assert.IsType<GateVerdict.Refused>(verdict);

        // The first unsatisfied condition names the refusal; the detail carries all three, so a
        // reader repairing one sees the rest rather than discovering them one at a time.
        Assert.Equal(GateRefusal.FirstPublicationLibraryRegistrationUnmet, refused.Reason);
        Assert.Contains("LibraryRegistration", refused.Detail);
        Assert.Contains("PaymentAccount", refused.Detail);
        Assert.Contains("TwoStepVerification", refused.Detail);
    }

    /// <summary>The refusal for each condition is its own named member of the closed refusal set.</summary>
    [Fact]
    public void EachConditionHasItsOwnNamedRefusal()
    {
        var refusals = FirstPublicationConditionRegister.All
            .Select(GatePredicates.RefusalFor)
            .ToArray();

        Assert.Equal(3, refusals.Distinct().Count());
        Assert.All(refusals, r => Assert.Contains(r, Enum.GetValues<GateRefusal>()));
    }
}

/// <summary>Owner approval, and the three ways it cannot be bypassed.</summary>
public sealed class OwnerApprovalTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(4);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

    private static FirstPublicationConditionRegister Satisfied =>
        new(Enum.GetValues<FirstPublicationCondition>().Select(c =>
            new ConditionObservation(c, ConditionState.Satisfied, "fixture", new DateOnly(2026, 9, 27))));

    /// <summary>
    /// Owner approval is absent from EVERY configuration surface. There is no key by which the
    /// step can be expressed, defaulted, disabled or removed, on any of the three key sets.
    /// </summary>
    [Fact]
    public void NoConfigurationKeyReachesTheOwnerApprovalStep()
    {
        var everyAdmittedKey = MediaCompany.Domain.Configuration.ConfigurationKeys.Admitted
            .Concat(ProductionConfigurationKeys.Admitted)
            .Concat(MediaCompany.Domain.Configuration.PublishingConfigurationKeys.Admitted)
            .ToArray();

        Assert.All(everyAdmittedKey, key =>
        {
            Assert.DoesNotContain("approval", key, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("gate", key, StringComparison.OrdinalIgnoreCase);
        });

        // And a key that tried to name it would be inadmissible rather than merely absent.
        Assert.True(ProductionConfigurationKeys.ReachesAControl("production.owner-approval"));
        Assert.True(MediaCompany.Domain.Configuration.PublishingConfigurationKeys
            .ReachesAControl("publishing.owner-approval"));
    }

    /// <summary>A dispatch carrying no owner approval is refused by name.</summary>
    [Fact]
    public void ADispatchWithNoOwnerApprovalIsRefused()
    {
        var verdict = GatePredicates.EvaluatePublish(
            Item, Version, MediaCompany.Domain.Publication.GateState.Approved,
            WorkforceRole.Publisher, [], [], rightsPreconditionMet: true, Now, Satisfied);

        var refused = Assert.IsType<GateVerdict.Refused>(verdict);
        Assert.Equal(GateRefusal.ApprovalSetIncomplete, refused.Reason);
    }

    /// <summary>
    /// A retry presented after the version changed is refused AT DISPATCH TIME by its own name, so
    /// it cannot ride the approval the earlier version carried.
    /// </summary>
    [Fact]
    public void ARetryAfterTheVersionChangedCannotRideTheEarlierApproval()
    {
        var token = new GatePassToken(Item, new ItemVersion(4), GatePredicates.OwnerApprovalGate, Now);

        var drift = GatePredicates.RefuseOnVersionDrift(token, Item, new ItemVersion(5));

        Assert.NotNull(drift);
        Assert.Equal(GateRefusal.DispatchVersionChanged, drift!.Reason);
        Assert.Contains("does not carry over", drift.Detail);
    }

    /// <summary>The same version passes the dispatch-time re-evaluation, so the check is not a blanket refusal.</summary>
    [Fact]
    public void TheSameVersionPassesTheDispatchTimeReEvaluation()
    {
        var token = new GatePassToken(Item, Version, GatePredicates.OwnerApprovalGate, Now);
        Assert.Null(GatePredicates.RefuseOnVersionDrift(token, Item, Version));
    }
}

/// <summary>The three separated approval quantities, and the unmeasured resolution.</summary>
public sealed class ApprovalMeasurementTests
{
    private static readonly ItemId Item = ItemId.New();
    private static readonly ItemVersion Version = new(2);
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

    private static ApprovalExerciseLabel Label => new(
        "approval-surface exercise",
        setSize: 1,
        classLimits:
        "taken on a held item against a built surface; it establishes that the instrument works "
        + "and is not the real approval series");

    private static Approval Approval(DateTimeOffset? queuedAt, DateTimeOffset? reworkOf = null) => new(
        Item, Version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
        ApprovalVerdict.Approved, "approved",
        Now.AddMinutes(-10), Now, queuedAt, reworkOf);

    /// <summary>Review, queue and rework are three distinct values, never one elapsed figure.</summary>
    [Fact]
    public void TheThreeQuantitiesAreRecordedSeparately()
    {
        var measurement = ApprovalMeasurement.Derive(
            Approval(queuedAt: Now.AddMinutes(-25)), reworkPredecessor: null, Label);

        Assert.Equal(ApprovalMeasurementResolution.Measured, measurement.Resolution);
        Assert.Equal(TimeSpan.FromMinutes(10), measurement.ReviewTime);
        Assert.Equal(TimeSpan.FromMinutes(15), measurement.QueueTime);
        Assert.Equal(TimeSpan.Zero, measurement.ReworkTime);

        // The three are not the same number, so nothing downstream can mistake one for another.
        Assert.NotEqual(measurement.ReviewTime, measurement.QueueTime);
    }

    /// <summary>
    /// An approval missing the queued mark resolves as UNMEASURED, not as a zero queue time.
    ///
    /// This is the distinction the whole instrument exists for. A zero would understate the cost of
    /// per-publication approval and argue for relaxing it on a figure that was never taken.
    /// </summary>
    [Fact]
    public void AnApprovalMissingTheQueuedMarkIsUnmeasuredRatherThanZero()
    {
        var measurement = ApprovalMeasurement.Derive(
            Approval(queuedAt: null), reworkPredecessor: null, Label);

        Assert.Equal(ApprovalMeasurementResolution.Unmeasured, measurement.Resolution);
        Assert.Null(measurement.QueueTime);
        Assert.NotEqual(TimeSpan.Zero, measurement.QueueTime ?? TimeSpan.FromTicks(1));
        Assert.Contains("queue time", measurement.UnmeasuredReason);
    }

    /// <summary>A genuine zero wait is recorded as zero, which is a different fact from unmeasured.</summary>
    [Fact]
    public void AZeroQueueTimeIsRecordedAsZeroRatherThanOmitted()
    {
        var presented = Now.AddMinutes(-10);
        var measurement = ApprovalMeasurement.Derive(
            Approval(queuedAt: presented), reworkPredecessor: null, Label);

        Assert.Equal(ApprovalMeasurementResolution.Measured, measurement.Resolution);
        Assert.Equal(TimeSpan.Zero, measurement.QueueTime);
        Assert.NotNull(measurement.QueueTime);
    }

    /// <summary>Rework is derived from the predecessor the rework link names.</summary>
    [Fact]
    public void ReworkTimeIsDerivedFromTheLinkedPredecessor()
    {
        var predecessorPresented = Now.AddMinutes(-120);
        var predecessor = new Approval(
            Item, Version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
            ApprovalVerdict.SentBack, "sent back", predecessorPresented, Now.AddMinutes(-100));

        var measurement = ApprovalMeasurement.Derive(
            Approval(queuedAt: Now.AddMinutes(-25), reworkOf: predecessorPresented),
            predecessor,
            Label);

        Assert.Equal(ApprovalMeasurementResolution.Measured, measurement.Resolution);
        Assert.Equal(TimeSpan.FromMinutes(90), measurement.ReworkTime);
    }

    /// <summary>A rework link whose predecessor is missing is unmeasured, not zero rework.</summary>
    [Fact]
    public void AMissingReworkPredecessorYieldsUnmeasured()
    {
        var measurement = ApprovalMeasurement.Derive(
            Approval(queuedAt: Now.AddMinutes(-25), reworkOf: Now.AddMinutes(-120)),
            reworkPredecessor: null,
            Label);

        Assert.Equal(ApprovalMeasurementResolution.Unmeasured, measurement.Resolution);
        Assert.Contains("rework time", measurement.UnmeasuredReason);
    }

    /// <summary>The exercise label is mandatory and carries the set size and the class limits.</summary>
    [Fact]
    public void TheExerciseLabelIsMandatoryAndCarriesItsClassLimits()
    {
        Assert.Throws<ArgumentException>(() => new ApprovalExerciseLabel("exercise", 1, "   "));
        Assert.Throws<ArgumentException>(() => new ApprovalExerciseLabel("  ", 1, "limits"));

        var measurement = ApprovalMeasurement.Derive(
            Approval(queuedAt: Now.AddMinutes(-25)), null, Label);

        Assert.Equal(1, measurement.Label.SetSize);
        Assert.Contains("not the real approval series", measurement.Label.ClassLimits);
    }

    /// <summary>
    /// A set with an unmeasured member reports that count rather than averaging over the measured
    /// remainder, because an average over the complete subset is the same overstatement again.
    /// </summary>
    [Fact]
    public void AnExerciseReportsItsUnmeasuredCountAlongsideItsTotals()
    {
        var exercise = new ApprovalSurfaceExercise(
        [
            ApprovalMeasurement.Derive(Approval(queuedAt: Now.AddMinutes(-25)), null, Label),
            ApprovalMeasurement.Derive(Approval(queuedAt: null), null, Label),
        ],
        Label);

        Assert.Equal(1, exercise.MeasuredCount);
        Assert.Equal(1, exercise.UnmeasuredCount);
        Assert.NotNull(exercise.Totals());
    }

    /// <summary>A set with no measured member has no totals at all, rather than totals of zero.</summary>
    [Fact]
    public void AnExerciseWithNoMeasuredMemberHasNoTotals()
    {
        var exercise = new ApprovalSurfaceExercise(
            [ApprovalMeasurement.Derive(Approval(queuedAt: null), null, Label)], Label);

        Assert.Null(exercise.Totals());
        Assert.Equal(0, exercise.MeasuredCount);
    }
}

/// <summary>The derived dispatch key, and what it makes impossible.</summary>
public sealed class DispatchKeyTests
{
    /// <summary>
    /// The key is DERIVED from the item and its exact version. Two attempts for one version cannot
    /// produce two keys, because neither attempt contributes anything to it.
    /// </summary>
    [Fact]
    public void TheKeyIsDerivedFromItemAndVersionAlone()
    {
        var item = ItemId.New();
        var version = new ItemVersion(7);

        var first = DispatchKey.Derive(item, version);
        var second = DispatchKey.Derive(item, version);

        Assert.Equal(first, second);
        Assert.Equal(first.Value, second.Value);
    }

    /// <summary>A different version is a different dispatch, so the keys differ.</summary>
    [Fact]
    public void ADifferentVersionDerivesADifferentKey()
    {
        var item = ItemId.New();

        Assert.NotEqual(
            DispatchKey.Derive(item, new ItemVersion(7)),
            DispatchKey.Derive(item, new ItemVersion(8)));
    }

    /// <summary>A different item is a different dispatch.</summary>
    [Fact]
    public void ADifferentItemDerivesADifferentKey()
    {
        var version = new ItemVersion(1);

        Assert.NotEqual(
            DispatchKey.Derive(ItemId.New(), version),
            DispatchKey.Derive(ItemId.New(), version));
    }

    /// <summary>
    /// Derived a hundred times over, the key never varies. Nothing per-attempt, and no clock, gets
    /// into it, which is the property a retry depends on.
    /// </summary>
    [Fact]
    public void TheKeyIsStableAcrossRepeatedDerivation()
    {
        var item = ItemId.New();
        var version = new ItemVersion(2);
        var expected = DispatchKey.Derive(item, version);

        Assert.All(
            Enumerable.Range(0, 100).Select(_ => DispatchKey.Derive(item, version)),
            k => Assert.Equal(expected, k));
    }
}
