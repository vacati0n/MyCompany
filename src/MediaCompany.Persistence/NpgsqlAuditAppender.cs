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
    private readonly IClock _clock;

    internal NpgsqlAuditAppender(NpgsqlConnection connection, NpgsqlTransaction transaction, IClock clock)
    {
        _connection = connection;
        _transaction = transaction;
        _clock = clock;
    }

    public async Task<AuditEntry> AppendAsync(AuditEntryDraft draft, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var previousHash = await HeadHashAsync(cancellationToken).ConfigureAwait(false);
        var id = AuditEntryId.New();
        var occurredAt = _clock.UtcNow;

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
