using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Domain.Analytics;

/// <summary>
/// Where one partition of a channel-partitioned reading comes from (decision D-003 of the
/// multi-channel design). No row a reading aggregates is dropped from the company figure: a channel
/// identifier the rows carry and the register does not is its own partition, and an entry that names
/// no work unit is reported as unattributed.
/// </summary>
public enum ChannelPartitionStanding
{
    /// <summary>The channel is held in the channel register.</summary>
    InRegister = 1,

    /// <summary>The rows name a channel identifier the channel register does not hold.</summary>
    NotInRegister = 2,

    /// <summary>The rows name no channel the record can relate them to.</summary>
    Unattributed = 3,
}

/// <summary>
/// Whether a month reading over the operation record is final (decision D-006 of the multi-channel
/// design).
///
/// A month is final once the record horizon is at or after the first instant of the next month,
/// because from then on no operation can be booked into it. A reading that is not final says so,
/// with the horizon it was decided against, and says that operations may still be booked into it.
/// The statement is a construction invariant: a month reading that does not say whether it can
/// still change is read as if it could not.
/// </summary>
public sealed record MonthFinality
{
    public MonthFinality(DateOnly month, bool isFinal, DateTimeOffset horizon, bool closedByThisRead, string statement)
    {
        if (month.Day != 1)
        {
            throw new ArgumentException("A month is named by its first day.", nameof(month));
        }

        if (string.IsNullOrWhiteSpace(statement))
        {
            throw new ArgumentException(
                "A month reading states whether it is final and against which horizon; without it a reading is read as final.",
                nameof(statement));
        }

        Month = month;
        IsFinal = isFinal;
        Horizon = horizon;
        ClosedByThisRead = closedByThisRead;
        Statement = statement;
    }

    public DateOnly Month { get; }

    /// <summary>True only where no operation can be booked into the month any more.</summary>
    public bool IsFinal { get; }

    /// <summary>The record horizon the finality was decided against.</summary>
    public DateTimeOffset Horizon { get; }

    /// <summary>Whether this read obtained the horizon and raised it, or read it as stored.</summary>
    public bool ClosedByThisRead { get; }

    /// <summary>Final, or not yet final and why. Required.</summary>
    public string Statement { get; }
}

/// <summary>
/// One partition of the operation-record reading for one month: a channel, or the company.
///
/// Every figure is one case of the closed three-case union, decided at one composing site. The cost
/// figures follow the delivered rule — unmeasured where the partition recorded no operation, an
/// observed zero where operations were recorded and cost nothing — and the operation count is a
/// count, observed zero included, for a channel the register holds. A channel the register does
/// not hold reads its counts as recorded but is named as not in the register.
/// </summary>
public sealed record ChannelOperationReading
{
    /// <summary>The channel, or null for the company partition.</summary>
    public ChannelId? Channel { get; init; }

    public required ChannelPartitionStanding Standing { get; init; }
    public required MeasurementQuantity Cost { get; init; }
    public required MeasurementQuantity Operations { get; init; }
    public required MeasurementQuantity DeterministicSetCost { get; init; }

    /// <summary>The company's variance against the approved envelope; unmeasured for a channel, which has no envelope of its own.</summary>
    public required MeasurementQuantity VarianceAgainstEnvelope { get; init; }

    public required IReadOnlyDictionary<CapabilityClass, MeasurementQuantity> CostByCapability { get; init; }
    public required MeasurementQuantity ServedTierRecords { get; init; }
    public required MeasurementQuantity TierRatio { get; init; }
    public required bool ContainsEstimates { get; init; }
}

/// <summary>
/// The operation record for one month, partitioned by the operation row's channel, with the company
/// figure computed by the datastore in the same statement over the same rows, so every additive
/// cost figure of the company is the exact sum of its channel figures.
/// </summary>
public sealed record OperationPartitionReading
{
    public required DateOnly Month { get; init; }
    public required MonthFinality Finality { get; init; }
    public required IReadOnlyList<ChannelOperationReading> Channels { get; init; }
    public required ChannelOperationReading Company { get; init; }

    /// <summary>What the partition establishes and what it does not. Required.</summary>
    public required string Statement { get; init; }

    /// <summary>Why the revenue-derived figures are dark for every channel and for the company. Required.</summary>
    public required string RevenueStatement { get; init; }
}

/// <summary>
/// One partition of the item dossier register for one month, reached through each item's channel:
/// how many dossiers were opened, and how many stage outcomes, supply-audit entries and determination
/// resolutions were recorded, in the month. Counts of RECORDED ROWS; none is a clip count, a supply
/// figure or a threshold.
/// </summary>
public sealed record ChannelDossierReading
{
    public ChannelId? Channel { get; init; }
    public required ChannelPartitionStanding Standing { get; init; }
    public required MeasurementQuantity DossiersOpened { get; init; }
    public required MeasurementQuantity StageOutcomes { get; init; }
    public required MeasurementQuantity SupplyAuditEntries { get; init; }
    public required MeasurementQuantity DeterminationResolutions { get; init; }
}

/// <summary>The item dossier register for one month, partitioned by the item's channel.</summary>
public sealed record DossierPartitionReading
{
    public required DateOnly Month { get; init; }
    public required IReadOnlyList<ChannelDossierReading> Channels { get; init; }
    public required ChannelDossierReading Company { get; init; }

