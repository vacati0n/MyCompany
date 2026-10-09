using MediaCompany.Application.Ports;
using MediaCompany.Credentials;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
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

/// <summary>
/// What the datastore would answer an admission with (the AI-economics change): the booking instant it
/// reserves, the prices in force, the governing readings of the booking month and the evidence. It
/// stands in for the datastore's ONE CLOCK: the reserved instant is set here, never read from a process
/// clock, so a demonstration can set the process clock anywhere and see which one the boundary used.
/// </summary>
internal sealed class FakeAdmission
{
    /// <summary>The booking instant the datastore reserves. Its month is the booking month.</summary>
    public DateTimeOffset Instant { get; set; } = DateTimeOffset.Parse("2026-10-01T12:00:00Z");

    public DateOnly Month => new(Instant.UtcDateTime.Year, Instant.UtcDateTime.Month, 1);

    /// <summary>The channel's recorded budget amount for the booking month, or null where none is recorded.</summary>
    public Money? ChannelBudget { get; set; } = new(100m);

    public ScopeSpend ChannelSpend { get; set; } = new(0, 0, Money.Zero());

    public ScopeSpend CompanySpend { get; set; } = new(0, 0, Money.Zero());

    public Money Headroom { get; set; } = new(100m);

    public CompanyId? Company { get; set; } = CompanyId.New();

    public List<ModelPrice> Prices { get; } = [];

    public List<BenchmarkObservation> Evidence { get; } = [];

    public int Reservations { get; set; }

    public int GoverningReads { get; set; }

    public int Snapshots { get; set; }

    /// <summary>The scope holds taken, in the order they were taken.</summary>
    public List<string> Holds { get; } = [];

    /// <summary>Whether another admission holds the company scope, so a try at it is refused (the second correction cycle).</summary>
    public bool CompanyScopeHeldElsewhere { get; set; }

    /// <summary>The price sets restored on a fresh transaction after a lost one.</summary>
    public List<IReadOnlyList<ModelPrice>> RestoredPrices { get; } = [];

    /// <summary>The route register the snapshot reads routes and forbidden sources from.</summary>
    public IRouteRegistry? RouteSource { get; set; }

    /// <summary>The availability ledger the snapshot reads availability from.</summary>
    public IRouteAvailabilityLedger? AvailabilitySource { get; set; }

    /// <summary>The registers the snapshot reads accounts from.</summary>
    public IOperatingRegisters? RegisterSource { get; set; }

    /// <summary>Set to make the next operation write fail, standing in for a transaction lost after the provider call.</summary>
    public bool LoseNextOperationWrite { get; set; }

    /// <summary>Puts a full price row set (input, output and cached) in force for a model.</summary>
    public void Price(ModelId model, decimal input, decimal output, decimal cached, DateTimeOffset from)
    {
        foreach (var (kind, price) in new[] { (PriceUnitKind.InputUnit, input), (PriceUnitKind.OutputUnit, output), (PriceUnitKind.CachedUnit, cached) })
        {
            Prices.Add(new ModelPrice(
                ModelPriceId.New(), model, kind, price, "USD", "ESTIMATE, a demonstration parameter",
                DateOnly.FromDateTime(from.UtcDateTime), from, null));
        }
    }

    /// <summary>The utilisation the datastore computes: booked times one hundred over the amount, to four places.</summary>
    internal static decimal? Percent(Money? amount, ScopeSpend spend) =>
        amount is { } a ? Math.Round(spend.Booked.Amount * 100m / a.Amount, 4) : null;
}

/// <summary>The admission ledger over <see cref="FakeAdmission"/>, on one transaction.</summary>
internal sealed class FakeAdmissionLedger(FakeAdmission state, List<AdmissionDecisionDraft> pending) : IAdmissionLedger
{
    private BookingReservation? _reservation;

