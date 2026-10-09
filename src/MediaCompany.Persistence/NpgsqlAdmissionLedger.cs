using System.Globalization;
using System.Text.Json;
using MediaCompany.Application.Ports;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Registry;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The admission ledger on one transaction (the AI-economics change, decision D-001 of its design).
///
/// <see cref="ReserveAsync"/> stamps the booking instant exactly as the operation recorder stamps — the
/// later of the datastore's clock and the record horizon, under a SHARED hold on the horizon kept to
/// the end of this transaction — and holds it in a transaction-local setting, so it reverts when the
/// transaction ends and no caller code ever carries it.
///
/// THE CORRECTION CYCLE. <see cref="ReadAsync"/> is ONE STATEMENT: routes, forbidden sources, availability,
/// accounts, prices in force, governing readings and evidence all come from one snapshot at the reserved
/// instant, and the prices it read are captured in a second transaction-local setting that the operation
/// recorder applies. <see cref="TryHoldScopesAsync"/> serialises metered admissions per scope with two
/// transaction-scoped advisory holds tried in one fixed order, the company's first, never waiting on either
/// (the second correction cycle).
/// </summary>
internal sealed class NpgsqlAdmissionLedger : IAdmissionLedger
{
    /// <summary>The transaction-local setting the reserved instant is held in.</summary>
    internal const string BookingInstantSetting = "mediacompany.booking_instant";

    /// <summary>The transaction-local setting the prices captured at admission are held in, as a JSON array.</summary>
    internal const string AdmissionPricesSetting = "mediacompany.admission_prices";

    private const string ReservedInstant = "current_setting('mediacompany.booking_instant')::timestamptz";

    /// <summary>
    /// The governing readings, as common table expressions over the booking month, shared by the snapshot
    /// read and the readings-only read. The headroom is the delivered minimum over the governing channel and
    /// department budgets, CAPPED by what remains of the company's metered allotment, the owner's decided company ceiling (the owner's
    /// decision of 2026-10-09): it caps each estimate exactly as channel headroom does.
    /// </summary>
    private const string ReadingCtes =
        $"""
        booking AS (
            SELECT {ReservedInstant} AS at,
                   (date_trunc('month', {ReservedInstant} AT TIME ZONE 'UTC'))::date AS month
        ),
        channel_budget AS (
            SELECT b.amount, b.currency
            FROM budgets b, booking
            WHERE b.scope_kind = 'Channel' AND b.scope_id = @channel_id AND b.period = booking.month
        ),
        channel_ops AS (
            SELECT count(*)                                        AS operations,
                   count(*) FILTER (WHERE cost_stated IS NOT TRUE) AS unstated,
                   COALESCE(sum(computed_cost), 0)                 AS booked,
                   COALESCE(min(currency), 'USD')                  AS currency
            FROM agent_costs, booking
            WHERE channel_id = @channel_id AND period = booking.month
        ),
        company_ops AS (
            SELECT count(*)                                        AS operations,
                   count(*) FILTER (WHERE cost_stated IS NOT TRUE) AS unstated,
                   COALESCE(sum(computed_cost), 0)                 AS booked,
                   COALESCE(min(currency), 'USD')                  AS currency
            FROM agent_costs, booking
            WHERE period = booking.month
        )
        """;

    private const string ReadingColumns =
        """
               booking.month,
               cb.amount,
               cb.currency,
               co.operations, co.unstated, co.booked, co.currency,
               CASE WHEN cb.amount IS NULL THEN NULL ELSE ROUND(co.booked * 100.0 / cb.amount, 4) END,
               (SELECT c.company_id FROM channels c WHERE c.channel_id = @channel_id),
               mo.operations, mo.unstated, mo.booked, mo.currency,
               ROUND(mo.booked * 100.0 / @allotment, 4),
               LEAST(
                   COALESCE((SELECT MIN(b.amount - u.utilized)
                             FROM fn_budgets_for(@channel_id, @department_id, booking.month) b
                             CROSS JOIN LATERAL fn_budget_utilization(b.budget_id) u), 0),
                   @allotment - mo.booked),
               COALESCE((SELECT MIN(b.currency) FROM fn_budgets_for(@channel_id, @department_id, booking.month) b), 'USD')
        """;

    private const string ReadingJoins =
        """
        FROM booking
        LEFT JOIN channel_budget cb ON true
        CROSS JOIN channel_ops co
        CROSS JOIN company_ops mo
        """;

    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;
    private BookingReservation? _reservation;

