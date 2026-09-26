namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// Availability is an explicit recorded state per route with recorded transitions, not a property
/// inferred from recent failures (decision D-008).
/// </summary>
public enum AvailabilityState
{
    Serving = 1,
    QuotaExhausted = 2,
    Outage = 3,
    QualityDegraded = 4,
    AccountDisabled = 5,
    AccountRevoked = 6,
}

/// <summary>One recorded availability state for one route, effective from an instant.</summary>
public sealed record RouteAvailability
{
    public RouteAvailability(
        RouteId route,
        AvailabilityState state,
        DateTimeOffset effectiveFrom,
        string reason,
        DateTimeOffset? resetOrProbePoint,
        QualityRating? observedQuality)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Every availability transition carries its reason.", nameof(reason));
        }

        Route = route;
        State = state;
        EffectiveFrom = effectiveFrom;
        Reason = reason;
        ResetOrProbePoint = resetOrProbePoint;
        ObservedQuality = observedQuality;
    }

    public RouteId Route { get; }
    public AvailabilityState State { get; }
    public DateTimeOffset EffectiveFrom { get; }
    public string Reason { get; }

    /// <summary>The recorded reset point for a quota, or the half-open probe schedule for an outage.</summary>
    public DateTimeOffset? ResetOrProbePoint { get; }

    /// <summary>
    /// On quality degradation the route's effective rating drops to the observed level, removing
    /// it from every request whose floor is above it.
    /// </summary>
    public QualityRating? ObservedQuality { get; }

    public static RouteAvailability Serving(RouteId route, DateTimeOffset at) =>
        new(route, AvailabilityState.Serving, at, "admitted", null, null);
}
