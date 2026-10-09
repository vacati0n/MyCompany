using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The analytics read surface (decision D-002, plan tasks T-006, T-007, T-012, T-013).
///
/// It is composed from read ports alone and writes nothing. It holds no dependency on the
/// capability boundary, the credential broker or the persistence adapters, and it cannot acquire
/// one: its assembly references the domain and the ports and nothing else, so the types a
/// reasoning-model call would have to name are not referenceable from here at all.
///
/// The throughput reading and the four record-source readings answer from the company's OWN
/// RECORDED STATE through the read ports the composition root wires, rather than from records a
/// caller hands to a composer. Every reading's measurement cases are decided by the composers, at
/// one site each; nothing here decides whether a count was observed.
/// </summary>
public sealed class AnalyticsReportService
{
    private readonly ICostRollupReader _costs;
    private readonly IServedTierReader _tiers;
    private readonly IRevenueParameterRegister _revenue;
    private readonly IThroughputReader _throughput;
    private readonly IItemDossierReader _dossiers;
    private readonly IGateLedger _gates;
    private readonly IAssetLedger _assets;
    private readonly IChannelPartitionReader _partitions;
    private readonly IApprovalQueueReader _approvals;
    private readonly IOperatingRegisters _registers;
    private readonly IBenchmarkReader _benchmarks;
    private readonly IClock _clock;

    public AnalyticsReportService(
        ICostRollupReader costs,
        IServedTierReader tiers,
        IRevenueParameterRegister revenue,
        IThroughputReader throughput,
        IItemDossierReader dossiers,
        IGateLedger gates,
        IAssetLedger assets,
        IChannelPartitionReader partitions,
        IApprovalQueueReader approvals,
        IOperatingRegisters registers,
        IBenchmarkReader benchmarks,
        IClock clock)
    {
        _costs = costs;
        _tiers = tiers;
        _revenue = revenue;
        _throughput = throughput;
        _dossiers = dossiers;
        _gates = gates;
        _assets = assets;
        _partitions = partitions;
        _approvals = approvals;
        _registers = registers;
        _benchmarks = benchmarks;
        _clock = clock;
    }

    public DateOnly CurrentPeriod()
    {
        var now = _clock.UtcNow;
        return new DateOnly(now.Year, now.Month, 1);
    }

