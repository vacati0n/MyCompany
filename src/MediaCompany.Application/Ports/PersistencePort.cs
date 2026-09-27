using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Work;

namespace MediaCompany.Application.Ports;

/// <summary>
/// The only path to durable state (module M-017). The port is defined before any
/// datastore-specific realization, per sequencing constraint P-008, so the claim mechanism the
/// stack decision moves substitutes without changing any consumer (risk R-011).
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Opens a transaction. Every writer the company uses is reachable only from the returned
    /// transaction, which is how the state change, the queue entry, the operation record and the
    /// audit entry are made to commit together. Constraint C-004 buys exactly-once with exactly
    /// that property: there is no writer that can commit on its own.
    /// </summary>
    Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken);
}

/// <summary>
/// One transaction. Nothing it wrote is durable until <see cref="CommitAsync"/> returns, and
/// everything it wrote is durable afterwards.
/// </summary>
public interface IWorkTransaction : IAsyncDisposable
{
    IAuditAppender Audit { get; }

    IOperationRecorder Operations { get; }

    IJobWriter Jobs { get; }

    IBudgetEvaluator Budgets { get; }

    /// <summary>
    /// Gate state changes. They are reachable only from a transaction so that an approval, a
    /// transition or a block and the entry that records it are one commit; a state change with no
    /// entry explaining it, or an entry describing a change that did not commit, is the gap this
    /// placement closes.
    /// </summary>
    IGateWriter Gates { get; }

    /// <summary>
    /// Route availability transitions, reachable only from a transaction for the same reason: the
    /// state a route moves to and the operation that caused the move commit together.
    /// </summary>
    IRouteAvailabilityWriter Availability { get; }

    /// <summary>
    /// Publication dispatch records and attempt records (module M-023).
    ///
    /// Reachable ONLY from a transaction, exactly as every other writer is, and that is where
    /// exactly-once lives: the dispatch row, the queue entry and the gate state change commit
    /// together or none of them does, so an interruption between them leaves neither a dispatch
    /// without a queue entry nor a queue entry without a dispatch.
    /// </summary>
    IDispatchWriter Dispatches { get; }

    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>Gate state changes, written inside the transaction that records them.</summary>
public interface IGateWriter
{
    Task RecordApprovalAsync(MediaCompany.Domain.Publication.Approval approval, CancellationToken cancellationToken);

    Task RecordBlockAsync(MediaCompany.Domain.Publication.Block block, CancellationToken cancellationToken);

    Task RecordTransitionAsync(
        ItemId item,
        ItemVersion version,
        MediaCompany.Domain.Publication.GateState from,
        MediaCompany.Domain.Publication.GateState to,
        string reason,
        DateTimeOffset at,
        CancellationToken cancellationToken);
}

/// <summary>
/// Publication dispatch and attempt records, written inside the transaction that writes the gate
/// state change and the queue entry (module M-023, module M-017).
/// </summary>
public interface IDispatchWriter
{
    /// <summary>
    /// Writes the dispatch record for one item version, or resolves to the record that already
    /// exists for it.
    ///
    /// The key is DERIVED from the item and its exact version, so a retry arrives at the same key
    /// and therefore at the same row. Uniqueness is the datastore's, which makes a second record
    /// impossible rather than detectable; this method returns whether the row it resolved to was
    /// written now or already existed, so a caller can tell a first dispatch from a retry without
    /// either of them producing two.
    /// </summary>
    Task<DispatchWriteOutcome> RecordDispatchAsync(
        MediaCompany.Domain.Publication.DispatchRecord record,
        CancellationToken cancellationToken);

    /// <summary>Appends one attempt, refused attempts included, with its five audit answers.</summary>
    Task RecordAttemptAsync(
        MediaCompany.Domain.Publication.AttemptRecord attempt,
        CancellationToken cancellationToken);
}

/// <summary>
/// Whether a dispatch write created the record or resolved to the one already there. Both are
/// success; exactly one record exists either way.
/// </summary>
public enum DispatchWriteOutcome
{
    /// <summary>This call wrote the record. The first dispatch of this item version.</summary>
    Created = 1,

    /// <summary>A record for this item version already existed. A retry, and still exactly one.</summary>
    AlreadyRecorded = 2,
}

/// <summary>Route availability transitions, written inside the transaction that records them.</summary>
public interface IRouteAvailabilityWriter
{
    Task RecordAsync(MediaCompany.Domain.Capabilities.RouteAvailability availability, CancellationToken cancellationToken);
}

/// <summary>
/// The append-only record (module M-007). There is no update and no delete on this interface:
/// amendment is refused because it is not expressible, and an attempt made through the datastore
/// is refused there too and recorded as its own entry (constraint C-014).
/// </summary>
public interface IAuditAppender
{
    /// <summary>Appends one entry, chaining it to the current head.</summary>
    Task<AuditEntry> AppendAsync(AuditEntryDraft draft, CancellationToken cancellationToken);

