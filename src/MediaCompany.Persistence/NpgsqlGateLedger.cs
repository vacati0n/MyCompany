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
            SELECT item_id, item_version, gate, approver, verdict, reason, presented_at, decided_at,
                   queued_at, rework_of
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
                reader.GetFieldValue<DateTimeOffset>(7),

                // Null stays null. It resolves as UNMEASURED at the measurement level, and
                // substituting a zero here would turn an unmeasured approval into a measured one.
                reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
                reader.IsDBNull(9) ? null : reader.GetFieldValue<DateTimeOffset>(9)));
        }

        return approvals;
    }

    public async Task<FirstPublicationConditionRegister> FirstPublicationConditionsAsync(
        ChannelId channel,
        CancellationToken cancellationToken)
    {
        // Every observation is read and the register picks the latest per condition. A channel
        // with no rows yields a register in which all three resolve ABSENT and therefore refuse;
        // there is no code path here that returns "nothing to check".
        //
        // ONE PAYEE (the multi-channel change, decision D-002). Library registration and two-step
        // verification are the channel's own observations. The payment account is the COMPANY'S,
        // reached through the channel's mandatory company reference, so every channel of a company
        // resolves it from the same rows. A per-channel payment-account row recorded before the
        // change is retained in its table and is not read here, so resolution can only be more
        // refusing than before; a channel the register does not hold reaches no company and reads
        // the payment account absent.
        await using var command = _dataSource.CreateCommand(ConditionsSql);
        command.Parameters.AddWithValue("channel_id", channel.Value);
        return await ReadConditionsAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// The three conditions of one channel, the company-level payment account reached through the channel's
    /// company. Shared with the company record reader's snapshot (the AI-management change), which runs it on
    /// its own transaction.
    /// </summary>
    internal const string ConditionsSql =
            """
            SELECT condition, state, evidence, observed_on
            FROM first_publication_conditions
            WHERE channel_id = @channel_id AND condition <> 'PaymentAccount'
            UNION ALL
            SELECT 'PaymentAccount', o.state, o.evidence, o.observed_on
            FROM company_payment_account_observations o
            JOIN channels c ON c.company_id = o.company_id
            WHERE c.channel_id = @channel_id
            ORDER BY observed_on
            """;

    /// <summary>Reads the condition observations a command returns into the total three-member register.</summary>
    internal static async Task<FirstPublicationConditionRegister> ReadConditionsAsync(
        NpgsqlCommand command,
        CancellationToken cancellationToken)
    {
        var observations = new List<ConditionObservation>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            observations.Add(new ConditionObservation(
                Enum.Parse<FirstPublicationCondition>(reader.GetString(0)),
                Enum.Parse<ConditionState>(reader.GetString(1)),
                reader.GetString(2),
                reader.GetFieldValue<DateOnly>(3)));
        }

        return new FirstPublicationConditionRegister(observations);
    }

    public async Task<GateState> CurrentStateAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken)
    {
        await using var command = _dataSource.CreateCommand(
            """
            SELECT to_state FROM gate_transitions
            WHERE item_id = @item_id AND item_version = @version
            ORDER BY recorded_order DESC LIMIT 1
            """);
        command.Parameters.AddWithValue("item_id", item.Value);
        command.Parameters.AddWithValue("version", version.Value);

        var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        return result is string state ? Enum.Parse<GateState>(state) : GateState.Draft;
    }
}
