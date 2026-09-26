namespace MediaCompany.Domain.Work;

/// <summary>The lifecycle positions a unit of production work passes (acceptance criterion AC-022).</summary>
public enum LifecyclePosition
{
    Queued = 1,
    Research = 2,
    Script = 3,
    AssetGathering = 4,
    Assembly = 5,
    RightsCheck = 6,
    OwnerReview = 7,
    Completed = 8,
    Abandoned = 9,
}

/// <summary>The outcome recorded at each stage. No stage ends without one.</summary>
public enum StageOutcome
{
    Pending = 1,
    Succeeded = 2,
    Retried = 3,
    Escalated = 4,
    Failed = 5,
    Held = 6,
}

/// <summary>
/// The claim state of a job in the durable queue. Under the selected stack the queue is a
/// transactional claim table in the same datastore as the state, so the state change and the
/// queue entry commit together and exactly-once under constraint C-004 needs no outbox.
/// </summary>
public enum ClaimState
{
    Ready = 1,
    Claimed = 2,
    Done = 3,
    Dead = 4,
}

/// <summary>One unit of production work.</summary>
public sealed record Job
{
    public required JobId Id { get; init; }
    public required ItemId Item { get; init; }
    public required ChannelId Channel { get; init; }
    public required string Workflow { get; init; }
    public required LifecyclePosition Position { get; init; }
    public required ClaimState ClaimState { get; init; }
    public required DateTimeOffset AvailableAt { get; init; }
    public string? ClaimedBy { get; init; }
    public DateTimeOffset? LeaseExpiresAt { get; init; }
}

/// <summary>One stage of one job, with its outcome, its attempt count and its escalation state.</summary>
public sealed record JobStage
{
    public required JobId Job { get; init; }
    public required LifecyclePosition Position { get; init; }
    public required StageOutcome Outcome { get; init; }
    public required int Attempts { get; init; }
    public required bool Escalated { get; init; }
    public required DateTimeOffset EnteredAt { get; init; }
    public DateTimeOffset? LeftAt { get; init; }
    public string? FailureReason { get; init; }
}

/// <summary>
/// The retry, backoff and escalation policy under which no failure ends in neither
/// (acceptance criterion AC-023).
/// </summary>
public sealed record FailurePolicy
{
    public required int MaxAttempts { get; init; }
    public required TimeSpan InitialBackoff { get; init; }
    public required double BackoffMultiplier { get; init; }
    public required TimeSpan MaxBackoff { get; init; }

    public static FailurePolicy Default { get; } = new()
    {
        MaxAttempts = 3,
        InitialBackoff = TimeSpan.FromSeconds(30),
        BackoffMultiplier = 2.0,
        MaxBackoff = TimeSpan.FromMinutes(15),
    };
}

/// <summary>A workflow definition: its ordered stages and the failure policy they run under.</summary>
public sealed record WorkflowDefinition(string Name, IReadOnlyList<LifecyclePosition> Stages, FailurePolicy FailurePolicy);
