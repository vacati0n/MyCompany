using MediaCompany.Deterministic.Accounting;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
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

    /// <summary>
    /// Unit prices in force at the booking instant, by model, holding ONLY the models priced for every
    /// one of the input, output and cached unit kinds. A provider route whose model is absent here is
    /// removed at step 4 with its own reason; it is never estimated at zero (the AI-economics change).
    /// </summary>
    public required IReadOnlyDictionary<ModelId, UnitPrices> Prices { get; init; }

    /// <summary>
    /// What remains of the governing channel and department budgets for the booking month, as an
    /// amount, read in the same statement as the controller's readings.
    /// </summary>
    public required Money BudgetRemaining { get; init; }

    /// <summary>The resolution instant: the booking instant the datastore reserved for this admission.</summary>
    public required DateTimeOffset Now { get; init; }

    /// <summary>
    /// The cost controller's decision for the booking month (the AI-economics change, decision D-004 of
    /// its design), or null where none was taken, which directs nothing.
    /// </summary>
    public ControllerDecision? Controller { get; init; }

    /// <summary>
    /// The evidence table for the request's task class, read in one statement at the booking instant;
    /// empty where none was read, which yields the configured ordering (decision D-003).
    /// </summary>
    public IReadOnlyList<BenchmarkObservation> Evidence { get; init; } = [];

    /// <summary>
    /// The item's recorded cap and its counted total (the production change, decision D-006 of its design), or
    /// null where the item has none, which keeps the delivered resolution exactly. Under a cap, a provider route
    /// whose WORST CASE would take the counted total past the cap is removed, and where none survives the request
    /// is refused under its own reason naming the cap, the counted total and the worst case.
    /// </summary>
    public ItemCapReading? ItemCap { get; init; }
}

/// <summary>The unit prices of one model, in one currency.</summary>
public sealed record UnitPrices(decimal PerInputUnit, decimal PerOutputUnit, decimal PerCachedUnit, string Currency)
{
    public static UnitPrices Free(string currency = Money.DefaultCurrency) => new(0m, 0m, 0m, currency);

    /// <summary>The price of one character (the production change, decision D-007). Zero where the model is not billed by it.</summary>
    public decimal PerCharacterUnit { get; init; }

    /// <summary>The price of one image (the production change, decision D-007). Zero where the model is not billed by it.</summary>
    public decimal PerImageUnit { get; init; }

    /// <summary>
    /// The price table entry of one model from the prices in force (the production change, decision D-007 of its
    /// design): a model enters ONLY where every unit kind it is billed by has a price in force. Where its billed
    /// kinds are not recorded it is billed by input, output and cached units, as delivered. A kind it is not
    /// billed by needs no price and is costed at zero. Where two rows of one kind are in force the higher is taken.
    /// </summary>
    public static UnitPrices? From(IEnumerable<ModelPrice> inForce, IReadOnlyList<PriceUnitKind>? billedKinds)
    {
        ArgumentNullException.ThrowIfNull(inForce);
        var rows = inForce.ToList();
        IReadOnlyList<PriceUnitKind> billed = billedKinds is { Count: > 0 }
            ? billedKinds
            : [PriceUnitKind.InputUnit, PriceUnitKind.OutputUnit, PriceUnitKind.CachedUnit];

        ModelPrice? Highest(PriceUnitKind kind) => rows.Where(p => p.UnitKind == kind).MaxBy(p => p.UnitPrice);

        if (billed.Any(kind => Highest(kind) is null))
        {
            return null;
        }

        decimal Price(PriceUnitKind kind) => billed.Contains(kind) ? Highest(kind)!.UnitPrice : 0m;

        return new UnitPrices(
            Price(PriceUnitKind.InputUnit),
            Price(PriceUnitKind.OutputUnit),
            Price(PriceUnitKind.CachedUnit),
            Highest(billed[0])!.Currency)
        {
            PerCharacterUnit = Price(PriceUnitKind.CharacterUnit),
            PerImageUnit = Price(PriceUnitKind.ImageUnit),
        };
    }
}

/// <summary>
/// One resolution and what it rested on: the outcome, the selection record, and what the tier rule did.
/// </summary>
public sealed record ResolutionRecord(CapabilityResolution Resolution, SelectionRecord Selection, string TierStatement);

