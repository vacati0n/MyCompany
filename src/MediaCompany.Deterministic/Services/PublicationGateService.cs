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
///
/// Every gate state change it writes starts from the RECORDED state (the multi-channel change,
/// decision D-008 of its design). The transition table is unchanged.
/// </summary>
public sealed class PublicationGateService
{
    /// <summary>The audit action of a submission for the rights check.</summary>
    public const string SubmittedAction = "gate.submitted-for-rights-check";

    /// <summary>The audit action of a presentation for owner approval.</summary>
    public const string PresentedAction = "gate.presented-for-owner-approval";

    /// <summary>The audit action of an item sent back from the rights check.</summary>
    public const string SentBackFromRightsCheckAction = "gate.sent-back-from-rights-check";

    /// <summary>The audit action of a gate step the service refused by name, writing nothing else.</summary>
    public const string StepRefusedAction = "gate.step-refused";

    private readonly IGateLedger _gates;
    private readonly IAssetLedger _assets;
    private readonly IItemRegister _items;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublicationGateService(
        IGateLedger gates,
        IAssetLedger assets,
        IItemRegister items,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _gates = gates;
        _assets = assets;
        _items = items;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Submits an item version for the rights check (the multi-channel change, decision D-008 of its
    /// design): the writer of the awaiting-rights-check state.
    ///
    /// The transition starts from the RECORDED state and is written only where the transition table
    /// admits it, which it does from Draft alone, under a role holding the approval-presentation
    /// action. Any other recorded state is refused by name, writing nothing but the refusal entry.
    /// </summary>
    public async Task<GateStepOutcome> SubmitForRightsCheckAsync(
        ItemId item,
        ItemVersion version,
        WorkforceRole actor,
        CancellationToken cancellationToken)
    {
        RequireAction(actor, ActionKind.ApprovalPresent);

        var current = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);

        if (!GateTransitionTable.IsAllowed(current, GateState.AwaitingRightsCheck))
        {
            return await RefuseAsync(
                item, version, actor, "submission for the rights check", current,
                GateStepRefusal.NotSubmittableFromRecordedState,
                $"the transition table admits no edge from {current} to {GateState.AwaitingRightsCheck}",
                cancellationToken).ConfigureAwait(false);
        }

        return await TransitionAsync(
            item, version, actor, current, GateState.AwaitingRightsCheck,
            "submitted for the rights check", SubmittedAction, RetentionClass.GovernanceRecord,
            "submission for the rights check", cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Presents an item version for owner approval. The presentation instant is the start of the
    /// elapsed-minutes clock; nothing enters that figure by hand.
    ///
    /// The presentation reads the RECORDED state and refuses by name, writing nothing but a refusal
    /// entry, unless the recorded state is awaiting rights check and the rights check is releasable
    /// for the item's RECORDED channel, which requires at least one recorded asset decision. A
    /// presentation from Draft is therefore refused here before any writer is reached, and the
    /// writer's table check and the datastore's recorded-from-state check stand behind it.
    /// </summary>
    public async Task<GateStepOutcome> PresentForOwnerApprovalAsync(
        ItemId item,
        ItemVersion version,
        CancellationToken cancellationToken)
    {
        const string Step = "presentation for owner approval";
        const WorkforceRole Presenter = WorkforceRole.Producer;

        var current = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);

        if (current != GateState.AwaitingRightsCheck)
        {
            return await RefuseAsync(
                item, version, Presenter, Step, current, GateStepRefusal.NotAwaitingRightsCheck,
                $"an item is presented for owner approval only from {GateState.AwaitingRightsCheck}; it is recorded in {current}",
                cancellationToken).ConfigureAwait(false);
        }

        var channel = await _items.RecordedChannelAsync(item, cancellationToken).ConfigureAwait(false);
        if (channel is not { } recordedChannel)
        {
            return await RefuseAsync(
                item, version, Presenter, Step, current, GateStepRefusal.ItemNotRecorded,
                "no item is recorded under this identifier, so it has no channel whose registrations its rights are checked against",
                cancellationToken).ConfigureAwait(false);
        }

        var today = DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var assets = await _assets.ForItemAsync(item, cancellationToken).ConfigureAwait(false);
        var registrations = await _assets.RegistrationsAsync(recordedChannel, cancellationToken).ConfigureAwait(false);
        var rights = RecordedRightsCheck.Evaluate(assets, registrations, today);

        if (!rights.Releasable)
        {
            return await RefuseAsync(
                item, version, Presenter, Step, current, GateStepRefusal.RightsNotReleasable, rights.Detail,
                cancellationToken).ConfigureAwait(false);
        }

        return await TransitionAsync(
            item, version, Presenter, current, GateState.AwaitingOwnerApproval,
            "approval package presented to the owner", PresentedAction, RetentionClass.GovernanceRecord,
            Step, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an item version back from the rights check where the check was not releasable (the
    /// multi-channel change, decision D-009 of its design). Only from awaiting rights check, under a
    /// role holding the asset-verification action; any other recorded state is refused by name.
    /// </summary>
    public async Task<GateStepOutcome> SendBackFromRightsCheckAsync(
        ItemId item,
        ItemVersion version,
        WorkforceRole actor,
        string reason,
        CancellationToken cancellationToken)
    {
        RequireAction(actor, ActionKind.AssetVerify);

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A send-back carries its reason.", nameof(reason));
        }

        const string Step = "send-back from the rights check";
        var current = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);

        if (current != GateState.AwaitingRightsCheck)
        {
            return await RefuseAsync(
                item, version, actor, Step, current, GateStepRefusal.NotAwaitingRightsCheck,
                $"an item is sent back from the rights check only from {GateState.AwaitingRightsCheck}; it is recorded in {current}",
                cancellationToken).ConfigureAwait(false);
        }

        return await TransitionAsync(
            item, version, actor, current, GateState.SentBack, $"sent back from the rights check: {reason}",
            SentBackFromRightsCheckAction, RetentionClass.RightsRecord, Step, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// One gate transition from the recorded state and its entry, on one transaction. Where the
    /// datastore refuses the from-state, because another writer moved the item after it was read,
    /// the transaction is rolled back and the refusal is named on a transaction of its own.
    /// </summary>
    private async Task<GateStepOutcome> TransitionAsync(
        ItemId item,
        ItemVersion version,
        WorkforceRole actor,
        GateState from,
        GateState to,
        string reason,
        string action,
        RetentionClass retention,
        string step,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        try
        {
            await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
            await transaction.Gates.RecordTransitionAsync(item, version, from, to, reason, now, cancellationToken)
                .ConfigureAwait(false);
            await transaction.Audit.AppendAsync(
                new AuditEntryDraft
                {
                    Actor = actor.ToString(),
                    Action = action,
                    Subject = $"item:{item} version:{version}",
                    Reason = reason,
                    InputsReference = $"from-state:{from}",
                    OutputsReference = $"at:{now:O}",
                    Decision = to.ToString(),
                    CostReference = "none",
                    Risk = "none",
                    RetentionClass = retention,
                },
                cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (GateStateConflictException conflict)
        {
            // Each datastore check is named by its own refusal (the AI-economics change, decision D-009 of
            // its design): a stale from-state, or a transition already recorded at the same instant.
            var refusal = conflict.Reason == GateConflictReason.InstantAlreadyRecorded
                ? GateStepRefusal.TransitionAtRecordedInstant
                : GateStepRefusal.RecordedStateChanged;

            return await RefuseAsync(
                item, version, actor, step, from, refusal, conflict.Message,
                cancellationToken).ConfigureAwait(false);
        }

        return new GateStepOutcome.Moved(from, to, now);
    }

    /// <summary>A refusal by name: one entry on its own transaction, and no state change.</summary>
    private async Task<GateStepOutcome> RefuseAsync(
        ItemId item,
        ItemVersion version,
        WorkforceRole actor,
        string step,
        GateState recorded,
        GateStepRefusal refusal,
        string detail,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = actor.ToString(),
                Action = StepRefusedAction,
                Subject = $"item:{item} version:{version}",
                Reason = $"{step} refused: {refusal}: {detail}",
                InputsReference = $"recorded-state:{recorded}",
                OutputsReference = "none; no gate state was changed",
                Decision = "refused",
                CostReference = "none",
                Risk = "none",
                RetentionClass = RetentionClass.GovernanceRecord,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        return new GateStepOutcome.Refused(recorded, refusal, detail);
    }

    private static void RequireAction(WorkforceRole actor, ActionKind action)
    {
        if (!ActionSet.Holds(actor, action))
        {
            throw new InvalidOperationException($"{actor} holds no {action} action; the action set is closed.");
        }
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

        // The channel is the ITEM'S (the multi-channel change, as ruled at its Design Gate). A
        // caller supplying another channel would have that channel's conditions and registrations
        // read for this item, so the evaluation is refused by name instead, and nothing else is read.
        var recordedChannel = await _items.RecordedChannelAsync(item, cancellationToken).ConfigureAwait(false);
        if (recordedChannel is not { } itemChannel || !itemChannel.Equals(channel))
        {
            var refusal = new GateVerdict.Refused(
                GateRefusal.ChannelNotTheItemsRecordedChannel,
                recordedChannel is { } other
                    ? $"publish evaluation was asked about channel {channel}, and the item is recorded against channel {other}"
                    : $"publish evaluation was asked about channel {channel}, and no item is recorded under this identifier");

            await using (var refused = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false))
            {
                await refused.Audit.AppendAsync(
                    new AuditEntryDraft
                    {
                        Actor = actor.ToString(),
                        Action = "gate.publish-evaluated",
                        Subject = $"item:{item} version:{version}",
                        Reason = $"{refusal.Reason}: {refusal.Detail}",
                        InputsReference = $"channel:{channel}",
                        OutputsReference = "no token; nothing else was read",
                        Decision = "refused",
                        CostReference = "none",
                        Risk = "none",
                        RetentionClass = RetentionClass.GovernanceRecord,
                    },
                    cancellationToken).ConfigureAwait(false);
                await refused.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return refusal;
        }

        var blocks = await _gates.OpenBlocksAsync(item, cancellationToken).ConfigureAwait(false);
        var approvals = await _gates.ApprovalsAsync(item, version, cancellationToken).ConfigureAwait(false);
        var state = await _gates.CurrentStateAsync(item, version, cancellationToken).ConfigureAwait(false);

        var assets = await _assets.ForItemAsync(item, cancellationToken).ConfigureAwait(false);
        var registrations = await _assets.RegistrationsAsync(channel, cancellationToken).ConfigureAwait(false);
        var rights = ReleasableRightsPrecondition.Evaluate(assets, registrations, today);

        // Read at evaluation time from recorded observations. An unreadable or absent register
        // yields the empty one, which refuses all three by name rather than passing them.
        var conditions = await _gates
            .FirstPublicationConditionsAsync(channel, cancellationToken).ConfigureAwait(false);

        var verdict = GatePredicates.EvaluatePublish(
            item, version, state, actor, blocks, approvals, rights.Releasable, now, conditions);

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

/// <summary>Why a gate step was refused by name. Closed; a seventh reason is a code change.</summary>
public enum GateStepRefusal
{
    /// <summary>The transition table admits no edge from the recorded state to awaiting rights check.</summary>
    NotSubmittableFromRecordedState = 1,

    /// <summary>The step is taken only from awaiting rights check, and the item is recorded elsewhere.</summary>
    NotAwaitingRightsCheck = 2,

    /// <summary>The rights check is not releasable for the item's recorded channel, or no asset decision is recorded.</summary>
    RightsNotReleasable = 3,

    /// <summary>No item is recorded under the identifier, so it has no recorded channel.</summary>
    ItemNotRecorded = 4,

    /// <summary>Another writer changed the item's recorded state after it was read; the datastore refused the stale state.</summary>
    RecordedStateChanged = 5,

    /// <summary>
    /// A transition of the item version is already recorded at the same instant; the datastore recorded the first
    /// admitted under the item hold and refused this one under its own name (the AI-economics change).
    /// </summary>
    TransitionAtRecordedInstant = 6,
}

/// <summary>
/// What one gate step did: the state moved, or the step was refused by name with the recorded state
/// it was refused from. A closed union, so a refusal is never an exception a caller can miss.
/// </summary>
public abstract record GateStepOutcome
{
    private GateStepOutcome()
    {
    }

    /// <summary>The recorded state moved, and the entry recording it committed with it.</summary>
    public sealed record Moved(GateState From, GateState To, DateTimeOffset At) : GateStepOutcome;

    /// <summary>Nothing moved; only the refusal entry was written.</summary>
    public sealed record Refused(GateState RecordedState, GateStepRefusal Reason, string Detail) : GateStepOutcome;
}
