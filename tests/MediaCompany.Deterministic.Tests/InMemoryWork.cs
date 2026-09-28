using MediaCompany.Application.Ports;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Tests;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;

    public void Advance(TimeSpan by) => UtcNow += by;
}

/// <summary>
/// An in-memory unit of work that reproduces the one property the datastore adapter exists to
/// give: nothing a transaction wrote is visible until it commits, and everything it wrote becomes
/// visible together. The lifecycle and gate demonstrations below rest on that, because the
/// corrections they cover are about what commits with what.
/// </summary>
internal sealed class InMemoryWork : IUnitOfWork
{
    public Dictionary<JobId, Job> Jobs { get; } = [];

    public List<JobStage> Stages { get; } = [];

    public List<AuditEntry> AuditEntries { get; } = [];

    public List<Approval> Approvals { get; } = [];

    public List<Block> Blocks { get; } = [];

    public List<(GateState From, GateState To)> Transitions { get; } = [];

    /// <summary>Dispatch records that became durable, keyed as the datastore keys them.</summary>
    public List<DispatchRecord> Dispatches { get; } = [];

    /// <summary>Attempt records that became durable, refused attempts included.</summary>
    public List<AttemptRecord> Attempts { get; } = [];

    /// <summary>Dossier headers that became durable.</summary>
    public HashSet<(ItemId Item, ItemVersion Version)> DossierHeaders { get; } = [];

    /// <summary>Dossier rows that became durable, each with the item version it was recorded against.</summary>
    public List<(ItemId Item, ItemVersion Version, object Row)> DossierRows { get; } = [];

    public int Commits { get; private set; }

    public Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IWorkTransaction>(new Txn(this));

    private sealed class Txn : IWorkTransaction
    {
        private readonly InMemoryWork _owner;
        private readonly List<Action> _pending = [];
        private bool _committed;

        internal Txn(InMemoryWork owner)
        {
            _owner = owner;
            Audit = new Appender(_pending, owner);
            Operations = new NoOperations();
            Jobs = new JobWriter(_pending, owner);
            Budgets = new NoBudgets();
            Gates = new GateWriter(_pending, owner);
            Availability = new NoAvailability();
            Dispatches = new DispatchWriter(_pending, owner);
            Dossiers = new DossierWriter(_pending, owner);
        }

        public IAuditAppender Audit { get; }

        public IOperationRecorder Operations { get; }

        public IJobWriter Jobs { get; }

        public IBudgetEvaluator Budgets { get; }

        public IGateWriter Gates { get; }

        public IRouteAvailabilityWriter Availability { get; }

        public IDispatchWriter Dispatches { get; }

        public IDossierWriter Dossiers { get; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            foreach (var apply in _pending)
            {
                apply();
            }

            _owner.Commits++;
            _committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                _pending.Clear();
            }

            return ValueTask.CompletedTask;
        }

        private sealed class Appender(List<Action> pending, InMemoryWork owner) : IAuditAppender
        {
            public Task<AuditEntry> AppendAsync(AuditEntryDraft draft, CancellationToken ct)
            {
                var entry = new AuditEntry
                {
                    Id = AuditEntryId.New(),
                    Actor = draft.Actor,
                    Action = draft.Action,
                    Subject = draft.Subject,
                    OccurredAt = DateTimeOffset.UnixEpoch,
                    Reason = draft.Reason,
                    InputsReference = draft.InputsReference,
                    OutputsReference = draft.OutputsReference,
                    Decision = draft.Decision,
                    CostReference = draft.CostReference,
                    Risk = draft.Risk,
                    RetentionClass = draft.RetentionClass,
                    PreviousEntryHash = string.Empty,
                    EntryHash = Guid.NewGuid().ToString("N"),
                };

                pending.Add(() => owner.AuditEntries.Add(entry));
                return Task.FromResult(entry);
            }

            public Task<AuditEntry> AppendRefusalAsync(
                AuditEntryId target, AuditRefusalReason reason, string actor, CancellationToken ct) =>
                AppendAsync(
                    new AuditEntryDraft
                    {
                        Actor = actor,
                        Action = "record.amendment-refused",
                        Subject = $"audit-entry:{target}",
                        Reason = reason.ToString(),
                        InputsReference = $"audit-entry:{target}",
                        OutputsReference = "none",
                        Decision = "refused",
                        CostReference = "none",
                        Risk = "an attempt was made to alter the recorded history",
                        RetentionClass = RetentionClass.GovernanceRecord,
                    },
                    ct);
        }

        private sealed class JobWriter(List<Action> pending, InMemoryWork owner) : IJobWriter
        {
            public Task EnqueueAsync(Job job, CancellationToken ct)
            {
                pending.Add(() => owner.Jobs[job.Id] = job);
                return Task.CompletedTask;
            }

            public Task RecordStageAsync(JobStage stage, CancellationToken ct)
            {
                pending.Add(() => owner.Stages.Add(stage));
                return Task.CompletedTask;
            }

            public Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken ct)
            {
                var ready = owner.Jobs.Values.FirstOrDefault(j => j.ClaimState == ClaimState.Ready);
                if (ready is null)
                {
                    return Task.FromResult<Job?>(null);
                }

                var claimed = ready with { ClaimState = ClaimState.Claimed, ClaimedBy = workerId };
                pending.Add(() => owner.Jobs[claimed.Id] = claimed);
                return Task.FromResult<Job?>(claimed);
            }

