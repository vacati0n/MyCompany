using MediaCompany.Domain.Authority;

namespace MediaCompany.Deterministic.Authority;

/// <summary>
/// Permission evaluation over the closed action set, and the exception invariant (module M-008,
/// decision D-003 layers one and four of the write-time checks, plan task T-013).
/// </summary>
public static class AuthorityRules
{
    /// <summary>
    /// Layer one. The answer is a lookup in the closed set of <see cref="ActionSet"/>: the
    /// copyright role holds no action that releases, so an attempt to release is refused because
    /// the action is not held, not because a rule rejected it.
    /// </summary>
    public static PermissionEvaluation Evaluate(WorkforceRole role, ActionKind action)
    {
        if (ActionSet.Holds(role, action))
        {
            return new PermissionEvaluation.Permitted(role, action);
        }

        return new PermissionEvaluation.Refused(
            role,
            action,
            RefusalGround.ActionNotHeld,
            $"{role} holds no {action} action; the action set is closed.");
    }

    /// <summary>
    /// The write-time invariant on exception approval (risk R-008, acceptance criterion AC-012).
    /// The approver may not be the requester, may not be a subject role of the control being
    /// excepted, and may not hold the action the exception would grant. A control with no subject
    /// role cannot exist, because <see cref="Control"/> refuses to construct one.
    /// </summary>
    public static PermissionEvaluation EvaluateExceptionApproval(
        Control control,
        ExceptionRequest request,
        WorkforceRole approver)
    {
        ArgumentNullException.ThrowIfNull(control);
        ArgumentNullException.ThrowIfNull(request);

        if (control.NonCuttable)
        {
            return new PermissionEvaluation.Refused(
                approver,
                ActionKind.ControlException,
                RefusalGround.ControlIsNonCuttable,
                $"Control {control.Id} is marked non-cuttable and cannot be excepted.");
        }

        if (approver == request.Requester)
        {
            return new PermissionEvaluation.Refused(
                approver,
                ActionKind.ControlException,
                RefusalGround.SelfApproval,
                $"{approver} requested exception {request.Id} and may not approve it.");
        }

        if (control.SubjectRoles.Contains(approver))
        {
            return new PermissionEvaluation.Refused(
                approver,
                ActionKind.ControlException,
                RefusalGround.ApproverSubjectToControl,
                $"{approver} is a subject role of control {control.Id}.");
        }

        if (control.GrantsActions.Any(a => ActionSet.Holds(approver, a)))
        {
            return new PermissionEvaluation.Refused(
                approver,
                ActionKind.ControlException,
                RefusalGround.ApproverWouldGainAction,
                $"{approver} already holds an action exception {request.Id} would grant.");
        }

        if (!ActionSet.Holds(approver, ActionKind.ControlException))
        {
            return new PermissionEvaluation.Refused(
                approver,
                ActionKind.ControlException,
                RefusalGround.ActionNotHeld,
                $"{approver} holds no ControlException action.");
        }

        return new PermissionEvaluation.Permitted(approver, ActionKind.ControlException);
    }
}

/// <summary>A cost decision, which records the controls it did not touch (acceptance criterion AC-028).</summary>
public sealed record CostDecision(
    string Id,
    WorkforceRole DecidedBy,
    string Reason,
    IReadOnlyList<string> ControlsReduced,
    IReadOnlyList<string> ControlsUntouched,
    DateTimeOffset DecidedAt);

/// <summary>
/// The protection of non-cuttable controls (module M-006 over M-008, plan task T-027). A cost
/// decision that would disable or reduce a control marked non-cuttable is refused, and every cost
/// decision records the controls it did not touch.
/// </summary>
public static class NonCuttableControlProtection
{
    public sealed record Verdict(bool Accepted, IReadOnlyList<string> RefusedControls, CostDecision? Recorded);

    public static Verdict Apply(
        IReadOnlyList<Control> allControls,
        IReadOnlyList<string> controlsTheDecisionWouldReduce,
        string decisionId,
        WorkforceRole decidedBy,
        string reason,
        DateTimeOffset decidedAt)
    {
        ArgumentNullException.ThrowIfNull(allControls);
        ArgumentNullException.ThrowIfNull(controlsTheDecisionWouldReduce);

        var refused = allControls
            .Where(c => c.NonCuttable && controlsTheDecisionWouldReduce.Contains(c.Id, StringComparer.Ordinal))
            .Select(c => c.Id)
            .ToArray();

        if (refused.Length > 0)
        {
            return new Verdict(false, refused, null);
        }

        var untouched = allControls
            .Select(c => c.Id)
            .Where(id => !controlsTheDecisionWouldReduce.Contains(id, StringComparer.Ordinal))
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        var recorded = new CostDecision(
            decisionId,
            decidedBy,
            reason,
            controlsTheDecisionWouldReduce.ToArray(),
            untouched,
            decidedAt);

        return new Verdict(true, [], recorded);
    }
}
