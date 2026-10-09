using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Record;
using MediaCompany.Domain.Audit;
using Npgsql;

namespace MediaCompany.Persistence;

/// <summary>
/// The append-only record writer (module M-007).
///
/// There is no update and no delete here, and the datastore refuses both with a trigger, so an
/// amendment is refused twice over. <see cref="AppendRefusalAsync"/> is how the refusal itself
/// becomes an entry: the amendment attempt rolls its own transaction back, so the refusal is
/// recorded on a separate one, naming the entry it was attempted against, and the original entry
/// is returned unchanged afterwards (constraint C-014, acceptance criterion AC-009).
/// </summary>
internal sealed class NpgsqlAuditAppender : IAuditAppender
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlAuditAppender(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<AuditEntry> AppendAsync(AuditEntryDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        // The chain head FIRST, exclusively, held to the end of this transaction: appenders
        // serialise here, so the head read below is one no other in-flight appender can extend and
        // two entries can never name the same predecessor. It is the only waited-for exclusive hold
        // on the record; every exclusive acquisition of the record horizon is non-waiting, so a
        // transaction waiting here can form no cycle with one holding the horizon shared.
        await HoldChainHeadAsync(cancellationToken).ConfigureAwait(false);

        // The stamp is the DATASTORE'S, taken before the hash is computed so the chain rule is
        // unchanged, and taken under a shared hold on the record horizon that lasts until this
        // transaction ends: no throughput read can close a period over this entry while it is in
        // flight, and the entry can never be stamped below a horizon a read has already set.
        var occurredAt = await StampAsync(cancellationToken).ConfigureAwait(false);
        var previousHash = await HeadHashAsync(cancellationToken).ConfigureAwait(false);
        var id = AuditEntryId.New();

        var hash = AuditChain.ComputeHash(
            id,
            draft.Actor,
            draft.Action,
            draft.Subject,
            occurredAt,
            draft.Reason,
            draft.InputsReference,
            draft.OutputsReference,
            draft.Decision,
            draft.CostReference,
            draft.Risk,
            draft.RetentionClass,
            previousHash);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO audit_entries (
                entry_id, actor, action, subject, occurred_at, reason,
                inputs_reference, outputs_reference, decision, cost_reference, risk,
                retention_class, previous_entry_hash, entry_hash)
            VALUES (@id, @actor, @action, @subject, @occurred_at, @reason,
                    @inputs, @outputs, @decision, @cost, @risk,
                    @retention, @previous_hash, @hash)
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("id", id.Value);
        command.Parameters.AddWithValue("actor", draft.Actor);
        command.Parameters.AddWithValue("action", draft.Action);
        command.Parameters.AddWithValue("subject", draft.Subject);
        command.Parameters.AddWithValue("occurred_at", occurredAt);
        command.Parameters.AddWithValue("reason", draft.Reason);
        command.Parameters.AddWithValue("inputs", draft.InputsReference);
        command.Parameters.AddWithValue("outputs", draft.OutputsReference);
        command.Parameters.AddWithValue("decision", draft.Decision);
        command.Parameters.AddWithValue("cost", draft.CostReference);
        command.Parameters.AddWithValue("risk", draft.Risk);
        command.Parameters.AddWithValue("retention", draft.RetentionClass.ToString());
        command.Parameters.AddWithValue("previous_hash", previousHash);
        command.Parameters.AddWithValue("hash", hash);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        return new AuditEntry
        {
            Id = id,
            Actor = draft.Actor,
            Action = draft.Action,
            Subject = draft.Subject,
            OccurredAt = occurredAt,
            Reason = draft.Reason,
            InputsReference = draft.InputsReference,
            OutputsReference = draft.OutputsReference,
            Decision = draft.Decision,
            CostReference = draft.CostReference,
            Risk = draft.Risk,
            RetentionClass = draft.RetentionClass,
            PreviousEntryHash = previousHash,
            EntryHash = hash,
        };
    }

    public Task<AuditEntry> AppendRefusalAsync(
        AuditEntryId targetEntry,
        AuditRefusalReason reason,
        string actor,
        CancellationToken cancellationToken) =>
        AppendAsync(
            new AuditEntryDraft
            {
                Actor = actor,
                Action = "record.amendment-refused",
                Subject = $"audit-entry:{targetEntry}",
                Reason = $"{reason}: the recorded history is append-only and was not altered.",
                InputsReference = $"audit-entry:{targetEntry}",
                OutputsReference = "none",
                Decision = "refused",
                CostReference = "none",
                Risk = "an attempt was made to alter the recorded history",
                RetentionClass = RetentionClass.GovernanceRecord,
            },
            cancellationToken);

    /// <summary>
    /// The later of the datastore's clock and the record horizon, read while holding the horizon
    /// shared. The hold is a row hold inside this transaction, so it lasts until commit or
    /// rollback; appenders hold it alongside one another and are not serialized by it.
    /// </summary>
    private async Task<DateTimeOffset> StampAsync(CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT GREATEST(clock_timestamp(), horizon) FROM audit_record_horizon WHERE only_row FOR SHARE",
            _connection,
            _transaction);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException(
                "The record horizon is missing; the fifth schema resource creates it, and no entry is stamped without it.");
        }

        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    /// <summary>
    /// Takes the chain head exclusively, waiting for an appender in flight to end. The datastore's
    /// chain check takes the same hold, so a writer that bypasses this appender waits too, and is
    /// refused if it names any predecessor other than the head it then reads.
    /// </summary>
    private async Task HoldChainHeadAsync(CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT only_row FROM audit_chain_head WHERE only_row FOR UPDATE",
            _connection,
            _transaction);

        var held = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        if (held is null)
        {
            throw new InvalidOperationException(
                "The audit chain head is missing; the sixth schema resource creates it, and no entry is chained without it.");
        }
    }

    private async Task<string> HeadHashAsync(CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            "SELECT entry_hash FROM audit_entries ORDER BY sequence_no DESC LIMIT 1",
            _connection,
            _transaction);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result as string ?? AuditChain.Genesis;
    }
}
