using MediaCompany.Application.Ports;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// Blocks, approvals and gate transitions (module M-009).
///
/// The elapsed minutes of an approval are read back from a GENERATED column derived from the two
/// recorded timestamps; nothing here writes that figure, which is what makes it a measurement
/// rather than an entry (decision D-007, risk RK-005).
/// </summary>
public sealed class NpgsqlGateLedger : IGateLedger
{
    private readonly NpgsqlDataSource _dataSource;

    public NpgsqlGateLedger(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<IReadOnlyList<Block>> OpenBlocksAsync(ItemId item, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            "SELECT block_id, item_id, placed_by, reason, asset_id, open FROM blocks WHERE item_id = @item_id AND open");
        command.Parameters.AddWithValue("item_id", item.Value);

        var blocks = new List<Block>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            blocks.Add(new Block(
                reader.GetGuid(0),
                new ItemId(reader.GetGuid(1)),
                Enum.Parse<WorkforceRole>(reader.GetString(2)),
                reader.GetString(3),
                reader.IsDBNull(4) ? null : new AssetId(reader.GetGuid(4)),
                reader.GetBoolean(5)));
        }

        return blocks;
    }

    public async Task<IReadOnlyList<Approval>> ApprovalsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT item_id, item_version, gate, approver, verdict, reason, presented_at, decided_at
            FROM approvals WHERE item_id = @item_id ORDER BY presented_at
            """);
        command.Parameters.AddWithValue("item_id", item.Value);

        var approvals = new List<Approval>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            approvals.Add(new Approval(
                new ItemId(reader.GetGuid(0)),
                new ItemVersion(reader.GetInt32(1)),
                reader.GetString(2),
                Enum.Parse<WorkforceRole>(reader.GetString(3)),
                Enum.Parse<ApprovalVerdict>(reader.GetString(4)),
                reader.GetString(5),
                reader.GetFieldValue<DateTimeOffset>(6),
                reader.GetFieldValue<DateTimeOffset>(7)));
        }

        return approvals;
    }

    public async Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT to_state FROM gate_transitions
            WHERE item_id = @item_id AND item_version = @version
            ORDER BY occurred_at DESC LIMIT 1
            """);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("version", version.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is string state ? Enum.Parse<GateState>(state) : GateState.Draft;
    }

    public async Task RecordApprovalAsync(Approval approval, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(approval);

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO approvals (item_id, item_version, gate, approver, verdict, reason, presented_at, decided_at)
            VALUES (@item_id, @version, @gate, @approver, @verdict, @reason, @presented_at, @decided_at)
            """);
        command.Parameters.AddWithValue("item_id", approval.Item.Value);
        command.Parameters.AddWithValue("version", approval.ItemVersion.Value);
        command.Parameters.AddWithValue("gate", approval.Gate);
        command.Parameters.AddWithValue("approver", approval.Approver.ToString());
        command.Parameters.AddWithValue("verdict", approval.Verdict.ToString());
        command.Parameters.AddWithValue("reason", approval.Reason);
        command.Parameters.AddWithValue("presented_at", approval.PresentedAt);
        command.Parameters.AddWithValue("decided_at", approval.DecidedAt);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task RecordBlockAsync(Block block, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(block);

        // Layer one of decision D-003 is enforced before the write reaches the datastore: only a
        // role holding the BlockPlace action can place one, and the publishing role holds none.
        if (!ActionSet.Holds(block.PlacedBy, ActionKind.BlockPlace))
        {
            throw new InvalidOperationException(
                $"{block.PlacedBy} holds no BlockPlace action; the action set is closed.");
        }

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO blocks (block_id, item_id, asset_id, placed_by, reason, open, placed_at)
            VALUES (@block_id, @item_id, @asset_id, @placed_by, @reason, @open, now())
            ON CONFLICT (block_id) DO UPDATE SET open = EXCLUDED.open
            """);
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

        await using var command = _dataSource.CreateCommand(
            """
            INSERT INTO gate_transitions (item_id, item_version, occurred_at, from_state, to_state, reason)
            VALUES (@item_id, @version, @at, @from, @to, @reason)
            """);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("version", version.Value);
        command.Parameters.AddWithValue("at", at);
        command.Parameters.AddWithValue("from", from.ToString());
        command.Parameters.AddWithValue("to", to.ToString());
        command.Parameters.AddWithValue("reason", reason);

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }
}
