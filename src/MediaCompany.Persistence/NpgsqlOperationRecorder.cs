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
///
/// THE DATASTORE BOOKS EVERY OPERATION (the multi-channel change, decision D-006 of its design).
/// The instant is stamped in the same statement as the later of the datastore's clock and the
/// record horizon, under a SHARED hold on the horizon kept to the end of this transaction, exactly
/// as the appender stamps an entry; the price rows are resolved at that instant, and the operation is
/// booked into that instant's month. The record returned carries the stored instant. A datastore
/// check takes the same hold and refuses an operation stamped below the horizon or booked into any
/// other month, so the rule binds every writer and a month the horizon has passed is final.
///
/// WHERE THE TRANSACTION HOLDS A RESERVATION (the AI-economics change, decision D-001 of its design) the
/// stamp is the RESERVED INSTANT, read from the transaction-local setting the admission ledger set, so
/// the operation is booked into the month it was admitted against; the shared hold the reservation took
/// keeps that instant at or above the horizon. Elsewhere the stamp is exactly as delivered. The row also
/// states whether its cost is stated: false where a consumed input, output or cached unit had no price
/// row in force at the booking instant, or any other unit was consumed, so a missing price stored as a
/// zero applied price never reads as a stated zero cost.
///
/// THE COST IN RANGE (the second correction cycle): the cost is computed in the statement, before the
/// insert, in unbounded precision; a cost the cost column cannot hold is refused, and the attempt is recorded
/// with zero applied prices, its cost not stated and the named reason <c>CostOutOfRange</c>, so an incurred
/// attempt is never lost to an unnamed overflow. Every unstated cost now carries its reason.
/// </summary>
internal sealed class NpgsqlOperationRecorder : IOperationRecorder
{
    private const string InsertSql =
        """
        WITH stamp AS (
            -- The datastore's instant, never the caller's: the instant this transaction reserved where
            -- it holds one, and otherwise the later of its clock and the record horizon, read under a
            -- shared hold on the horizon that lasts until this transaction ends.
            SELECT COALESCE(
                       NULLIF(current_setting('mediacompany.booking_instant', true), '')::timestamptz,
                       GREATEST(clock_timestamp(), h.horizon)) AS at
            FROM audit_record_horizon h
            WHERE h.only_row
            FOR SHARE
        ),
        -- The prices CAPTURED AT ADMISSION on this transaction, where it captured any (the correction
        -- cycle): the operation is costed at the prices the admission read, never at a later lookup in
        -- the editable register. Elsewhere the rows in force at the stamp are looked up, as delivered.
        captured AS (
            SELECT (c->>'id')::uuid AS model_price_id, c->>'kind' AS unit_kind, (c->>'price')::numeric AS unit_price,
                   c->>'currency' AS currency, NULL::timestamptz AS valid_from
            FROM jsonb_array_elements(
                     COALESCE(NULLIF(current_setting('mediacompany.admission_prices', true), '')::jsonb, '[]'::jsonb)) c
            WHERE c->>'model' = @model_id
        ),
        looked_up AS (
            SELECT p.model_price_id, p.unit_kind, p.unit_price, p.currency, p.valid_from
            FROM stamp
            JOIN model_prices p
              ON p.model_id = @model_id
             AND p.valid_from <= stamp.at
             AND (p.valid_to IS NULL OR p.valid_to > stamp.at)
            WHERE NULLIF(current_setting('mediacompany.admission_prices', true), '') IS NULL
        ),
        applicable AS (
            SELECT * FROM captured
            UNION ALL
            SELECT * FROM looked_up
        ),
        price AS (
            SELECT
                stamp.at                                                       AS at,
                -- The applied price reference is the input-unit row applied. `max` has no uuid form,
                -- and picking by aggregate would be arbitrary where two rows overlap, so the most
                -- recently effective one is taken explicitly.
                (array_agg(p.model_price_id ORDER BY p.valid_from DESC NULLS LAST)
                    FILTER (WHERE p.unit_kind = 'InputUnit'))[1]                AS reference_id,
                COALESCE(MAX(p.unit_price) FILTER (WHERE p.unit_kind = 'InputUnit'), 0)  AS input_price,
                COALESCE(MAX(p.unit_price) FILTER (WHERE p.unit_kind = 'OutputUnit'), 0) AS output_price,
                COALESCE(MAX(p.unit_price) FILTER (WHERE p.unit_kind = 'CachedUnit'), 0) AS cached_price,
                COALESCE(MAX(p.currency), 'USD') AS currency,
                COALESCE(bool_or(p.unit_kind = 'InputUnit'), false)  AS has_input,
                COALESCE(bool_or(p.unit_kind = 'OutputUnit'), false) AS has_output,
                COALESCE(bool_or(p.unit_kind = 'CachedUnit'), false) AS has_cached
            FROM stamp
            LEFT JOIN applicable p ON @model_id IS NOT NULL
            GROUP BY stamp.at
        ),
        -- THE COST IN RANGE (the second correction cycle): the units at the applied prices, rounded as the
        -- generated cost column rounds them, computed here in unbounded precision BEFORE the insert, so a
        -- cost the column cannot hold (ten billion or more) is refused rather than overflowing the insert.
        ranged AS (
            SELECT price.*,
                   round(@input_units * price.input_price + @output_units * price.output_price
                         + @cached_units * price.cached_price, 8) < 1e10 AS in_range
            FROM price
        ),
        -- Why the cost is not stated, or null where it is. A member of the deterministic set states zero; a
        -- row with no model states zero only where it consumed nothing; a priced row only where every consumed
        -- metered unit kind had a price row in force at the stamp, no other unit was consumed, and the cost is
        -- in range. An out-of-range cost is recorded with zero applied prices and its reason, never as a cost.
        verdict AS (
            SELECT ranged.*,
                   CASE
                       WHEN @deterministic_task IS NOT NULL THEN NULL
                       WHEN @model_id IS NULL THEN
                           CASE
                               WHEN @input_units > 0 OR @output_units > 0 OR @cached_units > 0 THEN 'PriceNotInForce'
                               WHEN @other_units > 0 THEN 'UnpricedUnitConsumed'
                               ELSE NULL
                           END
                       WHEN (@input_units > 0 AND NOT ranged.has_input)
                         OR (@output_units > 0 AND NOT ranged.has_output)
                         OR (@cached_units > 0 AND NOT ranged.has_cached) THEN 'PriceNotInForce'
                       WHEN @other_units > 0 THEN 'UnpricedUnitConsumed'
                       WHEN NOT ranged.in_range THEN 'CostOutOfRange'
                       ELSE NULL
                   END AS unstated_reason
            FROM ranged
        )
        INSERT INTO agent_costs (
            operation_id, run_id, occurred_at, attempt,
            item_id, channel_id, department_id, agent_id, capability_class, period,
            route_id, model_id, deterministic_task,
            input_units, output_units, cached_units, other_units,
            applied_price_id, applied_input_price, applied_output_price, applied_cached_price, currency,
            cost_basis, duration_ms, outcome, failure_reason,
            reasoning_tier_requested, reasoning_tier_served, cost_stated, cost_unstated_reason)
        SELECT
            @operation_id, @run_id, price.at, @attempt,
            @item_id, @channel_id, @department_id, @agent_id, @capability_class,
            (date_trunc('month', price.at AT TIME ZONE 'UTC'))::date,
            @route_id, @model_id, @deterministic_task,
            @input_units, @output_units, @cached_units, @other_units,
            CASE WHEN @deterministic_task IS NULL THEN price.reference_id  ELSE NULL END,
            CASE WHEN @deterministic_task IS NULL AND price.in_range THEN price.input_price  ELSE 0 END,
            CASE WHEN @deterministic_task IS NULL AND price.in_range THEN price.output_price ELSE 0 END,
            CASE WHEN @deterministic_task IS NULL AND price.in_range THEN price.cached_price ELSE 0 END,
            price.currency,
            @cost_basis, @duration_ms, @outcome, @failure_reason,
            @tier_requested, @tier_served,
            price.unstated_reason IS NULL,
            price.unstated_reason
        FROM verdict price
        -- One attempt is booked once (the correction cycle): a second write under the identifier minted
        -- before the provider call books nothing, and the writer is told the attempt is already recorded.
        ON CONFLICT (operation_id) DO NOTHING
        RETURNING computed_cost, currency, applied_price_id, occurred_at, cost_stated, cost_unstated_reason
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

        // Neither the instant nor the period is the caller's. Both are the datastore's, stamped and
        // booked in the statement below, so the draft's instant is not a parameter of the write.
        await using var command = new NpgsqlCommand(InsertSql, _connection, _transaction);
        command.Parameters.AddWithValue("operation_id", draft.Id.Value);
        command.Parameters.AddWithValue("run_id", draft.Run.Value);
        command.Parameters.AddWithValue("attempt", draft.Attempt);
        command.Parameters.AddWithValue("item_id", draft.Attribution.Item.Value);
        command.Parameters.AddWithValue("channel_id", draft.Attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", draft.Attribution.Department.Value);
        command.Parameters.AddWithValue("agent_id", draft.Attribution.Agent.Value);
        command.Parameters.AddWithValue("capability_class", draft.Capability.ToString());
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

        // The served value comes from the draft, which the resolution boundary populated from the
        // ADMITTING ROUTE. A null is the explicit absence marker and is stored as such; nothing
        // here falls back to the requested tier, because that would manufacture the very evidence
        // the column exists to measure.
        AddNullable(command, "tier_requested", NpgsqlDbType.Text, draft.ReasoningTierRequested?.ToString());
        AddNullable(command, "tier_served", NpgsqlDbType.Text, draft.ReasoningTierServed?.ToString());

        var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            await reader.DisposeAsync().ConfigureAwait(false);

            if (await StoredAsync(draft.Id, cancellationToken).ConfigureAwait(false) is { } recorded)
            {
                throw new OperationAlreadyRecordedException(recorded);
            }

            throw new InvalidOperationException(
                $"The operation row for {draft.Id} was not written: the record horizon the fifth schema resource "
                + "creates was not readable, so no instant could be stamped.");
        }

        await using var _ = reader;

        var computedCost = reader.GetDecimal(0);
        var currency = reader.GetString(1);
        var appliedPriceId = reader.IsDBNull(2) ? (Guid?)null : reader.GetGuid(2);
        var stored = reader.GetFieldValue<DateTimeOffset>(3);
        var costStated = reader.GetBoolean(4);
        var unstatedReason = reader.IsDBNull(5) ? (CostUnstatedReason?)null : Enum.Parse<CostUnstatedReason>(reader.GetString(5));

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
            // The STORED instant, which decides the month the operation is booked in.
            OccurredAt = stored,
            Attempt = draft.Attempt,
            FailureReason = draft.FailureReason,
            ReasoningTierRequested = draft.ReasoningTierRequested,
            ReasoningTierServed = draft.ReasoningTierServed,
            CostStated = costStated,
            CostUnstatedReason = unstatedReason,
        };
    }

    /// <summary>The operation stored under an identifier, as the datastore holds it, or null.</summary>
    private async Task<OperationRecord?> StoredAsync(OperationId id, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            SELECT run_id, item_id, channel_id, department_id, agent_id, capability_class, route_id, model_id,
                   deterministic_task, input_units, output_units, cached_units, other_units, applied_price_id,
                   computed_cost, currency, cost_basis, duration_ms, outcome, occurred_at, attempt, failure_reason,
                   reasoning_tier_requested, reasoning_tier_served, cost_stated, cost_unstated_reason
            FROM agent_costs WHERE operation_id = @operation_id
            """,
            _connection,
            _transaction);
        command.Parameters.AddWithValue("operation_id", id.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new OperationRecord
        {
            Id = id,
            Run = new RunId(reader.GetGuid(0)),
            Attribution = new MediaCompany.Domain.Capabilities.Attribution(
                new ItemId(reader.GetGuid(1)), new ChannelId(reader.GetGuid(2)),
                new DepartmentId(reader.GetGuid(3)), new AgentId(reader.GetGuid(4))),
            Capability = Enum.Parse<MediaCompany.Domain.Capabilities.CapabilityClass>(reader.GetString(5)),
            Route = reader.IsDBNull(6) ? null : new RouteId(reader.GetGuid(6)),
            Model = reader.IsDBNull(7) ? null : new ModelId(reader.GetString(7)),
            DeterministicTaskName = reader.IsDBNull(8) ? null : reader.GetString(8),
            Units = new UnitCounts(reader.GetInt64(9), reader.GetInt64(10), reader.GetInt64(11), reader.GetInt64(12)),
            AppliedPrice = reader.IsDBNull(13) ? null : new ModelPriceId(reader.GetGuid(13)),
            ComputedCost = new Money(reader.GetDecimal(14), reader.GetString(15)),
            CostBasis = Enum.Parse<CostBasis>(reader.GetString(16)),
            Duration = TimeSpan.FromMilliseconds(reader.GetInt64(17)),
            Outcome = Enum.Parse<OperationOutcome>(reader.GetString(18)),
            OccurredAt = reader.GetFieldValue<DateTimeOffset>(19),
            Attempt = reader.GetInt32(20),
            FailureReason = reader.IsDBNull(21) ? null : reader.GetString(21),
            ReasoningTierRequested = reader.IsDBNull(22) ? null : Enum.Parse<MediaCompany.Domain.Capabilities.ReasoningTier>(reader.GetString(22)),
            ReasoningTierServed = reader.IsDBNull(23) ? null : Enum.Parse<MediaCompany.Domain.Capabilities.ReasoningTier>(reader.GetString(23)),
            CostStated = reader.IsDBNull(24) ? null : reader.GetBoolean(24),
            CostUnstatedReason = reader.IsDBNull(25) ? null : Enum.Parse<CostUnstatedReason>(reader.GetString(25)),
        };
    }

    private static void AddNullable(NpgsqlCommand command, string name, NpgsqlDbType type, object? value)
    {
        var parameter = command.Parameters.Add(name, type);
        parameter.Value = value ?? DBNull.Value;
    }
}
