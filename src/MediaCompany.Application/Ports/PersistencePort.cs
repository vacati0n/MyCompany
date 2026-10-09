using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Dossier;
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

    /// <summary>
    /// The item dossier: its header, its typed components and its payload components.
    ///
    /// Reachable ONLY from a transaction, exactly as every other writer is, so a recorded
    /// component and the audit entry that records it commit together, and a component the
    /// datastore refuses leaves neither behind.
    /// </summary>
    IDossierWriter Dossiers { get; }

    /// <summary>
    /// The admission ledger (the AI-economics change, decision D-001 of its design): the booking
    /// instant this transaction reserves, the prices, governing readings and evidence read at it, and
    /// the decision record written on it.
    ///
    /// Reachable ONLY from a transaction, because the reservation IS a property of the transaction: the
    /// shared hold on the record horizon that keeps the reserved instant bookable lasts exactly as long
    /// as the transaction does, and the reserved instant is held by the datastore for this transaction
    /// alone, so no caller code carries it.
    /// </summary>
    IAdmissionLedger Admission { get; }

    /// <summary>
    /// The benchmark record's writer (decision D-002 of the AI-economics design), reachable only from a
    /// transaction so an observation and the operation it references commit together or not at all.
    /// </summary>
    IBenchmarkWriter Benchmarks { get; }

    Task CommitAsync(CancellationToken cancellationToken);
}

// ---------------------------------------------------------------------------
// The AI-economics change: the admission ledger and the benchmark writer
// ---------------------------------------------------------------------------

/// <summary>
/// One admission's ledger on its transaction (decision D-001 of the AI-economics design).
///
/// ONE BOOKING INSTANT, THE DATASTORE'S. <see cref="ReserveAsync"/> is the first statement of an
/// admission: it reserves the later of the datastore's clock and the record horizon, under a SHARED
/// hold on the horizon kept to the end of the transaction, exactly as the operation recorder stamps,
/// and holds the instant in a transaction-local datastore setting. Every other member reads AT that
/// instant, the operation recorder books at it, and the decision record is stamped with it, so the
/// prices, the governing readings, the evidence, the operation and its decision are on one clock and in
/// one month. Because the horizon is held shared from the reservation to the commit, and every closure
/// takes it exclusively without waiting, no closure can pass the reserved instant meanwhile, so the
/// month the operation is admitted against is the month it is booked into.
///
/// ONE SNAPSHOT (the correction cycle): <see cref="ReadAsync"/> reads the routes, the forbidden sources,
/// the availability, the accounts, the prices in force, the governing readings and the evidence in ONE
/// STATEMENT, so the estimate, the headroom it is compared with and the controller's readings come from
/// one consistent read; and it captures the prices it read on the transaction, so the operation recorder
/// applies those prices rather than looking them up again in the editable price register.
///
/// SERIALISED PER SCOPE (the correction cycle, on the ruling that no concurrent overshoot is accepted):
/// <see cref="HoldScopesAsync"/> takes the company scope's hold and then the channel scope's, always in
/// that order, each held to the end of the transaction, so two metered admissions of one scope cannot
/// both read spend that excludes the other's booking.
/// </summary>
public interface IAdmissionLedger
{
    /// <summary>
    /// Reserves the booking instant and returns it with its month. Calling it again on the same
    /// transaction returns the same reservation.
    /// </summary>
    Task<BookingReservation> ReserveAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Takes the company scope's admission hold and then the attribution channel's, in that fixed order,
    /// each held to the end of this transaction. A metered admission takes both before the read it decides
    /// on, so admissions of one scope are serialised and no deadlock between them is possible.
    /// </summary>
    Task HoldScopesAsync(ChannelId channel, CancellationToken cancellationToken);

    /// <summary>
    /// Everything one admission decides by, in ONE STATEMENT at the reserved instant: the capability's
    /// admitted routes, the forbidden sources, each route's current availability, the provider accounts,
    /// the prices in force for the routes' models, the governing readings of the attribution with the
    /// company's metered allotment capping the headroom, and, where a task class is named, the evidence.
    /// The prices read are captured on the transaction for the operation recorder.
    /// </summary>
    Task<AdmissionSnapshot> ReadAsync(
        CapabilityClass capability,
        Attribution attribution,
        Money companyAllotment,
        TaskClass? taskClass,
        CancellationToken cancellationToken);

    /// <summary>
    /// Captures price rows read by an earlier admission snapshot on THIS transaction, so an attempt recorded
    /// on a fresh transaction after a lost one applies the prices it was admitted at.
    /// </summary>
    Task RestorePricesAsync(IReadOnlyList<ModelPrice> prices, CancellationToken cancellationToken);

