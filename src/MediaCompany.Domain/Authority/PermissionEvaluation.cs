namespace MediaCompany.Domain.Authority;

/// <summary>Why a permission evaluation refused.</summary>
public enum RefusalGround
{
    /// <summary>The action is not in the role's closed set (layer one).</summary>
    ActionNotHeld = 1,

    /// <summary>The approver is the requester of the exception.</summary>
    SelfApproval = 2,

    /// <summary>The approver is a subject role of the control being excepted.</summary>
    ApproverSubjectToControl = 3,

    /// <summary>The approver would gain the action the exception grants.</summary>
    ApproverWouldGainAction = 4,

    /// <summary>The control is marked non-cuttable and no cost decision may reduce it.</summary>
    ControlIsNonCuttable = 5,
}

/// <summary>
/// The answer to "may this role take this action", recorded whichever way it resolves. A refusal
/// carries its ground so the record answers why, per constraint C-006.
/// </summary>
public abstract record PermissionEvaluation
{
    private PermissionEvaluation()
    {
    }

    public sealed record Permitted(WorkforceRole Role, ActionKind Action) : PermissionEvaluation;

    public sealed record Refused(WorkforceRole Role, ActionKind Action, RefusalGround Ground, string Detail)
        : PermissionEvaluation;
}
