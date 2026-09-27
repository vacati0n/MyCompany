using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// Persists a composed dispatch (modules M-022, M-023 over M-017).
///
/// EXACTLY-ONCE IS THIS METHOD'S TRANSACTION. The gate state change, the dispatch record and the
/// queue entry are written inside ONE transaction, so an interruption between any two of them
/// leaves NONE of the three rather than one. The dispatch key is derived from the item and its
/// exact version, and the datastore holds the uniqueness, so a repeat after success, a repeat
/// after failure and a concurrent duplicate all resolve to the one record that already exists.
///
/// The method ends at the persisted descriptor. There is nothing after it: no component in this
/// build consumes a dispatch record, and the queue entry it writes moves the unit to a TERMINAL
/// claim state, because the publishing workflow declares no position after composition.
/// </summary>
public sealed class PublicationDispatchService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublicationDispatchService(IUnitOfWork unitOfWork, IClock clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Writes the outcome of one dispatch attempt.
    ///
    /// A refused attempt writes its attempt row and its audit entry and no dispatch record. A
    /// composed attempt writes the dispatch record, the attempt row, the gate transition, the
    /// queue entry and the audit entry together.
    /// </summary>
    public async Task<DispatchPersistResult> PersistAsync(
        DispatchOutcome outcome,
        ItemId item,
        ItemVersion version,
        DestinationDescriptor destination,
        string approvedBy,
        string metadataAndSettingsDigest,
        JobId? job,
        GateStateChange? gateStateChange,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(destination);

        var now = _clock.UtcNow;
        var attempt = PublicationDispatchComposer.RecordFor(
            outcome, item, version, destination, approvedBy, metadataAndSettingsDigest, now);

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        if (outcome is DispatchOutcome.Refused refused)
        {
            // A refused attempt is as answerable as an unrefused one. It is recorded with all five
            // answers and the condition it failed, and it leaves no dispatch record behind.
            await transaction.Dispatches.RecordAttemptAsync(attempt, cancellationToken).ConfigureAwait(false);
            await transaction.Audit.AppendAsync(
                Entry(item, version, destination, "publication.dispatch-refused",
                      $"{refused.Reason}: {refused.Detail}", approvedBy),
                cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return new DispatchPersistResult(DispatchWriteOutcome.NoRecordWritten, Refused: true, attempt);
        }

        var composed = (DispatchOutcome.Composed)outcome;
        var descriptor = composed.Descriptor;

        var write = await transaction.Dispatches
            .RecordDispatchAsync(
                new DispatchRecord
                {
                    Key = descriptor.Key,
                    Item = descriptor.Item,
                    Version = descriptor.Version,
                    DestinationCanonical = descriptor.Destination.Canonical,
                    PlannedAt = descriptor.PlannedAt,
                    MetadataAndSettingsDigest = descriptor.MetadataAndSettingsDigest,
                    ComposedAt = descriptor.ComposedAt,
                },
                cancellationToken)
            .ConfigureAwait(false);

        await transaction.Dispatches.RecordAttemptAsync(attempt, cancellationToken).ConfigureAwait(false);

        if (write == DispatchWriteOutcome.Created)
        {
            // The gate state change goes in this same transaction. It is the write the dispatch
            // record and the queue entry must be atomic with, which is why all three are here.
            //
            // The change is SUPPLIED by the caller rather than chosen here, and this change adds no
            // state and no edge to the delivered transition table. Nothing in this build writes a
            // transition to Published: no publication occurs, so recording one would be a false
            // entry in an append-only record that refuses correction.
            if (gateStateChange is { } change)
            {
                if (change.To == GateState.Published)
                {
                    throw new ArgumentException(
                        "Nothing in this change publishes, so a transition to Published is not a state "
                        + "this path may record. The dispatch comes to rest at the composed descriptor.",
                        nameof(gateStateChange));
                }

                await transaction.Gates.RecordTransitionAsync(
                    item,
                    version,
                    change.From,
                    change.To,
                    change.Reason,
                    now,
                    cancellationToken).ConfigureAwait(false);
            }

            if (job is { } jobId)
            {
                // The queue entry, DERIVED from the publishing workflow definition rather than
                // hardcoded. Composition is the last position that definition declares, so the
                // engine's own successor lookup returns none and the unit reaches a terminal claim
                // state; were a stage ever added after composition this would advance to it
                // instead, which is exactly what the build-time boundary check refuses.
                var next = PublishingWorkflow.Definition.Next(LifecyclePosition.PublishingComposed);

                await transaction.Jobs.AdvanceAsync(
                    jobId,
                    next ?? LifecyclePosition.Completed,
                    next is null ? ClaimState.Done : ClaimState.Ready,
                    now,
                    cancellationToken).ConfigureAwait(false);
            }
        }

        await transaction.Audit.AppendAsync(
            Entry(item, version, destination,
                  write == DispatchWriteOutcome.Created
                      ? "publication.dispatch-composed"
                      : "publication.dispatch-already-recorded",
                  write == DispatchWriteOutcome.Created
                      ? $"descriptor composed and persisted under key {descriptor.Key}"
                      : $"a dispatch record already exists for version {version}; no second record was written",
                  approvedBy),
            cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new DispatchPersistResult(write, Refused: false, attempt);
    }

    private static AuditEntryDraft Entry(
        ItemId item,
        ItemVersion version,
        DestinationDescriptor destination,
        string action,
        string reason,
        string approvedBy) => new()
    {
        Actor = WorkforceRole.Publisher.ToString(),
        Action = action,
        Subject = $"item:{item} version:{version}",
        Reason = reason,
        InputsReference = $"destination:{destination.Canonical}",
        OutputsReference = "dispatch-descriptor at rest; no egress exists to consume it",
        Decision = action,
        CostReference = "none",
        Risk = $"approval:{approvedBy}",
        RetentionClass = RetentionClass.OperationalRecord,
    };
}

/// <summary>
/// The gate state change a dispatch commits with, supplied by the caller.
///
/// <see cref="GateState.Published"/> is refused by the service: no publication occurs in this
/// change, and the append-only record has no in-place correction, so a false transition written
/// here could not be taken back.
/// </summary>
public sealed record GateStateChange(GateState From, GateState To, string Reason);

/// <summary>
/// What one persist produced. <see cref="Write"/> tells a first dispatch from a retry, and both
/// leave exactly one record.
/// </summary>
public sealed record DispatchPersistResult(
    DispatchWriteOutcome Write,
    bool Refused,
    AttemptRecord Attempt);
