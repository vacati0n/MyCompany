using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Domain.Analytics;

// ---------------------------------------------------------------------------
// The AI-economics change: the benchmark record, the selection basis, the cost controller's decision
// and the tier distribution, as read models (module M-018 of its design).
//
// Every quantity on every type here is one case of the closed three-case measurement union, decided
// at one composing site; every recorded amount is a RecordedAmount, never an observation; and every
// string member is an authored label the build-time membership assertion names.
// ---------------------------------------------------------------------------

/// <summary>
/// One benchmark observation as the record holds it, with all seven carried facts: the corpus entry,
/// the task class, the model and the route, and the quality, cost and latency each in its case.
///
/// The quality is the case the observation was written in. The cost and the latency are the
/// datastore's own figures for the operation the observation references: the cost the generated cost
/// of that operation where its cost is stated and unmeasured where it is not, and the latency its
/// recorded duration. No writer supplies either.
/// </summary>
public sealed record BenchmarkObservation
{
    /// <summary>The unit an observed quality is stated in: the floor's closed rating scale.</summary>
    public const string QualityRatingUnit = "rating on the 0 to 100 scale";

    /// <summary>The unit an observed latency is stated in.</summary>
    public const string LatencyUnit = "milliseconds";

    public required BenchmarkObservationId Id { get; init; }
    public required CorpusEntryId Entry { get; init; }
    public required TaskClass TaskClass { get; init; }
    public required RouteId Route { get; init; }
    public required ModelId Model { get; init; }
    public required OperationId Operation { get; init; }
    public required MeasurementQuantity Quality { get; init; }
    public required MeasurementQuantity Cost { get; init; }
    public required MeasurementQuantity Latency { get; init; }

    /// <summary>The datastore's instant, at or above the record horizon and not before the operation's.</summary>
    public required DateTimeOffset ObservedAt { get; init; }

    public required DateOnly Period { get; init; }
}

/// <summary>
/// One route's benchmark position for one task class: the minimum observed quality, the maximum
/// observed cost and the maximum observed latency of its observations, and how many there are. Every
/// quantity is unmeasured, naming the register, the route and the task class looked for, where the
/// record holds no observation of the pair; and unmeasured, naming the observation, where any of them
/// is unmeasured in that quantity.
/// </summary>
public sealed record BenchmarkReading
{
    public required RouteId Route { get; init; }
    public required TaskClass TaskClass { get; init; }
    public required MeasurementQuantity Quality { get; init; }
    public required MeasurementQuantity Cost { get; init; }
    public required MeasurementQuantity Latency { get; init; }
    public required MeasurementQuantity Observations { get; init; }
}

/// <summary>Every admitted route's benchmark position for every task class, read in one statement.</summary>
public sealed record BenchmarkRecordReading
{
    public required IReadOnlyList<BenchmarkReading> Readings { get; init; }

    /// <summary>What the reading is and what it does not establish. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>The observations booked into one month, with the month's finality from the same read.</summary>
public sealed record BenchmarkMonthReading
{
    public required DateOnly Month { get; init; }
    public required MonthFinality Finality { get; init; }
    public required IReadOnlyList<BenchmarkObservation> Observations { get; init; }
    public required MeasurementQuantity Count { get; init; }
}

/// <summary>
/// One candidate a selection compared, in the position the selection gave it: its route tier, its
/// CONFIGURED rating — a rating, never presented as an observed quality — and its observed quality and
/// cost in their cases with the observation count behind them.
/// </summary>
public sealed record SelectionCandidate
{
    public required RouteId Route { get; init; }
    public required RouteTier Tier { get; init; }
    public required QualityRating ConfiguredRating { get; init; }
    public required MeasurementQuantity ObservedQuality { get; init; }
    public required MeasurementQuantity ObservedCost { get; init; }
    public required MeasurementQuantity Observations { get; init; }
}

/// <summary>
/// What one selection rested on: its basis, the task class, every candidate in position order, the
/// observations an evidence ranking rested on and their count, and a statement naming each unmeasured
/// quantity, unit mismatch or absent task class behind a configured selection. No observation minimum
/// is applied; the count is stated.
/// </summary>
public sealed record SelectionRecord
{
    public required SelectionBasis Basis { get; init; }
    public TaskClass? TaskClass { get; init; }
    public required IReadOnlyList<SelectionCandidate> Candidates { get; init; }
    public required IReadOnlyList<BenchmarkObservationId> ObservationsRankedOn { get; init; }
    public required MeasurementQuantity ObservationCount { get; init; }

    /// <summary>Why this basis. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>
/// The cost controller's actions, in ascending severity (decision D-004 of the AI-economics design).
/// The four thresholds map to these in code, and the most severe action demanded by any governing
/// reading is the one applied.
/// </summary>
public enum ControllerAction
{
    /// <summary>Below 50 percent: no action.</summary>
    None = 0,

