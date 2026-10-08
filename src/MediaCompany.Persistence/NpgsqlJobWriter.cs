using MediaCompany.Application.Ports;
using MediaCompany.Domain.Work;
using Npgsql;
using NpgsqlTypes;

namespace MediaCompany.Persistence;

/// <summary>
/// The durable job queue as a transactional claim table (module M-013 over M-017).
///
/// The claim is taken inside the same transaction as the work it authorises, which is the whole
/// reason stack O-005 was recommended: there is no broker, so there is no second system for the
/// state change and the queue entry to diverge across, and exactly-once under constraint C-004
/// needs no outbox.
///
/// <c>FOR UPDATE SKIP LOCKED</c> is what lets two workers take different jobs at the same time,
/// so work that depends on nothing in common progresses independently (acceptance criterion
/// AC-024).
/// </summary>
internal sealed class NpgsqlJobWriter : IJobWriter
{
    private readonly NpgsqlConnection _connection;
    private readonly NpgsqlTransaction _transaction;

    internal NpgsqlJobWriter(NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<DateTimeOffset> EnqueueAsync(Job job, TimeSpan claimableAfter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        // The availability instant is the DATASTORE'S clock plus the stated delay — the clock every
        // claim compares it against. The job's own availability field is not written.
        await using var command = new NpgsqlCommand(
            """
            INSERT INTO jobs (job_id, item_id, channel_id, workflow, position, claim_state, available_at, claimed_by, lease_expires_at)
            VALUES (@job_id, @item_id, @channel_id, @workflow, @position, @claim_state, now() + @delay, NULL, NULL)
            RETURNING available_at
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("job_id", job.Id.Value);
        command.Parameters.AddWithValue("item_id", job.Item.Value);
        command.Parameters.AddWithValue("channel_id", job.Channel.Value);
        command.Parameters.AddWithValue("workflow", job.Workflow);
        command.Parameters.AddWithValue("position", job.Position.ToString());
        command.Parameters.AddWithValue("claim_state", job.ClaimState.ToString());
        command.Parameters.AddWithValue("delay", Delay(claimableAfter));

        return await AvailableAtAsync(command, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>A delay until claimability is never negative; a negative one would be an instant in the past by another name.</summary>
    private static TimeSpan Delay(TimeSpan claimableAfter) =>
        claimableAfter < TimeSpan.Zero
            ? throw new ArgumentOutOfRangeException(nameof(claimableAfter), "A delay until claimability is not negative.")
            : claimableAfter;

    private static async Task<DateTimeOffset> AvailableAtAsync(NpgsqlCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            throw new InvalidOperationException("No queue row was written for the unit named.");
        }

        return reader.GetFieldValue<DateTimeOffset>(0);
    }

    public async Task RecordStageAsync(JobStage stage, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(stage);

        await using var command = new NpgsqlCommand(
            """
            INSERT INTO job_stages (job_id, position, entered_at, outcome, attempts, escalated, left_at, failure_reason)
            VALUES (@job_id, @position, @entered_at, @outcome, @attempts, @escalated, @left_at, @failure_reason)
            ON CONFLICT (job_id, position, entered_at) DO UPDATE
              SET outcome = EXCLUDED.outcome,
                  attempts = EXCLUDED.attempts,
                  escalated = EXCLUDED.escalated,
                  left_at = EXCLUDED.left_at,
                  failure_reason = EXCLUDED.failure_reason
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("job_id", stage.Job.Value);
        command.Parameters.AddWithValue("position", stage.Position.ToString());
        command.Parameters.AddWithValue("entered_at", stage.EnteredAt);
        command.Parameters.AddWithValue("outcome", stage.Outcome.ToString());
        command.Parameters.AddWithValue("attempts", stage.Attempts);
        command.Parameters.AddWithValue("escalated", stage.Escalated);
        command.Parameters.Add("left_at", NpgsqlDbType.TimestampTz).Value = (object?)stage.LeftAt ?? DBNull.Value;
        command.Parameters.Add("failure_reason", NpgsqlDbType.Text).Value = (object?)stage.FailureReason ?? DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Claims the oldest ready job. Availability and the lease expiry are measured against the
    /// datastore's own clock rather than an injected one, deliberately: two workers reading
    /// different clocks could each believe a job was theirs, and a single datastore is the one
    /// clock they already share. An injected clock therefore does not move claiming, and a test
    /// that seeds an availability instant must seed it in the datastore's past.
    /// </summary>
    public async Task<Job?> ClaimNextAsync(string workerId, TimeSpan lease, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            WITH candidate AS (
                SELECT job_id
                FROM jobs
                WHERE claim_state = 'Ready' AND available_at <= now()
                ORDER BY available_at
                FOR UPDATE SKIP LOCKED
                LIMIT 1
            )
            UPDATE jobs j
               SET claim_state = 'Claimed',
                   claimed_by = @worker,
                   lease_expires_at = now() + @lease
              FROM candidate
             WHERE j.job_id = candidate.job_id
            RETURNING j.job_id, j.item_id, j.channel_id, j.workflow, j.position,
                      j.claim_state, j.available_at, j.claimed_by, j.lease_expires_at
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("worker", workerId);
        command.Parameters.AddWithValue("lease", lease);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new Job
        {
            Id = new JobId(reader.GetGuid(0)),
            Item = new ItemId(reader.GetGuid(1)),
            Channel = new ChannelId(reader.GetGuid(2)),
            Workflow = reader.GetString(3),
            Position = Enum.Parse<LifecyclePosition>(reader.GetString(4)),
            ClaimState = Enum.Parse<ClaimState>(reader.GetString(5)),
            AvailableAt = reader.GetFieldValue<DateTimeOffset>(6),
            ClaimedBy = reader.IsDBNull(7) ? null : reader.GetString(7),
            LeaseExpiresAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        };
    }

    /// <summary>
    /// Claims the NAMED job, only when it is ready and available by the datastore's own clock, or
    /// returns null. The same skip-locked selection as <see cref="ClaimNextAsync"/>: a job another
    /// transaction holds is skipped rather than waited for, so a named claim never blocks on, or
    /// takes, a unit somebody else is working.
    /// </summary>
    public async Task<Job?> ClaimAsync(JobId job, string workerId, TimeSpan lease, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            WITH candidate AS (
                SELECT job_id
                FROM jobs
                WHERE job_id = @job_id AND claim_state = 'Ready' AND available_at <= now()
                FOR UPDATE SKIP LOCKED
            )
            UPDATE jobs j
               SET claim_state = 'Claimed',
                   claimed_by = @worker,
                   lease_expires_at = now() + @lease
              FROM candidate
             WHERE j.job_id = candidate.job_id
            RETURNING j.job_id, j.item_id, j.channel_id, j.workflow, j.position,
                      j.claim_state, j.available_at, j.claimed_by, j.lease_expires_at
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("job_id", job.Value);
        command.Parameters.AddWithValue("worker", workerId);
        command.Parameters.AddWithValue("lease", lease);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        return new Job
        {
            Id = new JobId(reader.GetGuid(0)),
            Item = new ItemId(reader.GetGuid(1)),
            Channel = new ChannelId(reader.GetGuid(2)),
            Workflow = reader.GetString(3),
            Position = Enum.Parse<LifecyclePosition>(reader.GetString(4)),
            ClaimState = Enum.Parse<ClaimState>(reader.GetString(5)),
            AvailableAt = reader.GetFieldValue<DateTimeOffset>(6),
            ClaimedBy = reader.IsDBNull(7) ? null : reader.GetString(7),
            LeaseExpiresAt = reader.IsDBNull(8) ? null : reader.GetFieldValue<DateTimeOffset>(8),
        };
    }

    public async Task<DateTimeOffset> ReleaseAsync(JobId job, ClaimState state, TimeSpan claimableAfter, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE jobs
               SET claim_state = @state,
                   available_at = now() + @delay,
                   claimed_by = NULL,
                   lease_expires_at = NULL
             WHERE job_id = @job_id
            RETURNING available_at
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("job_id", job.Value);
        command.Parameters.AddWithValue("state", state.ToString());
        command.Parameters.AddWithValue("delay", Delay(claimableAfter));

        return await AvailableAtAsync(command, cancellationToken).ConfigureAwait(false);
    }

    public async Task<DateTimeOffset> AdvanceAsync(
        JobId job,
        LifecyclePosition position,
        ClaimState state,
        TimeSpan claimableAfter,
        CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            UPDATE jobs
               SET position = @position,
                   claim_state = @state,
                   available_at = now() + @delay,
                   claimed_by = NULL,
                   lease_expires_at = NULL
             WHERE job_id = @job_id
            RETURNING available_at
            """,
            _connection,
            _transaction);

        command.Parameters.AddWithValue("job_id", job.Value);
        command.Parameters.AddWithValue("position", position.ToString());
        command.Parameters.AddWithValue("state", state.ToString());
        command.Parameters.AddWithValue("delay", Delay(claimableAfter));

        return await AvailableAtAsync(command, cancellationToken).ConfigureAwait(false);
    }
}