    /// <summary>
    /// Records that an amendment of <paramref name="targetEntry"/> was attempted and refused. The
    /// refusal is itself an entry naming the entry it was attempted against, and the original is
    /// returned unchanged (acceptance criterion AC-009).
    /// </summary>
    Task<AuditEntry> AppendRefusalAsync(
        AuditEntryId targetEntry,
        AuditRefusalReason reason,
        string actor,
        CancellationToken cancellationToken);
}

/// <summary>
/// The content of an entry before it is chained. The hash and the previous hash are assigned by
/// the appender, so a caller cannot author a chain.
/// </summary>
public sealed record AuditEntryDraft
{
    public required string Actor { get; init; }
    public required string Action { get; init; }
    public required string Subject { get; init; }
    public required string Reason { get; init; }
    public required string InputsReference { get; init; }
    public required string OutputsReference { get; init; }
    public required string Decision { get; init; }
    public required string CostReference { get; init; }
    public required string Risk { get; init; }
    public required RetentionClass RetentionClass { get; init; }
}

/// <summary>
/// The operation meter (module M-005). One row per attempt, written by the same boundary that
/// performed the work, inside the same transaction (decisions D-006, D-011).
/// </summary>
public interface IOperationRecorder
{
    /// <summary>
    /// Records one attempt. The computed cost is produced by the datastore from the unit counts
    /// and the applied price row, not by the caller, so the arithmetic stays exact and
    /// re-derivable after the price has changed (constraint C-005).
    /// </summary>
    Task<OperationRecord> RecordAsync(OperationDraft draft, CancellationToken cancellationToken);
}

/// <summary>The caller-supplied part of an operation row. It carries no computed cost.</summary>
public sealed record OperationDraft
{
    public required OperationId Id { get; init; }
    public required RunId Run { get; init; }
    public required MediaCompany.Domain.Capabilities.Attribution Attribution { get; init; }
    public required MediaCompany.Domain.Capabilities.CapabilityClass Capability { get; init; }
    public RouteId? Route { get; init; }
    public ModelId? Model { get; init; }
    public string? DeterministicTaskName { get; init; }
    public required UnitCounts Units { get; init; }
    public required CostBasis CostBasis { get; init; }
    public required TimeSpan Duration { get; init; }
    public required OperationOutcome Outcome { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required int Attempt { get; init; }
    public string? FailureReason { get; init; }

    /// <summary>The reasoning tier the request asked for.</summary>
    public MediaCompany.Domain.Capabilities.ReasoningTier? ReasoningTierRequested { get; init; }

    /// <summary>
    /// The reasoning tier the ADMITTING ROUTE stated it served. Null is the explicit absence
    /// marker: the route could not state one. It is never populated from the request.
    /// </summary>
    public MediaCompany.Domain.Capabilities.ReasoningTier? ReasoningTierServed { get; init; }
}

/// <summary>The durable job queue as a transactional claim table (module M-013 over M-017).</summary>
public interface IJobWriter
{
    Task EnqueueAsync(Job job, CancellationToken cancellationToken);

    Task RecordStageAsync(JobStage stage, CancellationToken cancellationToken);

    /// <summary>
    /// Claims one ready job for <paramref name="workerId"/>, or returns null when none is ready.
    /// The claim is part of this transaction, so a crash between the claim and the work it
    /// authorises leaves the job ready rather than lost.
    /// </summary>
    Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken cancellationToken);

    Task ReleaseAsync(JobId job, ClaimState state, DateTimeOffset availableAt, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a unit to its next lifecycle position and claim state together. A unit whose
    /// workflow has no next position reaches a terminal claim state here, which is what stops a
    /// finished unit from being claimed again.
    /// </summary>
    Task AdvanceAsync(
        JobId job,
        MediaCompany.Domain.Work.LifecyclePosition position,
        ClaimState state,
        DateTimeOffset availableAt,
        CancellationToken cancellationToken);
}

/// <summary>
/// Budget utilization and threshold crossing (module M-006). Utilization is computed by the
/// datastore from the recorded operations alone (acceptance criterion AC-027).
/// </summary>
public interface IBudgetEvaluator
{
    /// <summary>
    /// Evaluates every budget governing the attribution of a just-recorded operation and returns
    /// the alerts this evaluation raised. An alert is idempotent per budget, period and threshold.
    /// </summary>
    Task<IReadOnlyList<BudgetAlert>> EvaluateAsync(
        MediaCompany.Domain.Capabilities.Attribution attribution,
        DateOnly period,
        DateTimeOffset raisedAt,
        CancellationToken cancellationToken);
}