    /// <summary>At 50 percent: the delivered alert, and nothing else.</summary>
    AlertOnly = 1,

    /// <summary>At 75 percent: a reasoning-tier downgrade, never below the floor, never for a critical request.</summary>
    Downgrade = 2,

    /// <summary>At 90 percent: metered work is deferred and held.</summary>
    Defer = 3,

    /// <summary>At 100 percent, or on a missing amount or unmeasured spend: metered work is refused.</summary>
    Refuse = 4,
}

/// <summary>Which governing reading: the channel's, or the company's.</summary>
public enum GoverningScope
{
    Channel = 1,
    Company = 2,
}

/// <summary>
/// One governing reading of one booking month: the recorded amount it is governed against, the booked
/// spend and the utilisation in their cases, the threshold reached, the action it demands and, where
/// it refuses for want of a figure, its own reason.
/// </summary>
public sealed record GoverningReading
{
    public required GoverningScope Scope { get; init; }

    /// <summary>
    /// The channel or company register identifier; null for the company reading where the channel
    /// register holds no company for the attribution's channel. The company reading covers every
    /// operation booked into the month either way.
    /// </summary>
    public Guid? ScopeId { get; init; }

    public required DateOnly Month { get; init; }
    public required RecordedAmount Amount { get; init; }
    public required MeasurementQuantity BookedSpend { get; init; }
    public required MeasurementQuantity Utilisation { get; init; }
    public BudgetThreshold? Threshold { get; init; }
    public required ControllerAction Action { get; init; }
    public RefusalReason? Reason { get; init; }

    /// <summary>What this reading read and why it demands its action. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>
/// One controller decision: the booking month and the datastore instant it was taken at, every
/// governing reading, the action applied — the most severe any reading demands — and the reason for it,
/// with the company basis stated on every decision.
/// </summary>
public sealed record ControllerDecision
{
    public required DateOnly BookingMonth { get; init; }
    public required DateTimeOffset DecidedAt { get; init; }
    public required IReadOnlyList<GoverningReading> Readings { get; init; }
    public required ControllerAction Action { get; init; }

    /// <summary>The reason of the reading that decided a refusal or a deferral, or null.</summary>
    public RefusalReason? Reason { get; init; }

    /// <summary>The scope of the reading that decided the action, or null where the action is none.</summary>
    public GoverningScope? DecidedBy { get; init; }

    /// <summary>What the company reading is governed against and what that covers. Required.</summary>
    public required string CompanyBasisStatement { get; init; }
}

/// <summary>
/// One part of a month's tier distribution: the operations whose admitting route stated one reasoning
/// tier, or stated none, with their count, unit sums and cost, each in its case.
/// </summary>
public sealed record TierDistributionPart
{
    /// <summary>The stated tier, or null for the operations whose admitting route stated none.</summary>
    public ReasoningTier? Tier { get; init; }

    public required MeasurementQuantity Operations { get; init; }
    public required MeasurementQuantity InputUnits { get; init; }
    public required MeasurementQuantity OutputUnits { get; init; }
    public required MeasurementQuantity CachedUnits { get; init; }
    public required MeasurementQuantity Cost { get; init; }
}

/// <summary>
/// A month's tier distribution, from one close-then-read transaction: per served tier and untiered,
/// and the period total, every observed part summing exactly to the total; with the assumed split
/// labelled as an assumption, the count of operations carrying a served tier, the statement that no
/// level-to-tier relation is recorded so the reading decides no split, and the month's finality.
/// </summary>
public sealed record TierDistributionReading
{
    public required DateOnly Month { get; init; }
    public required MonthFinality Finality { get; init; }
    public required IReadOnlyList<TierDistributionPart> Parts { get; init; }
    public required TierDistributionPart Total { get; init; }
    public required MeasurementQuantity TierEvidence { get; init; }

    /// <summary>The assumed split, labelled an assumption. Required.</summary>
    public required string AssumedSplit { get; init; }

    /// <summary>That no relation of complexity levels to reasoning tiers is recorded. Required.</summary>
    public required string RelationStatement { get; init; }
}

/// <summary>
/// Every served-tier record of one month, each with its caveat, and the month's finality from the same
/// close-then-read transaction that produced the records (the AI-economics change, decision D-007 of its
/// design), so no reachable reading of the served-tier list leaves its finality unstated.
/// </summary>
public sealed record ServedTierReading
{
    public required DateOnly Month { get; init; }
    public required MonthFinality Finality { get; init; }
    public required IReadOnlyList<ServedTierReadModel> Records { get; init; }
}
