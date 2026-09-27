namespace MediaCompany.Domain.Authority;

/// <summary>
/// The closed enumerated action set of decision D-003, layer one. Authority is a closed set held
/// as data, so an absent permission is structurally absent rather than merely unexercised: the
/// publishing role's set is disjoint on every Block action, so overriding a block is not an
/// expressible action rather than a forbidden one.
/// </summary>
public enum ActionKind
{
    BlockPlace = 1,
    BlockClear = 2,
    AssetVerify = 3,
    AssetRecord = 4,
    ApprovalPresent = 5,
    ApprovalDecide = 6,
    Publish = 7,
    ReleaseCredentialRequest = 8,
    BudgetSet = 9,
    CostDecisionRecord = 10,
    ControlException = 11,
    ConfigurationChange = 12,
    RouteAdmit = 13,
    JobDispatch = 14,
    ReportRead = 15,

    /// <summary>
    /// Composing, validating and persisting a publication dispatch descriptor.
    ///
    /// STRUCTURAL ABSENCE THREE. This set is closed and holds NO action naming a destination
    /// egress, so an authority evaluation permitting one is not expressible: there is no action
    /// value to hold, to grant or to check. The dispatch action reaches the stopping surface and
    /// no further.
    /// </summary>
    PublicationDispatch = 16,
}

/// <summary>
/// The workforce roles this wave operates. The set carries no human-contributor role, per the
/// authority model settled at plan task T-012; open question Q-005 carries whether one is added.
/// </summary>
public enum WorkforceRole
{
    Owner = 1,
    Copyright = 2,
    Publisher = 3,
    Producer = 4,
    Researcher = 5,
    Accountant = 6,
    Operator = 7,
}