    public Task<BookingReservation> ReserveAsync(CancellationToken cancellationToken)
    {
        if (_reservation is null)
        {
            state.Reservations++;
            _reservation = new BookingReservation(state.Instant, state.Month);
        }

        return Task.FromResult(_reservation);
    }

    public Task<bool> TryHoldScopesAsync(ChannelId channel, CancellationToken cancellationToken)
    {
        // The order the datastore adapter tries the holds in, recorded so a demonstration can read it. Where
        // the demonstration says another admission holds the company scope, the first try is refused.
        state.Holds.Add("company");
        if (state.CompanyScopeHeldElsewhere)
        {
            return Task.FromResult(false);
        }

        state.Holds.Add($"channel:{channel}");
        return Task.FromResult(true);
    }

    public async Task<AdmissionSnapshot> ReadAsync(
        CapabilityClass capability, Attribution attribution, Money companyAllotment, TaskClass? taskClass, CancellationToken cancellationToken)
    {
        state.Snapshots++;
        var at = Reserved().Instant;
        var routes = state.RouteSource is null
            ? (IReadOnlyList<Route>)[]
            : await state.RouteSource.AdmittedRoutesAsync(capability, cancellationToken);
        var models = routes.Select(r => r.Target).OfType<RouteTarget.ProviderRoute>().Select(p => p.Model).ToHashSet();

        return new AdmissionSnapshot(
            routes,
            state.RouteSource is null ? [] : await state.RouteSource.ForbiddenSourcesAsync(cancellationToken),
            state.AvailabilitySource is null
                ? new Dictionary<RouteId, RouteAvailability>()
                : await state.AvailabilitySource.CurrentAsync(routes.Select(r => r.Id).ToArray(), cancellationToken),
            state.RegisterSource is null ? [] : await state.RegisterSource.ProviderAccountsAsync(cancellationToken),
            state.Prices.Where(p => models.Contains(p.Model) && p.ValidFrom <= at && (p.ValidTo is null || p.ValidTo > at)).ToArray(),
            await GoverningReadingsAsync(attribution, companyAllotment, cancellationToken),
            taskClass is { } task
                ? state.Evidence.Where(o => routes.Any(r => r.Id == o.Route) && o.TaskClass == task && o.ObservedAt <= at).ToArray()
                : []);
    }

    public Task RestorePricesAsync(IReadOnlyList<ModelPrice> prices, CancellationToken cancellationToken)
    {
        state.RestoredPrices.Add(prices);
        return Task.CompletedTask;
    }

    public Task<GoverningReadingsSummary> GoverningReadingsAsync(Attribution attribution, Money companyAllotment, CancellationToken cancellationToken)
    {
        state.GoverningReads++;
        return Task.FromResult(new GoverningReadingsSummary(
            Reserved().Month,
            attribution.Channel,
            state.ChannelBudget,
            state.ChannelSpend,
            FakeAdmission.Percent(state.ChannelBudget, state.ChannelSpend),
            state.Company,
            state.CompanySpend,
            FakeAdmission.Percent(companyAllotment, state.CompanySpend),
            state.Headroom));
    }

    public Task RecordDecisionAsync(AdmissionDecisionDraft decision, CancellationToken cancellationToken)
    {
        Reserved();
        pending.Add(decision);
        return Task.CompletedTask;
    }

    private BookingReservation Reserved() =>
        _reservation ?? throw new InvalidOperationException("No booking instant is reserved on this transaction.");
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

    /// <summary>What the datastore answers an admission with (the AI-economics change).</summary>
    public FakeAdmission Admission { get; } = new();

    /// <summary>The decision records that became durable.</summary>
    public List<AdmissionDecisionDraft> Decisions { get; } = [];

    /// <summary>The booked month each budget evaluation was asked for.</summary>
    public List<DateOnly> EvaluatedMonths { get; } = [];

    /// <summary>The stored alerts read back, by booked month and instant, for a recovered outcome.</summary>
    public List<(DateOnly Period, DateTimeOffset RaisedAt)> StoredAlertReads { get; } = [];

