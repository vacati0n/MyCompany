using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain;
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
    private readonly IClock _clock;

    public AnalyticsReportService(
        ICostRollupReader costs,
        IServedTierReader tiers,
        IRevenueParameterRegister revenue,
        IThroughputReader throughput,
        IItemDossierReader dossiers,
        IGateLedger gates,
        IAssetLedger assets,
        IClock clock)
    {
        _costs = costs;
        _tiers = tiers;
        _revenue = revenue;
        _throughput = throughput;
        _dossiers = dossiers;
        _gates = gates;
        _assets = assets;
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

    /// <summary>One calendar period's cost, on the same rule.</summary>
    public async Task<CostReadModel> PeriodCostAsync(DateOnly period, CancellationToken cancellationToken) =>
        AnalyticsComposers.Cost(await _costs.PeriodSummaryAsync(period, cancellationToken).ConfigureAwait(false));

    /// <summary>Every served-tier record of a period, each carrying the single-record caveat.</summary>
    public async Task<IReadOnlyList<ServedTierReadModel>> ServedTiersAsync(
        DateOnly period,
        CancellationToken cancellationToken) =>
        AnalyticsComposers.ServedTiers(
            await _tiers.RecordsForPeriodAsync(period, cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// The tier ratio, unmeasured while the period holds fewer than two records carrying both a
    /// requested and a served tier, and carrying its definition either way.
    /// </summary>
    public async Task<TierRatioReadModel> TierRatioAsync(DateOnly period, CancellationToken cancellationToken) =>
        AnalyticsComposers.TierRatio(
            period,
            await _tiers.RecordsForPeriodAsync(period, cancellationToken).ConfigureAwait(false));

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
}
