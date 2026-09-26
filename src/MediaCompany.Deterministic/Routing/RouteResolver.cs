using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Deterministic.Routing;

/// <summary>
/// Everything the resolution function reads. It is supplied rather than fetched, which is what
/// keeps <see cref="RouteResolver"/> a pure function and a member of the zero-AI-cost set.
/// </summary>
public sealed record ResolutionInputs
{
    public required IReadOnlyList<Route> AdmittedRoutes { get; init; }
    public required IReadOnlyList<ForbiddenSource> ForbiddenSources { get; init; }
    public required IReadOnlyDictionary<RouteId, RouteAvailability> Availability { get; init; }
    public required IReadOnlyDictionary<ProviderAccountId, ProviderAccountStatus> AccountStatus { get; init; }

    /// <summary>Unit prices in force, by model. Used only by the pre-flight ceiling estimate.</summary>
    public required IReadOnlyDictionary<ModelId, UnitPrices> Prices { get; init; }

    /// <summary>What remains of the governing budget for the period, as an amount.</summary>
    public required Money BudgetRemaining { get; init; }

    public required DateTimeOffset Now { get; init; }
}

/// <summary>The unit prices of one model, in one currency.</summary>
public sealed record UnitPrices(decimal PerInputUnit, decimal PerOutputUnit, decimal PerCachedUnit, string Currency)
{
    public static UnitPrices Free(string currency = Money.DefaultCurrency) => new(0m, 0m, 0m, currency);
}

/// <summary>
/// The capability route resolution function (module M-001, decision D-001), and a member of the
/// zero-AI-cost set under the name <see cref="DeterministicTaskRegistry.ProviderRouting"/>.
///
/// The six steps are the ones the technical design fixes. Two properties are structural rather
/// than guarded, and the shape of this function is what makes them so:
///
/// 1. The quality floor filter runs at step 2, before tier ordering at step 5. A route below the
///    floor is removed from the candidate set and is never reachable by the ordering, so there is
///    no branch that could admit one (constraint C-003).
/// 2. The empty survivor set at step 6 resolves to <see cref="CapabilityResolution.Held"/>. There
///    is no downgrade path, because the function returns a typed union with no member that could
///    carry a below-floor route.
/// </summary>
public static class RouteResolver
{
    public const string TaskName = DeterministicTaskRegistry.ProviderRouting;

    public static CapabilityResolution Resolve(CapabilityRequest request, ResolutionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(inputs);

        var tried = new List<RouteId>();

        var candidates = inputs.AdmittedRoutes
            .Where(r => r.Capability == request.Capability)
            .ToList();

        // Step 1. The forbidden-source register, checked at resolution over the routes this
        // request could actually reach. This is the second and independent refusal of constraint
        // C-001: route admission already refuses these, so an admission-time check and this one
        // would both have to fail before a forbidden path existed. A match removes the whole
        // candidate set and the refusal ends resolution.
        foreach (var route in candidates)
        {
            var match = MatchForbidden(route, inputs.ForbiddenSources);
            if (match is not null)
            {
                return new CapabilityResolution.Refused(
                    RefusalReason.ForbiddenSource,
                    $"Route {route.Id} for {route.Capability} matches forbidden source '{match.Identifier}': {match.Reason}",
                    match.Kind);
            }
        }

        if (candidates.Count == 0)
        {
            return Hold(request, inputs, RefusalReason.NoAdmittedRoute, tried);
        }

        // Step 2. The floor and the context requirement, applied before tier ordering. A route
        // whose recorded quality has been observed lower than its rating uses the observed value.
        var effectiveFloor = request.QualityFloor;
        var floorWasReduced = false;

        var aboveFloor = FilterByFloor(candidates, inputs, effectiveFloor, request.ContextRequirement, tried);

        if (aboveFloor.Count == 0 &&
            request.Criticality == Criticality.Routine &&
            request.ReducedFloorPolicy is { Permitted: true, ReducedFloor: { } reduced })
        {
            // The request's own declared policy permits a reduced floor. The reduction is recorded
            // and the item is barred from a releasable state until a role re-verifies it.
            effectiveFloor = reduced;
            floorWasReduced = true;
            aboveFloor = FilterByFloor(candidates, inputs, effectiveFloor, request.ContextRequirement, tried);
        }

        if (aboveFloor.Count == 0)
        {
            var reason = candidates.Any(c => EffectiveQuality(c, inputs) >= request.QualityFloor)
                ? RefusalReason.InsufficientContextCapacity
                : RefusalReason.NoRouteAtOrAboveFloor;
            return Hold(request, inputs, reason, tried);
        }

        // Step 3. Disabled or revoked accounts, and routes not in the serving state.
        var available = aboveFloor.Where(r => IsAvailable(r, inputs)).ToList();
        if (available.Count == 0)
        {
            return Hold(request, inputs, RefusalReason.NoAvailableRoute, tried);
        }

        // Step 4. The cost ceiling and the governing budget. Both comparisons are made against an
        // ESTIMATE: the unit prices are the recorded ones and the unit counts are the request's
        // own estimate. The authoritative cost of the operation is computed by the datastore from
        // the units the provider actually returned (decision D-006), so this step admits or
        // removes a route and never establishes what an operation cost.
        // A route whose recorded price is not denominated in the request's currency is not
        // comparable, so it is removed with its own recorded reason rather than raising.
        if (available.Any(r => !Comparable(r, request, inputs)))
        {
            var comparable = available.Where(r => Comparable(r, request, inputs)).ToList();
            if (comparable.Count == 0)
            {
                return Hold(request, inputs, RefusalReason.CurrencyMismatch, tried);
            }

            available = comparable;
        }

        var affordable = available.Where(r => WithinCeilingAndBudget(r, request, inputs)).ToList();
        if (affordable.Count == 0)
        {
            return Hold(request, inputs, RefusalReason.CostCeilingOrBudgetExceeded, tried);
        }

        // Step 5. Order primary, then secondary, then emergency, and resolve to the first.
        var selected = affordable
            .OrderBy(r => (int)r.Tier)
            .ThenBy(r => r.Id.Value)
            .First();

        return new CapabilityResolution.Resolved(selected, effectiveFloor, floorWasReduced, tried);
    }

