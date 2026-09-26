using MediaCompany.Domain.Accounting;

namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// What a caller asks for. Per decision D-012 the request declares what is needed and never who
/// provides it: there is no provider field and no model field, so a caller cannot express a
/// provider preference and the routing position owns the choice.
/// </summary>
public sealed record CapabilityRequest
{
    public CapabilityRequest(
        CapabilityClass capability,
        ReasoningTier reasoningTier,
        QualityRating qualityFloor,
        ContextCapacity contextRequirement,
        Money costCeiling,
        Criticality criticality,
        Attribution attribution,
        EstimatedUnits estimatedUnits,
        TimeSpan holdTimeout,
        ReducedFloorPolicy reducedFloorPolicy)
    {
        ArgumentNullException.ThrowIfNull(attribution);
        ArgumentNullException.ThrowIfNull(estimatedUnits);
        ArgumentNullException.ThrowIfNull(reducedFloorPolicy);

        if (criticality == Criticality.Critical && reducedFloorPolicy.Permitted)
        {
            throw new ArgumentException(
                "A critical request may not carry a reduced-floor policy; resolution step 6 holds it instead.",
                nameof(reducedFloorPolicy));
        }

        Capability = capability;
        ReasoningTier = reasoningTier;
        QualityFloor = qualityFloor;
        ContextRequirement = contextRequirement;
        CostCeiling = costCeiling;
        Criticality = criticality;
        Attribution = attribution;
        EstimatedUnits = estimatedUnits;
        HoldTimeout = holdTimeout;
        ReducedFloorPolicy = reducedFloorPolicy;
    }

    public CapabilityClass Capability { get; }
    public ReasoningTier ReasoningTier { get; }
    public QualityRating QualityFloor { get; }
    public ContextCapacity ContextRequirement { get; }
    public Money CostCeiling { get; }
    public Criticality Criticality { get; }
    public Attribution Attribution { get; }
    public EstimatedUnits EstimatedUnits { get; }
    public TimeSpan HoldTimeout { get; }
    public ReducedFloorPolicy ReducedFloorPolicy { get; }
}

/// <summary>
/// The unit counts a request expects to consume, used only by the pre-flight ceiling filter of
/// resolution step 4. The recorded consumption is what the operation actually returned.
/// </summary>
public sealed record EstimatedUnits(long InputUnits, long OutputUnits, long CachedUnits)
{
    public static EstimatedUnits None { get; } = new(0, 0, 0);
}

/// <summary>
/// Resolution step 6 never downgrades. A non-critical request may carry its own declared policy
/// permitting a reduced floor; when it is used the reduction is recorded and the item is barred
/// from a releasable state until a role re-verifies it.
/// </summary>
public sealed record ReducedFloorPolicy
{
    private ReducedFloorPolicy(bool permitted, QualityRating? reducedFloor)
    {
        Permitted = permitted;
        ReducedFloor = reducedFloor;
    }

    public static ReducedFloorPolicy Forbidden { get; } = new(false, null);

    public static ReducedFloorPolicy AllowDownTo(QualityRating reducedFloor) => new(true, reducedFloor);

    public bool Permitted { get; }
    public QualityRating? ReducedFloor { get; }
}