    public int Commits { get; private set; }

    public int RolledBack { get; private set; }

    /// <summary>Availability transitions written but not yet committed.</summary>
    public List<RouteAvailability> PendingAvailability { get; } = [];

    /// <summary>Availability transitions that became durable.</summary>
    public List<RouteAvailability> CommittedAvailability { get; } = [];

    /// <summary>Set to make the next commit throw, standing in for a crash mid-transaction.</summary>
    public bool FailNextCommit { get; set; }

    /// <summary>
    /// Set to make the next commit REACH THE STORE and then throw, standing in for a commit whose reply was
    /// lost: what it wrote is durable, and the caller cannot know it.
    /// </summary>
    public bool LoseNextCommitReply { get; set; }

    /// <summary>Binds the read ports the admission snapshot is composed from (the correction cycle).</summary>
    public FakeUnitOfWork Bind(IRouteRegistry routes, IRouteAvailabilityLedger availability, IOperatingRegisters registers)
    {
        Admission.RouteSource = routes;
        Admission.AvailabilitySource = availability;
        Admission.RegisterSource = registers;
        return this;
    }

    public Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IWorkTransaction>(new Transaction(this));

    private sealed class Transaction : IWorkTransaction
    {
        private readonly FakeUnitOfWork _owner;
        private readonly List<OperationRecord> _pendingOperations = [];
        private readonly List<AuditEntry> _pendingEntries = [];
        private readonly List<AdmissionDecisionDraft> _pendingDecisions = [];
        private bool _committed;

        internal Transaction(FakeUnitOfWork owner)
        {
            _owner = owner;
            Audit = new Appender(_pendingEntries);
            Operations = new Recorder(_pendingOperations, owner);
            Admission = new FakeAdmissionLedger(owner.Admission, _pendingDecisions);
            Benchmarks = new Benchmarkless();
            Jobs = new Jobless();
            Budgets = new Budgeter(owner);
            Gates = new Gateless();
            Availability = new AvailabilityRecorder(owner);
            Dispatches = new Dispatchless();
            Dossiers = new Dossierless();
        }

        public IAuditAppender Audit { get; }

        public IOperationRecorder Operations { get; }

        public IJobWriter Jobs { get; }

        public IBudgetEvaluator Budgets { get; }

        public IGateWriter Gates { get; }

        public IRouteAvailabilityWriter Availability { get; }

        public IDispatchWriter Dispatches { get; }

        public IDossierWriter Dossiers { get; }

        public IAdmissionLedger Admission { get; }

