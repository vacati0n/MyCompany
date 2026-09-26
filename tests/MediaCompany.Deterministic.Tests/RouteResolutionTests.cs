using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The capability routing demonstrations, against acceptance criteria AC-001 and AC-002.
/// </summary>
public sealed class RouteResolutionTests
{
    /// <summary>
    /// AC-001. A request that would reach a forbidden source resolves to zero reachable routes.
    ///
    /// The route is deliberately present in the admitted table, which is what makes this the
    /// SECOND and independent refusal: it stands in for an admission-time check that failed, and
    /// resolution still refuses. A single check could not demonstrate this.
    /// </summary>
    [Fact]
    public void AForbiddenSourceInTheTableResolvesToZeroRoutes()
    {
        var forbiddenAccount = new ProviderAccountId("consumer-chat-subscription");
        var route = new Route(
            RouteId.New(),
            CapabilityClass.EditorialReasoning,
            RouteTier.Primary,
            new RouteTarget.ProviderRoute(forbiddenAccount, Fixture.PrimaryModel),
            new QualityRating(95),
            new ContextCapacity(100_000),
            "consumer subscription",
            Fixture.Today);

        var register = new[]
        {
            new ForbiddenSource(
                ForbiddenSourceKind.ConsumerChatSubscription,
                "consumer-chat-subscription",
                "a consumer chat subscription driven programmatically is prohibited by the provider's terms",
                "platform-policy-dossier"),
        };

        var resolution = RouteResolver.Resolve(
            Fixture.Request(),
            Fixture.Inputs([route], forbidden: register));

        var refused = Assert.IsType<CapabilityResolution.Refused>(resolution);
        Assert.Equal(RefusalReason.ForbiddenSource, refused.Reason);
        Assert.Equal(ForbiddenSourceKind.ConsumerChatSubscription, refused.ForbiddenKind);
        Assert.Contains("forbidden source", refused.Detail, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// AC-001, the independence half. Admission refuses the same route before it ever reaches the
    /// table. Two independent refusals would both have to fail before a forbidden path existed.
    /// </summary>
    [Fact]
    public void AdmissionRefusesTheSameForbiddenRouteIndependently()
    {
        var forbiddenAccount = new ProviderAccountId("consumer-chat-subscription");
        var candidate = new Route(
            RouteId.New(),
            CapabilityClass.EditorialReasoning,
            RouteTier.Primary,
            new RouteTarget.ProviderRoute(forbiddenAccount, Fixture.PrimaryModel),
            new QualityRating(95),
            new ContextCapacity(100_000),
            "consumer subscription",
            Fixture.Today);

        var register = new[]
        {
            new ForbiddenSource(
                ForbiddenSourceKind.ConsumerChatSubscription,
                "consumer-chat-subscription",
                "prohibited by the provider's terms",
                "platform-policy-dossier"),
        };

        var verdict = RouteAdmission.Admit(candidate, register, [], Fixture.Today);

        var refused = Assert.IsType<AdmissionVerdict.Refused>(verdict);
        Assert.Equal(AdmissionRefusal.ForbiddenSource, refused.Reason);
        Assert.Equal(ForbiddenSourceKind.ConsumerChatSubscription, refused.ForbiddenKind);
    }

    /// <summary>The withdrawn generation interface is refused the same way.</summary>
    [Fact]
    public void TheWithdrawnGenerationInterfaceIsRefusedAtAdmission()
    {
        var candidate = new Route(
            RouteId.New(),
            CapabilityClass.GeneratedCutaways,
            RouteTier.Primary,
            new RouteTarget.ProviderRoute(new ProviderAccountId("withdrawn-generation-interface"), new ModelId("m")),
            new QualityRating(80),
            new ContextCapacity(1),
            "terms",
            Fixture.Today);

        var register = new[]
        {
            new ForbiddenSource(
                ForbiddenSourceKind.WithdrawnInterface,
                "withdrawn-generation-interface",
                "the generation interface was withdrawn by its provider on 2026-09-24",
                "ai-capacity-dossier"),
        };

        var refused = Assert.IsType<AdmissionVerdict.Refused>(
            RouteAdmission.Admit(candidate, register, [], Fixture.Today));
        Assert.Equal(ForbiddenSourceKind.WithdrawnInterface, refused.ForbiddenKind);
    }

    /// <summary>Risk R-001: a route past its terms re-verification cadence is not admitted.</summary>
    [Fact]
    public void ARoutePastItsTermsCadenceIsNotAdmitted()
    {
        var stale = new Route(
            RouteId.New(),
            CapabilityClass.EditorialReasoning,
            RouteTier.Primary,
            new RouteTarget.ProviderRoute(Fixture.PrimaryAccount, Fixture.PrimaryModel),
            new QualityRating(90),
            new ContextCapacity(100_000),
            "commercial terms",
            Fixture.Today.AddDays(-400));

        var refused = Assert.IsType<AdmissionVerdict.Refused>(
            RouteAdmission.Admit(stale, [], [], Fixture.Today));
        Assert.Equal(AdmissionRefusal.TermsBasisStale, refused.Reason);
    }

    /// <summary>Constraint C-002: a capability holds exactly one route per tier.</summary>
    [Fact]
    public void ASecondRouteAtAnOccupiedTierIsNotAdmitted()
    {
        var existing = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var duplicate = Fixture.Provider(RouteTier.Primary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);

        var refused = Assert.IsType<AdmissionVerdict.Refused>(
            RouteAdmission.Admit(duplicate, [], [existing], Fixture.Today));
        Assert.Equal(AdmissionRefusal.TierAlreadyOccupied, refused.Reason);
    }

    /// <summary>Resolution step 5: the first surviving route in tier order.</summary>
    [Fact]
    public void ResolutionTakesThePrimaryRouteWhenItIsServing()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 80);

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(Fixture.Request(floor: 70), Fixture.Inputs([primary, secondary])));