    /// <summary>
    /// The governing readings of one attribution in the booking month, in one statement at the reserved
    /// instant: the channel's recorded budget amount, its booked spend and utilisation; the company's
    /// booked spend and its utilisation of <paramref name="companyAllotment"/>; and the delivered
    /// headroom over the governing channel and department budgets. Every figure is the datastore's.
    /// </summary>
    Task<GoverningReadingsSummary> GoverningReadingsAsync(
        Attribution attribution,
        Money companyAllotment,
        CancellationToken cancellationToken);

    /// <summary>
    /// Writes the decision record of one operation recorded on this transaction: the header stamped
    /// with the reserved instant and month, one row per governing reading, one per candidate and one per
    /// observation ranked on. The datastore refuses a decision whose instant is not its operation's.
    /// </summary>
    Task RecordDecisionAsync(AdmissionDecisionDraft decision, CancellationToken cancellationToken);
}

/// <summary>The booking instant a transaction reserved, and its month (the first day, UTC).</summary>
public sealed record BookingReservation(DateTimeOffset Instant, DateOnly Month);

/// <summary>
/// Everything one admission decides by, as one statement read it at the reserved instant (the
/// AI-economics change, correction cycle).
/// </summary>
public sealed record AdmissionSnapshot(
    IReadOnlyList<Route> Routes,
    IReadOnlyList<ForbiddenSource> ForbiddenSources,
    IReadOnlyDictionary<RouteId, RouteAvailability> Availability,
    IReadOnlyList<ProviderAccount> Accounts,
    IReadOnlyList<ModelPrice> Prices,
    GoverningReadingsSummary Readings,
    IReadOnlyList<BenchmarkObservation> Evidence);

/// <summary>
/// An operation is already recorded under the identifier a writer presented (the AI-economics change,
/// correction cycle): one provider attempt is recorded under ONE identifier minted before the call, so a
/// retry after a commit whose outcome was unknown meets the first record rather than booking a second.
/// The stored record is carried, and the writing transaction is left usable.
/// </summary>
public sealed class OperationAlreadyRecordedException : InvalidOperationException
{
    public OperationAlreadyRecordedException(OperationRecord recorded)
        : base($"Operation {recorded?.Id} is already recorded; one attempt is booked once.")
    {
        ArgumentNullException.ThrowIfNull(recorded);
        Recorded = recorded;
    }

    /// <summary>The record already stored under the identifier.</summary>
    public OperationRecord Recorded { get; }
}

/// <summary>
/// One scope's booked operations in a booking month, as the datastore aggregated them: how many, how
/// many of them carry a cost that is not stated, and the booked cost.
/// </summary>
public sealed record ScopeSpend(long Operations, long UnstatedOperations, Money Booked);

/// <summary>
/// The governing readings of one attribution in one booking month, BARE, exactly as the datastore
/// computed them in one statement. Nothing here decides a measurement case or an action: the cost
/// controller decides both, at one site.
/// </summary>
public sealed record GoverningReadingsSummary(
    DateOnly Month,
    ChannelId Channel,
    Money? ChannelBudgetAmount,
    ScopeSpend ChannelSpend,
    decimal? ChannelUtilisationPercent,
    CompanyId? Company,
    ScopeSpend CompanySpend,
    decimal? CompanyUtilisationPercent,
    Money Headroom);

/// <summary>
/// The decision record of one admission. Its instant and month are not carried: the ledger stamps the
/// header with the reserved instant the datastore holds for the transaction.
/// </summary>
public sealed record AdmissionDecisionDraft
{
    public required OperationId Operation { get; init; }
    public required ControllerDecision Controller { get; init; }
    public required SelectionRecord Selection { get; init; }

    /// <summary>The resolved route's tier outcome, or null where no route was resolved.</summary>
    public TierOutcome? TierOutcome { get; init; }

    /// <summary>What the tier rule did, including where no downgrade was available and why. Required.</summary>
    public required string TierStatement { get; init; }

    /// <summary>Whether the admitted month's reservation held to the booking, or was lost and re-taken. Required.</summary>
    public required string ReservationStatement { get; init; }
}

/// <summary>
/// The benchmark record's writer (decision D-002 of the AI-economics design). There is no update and
/// no delete: every row is written once, and the datastore refuses an amendment for every writer.
/// </summary>
public interface IBenchmarkWriter
{
    /// <summary>
    /// Registers a corpus entry: a REFERENCE ONLY, its identifier and task class, stamped by the
    /// datastore. There is no member taking content, because the register has no column to hold it.
    /// </summary>
    Task<DateTimeOffset> RegisterEntryAsync(CorpusEntryId entry, TaskClass taskClass, CancellationToken cancellationToken);