/// <summary>
/// The capability route resolution function (module M-001, decision D-001), and a member of the
/// zero-AI-cost set under the name <see cref="DeterministicTaskRegistry.ProviderRouting"/>.
///
/// The steps are the ones the technical design fixes, extended by the AI-economics change. Two
/// properties are structural rather than guarded, and the shape of this function is what makes them so:
///
/// 1. The quality floor filter runs at step 2, before any ordering. A route below the floor is removed
///    from the candidate set and is never reachable by the ordering or the evidence ranking, so there is
///    no branch that could admit one (constraint C-003).
/// 2. The empty survivor set resolves to <see cref="CapabilityResolution.Held"/>. The function returns a
///    typed union with no member that could carry a below-floor route.
///
/// The AI-economics change adds, in order: the controller's restriction to targets that cost nothing
/// under a deferral, a refusal or an unmeasured reading; the removal of a provider route whose model has
/// no price in force for a metered unit kind; the reasoning-tier classes, under which a route stating a
/// LOWER tier than requested is served only as a recorded controller downgrade for a routine request;
/// and evidence-ready selection within the best tier class.
/// </summary>
public static class RouteResolver
{
    public const string TaskName = DeterministicTaskRegistry.ProviderRouting;

    public static CapabilityResolution Resolve(CapabilityRequest request, ResolutionInputs inputs) =>
        ResolveWithRecord(request, inputs).Resolution;

    public static ResolutionRecord ResolveWithRecord(CapabilityRequest request, ResolutionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(inputs);

        var tried = new List<RouteId>();
        const string NoTier = "no route was resolved, so no tier outcome applies";

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
                return Ended(
                    request,
                    new CapabilityResolution.Refused(
                        RefusalReason.ForbiddenSource,
                        $"Route {route.Id} for {route.Capability} matches forbidden source '{match.Identifier}': {match.Reason}",
                        match.Kind),
                    "a route matched the forbidden-source register at step 1",
                    NoTier);
            }