            public Task<Job?> ClaimAsync(JobId job, string workerId, TimeSpan lease, CancellationToken ct)
            {
                // The named claim: only the unit named, only when it is ready. Availability is not
                // modelled here, because the double has no second clock to disagree with.
                if (!owner.Jobs.TryGetValue(job, out var named) || named.ClaimState != ClaimState.Ready)
                {
                    return Task.FromResult<Job?>(null);
                }

                var claimed = named with { ClaimState = ClaimState.Claimed, ClaimedBy = workerId };
                pending.Add(() => owner.Jobs[claimed.Id] = claimed);
                return Task.FromResult<Job?>(claimed);
            }

            public Task ReleaseAsync(JobId job, ClaimState state, DateTimeOffset availableAt, CancellationToken ct)
            {
                pending.Add(() => owner.Jobs[job] = owner.Jobs[job] with
                {
                    ClaimState = state,
                    AvailableAt = availableAt,
                    ClaimedBy = null,
                });
                return Task.CompletedTask;
            }

            public Task AdvanceAsync(
                JobId job, LifecyclePosition position, ClaimState state,
                DateTimeOffset availableAt, CancellationToken ct)
            {
                pending.Add(() => owner.Jobs[job] = owner.Jobs[job] with
                {
                    Position = position,
                    ClaimState = state,
                    AvailableAt = availableAt,
                    ClaimedBy = null,
                });
                return Task.CompletedTask;
            }
        }

        private sealed class GateWriter(List<Action> pending, InMemoryWork owner) : IGateWriter
        {
            public Task RecordApprovalAsync(Approval approval, CancellationToken ct)
            {
                pending.Add(() => owner.Approvals.Add(approval));
                return Task.CompletedTask;
            }

            public Task RecordBlockAsync(Block block, CancellationToken ct)
            {
                pending.Add(() => owner.Blocks.Add(block));
                return Task.CompletedTask;
            }

            public Task RecordTransitionAsync(
                ItemId item, ItemVersion version, GateState from, GateState to,
                string reason, DateTimeOffset at, CancellationToken ct)
            {
                if (!GateTransitionTable.IsAllowed(from, to))
                {
                    throw new InvalidOperationException($"{from} to {to} is not a transition in the gate table.");
                }

                pending.Add(() => owner.Transitions.Add((from, to)));
                return Task.CompletedTask;
            }
        }

        private sealed class NoOperations : IOperationRecorder
        {
            public Task<OperationRecord> RecordAsync(OperationDraft draft, CancellationToken ct) =>
                throw new NotSupportedException("The lifecycle and gate demonstrations record no operation.");
        }

        private sealed class NoBudgets : IBudgetEvaluator
        {
            public Task<IReadOnlyList<BudgetAlert>> EvaluateAsync(
                Attribution attribution, DateOnly period, DateTimeOffset raisedAt, CancellationToken ct) =>
                Task.FromResult<IReadOnlyList<BudgetAlert>>([]);
        }

        private sealed class NoAvailability : IRouteAvailabilityWriter
        {
            public Task RecordAsync(RouteAvailability availability, CancellationToken ct) => Task.CompletedTask;
        }
    }

    private sealed class DispatchWriter(List<Action> pending, InMemoryWork owner) : IDispatchWriter
    {
        public Task<DispatchWriteOutcome> RecordDispatchAsync(
            DispatchRecord record, CancellationToken cancellationToken)
        {
            // Resolved against what is already durable, so a retry inside a fresh transaction sees
            // the committed record exactly as the datastore's unique key would.
            if (owner.Dispatches.Any(d => d.Item.Equals(record.Item) && d.Version.Equals(record.Version)))
            {
                return Task.FromResult(DispatchWriteOutcome.AlreadyRecorded);
            }

            pending.Add(() => owner.Dispatches.Add(record));
            return Task.FromResult(DispatchWriteOutcome.Created);
        }

        public Task RecordAttemptAsync(AttemptRecord attempt, CancellationToken cancellationToken)
        {
            pending.Add(() => owner.Attempts.Add(attempt));
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// The dossier writer, reproducing the two refusals the datastore makes that the demonstrations
    /// here rest on: a component recorded against a dossier never opened is refused, and a dossier
    /// is opened once.
    /// </summary>
    private sealed class DossierWriter(List<Action> pending, InMemoryWork owner) : IDossierWriter
    {
        public Task OpenAsync(ItemId item, ItemVersion version, DateTimeOffset openedAt, CancellationToken ct)
        {
            if (owner.DossierHeaders.Contains((item, version)))
            {
                throw new InvalidOperationException("The dossier of this item version is already opened.");
            }

            pending.Add(() => owner.DossierHeaders.Add((item, version)));
            return Task.CompletedTask;
        }

        public Task RecordStageAsync(ItemId item, ItemVersion version, MediaCompany.Domain.Dossier.StageEvidence evidence, CancellationToken ct) =>
            Row(item, version, evidence);

        public Task RecordSupplyAuditAsync(ItemId item, ItemVersion version, MediaCompany.Domain.Dossier.SupplyAuditEntry entry, CancellationToken ct) =>
            Row(item, version, entry);

        public Task RecordDeterminationAsync(ItemId item, ItemVersion version, MediaCompany.Domain.Dossier.DeterminationResolution resolution, CancellationToken ct) =>
            Row(item, version, resolution);

        public Task RecordComponentAsync(ItemId item, ItemVersion version, DossierComponent component, DateTimeOffset recordedAt, CancellationToken ct) =>
            Row(item, version, component);

        private Task Row(ItemId item, ItemVersion version, object row)
        {
            if (!owner.DossierHeaders.Contains((item, version)))
            {
                throw new InvalidOperationException("No dossier is opened for this item version.");
            }

            pending.Add(() => owner.DossierRows.Add((item, version, row)));
            return Task.CompletedTask;
        }
    }
}
