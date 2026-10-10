using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Application.Ports;

// ---------------------------------------------------------------------------
// The AI-management change: the company record reader and its one snapshot (decisions D-001 and D-002)
// ---------------------------------------------------------------------------

/// <summary>
/// The company record reader (the AI-management change, decision D-001 of its design). READ MEMBERS ONLY.
///
/// <see cref="ReadAsync"/> reads EVERY source the management reports rest on in ONE read-only snapshot
/// transaction whose first statement fixes the report instant on the datastore's clock, reads the record
/// horizon AS STORED and computes the period's bounds; every later statement takes that instant as a
/// parameter and reads only rows recorded at or before it. No statement takes a row hold, an advisory hold
/// or the closure, so the read never writes, never advances the horizon and never waits on any hold of any
/// length; a lock or statement bound reached ends it in a named outcome inside its own command timeout.
/// Finality is read, never made.
/// </summary>
public interface ICompanyRecordReader
{
    /// <summary>The one snapshot for the named ISO week, or for the week containing the report instant where none is named.</summary>
    Task<CompanySnapshot> ReadAsync(ReportWeek? week, CancellationToken cancellationToken);

    /// <summary>The datastore's current UTC month, named by its first day, read on the datastore's clock alone.</summary>
    Task<DateOnly> CurrentMonthAsync(CancellationToken cancellationToken);
}

/// <summary>An ISO week, named by its ISO year and its week number. The datastore computes its bounds.</summary>
public sealed record ReportWeek(int IsoYear, int Week)
{
    public override string ToString() => $"{IsoYear:D4}-W{Week:D2}";

    /// <summary>Reads a week named as 2026-W41; null where the text names none.</summary>
    public static ReportWeek? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        var parts = text.Trim().ToUpperInvariant().Split("-W");
        if (parts.Length == 2
            && int.TryParse(parts[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var year)
            && int.TryParse(parts[1], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var week)
            && year is >= 2000 and <= 9999
            && week is >= 1 and <= 53)
        {
            return new ReportWeek(year, week);
        }

        throw new FormatException($"'{text}' does not name an ISO week; name one as 2026-W41.");
    }
}

/// <summary>
/// A well-formed ISO week that does not exist, such as 2027-W53 (correction cycle): named, so the operator commands
/// refuse it exactly as they refuse a malformed one.
/// </summary>
public sealed class ReportWeekNotFoundException : ArgumentException
{
    public ReportWeekNotFoundException(ReportWeek named, ReportWeek resolved)
        : base($"ISO week {named} does not exist; the datastore resolves its Monday to week {resolved}. Name a week that exists.")
    {
        Named = named;
    }

    public ReportWeek Named { get; }
}

/// <summary>Why a company read ended without a snapshot. Each outcome is named; no other ends it.</summary>
public enum CompanyReadFailure
{
    /// <summary>A transaction-local lock or statement bound was reached: the store is being changed.</summary>
    StoreBeingChanged = 1,

    /// <summary>The eighth schema resource has not been applied to the store.</summary>
    EighthResourceMissing = 2,

    /// <summary>Any other datastore failure, its class named.</summary>
    ReadFailed = 3,

    /// <summary>
    /// The transaction-local statement bound was reached: the read itself was too slow. A lock wait ends at the
    /// lower lock bound first, so this bound names slow execution, never a store being changed (correction cycle).
    /// </summary>
    ReadTooSlow = 4,
}

/// <summary>A company read that ended in one of its named outcomes, carrying what ended it.</summary>
public sealed class CompanyReadException : Exception
{
    public CompanyReadException(CompanyReadFailure failure, string detail, Exception? inner = null)
        : base($"The company read ended without a snapshot ({failure}): {detail}", inner)
    {
        Failure = failure;
        Detail = detail;
    }

    public CompanyReadFailure Failure { get; }

    public string Detail { get; }
}

