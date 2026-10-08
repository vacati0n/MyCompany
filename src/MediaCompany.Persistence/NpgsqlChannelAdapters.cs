using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The record horizon's closure, the one discipline every reading that would be final shares (the
/// multi-channel change, decision D-006 of its design, over the delivered throughput reading's
/// closure).
///
/// The horizon is taken EXCLUSIVELY WITHOUT WAITING, which the datastore grants only when no audited
/// or metered transaction holds it shared, and is then raised to the later of the datastore's clock
/// and its prior value. Every exclusive acquisition of the horizon goes through here, and none waits,
/// so a transaction holding the horizon shared and waiting for the audit chain head can never wait
/// on one that waits for it.
/// </summary>
internal static class RecordHorizonClosure
{
    /// <summary>How many non-waiting attempts a reading makes before it stops trying to close.</summary>
    internal const int Attempts = 3;

    /// <summary>The pause between two attempts. No attempt waits on an in-flight transaction itself.</summary>
    internal static readonly TimeSpan RetryPause = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Takes the horizon exclusively and raises it, returning the raised horizon, or returns null
    /// where an audited or metered transaction is in flight. On null the transaction is aborted and
    /// the caller rolls it back.
    /// </summary>
    internal static async Task<DateTimeOffset?> TryCloseAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var hold = new NpgsqlCommand(
                "SELECT horizon FROM audit_record_horizon WHERE only_row FOR UPDATE NOWAIT",
                connection,
                transaction);
            await hold.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (PostgresException refused) when (refused.SqlState == PostgresErrorCodes.LockNotAvailable)
        {
            return null;
        }

        await using var raise = new NpgsqlCommand(
            """
            UPDATE audit_record_horizon
               SET horizon = GREATEST(clock_timestamp(), horizon)
             WHERE only_row
            RETURNING horizon
            """,
            connection,
            transaction);

        await using var raised = await raise.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await raised.ReadAsync(cancellationToken).ConfigureAwait(false);
        return raised.GetFieldValue<DateTimeOffset>(0);
    }
}