    /// <summary>
    /// Records one observation of an operation already recorded. Its route and model are the operation's
    /// own, its cost and latency are read through the operation, and its instant is the datastore's.
    /// </summary>
    Task<BenchmarkObservation> RecordObservationAsync(BenchmarkObservationDraft draft, CancellationToken cancellationToken);
}

/// <summary>
/// The caller-supplied part of an observation: the entry, its task class, the operation and the
/// quality in its case. It carries no cost, no latency, no route, no model and no instant.
/// </summary>
public sealed record BenchmarkObservationDraft
{
    public required BenchmarkObservationId Id { get; init; }
    public required CorpusEntryId Entry { get; init; }
    public required TaskClass TaskClass { get; init; }
    public required OperationId Operation { get; init; }
    public required MeasurementQuantity Quality { get; init; }
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
/// A gate transition refused because the state it claims to start from is not the item version's
/// latest recorded state (the multi-channel change, decision D-008 of its design).
///
/// The record store refuses such a transition for every writer; the gate writer reports the refusal
/// as this type, so a gate decision can name it rather than surfacing a datastore error. The
/// transaction that attempted the write is rolled back, so nothing it wrote is durable.
/// </summary>
public sealed class GateStateConflictException : InvalidOperationException
{
    public GateStateConflictException(string message, Exception? inner = null)
        : this(GateConflictReason.FromStateNotRecorded, message, inner)
    {
    }

    public GateStateConflictException(GateConflictReason reason, string message, Exception? inner = null)
        : base(message, inner)
    {
        Reason = reason;
    }

    /// <summary>Which of the datastore's gate checks refused the transition (the AI-economics change).</summary>
    public GateConflictReason Reason { get; }
}

/// <summary>Why the record store refused a gate transition. Each is its own named check.</summary>
public enum GateConflictReason
{
    /// <summary>The transition claimed a from-state that is not the item version's latest recorded state.</summary>
    FromStateNotRecorded = 1,

    /// <summary>
    /// A transition of the item version is already recorded at the same instant (the AI-economics
    /// change, decision D-009 of its design); the first admitted under the item hold is the one recorded.
    /// </summary>
    InstantAlreadyRecorded = 2,
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

    /// <summary>
    /// No dispatch record was written and none exists, because the attempt was refused before a
    /// descriptor was composed.
    ///
    /// Distinct from <see cref="AlreadyRecorded"/>, which asserts a record IS there. Reporting a
    /// refusal as "already recorded" would tell a reader a dispatch exists for this item version
    /// when none does, and the two are not the same fact.
    /// </summary>
    NoRecordWritten = 3,
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
    ///
    /// THE DATASTORE BOOKS THE OPERATION (the multi-channel change, decision D-006 of its design).
    /// Its instant is stamped by the datastore as the later of its own clock and the record horizon,
    /// under a shared hold on the horizon kept to the end of this transaction, and it is booked into
    /// that instant's month. The returned record carries the STORED instant, which is the one a
    /// month reading and a budget evaluation use; the draft's instant is the caller's observation
    /// and is not stored.
    ///
    /// WHERE THE TRANSACTION HOLDS A RESERVATION (the AI-economics change, decision D-001 of its
    /// design) the operation is booked AT THE RESERVED INSTANT, so it lands in the month it was admitted
    /// against; elsewhere it is stamped exactly as above. The record also states whether its cost is
    /// stated: not where a consumed input, output or cached unit had no price row in force at the
    /// booking instant, or any other unit was consumed, so a missing price never reads as a stated zero.
    ///
    /// Where the transaction captured prices at admission (the correction cycle) the recorder applies the
    /// captured prices, not a later lookup in the editable price register. Where an operation is already
    /// recorded under the draft's identifier it books nothing and raises
    /// <see cref="OperationAlreadyRecordedException"/> carrying the stored record.
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

/// <summary>
/// The durable job queue as a transactional claim table (module M-013 over M-017).
///
/// ONE CLOCK DECIDES CLAIMABILITY. Every member that makes a unit claimable states a DELAY until it
/// is claimable — zero, or a backoff — and the datastore sets the availability instant from its own
/// clock plus that delay, returning it. No member accepts an availability instant: the claim and the
/// lease are decided on the datastore's clock, so an instant set on any other clock could leave a
/// unit made available at once unclaimable at once.
/// </summary>
public interface IJobWriter
{
    /// <summary>
    /// Enqueues a unit, claimable after <paramref name="claimableAfter"/> by the datastore's clock.
    /// The unit's own availability instant is not written: the datastore sets it, and returns it.
    /// </summary>
    Task<DateTimeOffset> EnqueueAsync(Job job, TimeSpan claimableAfter, CancellationToken cancellationToken);

    Task RecordStageAsync(JobStage stage, CancellationToken cancellationToken);

