using MediaCompany.Application.Ports;
using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// What one drive of the publishing sequence is given: the value slots the delivered steps
/// already take as parameters, and nothing that reaches a control. No configuration value is read
/// by the drive, so no setting can reach a precondition, the owner-approval state or a step.
/// </summary>
public sealed record PublishingSequenceRequest
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required ChannelId Channel { get; init; }
    public required DeclaredSubject Subject { get; init; }
    public required DestinationDescriptor Destination { get; init; }
    public required IReadOnlyList<TreatmentCondition> TreatmentConditions { get; init; }
    public required SurfaceDraft SurfaceDraft { get; init; }
    public required PublicationSettings Settings { get; init; }
    public required PublicationTiming.TimingPolicy TimingPolicy { get; init; }
}

/// <summary>Where one drive came to rest. There is no member past composition, because there is no position past it.</summary>
public enum PublishingSequenceRest
{
    /// <summary>
    /// The descriptor was composed and persisted and the unit rests at the terminal claim state.
    /// Nothing follows: no component consumes the descriptor, and composition is the last
    /// position the definition declares.
    /// </summary>
    Composed = 1,

    /// <summary>The composer refused at gate evaluation, naming its reason and the gate's detail.</summary>
    RefusedAtGateEvaluation = 2,

    /// <summary>The timing step refused the supplied policy.</summary>
    RefusedAtTiming = 3,

    /// <summary>A dispatch is already recorded for this item version; no second record was written.</summary>
    DispatchAlreadyRecorded = 4,

    /// <summary>The unit was not ready and available to claim at the position it was due to run.</summary>
    NotClaimable = 5,
}

/// <summary>What one drive did and where it came to rest.</summary>
public sealed record PublishingSequenceResult(
    JobId Unit,
    PublishingSequenceRest Rest,
    LifecyclePosition RestingPosition,
    IReadOnlyList<LifecyclePosition> PositionsDriven,
    string Detail,
    DispatchPersistResult? Dispatch);

/// <summary>
/// The publishing entry point (decision D-005 of the accepted design).
///
/// ONE deterministic service drives one item version through every position the delivered
/// publishing workflow declares, in the order its successor lookup returns them, and comes to rest
/// at the terminal position or at a refusal the composer names. It adds no position, no outcome
/// case, no action and no transport: every position comes from the delivered definition, every
/// step is a delivered step, every outcome is recorded through the delivered lifecycle, and the
/// decision is the delivered composer's, presented once.
///
/// What it does NOT do is as much the design as what it does. It presents nothing for owner
/// approval, records no verdict and passes no gate transition, so owner approval is read from
/// recorded state and never written or shortcut here. It holds no reference to the capability
/// boundary or the credential broker — its assembly cannot name either — and it reads no
/// configuration. On the company's real recorded state, where no owner approval and no
/// first-publication condition is recorded, every drive comes to rest refused at gate evaluation.
/// </summary>
public sealed class PublishingSequenceService
{
    /// <summary>The worker identity the drive claims its unit as.</summary>
    public const string WorkerId = "publishing-sequence";

    /// <summary>The lease a claim is taken for. A drive runs each position's pure step inside it.</summary>
    public static readonly TimeSpan ClaimLease = TimeSpan.FromMinutes(5);

    /// <summary>The role the drive acts as. It holds the publish and dispatch actions and no egress action.</summary>
    public const WorkforceRole Actor = WorkforceRole.Publisher;

    private static readonly TimeSpan StepDeadline = TimeSpan.FromHours(1);

    private readonly WorkLifecycleService _lifecycle;
    private readonly PublicationGateService _gate;
    private readonly PublicationDispatchService _dispatch;
    private readonly IClock _clock;

    public PublishingSequenceService(
        WorkLifecycleService lifecycle,
        PublicationGateService gate,
        PublicationDispatchService dispatch,
        IClock clock)
    {
        _lifecycle = lifecycle;
        _gate = gate;
        _dispatch = dispatch;
        _clock = clock;
    }

