namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// Why a resolution produced no route. Every member is recorded; no path returns an unexplained
/// failure (constraint C-020).
/// </summary>
public enum RefusalReason
{
    /// <summary>The request named a source the forbidden-source register holds (resolution step 1).</summary>
    ForbiddenSource = 1,

    /// <summary>The capability has no admitted route at all.</summary>
    NoAdmittedRoute = 2,

    /// <summary>Every admitted route sat below the request's quality floor (resolution step 2).</summary>
    NoRouteAtOrAboveFloor = 3,

    /// <summary>Every surviving route lacked the required context capacity (resolution step 2).</summary>
    InsufficientContextCapacity = 4,

    /// <summary>Every surviving route was unavailable or its account disabled (resolution step 3).</summary>
    NoAvailableRoute = 5,

    /// <summary>Every surviving route exceeded the cost ceiling or the governing budget (resolution step 4).</summary>
    CostCeilingOrBudgetExceeded = 6,

    /// <summary>
    /// A route's recorded price is denominated in a currency the request's ceiling is not, so the
    /// two are not comparable. The route is removed and the reason is recorded, because a request
    /// must never fail without one.
    /// </summary>
    CurrencyMismatch = 7,

    // The AI-economics change (decisions D-004 and D-005 of its design). Each is its own reason, so a
    // refusal names what refused it and is never read as another: in particular a missing budget
    // amount and unmeasured spend are never read as the budget exceeded.

    /// <summary>
    /// The governing channel reading has no budget amount recorded for the booking month, so metered
    /// work is refused; work costing nothing stays admissible.
    /// </summary>
    BudgetAmountNotRecorded = 8,

    /// <summary>
    /// A governing reading's booked spend cannot be stated, because an operation booked into the month
    /// for its scope carries an unstated cost, so metered work is refused; work costing nothing stays
    /// admissible.
    /// </summary>
    SpendUnmeasured = 9,

    /// <summary>A governing reading reached the 90 percent threshold, so metered work is deferred and held.</summary>
    DeferredAtThreshold = 10,

    /// <summary>A governing reading reached the 100 percent threshold, so metered work is refused.</summary>
    RefusedAtThreshold = 11,

    /// <summary>
    /// A provider route's model has no price in force at the booking instant for one of the input,
    /// output and cached unit kinds, so it is removed rather than estimated at zero.
    /// </summary>
    PriceNotInForce = 12,

    /// <summary>
    /// Every surviving route states a lower reasoning tier than requested, and no controller downgrade
    /// was directed, so none is served.
    /// </summary>
    NoRouteAtRequestedTier = 13,

    /// <summary>
    /// A metered admission of the company scope, or of the attribution channel's scope, is already in
    /// progress, so this metered admission is deferred and held from the reserved booking instant rather than
    /// waiting on the scope's hold (the second correction cycle): an admission never waits on a scope hold,
    /// so no wait duration is chosen and no admission ends in an unnamed timeout.
    /// </summary>
    MeteredAdmissionInProgress = 14,

    /// <summary>
    /// The production change (decision D-006 of its design): every surviving provider route's WORST CASE would
    /// take the item's counted total past its recorded cap, so the call is not made. The counted total is the
    /// stated cost of the item's booked operations plus the worst case of every reservation of the item that no
    /// booking has reconciled; the refusal names the cap, the counted total and the worst case.
    /// </summary>
    ItemCapExceeded = 15,
}

/// <summary>
/// The outcome of resolving a capability request. Resolution never downgrades: when the survivor
/// set is empty the request is held with its floor, its reason and the routes tried, and it is
/// never quietly served by a route below the floor (constraint C-003, resolution step 6).
/// </summary>
public abstract record CapabilityResolution
{
    private CapabilityResolution()
    {
    }

    /// <summary>A route at or above the declared floor was selected.</summary>
    public sealed record Resolved(
        Route Route,
        QualityRating EffectiveFloorApplied,
        bool FloorWasReduced,
        IReadOnlyList<RouteId> RoutesTried) : CapabilityResolution
    {
        /// <summary>Where the route stands against the requested reasoning tier (the AI-economics change).</summary>
        public TierOutcome TierOutcome { get; init; } = TierOutcome.Untiered;

        /// <summary>Whether the route was taken on evidence or on the configured ordering (the AI-economics change).</summary>
        public SelectionBasis Basis { get; init; } = SelectionBasis.Configured;
    }

    /// <summary>
    /// No route at or above the floor survived. The request is a first-class held state carrying
    /// its floor, its reason and the routes tried, and escalates on its declared hold timeout.
    /// </summary>
    public sealed record Held(
        RefusalReason Reason,
        QualityRating FloorRequired,
        IReadOnlyList<RouteId> RoutesTried,
        DateTimeOffset EscalatesAt,
        bool EscalateToOwner) : CapabilityResolution;

    /// <summary>
    /// The request resolved to zero reachable routes because it named a forbidden source. This is
    /// the resolution-time half of the two independent refusals constraint C-001 requires.
    /// </summary>
    public sealed record Refused(
        RefusalReason Reason,
        string Detail,
        ForbiddenSourceKind? ForbiddenKind) : CapabilityResolution;
}
