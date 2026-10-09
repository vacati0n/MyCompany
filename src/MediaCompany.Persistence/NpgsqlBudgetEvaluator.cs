using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// Budget utilization and threshold alerts (module M-006).
///
/// The utilization is computed by <c>fn_budget_utilization</c> over the recorded operations, in
/// the datastore's exact decimal type. The alert row's primary key is the budget, the period and
/// the threshold, so <c>ON CONFLICT DO NOTHING</c> makes each crossing raise exactly one alert no
/// matter how often the evaluation runs (constraint C-018).
/// </summary>
internal sealed class NpgsqlBudgetEvaluator : IBudgetEvaluator
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlBudgetEvaluator(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<IReadOnlyList<BudgetAlert>> EvaluateAsync(
        Attribution attribution,
        DateOnly period,
        DateTimeOffset raisedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribution);

        // One statement: for every budget governing this attribution, compute utilization, expand
        // it to the thresholds it has reached, and insert the alerts that do not exist yet. What
        // comes back is exactly the set of crossings this evaluation raised.
        await using var command = new NpgsqlCommand(
            """
            WITH governing AS (
                SELECT * FROM fn_budgets_for(@channel_id, @department_id, @period)
            ),
            measured AS (
                SELECT g.budget_id, g.period, g.amount, g.currency, u.utilized, u.utilization_percent,
                       -- Whether the budget's scope holds a cost that is not stated in the month (the
                       -- AI-economics change, correction cycle): where it does, the utilisation is a partial
                       -- sum, so no threshold is read as reached from it and no alert presents it.
                       (SELECT count(*) FROM agent_costs c
                        WHERE c.period = g.period AND c.cost_stated IS NOT TRUE
                          AND ((g.scope_kind = 'Department' AND c.department_id = g.scope_id)
                            OR (g.scope_kind = 'Channel' AND c.channel_id = g.scope_id))) AS unstated
                FROM governing g
                CROSS JOIN LATERAL fn_budget_utilization(g.budget_id) u
            ),
            reached AS (
                SELECT m.*, t.threshold
                FROM measured m
                CROSS JOIN (VALUES (50), (75), (90), (100)) AS t(threshold)
                WHERE m.unstated = 0 AND m.utilization_percent >= t.threshold
            )
            INSERT INTO budget_alerts (budget_id, period, threshold, utilization, utilized, budget_amount, raised_at)
            SELECT budget_id, period, threshold, utilization_percent, utilized, amount, @raised_at
            FROM reached
            ON CONFLICT (budget_id, period, threshold) DO NOTHING
            RETURNING budget_id, period, threshold, utilization, utilized, budget_amount, raised_at
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("channel_id", attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", attribution.Department.Value);
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;
        command.Parameters.AddWithValue("raised_at", raisedAt);

        return await ReadAlertsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<BudgetAlert>> StoredAlertsAsync(
        Attribution attribution,
        DateOnly period,
        DateTimeOffset raisedAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribution);

        // The alerts the commit that booked an operation raised: those of the budgets governing its
        // attribution in its booked month, stamped with its booked instant, which is the instant every
        // alert of that evaluation carries (the second correction cycle).
        await using var command = new NpgsqlCommand(
            """
            SELECT a.budget_id, a.period, a.threshold, a.utilization, a.utilized, a.budget_amount, a.raised_at
            FROM budget_alerts a
            JOIN fn_budgets_for(@channel_id, @department_id, @period) g ON g.budget_id = a.budget_id
            WHERE a.period = @period AND a.raised_at = @raised_at
            ORDER BY a.budget_id, a.threshold
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("channel_id", attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", attribution.Department.Value);
        command.Parameters.Add("period", NpgsqlDbType.Date).Value = period;
        command.Parameters.AddWithValue("raised_at", raisedAt);

        return await ReadAlertsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<IReadOnlyList<BudgetAlert>> ReadAlertsAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        var alerts = new List<BudgetAlert>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            alerts.Add(new BudgetAlert(
                new BudgetId(reader.GetGuid(0)),
                reader.GetFieldValue<DateOnly>(1),
                reader.GetInt32(2),
                reader.GetDecimal(3),
                new Money(reader.GetDecimal(4)),
                new Money(reader.GetDecimal(5)),
                reader.GetFieldValue<DateTimeOffset>(6)));
        }

        return alerts;
    }
}
