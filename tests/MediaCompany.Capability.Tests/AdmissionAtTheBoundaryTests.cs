using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Deterministic;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// The admission at the boundary (the AI-economics change, decision D-001 of its design) over in-memory
/// ports: one reserved instant per admission, the cost controller's decision recorded beside every
/// outcome, and a lost reservation recorded on a fresh one. The reserved instant stands in for the
/// datastore's clock and is set apart from the process clock, so a demonstration can tell which one the
/// boundary used. Every amount and unit count is a demonstration parameter; the adapter double reaches
/// nothing.
/// </summary>
public sealed class AdmissionAtTheBoundaryTests
{
    private static readonly DateTimeOffset Reserved = DateTimeOffset.Parse("2026-10-31T23:59:59Z");
    private static readonly ProviderAccountId Account = new("provider-alpha");
    private static readonly ModelId Model = new("alpha-reasoning");

    /// <summary>
    /// With the process clock a day AHEAD of the reserved instant, every instant the boundary returns or
    /// records is the reserved one: the operation, the decision, a hold-and-escalate route's escalation and
    /// a held request's escalation; the governing readings are read once, on the admission transaction.
    /// </summary>
    [Fact]
    public async Task EveryInstantTheBoundaryWritesIsTheReservedOneWhateverTheProcessClockSays()
    {
        var h = Build(processClock: Reserved.AddDays(1));
        var hold = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Primary,
            new RouteTarget.HoldAndEscalate("the recorded emergency position"),
            new QualityRating(100), new ContextCapacity(int.MaxValue), "no provider", new DateOnly(2026, 9, 20));
        h.Routes.Routes.Add(hold);
        h.Availability.States[hold.Id] = RouteAvailability.Serving(hold.Id, Reserved);

