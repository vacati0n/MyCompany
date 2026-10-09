using MediaCompany.Credentials;
using MediaCompany.Deterministic;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using Xunit;

namespace MediaCompany.Capability.Tests;

/// <summary>
/// The resolution boundary end to end over in-memory ports. These exercise the property that the
/// boundary — and only the boundary — records the operation and the reason, in the same
/// transaction as the work (constraint C-004, decision D-006).
/// </summary>
public sealed class CapabilityGatewayTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    private static readonly ProviderAccountId Account = new("provider-alpha");
    private static readonly ModelId Model = new("alpha-reasoning");

    private sealed record Harness(
        ICapabilityGateway Gateway,
        FakeRouteRegistry Routes,
        FakeAvailabilityLedger Availability,
        FakeRegisters Registers,
        FakeBudgetReader Budget,
        FakeUnitOfWork Work,
        TestClock Clock);

    private static Harness Build()
    {
        var clock = new TestClock(Now);
        var routes = new FakeRouteRegistry();
        var availability = new FakeAvailabilityLedger();
        var registers = new FakeRegisters();
        var budget = new FakeBudgetReader();
        var work = new FakeUnitOfWork();

        registers.Accounts.Add(new ProviderAccount(Account, "alpha", "paid tier", new DateOnly(2026, 9, 1), ProviderAccountStatus.Active));
        registers.Models.Add(new Model(Model, Account, new QualityRating(90), new ContextCapacity(200_000), "text"));
        registers.Prices.Add(new ModelPrice(
            ModelPriceId.New(), Model, PriceUnitKind.InputUnit, 0.000_003m, "USD",
            "provider price page, ESTIMATE, not verified first-hand", new DateOnly(2026, 9, 1),
            Now.AddDays(-30), null));

        var secrets = new FakeSecretStore();
        var broker = new CredentialBroker(secrets, () => clock.UtcNow, TimeSpan.FromMinutes(5));

        var gateway = CapabilityGatewayFactory.Create(
            routes, availability, registers, budget, work, broker, broker, clock,
            endpoints: [], httpClient: new HttpClient());

        return new Harness(gateway, routes, availability, registers, budget, work, clock);
    }

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Script,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityRequest Request(int floor = 70, Criticality criticality = Criticality.Routine) => new(
        CapabilityClass.EditorialReasoning,
        ReasoningTier.Standard,
        new QualityRating(floor),
        new ContextCapacity(8_000),
        new Money(1.00m),
        criticality,
        new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()),
        new EstimatedUnits(1_000, 500, 0),
        TimeSpan.FromHours(4),
        ReducedFloorPolicy.Forbidden);

    private static Route ProviderRoute(RouteTier tier, int quality = 90) => new(
        RouteId.New(), CapabilityClass.EditorialReasoning, tier,
        new RouteTarget.ProviderRoute(Account, Model),
        new QualityRating(quality), new ContextCapacity(200_000),
        "paid tier under commercial terms", new DateOnly(2026, 9, 20));

    /// <summary>
    /// AC-001 at the boundary: a forbidden source resolves to zero routes, and the refusal is
    /// recorded as an operation and as an audit entry in the same committed transaction.
    /// </summary>
    [Fact]
    public async Task AForbiddenSourceIsRefusedAndTheRefusalIsRecorded()
    {
        var h = Build();
        var route = ProviderRoute(RouteTier.Primary);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);
        h.Routes.Forbidden.Add(new ForbiddenSource(
            ForbiddenSourceKind.ConsumerChatSubscription, Account.Value,
            "a consumer chat subscription driven programmatically is prohibited", "platform-policy-dossier"));

        var outcome = await h.Gateway.ExecuteAsync(Request(), Context(), CancellationToken.None);

        var refused = Assert.IsType<CapabilityOutcome.Refused>(outcome);
        Assert.Equal(RefusalReason.ForbiddenSource, refused.Reason);

        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(OperationOutcome.Refused, operation.Outcome);
        Assert.Equal(Money.Zero(), operation.ComputedCost);
        Assert.Single(h.Work.AuditEntries);
        Assert.Equal(1, h.Work.Commits);
    }

    /// <summary>
    /// Constraint C-004: the operation record and the audit entry are one fact. When the commit
    /// fails, neither is durable — there is no state in which the work is recorded and its reason
    /// is not, or the other way round.
    /// </summary>
    [Fact]
    public async Task AFailedCommitLeavesNeitherTheOperationNorItsReason()
    {
        var h = Build();
        h.Work.FailNextCommit = true;

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => h.Gateway.ExecuteAsync(Request(), Context(), CancellationToken.None));

        Assert.Empty(h.Work.Operations);
        Assert.Empty(h.Work.AuditEntries);
        Assert.Equal(0, h.Work.Commits);
        Assert.Equal(1, h.Work.RolledBack);
    }

    /// <summary>
    /// AC-002: with no route at or above the floor, the work is held with its reason and escalates
    /// on its declared timeout. It is never served below the floor.
    /// </summary>
    [Fact]
    public async Task WorkWithNoRouteAtOrAboveTheFloorIsHeldAndRecorded()
    {
        var h = Build();
        var low = ProviderRoute(RouteTier.Primary, quality: 40);
        h.Routes.Routes.Add(low);
        h.Availability.States[low.Id] = RouteAvailability.Serving(low.Id, Now);

        var outcome = await h.Gateway.ExecuteAsync(Request(floor: 95, criticality: Criticality.Critical), Context(), CancellationToken.None);

        var held = Assert.IsType<CapabilityOutcome.Held>(outcome);
        Assert.Equal(95, held.FloorRequired.Value);
        Assert.True(held.EscalatedToOwner);
        Assert.Equal(Now.AddHours(4), held.EscalatesAt);

        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(OperationOutcome.Held, operation.Outcome);
        Assert.Equal(Money.Zero(), operation.ComputedCost);
        Assert.Contains(h.Work.AuditEntries, e => e.Reason.Contains("held at floor", StringComparison.Ordinal));
    }

    /// <summary>
    /// Design fact F-021: the hold-and-escalate emergency position records the work as held with
    /// zero cost. No provider is reached, so no cost can attach.
    /// </summary>
    [Fact]
    public async Task AHoldAndEscalateRouteRecordsAHeldOperationAtZeroCost()
    {
        var h = Build();
        var hold = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Primary,
            new RouteTarget.HoldAndEscalate("high-stakes review holds and escalates to the owner"),
            new QualityRating(100), new ContextCapacity(int.MaxValue),
            "no provider; the recorded emergency position", new DateOnly(2026, 9, 20));
        h.Routes.Routes.Add(hold);
        h.Availability.States[hold.Id] = RouteAvailability.Serving(hold.Id, Now);

        var outcome = await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None);

        Assert.IsType<CapabilityOutcome.Held>(outcome);
        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(OperationOutcome.Held, operation.Outcome);
        Assert.Null(operation.Model);
        Assert.Equal(Money.Zero(), operation.ComputedCost);
    }

    /// <summary>
    /// AC-017 at the boundary: a non-AI substitute records the named deterministic task and no
    /// model, so the cost attributed to the named set is zero by the shape of the row.
    /// </summary>
    [Fact]
    public async Task ANonAiSubstituteRecordsTheNamedTaskAndNoModel()
    {
        var h = Build();
        var substitute = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Emergency,
            new RouteTarget.NonAiSubstitute(DeterministicTaskRegistry.MetadataTemplatePopulation),
            new QualityRating(75), new ContextCapacity(int.MaxValue),
            "non-AI substitute", new DateOnly(2026, 9, 20));
        h.Routes.Routes.Add(substitute);
        h.Availability.States[substitute.Id] = RouteAvailability.Serving(substitute.Id, Now);

        var outcome = await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None);

        var substituted = Assert.IsType<CapabilityOutcome.Substituted>(outcome);
        Assert.True(DeterministicTaskRegistry.Contains(substituted.DeterministicTaskName));

        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(DeterministicTaskRegistry.MetadataTemplatePopulation, operation.DeterministicTaskName);
        Assert.Null(operation.Model);
        Assert.Null(operation.AppliedPrice);
        Assert.Equal(Money.Zero(), operation.ComputedCost);
    }

    /// <summary>
    /// Decision D-011: a retry is its own operation record. Two attempts of the same work produce
    /// two rows, each carrying its own attempt ordinal, which is what makes exactly-once well
    /// defined under retry.
    /// </summary>
    [Fact]
    public async Task EachAttemptProducesItsOwnOperationRecord()
    {
        var h = Build();
        var request = Request(floor: 95, criticality: Criticality.Critical);

        await h.Gateway.ExecuteAsync(request, Context() with { Attempt = 1 }, CancellationToken.None);
        await h.Gateway.ExecuteAsync(request, Context() with { Attempt = 2 }, CancellationToken.None);

        Assert.Equal(2, h.Work.Operations.Count);
        Assert.Equal([1, 2], h.Work.Operations.Select(o => o.Attempt));
    }

    /// <summary>Every recorded operation carries the complete attribution tuple (AC-003).</summary>
    [Fact]
    public async Task EveryRecordedOperationCarriesTheFullAttributionTuple()
    {
        var h = Build();
        var request = Request(floor: 95);

        await h.Gateway.ExecuteAsync(request, Context(), CancellationToken.None);

        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(request.Attribution.Item, operation.Attribution.Item);
        Assert.Equal(request.Attribution.Channel, operation.Attribution.Channel);
        Assert.Equal(request.Attribution.Department, operation.Attribution.Department);
        Assert.Equal(request.Attribution.Agent, operation.Attribution.Agent);
        Assert.Equal(request.Capability, operation.Capability);
    }

    /// <summary>
    /// The gateway registers no adapter when no endpoint is configured, and says so rather than
    /// reaching anywhere. This wave creates no account and commits no spend (constraint C-013).
    /// </summary>
    [Fact]
    public async Task WithNoProviderEndpointConfiguredNoProviderIsReached()
    {
        var h = Build();
        var route = ProviderRoute(RouteTier.Primary);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);

        var outcome = await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None);

        var refused = Assert.IsType<CapabilityOutcome.Refused>(outcome);
        Assert.Contains("No adapter is registered", refused.Detail, StringComparison.Ordinal);

        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(OperationOutcome.Failed, operation.Outcome);
        Assert.Equal(Money.Zero(), operation.ComputedCost);
    }

    /// <summary>A budget with nothing left removes every route at resolution step 4.</summary>
    [Fact]
    public async Task AnExhaustedBudgetHoldsTheWork()
    {
        var h = Build();
        var route = ProviderRoute(RouteTier.Primary);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);
        h.Budget.Remaining = Money.Zero();

        var outcome = await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None);

        var held = Assert.IsType<CapabilityOutcome.Held>(outcome);
        Assert.Equal(RefusalReason.CostCeilingOrBudgetExceeded, held.Reason);
    }

    /// <summary>
    /// Admission reads its headroom for the DATASTORE'S booking month, the clock booking and the
    /// evaluation are decided on, and never for a month taken from the process clock.
    /// </summary>
    [Fact]
    public async Task AdmissionReadsHeadroomForTheDatastoresBookingMonth()
    {
        var h = Build();
        var route = ProviderRoute(RouteTier.Primary);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);
        h.Budget.Remaining = Money.Zero();
        h.Budget.BookingMonth = new DateOnly(2026, 11, 1);

        await h.Gateway.ExecuteAsync(Request(floor: 70), Context(), CancellationToken.None);

        Assert.Equal(1, h.Budget.BookingMonthReads);
        Assert.Equal(0, h.Budget.CallerMonthReads);
    }
}
