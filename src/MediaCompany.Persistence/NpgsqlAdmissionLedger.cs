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
/// transaction ends and no caller code ever carries it. Every read below is ONE STATEMENT taken AT that
/// instant, and the decision record is stamped with it; the datastore refuses a decision whose instant
/// is not its operation's booked one.
/// </summary>
internal sealed class NpgsqlAdmissionLedger : IAdmissionLedger
{
    /// <summary>The transaction-local setting the reserved instant is held in.</summary>
    internal const string BookingInstantSetting = "mediacompany.booking_instant";

    private const string ReservedInstant = "current_setting('mediacompany.booking_instant')::timestamptz";

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

    public async Task<IReadOnlyList<ModelPrice>> PricesInForceAsync(
        IReadOnlyCollection<ModelId> models,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(models);
        RequireReservation();

        // One row per model and unit kind in force at the reserved instant; where two overlap, the higher
        // price, which is the one the recorder applies at the same instant.
        await using var command = new NpgsqlCommand(
            $"""
            SELECT DISTINCT ON (model_id, unit_kind)
                   model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from, valid_to
            FROM model_prices
            WHERE model_id = ANY(@models)
              AND valid_from <= {ReservedInstant}
              AND (valid_to IS NULL OR valid_to > {ReservedInstant})
            ORDER BY model_id, unit_kind, unit_price DESC, valid_from DESC
            """,
            _connection,
            _transaction);
        command.Parameters.Add("models", NpgsqlDbType.Array | NpgsqlDbType.Text).Value = models.Select(m => m.Value).ToArray();

        var prices = new List<ModelPrice>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            prices.Add(new ModelPrice(
                new ModelPriceId(reader.GetGuid(0)),
                new ModelId(reader.GetString(1)),
                Enum.Parse<PriceUnitKind>(reader.GetString(2)),
                reader.GetDecimal(3),
                reader.GetString(4),
                reader.GetString(5),
                reader.GetFieldValue<DateOnly>(6),
                reader.GetFieldValue<DateTimeOffset>(7),
                reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8)));
        }

        return prices;
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
        // channel's company and the delivered headroom over the governing channel and department
        // budgets. A cost that is not stated — false, or null for a row recorded before the statement
        // existed — is counted as unstated, never as zero.
        await using var command = new NpgsqlCommand(
            $"""
            WITH booking AS (
                SELECT (date_trunc('month', {ReservedInstant} AT TIME ZONE 'UTC'))::date AS month
            ),
            channel_budget AS (
                SELECT b.amount, b.currency
                FROM budgets b, booking
                WHERE b.scope_kind = 'Channel' AND b.scope_id = @channel_id AND b.period = booking.month
            ),
            channel_ops AS (
                SELECT count(*)                                    AS operations,
                       count(*) FILTER (WHERE cost_stated IS NOT TRUE) AS unstated,
                       COALESCE(sum(computed_cost), 0)             AS booked,
                       COALESCE(min(currency), 'USD')              AS currency
                FROM agent_costs, booking
                WHERE channel_id = @channel_id AND period = booking.month
            ),
            company_ops AS (
                SELECT count(*)                                    AS operations,
                       count(*) FILTER (WHERE cost_stated IS NOT TRUE) AS unstated,
                       COALESCE(sum(computed_cost), 0)             AS booked,
                       COALESCE(min(currency), 'USD')              AS currency
                FROM agent_costs, booking
                WHERE period = booking.month
            )
            SELECT booking.month,
                   cb.amount,
                   cb.currency,
                   co.operations, co.unstated, co.booked, co.currency,
                   CASE WHEN cb.amount IS NULL THEN NULL ELSE ROUND(co.booked * 100.0 / cb.amount, 4) END,
                   (SELECT c.company_id FROM channels c WHERE c.channel_id = @channel_id),
                   mo.operations, mo.unstated, mo.booked, mo.currency,
                   ROUND(mo.booked * 100.0 / @allotment, 4),
                   COALESCE((SELECT MIN(b.amount - u.utilized)
                             FROM fn_budgets_for(@channel_id, @department_id, booking.month) b
                             CROSS JOIN LATERAL fn_budget_utilization(b.budget_id) u), 0),
                   COALESCE((SELECT MIN(b.currency) FROM fn_budgets_for(@channel_id, @department_id, booking.month) b), 'USD')
            FROM booking
            LEFT JOIN channel_budget cb ON true
            CROSS JOIN channel_ops co
            CROSS JOIN company_ops mo
            """,
            _connection,
            _transaction);
        command.Parameters.AddWithValue("channel_id", attribution.Channel.Value);
        command.Parameters.AddWithValue("department_id", attribution.Department.Value);
        command.Parameters.Add("allotment", NpgsqlDbType.Numeric).Value = companyAllotment.Amount;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("The governing readings returned no row; the booking month could not be read.");
        }

        return new GoverningReadingsSummary(
            reader.GetFieldValue<DateOnly>(0),
            attribution.Channel,
            reader.IsDBNull(1) ? null : new Money(reader.GetDecimal(1), reader.GetString(2)),
            new ScopeSpend(reader.GetInt64(3), reader.GetInt64(4), new Money(reader.GetDecimal(5), reader.GetString(6))),
            reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            reader.IsDBNull(8) ? null : new CompanyId(reader.GetGuid(8)),
            new ScopeSpend(reader.GetInt64(9), reader.GetInt64(10), new Money(reader.GetDecimal(11), reader.GetString(12))),
            reader.IsDBNull(13) ? null : reader.GetDecimal(13),
            new Money(reader.GetDecimal(14), reader.GetString(15)));
    }

    public async Task<IReadOnlyList<BenchmarkObservation>> EvidenceAsync(
        IReadOnlyCollection<RouteId> routes,
        TaskClass taskClass,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(routes);
        RequireReservation();

        // ONE STATEMENT, at the reserved instant: every observation of the named routes for the task
        // class stamped at or before it, each with its referenced operation's cost and latency.
        await using var command = new NpgsqlCommand(
            $"""
            {BenchmarkRows.Select}
            WHERE o.route_id = ANY(@routes)
              AND o.task_class = @task_class
              AND o.observed_at <= {ReservedInstant}
            ORDER BY o.observation_id
            """,
            _connection,
            _transaction);
        command.Parameters.Add("routes", NpgsqlDbType.Array | NpgsqlDbType.Uuid).Value = routes.Select(r => r.Value).ToArray();
        command.Parameters.AddWithValue("task_class", taskClass.ToString());

        return await BenchmarkRows.ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
    }

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

    internal static BenchmarkObservation Map(NpgsqlDataReader reader)
    {
        var operation = new OperationId(reader.GetGuid(5));

        // The cost is the operation's own, computed by the datastore, and observed only where the
        // operation states it; a cost that is not stated is unmeasured, never the zero it was stored as.
        var cost = !reader.IsDBNull(13) && reader.GetBoolean(13)
            ? MeasurementQuantity.Observed(reader.GetDecimal(11), reader.GetString(12))
            : MeasurementQuantity.NotMeasured(
                UnmeasuredReason.SourceCannotStateOne,
                $"the cost of operation {operation} is not stated: a consumed unit kind had no price in force at its "
                + "booking instant, or the operation was recorded before the record stated whether its cost is stated");

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
