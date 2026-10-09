using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Domain.Management;

// ---------------------------------------------------------------------------
// The AI-management change: the records the eighth schema resource adds, as the company record reader
// returns them (decisions D-006, D-007 and D-010 of its design). Each is a write-once row read back
// exactly as recorded; nothing here is a measurement, and nothing here changes a control.
// ---------------------------------------------------------------------------

/// <summary>Whether a register entry records a decision or a question.</summary>
public enum RegisterEntryKind
{
    Decision = 1,
    Question = 2,
}

/// <summary>
/// The status an entry was RECORDED with. "Superseded" is never recorded on an entry, because an entry is
/// written once: an entry is superseded when a later entry names it, which <see cref="RegisterEntry"/>
/// readers decide from the register as a whole.
/// </summary>
public enum RegisterEntryStatus
{
    Open = 1,
    Decided = 2,
}

/// <summary>
/// One entry of the open-decisions register, exactly as recorded: its identifier and statement, its kind
/// and recorded status, its owner and recorded date, the interim ruling that stands while it is open, its
/// decider and decision date where decided, the entry it supersedes where it supersedes one, and the
/// datastore's entry instant.
/// </summary>
public sealed record RegisterEntry
{
    public required string Identifier { get; init; }
    public required string Statement { get; init; }
    public required RegisterEntryKind Kind { get; init; }
    public required RegisterEntryStatus Status { get; init; }
    public required string Owner { get; init; }
    public required DateOnly RecordedOn { get; init; }
    public string? InterimRuling { get; init; }
    public string? DecidedBy { get; init; }
    public DateOnly? DecidedOn { get; init; }
    public string? Supersedes { get; init; }
    public required DateTimeOffset EnteredAt { get; init; }
}

/// <summary>One platform-policy statement as registered, after master plan section 29's platform-policy entity.</summary>
public sealed record PolicyStatementRecord
{
    public required string Identifier { get; init; }
    public required string Platform { get; init; }
    public required string Label { get; init; }
    public required string SourceReference { get; init; }
    public DateOnly? EffectiveOn { get; init; }
    public required DateTimeOffset RegisteredAt { get; init; }
}

/// <summary>One recorded re-verification of one statement, with the datastore's recording instant.</summary>
public sealed record ReverificationResult
{
    public required Guid Id { get; init; }
    public required string Statement { get; init; }
    public required DateOnly VerifiedOn { get; init; }
    public required bool Changed { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
}

/// <summary>
/// One held operation of the operation record, with its held-outcome row where one is recorded.
///
/// An operation held before the held-outcome record existed has no row: its reason, escalation instant,
/// hold timeout and escalation target are null, and only the free-text failure reason the operation row
/// carries is printed, as recorded text. Nothing is parsed out of it.
/// </summary>
public sealed record HeldOutcomeRecord
{
    public required OperationId Operation { get; init; }
    public required ChannelId Channel { get; init; }
    public required ItemId Item { get; init; }

    /// <summary>The operation's booked instant, the datastore's.</summary>
    public required DateTimeOffset HeldAt { get; init; }

    public RefusalReason? Reason { get; init; }
    public DateTimeOffset? EscalatesAt { get; init; }
    public TimeSpan? HoldTimeout { get; init; }
    public bool? EscalatesToOwner { get; init; }

    /// <summary>The failure reason the operation row carries, as recorded.</summary>
    public required string RecordedFailureReason { get; init; }

    /// <summary>Whether the held-outcome record holds a row for this operation.</summary>
    public bool HasHeldOutcomeRow => Reason is not null;

    /// <summary>Whether this held outcome is a deferral: at the 90 percent threshold, or behind an admission in progress.</summary>
    public bool IsDeferral => Reason is RefusalReason.DeferredAtThreshold or RefusalReason.MeteredAdmissionInProgress;
}
