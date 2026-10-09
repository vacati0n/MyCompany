using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The company record reader as a double for services that never read a snapshot: the snapshot read refuses,
/// so a service that reached it would fail its demonstration rather than pass on a read nobody took, and the
/// current month is a fixed demonstration month (the AI-management change).
/// </summary>
internal sealed class NoCompanyRecords : ICompanyRecordReader
{
    public static readonly DateOnly Month = new(2026, 10, 1);

    public Task<CompanySnapshot> ReadAsync(ReportWeek? week, CancellationToken cancellationToken) =>
        throw new NotSupportedException("No service under this demonstration reads the company snapshot.");

    public Task<DateOnly> CurrentMonthAsync(CancellationToken cancellationToken) => Task.FromResult(Month);
}

/// <summary>
/// A company snapshot built from DEMONSTRATION FIXTURES, as the company record reader would return it from one
/// read: every company, channel, operation, budget, decision, held request and register entry here exists only in
/// the demonstration that builds it, and none is an observation of the company's work.
/// </summary>
internal static class SnapshotFixture
{
    internal static readonly CompanyId Company = new(Guid.Parse("00000000-0000-0000-0000-00000000c001"));
    internal static readonly ChannelId ChannelOne = new(Guid.Parse("00000000-0000-0000-0000-0000000000a1"));

    /// <summary>The report instant of the fixture: Thursday 2026-10-08 12:00 UTC, inside ISO week 2026-W41.</summary>
    internal static readonly DateTimeOffset Instant = DateTimeOffset.Parse("2026-10-08T12:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    internal static readonly DateTimeOffset WeekStart = DateTimeOffset.Parse("2026-10-05T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture);
    internal static readonly DateTimeOffset WeekEnd = WeekStart.AddDays(7);
    internal static readonly DateOnly October = new(2026, 10, 1);

    internal static OperationTotals Totals(long operations, decimal cost, long unstated = 0) =>
        new(operations, new Money(cost), false, 0, Money.Zero(), new Dictionary<CapabilityClass, Money>(), 0, 0, 0)
        {
            UnstatedCosts = unstated,
        };

