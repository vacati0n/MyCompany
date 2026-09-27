using MediaCompany.Application.Ports;
using MediaCompany.Domain;
using MediaCompany.Domain.Analytics;

namespace MediaCompany.Deterministic.Analytics;

/// <summary>
/// The analytics read surface (decision D-002, plan tasks T-006, T-007, T-012, T-013).
///
/// It is composed from read ports alone and writes nothing. It holds no dependency on the
/// capability boundary, the credential broker or the persistence adapters, and it cannot acquire
/// one: its assembly references the domain and the ports and nothing else, so the types a
/// reasoning-model call would have to name are not referenceable from here at all.
/// </summary>
public sealed class AnalyticsReportService
{
    private readonly ICostRollupReader _costs;
    private readonly IServedTierReader _tiers;
    private readonly IRevenueParameterRegister _revenue;
    private readonly IClock _clock;

    public AnalyticsReportService(
        ICostRollupReader costs,
        IServedTierReader tiers,
        IRevenueParameterRegister revenue,
        IClock clock)
    {
        _costs = costs;
        _tiers = tiers;
        _revenue = revenue;
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

    /// <summary>The tier ratio, unmeasured while the period holds fewer than two served-tier records.</summary>
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
}