        var escalating = Assert.IsType<CapabilityOutcome.Held>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));
        var unrouted = Assert.IsType<CapabilityOutcome.Held>(await h.Gateway.ExecuteAsync(
            Request(floor: 70, capability: CapabilityClass.WebSearch), Context(), CancellationToken.None));

        Assert.Equal(Reserved + TimeSpan.FromHours(4), escalating.EscalatesAt);
        Assert.Equal(RefusalReason.NoAdmittedRoute, unrouted.Reason);
        Assert.Equal(Reserved + TimeSpan.FromHours(4), unrouted.EscalatesAt);
        Assert.All(h.Work.Operations, o => Assert.Equal(Reserved, o.OccurredAt));
        Assert.All(h.Work.Decisions, d => Assert.Equal(Reserved, d.Controller.DecidedAt));
        Assert.All(h.Work.Decisions, d => Assert.Equal(new DateOnly(2026, 10, 1), d.Controller.BookingMonth));
        Assert.Equal(2, h.Work.Admission.Reservations);
        Assert.Equal(2, h.Work.Admission.GoverningReads);
        Assert.Equal(2, h.Work.Decisions.Count);
    }

    /// <summary>
    /// A completed provider attempt is booked at the reserved instant and its budget evaluation is for the
    /// reserved month; its decision records the controller's readings with the recorded amounts apart from
    /// the observed spend and the company basis stated.
    /// </summary>
    [Fact]
    public async Task ACompletedAttemptIsBookedAndEvaluatedInTheReservedMonth()
    {
        var h = Build(processClock: Reserved.AddDays(1));
        var adapter = new AdapterDouble(Account);
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);

        var completed = Assert.IsType<CapabilityOutcome.Completed>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));

        Assert.Equal(1, adapter.Calls);
        Assert.Equal(Reserved, completed.Operation.OccurredAt);
        Assert.Equal(new DateOnly(2026, 10, 1), Assert.Single(h.Work.EvaluatedMonths));
        var decision = Assert.Single(h.Work.Decisions);
        Assert.Equal(CapabilityGateway.ReservationHeld, decision.ReservationStatement);
        var channel = decision.Controller.Readings.Single(r => r.Scope == GoverningScope.Channel);
        Assert.IsType<RecordedAmount.Recorded>(channel.Amount);
        Assert.IsType<MeasurementQuantity.ObservedZero>(channel.BookedSpend);
        Assert.Contains("recorded metered allotment", decision.Controller.CompanyBasisStatement, StringComparison.Ordinal);
        Assert.Equal(SelectionBasis.Configured, decision.Selection.Basis);
    }

    /// <summary>
    /// A missing channel amount refuses the metered route under its own reason, so the adapter is never
    /// called, while a substitute beside it is served at zero cost; both decisions are recorded.
    /// </summary>
    [Fact]
    public async Task AMissingAmountNeverReachesTheProviderAndASubstituteStaysAdmissible()
    {
        var h = Build(processClock: Reserved);
        var adapter = new AdapterDouble(Account);
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);
        h.Work.Admission.ChannelBudget = null;

        var refused = Assert.IsType<CapabilityOutcome.Refused>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.BudgetAmountNotRecorded, refused.Reason);
        Assert.Equal(0, adapter.Calls);

        var substitute = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Emergency,
            new RouteTarget.NonAiSubstitute(DeterministicTaskRegistry.MetadataTemplatePopulation),
            new QualityRating(80), new ContextCapacity(int.MaxValue), "non-AI substitute", new DateOnly(2026, 9, 20));
        h.Routes.Routes.Add(substitute);
        h.Availability.States[substitute.Id] = RouteAvailability.Serving(substitute.Id, Reserved);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));
        Assert.Equal(Money.Zero(), substituted.Operation.ComputedCost);
        Assert.Equal(0, adapter.Calls);
        Assert.Equal(2, h.Work.Decisions.Count);
        Assert.All(h.Work.Decisions, d => Assert.Equal(RefusalReason.BudgetAmountNotRecorded, d.Controller.Reason));
    }

    /// <summary>
    /// A LOST RESERVATION over the in-memory ports: the admission transaction is lost after the provider was
    /// called, while the datastore's clock has passed into the next month. The incurred attempt is recorded
    /// once, on a fresh transaction under a fresh reservation, in the month it is booked into, and its
    /// decision says the admitted reservation was lost and re-evaluates against that month.
    /// </summary>
    [Fact]
    public async Task ALostReservationIsRecordedOnceOnAFreshReservationThatSaysSo()
    {
        var h = Build(processClock: Reserved);
        var next = DateTimeOffset.Parse("2026-11-01T00:00:03Z");
        var adapter = new AdapterDouble(Account, () =>
        {
            h.Work.Admission.Instant = next;
            h.Work.Admission.LoseNextOperationWrite = true;
        });
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);

        var completed = Assert.IsType<CapabilityOutcome.Completed>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));

        Assert.Equal(1, adapter.Calls);
        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(next, operation.OccurredAt);
        Assert.Equal(next, completed.Operation.OccurredAt);
        Assert.Equal(1, h.Work.RolledBack);
        Assert.Equal(2, h.Work.Admission.Reservations);
        var decision = Assert.Single(h.Work.Decisions);
        Assert.Contains("was lost after the provider was called", decision.ReservationStatement, StringComparison.Ordinal);
        Assert.Contains("2026-10", decision.ReservationStatement, StringComparison.Ordinal);
        Assert.Equal(new DateOnly(2026, 11, 1), decision.Controller.BookingMonth);
        Assert.Equal(new DateOnly(2026, 11, 1), Assert.Single(h.Work.EvaluatedMonths));
    }

    /// <summary>
    /// ONE ATTEMPT, BOOKED ONCE (the correction cycle). The admission's commit reaches the store and its
    /// reply is lost: the recovery meets the first record under the attempt's one identifier and books
    /// nothing more — one operation, one decision, one audit entry — and returns the recorded operation.
    /// </summary>
    [Fact]
    public async Task ACommitWhoseReplyIsLostBooksTheAttemptOnce()
    {
        var h = Build(processClock: Reserved);
        var adapter = new AdapterDouble(Account, () => h.Work.LoseNextCommitReply = true);
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);

        var completed = Assert.IsType<CapabilityOutcome.Completed>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));

        Assert.Equal(1, adapter.Calls);
        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(operation.Id, completed.Operation.Id);
        Assert.Single(h.Work.Decisions);
        Assert.Single(h.Work.AuditEntries);
    }

    /// <summary>
    /// A cancellation that arrives once the provider has returned does not roll back the record of the cost
    /// already incurred: the attempt is recorded and committed.
    /// </summary>
    [Fact]
    public async Task ACancellationAfterTheProviderReturnedStillRecordsTheAttempt()
    {
        var h = Build(processClock: Reserved);
        using var cancellation = new CancellationTokenSource();
        var adapter = new AdapterDouble(Account, cancellation.Cancel);
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);

        Assert.IsType<CapabilityOutcome.Completed>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Single(h.Work.Operations);
        Assert.Equal(1, h.Work.Commits);
    }

    /// <summary>
    /// A METERED admission takes the company scope's hold and then the channel scope's, in that order, and
    /// is decided on a snapshot read after both are held; an admission costing nothing takes neither.
    /// </summary>
    [Fact]
    public async Task AMeteredAdmissionHoldsTheCompanyScopeThenTheChannelScopeBeforeItDecides()
    {
        var h = Build(processClock: Reserved);
        h.Adapters[Account] = new AdapterDouble(Account);
        AddProviderRoute(h);
        var request = Request(floor: 70);

        Assert.IsType<CapabilityOutcome.Completed>(await h.Gateway.ExecuteAsync(request, Context(), CancellationToken.None));
        Assert.Equal(["company", $"channel:{request.Attribution.Channel}"], h.Work.Admission.Holds);
        Assert.Equal(2, h.Work.Admission.Snapshots);

        var free = Build(processClock: Reserved);
        var hold = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Primary,
            new RouteTarget.HoldAndEscalate("the recorded emergency position"),
            new QualityRating(100), new ContextCapacity(int.MaxValue), "no provider", new DateOnly(2026, 9, 20));
        free.Routes.Routes.Add(hold);
        free.Availability.States[hold.Id] = RouteAvailability.Serving(hold.Id, Reserved);

        Assert.IsType<CapabilityOutcome.Held>(await free.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));
        Assert.Empty(free.Work.Admission.Holds);
        Assert.Equal(1, free.Work.Admission.Snapshots);
    }

    /// <summary>
    /// NO ADMISSION WAITS ON A SCOPE HOLD (the second correction cycle). Where another admission holds the
    /// company scope, the try is refused at once and the metered admission is deferred under its own reason,
    /// escalating from the reserved instant on the request's declared timeout: no provider is reached, no
    /// second snapshot is read, and the held operation, its decision and its audit entry commit together.
    /// </summary>
    [Fact]
    public async Task AMeteredAdmissionMeetingAHeldScopeIsDeferredUnderItsOwnReasonWithoutWaiting()
    {
        var h = Build(processClock: DateTimeOffset.UnixEpoch);
        var adapter = new AdapterDouble(Account);
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);
        h.Work.Admission.CompanyScopeHeldElsewhere = true;
        var request = Request(floor: 70);

        var held = Assert.IsType<CapabilityOutcome.Held>(await h.Gateway.ExecuteAsync(request, Context(), CancellationToken.None));

        Assert.Equal(RefusalReason.MeteredAdmissionInProgress, held.Reason);
        Assert.Equal(Reserved + request.HoldTimeout, held.EscalatesAt);
        Assert.Equal(0, adapter.Calls);
        Assert.Equal(["company"], h.Work.Admission.Holds);
        Assert.Equal(1, h.Work.Admission.Snapshots);
        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(OperationOutcome.Held, operation.Outcome);
        Assert.Equal(Reserved, operation.OccurredAt);
        Assert.Contains(nameof(RefusalReason.MeteredAdmissionInProgress), operation.FailureReason, StringComparison.Ordinal);
        Assert.Contains("already in progress", Assert.Single(h.Work.Decisions).TierStatement, StringComparison.Ordinal);
        Assert.Single(h.Work.AuditEntries);
        Assert.Equal(1, h.Work.Commits);
    }

    /// <summary>
    /// THE RECOVERY TAKES NO SCOPE HOLD AND CARRIES ITS ALERTS (the second correction cycle). After a lost
    /// commit reply the recovery takes no admission hold, and the recovered outcome carries the alerts the
    /// first commit stored, read back by the operation's booked month and instant.
    /// </summary>
    [Fact]
    public async Task ARecoveredOutcomeTakesNoScopeHoldAndCarriesTheStoredAlerts()
    {
        var h = Build(processClock: Reserved);
        var adapter = new AdapterDouble(Account, () => h.Work.LoseNextCommitReply = true);
        h.Adapters[Account] = adapter;
        AddProviderRoute(h);
        var alert = new BudgetAlert(
            BudgetId.New(), new DateOnly(2026, 10, 1), 50, 70m, new Money(0.0105m), new Money(0.0150m), Reserved);
        h.Work.Alerts.Add(alert);

        var completed = Assert.IsType<CapabilityOutcome.Completed>(await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None));

        Assert.Equal([alert], completed.Alerts);
        Assert.Equal([(new DateOnly(2026, 10, 1), completed.Operation.OccurredAt)], h.Work.StoredAlertReads);
        Assert.Equal(2, h.Work.Admission.Holds.Count);
        Assert.Single(h.Work.Operations);
    }

    // -----------------------------------------------------------------------

    private sealed record Harness(
        ICapabilityGateway Gateway,
        FakeRouteRegistry Routes,
        FakeAvailabilityLedger Availability,
        FakeUnitOfWork Work,
        Dictionary<ProviderAccountId, IProviderAdapter> Adapters);

    private static Harness Build(DateTimeOffset processClock)
    {
        var clock = new TestClock(processClock);
        var routes = new FakeRouteRegistry();
        var availability = new FakeAvailabilityLedger();
        var registers = new FakeRegisters();
        var work = new FakeUnitOfWork();
        work.Admission.Instant = Reserved;
        work.Admission.Price(Model, 0.000_003m, 0.000_015m, 0.000_000_3m, Reserved.AddDays(-30));
        registers.Accounts.Add(new ProviderAccount(Account, "alpha", "paid tier", new DateOnly(2026, 9, 1), ProviderAccountStatus.Active));

        var adapters = new Dictionary<ProviderAccountId, IProviderAdapter>();

        work.Bind(routes, availability, registers);
        var gateway = new CapabilityGateway(work, new StandInBroker(clock), adapters, TimeSpan.FromSeconds(60));
        return new Harness(gateway, routes, availability, work, adapters);
    }

    private static void AddProviderRoute(Harness h)
    {
        var route = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Primary,
            new RouteTarget.ProviderRoute(Account, Model),
            new QualityRating(90), new ContextCapacity(200_000), "paid tier under commercial terms", new DateOnly(2026, 9, 20));
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Reserved);
    }

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Script,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityRequest Request(
        int floor, bool critical = false, CapabilityClass capability = CapabilityClass.EditorialReasoning) => new(
        capability,
        ReasoningTier.Standard,
        new QualityRating(floor),
        new ContextCapacity(8_000),
        new Money(1.00m),
        critical ? Criticality.Critical : Criticality.Routine,
        new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()),
        new EstimatedUnits(1_000, 500, 0),
        TimeSpan.FromHours(4),
        ReducedFloorPolicy.Forbidden);

    /// <summary>A stand-in broker holding NO SECRET: it issues a scoped handle that attaches to nothing.</summary>
    private sealed class StandInBroker(TestClock clock) : ICredentialBroker
    {
        public Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<CredentialOutcome>(new CredentialOutcome.Issued(new ScopedHandle(
                Guid.NewGuid(), request.CredentialClass, request.ProviderAccount, request.Channel, request.Capability,
                request.Job, request.Stage, clock.UtcNow, clock.UtcNow.AddMinutes(5), 1)));
    }

    /// <summary>An adapter double that reaches nothing and returns a fixture attempt; a step may run during the call.</summary>
    private sealed class AdapterDouble(ProviderAccountId account, Action? during = null) : IProviderAdapter
    {
        public int Calls { get; private set; }

        public ProviderAccountId ProviderAccount { get; } = account;

        public Task<ProviderAttempt> InvokeAsync(
            RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
        {
            Calls++;
            during?.Invoke();
            return Task.FromResult(new ProviderAttempt(true, new UnitCounts(1_000, 500, 0, 0), CostBasis.Measurement, TimeSpan.FromMilliseconds(10), null, null));
        }
    }
}
