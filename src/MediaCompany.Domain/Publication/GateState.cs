using System.Collections.Frozen;

namespace MediaCompany.Domain.Publication;

/// <summary>
/// The publication gate states. <see cref="AwaitingOwnerApproval"/> is a state in this
/// transition table and is absent from the configuration surface, per decision D-007 and
/// constraint C-010: it is not a setting, so it cannot be configured away. Removing it is a
/// code change the Review Gate sees (risk R-012).
/// </summary>
public enum GateState
{
    Draft = 1,
    AwaitingRightsCheck = 2,
    AwaitingOwnerApproval = 3,
    SentBack = 4,
    Approved = 5,
    Published = 6,
    Withdrawn = 7,
}

/// <summary>
/// The transition table, layer three of decision D-003. <see cref="GateState.Published"/> is
/// reachable only from <see cref="GateState.Approved"/>, which is reachable only from
/// <see cref="GateState.AwaitingOwnerApproval"/>. There is no bypass transition, and this wave
/// delivers no route from <see cref="GateState.Published"/> to a publishing platform: exclusion
/// X-001 removes the upload path and leaves the gate's refusal semantics only.
/// </summary>
public static class GateTransitionTable
{
    private static readonly FrozenDictionary<GateState, FrozenSet<GateState>> Allowed =
        new Dictionary<GateState, FrozenSet<GateState>>
        {
            [GateState.Draft] = new[] { GateState.AwaitingRightsCheck, GateState.Withdrawn }.ToFrozenSet(),
            [GateState.AwaitingRightsCheck] = new[] { GateState.AwaitingOwnerApproval, GateState.SentBack, GateState.Withdrawn }.ToFrozenSet(),
            [GateState.AwaitingOwnerApproval] = new[] { GateState.Approved, GateState.SentBack, GateState.Withdrawn }.ToFrozenSet(),
            [GateState.SentBack] = new[] { GateState.Draft, GateState.Withdrawn }.ToFrozenSet(),
            [GateState.Approved] = new[] { GateState.Published, GateState.Withdrawn }.ToFrozenSet(),
            [GateState.Published] = FrozenSet<GateState>.Empty,
            [GateState.Withdrawn] = FrozenSet<GateState>.Empty,
        }.ToFrozenDictionary();

    public static bool IsAllowed(GateState from, GateState to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlySet<GateState> From(GateState state) =>
        Allowed.TryGetValue(state, out var targets) ? targets : FrozenSet<GateState>.Empty;

    /// <summary>
    /// Every path from <see cref="GateState.Draft"/> to <see cref="GateState.Published"/>, used
    /// by the demonstration that no path skips owner approval.
    /// </summary>
    public static IReadOnlyList<IReadOnlyList<GateState>> PathsToPublished()
    {
        var results = new List<IReadOnlyList<GateState>>();
        Walk(GateState.Draft, new List<GateState> { GateState.Draft }, results);
        return results;
    }

    private static void Walk(GateState current, List<GateState> path, List<IReadOnlyList<GateState>> results)
    {
        if (current == GateState.Published)
        {
            results.Add(path.ToArray());
            return;
        }

        foreach (var next in From(current))
        {
            if (path.Contains(next))
            {
                continue;
            }

            path.Add(next);
            Walk(next, path, results);
            path.RemoveAt(path.Count - 1);
        }
    }
}
