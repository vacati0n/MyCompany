using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The operation meter (module M-005).
///
/// The insert reads the price rows in force at the operation's instant and stores the unit prices
/// alongside the price row reference, so the cost is computed by the GENERATED column from values
/// on the row itself. The consequence is the one decision D-006 and assumption A-006 ask for: the
/// arithmetic is exact, it is the datastore's, and it stays re-derivable after the price that
/// produced it has been superseded.
///
/// A member of the named deterministic set is written with no price reference and no unit prices,
/// and a table check refuses any other combination, so a non-zero cost cannot be attached to it.
/// </summary>
internal sealed class NpgsqlOperationRecorder : IOperationRecorder
{
    private const string InsertSql =
        """
        WITH price AS (
            SELECT
                -- The applied price reference is the input-unit row in force at the instant.
                -- `max` has no uuid form, and picking by aggregate would be arbitrary where two
                -- rows overlap, so the most recently effective one is taken explicitly.
                (array_agg(p.model_price_id ORDER BY p.valid_from DESC)
                    FILTER (WHERE p.unit_kind = 'InputUnit'))[1]                AS reference_id,
                COALESCE(MAX(p.unit_price) FILTER (WHERE p.unit_kind = 'InputUnit'), 0)  AS input_price,
                COALESCE(MAX(p.unit_price) FILTER (WHERE p.unit_kind = 'OutputUnit'), 0) AS output_price,
                COALESCE(MAX(p.unit_price) FILTER (WHERE p.unit_kind = 'CachedUnit'), 0) AS cached_price,
                COALESCE(MAX(p.currency), 'USD') AS currency
            FROM model_prices p
            WHERE @model_id IS NOT NULL
              AND p.model_id = @model_id
              AND p.valid_from <= @occurred_at
              AND (p.valid_to IS NULL OR p.valid_to > @occurred_at)
        )
        INSERT INTO agent_costs (
            operation_id, run_id, occurred_at, attempt,
            item_id, channel_id, department_id, agent_id, capability_class, period,
            route_id, model_id, deterministic_task,
            input_units, output_units, cached_units, other_units,
            applied_price_id, applied_input_price, applied_output_price, applied_cached_price, currency,
            cost_basis, duration_ms, outcome, failure_reason)
        SELECT
            @operation_id, @run_id, @occurred_at, @attempt,
            @item_id, @channel_id, @department_id, @agent_id, @capability_class, @period,
            @route_id, @model_id, @deterministic_task,
            @input_units, @output_units, @cached_units, @other_units,
            CASE WHEN @deterministic_task IS NULL THEN price.reference_id  ELSE NULL END,
            CASE WHEN @deterministic_task IS NULL THEN price.input_price   ELSE 0 END,
            CASE WHEN @deterministic_task IS NULL THEN price.output_price  ELSE 0 END,
            CASE WHEN @deterministic_task IS NULL THEN price.cached_price  ELSE 0 END,
            price.currency,
            @cost_basis, @duration_ms, @outcome, @failure_reason
        FROM price
        RETURNING computed_cost, currency, applied_price_id
        """;

    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlOperationRecorder(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<OperationRecord> RecordAsync(OperationDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        // The period follows the instant, not the offset the caller expressed it in. Two
        // operations a minute apart across a month boundary must not land in different periods
        // because their offsets differed.
        var utc = draft.OccurredAt.UtcDateTime;
        var period = new DateOnly(utc.Year, utc.Month, 1);

        await using var command = new NpgsqlCommand(InsertSql, _connection, _transaction);
        command.Parameters.AddWithValue("operation_id", draft.Id.Value);
        command.Parameters.AddWithValue("run_id", draft.Run.Value);
        command.Parameters.AddWithValue("occurred_at", draft.OccurredAt);
        command.Parameters.AddWithValue("attempt", draft.Attempt);
        command.Parameters.AddWithValue("item_id", draft.Attribution.Item.Value);
        command.Parameters.AddWithValue("channel_id", draft.Attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", draft.Attribution.Department.Value);
        command.Parameters.AddWithValue("agent_id", draft.Attribution.Agent.Value);
        command.Parameters.AddWithValue("capability_class", draft.Capability.ToString());
        command.Parameters.AddWithValue("period", NpgsqlDbType.Date, period);
        AddNullable(command, "route_id", NpgsqlDbType.Uuid, draft.Route?.Value);
        AddNullable(command, "model_id", NpgsqlDbType.Text, draft.Model?.Value);
        AddNullable(command, "deterministic_task", NpgsqlDbType.Text, draft.DeterministicTaskName);
        command.Parameters.AddWithValue("input_units", draft.Units.InputUnits);
        command.Parameters.AddWithValue("output_units", draft.Units.OutputUnits);
        command.Parameters.AddWithValue("cached_units", draft.Units.CachedUnits);
        command.Parameters.AddWithValue("other_units", draft.Units.OtherUnits);
        command.Parameters.AddWithValue("cost_basis", draft.CostBasis.ToString());
        command.Parameters.AddWithValue("duration_ms", (long)draft.Duration.TotalMilliseconds);
        command.Parameters.AddWithValue("outcome", draft.Outcome.ToString());
        AddNullable(command, "failure_reason", NpgsqlDbType.Text, draft.FailureReason);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                $"The operation row for {draft.Id} was not written; no price row was resolvable for the instant.");
        }

        var computedCost = reader.GetDecimal(0);
        var currency = reader.GetString(1);
        var appliedPriceId = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);

        return new OperationRecord
        {
            Id = draft.Id,
            Run = draft.Run,
            Attribution = draft.Attribution,
            Capability = draft.Capability,
            Route = draft.Route,
            Model = draft.Model,
            DeterministicTaskName = draft.DeterministicTaskName,
            Units = draft.Units,
            AppliedPrice = appliedPriceId is { } id ? new ModelPriceId(id) : null,
            ComputedCost = new Money(computedCost, currency),
            CostBasis = draft.CostBasis,
            Duration = draft.Duration,
            Outcome = draft.Outcome,
            OccurredAt = draft.OccurredAt,
            Attempt = draft.Attempt,
            FailureReason = draft.FailureReason,
        };
    }

    private static void AddNullable(NpgsqlCommand command, string name, NpgsqlDbType type, object? value)
    {
        var parameter = command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }
}