/// <summary>
/// Everything one snapshot read, BARE, with the instant, the stored horizon and the period. Every summary
/// is the delivered shape its delivered composer decides cases over; nothing here decides a case.
/// </summary>
public sealed record CompanySnapshot
{
    /// <summary>The report instant: the first statement's datastore statement-start clock.</summary>
    public required DateTimeOffset Instant { get; init; }

    /// <summary>The record horizon as stored, read in the first statement.</summary>
    public required DateTimeOffset StoredHorizon { get; init; }

    /// <summary>The append-only record's first instant, or null where it holds none.</summary>
    public DateTimeOffset? EarliestEntry { get; init; }

    /// <summary>The ISO week read, and its half-open UTC bounds as the datastore computed them.</summary>
    public required ReportWeek Week { get; init; }

    public required DateTimeOffset PeriodStart { get; init; }

    public required DateTimeOffset PeriodEnd { get; init; }

    /// <summary>The UTC date of the report instant.</summary>
    public required DateOnly DatastoreDate { get; init; }

    public required IReadOnlyList<RegisteredChannel> Register { get; init; }

    /// <summary>Every booking month the week touches, one or two, each with its own closure as stored.</summary>
    public required IReadOnlyList<SnapshotMonth> Months { get; init; }

    /// <summary>The operations booked inside the week, per channel and for the company, under the week's own closure.</summary>
    public required WeekOperations WeekOperations { get; init; }

    /// <summary>The lifecycle counts of the week, under the stored horizon as coverage.</summary>
    public required ThroughputPartitionSummary Throughput { get; init; }

    /// <summary>Counts of the audit actions recorded inside the week, per item channel, unattributed where none.</summary>
    public required IReadOnlyList<AuditActionCount> AuditActions { get; init; }

    /// <summary>The recorded reasons of every send-back recorded inside the week.</summary>
    public required IReadOnlyList<RecordedSendBack> SendBacks { get; init; }

    /// <summary>The awaiting-approval listing over the recorded gate state.</summary>
    public required IReadOnlyList<AwaitingApproval> Awaiting { get; init; }

    /// <summary>Per registered channel, the item versions whose latest recorded gate state is published.</summary>
    public required IReadOnlyList<ChannelCount> Published { get; init; }

    /// <summary>Per registered channel, the three first-publication conditions as recorded.</summary>
    public required IReadOnlyList<ChannelConditions> Conditions { get; init; }

    public required IReadOnlyList<RevenueParameterRecord> RevenueParameters { get; init; }

    /// <summary>Every route and every observation recorded at or before the instant.</summary>
    public required BenchmarkRecordSummary Benchmark { get; init; }

    /// <summary>Every route the register holds, with its capability class.</summary>
    public required IReadOnlyList<Route> Routes { get; init; }

    /// <summary>
    /// Each route's latest recorded availability state and each provider account's status, as the snapshot holds
    /// them (correction cycle): the CTO report counts comparable runs over the same available provider routes the
    /// resolution function's availability step admits.
    /// </summary>
    public required IReadOnlyDictionary<RouteId, RouteAvailability> Availability { get; init; }

    public required IReadOnlyDictionary<ProviderAccountId, ProviderAccountStatus> AccountStatus { get; init; }

    /// <summary>Every controller decision booked inside the week, as recorded.</summary>
    public required IReadOnlyList<RecordedControllerDecision> Decisions { get; init; }

    /// <summary>Every held operation recorded at or before the instant, with its held-outcome row where one exists.</summary>
    public required IReadOnlyList<HeldOutcomeRecord> Held { get; init; }

    /// <summary>Per registered channel, how many operations failed inside the week.</summary>
    public required IReadOnlyList<ChannelCount> FailedOperations { get; init; }

    public required IReadOnlyList<RegisterEntry> RegisterEntries { get; init; }

    public required IReadOnlyList<PolicyStatementRecord> PolicyStatements { get; init; }

    public required IReadOnlyList<ReverificationResult> Reverifications { get; init; }