/// <summary>The item register: which channel an item is recorded against (the multi-channel change).</summary>
public sealed class NpgsqlItemRegister : IItemRegister
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlItemRegister(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<ChannelId?> RecordedChannelAsync(ItemId item, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand("SELECT channel_id FROM items WHERE item_id = @item_id");
        command.Parameters.AddWithValue("item_id", item.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is Guid channel ? new ChannelId(channel) : null;
    }
}

/// <summary>
/// The channel partition reader (decision D-003 of the multi-channel design).
///
/// Every aggregation is the datastore's, in its exact decimal type, and the company row of each is
/// computed IN THE SAME STATEMENT OVER THE SAME ROWS as the channel rows, by grouping sets, so every
/// additive company figure is the exact sum of its channel figures. Every channel identifier the
/// register holds and every one the rows carry gets a row, so nothing read is dropped from the sum.
/// Every figure comes back BARE; the measurement case is decided at the composing site.
/// </summary>
public sealed class NpgsqlChannelPartitionReader : IChannelPartitionReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlChannelPartitionReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<MonthClosure> CloseMonthAsync(DateOnly month, CancellationToken cancellationToken)
    {
        var (closure, _) = await ReadMonthAsync(month, static (_, _, _) => Task.FromResult(0), cancellationToken)
            .ConfigureAwait(false);
        return closure;
    }

    public async Task<OperationPartitionSummary> OperationsAsync(DateOnly month, CancellationToken cancellationToken)
    {
        RequireMonth(month);

        var (closure, read) = await ReadMonthAsync(
            month,
            async (connection, transaction, ct) =>
            {
                var register = await RegisterAsync(connection, transaction, ct).ConfigureAwait(false);
                var (channels, company, variance) = await OperationTotalsAsync(connection, transaction, month, ct).ConfigureAwait(false);
                return (register, channels, company, variance);
            },
            cancellationToken).ConfigureAwait(false);

        var registered = read.register.Select(r => r.Channel).ToHashSet();
        var rows = new List<OperationPartitionRow>();

        foreach (var channel in read.register)
        {
            rows.Add(new OperationPartitionRow(
                channel.Channel,
                InRegister: true,
                read.channels.TryGetValue(channel.Channel, out var totals) ? totals : EmptyOperations));
        }

        foreach (var (channel, totals) in read.channels.Where(c => !registered.Contains(c.Key)).OrderBy(c => c.Key.Value))
        {
            rows.Add(new OperationPartitionRow(channel, InRegister: false, totals));
        }

        return new OperationPartitionSummary(closure, read.register, rows, read.company, read.variance);
    }

    public async Task<DossierPartitionSummary> DossiersAsync(DateOnly month, CancellationToken cancellationToken)
    {
        RequireMonth(month);
        var (start, end) = Bounds(month);

        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);

        var register = await RegisterAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        var earliest = await EarliestEntryAsync(connection, transaction, cancellationToken).ConfigureAwait(false);

        await using var command = new NpgsqlCommand(
            """
            WITH recorded AS (
                SELECT i.channel_id, 'dossier' AS kind
                FROM item_dossiers d JOIN items i ON i.item_id = d.item_id
                WHERE d.opened_at >= @start AND d.opened_at < @end
                UNION ALL
                SELECT i.channel_id, 'stage'
                FROM dossier_stage_evidence e JOIN items i ON i.item_id = e.item_id
                WHERE e.recorded_at >= @start AND e.recorded_at < @end
                UNION ALL
                SELECT i.channel_id, 'supply'
                FROM dossier_supply_audit_entries s JOIN items i ON i.item_id = s.item_id
                WHERE s.audited_at >= @start AND s.audited_at < @end
                UNION ALL
                SELECT i.channel_id, 'determination'
                FROM dossier_determinations r JOIN items i ON i.item_id = r.item_id
                WHERE r.resolved_at >= @start AND r.resolved_at < @end
            )
            SELECT channel_id,
                   GROUPING(channel_id)                          AS is_company,
                   count(*) FILTER (WHERE kind = 'dossier')       AS dossiers,
                   count(*) FILTER (WHERE kind = 'stage')         AS stages,
                   count(*) FILTER (WHERE kind = 'supply')        AS supply,
                   count(*) FILTER (WHERE kind = 'determination') AS determinations
            FROM recorded
            GROUP BY GROUPING SETS ((channel_id), ())
            """,
            connection,
            transaction);
        command.Parameters.AddWithValue("start", start);
        command.Parameters.AddWithValue("end", end);

        var channels = new Dictionary<ChannelId, DossierTotals>();
        var company = new DossierTotals(0, 0, 0, 0);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var totals = new DossierTotals(reader.GetInt64(2), reader.GetInt64(3), reader.GetInt64(4), reader.GetInt64(5));
                if (reader.GetInt32(1) == 1)
                {
                    company = totals;
                }
                else
                {
                    channels[new ChannelId(reader.GetGuid(0))] = totals;
                }
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var registered = register.Select(r => r.Channel).ToHashSet();
        var rows = register
            .Select(r => new DossierPartitionRow(
                r.Channel, InRegister: true,
                channels.TryGetValue(r.Channel, out var totals) ? totals : new DossierTotals(0, 0, 0, 0)))
            .Concat(channels
                .Where(c => !registered.Contains(c.Key))
                .OrderBy(c => c.Key.Value)
                .Select(c => new DossierPartitionRow(c.Key, InRegister: false, c.Value)))
            .ToArray();

        return new DossierPartitionSummary(month, earliest, register, rows, company);
    }

    public async Task<ThroughputPartitionSummary> ThroughputAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= RecordHorizonClosure.Attempts; attempt++)
        {
            var summary = await TryThroughputAsync(periodStart, periodEnd, cancellationToken).ConfigureAwait(false);
            if (summary is not null)
            {
                return summary;
            }

            if (attempt < RecordHorizonClosure.Attempts)
            {
                await Task.Delay(RecordHorizonClosure.RetryPause, cancellationToken).ConfigureAwait(false);
            }
        }

        // No quiet instant was found, so no count was taken, exactly as the delivered reading.
        var unread = new ThroughputSummary(periodStart, periodEnd, EarliestEntry: null, Quiet: false, Horizon: null, 0, 0, 0, 0, 0, 0, 0, 0);
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        var register = await RegisterAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new ThroughputPartitionSummary(
            register,
            register.Select(r => new ThroughputPartitionRow(r.Channel, InRegister: true, unread)).ToArray(),
            Unattributed: null,
            unread);
    }

    public async Task<BudgetPartitionSummary> BudgetsAsync(DateOnly month, Money ceiling, CancellationToken cancellationToken)
    {
        RequireMonth(month);

        var (closure, read) = await ReadMonthAsync(
            month,
            async (connection, transaction, ct) =>
            {
                var channels = await ChannelBudgetsAsync(connection, transaction, month, ct).ConfigureAwait(false);
                var company = await CompanyCeilingAsync(connection, transaction, month, ceiling, ct).ConfigureAwait(false);
                return (channels, company);
            },
            cancellationToken).ConfigureAwait(false);

        return new BudgetPartitionSummary(
            closure, read.channels, read.company.Operations, read.company.Cost, read.company.Percent, ceiling);
    }

    // -----------------------------------------------------------------------
    // The month closure every month reading over the operation record takes
    // -----------------------------------------------------------------------

    private delegate Task<T> MonthRead<T>(NpgsqlConnection connection, NpgsqlTransaction transaction, CancellationToken cancellationToken);

    /// <summary>
    /// Closes the month and reads it. The closure is attempted without waiting; when it is granted
    /// the horizon is raised and the read is taken while the horizon is held exclusively, so no
    /// operation can be stamped meanwhile. When every attempt meets an in-flight transaction, the
    /// horizon is read AS STORED and the read is taken in one snapshot with it, which is still
    /// sound: a horizon at or after the month's end was set while no writer was in flight, so every
    /// operation of that month committed before it and is in the snapshot.
    /// </summary>
    private async Task<(MonthClosure Closure, T Read)> ReadMonthAsync<T>(
        DateOnly month,
        MonthRead<T> read,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; attempt <= RecordHorizonClosure.Attempts; attempt++)
        {
            await using (var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false))
            await using (var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
            {
                var horizon = await RecordHorizonClosure.TryCloseAsync(connection, transaction, cancellationToken)
                    .ConfigureAwait(false);

                if (horizon is { } raised)
                {
                    var earliest = await EarliestEntryAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
                    var result = await read(connection, transaction, cancellationToken).ConfigureAwait(false);
                    await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                    return (new MonthClosure(month, ClosedByThisRead: true, raised, earliest), result);
                }

                await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            }

            if (attempt < RecordHorizonClosure.Attempts)
            {
                await Task.Delay(RecordHorizonClosure.RetryPause, cancellationToken).ConfigureAwait(false);
            }
        }

        await using var fallback = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var snapshot = await fallback
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);

        DateTimeOffset stored;
        await using (var command = new NpgsqlCommand("SELECT horizon FROM audit_record_horizon WHERE only_row", fallback, snapshot))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                throw new InvalidOperationException("The record horizon is missing; the fifth schema resource creates it.");
            }

            stored = reader.GetFieldValue<DateTimeOffset>(0);
        }

        var first = await EarliestEntryAsync(fallback, snapshot, cancellationToken).ConfigureAwait(false);
        var value = await read(fallback, snapshot, cancellationToken).ConfigureAwait(false);
        await snapshot.CommitAsync(cancellationToken).ConfigureAwait(false);

        return (new MonthClosure(month, ClosedByThisRead: false, stored, first), value);
    }

    // -----------------------------------------------------------------------
    // The statements
    // -----------------------------------------------------------------------

    private static readonly OperationTotals EmptyOperations =
        new(0, Money.Zero(), false, 0, Money.Zero(), new Dictionary<CapabilityClass, Money>(), 0, 0, 0);

    private static async Task<IReadOnlyList<RegisteredChannel>> RegisterAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT channel_id, company_id FROM channels ORDER BY channel_id", connection, transaction);

        var register = new List<RegisteredChannel>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            register.Add(new RegisteredChannel(new ChannelId(reader.GetGuid(0)), new CompanyId(reader.GetGuid(1))));
        }

        return register;
    }

    private static async Task<DateTimeOffset?> EarliestEntryAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand("SELECT min(occurred_at) FROM audit_entries", connection, transaction);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
        return reader.IsDBNull(0) ? null : reader.GetFieldValue<DateTimeOffset>(0);
    }

    private static async Task<(Dictionary<ChannelId, OperationTotals> Channels, OperationTotals Company, Money Variance)> OperationTotalsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        // The capability split first, by the same grouping, so each partition carries its own.
        var byCapability = new Dictionary<ChannelId, Dictionary<CapabilityClass, Money>>();
        var companyByCapability = new Dictionary<CapabilityClass, Money>();

        await using (var split = new NpgsqlCommand(
            """
            SELECT channel_id, GROUPING(channel_id) AS is_company, capability_class, sum(computed_cost) AS total_cost
            FROM agent_costs
            WHERE period = @month
            GROUP BY GROUPING SETS ((channel_id, capability_class), (capability_class))
            """,
            connection,
            transaction))
        {
            split.Parameters.Add("month", NpgsqlDbType.Date).Value = month;

            await using var reader = await split.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var capability = Enum.Parse<CapabilityClass>(reader.GetString(2));
                var cost = new Money(reader.GetDecimal(3));

                if (reader.GetInt32(1) == 1)
                {
                    companyByCapability[capability] = cost;
                    continue;
                }

                var channel = new ChannelId(reader.GetGuid(0));
                if (!byCapability.TryGetValue(channel, out var of))
                {
                    byCapability[channel] = of = [];
                }

                of[capability] = cost;
            }
        }

        var channels = new Dictionary<ChannelId, OperationTotals>();
        var company = EmptyOperations;

        await using var command = new NpgsqlCommand(
            """
            SELECT channel_id,
                   GROUPING(channel_id)                                                AS is_company,
                   count(*)                                                            AS operations,
                   COALESCE(sum(computed_cost), 0)                                     AS total_cost,
                   COALESCE(bool_or(cost_basis = 'Estimate'), false)                   AS contains_estimates,
                   count(*) FILTER (WHERE deterministic_task IS NOT NULL)              AS deterministic_operations,
                   COALESCE(sum(computed_cost) FILTER (WHERE deterministic_task IS NOT NULL), 0) AS deterministic_cost,
                   count(*) FILTER (WHERE reasoning_tier_served IS NOT NULL)           AS served_tier_records,
                   count(*) FILTER (WHERE reasoning_tier_requested IS NOT NULL
                                      AND reasoning_tier_served IS NOT NULL)           AS carrying_both,
                   count(*) FILTER (WHERE reasoning_tier_requested IS NOT NULL
                                      AND reasoning_tier_served = reasoning_tier_requested) AS agreeing,
                   COALESCE(sum(computed_cost), 0) - @envelope                         AS variance_against_envelope
            FROM agent_costs
            WHERE period = @month
            GROUP BY GROUPING SETS ((channel_id), ())
            """,
            connection,
            transaction);
        command.Parameters.Add("month", NpgsqlDbType.Date).Value = month;
        command.Parameters.Add("envelope", NpgsqlDbType.Numeric).Value = ApprovedEnvelope.MonthlyTotal.Amount;

        // With no row in the month the company's variance is the whole envelope unspent, subtracted
        // by the datastore on the next read; here there is nothing to aggregate.
        var variance = new Money(-ApprovedEnvelope.MonthlyTotal.Amount);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var isCompany = reader.GetInt32(1) == 1;
                if (isCompany)
                {
                    variance = new Money(reader.GetDecimal(10));
                }

                ChannelId? channel = isCompany ? null : new ChannelId(reader.GetGuid(0));

                var totals = new OperationTotals(
                    reader.GetInt64(2),
                    new Money(reader.GetDecimal(3)),
                    reader.GetBoolean(4),
                    reader.GetInt64(5),
                    new Money(reader.GetDecimal(6)),
                    channel is { } of && byCapability.TryGetValue(of, out var split)
                        ? split
                        : isCompany ? companyByCapability : new Dictionary<CapabilityClass, Money>(),
                    reader.GetInt64(7),
                    reader.GetInt64(8),
                    reader.GetInt64(9));

                if (channel is { } id)
                {
                    channels[id] = totals;
                }
                else
                {
                    company = totals;
                }
            }
        }

        return (channels, company, variance);
    }

    private async Task<ThroughputPartitionSummary?> TryThroughputAsync(
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        var horizon = await RecordHorizonClosure.TryCloseAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        if (horizon is not { } raised)
        {
            await transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        var register = await RegisterAsync(connection, transaction, cancellationToken).ConfigureAwait(false);
        var earliest = await EarliestEntryAsync(connection, transaction, cancellationToken).ConfigureAwait(false);

        // Each counted entry reaches its channel through the work unit its declared subject names
        // and the unit's own channel reference. An entry whose subject names no recorded unit is
        // counted under the unattributed partition, and the company row counts every entry.
        await using var command = new NpgsqlCommand(
            """
            WITH counted AS (
                SELECT a.action, a.occurred_at, j.channel_id, (j.job_id IS NULL) AS unattributed
                FROM audit_entries a
                LEFT JOIN jobs j ON j.job_id = (substring(a.subject FROM @pattern))::uuid
                WHERE a.action = ANY(@actions) AND a.occurred_at < @period_end
            )
            SELECT channel_id,
                GROUPING(channel_id)                                                      AS is_company,
                count(*) FILTER (WHERE action = @enqueued)                                AS enqueued_to_close,
                count(*) FILTER (WHERE action = @succeeded)                               AS succeeded_to_close,
                count(*) FILTER (WHERE action = @retried)                                 AS retried_to_close,
                count(*) FILTER (WHERE action = @claimed)                                 AS claimed_to_close,
                count(*) FILTER (WHERE action = @claimed   AND occurred_at >= @period_start) AS claimed_in_period,
                count(*) FILTER (WHERE action = @retried   AND occurred_at >= @period_start) AS retried_in_period,
                count(*) FILTER (WHERE action = @escalated AND occurred_at >= @period_start) AS escalated_in_period,
                count(*) FILTER (WHERE action = @completed AND occurred_at >= @period_start) AS completed_in_period
            FROM counted
            GROUP BY GROUPING SETS ((channel_id), ())
            """,
            connection,
            transaction);

        command.Parameters.AddWithValue("pattern", LifecycleActions.UnitSubjectPattern);
        command.Parameters.AddWithValue("enqueued", LifecycleActions.Enqueued);
        command.Parameters.AddWithValue("succeeded", LifecycleActions.StageSucceeded);
        command.Parameters.AddWithValue("retried", LifecycleActions.StageRetried);
        command.Parameters.AddWithValue("claimed", LifecycleActions.Claimed);
        command.Parameters.AddWithValue("escalated", LifecycleActions.StageEscalated);
        command.Parameters.AddWithValue("completed", LifecycleActions.Completed);
        command.Parameters.AddWithValue("actions", LifecycleActions.All.ToArray());
        command.Parameters.AddWithValue("period_start", periodStart);
        command.Parameters.AddWithValue("period_end", periodEnd);

        ThroughputSummary Summary(NpgsqlDataReader r) => new(
            periodStart, periodEnd, earliest, Quiet: true, Horizon: raised,
            r.GetInt64(2), r.GetInt64(3), r.GetInt64(4), r.GetInt64(5),
            r.GetInt64(6), r.GetInt64(7), r.GetInt64(8), r.GetInt64(9));

        var channels = new Dictionary<ChannelId, ThroughputSummary>();
        ThroughputSummary? unattributed = null;
        var company = new ThroughputSummary(periodStart, periodEnd, earliest, true, raised, 0, 0, 0, 0, 0, 0, 0, 0);

        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (reader.GetInt32(1) == 1)
                {
                    company = Summary(reader);
                }
                else if (reader.IsDBNull(0))
                {
                    unattributed = Summary(reader);
                }
                else
                {
                    channels[new ChannelId(reader.GetGuid(0))] = Summary(reader);
                }
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        var empty = new ThroughputSummary(periodStart, periodEnd, earliest, true, raised, 0, 0, 0, 0, 0, 0, 0, 0);
        var registered = register.Select(r => r.Channel).ToHashSet();

        var rows = register
            .Select(r => new ThroughputPartitionRow(
                r.Channel, InRegister: true, channels.TryGetValue(r.Channel, out var s) ? s : empty))
            .Concat(channels
                .Where(c => !registered.Contains(c.Key))
                .OrderBy(c => c.Key.Value)
                .Select(c => new ThroughputPartitionRow(c.Key, InRegister: false, c.Value)))
            .ToArray();

        return new ThroughputPartitionSummary(
            register,
            rows,
            unattributed is null ? null : new ThroughputPartitionRow(null, InRegister: false, unattributed),
            company);
    }

    private static async Task<IReadOnlyList<ChannelBudgetRow>> ChannelBudgetsAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        // Every registered channel, its operations and recorded cost in the month, and its budget
        // where the budget register holds one. The utilisation is the delivered function's, and only
        // where a budget exists: no percentage is computed against an amount nobody recorded.
        await using var command = new NpgsqlCommand(
            """
            SELECT c.channel_id,
                   (SELECT count(*) FROM agent_costs a WHERE a.channel_id = c.channel_id AND a.period = @month)  AS operations,
                   (SELECT COALESCE(sum(a.computed_cost), 0) FROM agent_costs a
                     WHERE a.channel_id = c.channel_id AND a.period = @month)                                   AS utilised,
                   b.budget_id,
                   b.amount,
                   u.utilization_percent
            FROM channels c
            LEFT JOIN budgets b
                   ON b.scope_kind = 'Channel' AND b.scope_id = c.channel_id AND b.period = @month
            LEFT JOIN LATERAL fn_budget_utilization(b.budget_id) u ON b.budget_id IS NOT NULL
            ORDER BY c.channel_id
            """,
            connection,
            transaction);
        command.Parameters.Add("month", NpgsqlDbType.Date).Value = month;

        var rows = new List<(ChannelId Channel, long Operations, Money Utilised, Guid? Budget, Money? Amount, decimal? Percent)>();
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add((
                    new ChannelId(reader.GetGuid(0)),
                    reader.GetInt64(1),
                    new Money(reader.GetDecimal(2)),
                    reader.IsDBNull(3) ? null : reader.GetGuid(3),
                    reader.IsDBNull(4) ? null : new Money(reader.GetDecimal(4)),
                    reader.IsDBNull(5) ? null : reader.GetDecimal(5)));
            }
        }

        var alerts = new Dictionary<Guid, Dictionary<int, long>>();
        await using (var recorded = new NpgsqlCommand(
            "SELECT budget_id, threshold, count(*) FROM budget_alerts WHERE period = @month GROUP BY budget_id, threshold",
            connection,
            transaction))
        {
            recorded.Parameters.Add("month", NpgsqlDbType.Date).Value = month;
            await using var reader = await recorded.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var budget = reader.GetGuid(0);
                if (!alerts.TryGetValue(budget, out var of))
                {
                    alerts[budget] = of = [];
                }

                of[reader.GetInt32(1)] = reader.GetInt64(2);
            }
        }

        return rows
            .Select(r => new ChannelBudgetRow(
                r.Channel,
                r.Operations,
                r.Utilised,
                r.Amount,
                r.Percent,
                r.Budget is { } id && alerts.TryGetValue(id, out var of)
                    ? of
                    : new Dictionary<int, long>()))
            .ToArray();
    }

    private static async Task<(long Operations, Money Cost, decimal? Percent)> CompanyCeilingAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        DateOnly month,
        Money ceiling,
        CancellationToken cancellationToken)
    {
        // Every operation row of the month, whichever channel it carries, against the ceiling, and
        // the division is the datastore's, in its exact decimal type.
        await using var command = new NpgsqlCommand(
            """
            SELECT count(*),
                   COALESCE(sum(computed_cost), 0),
                   CASE WHEN count(*) = 0 THEN NULL
                        ELSE ROUND(COALESCE(sum(computed_cost), 0) * 100.0 / @ceiling, 4)
                   END
            FROM agent_costs
            WHERE period = @month
            """,
            connection,
            transaction);
        command.Parameters.Add("month", NpgsqlDbType.Date).Value = month;
        command.Parameters.Add("ceiling", NpgsqlDbType.Numeric).Value = ceiling.Amount;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        await reader.ReadAsync(cancellationToken).ConfigureAwait(false);

        return (reader.GetInt64(0), new Money(reader.GetDecimal(1)), reader.IsDBNull(2) ? null : reader.GetDecimal(2));
    }

    private static (DateTimeOffset Start, DateTimeOffset End) Bounds(DateOnly month)
    {
        var start = new DateTimeOffset(month.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return (start, start.AddMonths(1));
    }

    private static void RequireMonth(DateOnly month)
    {
        if (month.Day != 1)
        {
            throw new ArgumentException("A month is named by its first day.", nameof(month));
        }
    }
}

