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
        var gateway = new CapabilityGateway(work, new StandInBroker(clock), adapters);
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
