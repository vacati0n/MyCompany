using MediaCompany.Application.Ports;
using MediaCompany.Credentials;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// An in-memory secret store standing in for the dedicated external store. It is a test double
/// for the port, not a widening of it: the production adapter is internal to the credentials
/// assembly and resolves from outside the process. Open question Q-005 — which store product is
/// provisioned — remains open, and risk R-018 carries the consequence.
/// </summary>
internal sealed class FakeSecretStore : ISecretStore
{
    private readonly Dictionary<CredentialHolderKey, string> _secrets = [];

    public int Resolutions { get; private set; }

    public void Publish(CredentialHolderKey holder, string secret) => _secrets[holder] = secret;

    /// <summary>A rotation. The next resolution sees the new value; nothing is restarted.</summary>
    public void Rotate(CredentialHolderKey holder, string newSecret) => _secrets[holder] = newSecret;

    /// <summary>A revocation. The next resolution sees nothing; nothing is restarted.</summary>
    public void Revoke(CredentialHolderKey holder) => _secrets.Remove(holder);

    public Task<string?> ResolveAsync(CredentialHolderKey holder, CancellationToken cancellationToken)
    {
        Resolutions++;
        return Task.FromResult(_secrets.GetValueOrDefault(holder));
    }
}

internal sealed class TestClock : IClock
{
    public TestClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }

    public void Advance(TimeSpan by) => UtcNow += by;
}

internal sealed class FakeRouteRegistry : IRouteRegistry
{
    public List<Route> Routes { get; } = [];

    public List<ForbiddenSource> Forbidden { get; } = [];

    public Task<IReadOnlyList<Route>> AdmittedRoutesAsync(CapabilityClass capability, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<Route>>(Routes.Where(r => r.Capability == capability).ToArray());

    public Task<IReadOnlyList<ForbiddenSource>> ForbiddenSourcesAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<ForbiddenSource>>(Forbidden.ToArray());
}

internal sealed class FakeAvailabilityLedger : IRouteAvailabilityLedger
{
    public Dictionary<RouteId, RouteAvailability> States { get; } = [];

    public Task<IReadOnlyDictionary<RouteId, RouteAvailability>> CurrentAsync(
        IReadOnlyCollection<RouteId> routes,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<RouteId, RouteAvailability>>(
            States.Where(kv => routes.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value));
}

internal sealed class FakeRegisters : IOperatingRegisters
{
    public List<ProviderAccount> Accounts { get; } = [];

    public List<Model> Models { get; } = [];

    public List<ModelPrice> Prices { get; } = [];

    public Task<IReadOnlyList<ProviderAccount>> ProviderAccountsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ProviderAccount>>(Accounts.ToArray());

    public Task<IReadOnlyList<Model>> ModelsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Model>>(Models.ToArray());

    public Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(DateTimeOffset asOf, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<ModelPrice>>(
            Prices.Where(p => p.ValidFrom <= asOf && (p.ValidTo is null || p.ValidTo > asOf)).ToArray());

    public Task<IReadOnlyList<Channel>> ChannelsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Channel>>([]);

    public Task<IReadOnlyList<Department>> DepartmentsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Department>>([]);

    public Task<IReadOnlyList<WorkforceAgent>> AgentsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<WorkforceAgent>>([]);
}

internal sealed class FakeBudgetReader : IBudgetReader
{
    public Money Remaining { get; set; } = new(100m);

    public Task<Money> RemainingAsync(Attribution attribution, DateOnly period, CancellationToken ct) =>
        Task.FromResult(Remaining);
}

/// <summary>
/// An in-memory unit of work. It reproduces the one property the production adapter exists to
/// give: nothing a transaction wrote is visible until it commits, and everything it wrote becomes
/// visible together. That is what makes the operation record and the audit entry a single fact.
/// </summary>
internal sealed class FakeUnitOfWork : IUnitOfWork
{
    public List<OperationRecord> Operations { get; } = [];

    public List<AuditEntry> AuditEntries { get; } = [];

    public List<BudgetAlert> Alerts { get; } = [];

    public int Commits { get; private set; }

    public int RolledBack { get; private set; }

    /// <summary>Availability transitions written but not yet committed.</summary>
    public List<RouteAvailability> PendingAvailability { get; } = [];

    /// <summary>Availability transitions that became durable.</summary>
    public List<RouteAvailability> CommittedAvailability { get; } = [];

    /// <summary>Set to make the next commit throw, standing in for a crash mid-transaction.</summary>
    public bool FailNextCommit { get; set; }

    public Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IWorkTransaction>(new Transaction(this));

    private sealed class Transaction : IWorkTransaction
    {
        private readonly FakeUnitOfWork _owner;
        private readonly List<OperationRecord> _pendingOperations = [];
        private readonly List<AuditEntry> _pendingEntries = [];
        private bool _committed;

        internal Transaction(FakeUnitOfWork owner)
        {
            _owner = owner;
            Audit = new Appender(_pendingEntries);
            Operations = new Recorder(_pendingOperations);
            Jobs = new Jobless();
            Budgets = new Budgeter(owner);
            Gates = new Gateless();
            Availability = new AvailabilityRecorder(owner);
            Dispatches = new Dispatchless();
        }

        public IAuditAppender Audit { get; }

        public IOperationRecorder Operations { get; }

        public IJobWriter Jobs { get; }