    /// <summary>One produced item's cost, answerable for the records behind it.</summary>
    public async Task<CostReadModel> ItemCostAsync(ItemId item, CancellationToken cancellationToken) =>
        AnalyticsComposers.Cost(await _costs.ItemSummaryAsync(item, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// One calendar period's cost, on the same rule, carrying whether the month can still change. The
    /// figure and its finality come from ONE close-then-read transaction (the AI-economics change,
    /// decision D-007 of its design): the month is closed first and read afterwards on the same
    /// transaction, so a month read as final was final before the figure was read, and the finality is
    /// that of the read that produced the figure rather than of a separate closure.
    /// </summary>
    public async Task<CostReadModel> PeriodCostAsync(DateOnly period, CancellationToken cancellationToken)
    {
        var reading = await _costs.PeriodSummaryAsync(period, cancellationToken).ConfigureAwait(false);
        return AnalyticsComposers.Cost(reading.Value) with { Finality = ChannelAnalyticsComposers.Finality(reading.Closure) };
    }

    /// <summary>
    /// Every served-tier record of a period, each carrying the single-record caveat, with the month's
    /// finality from the read that produced the records.
    /// </summary>
    public async Task<ServedTierReading> ServedTiersAsync(DateOnly period, CancellationToken cancellationToken)
    {
        var reading = await _tiers.RecordsForPeriodAsync(period, cancellationToken).ConfigureAwait(false);
        return new ServedTierReading
        {
            Month = period,
            Finality = ChannelAnalyticsComposers.Finality(reading.Closure),
            Records = AnalyticsComposers.ServedTiers(reading.Value),
        };
    }

    /// <summary>
    /// The tier ratio, unmeasured while the period holds fewer than two records carrying both a
    /// requested and a served tier, and carrying its definition either way, with the finality of the read
    /// that produced the records it is computed from.
    /// </summary>
    public async Task<TierRatioReadModel> TierRatioAsync(DateOnly period, CancellationToken cancellationToken)
    {
        var reading = await _tiers.RecordsForPeriodAsync(period, cancellationToken).ConfigureAwait(false);
        return AnalyticsComposers.TierRatio(period, reading.Value) with
        {
            Finality = ChannelAnalyticsComposers.Finality(reading.Closure),
        };
    }

    /// <summary>
    /// A month's tier distribution per served tier and untiered (the AI-economics change, decision D-006
    /// of its design), from one close-then-read transaction, with the assumed split labelled, the count of
    /// operations carrying a served tier and the month's finality. It decides no split.
    /// </summary>
    public async Task<TierDistributionReading> TierDistributionAsync(DateOnly month, CancellationToken cancellationToken) =>
        EconomicsComposers.TierDistribution(
            await _partitions.TierDistributionAsync(month, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Every route's benchmark position for every task class (the AI-economics change, decision D-002 of
    /// its design), unmeasured wherever the benchmark record holds no observation of the pair.
    /// </summary>
    public async Task<BenchmarkRecordReading> BenchmarkAsync(CancellationToken cancellationToken) =>
        EconomicsComposers.Benchmark(await _benchmarks.RecordAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>The benchmark observations booked into one month, with the month's finality from the same read.</summary>
    public async Task<BenchmarkMonthReading> BenchmarkMonthAsync(DateOnly month, CancellationToken cancellationToken) =>
        EconomicsComposers.BenchmarkMonth(await _benchmarks.ObservationsAsync(month, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// The revenue-derived figures that are lit. Empty while the register holds no row carrying a
    /// source observation and its date, which is every state this change delivers.
    /// </summary>
    public async Task<IReadOnlyList<RevenueDerivedFigureReadModel>> RevenueDerivedAsync(
        CancellationToken cancellationToken) =>
        AnalyticsComposers.Visible(await _revenue.RecordedAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Waiting, claimed, retried, escalated and completed work for a caller-supplied half-open
    /// period of instants, read from the append-only record. Each quantity is observed, observed
    /// zero or unmeasured by the one coverage rule, and the reading states that it establishes no
    /// rate, depth or concurrency figure.
    /// </summary>
    public async Task<ThroughputReadModel> ThroughputAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken) =>
        AnalyticsComposers.Throughput(
            await _throughput.ReadAsync(periodStart, periodEnd, cancellationToken).ConfigureAwait(false));

    /// <summary>The item dossier of one item version, one count per recorded component.</summary>
    public async Task<ItemDossierReadModel> ItemDossierAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken) =>
        AnalyticsComposers.DossierCounts(
            item, version, await _dossiers.DossierAsync(item, version, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Every production-path stage outcome of one item version, and every refusal, from recorded
    /// state.
    ///
    /// The delivered publish-ready predicate is run over the RECONSTITUTED dossier with the current
    /// gate state, the channel's library registrations and the script-commit instant taken from the
    /// recorded script stage, and the three first-publication conditions are read for the channel.
    /// Where no dossier is opened every stage reads unmeasured and no predicate is evaluated, so no
    /// refusal is listed over a dossier that does not exist.
    /// </summary>
    public async Task<StageOutcomeSurface> StageOutcomesAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        var recorded = await _dossiers.DossierAsync(item, version, cancellationToken).ConfigureAwait(false);
        if (recorded is null)
        {
            return AnalyticsComposers.StageOutcomesUnrecorded(item, version);
        }

        var state = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);
        var registrations = await _assets.RegistrationsAsync(recorded.Channel, cancellationToken).ConfigureAwait(false);
        var conditions = await _gates
            .FirstPublicationConditionsAsync(recorded.Channel, cancellationToken).ConfigureAwait(false);

        // The script commits when its stage succeeds. A script stage recorded as anything else has
        // not committed, and the supply audit's ordering question does not yet arise.
        var script = recorded.Dossier.StageFor(ProductionStage.Script);
        var scriptCommittedAt = script is { Outcome: StageOutcome.Succeeded } ? script.RecordedAt : (DateTimeOffset?)null;

        var verdict = PublishReadyPredicate.Evaluate(
            recorded.Dossier, state, registrations, recorded.Channel, scriptCommittedAt);

        return AnalyticsComposers.StageOutcomes(recorded.Dossier, verdict, conditions);
    }

    /// <summary>The supply audit of one item version as recorded, one entry per recorded audit.</summary>
    public async Task<SupplyAuditReading> SupplyAuditAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken) =>
        AnalyticsComposers.SupplyAuditRecorded(
            item, version, await _dossiers.SupplyAuditAsync(item, version, cancellationToken).ConfigureAwait(false));

    /// <summary>The compliance determinations of one item version as recorded, over the closed set.</summary>
    public async Task<DeterminationReading> DeterminationsAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken) =>
        AnalyticsComposers.DeterminationsRecorded(
            item, version, await _dossiers.DeterminationsAsync(item, version, cancellationToken).ConfigureAwait(false));

    // -----------------------------------------------------------------------
    // The channel partition of every published reading (the multi-channel change)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The operation record for one month, by channel and for the company: cost, operations, the
    /// deterministic-set cost, the variance against the envelope, the cost by capability class, the
    /// served-tier records and the tier ratio, with the month's finality.
    /// </summary>
    public async Task<OperationPartitionReading> OperationsByChannelAsync(DateOnly month, CancellationToken cancellationToken) =>
        ChannelAnalyticsComposers.Operations(
            await _partitions.OperationsAsync(month, cancellationToken).ConfigureAwait(false));

    /// <summary>The item dossier register for one month, by the item's channel and for the company.</summary>
    public async Task<DossierPartitionReading> DossiersByChannelAsync(DateOnly month, CancellationToken cancellationToken) =>
        ChannelAnalyticsComposers.Dossiers(
            await _partitions.DossiersAsync(month, cancellationToken).ConfigureAwait(false));

    /// <summary>Throughput and queue quantities for a half-open period, by the channel of each entry's unit.</summary>
    public async Task<ThroughputPartitionReading> ThroughputByChannelAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken) =>
        ChannelAnalyticsComposers.Throughput(
            await _partitions.ThroughputAsync(periodStart, periodEnd, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Every registered channel's budget utilisation and the company ceiling's for one month, at the
    /// four thresholds, with the month's finality and the company reading's coverage statement.
    /// </summary>
    public async Task<BudgetPartitionReading> BudgetsByChannelAsync(DateOnly month, CancellationToken cancellationToken) =>
        ChannelAnalyticsComposers.Budgets(
            await _partitions.BudgetsAsync(month, ApprovedEnvelope.MonthlyTotal, cancellationToken).ConfigureAwait(false));

    /// <summary>One channel's approval queue: a view over recorded gate state.</summary>
    public async Task<ChannelApprovalListing> ApprovalListingAsync(ChannelId channel, CancellationToken cancellationToken)
    {
        var registered = await IsRegisteredAsync(channel, cancellationToken).ConfigureAwait(false);
        var awaiting = await _approvals.AwaitingOwnerApprovalAsync(channel, cancellationToken).ConfigureAwait(false);
        return ChannelAnalyticsComposers.Listing(channel, registered, awaiting);
    }

    /// <summary>Every registered channel's approval queue, each a view over recorded gate state.</summary>
    public async Task<IReadOnlyList<ChannelApprovalListing>> ApprovalListingsAsync(CancellationToken cancellationToken)
    {
        var channels = await _registers.ChannelsAsync(cancellationToken).ConfigureAwait(false);
        var awaiting = await _approvals.AwaitingOwnerApprovalAsync(cancellationToken).ConfigureAwait(false);
        return channels.Select(c => ChannelAnalyticsComposers.Listing(c.Id, inRegister: true, awaiting)).ToArray();
    }

    /// <summary>One channel's approval workload for one month, by decision instant.</summary>
    public async Task<ChannelApprovalWorkload> ApprovalWorkloadAsync(
        ChannelId channel,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        var registered = await IsRegisteredAsync(channel, cancellationToken).ConfigureAwait(false);
        var decided = await _approvals.DecidedAsync(channel, month, cancellationToken).ConfigureAwait(false);
        return ChannelAnalyticsComposers.Workload(channel, registered, month, decided);
    }

    private async Task<bool> IsRegisteredAsync(ChannelId channel, CancellationToken cancellationToken) =>
        (await _registers.ChannelsAsync(cancellationToken).ConfigureAwait(false)).Any(c => c.Id.Equals(channel));
}
