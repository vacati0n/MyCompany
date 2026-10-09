using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Accounting;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The channel composers (the multi-channel change, decisions D-003 to D-006 of its design): the ONE
/// composing site at which every per-channel case is decided.
///
/// They sit in the rule-determined assembly with the delivered composers and add no reference, so a
/// reasoning capability is not referenceable from here. Each is a pure function over a boundary
/// summary the datastore computed; none creates a record, none performs money arithmetic, and an
/// absent record resolves to the unmeasured case rather than being filled in.
///
/// The cases, decided once:
/// - a channel the register does not hold and the rows do not name reads unmeasured, naming the register;
/// - a channel identifier the rows name and the register does not hold is its own partition, read as
///   recorded and named as not in the register, so no row is dropped from the company figure;
/// - a month ending at or before the append-only record's first entry reads unmeasured, naming the
///   record's start, because the record was not yet observing;
/// - otherwise a count reads observed, observed zero included, and a cost figure follows the
///   delivered rule: unmeasured where the partition recorded no operation.
/// </summary>
public static class ChannelAnalyticsComposers
{
    /// <summary>What an operation partition establishes and what it does not.</summary>
    public const string OperationPartitionStatement =
        "each figure is the datastore's aggregation of the operation rows of one channel in the month, and the "
        + "company figure is aggregated in the same statement over the same rows, so every additive cost figure of "
        + "the company is the exact sum of its channel figures; the company figure covers the metered operations "
        + "the operation record carries and nothing else, and each cost is an estimate over unit prices no "
        + "first-hand verification has confirmed";

    /// <summary>Why every revenue-derived figure is dark, for every channel and for the company.</summary>
    public const string RevenueStatement =
        "the revenue-derived figures read a register that carries no channel and holds no admitting observation, "
        + "so they are dark for every channel and for the company; no channel is assigned a revenue figure by "
        + "division, estimate or default";

    /// <summary>What a dossier partition establishes and what it does not.</summary>
    public const string DossierPartitionStatement =
        "these are counts of rows the item dossier register holds, reached through each item's channel and "
        + "recorded in the month, as recorded at the read; the register accumulates, so they claim no permanence, "
        + "and none of them is a clip count, a supply figure or a threshold";

    /// <summary>What a throughput partition establishes and what it does not.</summary>
    public const string ThroughputPartitionStatement =
        "each lifecycle entry is attributed to the channel of the work unit its declared subject names, under the "
        + "delivered coverage rule, and an entry naming no recorded unit is counted as unattributed rather than "
        + "dropped; these counts establish no sustainable rate, buffer depth or concurrency figure, and a count "
        + "read from a demonstration store is a demonstration parameter";

    /// <summary>What the company ceiling reading covers and what it does not.</summary>
    public const string CeilingCoverageStatement =
        "the company reading covers the metered operations the operation record carries, across every channel of "
        + "the company, against the approved monthly envelope, and nothing else: the standing charge the envelope "
        + "includes is not carried by the operation record and is read as its own quantity, unmeasured while no "
        + "record of a standing commitment exists, so it is neither folded into the utilisation nor presented as zero. "
        + "This reading is against the whole envelope; the cost controller governs metered admission against a different "
        + "basis, the recorded metered allotment it states as its basis, so a threshold this reading shows unreached may "
        + "already defer or refuse metered work";

    /// <summary>Where a channel's budget amount is recorded (the AI-economics change, decision D-011).</summary>
    public const string BudgetRegister = "the budget register";

    /// <summary>Where the approved envelope is recorded (the AI-economics change, decision D-011).</summary>
    public const string EnvelopeConstant = "the owner-approved monthly envelope, a code constant of the accounting domain";

    /// <summary>What an approval listing is and is not.</summary>
    public const string ApprovalListingStatement =
        "a view over the recorded gate state: the item versions of this channel whose latest recorded transition "
        + "brought them to awaiting owner approval, as recorded at the read; it is not configuration, and nothing "
        + "on it presents, decides, sends back or transitions";

    /// <summary>What an approval workload reading is and what it does not claim.</summary>
    public const string ApprovalWorkloadStatement =
        "the approvals of this channel's items decided in the month, by decision instant, a sent-back verdict "
        + "counted as a change request, each with its three effort components from its own recorded marks, as "
        + "recorded at the read and claiming no permanence; no aggregate of minutes, threshold, target, pass line "
        + "or workload figure is formed";

