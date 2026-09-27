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

    /// <summary>
    /// The reasoning tier the request asked for (decision D-006), written at the resolution
    /// boundary in the same transaction as the work.
    /// </summary>
    public ReasoningTier? ReasoningTierRequested { get; init; }

    /// <summary>
    /// The reasoning tier the admitted route actually served.
    ///
    /// Null is an EXPLICIT ABSENCE MARKER, not an unset field: it records that the admitted route
    /// could not state a tier, so a consumer never reads an inferred value. Without this attribute
    /// the routing argument that carries the company's cost thesis stays untestable however many
    /// items are produced, which is why it is recorded per operation rather than derived later.
    /// </summary>
    public ReasoningTier? ReasoningTierServed { get; init; }

    /// <summary>
    /// Whether the tier evidence is present for this record. A record where the request named a
    /// tier and the route stated one is testable; anything else is recorded as absent and says so.
    /// </summary>
    public bool CarriesTierEvidence =>
        ReasoningTierRequested is not null && ReasoningTierServed is not null;
}