            // Step 1, continued (the production change, decision D-003 of its design): the route's RECORDED TERMS
            // POSITIONS. A provider route whose terms prohibit automated access, or take a licence over the
            // customer's content, is refused under the kind it matches; a narration or still-image provider route
            // whose positions are not recorded is refused under the kind whose position is missing, because a
            // position nobody recorded is never read as permitted. Nothing is sent on a refusal.
            if (TermsRefusal(route) is { } terms)
            {
                return Ended(
                    request,
                    new CapabilityResolution.Refused(RefusalReason.ForbiddenSource, terms.Detail, terms.Kind),
                    "a route's recorded terms positions refused it at step 1",
                    NoTier);
            }
        }

        if (candidates.Count == 0)
        {
            return Ended(request, Hold(request, inputs, RefusalReason.NoAdmittedRoute, tried), "no route is admitted for the capability", NoTier);
        }

        // Step 2. The floor and the context requirement, applied before any ordering. A route
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
            return Ended(request, Hold(request, inputs, reason, tried), "no route met the floor and the context requirement at step 2", NoTier);
        }

        // Step 3. Disabled or revoked accounts, and routes not in the serving state.
        var available = aboveFloor.Where(r => IsAvailable(r, inputs)).ToList();
        if (available.Count == 0)
        {
            return Ended(request, Hold(request, inputs, RefusalReason.NoAvailableRoute, tried), "no route was available at step 3", NoTier);
        }

        // The controller's restriction (the AI-economics change, decision D-004). Under a refusal, a
        // deferral or a reading nobody can state, only the targets that reach no provider stay
        // admissible, so work costing nothing is never refused by a spending control. Where none
        // survives, a deferral is held with its own reason and the booking instant plus the request's
        // hold timeout, and a refusal is refused naming its threshold or reason and its scope.
        if (inputs.Controller is { } controller && CostController.RestrictsToZeroCost(controller.Action))
        {
            var free = available.Where(r => r.Target is not RouteTarget.ProviderRoute).ToList();
            if (free.Count == 0)
            {
                var detail = ControllerDetail(controller);
                return controller.Action == ControllerAction.Defer
                    ? Ended(request, Hold(request, inputs, RefusalReason.DeferredAtThreshold, tried), detail, NoTier)
                    : Ended(
                        request,
                        new CapabilityResolution.Refused(controller.Reason ?? RefusalReason.RefusedAtThreshold, detail, null),
                        detail,
                        NoTier);
            }

            available = free;
        }

        // Step 4. A provider route whose model has no price in force for every metered unit kind is
        // removed with its own reason: a missing price never reads as zero (the AI-economics change).
        if (available.Any(r => !Priced(r, inputs)))
        {
            var priced = available.Where(r => Priced(r, inputs)).ToList();
            if (priced.Count == 0)
            {
                return Ended(request, Hold(request, inputs, RefusalReason.PriceNotInForce, tried),
                    "no surviving route had a price in force for every metered unit kind at step 4", NoTier);
            }

            available = priced;
        }

        // Step 4, continued. The cost ceiling and the governing budget. Both comparisons are made
        // against an ESTIMATE: the unit prices are the recorded ones and the unit counts are the
        // request's own estimate. The authoritative cost of the operation is computed by the datastore
        // from the units the provider actually returned (decision D-006), so this step admits or removes
        // a route and never establishes what an operation cost. A route whose recorded price is not
        // denominated in the request's currency is not comparable, so it is removed with its own
        // recorded reason rather than raising.
        if (available.Any(r => !Comparable(r, request, inputs)))
        {
            var comparable = available.Where(r => Comparable(r, request, inputs)).ToList();
            if (comparable.Count == 0)
            {
                return Ended(request, Hold(request, inputs, RefusalReason.CurrencyMismatch, tried),
                    "no surviving route's price was comparable with the request's ceiling at step 4", NoTier);
            }

            available = comparable;
        }

        // Step 4, under a recorded item cap (the production change, decision D-006 of its design): a provider
        // route whose WORST CASE would take the item's counted total past its cap is removed BEFORE the budget
        // comparison, and where none survives the call is refused naming the cap, the counted total and the
        // cheapest worst case. The worst case is the request's estimated units at the prices in force, which the
        // admission then reserves durably before the call.
        if (inputs.ItemCap is { } cap)
        {
            var withinCap = available.Where(r => WithinItemCap(r, request, inputs, cap)).ToList();
            if (withinCap.Count == 0)
            {
                var cheapest = available
                    .Select(r => EstimateCost(r, request, inputs)!.Value)
                    .OrderBy(m => m.Amount)
                    .First();
                var detail = $"the worst case of the call, {cheapest}, would take the item's counted total past its cap: "
                    + cap.Describe();
                return Ended(
                    request,
                    new CapabilityResolution.Refused(RefusalReason.ItemCapExceeded, detail, null),
                    detail,
                    NoTier);
            }

            available = withinCap;
        }

        var affordable = available.Where(r => WithinCeilingAndBudget(r, request, inputs)).ToList();
        if (affordable.Count == 0)
        {
            return Ended(request, Hold(request, inputs, RefusalReason.CostCeilingOrBudgetExceeded, tried),
                "no surviving route fitted the cost ceiling and the governing budget at step 4", NoTier);
        }

        // Step 5. The reasoning-tier classes (decision D-005).
        var (tierClass, outcome, tierStatement) = TierClass(request, affordable, inputs.Controller);
        if (tierClass.Count == 0)
        {
            return Ended(request, Hold(request, inputs, RefusalReason.NoRouteAtRequestedTier, tried),
                "every surviving route states a lower reasoning tier than requested", tierStatement);
        }

        // Step 6. Within the class, evidence where every compared quantity is observed in one unit,
        // and otherwise the delivered route-tier ordering exactly (decision D-003).
        var selection = EvidenceSelection.Select(
            tierClass, request.TaskClass, effectiveFloor, request.CostCeiling.Currency, inputs.Evidence);

        if (selection.Selected is not { } selected)
        {
            return new ResolutionRecord(
                Hold(request, inputs, RefusalReason.NoRouteAtOrAboveFloor, tried),
                selection.Record,
                tierStatement);
        }

        return new ResolutionRecord(
            new CapabilityResolution.Resolved(selected, effectiveFloor, floorWasReduced, tried)
            {
                TierOutcome = outcome,
                Basis = selection.Record.Basis,
            },
            selection.Record,
            tierStatement);
    }

    /// <summary>
    /// The tier class to rank within, how a route of it stands against the requested tier, and what the
    /// rule did. Among the survivors, the classes are the routes stating the requested tier, those
    /// stating a higher one, and those stating none, in that order; a route stating a LOWER tier is
    /// never served below the downgrade action. Under the downgrade action, for a routine request, the
    /// preferred class is the routes stating the highest tier strictly below the requested one; where it
    /// is empty, or the request is critical, the ordinary classes apply and the statement says why no
    /// downgrade was taken. A route stating no tier is never a downgrade.
    /// </summary>
    private static (List<Route> Class, TierOutcome Outcome, string Statement) TierClass(
        CapabilityRequest request,
        IReadOnlyList<Route> survivors,
        ControllerDecision? controller)
    {
        var requested = request.ReasoningTier;
        string? downgradeNote = null;

        if (controller is { Action: ControllerAction.Downgrade })
        {
            var below = survivors
                .Where(r => r.StatedReasoningTier is { } stated && stated < requested)
                .ToList();

            if (request.Criticality == Criticality.Critical)
            {
                downgradeNote = $"the controller directed a downgrade ({ControllerDetail(controller)}), and none was "
                    + "taken because the request is critical, and a critical request never downgrades";
            }
            else if (below.Count == 0)
            {
                downgradeNote = $"the controller directed a downgrade ({ControllerDetail(controller)}), and none was "
                    + $"available because no surviving route states a reasoning tier below {requested}; a route stating "
                    + "no tier is never a downgrade";
            }
            else
            {
                var highest = below.Max(r => r.StatedReasoningTier!.Value);
                var downgraded = below.Where(r => r.StatedReasoningTier == highest).ToList();
                return (
                    downgraded,
                    TierOutcome.Downgraded,
                    $"downgraded from {requested} to {highest}, the highest stated tier below the requested one, under "
                    + $"the controller's downgrade action ({ControllerDetail(controller)}); the floor of "
                    + $"{request.QualityFloor} still applied and the request is routine");
            }
        }

        var at = survivors.Where(r => r.StatedReasoningTier == requested).ToList();
        var higher = survivors.Where(r => r.StatedReasoningTier is { } stated && stated > requested).ToList();
        var untiered = survivors.Where(r => r.StatedReasoningTier is null).ToList();
        var prefix = downgradeNote is null ? string.Empty : downgradeNote + "; ";

        if (at.Count > 0)
        {
            return (at, TierOutcome.AtRequestedTier, prefix + $"served at the requested tier, {requested}");
        }

        if (higher.Count > 0)
        {
            return (higher, TierOutcome.HigherTier,
                prefix + $"no surviving route states {requested}, so a route stating a higher tier is served");
        }

        if (untiered.Count > 0)
        {
            return (untiered, TierOutcome.Untiered,
                prefix + $"no surviving route states {requested} or a higher tier, so a route stating no tier is served");
        }

        return ([], TierOutcome.Untiered,
            prefix + $"every surviving route states a tier below {requested}, and no lower tier is served except as a "
            + "recorded controller downgrade for a routine request");
    }

    private static string ControllerDetail(ControllerDecision controller)
    {
        var deciding = controller.Readings.FirstOrDefault(r => r.Scope == controller.DecidedBy && r.Action == controller.Action);
        var basis = deciding is null
            ? $"{controller.Action} in {controller.BookingMonth:yyyy-MM}"
            : deciding.Statement;
        return $"the cost controller's {controller.Action} for {controller.BookingMonth:yyyy-MM}: {basis}";
    }

    private static ResolutionRecord Ended(CapabilityRequest request, CapabilityResolution resolution, string why, string tierStatement) =>
        new(resolution, EvidenceSelection.NotRanked(request.TaskClass, why), tierStatement);

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

    // An unrecorded route has no serving state, so it is not a candidate. Decision D-008 makes availability
    // explicit rather than inferred, and absence is not "serving". The step is defined once, beside the
    // comparable-run count, so the CTO report excludes exactly the routes this step removes (the AI-management
    // change, correction cycle).
    private static bool IsAvailable(Route route, ResolutionInputs inputs) =>
        ComparableRuns.IsAvailable(route, inputs.Availability, inputs.AccountStatus);

    /// <summary>
    /// Whether a route can be estimated at all: a target reaching no provider costs nothing, and a
    /// provider route only where its model is priced in force for every metered unit kind.
    /// </summary>
    private static bool Priced(Route route, ResolutionInputs inputs) =>
        route.Target is not RouteTarget.ProviderRoute provider || inputs.Prices.ContainsKey(provider.Model);

    /// <summary>
    /// Whether the route's estimated cost can be compared with the request's ceiling and with the
    /// remaining budget at all. Acceptance criterion AC-002 requires that no request fails without
    /// a recorded reason, so a currency that cannot be compared is a recorded refusal rather than
    /// an exception escaping the single egress path.
    /// </summary>
    private static bool Comparable(Route route, CapabilityRequest request, ResolutionInputs inputs)
    {
        var estimate = EstimateCost(route, request, inputs)!.Value;
        return estimate.IsComparableTo(request.CostCeiling)
            && estimate.IsComparableTo(inputs.BudgetRemaining);
    }

    /// <summary>Whether a route's worst case fits what remains of the item's cap. A target reaching no provider costs nothing.</summary>
    private static bool WithinItemCap(Route route, CapabilityRequest request, ResolutionInputs inputs, ItemCapReading cap)
    {
        if (route.Target is not RouteTarget.ProviderRoute)
        {
            return true;
        }

        var worst = EstimateCost(route, request, inputs)!.Value;
        return worst.IsComparableTo(cap.Cap) && worst.Amount <= cap.Remaining.Amount;
    }

    /// <summary>
    /// The terms-position refusal of a route, or null (the production change, decision D-003 of its design). Only
    /// provider routes are judged; a narration or still-image provider route must record both positions.
    /// </summary>
    private static (ForbiddenSourceKind Kind, string Detail)? TermsRefusal(Route route)
    {
        if (route.Target is not RouteTarget.ProviderRoute provider)
        {
            return null;
        }

        var positions = route.TermsPositions;
        var evidence = positions?.EvidenceReference is { } reference
            ? $" (evidence: {reference}, read {positions.EvidenceReadOn:yyyy-MM-dd})"
            : string.Empty;

        if (positions?.AutomatedAccess == AutomatedAccessPosition.Prohibits)
        {
            return (ForbiddenSourceKind.AutomatedAccessProhibited,
                $"Route {route.Id} for {route.Capability} ({provider.ProviderAccount}): its recorded terms prohibit automated access{evidence}");
        }

        if (positions?.CustomerContent == CustomerContentPosition.LicenceTaken)
        {
            return (ForbiddenSourceKind.CustomerContentLicence,
                $"Route {route.Id} for {route.Capability} ({provider.ProviderAccount}): its recorded terms take a licence over the customer's content{evidence}");
        }

        var judged = route.Capability is CapabilityClass.Narration or CapabilityClass.StillImages;
        if (judged && positions?.AutomatedAccess is null)
        {
            return (ForbiddenSourceKind.AutomatedAccessProhibited,
                $"Route {route.Id} for {route.Capability} ({provider.ProviderAccount}): no position on automated access is recorded, and an unrecorded position is never read as permitted");
        }

        if (judged && positions?.CustomerContent is null)
        {
            return (ForbiddenSourceKind.CustomerContentLicence,
                $"Route {route.Id} for {route.Capability} ({provider.ProviderAccount}): no position on a licence over the customer's content is recorded, and an unrecorded position is never read as permitted");
        }

        return null;
    }

    private static bool WithinCeilingAndBudget(Route route, CapabilityRequest request, ResolutionInputs inputs)
    {
        var estimate = EstimateCost(route, request, inputs)!.Value;
        if (estimate > request.CostCeiling)
        {
            return false;
        }

        return estimate <= inputs.BudgetRemaining;
    }

    /// <summary>
    /// The pre-flight cost estimate. A hold-and-escalate position and a non-AI substitute cost
    /// nothing, because neither reaches a provider. A provider route whose model has no price in force
    /// has NO ESTIMATE, returned as null rather than as zero (the AI-economics change): the missing price
    /// is a reason to remove the route, never a free route.
    /// </summary>
    public static Money? EstimateCost(Route route, CapabilityRequest request, ResolutionInputs inputs)
    {
        ArgumentNullException.ThrowIfNull(route);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(inputs);

        if (route.Target is not RouteTarget.ProviderRoute provider)
        {
            return Money.Zero(request.CostCeiling.Currency);
        }

        if (!inputs.Prices.TryGetValue(provider.Model, out var prices))
        {
            return null;
        }

        var amount =
            (request.EstimatedUnits.InputUnits * prices.PerInputUnit) +
            (request.EstimatedUnits.OutputUnits * prices.PerOutputUnit) +
            (request.EstimatedUnits.CachedUnits * prices.PerCachedUnit) +
            (request.EstimatedUnits.CharacterUnits * prices.PerCharacterUnit) +
            (request.EstimatedUnits.ImageUnits * prices.PerImageUnit);

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
    /// on. The instant is derived from the supplied resolution instant — the booking instant the
    /// datastore reserved — and the request's own hold timeout, so a held state is complete when it is
    /// returned and no caller has to stamp it. The function still reads no clock: the instant is an input.
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