/// <summary>
/// The approval queue reader (decision D-005 of the multi-channel design). READ MEMBERS ONLY, over
/// the gate-transition record and the approval record, through each item's channel; it reads no
/// configuration, and nothing here writes, presents, decides or transitions.
/// </summary>
public sealed class NpgsqlApprovalQueueReader : IApprovalQueueReader
{
    private const string AwaitingSql =
        """
        SELECT i.channel_id, t.item_id, t.item_version, t.occurred_at
        FROM (
            SELECT DISTINCT ON (item_id, item_version) item_id, item_version, to_state, occurred_at
            FROM gate_transitions
            ORDER BY item_id, item_version, occurred_at DESC
        ) t
        JOIN items i ON i.item_id = t.item_id
        WHERE t.to_state = 'AwaitingOwnerApproval'
          AND (@channel_id IS NULL OR i.channel_id = @channel_id)
        ORDER BY t.occurred_at, t.item_id, t.item_version
        """;

    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlApprovalQueueReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public Task<IReadOnlyList<AwaitingApproval>> AwaitingOwnerApprovalAsync(ChannelId channel, CancellationToken cancellationToken) =>
        AwaitingAsync(channel.Value, cancellationToken);

    public Task<IReadOnlyList<AwaitingApproval>> AwaitingOwnerApprovalAsync(CancellationToken cancellationToken) =>
        AwaitingAsync(null, cancellationToken);

