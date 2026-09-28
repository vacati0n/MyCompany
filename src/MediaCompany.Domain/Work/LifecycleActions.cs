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
}
