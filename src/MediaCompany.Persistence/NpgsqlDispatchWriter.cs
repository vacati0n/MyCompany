using MediaCompany.Application.Ports;
using MediaCompany.Domain.Publication;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// Publication dispatch and attempt records (module M-023).
///
/// EXACTLY-ONCE LIVES HERE, and it lives in the datastore rather than in this class. The dispatch
/// table is keyed on the item and its exact version, so a second record for one item version is
/// refused by the primary key whatever this code does. The insert takes the conflict rather than
/// checking first: a read-then-write would leave a window between the check and the write that two
/// concurrent dispatches could both pass, and closing that window with an application lock would
/// move a property the datastore already holds into code that has to be right.
///
/// The writer is reachable only from a transaction, so the dispatch row, the queue entry and the
/// gate state change commit together or none of them does.
/// </summary>
internal sealed class NpgsqlDispatchWriter : IDispatchWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlDispatchWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<DispatchWriteOutcome> RecordDispatchAsync(
        DispatchRecord record,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);

        // ON CONFLICT DO NOTHING over the (item_id, item_version) key. A retry of a dispatch that
        // already succeeded writes nothing and reports AlreadyRecorded; a concurrent duplicate is
        // serialized by the datastore on the same key and one of the two reports the same. In
        // neither case does a second record come into existence.
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO publication_dispatches (
                item_id, item_version, dispatch_key, destination, planned_at, metadata_digest, composed_at)
            VALUES (
                @item_id, @version, @dispatch_key, @destination, @planned_at, @metadata_digest, @composed_at)
            ON CONFLICT (item_id, item_version) DO NOTHING
            RETURNING item_version
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("item_id", record.Item.Value);
        command.Parameters.AddWithValue("version", record.Version.Value);
        command.Parameters.AddWithValue("dispatch_key", record.Key.Value);
        command.Parameters.AddWithValue("destination", record.DestinationCanonical);
        command.Parameters.AddWithValue("planned_at", record.PlannedAt);
        command.Parameters.AddWithValue("metadata_digest", record.MetadataAndSettingsDigest);
        command.Parameters.AddWithValue("composed_at", record.ComposedAt);

        var written = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);

        // RETURNING yields no row when the conflict clause suppressed the insert, which is
        // precisely the retry case.
        return written is null ? DispatchWriteOutcome.AlreadyRecorded : DispatchWriteOutcome.Created;
    }

    public async Task RecordAttemptAsync(AttemptRecord attempt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attempt);

        // Every attempt is appended, refused attempts included. A retry of a refused attempt is a
        // new attempt row under the same dispatch key, never a second dispatch record.
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO publication_attempts (
                attempt_id, dispatch_key, item_id, item_version, destination, attempted_at,
                metadata_digest, approved_by, refused, refusal_reason, refusal_detail)
            VALUES (
                @attempt_id, @dispatch_key, @item_id, @version, @destination, @attempted_at,
                @metadata_digest, @approved_by, @refused, @refusal_reason, @refusal_detail)
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("attempt_id", attempt.AttemptId);
        command.Parameters.AddWithValue("dispatch_key", attempt.Key.Value);
        command.Parameters.AddWithValue("item_id", attempt.Item.Value);
        command.Parameters.AddWithValue("version", attempt.Version.Value);
        command.Parameters.AddWithValue("destination", attempt.DestinationCanonical);
        command.Parameters.AddWithValue("attempted_at", attempt.AttemptedAt);
        command.Parameters.AddWithValue("metadata_digest", attempt.MetadataAndSettingsDigest);
        command.Parameters.AddWithValue("approved_by", attempt.ApprovedBy);
        command.Parameters.AddWithValue("refused", attempt.Refused);
        AddNullable(command, "refusal_reason", attempt.Refused ? attempt.RefusalReason : null);
        AddNullable(command, "refusal_detail", attempt.Refused ? attempt.RefusalDetail : null);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static void AddNullable(NpgsqlCommand command, string name, string? value)
    {
        var parameter = command.Parameters.Add(name, NpgsqlDbType.Text);
        parameter.Value = string.IsNullOrWhiteSpace(value) ? DBNull.Value : value;
    }
}
