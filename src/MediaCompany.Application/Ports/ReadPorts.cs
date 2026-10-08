using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Rights;

namespace MediaCompany.Application.Ports;

/// <summary>The clock, so that elapsed minutes and validity intervals are testable.</summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

/// <summary>
/// The route policy register (module M-002). Admission is the first of the two independent
/// refusals constraint C-001 requires: a route matching a forbidden-source entry cannot be
/// admitted to the table at all, so an admission-time check and a resolution-time check would
/// both have to fail before a forbidden path existed.
/// </summary>
public interface IRouteRegistry
{
    Task<IReadOnlyList<Route>> AdmittedRoutesAsync(CapabilityClass capability, CancellationToken cancellationToken);

    Task<IReadOnlyList<ForbiddenSource>> ForbiddenSourcesAsync(CancellationToken cancellationToken);
}

/// <summary>Route availability state (module M-004), an explicit recorded state per decision D-008.</summary>
public interface IRouteAvailabilityLedger
{
    Task<IReadOnlyDictionary<RouteId, RouteAvailability>> CurrentAsync(
        IReadOnlyCollection<RouteId> routes,
        CancellationToken cancellationToken);
}

/// <summary>The operating registers (module M-011).</summary>
public interface IOperatingRegisters
{
    Task<IReadOnlyList<ProviderAccount>> ProviderAccountsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Model>> ModelsAsync(CancellationToken cancellationToken);

    /// <summary>The price rows in force at <paramref name="asOf"/>, one per model and unit kind.</summary>
    Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(DateTimeOffset asOf, CancellationToken cancellationToken);

    Task<IReadOnlyList<Channel>> ChannelsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<Department>> DepartmentsAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<WorkforceAgent>> AgentsAsync(CancellationToken cancellationToken);
}

/// <summary>The asset and rights ledger (module M-015).</summary>
public interface IAssetLedger
{
    Task<IReadOnlyList<Asset>> ForItemAsync(ItemId item, CancellationToken cancellationToken);

    Task<IReadOnlyList<LibraryRegistration>> RegistrationsAsync(ChannelId channel, CancellationToken cancellationToken);

    Task RecordAsync(Asset asset, CancellationToken cancellationToken);
}

/// <summary>Blocks and approvals over the publication gate (module M-009).</summary>
public interface IGateLedger
{
    Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken cancellationToken);

    Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// The three conditions of first publication, as recorded observations for one channel.
    ///
    /// Returns a register that is TOTAL over the closed three-member set whatever the store holds,
    /// so a channel with no observations yields a register in which all three resolve absent and
    /// therefore refuse. There is no return value meaning "nothing to check".
    /// </summary>
    Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(
        ChannelId channel,
        CancellationToken cancellationToken);
}

/// <summary>Temporal configuration (module M-012), read without redeployment (constraint C-011).</summary>
public interface IConfigurationStore
{
    Task<ConfigurationVersion?> InForceAsync(string key, string scope, DateTimeOffset asOf, CancellationToken cancellationToken);

    Task<IReadOnlyList<ConfigurationVersion>> HistoryAsync(string key, string scope, CancellationToken cancellationToken);

    /// <summary>
    /// Supersedes the value in force with a new row rather than updating in place, so the prior
    /// value stays retrievable with its validity dates (acceptance criterion AC-021).
    /// </summary>
    Task SupersedeAsync(ConfigurationVersion next, CancellationToken cancellationToken);
}

/// <summary>Cost rollups (module M-006), aggregated by the datastore over the recorded operations.</summary>
public interface ICostRollupReader
{
    Task<Money> CostForItemAsync(ItemId item, CancellationToken cancellationToken);

    Task<Money> CostForPeriodAsync(DateOnly period, CancellationToken cancellationToken);

