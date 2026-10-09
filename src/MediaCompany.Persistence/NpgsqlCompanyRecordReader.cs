using System.Globalization;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Registry;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The company record reader (the AI-management change, decision D-001 of its design, as ruled at its Design
/// Gate). ONE READ-ONLY REPEATABLE-READ TRANSACTION OF ITS OWN, never through the unit of work.
///
/// Before its first statement it sets two TRANSACTION-LOCAL bounds, each strictly below the data source's
/// 30-second command timeout with a stated margin: a lock bound of <see cref="LockBound"/> and a statement
/// bound of <see cref="StatementBound"/>. The only hold a plain read can meet is an exclusive structural hold
/// while a resource is applied, and the lock bound also covers the share lock every statement takes against
/// such a change; a bound reached ends the read with its named outcome from the datastore, never with a
/// client-side command timeout.
///
/// Its FIRST STATEMENT reads, in one statement, the datastore's statement-start clock as the report instant,
/// the record horizon AS STORED, the append-only record's first instant and the period's bounds, computed by
/// the datastore from that instant or from the named ISO week. Every later statement takes the instant as a
/// parameter and reads only rows RECORDED AT OR BEFORE IT, so a row a writer committed between the instant
/// and the snapshot is never read, and the process clock is read nowhere.
///
/// No statement selects for share or update, takes an advisory hold or calls the closure, so no admission's
/// shared horizon hold or scope hold, no recovery under its 120-second or unbounded timeout, no chain-head
/// hold and no transaction whose client was lost is ever waited on, whatever its duration; and nothing here
/// writes, so finality is read, never made: a period is final where the stored horizon is at or after its end.
/// </summary>
public sealed class NpgsqlCompanyRecordReader : ICompanyRecordReader
{
    /// <summary>The transaction-local lock bound: 2 seconds, 28 seconds inside the 30-second command timeout.</summary>
    internal static readonly TimeSpan LockBound = TimeSpan.FromSeconds(2);

    /// <summary>The transaction-local statement bound: 5 seconds, 25 seconds inside the 30-second command timeout.</summary>
    internal static readonly TimeSpan StatementBound = TimeSpan.FromSeconds(5);

    /// <summary>The pattern an audit entry's subject names its item by, gate and lifecycle entries alike.</summary>
    internal const string ItemSubjectPattern =
        "item:([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})";

    /// <summary>The tables the eighth schema resource creates, each looked for in the first statement.</summary>
    private static readonly string[] EighthResourceTables =
        ["owner_decision_register", "platform_policy_statements", "platform_policy_reverifications", "admission_held_outcomes"];

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlCompanyRecordReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    /// <summary>
    /// A DEMONSTRATION SEAM, null in every production composition: a step the demonstrations run on another session
    /// after the first statement fixed the instant and the snapshot and before any source is read, so a commit can be
    /// made to land during composition. It is internal to this assembly and its demonstrations.
    /// </summary>
    internal Func<CancellationToken, Task>? AfterFirstStatement { get; init; }

    public async Task<DateOnly> CurrentMonthAsync(CancellationToken cancellationToken)
    {
        // The datastore's clock and nothing else: the measurable-now command names the month the datastore is
        // in, whatever the process clock says (decision D-015).
        await using var command = _dataSource.CreateCommand(
            "SELECT (date_trunc('month', statement_timestamp() AT TIME ZONE 'UTC'))::date");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return reader.GetFieldValue<DateOnly>(0);
    }

    public async Task<CompanySnapshot> ReadAsync(ReportWeek? week, CancellationToken cancellationToken)
    {
        try
        {
            return await ReadCoreAsync(week, cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException failed)
        {
            throw Named(failed);
        }
        catch (NpgsqlException failed)
        {
            throw new CompanyReadException(
                CompanyReadFailure.ReadFailed,
                $"the read failed below the datastore ({failed.GetType().Name}): {failed.Message}",
                failed);
        }
    }

    /// <summary>
    /// The named outcome of a datastore failure (correction cycle): the lock bound reports the store being changed,
    /// the statement bound a read too slow, any other failure its class.
    /// </summary>
    internal static CompanyReadException Named(PostgresException failed) => failed.SqlState switch
    {
        PostgresErrorCodes.LockNotAvailable => new CompanyReadException(
            CompanyReadFailure.StoreBeingChanged,
            $"the transaction-local lock bound of {LockBound.TotalSeconds:0} seconds was reached ({failed.SqlState}): "
            + "the store is being changed, and the read ended rather than wait",
            failed),
        PostgresErrorCodes.QueryCanceled => new CompanyReadException(
            CompanyReadFailure.ReadTooSlow,
            $"the transaction-local statement bound of {StatementBound.TotalSeconds:0} seconds was reached ({failed.SqlState}): "
            + "the read was too slow; no lock was waited on, since a lock wait ends at the lower lock bound first",
            failed),
        _ => new CompanyReadException(
            CompanyReadFailure.ReadFailed,
            $"the datastore failed the read with class {failed.SqlState}: {failed.MessageText}",
            failed),
    };

    private async Task<CompanySnapshot> ReadCoreAsync(ReportWeek? week, CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);

        // READ ONLY and the two transaction-local bounds, before any statement takes the snapshot.
        await using (var bounds = new NpgsqlCommand(
            "SET TRANSACTION READ ONLY; "
            + $"SET LOCAL lock_timeout = '{(int)LockBound.TotalMilliseconds}ms'; "
            + $"SET LOCAL statement_timeout = '{(int)StatementBound.TotalMilliseconds}ms'",
            connection,
            transaction))
        {
            await bounds.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var head = await FirstStatementAsync(connection, transaction, week, cancellationToken).ConfigureAwait(false);
        if (!head.EighthResourcePresent)
        {
            throw new CompanyReadException(
                CompanyReadFailure.EighthResourceMissing,
                "the store lacks the eighth schema resource (008-management.sql): the open-decisions register, the "
                + "platform-policy records and the held-outcome record were looked for and are absent; apply it with the "
                + "install command, at a stopped system");
        }

        if (AfterFirstStatement is { } step)
        {
            await step(cancellationToken).ConfigureAwait(false);
        }

        var asOf = head.Instant;
        var register = await NpgsqlChannelPartitionReader.RegisterAsync(connection, transaction, cancellationToken).ConfigureAwait(false);

        var months = new List<SnapshotMonth>();
        foreach (var month in head.Months)
        {
            var closure = new MonthClosure(month, ClosedByThisRead: false, head.StoredHorizon, head.EarliestEntry);

            var (channels, company, variance) = await NpgsqlChannelPartitionReader
                .OperationTotalsAsync(connection, transaction, month, null, asOf, cancellationToken).ConfigureAwait(false);
            var budgets = await NpgsqlChannelPartitionReader
                .ChannelBudgetsAsync(connection, transaction, month, asOf, cancellationToken).ConfigureAwait(false);
            var ceiling = await NpgsqlChannelPartitionReader
                .CompanyCeilingAsync(connection, transaction, month, ApprovedEnvelope.Metered, asOf, cancellationToken).ConfigureAwait(false);
            var envelope = await NpgsqlChannelPartitionReader
                .CompanyCeilingAsync(connection, transaction, month, ApprovedEnvelope.MonthlyTotal, asOf, cancellationToken).ConfigureAwait(false);
            var tiers = await NpgsqlChannelPartitionReader
                .TierPartsAsync(connection, transaction, month, asOf, cancellationToken).ConfigureAwait(false);

            months.Add(new SnapshotMonth(
                closure,
                new OperationPartitionSummary(
                    closure, register, NpgsqlChannelPartitionReader.PartitionRows(register, channels), company, variance),
                Budget(closure, budgets, ceiling, ApprovedEnvelope.Metered),
                Budget(closure, budgets, envelope, ApprovedEnvelope.MonthlyTotal),
                new TierDistributionSummary(closure, tiers.Parts, tiers.Total, tiers.Parts.Where(p => p.Tier is not null).Sum(p => p.Operations))));
        }

        var (weekChannels, weekCompany, _) = await NpgsqlChannelPartitionReader
            .OperationTotalsAsync(connection, transaction, null, (head.PeriodStart, head.PeriodEnd), asOf, cancellationToken)
            .ConfigureAwait(false);

        var throughput = await NpgsqlChannelPartitionReader.ThroughputCountsAsync(
            connection, transaction, register, head.PeriodStart, head.PeriodEnd, head.EarliestEntry, head.StoredHorizon, asOf,
            cancellationToken).ConfigureAwait(false);

        var snapshot = new CompanySnapshot
        {
            Instant = head.Instant,
            StoredHorizon = head.StoredHorizon,
            EarliestEntry = head.EarliestEntry,
            Week = head.Week,
            PeriodStart = head.PeriodStart,
            PeriodEnd = head.PeriodEnd,
            DatastoreDate = head.DatastoreDate,
            Register = register,
            Months = months,
            WeekOperations = new WeekOperations(NpgsqlChannelPartitionReader.PartitionRows(register, weekChannels), weekCompany),
            Throughput = throughput,
            AuditActions = await AuditActionsAsync(connection, transaction, head, cancellationToken).ConfigureAwait(false),
            SendBacks = await SendBacksAsync(connection, transaction, head, cancellationToken).ConfigureAwait(false),
            Awaiting = await AwaitingAsync(connection, transaction, cancellationToken).ConfigureAwait(false),
            Published = await PublishedAsync(connection, transaction, register, cancellationToken).ConfigureAwait(false),
            Conditions = await ConditionsAsync(connection, transaction, register, cancellationToken).ConfigureAwait(false),
            RevenueParameters = await RevenueAsync(connection, transaction, cancellationToken).ConfigureAwait(false),
            Benchmark = await NpgsqlBenchmarkReader.RecordOnAsync(connection, transaction, asOf, cancellationToken).ConfigureAwait(false),
            Routes = await RoutesAsync(connection, transaction, cancellationToken).ConfigureAwait(false),
            Availability = await AvailabilityAsync(connection, transaction, cancellationToken).ConfigureAwait(false),
            AccountStatus = await AccountsAsync(connection, transaction, cancellationToken).ConfigureAwait(false),
            Decisions = await DecisionsAsync(connection, transaction, head, cancellationToken).ConfigureAwait(false),
            Held = await HeldAsync(connection, transaction, asOf, cancellationToken).ConfigureAwait(false),
            FailedOperations = await FailedAsync(connection, transaction, head, register, cancellationToken).ConfigureAwait(false),
            RegisterEntries = await RegisterEntriesAsync(connection, transaction, asOf, cancellationToken).ConfigureAwait(false),
            PolicyStatements = await PolicyStatementsAsync(connection, transaction, asOf, cancellationToken).ConfigureAwait(false),
            Reverifications = await ReverificationsAsync(connection, transaction, asOf, cancellationToken).ConfigureAwait(false),
            ProducedItems = await ProducedItemsAsync(connection, transaction, asOf, cancellationToken).ConfigureAwait(false),
        };

        // Nothing was written; ending the read-only transaction releases its share locks and its snapshot.
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return snapshot;
    }

    // -----------------------------------------------------------------------
    // The first statement: the instant, the stored horizon, the period, the months
    // -----------------------------------------------------------------------

    private sealed record Head(
        DateTimeOffset Instant,
        DateTimeOffset StoredHorizon,
        DateTimeOffset? EarliestEntry,
        DateTimeOffset PeriodStart,
        DateTimeOffset PeriodEnd,
        ReportWeek Week,
        DateOnly DatastoreDate,
        IReadOnlyList<DateOnly> Months,
        bool EighthResourcePresent);

    private static async Task<Head> FirstStatementAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        ReportWeek? week,
        CancellationToken cancellationToken)
    {
        // ONE STATEMENT. Its statement-start clock is the report instant; the horizon is read AS STORED, with no
        // hold of any kind; and the week's bounds are the datastore's: the UTC week from Monday 00:00 inclusive to
        // the next Monday 00:00 exclusive, the one containing the instant, or the named ISO week.
        await using var command = new NpgsqlCommand(
            """
            WITH instant AS (SELECT statement_timestamp() AS at),
                 week AS (
                     SELECT CASE WHEN @iso_year IS NULL
                                 THEN date_trunc('week', (SELECT at FROM instant) AT TIME ZONE 'UTC')
                                 ELSE to_date(@iso_year::text || '-' || lpad(@iso_week::text, 2, '0') || '-1', 'IYYY-IW-ID')::timestamp
                            END AS monday
                 )
            SELECT i.at,
                   h.horizon,
                   (SELECT min(occurred_at) FROM audit_entries WHERE occurred_at <= i.at),
                   w.monday AT TIME ZONE 'UTC',
                   (w.monday + interval '7 days') AT TIME ZONE 'UTC',
                   extract(isoyear FROM w.monday)::int,
                   extract(week FROM w.monday)::int,
                   (i.at AT TIME ZONE 'UTC')::date,
                   date_trunc('month', w.monday)::date,
                   date_trunc('month', w.monday + interval '6 days')::date,
                   (SELECT bool_and(to_regclass(t) IS NOT NULL) FROM unnest(@eighth) AS t)
            FROM instant i
            CROSS JOIN week w
            CROSS JOIN audit_record_horizon h
            WHERE h.only_row
            """,
            connection,
            transaction);
        command.Parameters.Add("iso_year", NpgsqlDbType.Integer).Value = (object?)week?.IsoYear ?? DBNull.Value;
        command.Parameters.Add("iso_week", NpgsqlDbType.Integer).Value = (object?)week?.Week ?? DBNull.Value;
        command.Parameters.Add("eighth", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = EighthResourceTables;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new CompanyReadException(
                CompanyReadFailure.ReadFailed,
                "the record horizon the fifth schema resource creates was not readable, so no instant or period could be read");
        }

        var read = new ReportWeek(reader.GetInt32(5), reader.GetInt32(6));
        if (week is not null && read != week)
        {
            throw new ReportWeekNotFoundException(week, read);
        }

        var first = reader.GetFieldValue<DateOnly>(8);
        var last = reader.GetFieldValue<DateOnly>(9);

        return new Head(
            reader.GetFieldValue<DateTimeOffset>(0),
            reader.GetFieldValue<DateTimeOffset>(1),
            reader.IsDBNull(2) ? null : reader.GetFieldValue<DateTimeOffset>(2),
            reader.GetFieldValue<DateTimeOffset>(3),
            reader.GetFieldValue<DateTimeOffset>(4),
            read,
            reader.GetFieldValue<DateOnly>(7),
            first == last ? [first] : [first, last],
            !reader.IsDBNull(10) && reader.GetBoolean(10));
    }

    private static BudgetPartitionSummary Budget(
        MonthClosure closure,
        IReadOnlyList<ChannelBudgetRow> channels,
        (long Operations, Money Cost, decimal? Percent, long Unstated) company,
        Money ceiling) =>
        new(closure, channels, company.Operations, company.Cost, company.Percent, ceiling)
        {
            CompanyUnstatedOperations = company.Unstated,
        };

    private static void Week(NpgsqlCommand command, Head head)
    {
        command.Parameters.AddWithValue("start", head.PeriodStart);
        command.Parameters.AddWithValue("end", head.PeriodEnd);
        command.Parameters.AddWithValue("as_of", head.Instant);
    }

    // -----------------------------------------------------------------------
    // The append-only record: action counts and send-back reasons of the week
    // -----------------------------------------------------------------------

    private static async Task<IReadOnlyList<AuditActionCount>> AuditActionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Head head,
        CancellationToken cancellationToken)
    {
        // The action and the subject of each entry stamped inside the week and at or before the instant, the
        // subject's item reaching its channel through the item's own channel reference. No reason, reference
        // or other free-form column is read here.
        await using var command = new NpgsqlCommand(
            """
            SELECT a.action, i.channel_id, count(*)
            FROM audit_entries a
            LEFT JOIN items i ON i.item_id = (substring(a.subject FROM @pattern))::uuid
            WHERE a.occurred_at >= @start AND a.occurred_at < @end AND a.occurred_at <= @as_of
            GROUP BY a.action, i.channel_id
            ORDER BY a.action, i.channel_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("pattern", ItemSubjectPattern);
        Week(command, head);

        var counts = new List<AuditActionCount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            counts.Add(new AuditActionCount(
                reader.GetString(0), reader.IsDBNull(1) ? null : new ChannelId(reader.GetGuid(1)), reader.GetInt64(2)));
        }

        return counts;
    }

    private static async Task<IReadOnlyList<RecordedSendBack>> SendBacksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Head head,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT a.action, i.channel_id, a.reason, a.occurred_at
            FROM audit_entries a
            LEFT JOIN items i ON i.item_id = (substring(a.subject FROM @pattern))::uuid
            WHERE a.action = ANY(@actions)
              AND a.occurred_at >= @start AND a.occurred_at < @end AND a.occurred_at <= @as_of
            ORDER BY a.occurred_at, a.sequence_no
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("pattern", ItemSubjectPattern);
        command.Parameters.AddWithValue(
            "actions",
            new[] { PublicationGateService.OwnerSentBackAction, PublicationGateService.SentBackFromRightsCheckAction });
        Week(command, head);

        var reasons = new List<RecordedSendBack>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            reasons.Add(new RecordedSendBack(
                reader.GetString(0),
                reader.IsDBNull(1) ? null : new ChannelId(reader.GetGuid(1)),
                reader.GetString(2),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return reasons;
    }

    // -----------------------------------------------------------------------
    // The gate state, the conditions, the revenue register and the route register
    // -----------------------------------------------------------------------

    private static async Task<IReadOnlyList<AwaitingApproval>> AwaitingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        // The delivered per-channel view over the recorded gate state, every channel, latest by the
        // datastore's recorded order. The transitions carry caller instants, so they are bounded by the
        // snapshot itself rather than by the report instant (decision record of this change, deviation recorded).
        await using var command = new NpgsqlCommand(NpgsqlApprovalQueueReader.AwaitingSql, connection, transaction);
        command.Parameters.Add("channel_id", NpgsqlDbType.Uuid).Value = DBNull.Value;
        return await NpgsqlApprovalQueueReader.ReadAwaitingAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<ChannelCount>> PublishedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<RegisteredChannel> register,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT i.channel_id, count(*)
            FROM (
                SELECT DISTINCT ON (item_id, item_version) item_id, item_version, to_state
                FROM gate_transitions
                ORDER BY item_id, item_version, recorded_order DESC
            ) t
            JOIN items i ON i.item_id = t.item_id
            WHERE t.to_state = 'Published'
            GROUP BY i.channel_id
            """,
            connection,
            transaction);

        var counts = new Dictionary<ChannelId, long>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                counts[new ChannelId(reader.GetGuid(0))] = reader.GetInt64(1);
            }
        }

        return register.Select(r => new ChannelCount(r.Channel, counts.TryGetValue(r.Channel, out var n) ? n : 0)).ToArray();
    }

    private static async Task<IReadOnlyList<ChannelConditions>> ConditionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IReadOnlyList<RegisteredChannel> register,
        CancellationToken cancellationToken)
    {
        var conditions = new List<ChannelConditions>();
        foreach (var channel in register)
        {
            await using var command = new NpgsqlCommand(NpgsqlGateLedger.ConditionsSql, connection, transaction);
            command.Parameters.AddWithValue("channel_id", channel.Channel.Value);
            conditions.Add(new ChannelConditions(
                channel.Channel, await NpgsqlGateLedger.ReadConditionsAsync(command, cancellationToken).ConfigureAwait(false)));
        }

        return conditions;
    }

    private static async Task<IReadOnlyList<RevenueParameterRecord>> RevenueAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(NpgsqlRevenueParameterRegister.RecordedSql, connection, transaction);
        return await NpgsqlRevenueParameterRegister.ReadRecordedAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<Route>> RoutesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                   substitute_task, hold_reason, rated_quality, context_capacity, terms_basis, terms_verified_on,
                   reasoning_tier_stated
            FROM routes
            ORDER BY capability_class, tier, route_id
            """,
            connection,
            transaction);
        return await NpgsqlRouteRegistry.ReadRoutesAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyDictionary<RouteId, RouteAvailability>> AvailabilityAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        // The latest recorded state per route, as the admission snapshot reads it; current state, bounded by the
        // snapshot (its instants are the writer's).
        await using var command = new NpgsqlCommand(
            """
            SELECT DISTINCT ON (route_id) route_id, state, effective_from, reason, reset_or_probe_point, observed_quality
            FROM route_availability
            ORDER BY route_id, effective_from DESC
            """,
            connection,
            transaction);

        var states = new Dictionary<RouteId, RouteAvailability>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var route = new RouteId(reader.GetGuid(0));
            states[route] = new RouteAvailability(
                route,
                Enum.Parse<AvailabilityState>(reader.GetString(1)),
                reader.GetFieldValue<DateTimeOffset>(2),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetFieldValue<DateTimeOffset>(4),
                reader.IsDBNull(5) ? null : new QualityRating(reader.GetInt32(5)));
        }

        return states;
    }

    private static async Task<IReadOnlyDictionary<ProviderAccountId, ProviderAccountStatus>> AccountsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT provider_account_id, status FROM provider_accounts", connection, transaction);
        var accounts = new Dictionary<ProviderAccountId, ProviderAccountStatus>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            accounts[new ProviderAccountId(reader.GetString(0))] = Enum.Parse<ProviderAccountStatus>(reader.GetString(1));
        }

        return accounts;
    }

    // -----------------------------------------------------------------------
    // The controller's recorded decisions of the week, read back exactly as recorded
    // -----------------------------------------------------------------------

    private static async Task<IReadOnlyList<RecordedControllerDecision>> DecisionsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Head head,
        CancellationToken cancellationToken)
    {
        const string InWeek = "d.decided_at >= @start AND d.decided_at < @end AND d.decided_at <= @as_of";

        var readings = new Dictionary<Guid, List<RecordedDecisionReading>>();
        await using (var command = new NpgsqlCommand(
            $"""
            SELECT r.operation_id, r.scope_kind,
                   r.amount_case, r.amount, r.amount_currency, r.amount_recorded_in, r.amount_looked_for,
                   r.spend_case, r.spend_amount, r.spend_unit, r.spend_unmeasured_reason, r.spend_unmeasured_detail,
                   r.utilisation_case, r.utilisation_amount, r.utilisation_unit, r.utilisation_unmeasured_reason,
                   r.utilisation_unmeasured_detail, r.threshold, r.action, r.reason
            FROM admission_decision_readings r
            JOIN admission_decisions d ON d.operation_id = r.operation_id
            WHERE {InWeek}
            ORDER BY r.operation_id, r.scope_kind
            """,
            connection,
            transaction))
        {
            Week(command, head);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var operation = reader.GetGuid(0);
                if (!readings.TryGetValue(operation, out var of))
                {
                    readings[operation] = of = [];
                }

                of.Add(new RecordedDecisionReading
                {
                    Scope = Enum.Parse<GoverningScope>(reader.GetString(1)),
                    Amount = reader.GetString(2) switch
                    {
                        "Recorded" => RecordedAmount.Of(new Money(reader.GetDecimal(3), reader.GetString(4)), reader.GetString(5)),
                        "NotRecorded" => RecordedAmount.Missing(reader.GetString(6)),
                        var other => throw new InvalidOperationException($"The record holds a recorded-amount shape it does not admit: {other}."),
                    },
                    BookedSpend = MeasurementColumns.Read(reader, 7),
                    Utilisation = MeasurementColumns.Read(reader, 12),
                    Threshold = reader.IsDBNull(17) ? null : (BudgetThreshold)reader.GetInt32(17),
                    Action = Enum.Parse<ControllerAction>(reader.GetString(18)),
                    Reason = reader.IsDBNull(19) ? null : Enum.Parse<RefusalReason>(reader.GetString(19)),
                });
            }
        }

        var candidates = new Dictionary<Guid, List<RecordedDecisionCandidate>>();
        await using (var command = new NpgsqlCommand(
            $"""
            SELECT c.operation_id, c.route_id, c.configured_rating,
                   c.quality_case, c.quality_amount, c.quality_unit, c.quality_unmeasured_reason, c.quality_unmeasured_detail,
                   c.cost_case, c.cost_amount, c.cost_unit, c.cost_unmeasured_reason, c.cost_unmeasured_detail,
                   c.observations
            FROM admission_decision_candidates c
            JOIN admission_decisions d ON d.operation_id = c.operation_id
            WHERE {InWeek}
            ORDER BY c.operation_id, c.position
            """,
            connection,
            transaction))
        {
            Week(command, head);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var operation = reader.GetGuid(0);
                if (!candidates.TryGetValue(operation, out var of))
                {
                    candidates[operation] = of = [];
                }

                of.Add(new RecordedDecisionCandidate
                {
                    Route = new RouteId(reader.GetGuid(1)),
                    ConfiguredRating = new QualityRating(reader.GetInt32(2)),
                    ObservedQuality = MeasurementColumns.Read(reader, 3),
                    ObservedCost = MeasurementColumns.Read(reader, 8),
                    Observations = MeasurementQuantity.Count(reader.GetInt64(13), "observations"),
                });
            }
        }

        var decisions = new List<RecordedControllerDecision>();
        await using (var command = new NpgsqlCommand(
            $"""
            SELECT d.operation_id, c.channel_id, d.decided_at, d.booking_month, d.action, d.basis, d.basis_statement,
                   d.task_class, d.tier_statement, d.company_basis_statement, d.reservation_statement, d.observations_ranked_on
            FROM admission_decisions d
            JOIN agent_costs c ON c.operation_id = d.operation_id
            WHERE {InWeek}
            ORDER BY d.decided_at, d.operation_id
            """,
            connection,
            transaction))
        {
            Week(command, head);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var operation = reader.GetGuid(0);
                decisions.Add(new RecordedControllerDecision
                {
                    Operation = new OperationId(operation),
                    Channel = new ChannelId(reader.GetGuid(1)),
                    DecidedAt = reader.GetFieldValue<DateTimeOffset>(2),
                    BookingMonth = reader.GetFieldValue<DateOnly>(3),
                    Action = Enum.Parse<ControllerAction>(reader.GetString(4)),
                    Basis = Enum.Parse<SelectionBasis>(reader.GetString(5)),
                    BasisStatement = reader.GetString(6),
                    TaskClass = reader.IsDBNull(7) ? null : Enum.Parse<TaskClass>(reader.GetString(7)),
                    TierStatement = reader.GetString(8),
                    CompanyBasisStatement = reader.GetString(9),
                    ReservationStatement = reader.GetString(10),
                    ObservationsRankedOn = MeasurementQuantity.Count(reader.GetInt64(11), "observations ranked on"),
                    Readings = readings.TryGetValue(operation, out var r) ? r : [],
                    Candidates = candidates.TryGetValue(operation, out var c) ? c : [],
                });
            }
        }

        return decisions;
    }

    // -----------------------------------------------------------------------
    // Held work, failures, the register and the platform-policy records
    // -----------------------------------------------------------------------

    private static async Task<IReadOnlyList<HeldOutcomeRecord>> HeldAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        // Every held operation booked at or before the instant: nothing re-admits one, so none leaves the list.
        await using var command = new NpgsqlCommand(
            """
            SELECT c.operation_id, c.channel_id, c.item_id, c.occurred_at,
                   h.reason, h.escalates_at, h.hold_timeout, h.escalates_to_owner, COALESCE(c.failure_reason, '')
            FROM agent_costs c
            LEFT JOIN admission_held_outcomes h ON h.operation_id = c.operation_id
            WHERE c.outcome = 'Held' AND c.occurred_at <= @as_of
            ORDER BY c.occurred_at, c.operation_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("as_of", asOf);

        var held = new List<HeldOutcomeRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            held.Add(new HeldOutcomeRecord
            {
                Operation = new OperationId(reader.GetGuid(0)),
                Channel = new ChannelId(reader.GetGuid(1)),
                Item = new ItemId(reader.GetGuid(2)),
                HeldAt = reader.GetFieldValue<DateTimeOffset>(3),
                Reason = reader.IsDBNull(4) ? null : Enum.Parse<RefusalReason>(reader.GetString(4)),
                EscalatesAt = reader.IsDBNull(5) ? null : reader.GetFieldValue<DateTimeOffset>(5),
                HoldTimeout = reader.IsDBNull(6) ? null : reader.GetFieldValue<TimeSpan>(6),
                EscalatesToOwner = reader.IsDBNull(7) ? null : reader.GetBoolean(7),
                RecordedFailureReason = reader.GetString(8),
            });
        }

        return held;
    }

    private static async Task<IReadOnlyList<ChannelCount>> FailedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        Head head,
        IReadOnlyList<RegisteredChannel> register,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT channel_id, count(*)
            FROM agent_costs
            WHERE outcome = 'Failed' AND occurred_at >= @start AND occurred_at < @end AND occurred_at <= @as_of
            GROUP BY channel_id
            """,
            connection,
            transaction);
        Week(command, head);

        var counts = new Dictionary<ChannelId, long>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                counts[new ChannelId(reader.GetGuid(0))] = reader.GetInt64(1);
            }
        }

        return register.Select(r => new ChannelCount(r.Channel, counts.TryGetValue(r.Channel, out var n) ? n : 0)).ToArray();
    }

    /// <summary>
    /// The latest production of each item at or before the instant (the production change, decision D-017 of its
    /// design), in one statement: its stage outcomes, its operations and their booked cost, its cap and open
    /// reservations, and the rendered file's measured runtime. A store the ninth resource was never applied to reads
    /// none, and the composer composes nothing.
    /// </summary>
    private static async Task<IReadOnlyList<ProducedItemReading>?> ProducedItemsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        await using (var present = new NpgsqlCommand("SELECT to_regclass('production_versions') IS NOT NULL", connection, transaction))
        {
            if (await present.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
            {
                return null;
            }
        }

        await using var command = new NpgsqlCommand(
            """
            WITH latest AS (
                SELECT DISTINCT ON (v.item_id) v.item_id, v.item_version, v.mode, v.designation, v.recorded_at
                FROM production_versions v
                WHERE v.recorded_at <= @as_of
                ORDER BY v.item_id, v.item_version DESC
            )
            SELECT l.item_id, l.item_version, l.mode, l.designation, l.recorded_at,
                   (SELECT COALESCE(json_agg(json_build_object('stage', e.stage, 'outcome', e.outcome, 'summary', e.summary,
                                                              'at', e.recorded_at) ORDER BY e.recorded_at), '[]')
                    FROM dossier_stage_evidence e
                    WHERE e.item_id = l.item_id AND e.item_version = l.item_version AND e.recorded_at <= @as_of)::text,
                   (SELECT count(*) FROM agent_costs a WHERE a.item_id = l.item_id AND a.occurred_at <= @as_of),
                   (SELECT count(*) FROM agent_costs a WHERE a.item_id = l.item_id AND a.occurred_at <= @as_of AND a.cost_stated IS NOT TRUE),
                   (SELECT COALESCE(sum(a.computed_cost), 0) FROM agent_costs a WHERE a.item_id = l.item_id AND a.occurred_at <= @as_of),
                   c.amount, c.currency, c.source,
                   (SELECT COALESCE(sum(r.worst_case_amount), 0) FROM admission_reservations r
                    WHERE r.item_id = l.item_id AND r.recorded_at <= @as_of
                      AND NOT EXISTS (SELECT 1 FROM agent_costs a WHERE a.operation_id = r.operation_id AND a.occurred_at <= @as_of)),
                   (SELECT count(*) FROM admission_reservations r
                    WHERE r.item_id = l.item_id AND r.recorded_at <= @as_of
                      AND NOT EXISTS (SELECT 1 FROM agent_costs a WHERE a.operation_id = r.operation_id AND a.occurred_at <= @as_of)),
                   (SELECT p.measured_duration_ms FROM production_artifacts p
                    WHERE p.item_id = l.item_id AND p.item_version = l.item_version AND p.role = 'RenderedVideo' AND p.recorded_at <= @as_of
                    ORDER BY p.recorded_at DESC LIMIT 1),
                   (SELECT p.relative_path FROM production_artifacts p
                    WHERE p.item_id = l.item_id AND p.item_version = l.item_version AND p.role = 'RenderedVideo' AND p.recorded_at <= @as_of
                    ORDER BY p.recorded_at DESC LIMIT 1)
            FROM latest l
            LEFT JOIN item_caps c ON c.item_id = l.item_id
            ORDER BY l.item_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("as_of", asOf);

        var items = new List<ProducedItemReading>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            var currency = reader.IsDBNull(10) ? "USD" : reader.GetString(10);
            using var stages = System.Text.Json.JsonDocument.Parse(reader.GetString(5));
            items.Add(new ProducedItemReading
            {
                Item = new ItemId(reader.GetGuid(0)),
                Version = new ItemVersion(reader.GetInt32(1)),
                Mode = reader.GetString(2),
                Designation = reader.GetString(3),
                OpenedAt = reader.GetFieldValue<DateTimeOffset>(4),
                Stages = stages.RootElement.EnumerateArray().Select(e => new MediaCompany.Domain.Production.ProducedStageOutcome(
                    Enum.Parse<MediaCompany.Domain.Production.ProductionStage>(e.GetProperty("stage").GetString()!),
                    Enum.Parse<MediaCompany.Domain.Work.StageOutcome>(e.GetProperty("outcome").GetString()!),
                    e.GetProperty("summary").GetString()!,
                    DateTimeOffset.Parse(e.GetProperty("at").GetString()!, System.Globalization.CultureInfo.InvariantCulture))).ToArray(),
                Operations = reader.GetInt64(6),
                UnstatedOperations = reader.GetInt64(7),
                Booked = new Money(reader.GetDecimal(8), currency),
                Cap = reader.IsDBNull(9) ? null : new Money(reader.GetDecimal(9), currency),
                CapSource = reader.IsDBNull(11) ? null : reader.GetString(11),
                OpenReservations = new Money(reader.GetDecimal(12), currency),
                OpenReservationCount = reader.GetInt64(13),
                MeasuredRuntime = reader.IsDBNull(14) ? null : TimeSpan.FromMilliseconds(reader.GetInt64(14)),
                RenderedPath = reader.IsDBNull(15) ? null : reader.GetString(15),
            });
        }

        return items;
    }

    private static async Task<IReadOnlyList<RegisterEntry>> RegisterEntriesAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT entry_id, statement, kind, status, owner, recorded_on, interim_ruling, decided_by, decided_on,
                   supersedes, entered_at
            FROM owner_decision_register
            WHERE entered_at <= @as_of
            ORDER BY entered_at, entry_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("as_of", asOf);

        var entries = new List<RegisterEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            entries.Add(new RegisterEntry
            {
                Identifier = reader.GetString(0),
                Statement = reader.GetString(1),
                Kind = Enum.Parse<RegisterEntryKind>(reader.GetString(2)),
                Status = Enum.Parse<RegisterEntryStatus>(reader.GetString(3)),
                Owner = reader.GetString(4),
                RecordedOn = reader.GetFieldValue<DateOnly>(5),
                InterimRuling = reader.IsDBNull(6) ? null : reader.GetString(6),
                DecidedBy = reader.IsDBNull(7) ? null : reader.GetString(7),
                DecidedOn = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateOnly>(8),
                Supersedes = reader.IsDBNull(9) ? null : reader.GetString(9),
                EnteredAt = reader.GetFieldValue<DateTimeOffset>(10),
            });
        }

        return entries;
    }

    private static async Task<IReadOnlyList<PolicyStatementRecord>> PolicyStatementsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT statement_id, platform, label, source_reference, effective_on, registered_at
            FROM platform_policy_statements
            WHERE registered_at <= @as_of
            ORDER BY statement_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("as_of", asOf);

        var statements = new List<PolicyStatementRecord>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            statements.Add(new PolicyStatementRecord
            {
                Identifier = reader.GetString(0),
                Platform = reader.GetString(1),
                Label = reader.GetString(2),
                SourceReference = reader.GetString(3),
                EffectiveOn = reader.IsDBNull(4) ? null : reader.GetFieldValue<DateOnly>(4),
                RegisteredAt = reader.GetFieldValue<DateTimeOffset>(5),
            });
        }

        return statements;
    }

    private static async Task<IReadOnlyList<ReverificationResult>> ReverificationsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateTimeOffset asOf,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT reverification_id, statement_id, verified_on, changed, recorded_at
            FROM platform_policy_reverifications
            WHERE recorded_at <= @as_of
            ORDER BY recorded_at, reverification_id
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("as_of", asOf);

        var results = new List<ReverificationResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            results.Add(new ReverificationResult
            {
                Id = reader.GetGuid(0),
                Statement = reader.GetString(1),
                VerifiedOn = reader.GetFieldValue<DateOnly>(2),
                Changed = reader.GetBoolean(3),
                RecordedAt = reader.GetFieldValue<DateTimeOffset>(4),
            });
        }

        return results;
    }

    /// <summary>The bounds as text, for a demonstration that states them.</summary>
    internal static string BoundsStatement =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"lock bound {LockBound.TotalSeconds:0} s, statement bound {StatementBound.TotalSeconds:0} s, inside the 30 s command timeout");
}