    /// <summary>What the partition establishes and what it does not. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>One partition of a throughput reading: a channel, the unattributed entries, or the company.</summary>
public sealed record ChannelThroughputReading
{
    public ChannelId? Channel { get; init; }
    public required ChannelPartitionStanding Standing { get; init; }
    public required ThroughputReadModel Throughput { get; init; }
}

/// <summary>
/// Throughput and queue quantities for one half-open period, partitioned by the channel of the work
/// unit each lifecycle entry names, under the delivered coverage rule.
/// </summary>
public sealed record ThroughputPartitionReading
{
    public required DateTimeOffset PeriodStart { get; init; }
    public required DateTimeOffset PeriodEnd { get; init; }
    public required IReadOnlyList<ChannelThroughputReading> Channels { get; init; }
    public required ChannelThroughputReading Company { get; init; }

    /// <summary>What the partition establishes and what it does not. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>The four utilisation thresholds the delivered tracking fixes, as a closed set.</summary>
public enum BudgetThreshold
{
    Fifty = 50,
    SeventyFive = 75,
    Ninety = 90,
    Hundred = 100,
}

/// <summary>
/// One threshold of one utilisation: whether it is reached, as a count of one or an observed zero,
/// unmeasured where the utilisation is; and how many alerts the delivered tracking recorded for it.
/// </summary>
public sealed record ThresholdReading
{
    public required BudgetThreshold Threshold { get; init; }
    public required MeasurementQuantity Reached { get; init; }
    public required MeasurementQuantity AlertsRecorded { get; init; }
}

/// <summary>
/// One channel's budget utilisation for one month. Unmeasured, naming the missing budget, where the
/// budget register holds no budget for the channel and month — no percentage is computed against an
/// amount nobody recorded — and unmeasured, naming the absent operations, where the channel recorded
/// none.
/// </summary>
public sealed record ChannelBudgetReading
{
    public required ChannelId Channel { get; init; }
    public required ChannelPartitionStanding Standing { get; init; }
    public required MeasurementQuantity BudgetAmount { get; init; }
    public required MeasurementQuantity Utilised { get; init; }
    public required MeasurementQuantity Utilisation { get; init; }
    public required IReadOnlyList<ThresholdReading> Thresholds { get; init; }
}

/// <summary>
/// The company ceiling for one month: the company's recorded operation cost across every channel
/// against the approved monthly envelope, at the four thresholds.
///
/// It carries a REQUIRED coverage statement — it covers the metered operations the operation record
/// carries and nothing else — and the standing commitment as its own quantity, unmeasured because no
/// record of a standing commitment exists, so the standing charge is neither folded in nor presented
/// as zero. No company alert is persisted.
/// </summary>
public sealed record CompanyCeilingReading
{
    public required MeasurementQuantity Ceiling { get; init; }
    public required MeasurementQuantity CompanyCost { get; init; }
    public required MeasurementQuantity Utilisation { get; init; }
    public required IReadOnlyList<ThresholdReading> Thresholds { get; init; }
    public required MeasurementQuantity StandingCommitment { get; init; }

    /// <summary>What the company reading covers and what it does not. Required.</summary>
    public required string CoverageStatement { get; init; }
}

/// <summary>Every registered channel's budget utilisation for one month, and the company ceiling's.</summary>
public sealed record BudgetPartitionReading
{
    public required DateOnly Month { get; init; }
    public required MonthFinality Finality { get; init; }
    public required IReadOnlyList<ChannelBudgetReading> Channels { get; init; }
    public required CompanyCeilingReading Company { get; init; }
}

/// <summary>One item version awaiting owner approval, as the gate-transition record holds it.</summary>
public sealed record ApprovalListingEntry
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }

    /// <summary>When its latest recorded transition brought it to awaiting owner approval.</summary>
    public required DateTimeOffset AwaitingSince { get; init; }
}

/// <summary>
/// One channel's approval queue: a VIEW over recorded gate state, never configuration. It holds
/// exactly the item versions of the channel whose latest recorded gate state is awaiting owner
/// approval, and nothing on it writes, presents, decides or transitions.
/// </summary>
public sealed record ChannelApprovalListing
{
    public required ChannelId Channel { get; init; }
    public required IReadOnlyList<ApprovalListingEntry> Entries { get; init; }
    public required MeasurementQuantity Awaiting { get; init; }

    /// <summary>What the listing is and is not. Required.</summary>
    public required string Statement { get; init; }
}

/// <summary>
/// One approval's three effort components, or — where the channel recorded no approval in the
/// month — one entry naming no item whose three components are unmeasured, never zero minutes.
/// </summary>
public sealed record ChannelApprovalEffort
{
    public ItemId? Item { get; init; }
    public ItemVersion? Version { get; init; }
    public required MeasurementQuantity Review { get; init; }
    public required MeasurementQuantity Queue { get; init; }
    public required MeasurementQuantity Rework { get; init; }
}

/// <summary>
/// One channel's approval workload for one month, by decision instant: how many approvals were
/// decided, how many of them were change requests, and each approval's three effort components from
/// its own recorded marks. No aggregate of minutes, threshold, target, pass line or workload figure
/// is formed, here or anywhere the reading travels.
/// </summary>
public sealed record ChannelApprovalWorkload
{
    public required ChannelId Channel { get; init; }
    public required ChannelPartitionStanding Standing { get; init; }
    public required DateOnly Month { get; init; }
    public required MeasurementQuantity Approvals { get; init; }
    public required MeasurementQuantity ChangeRequests { get; init; }
    public required IReadOnlyList<ChannelApprovalEffort> Efforts { get; init; }

    /// <summary>What the reading is and what it does not claim. Required.</summary>
    public required string Statement { get; init; }
}
