using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Publication;
using MediaCompany.Deterministic.Rights;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// The publication gate as the company operates it (module M-009).
///
/// This service lives in the deterministic module and holds no dependency on the capability
/// resolution boundary, the provider adapters or the credential broker, so no gate decision can
/// make a model call (decision D-005).
///
/// Every state change it makes is written on the same transaction as the entry that records it,
/// so no committed approval, transition or block exists without its entry, and no entry describes
/// a change that did not commit.
///
/// The service carries NO release path. Exclusion X-001 removes the upload, so the gate produces
/// a <see cref="GatePassToken"/> and refusals, and nothing here reaches a publishing platform.
/// </summary>
public sealed class PublicationGateService
{
    private readonly IGateLedger _gates;
    private readonly IAssetLedger _assets;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublicationGateService(IGateLedger gates, IAssetLedger assets, IUnitOfWork unitOfWork, IClock clock)
    {
        _gates = gates;
        _assets = assets;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Presents an item version for owner approval. The presentation instant is the start of the
    /// elapsed-minutes clock; nothing enters that figure by hand.
    /// </summary>
    public async Task<DateTimeOffset> PresentForOwnerApprovalAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var current = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await transaction.Gates.RecordTransitionAsync(
            item, version, current, GateState.AwaitingOwnerApproval,
            "approval package presented to the owner", now, cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = "gate",
                Action = "gate.presented-for-owner-approval",
                Subject = $"item:{item} version:{version}",
                Reason = "the approval package was presented to the owner",
                InputsReference = $"from-state:{current}",
                OutputsReference = $"presented-at:{now:O}",
                Decision = GateState.AwaitingOwnerApproval.ToString(),
                CostReference = "none",
                Risk = "none",
                RetentionClass = RetentionClass.GovernanceRecord,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return now;
    }

    /// <summary>
    /// Records one owner verdict. The elapsed minutes are derived from the presentation and the
    /// verdict instants; the approval row's generated column recomputes the same figure in the
    /// datastore, so neither side can enter one (decision D-007).
    ///
    /// No threshold is proposed here. Design fact F-013 records that no supplied source
    /// establishes one, and the measured distribution is the baseline a threshold would later be
    /// proposed from.
    /// </summary>
    public async Task<Approval> RecordOwnerVerdictAsync(
        ItemId item,
        ItemVersion version,
        ApprovalVerdict verdict,
        string reason,
        DateTimeOffset presentedAt,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        if (!ActionSet.Holds(WorkforceRole.Owner, ActionKind.ApprovalDecide))
        {
            throw new InvalidOperationException("The owner role holds no ApprovalDecide action.");
        }

        var approval = new Approval(
            item, version, GatePredicates.OwnerApprovalGate, WorkforceRole.Owner,
            verdict, reason, presentedAt, now);

        var next = verdict == ApprovalVerdict.Approved ? GateState.Approved : GateState.SentBack;

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await transaction.Gates.RecordApprovalAsync(approval, cancellationToken).ConfigureAwait(false);
        await transaction.Gates.RecordTransitionAsync(
            item, version, GateState.AwaitingOwnerApproval, next,
            $"{verdict}: {reason}", now, cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = WorkforceRole.Owner.ToString(),
                Action = $"gate.owner-{verdict.ToString().ToLowerInvariant()}",
                Subject = $"item:{item} version:{version}",
                Reason = reason,
                InputsReference = $"presented-at:{presentedAt:O}",
                OutputsReference = $"decided-at:{now:O} elapsed-minutes:{approval.ElapsedMinutes}",
                Decision = verdict.ToString(),
                CostReference = "none",
                Risk = "none",
                RetentionClass = RetentionClass.GovernanceRecord,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return approval;
    }

    /// <summary>
    /// Evaluates the publish predicate for an item version. All four enforcement layers of
    /// decision D-003 are live: the actor's closed action set, this predicate, the transition
    /// table, and — at the credential broker — the gate-pass token this returns.
    ///
    /// When the rights precondition is unmet the evaluation also places a block naming each
    /// asset and what is missing from its permission basis, so the refusal becomes a standing
    /// fact a role must clear rather than a verdict that must be re-derived on every call
    /// (acceptance criterion AC-006).
    /// </summary>
    public async Task<GateVerdict> EvaluatePublishAsync(
        ItemId item,
        ItemVersion version,
        ChannelId channel,
        WorkforceRole actor,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;
        var today = DateOnly.FromDateTime(now.UtcDateTime);

        var blocks = await _gates.OpenBlocksAsync(item, cancellationToken).ConfigureAwait(false);
        var approvals = await _gates.ApprovalsAsync(item, version, cancellationToken).ConfigureAwait(false);
        var state = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);

        var assets = await _assets.ForItemAsync(item, cancellationToken).ConfigureAwait(false);
        var registrations = await _assets.RegistrationsAsync(channel, cancellationToken).ConfigureAwait(false);
        var rights = ReleasableRightsPrecondition.Evaluate(assets, registrations, today);

        var verdict = GatePredicates.EvaluatePublish(
            item, version, state, actor, blocks, approvals, rights.Releasable, now);

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);

        foreach (var refused in rights.Blocking)
        {
            // The block names the asset and the missing basis, which is what the criterion asks
            // the block to carry. It is placed by the copyright role, the only one holding the
            // action.
            var block = new Block(
                Guid.NewGuid(),
                item,
                WorkforceRole.Copyright,
                $"asset {refused.Asset} is not permitted: {string.Join("; ", refused.MissingOrFailing)}",
                refused.Asset,
                open: true);

            await transaction.Gates.RecordBlockAsync(block, cancellationToken).ConfigureAwait(false);
        }

        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = actor.ToString(),
                Action = "gate.publish-evaluated",
                Subject = $"item:{item} version:{version}",
                Reason = verdict is GateVerdict.Refused refusedVerdict
                    ? $"{refusedVerdict.Reason}: {refusedVerdict.Detail}"
                    : "all preconditions met",
                InputsReference = $"blocks:{blocks.Count} approvals:{approvals.Count} assets:{assets.Count}",
                OutputsReference = verdict is GateVerdict.Passed
                    ? "gate-pass token issued"
                    : $"no token; {rights.Blocking.Count} rights block(s) placed",
                Decision = verdict is GateVerdict.Passed ? "passed" : "refused",
                CostReference = "none",
                Risk = rights.Releasable ? "none" : "an asset lacks a verified permission basis",
                RetentionClass = RetentionClass.GovernanceRecord,
            },
            cancellationToken).ConfigureAwait(false);

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return verdict;
    }

    /// <summary>
    /// Places a copyright block. Only a role holding the BlockPlace action can reach this, and the
    /// publishing role holds none, so "the publisher overrides a block" is not an expressible act.
    /// </summary>
    public async Task<Block> PlaceBlockAsync(
        ItemId item,
        WorkforceRole actor,
        string reason,
        AssetId? asset,
        CancellationToken cancellationToken)
    {
        if (!ActionSet.Holds(actor, ActionKind.BlockPlace))
        {
            throw new InvalidOperationException(
                $"{actor} holds no BlockPlace action; the action set is closed.");
        }

        var block = new Block(Guid.NewGuid(), item, actor, reason, asset, open: true);

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await transaction.Gates.RecordBlockAsync(block, cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = actor.ToString(),
                Action = "gate.block-placed",
                Subject = $"item:{item}",
                Reason = reason,
                InputsReference = asset is { } a ? $"asset:{a}" : "item-level",
                OutputsReference = $"block:{block.Id}",
                Decision = "blocked",
                CostReference = "none",
                Risk = "publication is blocked until the block is cleared by the copyright role",
                RetentionClass = RetentionClass.RightsRecord,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return block;
    }
}
