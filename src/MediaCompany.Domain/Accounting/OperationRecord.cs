using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Domain.Accounting;

public enum OperationOutcome
{
    Succeeded = 1,
    Failed = 2,
    Refused = 3,
    Held = 4,
}

/// <summary>
/// The unit counts one attempt consumed. Recorded as returned by the provider where assumption
/// A-002 holds, and as an estimate otherwise, which <see cref="OperationRecord.CostBasis"/> states.
/// </summary>
public sealed record UnitCounts(long InputUnits, long OutputUnits, long CachedUnits, long OtherUnits)
{
    public static UnitCounts None { get; } = new(0, 0, 0, 0);
}

/// <summary>
/// One accounted operation. A retry is its own record with its own outcome, so exactly-once under
/// constraint C-004 means one record per attempt (decision D-011).
///
/// The record stores the applied price row reference rather than only the computed figure, which
/// is what keeps the arithmetic re-derivable after the price has changed (assumption A-006), and
/// the computed cost itself is produced by the datastore's exact decimal type rather than here
/// (decision D-006). <see cref="ComputedCost"/> is therefore read back from the store, never
/// calculated in this type.
/// </summary>
public sealed record OperationRecord
{
    public required OperationId Id { get; init; }
    public required RunId Run { get; init; }
    public required Attribution Attribution { get; init; }
    public required CapabilityClass Capability { get; init; }

    /// <summary>The route actually used. Null when the operation was refused before a route was selected.</summary>
    public RouteId? Route { get; init; }

    /// <summary>The model actually used. Null for a hold, a substitute, or a refusal.</summary>
    public ModelId? Model { get; init; }

    /// <summary>
    /// The deterministic task this operation served, when it is a member of the named zero-AI-cost
    /// set. A record carrying this name and a non-zero cost is what acceptance criterion AC-017
    /// measures against.
    /// </summary>
    public string? DeterministicTaskName { get; init; }

    public required UnitCounts Units { get; init; }

    /// <summary>The price row applied, retained so the arithmetic stays re-derivable (risk R-006).</summary>
    public ModelPriceId? AppliedPrice { get; init; }

    /// <summary>Computed by the datastore from the units and the applied price; never computed here.</summary>
    public Money ComputedCost { get; init; } = Money.Zero();

    public required CostBasis CostBasis { get; init; }
    public required TimeSpan Duration { get; init; }
    public required OperationOutcome Outcome { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }

    /// <summary>The attempt ordinal within its stage. One record per attempt (decision D-011).</summary>
    public required int Attempt { get; init; }

    public string? FailureReason { get; init; }
}
