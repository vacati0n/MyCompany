namespace MediaCompany.Domain.Audit;

/// <summary>
/// The retention class every entry carries from its first write. Assumption A-006 records that no
/// retention period is stated; carrying the class from the outset lets a policy arriving later
/// apply prospectively without altering entries an append-only store cannot correct in place
/// (risk R-015).
/// </summary>
public enum RetentionClass
{
    OperationalRecord = 1,
    FinancialRecord = 2,
    RightsRecord = 3,
    GovernanceRecord = 4,
}

/// <summary>
/// One entry of the company's recorded history. The entry is self-explaining: the permission
/// answer and the cost derivation are resolvable from the record with no reference outside it
/// (constraint C-006).
///
/// The record is append-only. An amendment attempt is refused and the refusal is itself recorded
/// as its own entry naming the entry it was attempted against (constraint C-014).
/// </summary>
public sealed record AuditEntry
{
    public required AuditEntryId Id { get; init; }
    public required string Actor { get; init; }
    public required string Action { get; init; }
    public required string Subject { get; init; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required string Reason { get; init; }
    public required string InputsReference { get; init; }
    public required string OutputsReference { get; init; }
    public required string Decision { get; init; }
    public required string CostReference { get; init; }
    public required string Risk { get; init; }
    public required RetentionClass RetentionClass { get; init; }

    /// <summary>The hash of the entry immediately preceding this one, or the empty string at the head.</summary>
    public required string PreviousEntryHash { get; init; }

    public required string EntryHash { get; init; }
}

/// <summary>Why an append or an amendment was refused.</summary>
public enum AuditRefusalReason
{
    AmendmentAttempted = 1,
    DeletionAttempted = 2,
    ChainBroken = 3,
}