    // -----------------------------------------------------------------------
    // Finality (decision D-006)
    // -----------------------------------------------------------------------

    /// <summary>
    /// A month is final only when the record horizon is at or after the first instant of the next
    /// month: from then on no operation can be booked into it.
    /// </summary>
    public static MonthFinality Finality(MonthClosure closure)
    {
        ArgumentNullException.ThrowIfNull(closure);

        var next = new DateTimeOffset(closure.Month.AddMonths(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        var final = closure.Horizon >= next;
        var how = closure.ClosedByThisRead
            ? "the horizon this read set"
            : "the horizon as stored, because an audited or metered transaction was in flight at every attempt to close the record";

        var statement = final
            ? $"final: {how}, {closure.Horizon:O}, is at or after {next:O}, so no operation can be booked into "
              + $"{closure.Month:yyyy-MM} any more and this reading cannot change"
            : $"not yet final: {how}, {closure.Horizon:O}, is before {next:O}, the first instant of the next month, "
              + $"so operations may still be booked into {closure.Month:yyyy-MM} and this reading is as recorded at the read";

        return new MonthFinality(closure.Month, final, closure.Horizon, closure.ClosedByThisRead, statement);
    }

    /// <summary>
    /// Whether the append-only record was observing during the month: the month ends after the
    /// record's first entry. A record holding no entry was observing no month.
    /// </summary>
    private static bool Observing(DateOnly month, DateTimeOffset? earliestEntry) =>
        earliestEntry is { } earliest
        && new DateTimeOffset(month.AddMonths(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero) > earliest;

    private static string NotObserving(string label, DateOnly month, DateTimeOffset? earliestEntry) =>
        earliestEntry is { } earliest
            ? $"no observation of {label} exists for {month:yyyy-MM}: the month ends at or before the append-only "
              + $"record's first entry at {earliest:O}, so the record was not yet observing"
            : $"no observation of {label} exists for {month:yyyy-MM}: the append-only record holds no entry, so it "
              + "has not begun observing";

    private static string NotInRegister(ChannelId channel, string lookedFor) =>
        $"{lookedFor} of channel {channel} was looked for, and the channel register holds no such channel and no "
        + "recorded row names it";

    private static string Label(ChannelId? channel) => channel is { } id ? $"channel {id}" : "the company";

    // -----------------------------------------------------------------------
    // The operation record (decision D-003)
    // -----------------------------------------------------------------------

    public static OperationPartitionReading Operations(OperationPartitionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var month = summary.Closure.Month;
        var observing = Observing(month, summary.Closure.EarliestEntry);

        var channels = summary.Channels
            .Select(row => OperationPartition(
                row.Channel,
                row.InRegister ? ChannelPartitionStanding.InRegister : ChannelPartitionStanding.NotInRegister,
                row.Totals,
                month,
                observing,
                summary.Closure.EarliestEntry,
                MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.SourceCannotStateOne,
                    $"the approved envelope of {ApprovedEnvelope.MonthlyTotal} is a company ceiling across every channel; "
                    + $"channel {row.Channel} holds no envelope of its own, so no variance is read for it")))
            .ToArray();

        var company = OperationPartition(
            null,
            ChannelPartitionStanding.InRegister,
            summary.Company,
            month,
            observing,
            summary.Closure.EarliestEntry,
            AnalyticsComposers.FromOperations(
                summary.Company.Operations,
                summary.Company.UnstatedCosts,
                summary.CompanyVarianceAgainstEnvelope,
                $"no operation is recorded for the company in period {month:yyyy-MM}",
                $"the company in period {month:yyyy-MM}"));

        return new OperationPartitionReading
        {
            Month = month,
            Finality = Finality(summary.Closure),
            Channels = channels,
            Company = company,
            Statement = OperationPartitionStatement,
            RevenueStatement = RevenueStatement,
        };
    }

    /// <summary>
    /// One channel's partition of an operation reading. A channel the reading holds no partition for,
    /// because the register does not hold it and no row names it, reads unmeasured naming the register.
    /// </summary>
    public static ChannelOperationReading ForChannel(OperationPartitionReading reading, ChannelId channel)
    {
        ArgumentNullException.ThrowIfNull(reading);

        var held = reading.Channels.FirstOrDefault(c => c.Channel is { } id && id.Equals(channel));
        if (held is not null)
        {
            return held;
        }

        MeasurementQuantity Absent(string lookedFor) =>
            MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, NotInRegister(channel, lookedFor));

        return new ChannelOperationReading
        {
            Channel = channel,
            Standing = ChannelPartitionStanding.NotInRegister,
            Cost = Absent($"the cost in period {reading.Month:yyyy-MM}"),
            Operations = Absent($"the operation count in period {reading.Month:yyyy-MM}"),
            DeterministicSetCost = Absent($"the deterministic-set cost in period {reading.Month:yyyy-MM}"),
            VarianceAgainstEnvelope = Absent("the variance against the envelope"),
            CostByCapability = new Dictionary<CapabilityClass, MeasurementQuantity>(),
            ServedTierRecords = Absent($"the served-tier records in period {reading.Month:yyyy-MM}"),
            TierRatio = Absent($"the tier ratio in period {reading.Month:yyyy-MM}"),
            ContainsEstimates = false,
        };
    }

    private static ChannelOperationReading OperationPartition(
        ChannelId? channel,
        ChannelPartitionStanding standing,
        OperationTotals totals,
        DateOnly month,
        bool observing,
        DateTimeOffset? earliestEntry,
        MeasurementQuantity variance)
    {
        var label = Label(channel);

        if (totals.Operations == 0 && !observing)
        {
            var detail = NotObserving(label, month, earliestEntry);
            var none = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, detail);

            return new ChannelOperationReading
            {
                Channel = channel,
                Standing = standing,
                Cost = none,
                Operations = none,
                DeterministicSetCost = none,
                VarianceAgainstEnvelope = channel is null ? none : variance,
                CostByCapability = new Dictionary<CapabilityClass, MeasurementQuantity>(),
                ServedTierRecords = none,
                TierRatio = none,
                ContainsEstimates = false,
            };
        }

        var absent = $"no operation is recorded for {label} in period {month:yyyy-MM}";
        var scope = $"{label} in period {month:yyyy-MM}";

        // Every cost figure honours whether each cost is stated (correction cycle): a partition holding an
        // unstated cost reads that cost unmeasured, as the cost controller reads the same month's spend.
        return new ChannelOperationReading
        {
            Channel = channel,
            Standing = standing,
            Cost = AnalyticsComposers.FromOperations(totals.Operations, totals.UnstatedCosts, totals.Cost, absent, scope),
            Operations = MeasurementQuantity.Count(totals.Operations, "operations"),
            DeterministicSetCost = AnalyticsComposers.FromOperations(
                totals.Operations, totals.UnstatedDeterministicCosts, totals.DeterministicSetCost, absent, $"the deterministic set of {scope}"),
            VarianceAgainstEnvelope = variance,
            CostByCapability = totals.CostByCapability.ToDictionary(
                pair => pair.Key,
                pair => totals.UnstatedByCapability.TryGetValue(pair.Key, out var unstated) && unstated > 0
                    ? MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.SourceCannotStateOne,
                        $"{unstated} operations of capability class {pair.Key} of {scope} carry a cost that is not stated, so its cost cannot be stated")
                    : MeasurementQuantity.Observed(pair.Value.Amount, pair.Value.Currency)),
            ServedTierRecords = MeasurementQuantity.Count(totals.ServedTierRecords, "served-tier records"),
            TierRatio = AnalyticsComposers.TierRatioFromCounts(
                month, totals.CarryingBothTiers, totals.AgreeingTiers, $"the {label} partition of period"),
            ContainsEstimates = totals.ContainsEstimates,
        };
    }

    // -----------------------------------------------------------------------
    // The item dossier register (decision D-003)
    // -----------------------------------------------------------------------

    public static DossierPartitionReading Dossiers(DossierPartitionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var observing = Observing(summary.Month, summary.EarliestEntry);

        ChannelDossierReading Partition(ChannelId? channel, ChannelPartitionStanding standing, DossierTotals totals)
        {
            var empty = totals is { DossiersOpened: 0, StageOutcomes: 0, SupplyAuditEntries: 0, DeterminationResolutions: 0 };

            MeasurementQuantity Rows(long rows, string unit) =>
                empty && !observing
                    ? MeasurementQuantity.NotMeasured(
                        UnmeasuredReason.NoObservationExists, NotObserving($"{unit} of {Label(channel)}", summary.Month, summary.EarliestEntry))
                    : MeasurementQuantity.Count(rows, unit);

            return new ChannelDossierReading
            {
                Channel = channel,
                Standing = standing,
                DossiersOpened = Rows(totals.DossiersOpened, "dossiers opened"),
                StageOutcomes = Rows(totals.StageOutcomes, "stage outcomes"),
                SupplyAuditEntries = Rows(totals.SupplyAuditEntries, "supply audit entries"),
                DeterminationResolutions = Rows(totals.DeterminationResolutions, "determination resolutions"),
            };
        }

        return new DossierPartitionReading
        {
            Month = summary.Month,
            Channels = summary.Channels
                .Select(row => Partition(
                    row.Channel,
                    row.InRegister ? ChannelPartitionStanding.InRegister : ChannelPartitionStanding.NotInRegister,
                    row.Totals))
                .ToArray(),
            Company = Partition(null, ChannelPartitionStanding.InRegister, summary.Company),
            Statement = DossierPartitionStatement,
        };
    }

    // -----------------------------------------------------------------------
    // Throughput (decision D-003, over the delivered coverage rule)
    // -----------------------------------------------------------------------

    public static ThroughputPartitionReading Throughput(ThroughputPartitionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var channels = summary.Channels
            .Select(row => new ChannelThroughputReading
            {
                Channel = row.Channel,
                Standing = row.InRegister ? ChannelPartitionStanding.InRegister : ChannelPartitionStanding.NotInRegister,
                Throughput = AnalyticsComposers.Throughput(row.Summary),
            })
            .ToList();

        if (summary.Unattributed is { } unattributed)
        {
            channels.Add(new ChannelThroughputReading
            {
                Channel = null,
                Standing = ChannelPartitionStanding.Unattributed,
                Throughput = AnalyticsComposers.Throughput(unattributed.Summary),
            });
        }

        return new ThroughputPartitionReading
        {
            PeriodStart = summary.Company.PeriodStart,
            PeriodEnd = summary.Company.PeriodEnd,
            Channels = channels,
            Company = new ChannelThroughputReading
            {
                Channel = null,
                Standing = ChannelPartitionStanding.InRegister,
                Throughput = AnalyticsComposers.Throughput(summary.Company),
            },
            Statement = ThroughputPartitionStatement,
        };
    }

    // -----------------------------------------------------------------------
    // Budget utilisation (decision D-004)
    // -----------------------------------------------------------------------

    public static BudgetPartitionReading Budgets(BudgetPartitionSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var month = summary.Closure.Month;

        return new BudgetPartitionReading
        {
            Month = month,
            Finality = Finality(summary.Closure),
            Channels = summary.Channels.Select(row => ChannelBudget(row, month)).ToArray(),
            Company = CompanyCeiling(summary, month),
        };
    }

    private static ChannelBudgetReading ChannelBudget(ChannelBudgetRow row, DateOnly month)
    {
        var label = Label(row.Channel);

        if (row.BudgetAmount is not { } amount)
        {
            var missing = $"no budget is recorded for {label} in period {month:yyyy-MM} in the budget register, so no "
                + "utilisation is computed against an amount nobody recorded";
            var none = MeasurementQuantity.NotMeasured(UnmeasuredReason.NoObservationExists, missing);

            return new ChannelBudgetReading
            {
                Channel = row.Channel,
                Standing = ChannelPartitionStanding.InRegister,

                // A RECORDED AMOUNT that was not recorded, never an observation and never zero.
                BudgetAmount = RecordedAmount.Missing(missing),
                Utilised = AnalyticsComposers.FromOperations(
                    row.Operations, row.UnstatedOperations, row.Utilised,
                    $"no operation is recorded for {label} in period {month:yyyy-MM}", $"{label} in period {month:yyyy-MM}"),
                Utilisation = none,
                Thresholds = Thresholds(null, none, _ => none),
            };
        }

        // A recorded amount, labelled as recorded in the budget register: nobody observed a budget.
        var budgetAmount = RecordedAmount.Of(amount, BudgetRegister);

        if (row.UnstatedOperations > 0)
        {
            // A partial sum is never a utilisation (correction cycle): unmeasured, naming the operations,
            // exactly as the cost controller reads the same channel's booked spend.
            var unstated = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"{row.UnstatedOperations} of the {row.Operations} operations of {label} in period {month:yyyy-MM} carry a cost "
                + "that is not stated, so its utilisation cannot be stated");

            return new ChannelBudgetReading
            {
                Channel = row.Channel,
                Standing = ChannelPartitionStanding.InRegister,
                BudgetAmount = budgetAmount,
                Utilised = unstated,
                Utilisation = unstated,
                Thresholds = Thresholds(null, unstated, threshold => Alerts(row, threshold)),
            };
        }

        if (row.Operations == 0 || row.UtilisationPercent is not { } percent)
        {
            var noOperations = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no operation is recorded for {label} in period {month:yyyy-MM}, so its utilisation was not observed");

            return new ChannelBudgetReading
            {
                Channel = row.Channel,
                Standing = ChannelPartitionStanding.InRegister,
                BudgetAmount = budgetAmount,
                Utilised = noOperations,
                Utilisation = noOperations,
                Thresholds = Thresholds(null, noOperations, threshold => Alerts(row, threshold)),
            };
        }

        return new ChannelBudgetReading
        {
            Channel = row.Channel,
            Standing = ChannelPartitionStanding.InRegister,
            BudgetAmount = budgetAmount,
            Utilised = MeasurementQuantity.Observed(row.Utilised.Amount, row.Utilised.Currency),
            Utilisation = MeasurementQuantity.Observed(percent, "percent"),
            Thresholds = Thresholds(percent, null, threshold => Alerts(row, threshold)),
        };
    }

    private static MeasurementQuantity Alerts(ChannelBudgetRow row, BudgetThreshold threshold) =>
        MeasurementQuantity.Count(
            row.AlertsByThreshold.TryGetValue((int)threshold, out var count) ? count : 0,
            "alerts recorded");

    private static CompanyCeilingReading CompanyCeiling(BudgetPartitionSummary summary, DateOnly month)
    {
        var noAlert = MeasurementQuantity.NotMeasured(
            UnmeasuredReason.SourceCannotStateOne,
            "no company alert is persisted: the delivered alert is kept per budget, and the company ceiling is not a "
            + "budget the budget register holds");

        var utilisation = summary.CompanyUnstatedOperations > 0
            ? MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"{summary.CompanyUnstatedOperations} of the {summary.CompanyOperations} operations of the company in period "
                + $"{month:yyyy-MM} carry a cost that is not stated, so its utilisation of the ceiling cannot be stated")
            : summary.CompanyOperations == 0 || summary.CompanyUtilisationPercent is not { } percent
                ? MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists,
                    $"no operation is recorded for the company in period {month:yyyy-MM}, so its utilisation of the ceiling was not observed")
                : MeasurementQuantity.Observed(percent, "percent");

        return new CompanyCeilingReading
        {
            // The envelope is a recorded amount, a code constant, never an observation.
            Ceiling = RecordedAmount.Of(summary.Ceiling, EnvelopeConstant),
            CompanyCost = AnalyticsComposers.FromOperations(
                summary.CompanyOperations, summary.CompanyUnstatedOperations, summary.CompanyCost,
                $"no operation is recorded for the company in period {month:yyyy-MM}", $"the company in period {month:yyyy-MM}"),

            // The basis the cost controller governs metered admission on (correction cycle): the recorded
            // metered allotment, stated beside this reading's own envelope so the two cannot be confused.
            ControllerBasis = MediaCompany.Deterministic.Accounting.CostController.CompanyBasis,
            Utilisation = utilisation,
            Thresholds = Thresholds(
                utilisation is MeasurementQuantity.Unmeasured ? null : summary.CompanyUtilisationPercent,
                utilisation as MeasurementQuantity.Unmeasured,
                _ => noAlert),
            StandingCommitment = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no record of a standing commitment exists, so the standing charge of {ApprovedEnvelope.Standing} the "
                + "approved envelope includes is neither read as incurred nor read as zero"),
            CoverageStatement = CeilingCoverageStatement,
        };
    }

    /// <summary>
    /// The four thresholds of one utilisation, each reached or not by the delivered threshold
    /// detection, and unmeasured wherever the utilisation is.
    /// </summary>
    private static IReadOnlyList<ThresholdReading> Thresholds(
        decimal? percent,
        MeasurementQuantity? unmeasured,
        Func<BudgetThreshold, MeasurementQuantity> alerts)
    {
        var highest = percent is { } observed ? BudgetThresholds.HighestReached(observed) : null;

        return Enum.GetValues<BudgetThreshold>()
            .Select(threshold => new ThresholdReading
            {
                Threshold = threshold,
                Reached = percent is null
                    ? unmeasured!
                    : MeasurementQuantity.Count(highest is { } h && h >= (int)threshold ? 1 : 0, "thresholds reached"),
                AlertsRecorded = alerts(threshold),
            })
            .ToArray();
    }

    // -----------------------------------------------------------------------
    // The approval queue and workload (decision D-005)
    // -----------------------------------------------------------------------

    /// <summary>One channel's approval listing, a view over the recorded gate state.</summary>
    public static ChannelApprovalListing Listing(ChannelId channel, bool inRegister, IReadOnlyList<AwaitingApproval> awaiting)
    {
        ArgumentNullException.ThrowIfNull(awaiting);

        var entries = awaiting
            .Where(a => a.Channel.Equals(channel))
            .Select(a => new ApprovalListingEntry { Item = a.Item, Version = a.Version, AwaitingSince = a.AwaitingSince })
            .ToArray();

        return new ChannelApprovalListing
        {
            Channel = channel,
            Entries = entries,
            Awaiting = inRegister || entries.Length > 0
                ? MeasurementQuantity.Count(entries.Length, "item versions awaiting owner approval")
                : MeasurementQuantity.NotMeasured(
                    UnmeasuredReason.NoObservationExists, NotInRegister(channel, "the approval listing")),
            Statement = ApprovalListingStatement,
        };
    }

    /// <summary>
    /// One channel's approval workload for one month. A channel with no recorded approval reads one
    /// entry whose three components are unmeasured, never zero minutes.
    /// </summary>
    public static ChannelApprovalWorkload Workload(
        ChannelId channel,
        bool inRegister,
        DateOnly month,
        IReadOnlyList<DecidedApproval> decided)
    {
        ArgumentNullException.ThrowIfNull(decided);

        if (!inRegister && decided.Count == 0)
        {
            var absent = MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists, NotInRegister(channel, $"the approvals decided in {month:yyyy-MM}"));

            return new ChannelApprovalWorkload
            {
                Channel = channel,
                Standing = ChannelPartitionStanding.NotInRegister,
                Month = month,
                Approvals = absent,
                ChangeRequests = absent,
                Efforts = [new ChannelApprovalEffort { Review = absent, Queue = absent, Rework = absent }],
                Statement = ApprovalWorkloadStatement,
            };
        }

        IReadOnlyList<ChannelApprovalEffort> efforts;
        if (decided.Count == 0)
        {
            MeasurementQuantity None(string component) => MeasurementQuantity.NotMeasured(
                UnmeasuredReason.NoObservationExists,
                $"no approval of channel {channel} was decided in {month:yyyy-MM}, so no {component} time is recorded");

            efforts = [new ChannelApprovalEffort { Review = None("review"), Queue = None("queue"), Rework = None("rework") }];
        }
        else
        {
            var label = new ApprovalExerciseLabel(
                $"recorded approvals of channel {channel} decided in {month:yyyy-MM}", decided.Count, ApprovalWorkloadStatement);

            efforts = decided
                .Select(d => AnalyticsComposers.ApprovalEffort(ApprovalMeasurement.Derive(d.Approval, d.ReworkPredecessor, label)))
                .Select(effort => new ChannelApprovalEffort
                {
                    Item = effort.Item,
                    Version = effort.Version,
                    Review = effort.Review,
                    Queue = effort.Queue,
                    Rework = effort.Rework,
                })
                .ToArray();
        }

        return new ChannelApprovalWorkload
        {
            Channel = channel,
            Standing = inRegister ? ChannelPartitionStanding.InRegister : ChannelPartitionStanding.NotInRegister,
            Month = month,
            Approvals = MeasurementQuantity.Count(decided.Count, "approvals decided"),
            ChangeRequests = MeasurementQuantity.Count(
                decided.Count(d => d.Approval.Verdict == ApprovalVerdict.SentBack), "change requests"),
            Efforts = efforts,
            Statement = ApprovalWorkloadStatement,
        };
    }
}
