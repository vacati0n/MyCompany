using MediaCompany.Deterministic.Routing;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// A small routing position for the demonstrations. It is deliberately explicit: each test states
/// the position it resolves against, so a demonstration is judgeable by a role other than the one
/// that performs it.
/// </summary>
internal static class Fixture
{
    internal static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-10-01T12:00:00Z");
    internal static readonly DateOnly Today = new(2026, 10, 1);

    internal static readonly ProviderAccountId PrimaryAccount = new("provider-alpha");
    internal static readonly ProviderAccountId SecondaryAccount = new("provider-beta");
    internal static readonly ProviderAccountId EmergencyAccount = new("provider-gamma");

    internal static readonly ModelId PrimaryModel = new("alpha-reasoning");
    internal static readonly ModelId SecondaryModel = new("beta-reasoning");
    internal static readonly ModelId EmergencyModel = new("gamma-reasoning");

    internal static Attribution Attribution() =>
        new(ItemId.New(), ChannelId.New(), DepartmentId.New(), AgentId.New());

    internal static Route Provider(
        RouteTier tier,
        ProviderAccountId account,
        ModelId model,
        int quality,
        int context = 100_000) =>
        new(RouteId.New(),
            CapabilityClass.EditorialReasoning,
            tier,
            new RouteTarget.ProviderRoute(account, model),
            new QualityRating(quality),
            new ContextCapacity(context),
            "commercial terms, paid tier",
            Today.AddDays(-10));

    internal static Route HoldAndEscalate(RouteTier tier, int quality = 100) =>
        new(RouteId.New(),
            CapabilityClass.EditorialReasoning,
            tier,
            new RouteTarget.HoldAndEscalate("high-stakes review holds and escalates to the owner"),
            new QualityRating(quality),
            new ContextCapacity(int.MaxValue),
            "no provider; the recorded emergency position",
            Today.AddDays(-10));

    internal static Route Substitute(RouteTier tier, string task, int quality = 70) =>
        new(RouteId.New(),
            CapabilityClass.EditorialReasoning,
            tier,
            new RouteTarget.NonAiSubstitute(task),
            new QualityRating(quality),
            new ContextCapacity(int.MaxValue),
            "non-AI substitute; no provider terms apply",
            Today.AddDays(-10));

    internal static CapabilityRequest Request(
        int floor = 70,
        Criticality criticality = Criticality.Routine,
        decimal ceiling = 1.00m,
        int context = 8_000,
        ReducedFloorPolicy? reducedFloor = null,
        long inputUnits = 1_000,
        long outputUnits = 500) =>
        new(CapabilityClass.EditorialReasoning,
            ReasoningTier.Standard,
            new QualityRating(floor),
            new ContextCapacity(context),
            new Money(ceiling),
            criticality,
            Attribution(),
            new EstimatedUnits(inputUnits, outputUnits, 0),
            TimeSpan.FromHours(4),
            reducedFloor ?? ReducedFloorPolicy.Forbidden);

    internal static ResolutionInputs Inputs(
        IReadOnlyList<Route> routes,
        IReadOnlyList<ForbiddenSource>? forbidden = null,
        IReadOnlyDictionary<RouteId, RouteAvailability>? availability = null,
        IReadOnlyDictionary<ProviderAccountId, ProviderAccountStatus>? accounts = null,
        IReadOnlyDictionary<ModelId, UnitPrices>? prices = null,
        decimal budgetRemaining = 100m) =>
        new()
        {
            AdmittedRoutes = routes,
            ForbiddenSources = forbidden ?? [],
            Availability = availability ?? AllServing(routes),
            AccountStatus = accounts ?? AllActive(),
            Prices = prices ?? DefaultPrices(),
            BudgetRemaining = new Money(budgetRemaining),
            Now = Now,
        };

    internal static Dictionary<RouteId, RouteAvailability> AllServing(IReadOnlyList<Route> routes) =>
        routes.ToDictionary(r => r.Id, r => RouteAvailability.Serving(r.Id, Now));

    internal static Dictionary<ProviderAccountId, ProviderAccountStatus> AllActive() => new()
    {
        [PrimaryAccount] = ProviderAccountStatus.Active,
        [SecondaryAccount] = ProviderAccountStatus.Active,
        [EmergencyAccount] = ProviderAccountStatus.Active,
    };

    /// <summary>
    /// Unit prices used only by the pre-flight ceiling estimate. They are ESTIMATES: no unit price
    /// in this repository has been verified first-hand, and risk RK-002 requires a re-fetch
    /// immediately before any spend.
    /// </summary>
    internal static Dictionary<ModelId, UnitPrices> DefaultPrices() => new()
    {
        [PrimaryModel] = new UnitPrices(0.000_003m, 0.000_015m, 0.000_000_3m, "USD"),
        [SecondaryModel] = new UnitPrices(0.000_001m, 0.000_005m, 0.000_000_1m, "USD"),
        [EmergencyModel] = new UnitPrices(0.000_010m, 0.000_050m, 0.000_001m, "USD"),
    };
}
