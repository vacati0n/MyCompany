namespace MediaCompany.Domain.Work;

/// <summary>
/// The names of the append-only entries the work lifecycle writes, declared ONCE.
///
/// The lifecycle service and the dispatch service write these, and the throughput reader counts
/// them. Declaring them in one place is what keeps a count honest: a reader coded against a name
/// the writers do not share would count zero where events occurred, and that zero would read as
/// an observation rather than as a defect. A renamed action is a change to this one declaration,
/// which every writer and the reader pick up together.
/// </summary>
public static class LifecycleActions
{
    /// <summary>A unit entered the ready queue at its workflow's first position.</summary>
    public const string Enqueued = "job.enqueued";

    /// <summary>A ready unit was claimed, leaving the ready queue.</summary>
    public const string Claimed = "job.claimed";

    /// <summary>A stage succeeded and the unit returned to the ready queue at the next position.</summary>
    public const string StageSucceeded = "job.stage-succeeded";

    /// <summary>A unit finished the last position its workflow declares and reached the terminal claim state.</summary>
    public const string Completed = "job.completed";

    /// <summary>A stage attempt failed and the unit was released to the ready queue for retry.</summary>
    public const string StageRetried = "job.stage-retried";

    /// <summary>A stage attempt failed and the unit was escalated to the dead claim state.</summary>
    public const string StageEscalated = "job.stage-escalated";

    /// <summary>Every lifecycle action, in the order a unit can meet them.</summary>
    public static IReadOnlyList<string> All { get; } =
        [Enqueued, Claimed, StageSucceeded, Completed, StageRetried, StageEscalated];

    /// <summary>
    /// The subject every lifecycle entry names, declared ONCE beside the action names (the
    /// multi-channel change). The lifecycle service and the dispatch service write it, and the
    /// channel partition of the throughput reading reads the work unit back out of it with
    /// <see cref="UnitSubjectPattern"/> and reaches the unit's channel through the unit's own
    /// channel reference, so no entry needs a channel column of its own.
    /// </summary>
    public static string UnitSubject(JobId job, ItemId item) => $"job:{job.Value:D} item:{item.Value:D}";

    /// <summary>
    /// The pattern the work unit is read back out of a lifecycle entry's subject with: its first
    /// capture group is the unit's identifier. An entry whose subject does not match is reported as
    /// unattributed rather than dropped.
    /// </summary>
    public const string UnitSubjectPattern =
        "^job:([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}) item:";
}