    public async Task<IReadOnlyList<DecidedApproval>> DecidedAsync(
        ChannelId channel,
        DateOnly month,
        CancellationToken cancellationToken)
    {
        var start = new DateTimeOffset(month.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);

        await using var command = _dataSource.CreateCommand(
            """
            SELECT a.item_id, a.item_version, a.gate, a.approver, a.verdict, a.reason, a.presented_at, a.decided_at,
                   a.queued_at, a.rework_of,
                   p.item_id, p.item_version, p.gate, p.approver, p.verdict, p.reason, p.presented_at, p.decided_at,
                   p.queued_at, p.rework_of
            FROM approvals a
            JOIN items i ON i.item_id = a.item_id
            LEFT JOIN approvals p
                   ON p.item_id = a.item_id AND p.gate = a.gate AND p.approver = a.approver
                  AND p.presented_at = a.rework_of
            WHERE i.channel_id = @channel_id
              AND a.decided_at >= @start AND a.decided_at < @end
            ORDER BY a.decided_at, a.item_id, a.item_version
            """);
        command.Parameters.AddWithValue("channel_id", channel.Value);
        command.Parameters.AddWithValue("start", start);
        command.Parameters.AddWithValue("end", start.AddMonths(1));

        var decided = new List<DecidedApproval>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            decided.Add(new DecidedApproval(
                ReadApproval(reader, 0),
                reader.IsDBNull(10) ? null : ReadApproval(reader, 10)));
        }

