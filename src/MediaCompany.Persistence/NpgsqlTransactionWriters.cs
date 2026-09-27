using MediaCompany.Application.Ports;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Publication;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// Gate state changes, written on the calling transaction's own connection.
///
/// This is the correction the review raised: an approval, a transition or a block and the audit
/// entry that records it now commit together, so no committed state change exists without its
/// entry and no entry describes a change that did not commit.
/// </summary>
internal sealed class NpgsqlGateWriter : IGateWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlGateWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task RecordApprovalAsync(Approval approval, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approval);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO approvals (
                item_id, item_version, gate, approver, verdict, reason, presented_at, decided_at,
                queued_at, rework_of)
            VALUES (
                @item_id, @version, @gate, @approver, @verdict, @reason, @presented_at, @decided_at,
                @queued_at, @rework_of)
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("item_id", approval.Item.Value);
        command.Parameters.AddWithValue("version", approval.ItemVersion.Value);
        command.Parameters.AddWithValue("gate", approval.Gate);
        command.Parameters.AddWithValue("approver", approval.Approver.ToString());
        command.Parameters.AddWithValue("verdict", approval.Verdict.ToString());
        command.Parameters.AddWithValue("reason", approval.Reason);
        command.Parameters.AddWithValue("presented_at", approval.PresentedAt);
        command.Parameters.AddWithValue("decided_at", approval.DecidedAt);

        // Null rather than a substituted zero. A null queued mark resolves as UNMEASURED at the
        // measurement level, which is a different fact from a measured zero wait.
        command.Parameters.Add("queued_at", NpgsqlDbType.TimestampTz).Value =
            (object?)approval.QueuedAt ?? DBNull.Value;
        command.Parameters.Add("rework_of", NpgsqlDbType.TimestampTz).Value =
            (object?)approval.ReworkOf ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordBlockAsync(Block block, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(block);

        // Layer one of decision D-003, enforced before the write reaches the datastore: only a
        // role holding the BlockPlace action can place one, and the publishing role holds none.
        if (!ActionSet.Holds(block.PlacedBy, ActionKind.BlockPlace))
        {
            throw new InvalidOperationException(
                $"{block.PlacedBy} holds no BlockPlace action; the action set is closed.");
        }

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO blocks (block_id, item_id, asset_id, placed_by, reason, open, placed_at)
            VALUES (@block_id, @item_id, @asset_id, @placed_by, @reason, @open, now())
            ON CONFLICT (block_id) DO UPDATE SET open = EXCLUDED.open
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("block_id", block.Id);
        command.Parameters.AddWithValue("item_id", block.Item.Value);
        command.Parameters.Add("asset_id", NpgsqlDbType.Uuid).Value = (object?)block.Asset?.Value ?? DBNull.Value;
        command.Parameters.AddWithValue("placed_by", block.PlacedBy.ToString());
        command.Parameters.AddWithValue("reason", block.Reason);
        command.Parameters.AddWithValue("open", block.Open);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordTransitionAsync(
        ItemId item,
        ItemVersion version,
        GateState from,
        GateState to,
        string reason,
        DateTimeOffset at,
        CancellationToken cancellationToken)
    {
        // Layer three of decision D-003. The table in code is the authority; this check keeps a
        // caller from writing a transition the table does not contain, and the datastore trigger
        // refuses the one that matters even if this check were bypassed.
        if (!GateTransitionTable.IsAllowed(from, to))
        {
            throw new InvalidOperationException(
                $"{from} to {to} is not a transition in the gate table; there is no bypass into Published.");
        }

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO gate_transitions (item_id, item_version, occurred_at, from_state, to_state, reason)
            VALUES (@item_id, @version, @at, @from, @to, @reason)
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("version", version.Value);
        command.Parameters.AddWithValue("at", at);
        command.Parameters.AddWithValue("from", from.ToString());
        command.Parameters.AddWithValue("to", to.ToString());
        command.Parameters.AddWithValue("reason", reason);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}

/// <summary>
/// Route availability transitions, written on the calling transaction's own connection, so the
/// state a route moves to and the operation that caused the move commit together.
/// </summary>
internal sealed class NpgsqlRouteAvailabilityWriter : IRouteAvailabilityWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlRouteAvailabilityWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task RecordAsync(RouteAvailability availability, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(availability);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO route_availability (route_id, effective_from, state, reason, reset_or_probe_point, observed_quality)
            VALUES (@route_id, @effective_from, @state, @reason, @reset, @observed)
            ON CONFLICT (route_id, effective_from) DO NOTHING
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("route_id", availability.Route.Value);
        command.Parameters.AddWithValue("effective_from", availability.EffectiveFrom);
        command.Parameters.AddWithValue("state", availability.State.ToString());
        command.Parameters.AddWithValue("reason", availability.Reason);
        command.Parameters.Add("reset", NpgsqlDbType.TimestampTz).Value =
            (object?)availability.ResetOrProbePoint ?? DBNull.Value;
        command.Parameters.Add("observed", NpgsqlDbType.Integer).Value =
            (object?)availability.ObservedQuality?.Value ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
