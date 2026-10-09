using MediaCompany.Application.Ports;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The benchmark record's writer on one transaction (the AI-economics change, decision D-002 of its
/// design).
///
/// A corpus entry is registered as a reference only, stamped by the datastore. An observation is written
/// in ONE STATEMENT that takes its route and model from the operation it references — so the three agree
/// for this writer as the composite references make them agree for every writer — encodes its quality in
/// the row's case columns, and is stamped by the datastore: the later of its clock, the record horizon
/// under a shared hold kept to the end of this transaction, and the referenced operation's own instant,
/// booked into that instant's month. No cost, latency, route, model or instant is taken from the caller.
/// </summary>
internal sealed class NpgsqlBenchmarkWriter : IBenchmarkWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlBenchmarkWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<DateTimeOffset> RegisterEntryAsync(
        CorpusEntryId entry,
        TaskClass taskClass,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            WITH stamp AS (
                SELECT GREATEST(clock_timestamp(), h.horizon) AS at
                FROM audit_record_horizon h
                WHERE h.only_row
                FOR SHARE
            )
            INSERT INTO benchmark_corpus_entries (entry_id, task_class, registered_at)
            SELECT @entry_id, @task_class, stamp.at FROM stamp
            RETURNING registered_at
            """,
            _connection,
            _transaction);
        command.Parameters.AddWithValue("entry_id", entry.Value);
        command.Parameters.AddWithValue("task_class", taskClass.ToString());

        var registered = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return registered is DateTimeOffset at
            ? at
            : registered is DateTime utc
                ? new DateTimeOffset(DateTime.SpecifyKind(utc, DateTimeKind.Utc))
                : throw new InvalidOperationException(
                    $"Corpus entry {entry} was not registered: the record horizon was not readable, so no instant could be stamped.");
    }

    public async Task<BenchmarkObservation> RecordObservationAsync(
        BenchmarkObservationDraft draft,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        await using (var command = new NpgsqlCommand(
            """
            WITH stamp AS (
                SELECT GREATEST(clock_timestamp(), h.horizon) AS at
                FROM audit_record_horizon h
                WHERE h.only_row
                FOR SHARE
            ),
            ran AS (
                SELECT operation_id, route_id, model_id, occurred_at
                FROM agent_costs
                WHERE operation_id = @operation_id
            )
            INSERT INTO benchmark_observations (
                observation_id, entry_id, task_class, operation_id, route_id, model_id,
                quality_case, quality_amount, quality_unit, quality_unmeasured_reason, quality_unmeasured_detail,
                observed_at, period)
            SELECT @observation_id, @entry_id, @task_class, ran.operation_id, ran.route_id, ran.model_id,
                   @quality_case, @quality_amount, @quality_unit, @quality_reason, @quality_detail,
                   GREATEST(stamp.at, ran.occurred_at),
                   (date_trunc('month', GREATEST(stamp.at, ran.occurred_at) AT TIME ZONE 'UTC'))::date
            FROM stamp, ran
            RETURNING observation_id
            """,
            _connection,
            _transaction))
        {
            command.Parameters.AddWithValue("observation_id", draft.Id.Value);
            command.Parameters.AddWithValue("entry_id", draft.Entry.Value);
            command.Parameters.AddWithValue("task_class", draft.TaskClass.ToString());
            command.Parameters.AddWithValue("operation_id", draft.Operation.Value);
            MeasurementColumns.Add(command, "quality", draft.Quality);

            if (await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is null)
            {
                throw new InvalidOperationException(
                    $"Observation {draft.Id} was not recorded: no operation {draft.Operation} is recorded for it to reference.");
            }
        }

        await using var readBack = new NpgsqlCommand(
            $"""
            {BenchmarkRows.Select}
            WHERE o.observation_id = @observation_id
            """,
            _connection,
            _transaction);
        readBack.Parameters.AddWithValue("observation_id", draft.Id.Value);

        return (await BenchmarkRows.ReadAllAsync(readBack, cancellationToken).ConfigureAwait(false)).Single();
    }
}

/// <summary>
/// The benchmark record's reader (decision D-002 of the AI-economics design). READ MEMBERS ONLY.
///
/// Each member reads in one transaction: the month's observations inside the delivered close-then-read
/// transaction, so their finality is the read's own; and every route the register holds with every
/// observation recorded in one repeatable-read snapshot, so a route with no observation is named and read
/// unmeasured rather than omitted. Nothing here aggregates or decides a case.
/// </summary>
public sealed class NpgsqlBenchmarkReader : IBenchmarkReader
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlBenchmarkReader(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<MonthReading<IReadOnlyList<BenchmarkObservation>>> ObservationsAsync(
        DateOnly month,
        CancellationToken cancellationToken)
    {
        var (closure, observations) = await MonthReads.ReadAsync(
            _dataSource,
            month,
            async (connection, transaction, ct) =>
            {
                await using var command = new NpgsqlCommand(
                    $"""
                    {BenchmarkRows.Select}
                    WHERE o.period = @month
                    ORDER BY o.observed_at, o.observation_id
                    """,
                    connection,
                    transaction);
                command.Parameters.Add("month", NpgsqlDbType.Date).Value = month;
                return await BenchmarkRows.ReadAllAsync(command, ct).ConfigureAwait(false);
            },
            cancellationToken).ConfigureAwait(false);

        return new MonthReading<IReadOnlyList<BenchmarkObservation>>(closure, observations);
    }

    public async Task<BenchmarkRecordSummary> RecordAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection
            .BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken).ConfigureAwait(false);

        var routes = new List<RouteId>();
        await using (var command = new NpgsqlCommand("SELECT route_id FROM routes ORDER BY route_id", connection, transaction))
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
        {
            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                routes.Add(new RouteId(reader.GetGuid(0)));
            }
        }

        IReadOnlyList<BenchmarkObservation> observations;
        await using (var command = new NpgsqlCommand(
            $"""
            {BenchmarkRows.Select}
            ORDER BY o.observation_id
            """,
            connection,
            transaction))
        {
            observations = await BenchmarkRows.ReadAllAsync(command, cancellationToken).ConfigureAwait(false);
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return new BenchmarkRecordSummary(routes, observations);
    }
}