    /// <summary>
    /// Claims one ready job for <paramref name="workerId"/>, or returns null when none is ready.
    /// The claim is part of this transaction, so a crash between the claim and the work it
    /// authorises leaves the job ready rather than lost.
    /// </summary>
    Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken cancellationToken);

    /// <summary>
    /// Claims the NAMED unit for <paramref name="workerId"/>, only when it is ready and available,
    /// or returns null. The same skip-locked discipline and the same datastore clock as
    /// <see cref="ClaimNextAsync"/>, so a caller driving one unit claims that unit and no other.
    /// </summary>
    Task<Job?> ClaimAsync(JobId job, string workerId, TimeSpan lease, CancellationToken cancellationToken);

    /// <summary>Releases a claim into <paramref name="state"/>, claimable after the stated delay; returns the instant set.</summary>
    Task<DateTimeOffset> ReleaseAsync(JobId job, ClaimState state, TimeSpan claimableAfter, CancellationToken cancellationToken);

    /// <summary>
    /// Moves a unit to its next lifecycle position and claim state together. A unit whose
    /// workflow has no next position reaches a terminal claim state here, which is what stops a
    /// finished unit from being claimed again.
    /// </summary>
    Task<DateTimeOffset> AdvanceAsync(
        JobId job,
        MediaCompany.Domain.Work.LifecyclePosition position,
        ClaimState state,
        TimeSpan claimableAfter,
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

/// <summary>
/// The item dossier's writer (decision D-004 of the accepted design), reachable only from a
/// transaction.
///
/// A header row per item version is written once, when the dossier is opened, and is the
/// observation boundary: every component is recorded against an opened dossier, and a component
/// recorded against one that was never opened is refused by the datastore. Stage evidence,
/// supply-audit entries and determination resolutions have typed homes whose table checks admit
/// exactly the row shapes the domain rules admit; the remaining components are held one row per
/// recorded component as one structured payload. Every row is written once.
/// </summary>
public interface IDossierWriter
{
    /// <summary>Opens the dossier of one item version. A second opening of the same version is refused.</summary>
    Task OpenAsync(ItemId item, ItemVersion version, DateTimeOffset openedAt, CancellationToken cancellationToken);

    /// <summary>Records one production stage's outcome. One outcome per stage per item version.</summary>
    Task RecordStageAsync(ItemId item, ItemVersion version, StageEvidence evidence, CancellationToken cancellationToken);

    /// <summary>
    /// Records one supply-audit entry, holding its count exactly as the entry holds it: a zero as
    /// zero and an absent count as absent.
    /// </summary>
    Task RecordSupplyAuditAsync(ItemId item, ItemVersion version, SupplyAuditEntry entry, CancellationToken cancellationToken);

    /// <summary>Records one determination's resolution. One resolution per determination per item version.</summary>
    Task RecordDeterminationAsync(
        ItemId item,
        ItemVersion version,
        DeterminationResolution resolution,
        CancellationToken cancellationToken);

    /// <summary>Records one payload component. The four singleton kinds are unique per item version.</summary>
    Task RecordComponentAsync(
        ItemId item,
        ItemVersion version,
        DossierComponent component,
        DateTimeOffset recordedAt,
        CancellationToken cancellationToken);
}

/// <summary>
/// The closed set of dossier components held as one structured payload each. The four quantity-
/// free singletons are unique per item version; the other four are one row per recorded entry.
/// </summary>
public enum DossierComponentKind
{
    TreatmentVerdict = 1,
    AudienceDesignation = 2,
    VisualProvenance = 3,
    ClipOriginAssessment = 4,
    ClaimAttribution = 5,
    ItemMetadata = 6,
    OriginalityAssessment = 7,
    Runtime = 8,
}

/// <summary>
/// One payload component of the item dossier. A closed union: each case carries exactly the
/// delivered domain value it records, and there is no case a writer could fill with anything else.
/// </summary>
public abstract record DossierComponent
{
    private DossierComponent()
    {
    }

    public abstract DossierComponentKind Kind { get; }

    public sealed record Treatment(TreatmentVerdict Verdict) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.TreatmentVerdict;
    }

    public sealed record Audience(AudienceDesignation Designation) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.AudienceDesignation;
    }

    public sealed record Visual(VisualProvenance Provenance) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.VisualProvenance;
    }

    public sealed record ClipOrigin(ClipOriginAssessment Assessment) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.ClipOriginAssessment;
    }

    public sealed record Claim(ClaimAttribution Attribution) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.ClaimAttribution;
    }

    public sealed record Metadata(ItemMetadata Surfaces) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.ItemMetadata;
    }

    public sealed record Originality(FootageRemovalAssessment Assessment) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.OriginalityAssessment;
    }

    public sealed record Runtime(TimeSpan Duration) : DossierComponent
    {
        public override DossierComponentKind Kind => DossierComponentKind.Runtime;
    }
}
