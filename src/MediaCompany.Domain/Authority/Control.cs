namespace MediaCompany.Domain.Authority;

/// <summary>
/// A control the company operates. The subject-role relation is required at construction, so a
/// control with no subject role cannot be admitted — the mitigation recorded against risk R-008,
/// without which a role could approve an exception to a control it is itself subject to.
/// </summary>
public sealed record Control
{
    public Control(
        string id,
        string description,
        bool nonCuttable,
        IReadOnlyCollection<WorkforceRole> subjectRoles,
        IReadOnlyCollection<ActionKind> grantsActions)
    {
        ArgumentNullException.ThrowIfNull(subjectRoles);
        ArgumentNullException.ThrowIfNull(grantsActions);

        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("A control carries an identifier.", nameof(id));
        }

        if (subjectRoles.Count == 0)
        {
            throw new ArgumentException(
                "A control with no subject role cannot be admitted; the relation is required so that no role approves an exception to a control it is subject to.",
                nameof(subjectRoles));
        }

        Id = id;
        Description = description;
        NonCuttable = nonCuttable;
        SubjectRoles = subjectRoles.ToArray();
        GrantsActions = grantsActions.ToArray();
    }

    public string Id { get; }
    public string Description { get; }

    /// <summary>A non-cuttable control carries a marker no cost decision can clear (constraint C-019).</summary>
    public bool NonCuttable { get; }

    public IReadOnlyList<WorkforceRole> SubjectRoles { get; }

    /// <summary>The actions an exception to this control would grant.</summary>
    public IReadOnlyList<ActionKind> GrantsActions { get; }
}

/// <summary>A request to except a control, and the verdict on it.</summary>
public sealed record ExceptionRequest(string Id, string ControlId, WorkforceRole Requester, string Reason);

public sealed record ExceptionApproval(string ExceptionId, WorkforceRole Approver, bool Granted, string Reason);
