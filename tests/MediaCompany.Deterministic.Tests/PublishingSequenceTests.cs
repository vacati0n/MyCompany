using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Rights;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The publishing entry point, the dispatch service's terminal-stage closure, the named-unit
/// claim and the dossier recorder, over the in-memory unit of work.
///
/// These assert the SHAPE of what is written: which positions are driven and in which order, which
/// rows and entries each outcome produces and which it does not. The same properties against the
/// record store are demonstrated in the persistence suite.
/// </summary>
public sealed class PublishingSequenceTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-01T12:00:00Z");
    private static readonly DateOnly Observed = new(2026, 8, 30);

    private sealed class Harness
    {
        public InMemoryWork Work { get; } = new();
        public FixedClock Clock { get; } = new(Now);
        public RecordedGateState Gate { get; } = new();

        public PublishingSequenceService Sequence()
        {
            var gate = new PublicationGateService(Gate, new NoAssets(), Work, Clock);
            return new PublishingSequenceService(
                new WorkLifecycleService(Work, Clock),
                gate,
                new PublicationDispatchService(Work, Clock),
                Clock);
        }
    }

    /// <summary>
    /// Recorded gate state: the blocks, approvals, current state and condition observations the
    /// gate service reads. Nothing here is configuration; each test records what it asserts over.
    /// </summary>
    private sealed class RecordedGateState : IGateLedger
    {
        public GateState State { get; set; } = GateState.Draft;
        public List<Approval> Approvals { get; } = [];
        public List<ConditionObservation> Conditions { get; } = [];

        public Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Block>>([]);

        public Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Approval>>(Approvals.ToArray());

        public Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken ct) =>
            Task.FromResult(State);

        public Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(ChannelId channel, CancellationToken ct) =>
            Task.FromResult(new FirstPublicationConditionRegister(Conditions));

        public void RecordOwnerApproval(ItemId item, ItemVersion version)
        {
            State = GateState.Approved;
            Approvals.Add(new Approval(item, version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
                ApprovalVerdict.Approved, "approved", Now.AddHours(-2), Now.AddHours(-1)));
        }

        public void RecordConditions(ConditionState library, ConditionState payment, ConditionState twoStep)
        {
            Conditions.Clear();
            Conditions.Add(new ConditionObservation(FirstPublicationCondition.LibraryRegistration, library, "fixture observation", Observed));
            Conditions.Add(new ConditionObservation(FirstPublicationCondition.PaymentAccount, payment, "fixture observation", Observed));
            Conditions.Add(new ConditionObservation(FirstPublicationCondition.TwoStepVerification, twoStep, "fixture observation", Observed));
        }
    }

    /// <summary>The demonstration item carries no asset rows, so the rights precondition passes on the delivered rule.</summary>
    private sealed class NoAssets : IAssetLedger
    {
        public Task<IReadOnlyList<Asset>> ForItemAsync(ItemId item, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Asset>>([]);

        public Task<IReadOnlyList<LibraryRegistration>> RegistrationsAsync(ChannelId channel, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<LibraryRegistration>>([]);

        public Task RecordAsync(Asset asset, CancellationToken ct) => Task.CompletedTask;
    }

    private static PublishingSequenceRequest Request(ItemId item, ItemVersion version, int leadDays = 3) => new()
    {
        Item = item,
        Version = version,
        Channel = ChannelId.New(),
        Subject = PublishingFixture.Subject,
        Destination = PublishingFixture.Destination,
        TreatmentConditions = Enum.GetValues<TreatmentCondition>(),
        SurfaceDraft = PublishingFixture.Draft(),
        Settings = PublishingFixture.Settings,
        TimingPolicy = new PublicationTiming.TimingPolicy { LeadDays = leadDays, PreferredTimeOfDay = new TimeOnly(14, 0) },
    };

    // -----------------------------------------------------------------------
    // The entry point over real-shaped recorded state: it refuses at gate evaluation
    // -----------------------------------------------------------------------

    /// <summary>
    /// Over the company's real recorded state — no owner approval, no condition observation, the
    /// gate state Draft — the drive comes to rest REFUSED AT GATE EVALUATION. The refused attempt
    /// is recorded with the composer's reason and the gate's detail, no dispatch record exists,
    /// and the unit rests escalated in the dead claim state with every stage row closed.
    /// </summary>
    [Fact]
    public async Task OverRealRecordedStateTheDriveIsRefusedAtGateEvaluation()
    {
        var harness = new Harness();
        var item = ItemId.New();

        var result = await harness.Sequence().DriveAsync(Request(item, new ItemVersion(1)), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Equal(LifecyclePosition.PublishingGateEvaluation, result.RestingPosition);
        Assert.Contains(nameof(DispatchRefusalReason.GateRefused), result.Detail, StringComparison.Ordinal);
        Assert.Contains("Published is reachable only from Approved", result.Detail, StringComparison.Ordinal);

        var attempt = Assert.Single(harness.Work.Attempts);
        Assert.True(attempt.Refused);
        Assert.Empty(harness.Work.Dispatches);

        var unit = harness.Work.Jobs[result.Unit];
        Assert.Equal(ClaimState.Dead, unit.ClaimState);
        Assert.Equal(LifecyclePosition.PublishingGateEvaluation, unit.Position);

        Assert.Contains(harness.Work.AuditEntries, e => e.Action == LifecycleActions.StageEscalated);
        Assert.DoesNotContain(harness.Work.AuditEntries, e => e.Action == LifecycleActions.Completed);
        Assert.Contains(harness.Work.Stages, s =>
            s.Position == LifecyclePosition.PublishingGateEvaluation && s.Outcome == StageOutcome.Escalated && s.Escalated);

        // No gate transition was written: the entry point presents, records and passes nothing.
        Assert.Empty(harness.Work.Transitions);
        Assert.Empty(harness.Work.Approvals);
    }

    /// <summary>
    /// With the owner approval recorded and NO condition observed, the refusal names every one of
    /// the three conditions, each as ABSENT, under the closed first-publication reason.
    /// </summary>
    [Fact]
    public async Task WithNoConditionRecordedTheRefusalNamesAllThreeAsAbsent()
    {
        var harness = new Harness();
        var item = ItemId.New();
        var version = new ItemVersion(1);
        harness.Gate.RecordOwnerApproval(item, version);

        var result = await harness.Sequence().DriveAsync(Request(item, version), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Contains(nameof(DispatchRefusalReason.FirstPublicationConditionUnmet), result.Detail, StringComparison.Ordinal);
        foreach (var condition in Enum.GetNames<FirstPublicationCondition>())
        {
            Assert.Contains($"{condition} is NOT satisfied: NO observation is recorded", result.Detail, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// Each condition recorded NOT SATISFIED in turn, the other two satisfied: the refusal names
    /// that condition individually and no other.
    /// </summary>
    [Theory]
    [InlineData(FirstPublicationCondition.LibraryRegistration)]
    [InlineData(FirstPublicationCondition.PaymentAccount)]
    [InlineData(FirstPublicationCondition.TwoStepVerification)]
    public async Task EachConditionUnsetInTurnIsNamedIndividually(FirstPublicationCondition unset)
    {
        var harness = new Harness();
        var item = ItemId.New();
        var version = new ItemVersion(1);
        harness.Gate.RecordOwnerApproval(item, version);

        ConditionState StateOf(FirstPublicationCondition c) => c == unset ? ConditionState.NotSatisfied : ConditionState.Satisfied;
        harness.Gate.RecordConditions(
            StateOf(FirstPublicationCondition.LibraryRegistration),
            StateOf(FirstPublicationCondition.PaymentAccount),
            StateOf(FirstPublicationCondition.TwoStepVerification));

        var result = await harness.Sequence().DriveAsync(Request(item, version), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Contains($"{GatePredicates.RefusalFor(unset)}", result.Detail, StringComparison.Ordinal);
        Assert.Contains($"{unset} is NOT satisfied: observed false", result.Detail, StringComparison.Ordinal);
        foreach (var other in Enum.GetValues<FirstPublicationCondition>().Where(c => c != unset))
        {
            Assert.DoesNotContain($"{other} is NOT satisfied", result.Detail, StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// A condition recorded UNKNOWN folds to not-satisfied and stays VISIBLE as unknown in the
    /// refusal, distinct from absent and from observed false.
    /// </summary>
    [Fact]
    public async Task AConditionRecordedUnknownFoldsToNotSatisfiedAndStaysVisible()
    {
        var harness = new Harness();
        var item = ItemId.New();
        var version = new ItemVersion(1);
        harness.Gate.RecordOwnerApproval(item, version);
        harness.Gate.RecordConditions(ConditionState.Satisfied, ConditionState.Unknown, ConditionState.Satisfied);

        var result = await harness.Sequence().DriveAsync(Request(item, version), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtGateEvaluation, result.Rest);
        Assert.Contains("PaymentAccount is NOT satisfied: recorded UNKNOWN", result.Detail, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The entry point over state admitting it: composition, and no further
    // -----------------------------------------------------------------------

    /// <summary>
    /// Over recorded state admitting it, the drive passes every position the definition declares,
    /// IN THE DEFINITION'S ORDER, and comes to rest at composition: one dispatch record, the unit
    /// at the terminal claim state, the composition stage row closed as succeeded, and one
    /// completion entry. No position follows composition, and no gate transition is written.
    /// </summary>
    [Fact]
    public async Task OverAdmittingStateTheDriveComposesAndStopsAtTheTerminalPosition()
    {
        var harness = new Harness();
        var item = ItemId.New();
        var version = new ItemVersion(1);
        harness.Gate.RecordOwnerApproval(item, version);
        harness.Gate.RecordConditions(ConditionState.Satisfied, ConditionState.Satisfied, ConditionState.Satisfied);

        var result = await harness.Sequence().DriveAsync(Request(item, version), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.Composed, result.Rest);
        Assert.Equal(PublishingWorkflow.Definition.Stages, result.PositionsDriven);
        Assert.Equal(LifecyclePosition.PublishingComposed, result.RestingPosition);
        Assert.Null(PublishingWorkflow.Definition.Next(result.RestingPosition));

        Assert.Single(harness.Work.Dispatches);
        var unit = harness.Work.Jobs[result.Unit];
        Assert.Equal(ClaimState.Done, unit.ClaimState);
        Assert.Equal(LifecyclePosition.Completed, unit.Position);

        var composition = harness.Work.Stages.Where(s => s.Position == LifecyclePosition.PublishingComposed).ToArray();
        Assert.Contains(composition, s => s.Outcome == StageOutcome.Succeeded && s.LeftAt is not null);

        // Every pending row the drive opened was closed on its own key: one pending and one
        // closing write per position, on the same entered instant.
        foreach (var position in PublishingWorkflow.Definition.Stages)
        {
            var rows = harness.Work.Stages.Where(s => s.Position == position).ToArray();
            Assert.Equal(2, rows.Length);
            Assert.Equal(rows[0].EnteredAt, rows[1].EnteredAt);
            Assert.Equal(StageOutcome.Pending, rows[0].Outcome);
            Assert.Equal(StageOutcome.Succeeded, rows[1].Outcome);
        }

        Assert.Single(harness.Work.AuditEntries, e => e.Action == LifecycleActions.Completed);
        Assert.Empty(harness.Work.Transitions);
    }

    /// <summary>
    /// A second drive of the same item version meets the dispatch record already there: no second
    /// record is written, and the second unit comes to rest escalated at composition, naming the
    /// existing record, with no completion entry of its own.
    /// </summary>
    [Fact]
    public async Task ASecondDriveOfTheSameVersionRestsEscalatedNamingTheExistingRecord()
    {
        var harness = new Harness();
        var item = ItemId.New();
        var version = new ItemVersion(1);
        harness.Gate.RecordOwnerApproval(item, version);
        harness.Gate.RecordConditions(ConditionState.Satisfied, ConditionState.Satisfied, ConditionState.Satisfied);

        await harness.Sequence().DriveAsync(Request(item, version), CancellationToken.None);
        var second = await harness.Sequence().DriveAsync(Request(item, version), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.DispatchAlreadyRecorded, second.Rest);
        Assert.Equal(DispatchWriteOutcome.AlreadyRecorded, second.Dispatch!.Write);
        Assert.Contains("a dispatch record already exists", second.Detail, StringComparison.Ordinal);
        Assert.Single(harness.Work.Dispatches);

        var unit = harness.Work.Jobs[second.Unit];
        Assert.Equal(ClaimState.Dead, unit.ClaimState);
        Assert.Equal(LifecyclePosition.PublishingComposed, unit.Position);
        Assert.Single(harness.Work.AuditEntries, e => e.Action == LifecycleActions.Completed);
    }

    /// <summary>A timing policy the delivered step refuses brings the drive to rest refused at timing.</summary>
    [Fact]
    public async Task ATimingPolicyTheStepRefusesRestsTheDriveAtTiming()
    {
        var harness = new Harness();

        var result = await harness.Sequence().DriveAsync(
            Request(ItemId.New(), new ItemVersion(1), leadDays: -1), CancellationToken.None);

        Assert.Equal(PublishingSequenceRest.RefusedAtTiming, result.Rest);
        Assert.Equal(LifecyclePosition.PublishingTimingComputation, result.RestingPosition);
        Assert.Equal(ClaimState.Dead, harness.Work.Jobs[result.Unit].ClaimState);
        Assert.Empty(harness.Work.Attempts);
    }

    /// <summary>
    /// The entry point reads no configuration and takes no dependency that could present, record
    /// or shortcut an owner approval: its dependencies are the lifecycle, the gate service, the
    /// dispatch service and a clock, and nothing else.
    /// </summary>
    [Fact]
    public void TheEntryPointTakesNoConfigurationAndNoApprovalWriter()
    {
        var parameters = Assert.Single(typeof(PublishingSequenceService).GetConstructors())
            .GetParameters().Select(p => p.ParameterType).ToArray();

        Assert.Equal(
            new[] { typeof(WorkLifecycleService), typeof(PublicationGateService), typeof(PublicationDispatchService), typeof(IClock) },
            parameters);
        Assert.DoesNotContain(typeof(IConfigurationStore), parameters);

        var methods = typeof(PublishingSequenceService).GetMethods().Select(m => m.Name).ToArray();
        Assert.DoesNotContain(methods, m => m.Contains("Approv", StringComparison.Ordinal) || m.Contains("Present", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // The dispatch service's terminal-stage closure
    // -----------------------------------------------------------------------

    private static JobStage Closure(JobId job) => new()
    {
        Job = job,
        Position = LifecyclePosition.PublishingComposed,
        Outcome = StageOutcome.Succeeded,
        Attempts = 1,
        Escalated = false,
        EnteredAt = Now,
    };

    private static async Task<(InMemoryWork Work, JobId Job)> WorkWithAUnitAtComposition()
    {
        var work = new InMemoryWork();
        var job = JobId.New();
        await using var transaction = await work.BeginAsync(CancellationToken.None);
        await transaction.Jobs.EnqueueAsync(new Job
        {
            Id = job,
            Item = ItemId.New(),
            Channel = ChannelId.New(),
            Workflow = PublishingWorkflow.Name,
            Position = LifecyclePosition.PublishingComposed,
            ClaimState = ClaimState.Claimed,
            AvailableAt = Now,
        }, CancellationToken.None);
        await transaction.CommitAsync(CancellationToken.None);
        return (work, job);
    }

    /// <summary>
    /// A created dispatch with an advanced unit writes the closure and ONE completion entry with
    /// the dispatch record, in one commit.
    /// </summary>
    [Fact]
    public async Task ACreatedDispatchWritesTheClosureAndTheCompletionEntryInOneCommit()
    {
        var (work, job) = await WorkWithAUnitAtComposition();
        var commitsBefore = work.Commits;
        var item = ItemId.New();
        var version = new ItemVersion(3);

        var composed = ComposedFor(item, version);
        var result = await new PublicationDispatchService(work, new FixedClock(Now)).PersistAsync(
            composed, item, version, PublishingFixture.Destination, "owner", Digest(composed),
            job, gateStateChange: null, Closure(job), CancellationToken.None);

        Assert.Equal(DispatchWriteOutcome.Created, result.Write);
        Assert.Equal(commitsBefore + 1, work.Commits);
        Assert.Contains(work.Stages, s => s.Job == job && s.Position == LifecyclePosition.PublishingComposed
                                          && s.Outcome == StageOutcome.Succeeded && s.LeftAt == Now);
        Assert.Single(work.AuditEntries, e => e.Action == LifecycleActions.Completed);
        Assert.Equal(ClaimState.Done, work.Jobs[job].ClaimState);
    }

    /// <summary>
    /// A REFUSED attempt and an attempt resolving to a record ALREADY THERE write neither the
    /// closure nor the completion entry, so neither states an event that did not occur. A refused
    /// attempt writes its attempt row and audit entry, no dispatch record, and does not advance.
    /// </summary>
    [Fact]
    public async Task ARefusedOrAlreadyRecordedDispatchWritesNoClosureAndNoCompletion()
    {
        var (work, job) = await WorkWithAUnitAtComposition();
        var service = new PublicationDispatchService(work, new FixedClock(Now));
        var item = ItemId.New();
        var version = new ItemVersion(3);

        var refused = new DispatchOutcome.Refused(DispatchRefusalReason.GateRefused, "refused for the demonstration");
        await service.PersistAsync(refused, item, version, PublishingFixture.Destination, string.Empty,
            new string('0', 64), job, gateStateChange: null, Closure(job), CancellationToken.None);

        Assert.Empty(work.Dispatches);
        Assert.Single(work.Attempts);
        Assert.Equal(ClaimState.Claimed, work.Jobs[job].ClaimState);
        Assert.DoesNotContain(work.Stages, s => s.Job == job);
        Assert.DoesNotContain(work.AuditEntries, e => e.Action == LifecycleActions.Completed);

        var composed = ComposedFor(item, version);
        await service.PersistAsync(composed, item, version, PublishingFixture.Destination, "owner", Digest(composed),
            job: null, gateStateChange: null, CancellationToken.None);

        await service.PersistAsync(composed, item, version, PublishingFixture.Destination, "owner", Digest(composed),
            job, gateStateChange: null, Closure(job), CancellationToken.None);

        Assert.Single(work.Dispatches);
        Assert.Equal(ClaimState.Claimed, work.Jobs[job].ClaimState);
        Assert.DoesNotContain(work.Stages, s => s.Job == job);
        Assert.DoesNotContain(work.AuditEntries, e => e.Action == LifecycleActions.Completed);
    }

    /// <summary>A closure for another unit, or for another position, is refused before anything is written.</summary>
    [Fact]
    public async Task AClosureForAnotherUnitOrPositionIsRefused()
    {
        var (work, job) = await WorkWithAUnitAtComposition();
        var service = new PublicationDispatchService(work, new FixedClock(Now));
        var item = ItemId.New();
        var version = new ItemVersion(3);
        var composed = ComposedFor(item, version);

        await Assert.ThrowsAsync<ArgumentException>(() => service.PersistAsync(
            composed, item, version, PublishingFixture.Destination, "owner", Digest(composed),
            job, null, Closure(JobId.New()), CancellationToken.None));

        await Assert.ThrowsAsync<ArgumentException>(() => service.PersistAsync(
            composed, item, version, PublishingFixture.Destination, "owner", Digest(composed),
            job, null, Closure(job) with { Position = LifecyclePosition.PublishingGateEvaluation }, CancellationToken.None));

        Assert.Empty(work.Dispatches);
    }

    private static DispatchOutcome ComposedFor(ItemId item, ItemVersion version) =>
        PublicationDispatchComposer.Compose(
            WorkforceRole.Publisher, PublishingFixture.Passed(item, version), item, version,
            PublishingFixture.Destination, PublishingFixture.Settings,
            PublishingFixture.Surfaces(item, version), Now.AddDays(3), Now);

    private static string Digest(DispatchOutcome outcome) =>
        ((DispatchOutcome.Composed)outcome).Descriptor.MetadataAndSettingsDigest;

    // -----------------------------------------------------------------------
    // The named-unit claim
    // -----------------------------------------------------------------------

    /// <summary>
    /// The named claim takes the unit it is given and no other, even when another unit is ready
    /// and older, and writes the same claim entry the delivered claim writes.
    /// </summary>
    [Fact]
    public async Task TheNamedClaimTakesOnlyTheNamedUnit()
    {
        var work = new InMemoryWork();
        var lifecycle = new WorkLifecycleService(work, new FixedClock(Now));

        Job Unit() => new()
        {
            Id = JobId.New(),
            Item = ItemId.New(),
            Channel = ChannelId.New(),
            Workflow = PublishingWorkflow.Name,
            Position = LifecyclePosition.Queued,
            ClaimState = ClaimState.Ready,
            AvailableAt = Now,
        };

        var other = Unit();
        var named = Unit();
        await lifecycle.EnqueueAsync(other, CancellationToken.None);
        await lifecycle.EnqueueAsync(named, CancellationToken.None);

        var claimed = await lifecycle.ClaimUnitAsync(named.Id, "worker", TimeSpan.FromMinutes(1), CancellationToken.None);

        Assert.Equal(named.Id, claimed!.Id);
        Assert.Equal(ClaimState.Ready, work.Jobs[other.Id].ClaimState);
        Assert.Single(work.AuditEntries, e => e.Action == LifecycleActions.Claimed);

        Assert.Null(await lifecycle.ClaimUnitAsync(named.Id, "worker", TimeSpan.FromMinutes(1), CancellationToken.None));
    }

    // -----------------------------------------------------------------------
    // The dossier recorder
    // -----------------------------------------------------------------------

    /// <summary>
    /// A component recorded against a dossier never opened is refused and leaves NEITHER the row
    /// nor its audit entry; once opened, each recorded component and its entry commit together.
    /// </summary>
    [Fact]
    public async Task TheRecorderWritesEachComponentWithItsEntryAndRefusesAnUnopenedDossier()
    {
        var work = new InMemoryWork();
        var recorder = new ItemDossierRecorder(work, new FixedClock(Now));
        var item = ItemId.New();
        var version = new ItemVersion(1);
        var evidence = new StageEvidence
        {
            Stage = ProductionStage.Script,
            Outcome = StageOutcome.Succeeded,
            Summary = "script committed",
            RecordedAt = Now,
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            recorder.RecordStageAsync(item, version, evidence, WorkforceRole.Producer, CancellationToken.None));
        Assert.Empty(work.DossierRows);
        Assert.Empty(work.AuditEntries);

        await recorder.OpenAsync(item, version, WorkforceRole.Producer, CancellationToken.None);
        await recorder.RecordStageAsync(item, version, evidence, WorkforceRole.Producer, CancellationToken.None);
        await recorder.RecordComponentAsync(
            item, version, new DossierComponent.Runtime(TimeSpan.FromMinutes(12)), WorkforceRole.Producer, CancellationToken.None);

        Assert.Equal(2, work.DossierRows.Count);
        Assert.Single(work.AuditEntries, e => e.Action == ItemDossierRecorder.OpenedAction);
        Assert.Equal(2, work.AuditEntries.Count(e => e.Action == ItemDossierRecorder.RecordedAction));
        Assert.All(work.AuditEntries, e => Assert.Equal(nameof(WorkforceRole.Producer), e.Actor));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            recorder.OpenAsync(item, version, WorkforceRole.Producer, CancellationToken.None));
    }
}
