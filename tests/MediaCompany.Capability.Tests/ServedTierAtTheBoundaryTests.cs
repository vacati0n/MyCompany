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
/// The served reasoning tier, recorded AT THE RESOLUTION BOUNDARY from the admitting route.
///
/// The distinction the whole measurement rests on: the request states what was ASKED FOR, and only
/// the admitting route can state what was SERVED. A served value copied from the request would
/// manufacture exactly the evidence the column exists to measure, and the resulting split would
/// look like a measurement while being a restatement of the assumption.
/// </summary>
public sealed class ServedTierAtTheBoundaryTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    private static readonly ProviderAccountId Account = new("provider-alpha");
    private static readonly ModelId Model = new("alpha-reasoning");

    private sealed record Harness(
        ICapabilityGateway Gateway, FakeRouteRegistry Routes,
        FakeAvailabilityLedger Availability, FakeUnitOfWork Work);

    private static Harness Build()
    {
        var clock = new TestClock(Now);
        var routes = new FakeRouteRegistry();
        var availability = new FakeAvailabilityLedger();
        var registers = new FakeRegisters();
        var work = new FakeUnitOfWork();
        work.Admission.Instant = Now;

        registers.Accounts.Add(new ProviderAccount(
            Account, "alpha", "paid tier", new DateOnly(2026, 9, 1), ProviderAccountStatus.Active));
        registers.Models.Add(new Model(
            Model, Account, new QualityRating(90), new ContextCapacity(200_000), "text"));

        var secrets = new FakeSecretStore();
        var broker = new CredentialBroker(secrets, () => clock.UtcNow, TimeSpan.FromMinutes(5));

        work.Bind(routes, availability, registers);
        var gateway = CapabilityGatewayFactory.Create(
            work, broker, broker, clock,
            endpoints: [], httpClient: new HttpClient());

        return new Harness(gateway, routes, availability, work);
    }

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Script,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityRequest Request(ReasoningTier requested) => new(
        CapabilityClass.EditorialReasoning,
        requested,
        new QualityRating(70),
        new ContextCapacity(8_000),
        new Money(1.00m),
        Criticality.Routine,
        new Attribution(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New()),
        new EstimatedUnits(1_000, 500, 0),
        TimeSpan.FromHours(4),
        ReducedFloorPolicy.Forbidden);

    private static Route HoldRoute(ReasoningTier? statedTier) => new(
        RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Primary,
        new RouteTarget.HoldAndEscalate("held"),
        new QualityRating(100), new ContextCapacity(int.MaxValue),
        "no provider; the recorded emergency position", new DateOnly(2026, 9, 20),
        statedTier);

    /// <summary>
    /// The served value is the ROUTE's stated tier, even where it differs from the requested one.
    ///
    /// The two differ here deliberately: the request asks Light and the admitting route states
    /// Deep, and the record carries both, which is the only shape in which the split is testable.
    ///
    /// Re-pointed by the AI-economics change (decision D-005 of its design): the delivered shape asked
    /// Deep of a route stating Light, and a route stating a LOWER tier than requested is no longer
    /// served except as a recorded controller downgrade, so the request is now held. The property under
    /// test — the served value is the route's, not the request's — is unchanged, and is shown here with a
    /// route stating a HIGHER tier, which is served.
    /// </summary>
    [Fact]
    public async Task TheServedTierIsReadFromTheRouteAndNotFromTheRequest()
    {
        var h = Build();
        var route = HoldRoute(ReasoningTier.Deep);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);

        await h.Gateway.ExecuteAsync(Request(ReasoningTier.Light), Context(), CancellationToken.None);

        var operation = Assert.Single(h.Work.Operations);

        Assert.Equal(ReasoningTier.Light, operation.ReasoningTierRequested);
        Assert.Equal(ReasoningTier.Deep, operation.ReasoningTierServed);
        Assert.NotEqual(operation.ReasoningTierRequested, operation.ReasoningTierServed);
        Assert.True(operation.CarriesTierEvidence);
    }

    /// <summary>
    /// A route that states no tier yields the EXPLICIT ABSENCE MARKER. The record does not fall
    /// back to the requested tier, so an untiered route stays visible as untiered and the count of
    /// records carrying tier evidence stays a measured count that may be zero.
    /// </summary>
    [Fact]
    public async Task ARouteStatingNoTierLeavesTheServedValueAbsent()
    {
        var h = Build();
        var route = HoldRoute(statedTier: null);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);

        await h.Gateway.ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);

        var operation = Assert.Single(h.Work.Operations);

        Assert.Equal(ReasoningTier.Deep, operation.ReasoningTierRequested);
        Assert.Null(operation.ReasoningTierServed);
        Assert.False(operation.CarriesTierEvidence);
    }

    /// <summary>
    /// An operation that reached no route at all carries the absence marker too. There was no
    /// admitting route, so there is nothing that could have stated a tier.
    /// </summary>
    [Fact]
    public async Task AnOperationWithNoAdmittedRouteCarriesTheAbsenceMarker()
    {
        var h = Build();

        // No route is registered, so the resolution refuses before any route is admitted.
        await h.Gateway.ExecuteAsync(Request(ReasoningTier.Standard), Context(), CancellationToken.None);

        var operation = Assert.Single(h.Work.Operations);

        Assert.Equal(ReasoningTier.Standard, operation.ReasoningTierRequested);
        Assert.Null(operation.ReasoningTierServed);
        Assert.False(operation.CarriesTierEvidence);
    }

    /// <summary>
    /// A substitute route's stated tier reaches the record the same way. Re-pointed by the AI-economics
    /// change for the same reason as above: the substitute states a higher tier than requested, which is
    /// served, rather than a lower one, which is no longer served outside a recorded downgrade.
    /// </summary>
    [Fact]
    public async Task ASubstituteRoutesStatedTierReachesTheRecord()
    {
        var h = Build();
        var substitute = new Route(
            RouteId.New(), CapabilityClass.EditorialReasoning, RouteTier.Emergency,
            new RouteTarget.NonAiSubstitute(DeterministicTaskRegistry.MetadataTemplatePopulation),
            new QualityRating(75), new ContextCapacity(int.MaxValue),
            "non-AI substitute", new DateOnly(2026, 9, 20),
            ReasoningTier.Standard);

        h.Routes.Routes.Add(substitute);
        h.Availability.States[substitute.Id] = RouteAvailability.Serving(substitute.Id, Now);

        await h.Gateway.ExecuteAsync(Request(ReasoningTier.Light), Context(), CancellationToken.None);

        var operation = Assert.Single(h.Work.Operations);
        Assert.Equal(ReasoningTier.Standard, operation.ReasoningTierServed);
    }

    /// <summary>
    /// The tier rule at the boundary (the AI-economics change, decision D-005): a request whose only
    /// surviving route states a LOWER tier is held with its own reason, recorded at zero cost with no
    /// served tier, and nothing is served below the requested tier without a recorded downgrade.
    /// </summary>
    [Fact]
    public async Task ARouteStatingALowerTierIsNotServedWithoutADowngrade()
    {
        var h = Build();
        var route = HoldRoute(ReasoningTier.Light);
        h.Routes.Routes.Add(route);
        h.Availability.States[route.Id] = RouteAvailability.Serving(route.Id, Now);

        var outcome = await h.Gateway.ExecuteAsync(Request(ReasoningTier.Deep), Context(), CancellationToken.None);

        var held = Assert.IsType<CapabilityOutcome.Held>(outcome);
        Assert.Equal(RefusalReason.NoRouteAtRequestedTier, held.Reason);
        var operation = Assert.Single(h.Work.Operations);
        Assert.Null(operation.ReasoningTierServed);
        var decision = Assert.Single(h.Work.Decisions);
        Assert.Null(decision.TierOutcome);
        Assert.Contains("below Deep", decision.TierStatement, StringComparison.Ordinal);
    }
}