        public IBenchmarkWriter Benchmarks { get; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            if (_owner.FailNextCommit)
            {
                _owner.FailNextCommit = false;
                throw new InvalidOperationException("induced crash between the operation and its record");
            }

            _owner.Operations.AddRange(_pendingOperations);
            _owner.AuditEntries.AddRange(_pendingEntries);
            _owner.Decisions.AddRange(_pendingDecisions);
            _owner.CommittedAvailability.AddRange(_owner.PendingAvailability);
            _owner.PendingAvailability.Clear();
            _owner.Commits++;
            _committed = true;

            if (_owner.LoseNextCommitReply)
            {
                _owner.LoseNextCommitReply = false;
                throw new IOException("induced loss of the commit's reply after the commit reached the store");
            }

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

        private sealed class Recorder(List<OperationRecord> sink, FakeUnitOfWork owner) : IOperationRecorder
        {
            public Task<OperationRecord> RecordAsync(OperationDraft draft, CancellationToken ct)
            {
                if (owner.Admission.LoseNextOperationWrite)
                {
                    owner.Admission.LoseNextOperationWrite = false;
                    throw new InvalidOperationException("induced loss of the admission transaction after the provider call");
                }

                // The datastore's key on the operation identifier: a second write under one identifier books
                // nothing and says the attempt is already recorded (the correction cycle).
                if (owner.Operations.FirstOrDefault(o => o.Id == draft.Id) is { } recorded)
                {
                    throw new OperationAlreadyRecordedException(recorded);
                }

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

                    // The datastore books at the instant the transaction reserved; the draft's instant is not
                    // stored. The double stands in for the datastore here, exactly as the recorder does.
                    OccurredAt = owner.Admission.Instant,
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
            public Task<DateTimeOffset> EnqueueAsync(Job job, TimeSpan claimableAfter, CancellationToken ct) =>
                Task.FromResult(DateTimeOffset.UnixEpoch);

            public Task RecordStageAsync(JobStage stage, CancellationToken ct) => Task.CompletedTask;

            public Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken ct) =>
                Task.FromResult<Job?>(null);

            public Task<Job?> ClaimAsync(JobId job, string workerId, TimeSpan lease, CancellationToken ct) =>
                Task.FromResult<Job?>(null);

            public Task<DateTimeOffset> ReleaseAsync(JobId job, ClaimState state, TimeSpan claimableAfter, CancellationToken ct) =>
                Task.FromResult(DateTimeOffset.UnixEpoch);

            public Task<DateTimeOffset> AdvanceAsync(JobId job, LifecyclePosition position, ClaimState state,
                TimeSpan claimableAfter, CancellationToken ct) => Task.FromResult(DateTimeOffset.UnixEpoch);
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
                Attribution attribution, DateOnly period, DateTimeOffset raisedAt, CancellationToken ct)
            {
                owner.EvaluatedMonths.Add(period);
                return Task.FromResult<IReadOnlyList<BudgetAlert>>(owner.Alerts.ToArray());
            }

            public Task<IReadOnlyList<BudgetAlert>> StoredAlertsAsync(
                Attribution attribution, DateOnly period, DateTimeOffset raisedAt, CancellationToken ct)
            {
                owner.StoredAlertReads.Add((period, raisedAt));
                return Task.FromResult<IReadOnlyList<BudgetAlert>>(owner.Alerts.ToArray());
            }
        }

        private sealed class Benchmarkless : IBenchmarkWriter
        {
            public Task<DateTimeOffset> RegisterEntryAsync(CorpusEntryId entry, TaskClass taskClass, CancellationToken ct) =>
                throw new NotSupportedException("The resolution boundary registers no corpus entry.");

            public Task<BenchmarkObservation> RecordObservationAsync(BenchmarkObservationDraft draft, CancellationToken ct) =>
                throw new NotSupportedException("The resolution boundary records no benchmark observation.");
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

    /// <summary>The resolution boundary writes no dossier; this fake exists to prove it is never called.</summary>
    private sealed class Dossierless : IDossierWriter
    {
        public Task OpenAsync(ItemId item, ItemVersion version, DateTimeOffset openedAt, CancellationToken ct) =>
            throw new NotSupportedException("The resolution boundary opens no dossier.");

        public Task RecordStageAsync(ItemId item, ItemVersion version,
            MediaCompany.Domain.Dossier.StageEvidence evidence, CancellationToken ct) =>
            throw new NotSupportedException("The resolution boundary records no stage evidence.");

        public Task RecordSupplyAuditAsync(ItemId item, ItemVersion version,
            MediaCompany.Domain.Dossier.SupplyAuditEntry entry, CancellationToken ct) =>
            throw new NotSupportedException("The resolution boundary records no supply audit.");

        public Task RecordDeterminationAsync(ItemId item, ItemVersion version,
            MediaCompany.Domain.Dossier.DeterminationResolution resolution, CancellationToken ct) =>
            throw new NotSupportedException("The resolution boundary records no determination.");

        public Task RecordComponentAsync(ItemId item, ItemVersion version, DossierComponent component,
            DateTimeOffset recordedAt, CancellationToken ct) =>
            throw new NotSupportedException("The resolution boundary records no dossier component.");
    }
}