    /// <summary>
    /// The monthly total against the approved envelope, with the variance. Every figure is
    /// aggregated and subtracted by the datastore in its exact decimal type, so the reported
    /// variance and the recorded one are one number rather than two (constraint C-005).
    /// </summary>
    Task<PeriodSummary> PeriodSummaryAsync(DateOnly period, CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<CapabilityClass, Money>> CostByCapabilityAsync(DateOnly period, CancellationToken cancellationToken);

    /// <summary>
    /// The total cost attributed to the named deterministic task set over a period. Acceptance
    /// criterion AC-017 measures this to zero.
    /// </summary>
    Task<Money> CostForDeterministicSetAsync(DateOnly period, CancellationToken cancellationToken);

    // -----------------------------------------------------------------------
    // The parallel three-state members (decision D-005).
    //
    // The delivered currency-returning members above keep their signatures and their behaviour, so
    // every existing caller compiles and behaves unchanged through the coexistence period. They are
    // removed only once the build shows no caller, which is a condition the build decides rather
    // than a judgement somebody makes.
    //
    // The defect the parallel members exist to correct is concrete: a currency value cannot express
    // the absence of an observation, so a period holding no recorded operation is returned today as
    // a zero amount and is indistinguishable from a period that was measured and cost nothing.
    // -----------------------------------------------------------------------

    /// <summary>One item's cost in three states. Unmeasured where no operation is recorded for it.</summary>
    Task<MeasurementQuantity> ItemCostQuantityAsync(ItemId item, CancellationToken cancellationToken);

    /// <summary>One period's cost in three states. Unmeasured where the period holds no recorded operation.</summary>
    Task<MeasurementQuantity> PeriodCostQuantityAsync(DateOnly period, CancellationToken cancellationToken);

    /// <summary>
    /// One item's cost position as the datastore computes it, mirroring the delivered period
    /// summary. <see cref="ItemSummary.Operations"/> is what decides the measurement state, exactly
    /// as the period summary's count already does.
    /// </summary>
    Task<ItemSummary> ItemSummaryAsync(ItemId item, CancellationToken cancellationToken);
}

/// <summary>
/// What remains of a governing budget, read before a route is admitted at resolution step 4. The
/// remaining amount is computed by the datastore from the recorded operations and the configured
/// budget, not here (constraint C-005).
/// </summary>
public interface IBudgetReader
{
    Task<Money> RemainingAsync(Attribution attribution, DateOnly period, CancellationToken cancellationToken);
}

/// <summary>
/// One period's cost position as the datastore computes it. <see cref="ContainsEstimates"/> is
/// true whenever any operation in the period was accounted from estimated rather than measured
/// unit counts, which every presentation of these figures must state.
/// </summary>
public sealed record PeriodSummary(
    DateOnly Period,
    Money Total,
    Money EnvelopeTotal,
    Money EnvelopeMetered,
    Money EnvelopeStanding,
    Money VarianceAgainstEnvelope,
    bool ContainsEstimates,
    long Operations);

/// <summary>
/// One item's cost position as the datastore computes it, on the same shape as
/// <see cref="PeriodSummary"/>. <see cref="Operations"/> is the count of recorded operations the
/// total aggregates, and a count of zero is what makes the figure unmeasured rather than zero.
/// </summary>
public sealed record ItemSummary(
    ItemId Item,
    Money Total,
    bool ContainsEstimates,
    long Operations);

/// <summary>
/// The served reasoning tier as recorded, one entry per accounted operation in a period.
///
/// The served value on each record was written at the resolution boundary from the ADMITTED
/// ROUTE's stated tier and never from the request, and a null served value is the explicit absence
/// marker for a route that stated none. This port reads those records; it derives nothing.
/// </summary>
public interface IServedTierReader
{
    Task<IReadOnlyList<ServedTierRecord>> RecordsForPeriodAsync(DateOnly period, CancellationToken cancellationToken);
}

/// <summary>
/// The register in which an observed revenue parameter is recorded with the source observation it
/// came from and that observation's date (decision D-003).
///
/// The register is created empty and this change records no row in it. A row recorded without a
/// source observation and its date is an assumed parameter: it is readable here and it admits
/// nothing, which is what keeps the six revenue-derived figures unlit.
/// </summary>
public interface IRevenueParameterRegister
{
    Task<IReadOnlyList<RevenueParameterRecord>> RecordedAsync(CancellationToken cancellationToken);

