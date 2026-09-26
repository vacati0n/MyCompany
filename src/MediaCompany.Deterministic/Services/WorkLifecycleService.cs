using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Resilience;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Work;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// The work lifecycle (module M-013, plan task T-024).
///
/// The claim, the stage record and the audit entry are written in one transaction, so a crash
/// between the claim and the work it authorises leaves the job ready rather than lost, and the
/// stage history of a completed unit resolves to every position it passed with none missing
/// (acceptance criterion AC-022).
/// </summary>
public sealed class WorkLifecycleService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public WorkLifecycleService(IUnitOfWork unitOfWork, IClock clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task EnqueueAsync(Job job, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await transaction.Jobs.EnqueueAsync(job, cancellationToken).ConfigureAwait(false);
        await transaction.Jobs.RecordStageAsync(
            new JobStage
            {
                Job = job.Id,
                Position = job.Position,
                Outcome = StageOutcome.Pending,
                Attempts = 0,
                Escalated = false,
                EnteredAt = _clock.UtcNow,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(Entry(job, "job.enqueued", "queued for production"), cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Claims one ready job. Two workers calling this concurrently take different jobs, because
    /// the claim skips locked rows, which is how work that depends on nothing in common progresses
    /// independently (acceptance criterion AC-024).
    /// </summary>
    public async Task<Job?> ClaimAsync(string workerId, TimeSpan lease, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        var job = await transaction.Jobs.ClaimNextAsync(workerId, lease, cancellationToken).ConfigureAwait(false);
        if (job is null)
        {
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        await transaction.Audit.AppendAsync(Entry(job, "job.claimed", $"claimed by {workerId}"), cancellationToken)
            .ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        return job;
    }

    /// <summary>
    /// Records the outcome of one stage attempt and applies the failure policy.
    ///
    /// Every failure ends either retried or escalated, because
    /// <see cref="FailureHandling.Decide"/> returns a three-member union with no "do nothing"
    /// member. Acceptance criterion AC-023 counts failures ending in neither, and the type is what
    /// makes that count zero.
    /// </summary>
    public async Task<FailureDisposition?> RecordStageOutcomeAsync(
        Job job,
        LifecyclePosition position,
        DateTimeOffset enteredAt,
        bool succeeded,
        FailureClass? failureClass,
        int attemptsSoFar,
        FailurePolicy policy,
        TimeSpan elapsed,
        TimeSpan deadline,
        string? failureReason,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(policy);

        var now = _clock.UtcNow;

        if (succeeded)
        {
            await using var ok = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
            await ok.Jobs.RecordStageAsync(
                new JobStage
                {
                    Job = job.Id,
                    Position = position,
                    Outcome = StageOutcome.Succeeded,
                    Attempts = attemptsSoFar,
                    Escalated = false,
                    EnteredAt = enteredAt,
                    LeftAt = now,
                },
                cancellationToken).ConfigureAwait(false);
            await ok.Jobs.ReleaseAsync(job.Id, ClaimState.Ready, now, cancellationToken).ConfigureAwait(false);
            await ok.Audit.AppendAsync(Entry(job, "job.stage-succeeded", position.ToString()), cancellationToken)
                .ConfigureAwait(false);
            await ok.CommitAsync(cancellationToken).ConfigureAwait(false);
            return null;
        }

        var disposition = FailureHandling.Decide(
            failureClass ?? FailureClass.Transient, attemptsSoFar, policy, elapsed, deadline);

        var (outcome, escalated, availableAt, claimState) = disposition switch
        {
            FailureDisposition.RetrySameRoute retry =>
                (StageOutcome.Retried, false, now + retry.Backoff, ClaimState.Ready),
            FailureDisposition.ReResolve => (StageOutcome.Retried, false, now, ClaimState.Ready),
            FailureDisposition.Escalate => (StageOutcome.Escalated, true, now, ClaimState.Dead),
            _ => throw new InvalidOperationException("Unreachable: the disposition union has three members."),
        };

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await transaction.Jobs.RecordStageAsync(
            new JobStage
            {
                Job = job.Id,
                Position = position,
                Outcome = outcome,
                Attempts = attemptsSoFar,
                Escalated = escalated,
                EnteredAt = enteredAt,
                LeftAt = now,
                FailureReason = failureReason,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.Jobs.ReleaseAsync(job.Id, claimState, availableAt, cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(
            Entry(job, escalated ? "job.stage-escalated" : "job.stage-retried", failureReason ?? outcome.ToString()),
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return disposition;
    }

    private static AuditEntryDraft Entry(Job job, string action, string reason) => new()
    {
        Actor = "lifecycle",
        Action = action,
        Subject = $"job:{job.Id} item:{job.Item}",
        Reason = reason,
        InputsReference = $"workflow:{job.Workflow} position:{job.Position}",
        OutputsReference = $"claim-state:{job.ClaimState}",
        Decision = action,
        CostReference = "none",
        Risk = "none",
        RetentionClass = RetentionClass.OperationalRecord,
    };
}