    private static List<Route> FilterByFloor(
        IReadOnlyList<Route> candidates,
        ResolutionInputs inputs,
        QualityRating floor,
        ContextCapacity contextRequired,
        List<RouteId> tried)
    {
        var kept = new List<Route>();
        foreach (var route in candidates)
        {
            if (!tried.Contains(route.Id))
            {
                tried.Add(route.Id);
            }

            if (EffectiveQuality(route, inputs) < floor)
            {
                continue;
            }

            if (route.ContextCapacity < contextRequired)
            {
                continue;
            }

            kept.Add(route);
        }

        return kept;
    }

    /// <summary>
    /// A recorded quality-degradation observation drops the route's effective rating to the
    /// observed level, which removes it from every request whose floor is above it.
    /// </summary>
    private static QualityRating EffectiveQuality(Route route, ResolutionInputs inputs)
    {
        if (inputs.Availability.TryGetValue(route.Id, out var state) &&
            state.State == AvailabilityState.QualityDegraded &&
            state.ObservedQuality is { } observed)
        {
            return observed;
        }

        return route.RatedQuality;
    }

    private static bool IsAvailable(Route route, ResolutionInputs inputs)
    {
        if (route.Target is RouteTarget.ProviderRoute provider)
        {
            if (!inputs.AccountStatus.TryGetValue(provider.ProviderAccount, out var status) ||
                status != ProviderAccountStatus.Active)
            {
                return false;
            }
        }

        if (!inputs.Availability.TryGetValue(route.Id, out var availability))
        {
            // An unrecorded route has no serving state, so it is not a candidate. Decision D-008
            // makes availability explicit rather than inferred, and absence is not "serving".
            return false;
        }

        return availability.State == AvailabilityState.Serving;
    }

    /// <summary>
    /// Whether the route's estimated cost can be compared with the request's ceiling and with the
    /// remaining budget at all. Acceptance criterion AC-002 requires that no request fails without
    /// a recorded reason, so a currency that cannot be compared is a recorded refusal rather than
    /// an exception escaping the single egress path.
    /// </summary>
    private static bool Comparable(Route route, CapabilityRequest request, ResolutionInputs inputs)
    {
        var estimate = EstimateCost(route, request, inputs);
        return estimate.IsComparableTo(request.CostCeiling)
            && estimate.IsComparableTo(inputs.BudgetRemaining);
    }

    private static bool WithinCeilingAndBudget(Route route, CapabilityRequest request, ResolutionInputs inputs)
    {
        var estimate = EstimateCost(route, request, inputs);
        if (estimate > request.CostCeiling)
        {
            return false;
        }

        return estimate <= inputs.BudgetRemaining;
    }

    /// <summary>
    /// The pre-flight cost estimate. A hold-and-escalate position and a non-AI substitute cost
    /// nothing, because neither reaches a provider.
    /// </summary>
    public static Money EstimateCost(Route route, CapabilityRequest request, ResolutionInputs inputs)
    {
        if (route.Target is not RouteTarget.ProviderRoute provider)
        {
            return Money.Zero(request.CostCeiling.Currency);
        }

        if (!inputs.Prices.TryGetValue(provider.Model, out var prices))
        {
            return Money.Zero(request.CostCeiling.Currency);
        }

        var amount =
            (request.EstimatedUnits.InputUnits * prices.PerInputUnit) +
            (request.EstimatedUnits.OutputUnits * prices.PerOutputUnit) +
            (request.EstimatedUnits.CachedUnits * prices.PerCachedUnit);

        return new Money(amount, prices.Currency);
    }

    private static ForbiddenSource? MatchForbidden(Route route, IReadOnlyList<ForbiddenSource> register)
    {
        var identifiers = new List<string>();
        switch (route.Target)
        {
            case RouteTarget.ProviderRoute provider:
                identifiers.Add(provider.ProviderAccount.Value);
                identifiers.Add(provider.Model.Value);
                break;
            case RouteTarget.NonAiSubstitute substitute:
                identifiers.Add(substitute.DeterministicTaskName);
                break;
        }

        foreach (var entry in register)
        {
            foreach (var identifier in identifiers)
            {
                if (string.Equals(identifier, entry.Identifier, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// A held request carries its floor, its reason, the routes tried and the instant it escalates
    /// on. The instant is derived from the supplied resolution instant and the request's own hold
    /// timeout, so a held state is complete when it is returned and no caller has to stamp it.
    /// The function still reads no clock: the instant is an input.
    /// </summary>
    private static CapabilityResolution Hold(
        CapabilityRequest request,
        ResolutionInputs inputs,
        RefusalReason reason,
        List<RouteId> tried) =>
        new CapabilityResolution.Held(
            reason,
            request.QualityFloor,
            tried.ToArray(),
            inputs.Now + request.HoldTimeout,
            request.Criticality == Criticality.Critical);
}