    Task RecordAsync(RevenueParameterRecord parameter, CancellationToken cancellationToken);
}

/// <summary>
/// The throughput reader (decision D-003 of the accepted design).
///
/// It reads the append-only record by action and instant and by nothing else: no reason, subject,
/// reference or other free-form column of an entry is read. The record is the source because the
/// lifecycle writes an entry for every transition in the transaction that makes it, whereas the
/// queue table holds only the current claim state and is overwritten on every claim, release and
/// advance, so a count taken from it over a closed period could change after it was read.
///
/// It returns BARE COUNTS on a boundary summary, exactly as the delivered cost summaries do. They
/// become quantities at one composing site only, which decides every measurement case; nothing
/// here decides whether a count was observed.
/// </summary>
public interface IThroughputReader
{
    /// <summary>
    /// The action counts over the half-open interval from <paramref name="periodStart"/> to
    /// <paramref name="periodEnd"/>, the record's earliest instant, and the instant the datastore
    /// read them at, by its own clock.
    /// </summary>
    Task<ThroughputSummary> ReadAsync(DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken);
}

/// <summary>
/// What the append-only record held for one half-open interval, as counts of lifecycle actions.
///
/// The in-period counts are the entries whose instant lies in the interval. The to-close counts
/// are every entry up to the interval's close, which is what the units waiting at the close are
/// reconstructed from: arrivals into the ready queue less the claims that took units out of it.
/// <see cref="EarliestEntry"/> is null when the record holds no entry at all.
///
/// <see cref="Quiet"/> states whether the read obtained the record horizon, which it can only do
/// when no audited transaction is in flight; <see cref="Horizon"/> is the horizon that read set, the
/// instant below which no entry can commit any more. When the read was not quiet no count was
/// taken: the horizon is null and every count is zero by default and means nothing, which the
/// composing site decides from <see cref="Quiet"/> rather than from the counts.
/// </summary>
public sealed record ThroughputSummary(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset? EarliestEntry,
    bool Quiet,
    DateTimeOffset? Horizon,
    long EnqueuedToClose,
    long StageSucceededToClose,
    long RetriedToClose,
    long ClaimedToClose,
    long ClaimedInPeriod,
    long RetriedInPeriod,
    long EscalatedInPeriod,
    long CompletedInPeriod);

/// <summary>
/// The item dossier reader (decision D-004 of the accepted design). One member per recorded
/// source, each answering for ONE item version and never for another version of the same item.
///
/// Every member returns null where no dossier header exists for the item version, and only
/// there: null means "no dossier was opened", which is a different fact from a dossier that was
/// opened and holds no row of the kind asked for, returned as an empty list.
/// </summary>
public interface IItemDossierReader
{
    /// <summary>The reconstituted dossier with the channel its item belongs to, or null where none was opened.</summary>
    Task<RecordedDossier?> DossierAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>The recorded stage outcomes, or null where no dossier was opened.</summary>
    Task<IReadOnlyList<StageEvidence>?> StageEvidenceAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>The recorded supply-audit entries, or null where no dossier was opened.</summary>
    Task<IReadOnlyList<SupplyAuditEntry>?> SupplyAuditAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>The recorded determination resolutions, or null where no dossier was opened.</summary>
    Task<IReadOnlyList<DeterminationResolution>?> DeterminationsAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken);
}

/// <summary>A dossier read back from the record store, with the channel its item belongs to.</summary>
public sealed record RecordedDossier(ItemDossier Dossier, ChannelId Channel, DateTimeOffset OpenedAt);

// ---------------------------------------------------------------------------
// The multi-channel change: the channel partition, the approval queue and the item's channel
// ---------------------------------------------------------------------------