    /// <summary>
    /// Drives one item version through the publishing sequence.
    ///
    /// The drive enqueues a NEW unit of the publishing workflow at its first position, and for each
    /// position claims THAT unit by name, runs the position's step and records the outcome through
    /// the lifecycle. An interrupted drive is not resumed on the same unit; a second drive starts a
    /// new unit, and exactly-once is held at the dispatch record.
    /// </summary>
    public async Task<PublishingSequenceResult> DriveAsync(
        PublishingSequenceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = PublishingWorkflow.Definition;
        var first = definition.Stages[0];

        var unit = new Job
        {
            Id = JobId.New(),
            Item = request.Item,
            Channel = request.Channel,
            Workflow = definition.Name,
            Position = first,
            ClaimState = ClaimState.Ready,
            AvailableAt = _clock.UtcNow,
        };

        var firstEnteredAt = await _lifecycle.EnqueueAsync(unit, cancellationToken).ConfigureAwait(false);

        var driven = new List<LifecyclePosition>();
        IReadOnlyList<SurfaceRecord> surfaces = [];
        PublicationTiming.PlannedPublication? planned = null;
        DispatchOutcome.Composed? composed = null;

        for (LifecyclePosition? next = first; next is { } position; next = definition.Next(position))
        {
            var claimed = await _lifecycle
                .ClaimUnitAsync(unit.Id, WorkerId, ClaimLease, cancellationToken).ConfigureAwait(false);

            if (claimed is null)
            {
                return new PublishingSequenceResult(
                    unit.Id, PublishingSequenceRest.NotClaimable, position, driven,
                    $"unit {unit.Id} was not ready and available to claim at {position}; it rests there unclaimed",
                    Dispatch: null);
            }

            if (claimed.Position != position)
            {
                throw new InvalidOperationException(
                    $"Unit {unit.Id} was claimed at {claimed.Position}, but the definition's next position is {position}.");
            }

            driven.Add(position);

            // The pending stage row this position opened, keyed on the instant it was entered at.
            // The first position's row was entered at enqueue; every later one at the advance that
            // made the unit ready, which is the instant its availability was set to.
            var enteredAt = position == first ? firstEnteredAt : claimed.AvailableAt;

            switch (position)
            {
                case LifecyclePosition.Queued:
                    // The queue position runs no step of its own: being claimed from the queue is
                    // what it consists of.
                    await SucceedAsync(claimed, position, enteredAt, cancellationToken).ConfigureAwait(false);
                    break;

                case LifecyclePosition.PublishingSurfaceProduction:
                    surfaces = PublishedFacingSurfaces.Produce(
                        request.Item,
                        request.Version,
                        request.Subject,
                        request.Destination,
                        request.TreatmentConditions,
                        request.SurfaceDraft,
                        _clock.UtcNow);
                    await SucceedAsync(claimed, position, enteredAt, cancellationToken).ConfigureAwait(false);
                    break;

                case LifecyclePosition.PublishingTimingComputation:
                    try
                    {
                        planned = PublicationTiming.Compute(
                            request.Item, request.Version, _clock.UtcNow, request.TimingPolicy);
                    }
                    catch (ArgumentException refused)
                    {
                        var reason = $"the timing step refused the supplied policy: {refused.Message}";
                        await RefuseAsync(claimed, position, enteredAt, reason, cancellationToken).ConfigureAwait(false);
                        return new PublishingSequenceResult(
                            unit.Id, PublishingSequenceRest.RefusedAtTiming, position, driven, reason, Dispatch: null);
                    }

                    await SucceedAsync(claimed, position, enteredAt, cancellationToken).ConfigureAwait(false);
                    break;

                case LifecyclePosition.PublishingGateEvaluation:
                {
                    // The gate, evaluated over RECORDED STATE: blocks, approvals, the current gate
                    // state, the assets, the registrations and the three conditions of first
                    // publication, all read at this moment. Its verdict and the step outputs are
                    // presented to the composer ONCE, and the composer decides.
                    var verdict = await _gate.EvaluatePublishAsync(
                        request.Item, request.Version, request.Channel, Actor, cancellationToken).ConfigureAwait(false);

                    var outcome = PublicationDispatchComposer.Compose(
                        Actor,
                        verdict,
                        request.Item,
                        request.Version,
                        request.Destination,
                        request.Settings,
                        surfaces,
                        planned!.PlannedAt,
                        _clock.UtcNow);

                    if (outcome is DispatchOutcome.Refused refused)
                    {
                        // The refusal is a completed outcome, not a failure of the drive: it is
                        // persisted as a refused attempt naming the composer's reason and the
                        // gate's detail, and the stage is recorded under the refusal class, which
                        // escalates and never retries.
                        var persisted = await _dispatch.PersistAsync(
                            refused,
                            request.Item,
                            request.Version,
                            request.Destination,
                            approvedBy: string.Empty,
                            DispatchDescriptor.DigestOf(request.Destination, request.Settings, surfaces),
                            unit.Id,
                            gateStateChange: null,
                            cancellationToken).ConfigureAwait(false);

                        var reason = $"{refused.Reason}: {refused.Detail}";
                        await RefuseAsync(claimed, position, enteredAt, reason, cancellationToken).ConfigureAwait(false);

                        return new PublishingSequenceResult(
                            unit.Id, PublishingSequenceRest.RefusedAtGateEvaluation, position, driven, reason, persisted);
                    }

                    composed = (DispatchOutcome.Composed)outcome;
                    await SucceedAsync(claimed, position, enteredAt, cancellationToken).ConfigureAwait(false);
                    break;
                }

                case LifecyclePosition.PublishingComposed:
                {
                    // The unit reaches composition ONLY with a passing verdict, because a refusal
                    // above returns. The descriptor is persisted with the unit, NO gate
                    // transition, and the unit's terminal-stage closure, which the dispatch
                    // service writes with the completion entry in the transaction that writes the
                    // dispatch record and the terminal claim state.
                    var descriptor = composed!.Descriptor;
                    var closure = new JobStage
                    {
                        Job = unit.Id,
                        Position = position,
                        Outcome = StageOutcome.Succeeded,
                        Attempts = 1,
                        Escalated = false,
                        EnteredAt = enteredAt,
                    };

                    var persisted = await _dispatch.PersistAsync(
                        composed,
                        request.Item,
                        request.Version,
                        request.Destination,
                        ApprovedBy(descriptor.GatePass),
                        descriptor.MetadataAndSettingsDigest,
                        unit.Id,
                        gateStateChange: null,
                        closure,
                        cancellationToken).ConfigureAwait(false);

                    if (persisted.Write == DispatchWriteOutcome.Created)
                    {
                        return new PublishingSequenceResult(
                            unit.Id, PublishingSequenceRest.Composed, position, driven,
                            $"composed and persisted under key {descriptor.Key}; the unit rests at the terminal "
                            + "claim state and nothing follows composition",
                            persisted);
                    }

                    // A dispatch already recorded for this version: exactly-once holds at the
                    // record, and this second unit comes to rest escalated, naming the record.
                    var reason = $"a dispatch record already exists for item {request.Item} version {request.Version} "
                        + $"under key {descriptor.Key}; no second record was written";
                    await RefuseAsync(claimed, position, enteredAt, reason, cancellationToken).ConfigureAwait(false);

                    return new PublishingSequenceResult(
                        unit.Id, PublishingSequenceRest.DispatchAlreadyRecorded, position, driven, reason, persisted);
                }

                default:
                    // The entry point runs only delivered steps. A position the definition declares
                    // and the entry point has no delivered step for is refused rather than
                    // improvised; composition is the last position, so this is not reachable.
                    throw new InvalidOperationException(
                        $"The publishing workflow declares {position}, for which no delivered step exists.");
            }
        }

        throw new InvalidOperationException(
            "The publishing workflow ended without reaching composition, which is its last declared position.");
    }