    /// <summary>
    /// A snapshot of one registered channel with no budget amount, the given company spend in October against the
    /// decided ceiling, and the record horizon as stored at the given instant.
    /// </summary>
    internal static CompanySnapshot Build(
        long operations = 0,
        decimal cost = 0m,
        DateTimeOffset? horizon = null,
        DateTimeOffset? earliest = null,
        decimal? channelBudget = null,
        IReadOnlyList<AwaitingApproval>? awaiting = null,
        IReadOnlyList<HeldOutcomeRecord>? held = null,
        IReadOnlyList<RecordedControllerDecision>? decisions = null,
        IReadOnlyList<RegisterEntry>? register = null,
        IReadOnlyList<Route>? routes = null,
        IReadOnlyList<BenchmarkObservation>? observations = null,
        IReadOnlyList<PolicyStatementRecord>? statements = null,
        IReadOnlyList<ReverificationResult>? results = null,
        IReadOnlyList<AuditActionCount>? actions = null)
    {
        var storedHorizon = horizon ?? Instant.AddHours(-1);
        var closure = new MonthClosure(October, false, storedHorizon, earliest ?? DateTimeOffset.Parse("2026-09-01T00:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
        var registerChannels = new[] { new RegisteredChannel(ChannelOne, Company) };
        var company = Totals(operations, cost);
        var percentOfCeiling = operations == 0 ? (decimal?)null : Math.Round(cost * 100m / ApprovedEnvelope.Metered.Amount, 4);
        var percentOfEnvelope = operations == 0 ? (decimal?)null : Math.Round(cost * 100m / ApprovedEnvelope.MonthlyTotal.Amount, 4);
        var channelRow = new ChannelBudgetRow(
            ChannelOne,
            operations,
            new Money(cost),
            channelBudget is { } amount ? new Money(amount) : null,
            channelBudget is { } b && operations > 0 ? Math.Round(cost * 100m / b, 4) : null,
            new Dictionary<int, long>());

        var operationSummary = new OperationPartitionSummary(
            closure,
            registerChannels,
            [new OperationPartitionRow(ChannelOne, true, company)],
            company,
            new Money(cost - ApprovedEnvelope.MonthlyTotal.Amount));

        var throughputSummary = new ThroughputSummary(WeekStart, WeekEnd, closure.EarliestEntry, true, storedHorizon, 0, 0, 0, 0, 0, 0, 0, 0);

        return new CompanySnapshot
        {
            Instant = Instant,
            StoredHorizon = storedHorizon,
            EarliestEntry = closure.EarliestEntry,
            Week = new ReportWeek(2026, 41),
            PeriodStart = WeekStart,
            PeriodEnd = WeekEnd,
            DatastoreDate = DateOnly.FromDateTime(Instant.UtcDateTime),
            Register = registerChannels,
            Months =
            [
                new SnapshotMonth(
                    closure,
                    operationSummary,
                    new BudgetPartitionSummary(closure, [channelRow], operations, new Money(cost), percentOfCeiling, ApprovedEnvelope.Metered),
                    new BudgetPartitionSummary(closure, [channelRow], operations, new Money(cost), percentOfEnvelope, ApprovedEnvelope.MonthlyTotal),
                    new TierDistributionSummary(closure, [], new TierPartTotals(null, 0, 0, 0, 0, Money.Zero(), 0), 0)),
            ],
            WeekOperations = new WeekOperations([new OperationPartitionRow(ChannelOne, true, company)], company),
            Throughput = new ThroughputPartitionSummary(
                registerChannels, [new ThroughputPartitionRow(ChannelOne, true, throughputSummary)], null, throughputSummary),
            AuditActions = actions ?? [],
            SendBacks = [],
            Awaiting = awaiting ?? [],
            Published = [new ChannelCount(ChannelOne, 0)],
            Conditions = [new ChannelConditions(ChannelOne, FirstPublicationConditionRegister.Empty)],
            RevenueParameters = [],
            Benchmark = new BenchmarkRecordSummary((routes ?? []).Select(r => r.Id).ToArray(), observations ?? []),
            Routes = routes ?? [],
            Availability = (routes ?? []).ToDictionary(r => r.Id, r => RouteAvailability.Serving(r.Id, Instant.AddDays(-1))),
            AccountStatus = Fixture.AllActive(),
            Decisions = decisions ?? [],
            Held = held ?? [],
            FailedOperations = [new ChannelCount(ChannelOne, 0)],
            RegisterEntries = register ?? Register(),
            PolicyStatements = statements ?? [],
            Reverifications = results ?? [],
        };
    }

    /// <summary>A register fixture: one decided entry, the unbudgeted-channel question and the re-admission question open.</summary>
    internal static IReadOnlyList<RegisterEntry> Register() =>
    [
        Entry("REG-003", RegisterEntryStatus.Decided, "the owner's decided ceiling, a fixture entry"),
        Entry("REG-004", RegisterEntryStatus.Open, "may an unbudgeted channel rely on the company ceiling, a fixture entry", "no: refused"),
        Entry("REG-008", RegisterEntryStatus.Open, "are deferred requests re-admitted, a fixture entry", "not re-admitted"),
    ];

    internal static RegisterEntry Entry(string id, RegisterEntryStatus status, string statement, string? interim = null, string? supersedes = null) => new()
    {
        Identifier = id,
        Statement = statement,
        Kind = status == RegisterEntryStatus.Decided ? RegisterEntryKind.Decision : RegisterEntryKind.Question,
        Status = status,
        Owner = "the owner",
        RecordedOn = new DateOnly(2026, 10, 9),
        InterimRuling = interim,
        DecidedBy = status == RegisterEntryStatus.Decided ? "the owner" : null,
        DecidedOn = status == RegisterEntryStatus.Decided ? new DateOnly(2026, 10, 9) : null,
        Supersedes = supersedes,
        EnteredAt = Instant.AddDays(-1),
    };
}