/// <summary>
/// The register an item's channel is read from (the multi-channel change). An item carries a
/// mandatory channel reference, and a gate decision that concerns an item reads THAT channel rather
/// than one a caller supplies.
/// </summary>
public interface IItemRegister
{
    /// <summary>The channel the item is recorded against, or null where no such item is recorded.</summary>
    Task<ChannelId?> RecordedChannelAsync(ItemId item, CancellationToken cancellationToken);
}

/// <summary>
/// The channel partition reader (decision D-003 of the multi-channel design).
///
/// Every member returns BARE AGGREGATIONS on boundary summaries, exactly as the delivered cost and
/// throughput summaries do: one row per channel identifier present in the channel register or in the
/// rows read, and the company row, each computed by the datastore over the same rows in one statement,
/// so every additive company figure is the exact sum of its channel figures. The measurement cases
/// are decided at one composing site; nothing here decides whether a quantity was observed.
///
/// It has READ MEMBERS ONLY. Nothing here records a channel, a budget or a configuration value.
/// </summary>
public interface IChannelPartitionReader
{
    /// <summary>
    /// The operation record for one calendar month, partitioned by the operation row's channel, with
    /// the month's closure taken first: the delivered non-waiting closure of the record horizon,
    /// raising it when granted and otherwise reading it as stored.
    /// </summary>
    Task<OperationPartitionSummary> OperationsAsync(DateOnly month, CancellationToken cancellationToken);

    /// <summary>The closure of one calendar month alone, taken as <see cref="OperationsAsync"/> takes it.</summary>
    Task<MonthClosure> CloseMonthAsync(DateOnly month, CancellationToken cancellationToken);

    /// <summary>The item dossier register for one calendar month, partitioned by the item's channel.</summary>
    Task<DossierPartitionSummary> DossiersAsync(DateOnly month, CancellationToken cancellationToken);

    /// <summary>
    /// The lifecycle counts of the append-only record over a half-open period, partitioned by the
    /// channel of the work unit each entry's declared subject names, under the delivered closure.
    /// </summary>
    Task<ThroughputPartitionSummary> ThroughputAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);

    /// <summary>
    /// Each registered channel's budget and utilisation for one calendar month, the company's
    /// recorded cost against the supplied ceiling, and the alerts the delivered tracking recorded.
    /// </summary>
    Task<BudgetPartitionSummary> BudgetsAsync(DateOnly month, Money ceiling, CancellationToken cancellationToken);
}

/// <summary>One channel the channel register holds, with its company.</summary>
public sealed record RegisteredChannel(ChannelId Channel, CompanyId Company);

/// <summary>
/// How one calendar month's closure went. <see cref="ClosedByThisRead"/> states whether the read
/// obtained the record horizon exclusively, which it can only do when no audited or metered
/// transaction is in flight, and raised it; otherwise <see cref="Horizon"/> is the horizon as stored.
/// <see cref="EarliestEntry"/> is the append-only record's first instant, or null where it holds none.
/// </summary>
public sealed record MonthClosure(
    DateOnly Month,
    bool ClosedByThisRead,
    DateTimeOffset Horizon,
    DateTimeOffset? EarliestEntry);

/// <summary>One partition's aggregation over the operation record, as the datastore computed it.</summary>
public sealed record OperationTotals(
    long Operations,
    Money Cost,
    bool ContainsEstimates,
    long DeterministicOperations,
    Money DeterministicSetCost,
    IReadOnlyDictionary<CapabilityClass, Money> CostByCapability,
    long ServedTierRecords,
    long CarryingBothTiers,
    long AgreeingTiers);

/// <summary>One channel's partition of the operation record, and whether the register holds the channel.</summary>
public sealed record OperationPartitionRow(ChannelId Channel, bool InRegister, OperationTotals Totals);

/// <summary>
/// The operation record for one month: the closure, the register, each channel's partition and the
/// company's, and the company's variance against the approved envelope as the datastore subtracted it.
/// </summary>
public sealed record OperationPartitionSummary(
    MonthClosure Closure,
    IReadOnlyList<RegisteredChannel> Register,
    IReadOnlyList<OperationPartitionRow> Channels,
    OperationTotals Company,
    Money CompanyVarianceAgainstEnvelope);