    internal NpgsqlAdmissionLedger(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<BookingReservation> ReserveAsync(CancellationToken cancellationToken)
    {
        if (_reservation is { } reserved)
        {
            return reserved;
        }

        await using var command = new NpgsqlCommand(
            """
            WITH stamp AS (
                SELECT GREATEST(clock_timestamp(), h.horizon) AS at
                FROM audit_record_horizon h
                WHERE h.only_row
                FOR SHARE
            )
            SELECT stamp.at,
                   (date_trunc('month', stamp.at AT TIME ZONE 'UTC'))::date,
                   set_config('mediacompany.booking_instant', stamp.at::text, true)
            FROM stamp
            """,
            _connection,
            _transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "No booking instant could be reserved: the record horizon the fifth schema resource creates was not readable.");
        }

        _reservation = new BookingReservation(reader.GetFieldValue<DateTimeOffset>(0), reader.GetFieldValue<DateOnly>(1));
        return _reservation;
    }

    public async Task<bool> TryHoldScopesAsync(ChannelId channel, CancellationToken cancellationToken)
    {
        // Two statements, so the order is the order they are sent in: the company scope first, then the
        // channel scope. Each is TRIED (the second correction cycle): `pg_try_advisory_xact_lock` takes the
        // hold where it is free and answers false at once where another transaction holds it, so no
        // admission waits on a scope hold, whatever the holder's provider call takes. A hold taken is
        // transaction-scoped and released at commit or rollback; where the channel's is refused after the
        // company's was taken, the company's is released with the transaction that records the deferral.
        await using (var company = new NpgsqlCommand(
            "SELECT pg_try_advisory_xact_lock(hashtextextended('mediacompany.admission.company', 0))",
            _connection,
            _transaction))
        {
            if (await company.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not true)
            {
                return false;
            }
        }

        await using var scope = new NpgsqlCommand(
            "SELECT pg_try_advisory_xact_lock(hashtextextended('mediacompany.admission.channel:' || @channel::text, 0))",
            _connection,
            _transaction);
        scope.Parameters.AddWithValue("channel", channel.Value);
        return await scope.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is true;
    }

    public async Task<AdmissionSnapshot> ReadAsync(
        CapabilityClass capability,
        Attribution attribution,
        Money companyAllotment,
        TaskClass? taskClass,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribution);
        RequireReservation();

