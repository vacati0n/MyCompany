using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Publication;

/// <summary>Why the publication gate refused to pass.</summary>
public enum GateRefusal
{
    /// <summary>A copyright block stands against the item.</summary>
    BlockStanding = 1,

    /// <summary>The approval set is incomplete: the owner approval is absent.</summary>
    ApprovalSetIncomplete = 2,

    /// <summary>The approval presented binds to a different version of the item.</summary>
    ApprovalVersionMismatch = 3,

    /// <summary>The transition asked for is not in the transition table.</summary>
    TransitionNotInTable = 4,

    /// <summary>The actor holds no action that would take this transition.</summary>
    ActorHoldsNoAction = 5,

    /// <summary>An asset lacks a verified permission basis, or the channel is unregistered.</summary>
    RightsPreconditionUnmet = 6,
}

/// <summary>The verdict of the gate on one item version.</summary>
public abstract record GateVerdict
{
    private GateVerdict()
    {
    }

    /// <summary>
    /// The gate passed. The token names the exact item version, and is the precondition the
    /// credential broker requires before a release credential is reachable.
    /// </summary>
    public sealed record Passed(GatePassToken Token) : GateVerdict;

    public sealed record Refused(GateRefusal Reason, string Detail) : GateVerdict;
}

/// <summary>
/// The publication gate predicates (module M-009, plan task T-014), under the names
/// <see cref="DeterministicTaskRegistry.GateBlocking"/>,
/// <see cref="DeterministicTaskRegistry.GateRelease"/> and
/// <see cref="DeterministicTaskRegistry.ApprovalTokenVerification"/>.
///
/// This is layer two of decision D-003. It composes with the other three layers rather than
/// replacing them: even if this predicate were wrong, the publishing role holds no Block action
/// (layer one), the transition table has no bypass into <see cref="GateState.Published"/>
/// (layer three), and the release credential still requires the token this produces (layer four).
///
/// No member of this type reaches a publishing platform. Exclusion X-001 removes the upload path
/// and leaves the refusal semantics only, so the gate emits a token and nothing consumes it to
/// publish.
/// </summary>
public static class GatePredicates
{
    public const string OwnerApprovalGate = "publication";

    /// <summary>
    /// Evaluates the publish predicate: zero open blocks, a complete approval set including the
    /// owner approval bound to the exact version, and a transition the table allows.
    /// </summary>
    public static GateVerdict EvaluatePublish(
        ItemId item,
        ItemVersion version,
        GateState currentState,
        WorkforceRole actor,
        IReadOnlyList<Block> openBlocks,
        IReadOnlyList<Approval> approvals,
        bool rightsPreconditionMet,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(openBlocks);
        ArgumentNullException.ThrowIfNull(approvals);

        if (!ActionSet.Holds(actor, ActionKind.Publish))
        {
            return new GateVerdict.Refused(
                GateRefusal.ActorHoldsNoAction,
                $"{actor} holds no Publish action.");
        }

        if (!GateTransitionTable.IsAllowed(currentState, GateState.Published))
        {
            return new GateVerdict.Refused(
                GateRefusal.TransitionNotInTable,
                $"{currentState} to Published is not a transition in the table; Published is reachable only from Approved.");
        }

        var standing = openBlocks.Where(b => b.Open && b.Item.Equals(item)).ToArray();
        if (standing.Length > 0)
        {
            return new GateVerdict.Refused(
                GateRefusal.BlockStanding,
                $"{standing.Length} copyright block(s) stand: {string.Join("; ", standing.Select(b => b.Reason))}");
        }

        if (!rightsPreconditionMet)
        {
            return new GateVerdict.Refused(
                GateRefusal.RightsPreconditionUnmet,
                "An asset lacks a verified permission basis, or the channel is unregistered on a library its assets come from.");
        }

        // The owner approval is mandatory. It is a state in the transition table and absent from
        // the configuration surface (decision D-007), so there is no setting under which this
        // check does not run.
        var ownerApprovals = approvals
            .Where(a => a.Item.Equals(item) && a.Approver == WorkforceRole.Owner && a.Gate == OwnerApprovalGate)
            .ToArray();

        var forThisVersion = ownerApprovals
            .Where(a => a.ItemVersion.Equals(version) && a.Verdict == ApprovalVerdict.Approved)
            .ToArray();

        if (forThisVersion.Length == 0)
        {
            var otherVersion = ownerApprovals.Any(a => !a.ItemVersion.Equals(version));
            if (otherVersion)
            {
                return new GateVerdict.Refused(
                    GateRefusal.ApprovalVersionMismatch,
                    $"The owner approval on record binds to a different version of item {item}; version {version} is not approved.");
            }

            return new GateVerdict.Refused(
                GateRefusal.ApprovalSetIncomplete,
                $"No owner approval is recorded for item {item} version {version}.");
        }

        return new GateVerdict.Passed(new GatePassToken(item, version, OwnerApprovalGate, now));
    }

    /// <summary>
    /// Whether a transition is in the table. Exposed so the lifecycle can ask before it moves, and
    /// so the demonstration can enumerate every path and show none reaches
    /// <see cref="GateState.Published"/> without passing
    /// <see cref="GateState.AwaitingOwnerApproval"/>.
    /// </summary>
    public static bool EveryPathPassesOwnerApproval() =>
        GateTransitionTable.PathsToPublished().All(path => path.Contains(GateState.AwaitingOwnerApproval));
}
