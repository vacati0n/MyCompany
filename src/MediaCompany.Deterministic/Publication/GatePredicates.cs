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

    /// <summary>
    /// The channel is not registered on every music and stock library its assets come from. One of
    /// the three conditions of first publication, decided from a recorded observation.
    /// </summary>
    FirstPublicationLibraryRegistrationUnmet = 7,

    /// <summary>The payment account does not exist. A condition of first publication.</summary>
    FirstPublicationPaymentAccountUnmet = 8,

    /// <summary>Two-step verification is unconfirmed or disabled. A condition of first publication.</summary>
    FirstPublicationTwoStepVerificationUnmet = 9,

    /// <summary>
    /// The dispatch is presented against a version other than the one its approval binds to.
    /// Evaluated AT DISPATCH TIME, so a retry of an attempt recorded against an earlier version
    /// cannot ride the approval that version carried.
    /// </summary>
    DispatchVersionChanged = 10,
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
    /// <summary>
    /// The rule-determined steps this type realizes. Declared here, beside the code, so the
    /// build-time check discovers them from the type rather than from a list maintained elsewhere:
    /// a step realized here and registered nowhere fails that check.
    /// </summary>
    public static readonly IReadOnlyList<string> TaskNames =
    [
        DeterministicTaskRegistry.GateBlocking,
        DeterministicTaskRegistry.GateRelease,
        DeterministicTaskRegistry.ApprovalTokenVerification,
        DeterministicTaskRegistry.FirstPublicationConditionEvaluation,
    ];

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
        DateTimeOffset now,
        FirstPublicationConditionRegister firstPublicationConditions)
    {
        ArgumentNullException.ThrowIfNull(openBlocks);
        ArgumentNullException.ThrowIfNull(approvals);

        // Required, not optional. A caller cannot omit the condition set and thereby skip the
        // three conditions of first publication: there is no overload without it, so "no register
        // supplied" is not a state the predicate can be in. An EMPTY register is expressible and
        // refuses all three, which is the correct reading of no recorded observation.
        ArgumentNullException.ThrowIfNull(firstPublicationConditions);

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

        // The three conditions of first publication, evaluated LAST so every delivered refusal
        // keeps the precedence it had, and evaluated from recorded state read at this moment
        // rather than from a default or a value fixed when this code was built.
        //
        // The register is total over the closed three-member set, so an absent observation
        // produces an ABSENT standing that refuses. Unknown and absent both fold to not satisfied
        // here, at evaluation, and both stay visible in the refusal detail: a reader can tell an
        // unverifiable condition from one observed to be false, which matters because they are
        // discharged by different acts.
        var unsatisfied = firstPublicationConditions.Unsatisfied();
        if (unsatisfied.Count > 0)
        {
            var first = unsatisfied[0];
            return new GateVerdict.Refused(
                RefusalFor(first.Condition),
                string.Join(" ", unsatisfied.Select(s => s.Describe())));
        }

        return new GateVerdict.Passed(new GatePassToken(item, version, OwnerApprovalGate, now));
    }

    /// <summary>
    /// The named refusal for each condition of first publication. The mapping is total over the
    /// closed three-member set, so a fourth condition is a compile error rather than a silent
    /// fall-through to a generic refusal.
    /// </summary>
    public static GateRefusal RefusalFor(FirstPublicationCondition condition) => condition switch
    {
        FirstPublicationCondition.LibraryRegistration => GateRefusal.FirstPublicationLibraryRegistrationUnmet,
        FirstPublicationCondition.PaymentAccount => GateRefusal.FirstPublicationPaymentAccountUnmet,
        FirstPublicationCondition.TwoStepVerification => GateRefusal.FirstPublicationTwoStepVerificationUnmet,
        _ => throw new ArgumentOutOfRangeException(
            nameof(condition), condition, "The condition set is closed at three members."),
    };

    /// <summary>
    /// Re-evaluates the approval binding against the exact version being dispatched.
    ///
    /// Called at DISPATCH time rather than only at gate time. A retry that carries an attempt
    /// recorded against an earlier version is refused by its own name, so it cannot ride the
    /// approval that earlier version carried.
    /// </summary>
    public static GateVerdict.Refused? RefuseOnVersionDrift(
        GatePassToken gatePass,
        ItemId item,
        ItemVersion versionBeingDispatched)
    {
        ArgumentNullException.ThrowIfNull(gatePass);

        if (!gatePass.Item.Equals(item))
        {
            return new GateVerdict.Refused(
                GateRefusal.DispatchVersionChanged,
                $"The gate pass names item {gatePass.Item}, but item {item} is being dispatched.");
        }

        if (!gatePass.ItemVersion.Equals(versionBeingDispatched))
        {
            return new GateVerdict.Refused(
                GateRefusal.DispatchVersionChanged,
                $"The gate pass binds to version {gatePass.ItemVersion}, but version "
                + $"{versionBeingDispatched} is being dispatched; the approval does not carry over.");
        }

        return null;
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