    private Task SucceedAsync(
        Job claimed,
        LifecyclePosition position,
        DateTimeOffset enteredAt,
        CancellationToken cancellationToken) =>
        _lifecycle.RecordStageOutcomeAsync(
            claimed,
            PublishingWorkflow.Definition,
            position,
            enteredAt,
            succeeded: true,
            failureClass: null,
            attemptsSoFar: 1,
            PublishingWorkflow.Definition.FailurePolicy,
            TimeSpan.Zero,
            StepDeadline,
            failureReason: null,
            cancellationToken);

    private Task RefuseAsync(
        Job claimed,
        LifecyclePosition position,
        DateTimeOffset enteredAt,
        string reason,
        CancellationToken cancellationToken) =>
        _lifecycle.RecordStageOutcomeAsync(
            claimed,
            PublishingWorkflow.Definition,
            position,
            enteredAt,
            succeeded: false,
            FailureClass.Refusal,
            attemptsSoFar: 1,
            PublishingWorkflow.Definition.FailurePolicy,
            TimeSpan.Zero,
            StepDeadline,
            reason,
            cancellationToken);

    /// <summary>Answer five of the attempt record: on whose approval. Read from the gate pass, never written here.</summary>
    private static string ApprovedBy(GatePassToken pass) =>
        $"{WorkforceRole.Owner} approval at gate '{pass.Gate}' bound to item {pass.Item} version {pass.ItemVersion}";
}