/// <summary>One partition's counts over the item dossier register for one month.</summary>
public sealed record DossierTotals(long DossiersOpened, long StageOutcomes, long SupplyAuditEntries, long DeterminationResolutions);

/// <summary>One channel's partition of the item dossier register.</summary>
public sealed record DossierPartitionRow(ChannelId Channel, bool InRegister, DossierTotals Totals);

/// <summary>The item dossier register for one month, by the item's channel, and for the company.</summary>
public sealed record DossierPartitionSummary(
    DateOnly Month,
    DateTimeOffset? EarliestEntry,
    IReadOnlyList<RegisteredChannel> Register,
    IReadOnlyList<DossierPartitionRow> Channels,
    DossierTotals Company);

/// <summary>
/// One partition's lifecycle counts, in the delivered throughput summary's shape. The channel is
/// null for the unattributed partition.
/// </summary>
public sealed record ThroughputPartitionRow(ChannelId? Channel, bool InRegister, ThroughputSummary Summary);

/// <summary>
/// The lifecycle counts of one period, by the channel of the unit each entry names, and for the
/// company. <see cref="Unattributed"/> holds the entries whose subject names no recorded unit, or
/// null where there are none.
/// </summary>
public sealed record ThroughputPartitionSummary(
    IReadOnlyList<RegisteredChannel> Register,
    IReadOnlyList<ThroughputPartitionRow> Channels,
    ThroughputPartitionRow? Unattributed,
    ThroughputSummary Company);

/// <summary>
/// One registered channel's budget position for one month. <see cref="BudgetAmount"/> is null where
/// the budget register holds no budget for the channel and month, and then no utilisation is computed.
/// </summary>
public sealed record ChannelBudgetRow(
    ChannelId Channel,
    long Operations,
    Money Utilised,
    Money? BudgetAmount,
    decimal? UtilisationPercent,
    IReadOnlyDictionary<int, long> AlertsByThreshold);

/// <summary>Every registered channel's budget position, and the company's cost against the ceiling, for one month.</summary>
public sealed record BudgetPartitionSummary(
    MonthClosure Closure,
    IReadOnlyList<ChannelBudgetRow> Channels,
    long CompanyOperations,
    Money CompanyCost,
    decimal? CompanyUtilisationPercent,
    Money Ceiling);

/// <summary>
/// The approval queue reader (decision D-005 of the multi-channel design).
///
/// READ MEMBERS ONLY, over the gate-transition record and the approval record, through each item's
/// channel. It reads no configuration, and there is no member here that writes, presents, decides or
/// transitions, so no route around owner approval exists through it.
/// </summary>
public interface IApprovalQueueReader
{
    /// <summary>The item versions of one channel whose latest recorded gate state is awaiting owner approval.</summary>
    Task<IReadOnlyList<AwaitingApproval>> AwaitingOwnerApprovalAsync(ChannelId channel, CancellationToken cancellationToken);

    /// <summary>The item versions of every channel whose latest recorded gate state is awaiting owner approval.</summary>
    Task<IReadOnlyList<AwaitingApproval>> AwaitingOwnerApprovalAsync(CancellationToken cancellationToken);

    /// <summary>
    /// The approvals of one channel's items decided in one calendar month, by decision instant, each
    /// with the predecessor its rework mark names where one is recorded.
    /// </summary>
    Task<IReadOnlyList<DecidedApproval>> DecidedAsync(ChannelId channel, DateOnly month, CancellationToken cancellationToken);
}

/// <summary>One item version awaiting owner approval, with its channel and the instant it came to rest there.</summary>
public sealed record AwaitingApproval(ChannelId Channel, ItemId Item, ItemVersion Version, DateTimeOffset AwaitingSince);

/// <summary>One recorded approval and the predecessor its rework mark names, where that is recorded.</summary>
public sealed record DecidedApproval(Approval Approval, Approval? ReworkPredecessor);