    /// <summary>
    /// The latest production of each produced item, read in the same snapshot (the production change, decision
    /// D-017 of its design). Null where the reader did not read it, which composes nothing, as delivered; empty where
    /// the production register holds no production, which composes a line reading not recorded and naming it.
    /// </summary>
    public IReadOnlyList<ProducedItemReading>? ProducedItems { get; init; }

    /// <summary>
    /// The store's recorded designation, read in the same snapshot (the production change, decision D-005 of its design),
    /// or null where none is recorded. In a DEMONSTRATION store every observed figure of every report is a demonstration
    /// figure and is never read, counted or rendered as an observation.
    /// </summary>
    public MediaCompany.Domain.Production.StoreDesignation? Designation { get; init; }

    /// <summary>
    /// How many of each channel's items carry a recorded cap, read in the same snapshot (the production change; the
    /// ruling on the review's question of the unbudgeted-channel rule). Null where the reader did not read it or the
    /// cap record is absent, which keeps the delivered rule exactly; a channel with no row has no item.
    /// </summary>
    public IReadOnlyList<ChannelItemCapCoverage>? ItemCapCoverage { get; init; }
}

/// <summary>One channel's items, and how many of them carry a recorded cap, BARE, as the snapshot read them.</summary>
public sealed record ChannelItemCapCoverage
{
    public required ChannelId Channel { get; init; }

    public required long Items { get; init; }

    public required long CappedItems { get; init; }
}

/// <summary>
/// One produced item's latest production, BARE, as the snapshot read it (the production change, decision D-017 of
/// its design): its version, mode and the store's designation, its stage outcomes, its operations and their booked
/// cost, its recorded cap with the open reservations, and the runtime a probe of the rendered file measured. Nothing
/// here decides a case: the composer labels every figure of a demonstration store demonstration, never observed.
/// </summary>
public sealed record ProducedItemReading
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required string Mode { get; init; }
    public required string Designation { get; init; }
    public required DateTimeOffset OpenedAt { get; init; }
    public required IReadOnlyList<MediaCompany.Domain.Production.ProducedStageOutcome> Stages { get; init; }
    public required long Operations { get; init; }
    public required long UnstatedOperations { get; init; }
    public required MediaCompany.Domain.Accounting.Money Booked { get; init; }
    public MediaCompany.Domain.Accounting.Money? Cap { get; init; }
    public string? CapSource { get; init; }
    public required MediaCompany.Domain.Accounting.Money OpenReservations { get; init; }
    public required long OpenReservationCount { get; init; }
    public TimeSpan? MeasuredRuntime { get; init; }
    public string? RenderedPath { get; init; }
}

/// <summary>One booking month the week touches: its closure as stored, and its readings bounded by the instant.</summary>
public sealed record SnapshotMonth(
    MonthClosure Closure,
    OperationPartitionSummary Operations,
    BudgetPartitionSummary AgainstCeiling,
    BudgetPartitionSummary AgainstEnvelope,
    TierDistributionSummary Tiers);

/// <summary>
/// The operations booked inside the week, one row per channel the register holds and per channel identifier the
/// rows name, and the company's, aggregated by the datastore over the same rows in one statement.
/// </summary>
public sealed record WeekOperations(IReadOnlyList<OperationPartitionRow> Channels, OperationTotals Company);

/// <summary>A bare count of one audit action in the week, for the channel the entry's item belongs to, or null where none.</summary>
public sealed record AuditActionCount(string Action, ChannelId? Channel, long Count);

/// <summary>A bare count for one channel.</summary>
public sealed record ChannelCount(ChannelId Channel, long Count);

/// <summary>One send-back's recorded reason, verbatim, with its action and datastore instant.</summary>
public sealed record RecordedSendBack(string Action, ChannelId? Channel, string Reason, DateTimeOffset RecordedAt);

/// <summary>One channel's three first-publication conditions, as recorded.</summary>
public sealed record ChannelConditions(ChannelId Channel, FirstPublicationConditionRegister Register);