        // ONE STATEMENT, ONE SNAPSHOT. The registers and the readings are aggregated as JSON columns of one
        // row, and the prices in force are captured on the transaction in the same statement.
        await using var command = new NpgsqlCommand(
            $"""
            WITH {ReadingCtes},
            admitted AS (
                SELECT route_id, capability_class, tier, target_kind, provider_account_id, model_id,
                       substitute_task, hold_reason, rated_quality, context_capacity, terms_basis, terms_verified_on,
                       reasoning_tier_stated, automated_access_position, customer_content_position,
                       terms_positions_evidence, terms_positions_read_on
                FROM routes
                WHERE capability_class = @capability
            ),
            in_force AS (
                SELECT DISTINCT ON (p.model_id, p.unit_kind)
                       p.model_price_id, p.model_id, p.unit_kind, p.unit_price, p.currency, p.source, p.verified_on,
                       p.valid_from, p.valid_to
                FROM model_prices p, booking
                WHERE p.model_id IN (SELECT model_id FROM admitted WHERE model_id IS NOT NULL)
                  AND p.valid_from <= booking.at AND (p.valid_to IS NULL OR p.valid_to > booking.at)
                ORDER BY p.model_id, p.unit_kind, p.unit_price DESC, p.valid_from DESC
            )
            SELECT {ReadingColumns},
                   (SELECT COALESCE(json_agg(a ORDER BY a.tier, a.route_id), '[]') FROM admitted a)::text,
                   (SELECT COALESCE(json_agg(f ORDER BY f.kind, f.identifier), '[]') FROM forbidden_sources f)::text,
                   (SELECT COALESCE(json_agg(v), '[]') FROM (
                        SELECT DISTINCT ON (route_id) route_id, state, effective_from, reason, reset_or_probe_point, observed_quality
                        FROM route_availability
                        WHERE route_id IN (SELECT route_id FROM admitted)
                        ORDER BY route_id, effective_from DESC) v)::text,
                   (SELECT COALESCE(json_agg(pa ORDER BY pa.provider), '[]') FROM provider_accounts pa)::text,
                   (SELECT COALESCE(json_agg(i ORDER BY i.model_id, i.unit_kind), '[]') FROM in_force i)::text,
                   set_config('{AdmissionPricesSetting}',
                       (SELECT COALESCE(json_agg(json_build_object(
                            'id', i.model_price_id, 'model', i.model_id, 'kind', i.unit_kind,
                            'price', i.unit_price, 'currency', i.currency)), '[]') FROM in_force i)::text,
                       true),
                   CASE WHEN @task_class IS NULL THEN '[]' ELSE (
                       SELECT COALESCE(json_agg(e ORDER BY e.observation_id), '[]') FROM (
                           SELECT o.observation_id, o.entry_id, o.task_class, o.route_id, o.model_id, o.operation_id,
                                  o.quality_case, o.quality_amount, o.quality_unit, o.quality_unmeasured_reason,
                                  o.quality_unmeasured_detail, ac.computed_cost, ac.currency, ac.cost_stated, ac.duration_ms,
                                  o.observed_at, o.period
                           FROM benchmark_observations o
                           JOIN agent_costs ac ON ac.operation_id = o.operation_id
                           WHERE o.route_id IN (SELECT route_id FROM admitted)
                             AND o.task_class = @task_class
                             AND o.observed_at <= (SELECT at FROM booking)) e)::text END,
                   -- THE ITEM CAP (the production change, decision D-006 of its design), in the same statement: the
                   -- recorded cap, the stated cost of the item's booked operations over all time, how many of them
                   -- carry an unstated cost, and the worst case of every reservation of the item no booking has
                   -- reconciled. Null where the item has no recorded cap.
                   (SELECT json_build_object(
                        'amount', c.amount, 'currency', c.currency, 'source', c.source,
                        'booked', COALESCE((SELECT sum(a.computed_cost) FROM agent_costs a
                                            WHERE a.item_id = c.item_id AND a.cost_stated IS TRUE), 0),
                        'unstated', (SELECT count(*) FROM agent_costs a
                                     WHERE a.item_id = c.item_id AND a.cost_stated IS NOT TRUE),
                        'open_amount', COALESCE((SELECT sum(r.worst_case_amount) FROM admission_reservations r
                                                 WHERE r.item_id = c.item_id
                                                   AND NOT EXISTS (SELECT 1 FROM agent_costs a WHERE a.operation_id = r.operation_id)), 0),
                        'open_count', (SELECT count(*) FROM admission_reservations r
                                       WHERE r.item_id = c.item_id
                                         AND NOT EXISTS (SELECT 1 FROM agent_costs a WHERE a.operation_id = r.operation_id)))
                    FROM item_caps c WHERE c.item_id = @item_id)::text,
                   -- The unit kinds each admitted model is recorded as billed by (the production change, decision D-007).
                   (SELECT COALESCE(json_object_agg(m.model_id, m.billed_kinds), json_build_object())
                    FROM models m
                    WHERE m.model_id IN (SELECT model_id FROM admitted WHERE model_id IS NOT NULL) AND m.billed_kinds IS NOT NULL)::text
            {ReadingJoins}
            """,
            _connection,
            _transaction);
        AddReadingParameters(command, attribution, companyAllotment);
        command.Parameters.AddWithValue("capability", capability.ToString());
        command.Parameters.Add("task_class", NpgsqlDbType.Text).Value = (object?)taskClass?.ToString() ?? DBNull.Value;
        command.Parameters.AddWithValue("item_id", attribution.Item.Value);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The admission snapshot returned no row; the booking month could not be read.");
        }

        var readings = ReadReadings(reader, attribution);
        var routes = SnapshotJson.Routes(reader.GetString(16));
        return new AdmissionSnapshot(
            routes,
            SnapshotJson.Forbidden(reader.GetString(17)),
            SnapshotJson.Availability(reader.GetString(18)),
            SnapshotJson.Accounts(reader.GetString(19)),
            SnapshotJson.Prices(reader.GetString(20)),
            readings,
            SnapshotJson.Evidence(reader.GetString(22)))
        {
            ItemCap = reader.IsDBNull(23) ? null : SnapshotJson.ItemCap(reader.GetString(23)),
            BilledKinds = SnapshotJson.BilledKinds(reader.GetString(24)),
        };
    }

    /// <summary>
    /// Writes one worst-case reservation on THIS transaction, which the caller commits before the call it reserves
    /// for (the production change, decision D-006 of its design). It takes no scope hold and needs no reserved
    /// instant of its own: the admitted instant is the admission's, and the row's recording instant the datastore's.
    /// </summary>
    public async Task RecordReservationAsync(ReservationDraft reservation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO admission_reservations (
                operation_id, item_id, route_id, model_id, input_units, output_units, cached_units, character_units,
                image_units, worst_case_amount, currency, admitted_at, recorded_at)
            VALUES (
                @operation_id, @item_id, @route_id, @model_id, @input_units, @output_units, @cached_units, @character_units,
                @image_units, @amount, @currency, @admitted_at, clock_timestamp())
            """,
            _connection,
            _transaction);
        command.Parameters.AddWithValue("operation_id", reservation.Operation.Value);
        command.Parameters.AddWithValue("item_id", reservation.Item.Value);
        command.Parameters.AddWithValue("route_id", reservation.Route.Value);
        command.Parameters.AddWithValue("model_id", reservation.Model.Value);
        command.Parameters.AddWithValue("input_units", reservation.WorstCaseUnits.InputUnits);
        command.Parameters.AddWithValue("output_units", reservation.WorstCaseUnits.OutputUnits);
        command.Parameters.AddWithValue("cached_units", reservation.WorstCaseUnits.CachedUnits);
        command.Parameters.AddWithValue("character_units", reservation.WorstCaseUnits.CharacterUnits);
        command.Parameters.AddWithValue("image_units", reservation.WorstCaseUnits.ImageUnits);
        command.Parameters.Add("amount", NpgsqlDbType.Numeric).Value = reservation.WorstCaseAmount.Amount;
        command.Parameters.AddWithValue("currency", reservation.WorstCaseAmount.Currency);
        command.Parameters.AddWithValue("admitted_at", reservation.AdmittedAt);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RestorePricesAsync(IReadOnlyList<ModelPrice> prices, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(prices);

        var captured = JsonSerializer.Serialize(prices.Select(p => new Dictionary<string, object>
        {
            ["id"] = p.Id.Value,
            ["model"] = p.Model.Value,
            ["kind"] = p.UnitKind.ToString(),
            ["price"] = p.UnitPrice,
            ["currency"] = p.Currency,
        }));

        await using var command = new NpgsqlCommand(
            $"SELECT set_config('{AdmissionPricesSetting}', @captured, true)", _connection, _transaction);
        command.Parameters.AddWithValue("captured", captured);
        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<GoverningReadingsSummary> GoverningReadingsAsync(
        Attribution attribution,
        Money companyAllotment,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribution);
        RequireReservation();

        // ONE STATEMENT: the booking month, the channel's recorded budget amount, the channel's and the
        // company's booked operations with how many carry an unstated cost, both utilisations, the
        // channel's company and the capped headroom. A cost that is not stated — false, or null for a row
        // recorded before the statement existed — is counted as unstated, never as zero.
        await using var command = new NpgsqlCommand(
            $"""
            WITH {ReadingCtes}
            SELECT {ReadingColumns}
            {ReadingJoins}
            """,
            _connection,
            _transaction);
        AddReadingParameters(command, attribution, companyAllotment);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The governing readings returned no row; the booking month could not be read.");
        }

        return ReadReadings(reader, attribution);
    }

    private static void AddReadingParameters(NpgsqlCommand command, Attribution attribution, Money companyAllotment)
    {
        command.Parameters.AddWithValue("channel_id", attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", attribution.Department.Value);
        command.Parameters.Add("allotment", NpgsqlDbType.Numeric).Value = companyAllotment.Amount;
    }

    private static GoverningReadingsSummary ReadReadings(NpgsqlDataReader reader, Attribution attribution) =>
        new(
            reader.GetFieldValue<DateOnly>(0),
            attribution.Channel,
            reader.IsDBNull(1) ? null : new Money(reader.GetDecimal(1), reader.GetString(2)),
            new ScopeSpend(reader.GetInt64(3), reader.GetInt64(4), new Money(reader.GetDecimal(5), reader.GetString(6))),
            reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            reader.IsDBNull(8) ? null : new CompanyId(reader.GetGuid(8)),
            new ScopeSpend(reader.GetInt64(9), reader.GetInt64(10), new Money(reader.GetDecimal(11), reader.GetString(12))),
            reader.IsDBNull(13) ? null : reader.GetDecimal(13),
            new Money(reader.GetDecimal(14), reader.GetString(15)));

    public async Task RecordDecisionAsync(AdmissionDecisionDraft decision, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(decision);
        RequireReservation();

        var controller = decision.Controller;
        var selection = decision.Selection;

        await using (var header = new NpgsqlCommand(
            $"""
            INSERT INTO admission_decisions (
                operation_id, decided_at, booking_month, action, basis, basis_statement, task_class,
                tier_outcome, tier_statement, company_basis_statement, reservation_statement, observations_ranked_on)
            VALUES (
                @operation_id, {ReservedInstant}, (date_trunc('month', {ReservedInstant} AT TIME ZONE 'UTC'))::date,
                @action, @basis, @basis_statement, @task_class,
                @tier_outcome, @tier_statement, @company_basis_statement, @reservation_statement, @observations_ranked_on)
            """,
            _connection,
            _transaction))
        {
            header.Parameters.AddWithValue("operation_id", decision.Operation.Value);
            header.Parameters.AddWithValue("action", controller.Action.ToString());
            header.Parameters.AddWithValue("basis", selection.Basis.ToString());
            header.Parameters.AddWithValue("basis_statement", selection.Statement);
            Add(header, "task_class", NpgsqlDbType.Text, selection.TaskClass?.ToString());
            Add(header, "tier_outcome", NpgsqlDbType.Text, decision.TierOutcome?.ToString());
            header.Parameters.AddWithValue("tier_statement", decision.TierStatement);
            header.Parameters.AddWithValue("company_basis_statement", controller.CompanyBasisStatement);
            header.Parameters.AddWithValue("reservation_statement", decision.ReservationStatement);
            header.Parameters.AddWithValue("observations_ranked_on", (long)selection.ObservationsRankedOn.Count);
            await header.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        // THE HELD OUTCOME (the AI-management change, decision D-006 of its design), beside the decision on the
        // same transaction, at the reserved instant the operation was booked at; the record's own check refuses
        // it below the horizon or at any other instant.
        if (decision.Held is { } held)
        {
            await using var row = new NpgsqlCommand(
                $"""
                INSERT INTO admission_held_outcomes (operation_id, reason, held_at, escalates_at, hold_timeout, escalates_to_owner)
                VALUES (@operation_id, @reason, {ReservedInstant}, @escalates_at, @hold_timeout, @escalates_to_owner)
                """,
                _connection,
                _transaction);
            row.Parameters.AddWithValue("operation_id", decision.Operation.Value);
            row.Parameters.AddWithValue("reason", held.Reason.ToString());
            row.Parameters.AddWithValue("escalates_at", held.EscalatesAt);
            row.Parameters.Add("hold_timeout", NpgsqlDbType.Interval).Value = held.HoldTimeout;
            row.Parameters.AddWithValue("escalates_to_owner", held.EscalatesToOwner);
            await row.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var reading in controller.Readings)
        {
            await using var row = new NpgsqlCommand(
                """
                INSERT INTO admission_decision_readings (
                    operation_id, scope_kind, scope_id,
                    amount_case, amount, amount_currency, amount_recorded_in, amount_looked_for,
                    spend_case, spend_amount, spend_unit, spend_unmeasured_reason, spend_unmeasured_detail,
                    utilisation_case, utilisation_amount, utilisation_unit, utilisation_unmeasured_reason,
                    utilisation_unmeasured_detail, threshold, action, reason)
                VALUES (
                    @operation_id, @scope_kind, @scope_id,
                    @amount_case, @amount, @amount_currency, @amount_recorded_in, @amount_looked_for,
                    @spend_case, @spend_amount, @spend_unit, @spend_reason, @spend_detail,
                    @util_case, @util_amount, @util_unit, @util_reason, @util_detail, @threshold, @action, @reason)
                """,
                _connection,
                _transaction);
            row.Parameters.AddWithValue("operation_id", decision.Operation.Value);
            row.Parameters.AddWithValue("scope_kind", reading.Scope.ToString());
            Add(row, "scope_id", NpgsqlDbType.Uuid, reading.ScopeId);
            AddRecordedAmount(row, reading.Amount);
            MeasurementColumns.Add(row, "spend", reading.BookedSpend);
            MeasurementColumns.Add(row, "util", reading.Utilisation);
            Add(row, "threshold", NpgsqlDbType.Integer, reading.Threshold is { } t ? (int)t : null);
            row.Parameters.AddWithValue("action", reading.Action.ToString());
            Add(row, "reason", NpgsqlDbType.Text, reading.Reason?.ToString());
            await row.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var position = 0;
        foreach (var candidate in selection.Candidates)
        {
            position++;
            await using var row = new NpgsqlCommand(
                """
                INSERT INTO admission_decision_candidates (
                    operation_id, route_id, position, configured_rating,
                    quality_case, quality_amount, quality_unit, quality_unmeasured_reason, quality_unmeasured_detail,
                    cost_case, cost_amount, cost_unit, cost_unmeasured_reason, cost_unmeasured_detail, observations)
                VALUES (
                    @operation_id, @route_id, @position, @configured_rating,
                    @quality_case, @quality_amount, @quality_unit, @quality_reason, @quality_detail,
                    @cost_case, @cost_amount, @cost_unit, @cost_reason, @cost_detail, @observations)
                """,
                _connection,
                _transaction);
            row.Parameters.AddWithValue("operation_id", decision.Operation.Value);
            row.Parameters.AddWithValue("route_id", candidate.Route.Value);
            row.Parameters.AddWithValue("position", position);
            row.Parameters.AddWithValue("configured_rating", candidate.ConfiguredRating.Value);
            MeasurementColumns.Add(row, "quality", candidate.ObservedQuality);
            MeasurementColumns.Add(row, "cost", candidate.ObservedCost);
            row.Parameters.AddWithValue("observations", MeasurementColumns.CountOf(candidate.Observations));
            await row.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var observation in selection.ObservationsRankedOn)
        {
            await using var row = new NpgsqlCommand(
                "INSERT INTO admission_decision_observations (operation_id, observation_id) VALUES (@operation_id, @observation_id)",
                _connection,
                _transaction);
            row.Parameters.AddWithValue("operation_id", decision.Operation.Value);
            row.Parameters.AddWithValue("observation_id", observation.Value);
            await row.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private void RequireReservation()
    {
        if (_reservation is null)
        {
            throw new InvalidOperationException(
                "No booking instant is reserved on this transaction: an admission reserves it first, so every read "
                + "and the decision record are taken at the datastore's one instant.");
        }
    }

    private static void AddRecordedAmount(NpgsqlCommand command, RecordedAmount amount)
    {
        switch (amount)
        {
            case RecordedAmount.Recorded recorded:
                command.Parameters.AddWithValue("amount_case", "Recorded");
                Add(command, "amount", NpgsqlDbType.Numeric, recorded.Amount.Amount);
                Add(command, "amount_currency", NpgsqlDbType.Text, recorded.Amount.Currency);
                Add(command, "amount_recorded_in", NpgsqlDbType.Text, recorded.RecordedIn);
                Add(command, "amount_looked_for", NpgsqlDbType.Text, null);
                break;
            case RecordedAmount.NotRecorded missing:
                command.Parameters.AddWithValue("amount_case", "NotRecorded");
                Add(command, "amount", NpgsqlDbType.Numeric, null);
                Add(command, "amount_currency", NpgsqlDbType.Text, null);
                Add(command, "amount_recorded_in", NpgsqlDbType.Text, null);
                Add(command, "amount_looked_for", NpgsqlDbType.Text, missing.LookedFor);
                break;
            default:
                throw new InvalidOperationException("Unreachable: a recorded amount has two shapes.");
        }
    }

    private static void Add(NpgsqlCommand command, string name, NpgsqlDbType type, object? value) =>
        command.Parameters.Add(name, type).Value = value ?? DBNull.Value;
}

/// <summary>
/// The one encoding of a measurement case into row columns, and back, used by every writer and reader of
/// the AI-economics records: an observed value as its non-zero amount and unit, an observed zero as its
/// unit and NO amount, an unmeasured quantity as its reason and detail and no amount and no unit.
/// </summary>
internal static class MeasurementColumns
{
    internal static void Add(NpgsqlCommand command, string prefix, MeasurementQuantity quantity)
    {
        var (theCase, amount, unit, reason, detail) = quantity switch
        {
            MeasurementQuantity.ObservedValue observed => ("ObservedValue", (decimal?)observed.Amount, observed.Unit, (string?)null, (string?)null),
            MeasurementQuantity.ObservedZero zero => ("ObservedZero", null, zero.Unit, null, null),
            MeasurementQuantity.Unmeasured unmeasured => ("Unmeasured", null, null, unmeasured.Reason.ToString(), unmeasured.Detail),
            _ => throw new InvalidOperationException("Unreachable: the measurement union has three cases."),
        };

        command.Parameters.AddWithValue($"{prefix}_case", theCase);
        command.Parameters.Add($"{prefix}_amount", NpgsqlDbType.Numeric).Value = (object?)amount ?? DBNull.Value;
        command.Parameters.Add($"{prefix}_unit", NpgsqlDbType.Text).Value = (object?)unit ?? DBNull.Value;
        command.Parameters.Add($"{prefix}_reason", NpgsqlDbType.Text).Value = (object?)reason ?? DBNull.Value;
        command.Parameters.Add($"{prefix}_detail", NpgsqlDbType.Text).Value = (object?)detail ?? DBNull.Value;
    }

    /// <summary>Reads a case written by <see cref="Add"/> back into the case it was written in.</summary>
    internal static MeasurementQuantity Read(NpgsqlDataReader reader, int at)
    {
        var theCase = reader.GetString(at);
        return theCase switch
        {
            "ObservedValue" => MeasurementQuantity.Observed(reader.GetDecimal(at + 1), reader.GetString(at + 2)),
            "ObservedZero" => MeasurementQuantity.Zero(reader.GetString(at + 2)),
            "Unmeasured" => MeasurementQuantity.NotMeasured(
                Enum.Parse<UnmeasuredReason>(reader.GetString(at + 3)), reader.GetString(at + 4)),
            _ => throw new InvalidOperationException($"The record holds a measurement case it does not admit: {theCase}."),
        };
    }

    /// <summary>A count, as the integer a count column holds. Only an observed count is a count.</summary>
    internal static long CountOf(MeasurementQuantity count) => count switch
    {
        MeasurementQuantity.ObservedValue observed => (long)observed.Amount,
        MeasurementQuantity.ObservedZero => 0,
        _ => throw new InvalidOperationException("A count column holds an observed count, and this count is unmeasured."),
    };
}

/// <summary>
/// The one SELECT and mapping of a benchmark observation with its cost and latency read through the
/// operation it references, shared by the admission ledger, the writer and the reader.
/// </summary>
internal static class BenchmarkRows
{
    internal const string Select =
        """
        SELECT o.observation_id, o.entry_id, o.task_class, o.route_id, o.model_id, o.operation_id,
               o.quality_case, o.quality_amount, o.quality_unit, o.quality_unmeasured_reason, o.quality_unmeasured_detail,
               a.computed_cost, a.currency, a.cost_stated, a.duration_ms, o.observed_at, o.period
        FROM benchmark_observations o
        JOIN agent_costs a ON a.operation_id = o.operation_id
        """;

    internal static async Task<IReadOnlyList<BenchmarkObservation>> ReadAllAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var observations = new List<BenchmarkObservation>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            observations.Add(Map(reader));
        }

        return observations;
    }

    /// <summary>The unmeasured cost of an operation whose cost is not stated, never the zero it was stored as.</summary>
    internal static MeasurementQuantity UnstatedCost(OperationId operation) =>
        MeasurementQuantity.NotMeasured(
            UnmeasuredReason.SourceCannotStateOne,
            $"the cost of operation {operation} is not stated: a consumed unit kind had no price in force at its "
            + "booking instant, or the operation was recorded before the record stated whether its cost is stated");

    internal static BenchmarkObservation Map(NpgsqlDataReader reader)
    {
        var operation = new OperationId(reader.GetGuid(5));

        // The cost is the operation's own, computed by the datastore, and observed only where the
        // operation states it; a cost that is not stated is unmeasured, never the zero it was stored as.
        var cost = !reader.IsDBNull(13) && reader.GetBoolean(13)
            ? MeasurementQuantity.Observed(reader.GetDecimal(11), reader.GetString(12))
            : UnstatedCost(operation);

        return new BenchmarkObservation
        {
            Id = new BenchmarkObservationId(reader.GetGuid(0)),
            Entry = new CorpusEntryId(reader.GetGuid(1)),
            TaskClass = Enum.Parse<TaskClass>(reader.GetString(2)),
            Route = new RouteId(reader.GetGuid(3)),
            Model = new ModelId(reader.GetString(4)),
            Operation = operation,
            Quality = MeasurementColumns.Read(reader, 6),
            Cost = cost,
            Latency = MeasurementQuantity.Observed(reader.GetInt64(14), BenchmarkObservation.LatencyUnit),
            ObservedAt = reader.GetFieldValue<DateTimeOffset>(15),
            Period = reader.GetFieldValue<DateOnly>(16),
        };
    }
}

/// <summary>
/// Reads the JSON columns of the one-statement admission snapshot back into the domain types, with the
/// same mapping the delivered registers apply (the AI-economics change, correction cycle). Every value is
/// the datastore's, read in the one snapshot; nothing here computes a figure.
/// </summary>
internal static class SnapshotJson
{
    internal static IReadOnlyList<Route> Routes(string json) =>
        Each(json, e =>
        {
            RouteTarget target = Text(e, "target_kind") switch
            {
                "ProviderRoute" => new RouteTarget.ProviderRoute(
                    new ProviderAccountId(Text(e, "provider_account_id")!), new ModelId(Text(e, "model_id")!)),
                "NonAiSubstitute" => new RouteTarget.NonAiSubstitute(Text(e, "substitute_task")!),
                "HoldAndEscalate" => new RouteTarget.HoldAndEscalate(Text(e, "hold_reason")!),
                var kind => throw new InvalidOperationException($"Unknown route target kind '{kind}'."),
            };

            var stated = Text(e, "reasoning_tier_stated");
            var access = Text(e, "automated_access_position");
            var content = Text(e, "customer_content_position");
            return new Route(
                new RouteId(e.GetProperty("route_id").GetGuid()),
                Enum.Parse<CapabilityClass>(Text(e, "capability_class")!),
                Enum.Parse<RouteTier>(Text(e, "tier")!),
                target,
                new QualityRating(e.GetProperty("rated_quality").GetInt32()),
                new ContextCapacity(e.GetProperty("context_capacity").GetInt32()),
                Text(e, "terms_basis")!,
                Date(e, "terms_verified_on"),
                statedReasoningTier: stated is null ? null : Enum.Parse<ReasoningTier>(stated))
            {
                // The production change (decision D-003 of its design): the recorded terms positions, or none.
                TermsPositions = access is null && content is null
                    ? null
                    : new RouteTermsPositions(
                        access is null ? null : Enum.Parse<AutomatedAccessPosition>(access),
                        content is null ? null : Enum.Parse<CustomerContentPosition>(content),
                        Text(e, "terms_positions_evidence"),
                        Null(e, "terms_positions_read_on") ? null : Date(e, "terms_positions_read_on")),
            };
        });

    /// <summary>The item cap column of the snapshot (the production change, decision D-006 of its design).</summary>
    internal static ItemCapReading ItemCap(string json)
    {
        using var document = JsonDocument.Parse(json);
        var e = document.RootElement;
        var currency = Text(e, "currency")!;
        return new ItemCapReading
        {
            Cap = new Money(e.GetProperty("amount").GetDecimal(), currency),
            Source = Text(e, "source")!,
            Booked = new Money(e.GetProperty("booked").GetDecimal(), currency),
            UnstatedOperations = e.GetProperty("unstated").GetInt64(),
            OpenReservations = new Money(e.GetProperty("open_amount").GetDecimal(), currency),
            OpenReservationCount = e.GetProperty("open_count").GetInt64(),
        };
    }

    /// <summary>The billed kinds column of the snapshot (the production change, decision D-007 of its design).</summary>
    internal static IReadOnlyDictionary<ModelId, IReadOnlyList<PriceUnitKind>> BilledKinds(string json)
    {
        using var document = JsonDocument.Parse(json);
        var kinds = new Dictionary<ModelId, IReadOnlyList<PriceUnitKind>>();
        foreach (var model in document.RootElement.EnumerateObject())
        {
            kinds[new ModelId(model.Name)] = model.Value.EnumerateArray()
                .Select(k => Enum.Parse<PriceUnitKind>(k.GetString()!))
                .ToArray();
        }

        return kinds;
    }

    internal static IReadOnlyList<ForbiddenSource> Forbidden(string json) =>
        Each(json, e => new ForbiddenSource(
            Enum.Parse<ForbiddenSourceKind>(Text(e, "kind")!),
            Text(e, "identifier")!,
            Text(e, "reason")!,
            Text(e, "evidence_reference")!));

    internal static IReadOnlyDictionary<RouteId, RouteAvailability> Availability(string json) =>
        Each(json, e =>
        {
            var route = new RouteId(e.GetProperty("route_id").GetGuid());
            return new RouteAvailability(
                route,
                Enum.Parse<AvailabilityState>(Text(e, "state")!),
                Instant(e, "effective_from")!.Value,
                Text(e, "reason")!,
                Instant(e, "reset_or_probe_point"),
                Null(e, "observed_quality") ? null : new QualityRating(e.GetProperty("observed_quality").GetInt32()));
        }).ToDictionary(a => a.Route, a => a);

    internal static IReadOnlyList<ProviderAccount> Accounts(string json) =>
        Each(json, e => new ProviderAccount(
            new ProviderAccountId(Text(e, "provider_account_id")!),
            Text(e, "provider")!,
            Text(e, "commercial_terms_basis")!,
            Date(e, "verified_on"),
            Enum.Parse<ProviderAccountStatus>(Text(e, "status")!))
        {
            // The production change (decision D-002 of its design): the recorded scope and scheme, or none.
            Scope = Text(e, "credential_scope") is { } scope ? Enum.Parse<CredentialScope>(scope) : null,
            Scheme = Text(e, "authentication_scheme") is { } scheme ? Enum.Parse<AuthenticationScheme>(scheme) : null,
        });

    internal static IReadOnlyList<ModelPrice> Prices(string json) =>
        Each(json, e => new ModelPrice(
            new ModelPriceId(e.GetProperty("model_price_id").GetGuid()),
            new ModelId(Text(e, "model_id")!),
            Enum.Parse<PriceUnitKind>(Text(e, "unit_kind")!),
            e.GetProperty("unit_price").GetDecimal(),
            Text(e, "currency")!,
            Text(e, "source")!,
            Date(e, "verified_on"),
            Instant(e, "valid_from")!.Value,
            Instant(e, "valid_to")));

    internal static IReadOnlyList<BenchmarkObservation> Evidence(string json) =>
        Each(json, e =>
        {
            var operation = new OperationId(e.GetProperty("operation_id").GetGuid());
            var stated = !Null(e, "cost_stated") && e.GetProperty("cost_stated").GetBoolean();

            MeasurementQuantity quality = Text(e, "quality_case") switch
            {
                "ObservedValue" => MeasurementQuantity.Observed(e.GetProperty("quality_amount").GetDecimal(), Text(e, "quality_unit")!),
                "ObservedZero" => MeasurementQuantity.Zero(Text(e, "quality_unit")!),
                "Unmeasured" => MeasurementQuantity.NotMeasured(
                    Enum.Parse<UnmeasuredReason>(Text(e, "quality_unmeasured_reason")!), Text(e, "quality_unmeasured_detail")!),
                var other => throw new InvalidOperationException($"The record holds a measurement case it does not admit: {other}."),
            };

            return new BenchmarkObservation
            {
                Id = new BenchmarkObservationId(e.GetProperty("observation_id").GetGuid()),
                Entry = new CorpusEntryId(e.GetProperty("entry_id").GetGuid()),
                TaskClass = Enum.Parse<TaskClass>(Text(e, "task_class")!),
                Route = new RouteId(e.GetProperty("route_id").GetGuid()),
                Model = new ModelId(Text(e, "model_id")!),
                Operation = operation,
                Quality = quality,
                Cost = stated
                    ? MeasurementQuantity.Observed(e.GetProperty("computed_cost").GetDecimal(), Text(e, "currency")!)
                    : BenchmarkRows.UnstatedCost(operation),
                Latency = MeasurementQuantity.Observed(e.GetProperty("duration_ms").GetInt64(), BenchmarkObservation.LatencyUnit),
                ObservedAt = Instant(e, "observed_at")!.Value,
                Period = Date(e, "period"),
            };
        });

    private static List<T> Each<T>(string json, Func<JsonElement, T> map)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.EnumerateArray().Select(map).ToList();
    }

    private static bool Null(JsonElement e, string name) =>
        !e.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null;

    private static string? Text(JsonElement e, string name) => Null(e, name) ? null : e.GetProperty(name).GetString();

    private static DateOnly Date(JsonElement e, string name) =>
        DateOnly.ParseExact(e.GetProperty(name).GetString()!, "yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static DateTimeOffset? Instant(JsonElement e, string name) =>
        Null(e, name) ? null : DateTimeOffset.Parse(e.GetProperty(name).GetString()!, CultureInfo.InvariantCulture);
}