        Assert.Equal(primary.Id, resolved.Route.Id);
        Assert.False(resolved.FloorWasReduced);
    }

    /// <summary>
    /// AC-002. With the primary and the secondary both unavailable, the work takes the emergency
    /// route. The reason for each unavailability is recorded state, not inference (decision D-008).
    /// </summary>
    [Fact]
    public void WithPrimaryAndSecondaryUnavailableTheEmergencyRouteIsTaken()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);
        var emergency = Fixture.Provider(RouteTier.Emergency, Fixture.EmergencyAccount, Fixture.EmergencyModel, 80);

        var availability = new Dictionary<RouteId, RouteAvailability>
        {
            [primary.Id] = new(primary.Id, AvailabilityState.QuotaExhausted, Fixture.Now,
                "the account's spend ledger reached its recorded cap", Fixture.Now.AddDays(1), null),
            [secondary.Id] = new(secondary.Id, AvailabilityState.Outage, Fixture.Now,
                "consecutive adapter failures crossed the declared threshold", Fixture.Now.AddMinutes(5), null),
            [emergency.Id] = RouteAvailability.Serving(emergency.Id, Fixture.Now),
        };

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 70),
                Fixture.Inputs([primary, secondary, emergency], availability: availability)));

        Assert.Equal(emergency.Id, resolved.Route.Id);
        Assert.Equal(RouteTier.Emergency, resolved.Route.Tier);
    }

    /// <summary>
    /// AC-002 and design fact F-021. For a capability whose emergency tier is hold-and-escalate,
    /// the work is recorded as held at its declared quality rather than served below it.
    /// </summary>
    [Fact]
    public void WhereTheEmergencyTierIsHoldAndEscalateTheWorkIsHeld()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var emergency = Fixture.HoldAndEscalate(RouteTier.Emergency);

        var availability = new Dictionary<RouteId, RouteAvailability>
        {
            [primary.Id] = new(primary.Id, AvailabilityState.Outage, Fixture.Now, "outage", null, null),
            [emergency.Id] = RouteAvailability.Serving(emergency.Id, Fixture.Now),
        };

        var resolution = RouteResolver.Resolve(
            Fixture.Request(floor: 70, criticality: Criticality.Critical),
            Fixture.Inputs([primary, emergency], availability: availability));

        // The emergency position IS the hold: the union member is selected, and the gateway records
        // it as held and escalates to the owner.
        var resolved = Assert.IsType<CapabilityResolution.Resolved>(resolution);
        Assert.IsType<RouteTarget.HoldAndEscalate>(resolved.Route.Target);
    }

    /// <summary>A non-AI substitute routes to a member of the named deterministic set.</summary>
    [Fact]
    public void ANonAiSubstituteRoutesToANamedDeterministicTask()
    {
        var emergency = Fixture.Substitute(RouteTier.Emergency, DeterministicTaskRegistry.MetadataTemplatePopulation);

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(Fixture.Request(floor: 60), Fixture.Inputs([emergency])));

        var substitute = Assert.IsType<RouteTarget.NonAiSubstitute>(resolved.Route.Target);
        Assert.True(DeterministicTaskRegistry.Contains(substitute.DeterministicTaskName));
    }

    /// <summary>
    /// Constraint C-003, stated as the design states it: the property is the ABSENCE of a
    /// below-floor candidate, not a rejected one. A request whose floor exceeds every route
    /// resolves to held, carrying the floor and the routes tried.
    /// </summary>
    [Fact]
    public void ARequestWhoseFloorExceedsEveryRouteIsHeldAndNeverDowngraded()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 70);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 60);

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 95, criticality: Criticality.Critical),
                Fixture.Inputs([primary, secondary])));

        Assert.Equal(RefusalReason.NoRouteAtOrAboveFloor, held.Reason);
        Assert.Equal(95, held.FloorRequired.Value);
        Assert.Equal(2, held.RoutesTried.Count);
        Assert.True(held.EscalateToOwner);
    }

    /// <summary>
    /// The floor filter runs before tier ordering. Here the below-floor route is the ONLY serving
    /// one, and resolution still holds rather than taking it.
    /// </summary>
    [Fact]
    public void TheOnlyServingRouteIsNotTakenWhenItSitsBelowTheFloor()
    {
        var belowFloor = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 50);

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(Fixture.Request(floor: 80), Fixture.Inputs([belowFloor])));

        Assert.Equal(RefusalReason.NoRouteAtOrAboveFloor, held.Reason);
    }

    /// <summary>
    /// A non-critical request whose own declared policy permits a reduced floor runs at the
    /// reduced floor, and the reduction is recorded so the item can be barred from a releasable
    /// state until a role re-verifies it.
    /// </summary>
    [Fact]
    public void ADeclaredReducedFloorPolicyIsUsedAndTheReductionIsRecorded()
    {
        var route = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 65);

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 80, reducedFloor: ReducedFloorPolicy.AllowDownTo(new QualityRating(60))),
                Fixture.Inputs([route])));

        Assert.True(resolved.FloorWasReduced);
        Assert.Equal(60, resolved.EffectiveFloorApplied.Value);
    }

    /// <summary>Resolution step 3: a disabled or revoked provider account removes its routes.</summary>
    [Fact]
    public void ARevokedProviderAccountRemovesItsRoutes()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var accounts = Fixture.AllActive();
        accounts[Fixture.PrimaryAccount] = ProviderAccountStatus.Revoked;

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(Fixture.Request(floor: 70), Fixture.Inputs([primary], accounts: accounts)));

        Assert.Equal(RefusalReason.NoAvailableRoute, held.Reason);
    }

    /// <summary>
    /// A recorded quality-degradation observation drops the route's effective rating, removing it
    /// from every request whose floor is above the observed level.
    /// </summary>
    [Fact]
    public void ADegradedRouteIsRemovedFromRequestsAboveItsObservedQuality()
    {
        var primary = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);
        var secondary = Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85);

        var availability = Fixture.AllServing([primary, secondary]);
        availability[primary.Id] = new RouteAvailability(
            primary.Id, AvailabilityState.QualityDegraded, Fixture.Now,
            "a recorded verdict found delivered quality below the rating", null, new QualityRating(60));

        var resolved = Assert.IsType<CapabilityResolution.Resolved>(
            RouteResolver.Resolve(Fixture.Request(floor: 80), Fixture.Inputs([primary, secondary], availability: availability)));

        Assert.Equal(secondary.Id, resolved.Route.Id);
    }

    /// <summary>Resolution step 4: a route whose estimated cost exceeds the ceiling is removed.</summary>
    [Fact]
    public void ARouteExceedingTheCostCeilingIsRemoved()
    {
        var expensive = Fixture.Provider(RouteTier.Primary, Fixture.EmergencyAccount, Fixture.EmergencyModel, 95);

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 90, ceiling: 0.000_001m, inputUnits: 1_000_000, outputUnits: 1_000_000),
                Fixture.Inputs([expensive])));

        Assert.Equal(RefusalReason.CostCeilingOrBudgetExceeded, held.Reason);
    }

    /// <summary>Resolution step 4: the governing budget is a second ceiling.</summary>
    [Fact]
    public void ARouteThatWouldExceedTheRemainingBudgetIsRemoved()
    {
        var route = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 70, ceiling: 100m, inputUnits: 10_000_000, outputUnits: 10_000_000),
                Fixture.Inputs([route], budgetRemaining: 0.01m)));

        Assert.Equal(RefusalReason.CostCeilingOrBudgetExceeded, held.Reason);
    }

    /// <summary>Resolution step 2: the context requirement is applied alongside the floor.</summary>
    [Fact]
    public void ARouteWithoutTheRequiredContextCapacityIsRemoved()
    {
        var small = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 95, context: 4_000);

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(Fixture.Request(floor: 70, context: 200_000), Fixture.Inputs([small])));

        Assert.Equal(RefusalReason.InsufficientContextCapacity, held.Reason);
    }

    /// <summary>A capability with no admitted route holds rather than failing without a reason.</summary>
    [Fact]
    public void ACapabilityWithNoAdmittedRouteIsHeldWithARecordedReason()
    {
        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(Fixture.Request(), Fixture.Inputs([])));

        Assert.Equal(RefusalReason.NoAdmittedRoute, held.Reason);
    }

    /// <summary>
    /// A route with no recorded availability is not a candidate. Decision D-008 makes availability
    /// explicit, and the absence of a record is not "serving".
    /// </summary>
    [Fact]
    public void ARouteWithNoRecordedAvailabilityIsNotACandidate()
    {
        var route = Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90);

        var held = Assert.IsType<CapabilityResolution.Held>(
            RouteResolver.Resolve(
                Fixture.Request(floor: 70),
                Fixture.Inputs([route], availability: new Dictionary<RouteId, RouteAvailability>())));

        Assert.Equal(RefusalReason.NoAvailableRoute, held.Reason);
    }

    /// <summary>Determinism: the same inputs yield the same resolution.</summary>
    [Fact]
    public void ResolutionIsDeterministic()
    {
        var routes = new[]
        {
            Fixture.Provider(RouteTier.Primary, Fixture.PrimaryAccount, Fixture.PrimaryModel, 90),
            Fixture.Provider(RouteTier.Secondary, Fixture.SecondaryAccount, Fixture.SecondaryModel, 85),
        };
        var request = Fixture.Request(floor: 70);
        var inputs = Fixture.Inputs(routes);

        var first = Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(request, inputs));
        var second = Assert.IsType<CapabilityResolution.Resolved>(RouteResolver.Resolve(request, inputs));

        Assert.Equal(first.Route.Id, second.Route.Id);
    }
}
