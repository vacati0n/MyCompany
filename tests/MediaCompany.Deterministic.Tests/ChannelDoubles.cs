using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The item register as a demonstration double: each item's recorded channel, and optionally one
/// channel every item is recorded against. An item it holds nothing for reads as not recorded.
/// </summary>
internal sealed class RecordedItems(ChannelId? everyItemIn = null) : IItemRegister
{
    private readonly Dictionary<ItemId, ChannelId> _items = [];

    public RecordedItems Record(ItemId item, ChannelId channel)
    {
        _items[item] = channel;
        return this;
    }

    public Task<ChannelId?> RecordedChannelAsync(ItemId item, CancellationToken cancellationToken) =>
        Task.FromResult(_items.TryGetValue(item, out var channel) ? channel : everyItemIn);
}

/// <summary>
/// The channel partition reader as a demonstration double: it returns the summaries a demonstration
/// supplies, and by default an empty month closed at a horizon the demonstration names.
/// </summary>
internal sealed class StubPartitions : IChannelPartitionReader
{
    public DateTimeOffset Horizon { get; set; } = DateTimeOffset.UnixEpoch;
    public DateTimeOffset? EarliestEntry { get; set; }
    public bool ClosedByThisRead { get; set; } = true;
    public IReadOnlyList<RegisteredChannel> Register { get; set; } = [];
    public IReadOnlyList<OperationPartitionRow> OperationRows { get; set; } = [];
    public OperationTotals CompanyOperations { get; set; } = Empty;
    public Money CompanyVariance { get; set; } = new(-77.41m);

    public static OperationTotals Empty { get; } =
        new(0, Money.Zero(), false, 0, Money.Zero(), new Dictionary<CapabilityClass, Money>(), 0, 0, 0);

    public MonthClosure Closure(DateOnly month) => new(month, ClosedByThisRead, Horizon, EarliestEntry);

    public Task<MonthClosure> CloseMonthAsync(DateOnly month, CancellationToken cancellationToken) =>
        Task.FromResult(Closure(month));

    public Task<OperationPartitionSummary> OperationsAsync(DateOnly month, CancellationToken cancellationToken) =>
        Task.FromResult(new OperationPartitionSummary(Closure(month), Register, OperationRows, CompanyOperations, CompanyVariance));

    public Task<DossierPartitionSummary> DossiersAsync(DateOnly month, CancellationToken cancellationToken) =>
        Task.FromResult(new DossierPartitionSummary(month, EarliestEntry, Register, [], new DossierTotals(0, 0, 0, 0)));

    public Task<ThroughputPartitionSummary> ThroughputAsync(
        DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken)
    {
        var none = new ThroughputSummary(periodStart, periodEnd, EarliestEntry, true, Horizon, 0, 0, 0, 0, 0, 0, 0, 0);
        return Task.FromResult(new ThroughputPartitionSummary(Register, [], null, none));
    }

    public Task<BudgetPartitionSummary> BudgetsAsync(DateOnly month, Money ceiling, CancellationToken cancellationToken) =>
        Task.FromResult(new BudgetPartitionSummary(Closure(month), [], 0, Money.Zero(), null, ceiling));

    /// <summary>The tier distribution parts a demonstration supplies; by default a month holding no operation.</summary>
    public IReadOnlyList<TierPartTotals> TierParts { get; set; } = [];

    public Task<TierDistributionSummary> TierDistributionAsync(DateOnly month, CancellationToken cancellationToken)
    {
        var total = new TierPartTotals(
            null,
            TierParts.Sum(p => p.Operations),
            TierParts.Sum(p => p.InputUnits),
            TierParts.Sum(p => p.OutputUnits),
            TierParts.Sum(p => p.CachedUnits),
            new Money(TierParts.Sum(p => p.Cost.Amount)),
            TierParts.Sum(p => p.UnstatedCosts));

        return Task.FromResult(new TierDistributionSummary(
            Closure(month), TierParts, total, TierParts.Where(p => p.Tier is not null).Sum(p => p.Operations)));
    }
}
