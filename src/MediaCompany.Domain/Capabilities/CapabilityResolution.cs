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
        IReadOnlyList<RouteId> RoutesTried) : CapabilityResolution;

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