        return decided;
    }

    private async Task<IReadOnlyList<AwaitingApproval>> AwaitingAsync(Guid? channel, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(AwaitingSql);
        command.Parameters.Add("channel_id", NpgsqlDbType.Uuid).Value = (object?)channel ?? DBNull.Value;

        var awaiting = new List<AwaitingApproval>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            awaiting.Add(new AwaitingApproval(
                new ChannelId(reader.GetGuid(0)),
                new ItemId(reader.GetGuid(1)),
                new ItemVersion(reader.GetInt32(2)),
                reader.GetFieldValue<DateTimeOffset>(3)));
        }

        return awaiting;
    }

    private static Approval ReadApproval(NpgsqlDataReader reader, int at) => new(
        new ItemId(reader.GetGuid(at)),
        new ItemVersion(reader.GetInt32(at + 1)),
        reader.GetString(at + 2),
        Enum.Parse<WorkforceRole>(reader.GetString(at + 3)),
        Enum.Parse<ApprovalVerdict>(reader.GetString(at + 4)),
        reader.GetString(at + 5),
        reader.GetFieldValue<DateTimeOffset>(at + 6),
        reader.GetFieldValue<DateTimeOffset>(at + 7),

        // Null stays null: an absent mark resolves as unmeasured at the derivation site.
        reader.IsDBNull(at + 8) ? null : reader.GetFieldValue<DateTimeOffset>(at + 8),
        reader.IsDBNull(at + 9) ? null : reader.GetFieldValue<DateTimeOffset>(at + 9));
}
