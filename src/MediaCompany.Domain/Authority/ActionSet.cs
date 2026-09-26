using System.Collections.Frozen;

namespace MediaCompany.Domain.Authority;

/// <summary>
/// The role-to-action assignment, layer one of the four independent structural layers of
/// decision D-003. It is a frozen closed mapping rather than a configurable policy: the
/// copyright role holds the block actions and does not hold <see cref="ActionKind.Publish"/>,
/// and the publishing role's set is disjoint on every Block action, so neither "copyright
/// releases" nor "publisher overrides a block" is an expressible action.
/// </summary>
public static class ActionSet
{
    private static readonly FrozenDictionary<WorkforceRole, FrozenSet<ActionKind>> Assignments =
        new Dictionary<WorkforceRole, FrozenSet<ActionKind>>
        {
            [WorkforceRole.Owner] = new[]
            {
                ActionKind.ApprovalDecide,
                ActionKind.BudgetSet,
                ActionKind.ControlException,
                ActionKind.ReportRead,
            }.ToFrozenSet(),

            // Can block publication; holds no action that releases (acceptance criterion AC-010).
            [WorkforceRole.Copyright] = new[]
            {
                ActionKind.BlockPlace,
                ActionKind.BlockClear,
                ActionKind.AssetVerify,
                ActionKind.ReportRead,
            }.ToFrozenSet(),

            // Disjoint from every Block action; cannot override a block (acceptance criterion AC-011).
            [WorkforceRole.Publisher] = new[]
            {
                ActionKind.Publish,
                ActionKind.ReleaseCredentialRequest,
                ActionKind.ApprovalPresent,
                ActionKind.ReportRead,
            }.ToFrozenSet(),

            [WorkforceRole.Producer] = new[]
            {
                ActionKind.AssetRecord,
                ActionKind.ApprovalPresent,
                ActionKind.JobDispatch,
                ActionKind.ReportRead,
            }.ToFrozenSet(),

            [WorkforceRole.Researcher] = new[]
            {
                ActionKind.AssetRecord,
                ActionKind.JobDispatch,
                ActionKind.ReportRead,
            }.ToFrozenSet(),

            [WorkforceRole.Accountant] = new[]
            {
                ActionKind.CostDecisionRecord,
                ActionKind.ReportRead,
            }.ToFrozenSet(),

            [WorkforceRole.Operator] = new[]
            {
                ActionKind.ConfigurationChange,
                ActionKind.RouteAdmit,
                ActionKind.JobDispatch,
                ActionKind.ReportRead,
            }.ToFrozenSet(),
        }.ToFrozenDictionary();

    /// <summary>The actions a role holds. A role outside the closed set holds none.</summary>
    public static IReadOnlySet<ActionKind> For(WorkforceRole role) =>
        Assignments.TryGetValue(role, out var actions) ? actions : FrozenSet<ActionKind>.Empty;

    public static bool Holds(WorkforceRole role, ActionKind action) => For(role).Contains(action);

    public static IEnumerable<WorkforceRole> Roles => Assignments.Keys;
}