        public IBudgetEvaluator Budgets { get; }

        public IGateWriter Gates { get; }

        public IRouteAvailabilityWriter Availability { get; }

        public IDispatchWriter Dispatches { get; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            if (_owner.FailNextCommit)
            {
                _owner.FailNextCommit = false;
                throw new InvalidOperationException("induced crash between the operation and its record");
            }

            _owner.Operations.AddRange(_pendingOperations);
            _owner.AuditEntries.AddRange(_pendingEntries);
            _owner.CommittedAvailability.AddRange(_owner.PendingAvailability);
            _owner.PendingAvailability.Clear();
            _owner.Commits++;
            _committed = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                _owner.PendingAvailability.Clear();
                _owner.RolledBack++;
            }

            return ValueTask.CompletedTask;
        }

        private sealed class Appender(List<AuditEntry> sink) : IAuditAppender
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
                sink.Add(entry);
                return Task.FromResult(entry);
            }

            public Task<AuditEntry> AppendRefusalAsync(AuditEntryId target, AuditRefusalReason reason, string actor, CancellationToken ct) =>
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

        private sealed class Recorder(List<OperationRecord> sink) : IOperationRecorder
        {
            public Task<OperationRecord> RecordAsync(OperationDraft draft, CancellationToken ct)
            {
                // The production recorder lets the datastore compute the cost. Here the double
                // records zero, which is correct for every path this fake is used on: refusals,
                // holds and substitutes reach no provider and carry no units.
                var record = new OperationRecord
                {
                    Id = draft.Id,
                    Run = draft.Run,
                    Attribution = draft.Attribution,
                    Capability = draft.Capability,
                    Route = draft.Route,
                    Model = draft.Model,
                    DeterministicTaskName = draft.DeterministicTaskName,
                    Units = draft.Units,
                    AppliedPrice = null,
                    ComputedCost = Money.Zero(),
                    CostBasis = draft.CostBasis,
                    Duration = draft.Duration,
                    Outcome = draft.Outcome,
                    OccurredAt = draft.OccurredAt,
                    Attempt = draft.Attempt,
                    FailureReason = draft.FailureReason,

                    // Carried through exactly as the draft holds them. The served value is the one
                    // the boundary read from the admitting route, and a null stays null: a fake
                    // that filled it in from the request would hide the very thing under test.
                    ReasoningTierRequested = draft.ReasoningTierRequested,
                    ReasoningTierServed = draft.ReasoningTierServed,
                };
                sink.Add(record);
                return Task.FromResult(record);
            }
        }

        private sealed class Jobless : IJobWriter
        {
            public Task EnqueueAsync(Job job, CancellationToken ct) => Task.CompletedTask;

            public Task RecordStageAsync(JobStage stage, CancellationToken ct) => Task.CompletedTask;

            public Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken ct) =>
                Task.FromResult<Job?>(null);

            public Task ReleaseAsync(JobId job, ClaimState state, DateTimeOffset availableAt, CancellationToken ct) =>
                Task.CompletedTask;

            public Task AdvanceAsync(JobId job, LifecyclePosition position, ClaimState state,
                DateTimeOffset availableAt, CancellationToken ct) => Task.CompletedTask;
        }

        private sealed class Gateless : IGateWriter
        {
            public Task RecordApprovalAsync(MediaCompany.Domain.Publication.Approval approval, CancellationToken ct) =>
                Task.CompletedTask;

            public Task RecordBlockAsync(MediaCompany.Domain.Publication.Block block, CancellationToken ct) =>
                Task.CompletedTask;

            public Task RecordTransitionAsync(ItemId item, ItemVersion version,
                MediaCompany.Domain.Publication.GateState from, MediaCompany.Domain.Publication.GateState to,
                string reason, DateTimeOffset at, CancellationToken ct) => Task.CompletedTask;
        }

        /// <summary>
        /// Records the availability transition into the owner only on commit, which is how the
        /// double reproduces the property that the transition and the operation that caused it
        /// become durable together.
        /// </summary>
        private sealed class AvailabilityRecorder(FakeUnitOfWork owner) : IRouteAvailabilityWriter
        {
            public Task RecordAsync(RouteAvailability availability, CancellationToken ct)
            {
                owner.PendingAvailability.Add(availability);
                return Task.CompletedTask;
            }
        }

        private sealed class Budgeter(FakeUnitOfWork owner) : IBudgetEvaluator
        {
            public Task<IReadOnlyList<BudgetAlert>> EvaluateAsync(
                Attribution attribution, DateOnly period, DateTimeOffset raisedAt, CancellationToken ct) =>
                Task.FromResult<IReadOnlyList<BudgetAlert>>(owner.Alerts.ToArray());
        }
    }

    private sealed class Dispatchless : IDispatchWriter
    {
        public Task<DispatchWriteOutcome> RecordDispatchAsync(
            MediaCompany.Domain.Publication.DispatchRecord record, CancellationToken cancellationToken) =>
            throw new NotSupportedException(
                "The resolution boundary writes no publication dispatch; this fake exists to prove it is never called.");

        public Task RecordAttemptAsync(
            MediaCompany.Domain.Publication.AttemptRecord attempt, CancellationToken cancellationToken) =>
            throw new NotSupportedException(
                "The resolution boundary records no publication attempt.");
    }
}
