namespace MediaCompany.Domain.Publication;

/// <summary>
/// The dispatch record (module M-023), keyed on item and item version.
///
/// Exactly one of these exists per item version, enforced by a uniqueness constraint in the
/// datastore rather than by an application-side check: a duplicate is IMPOSSIBLE rather than
/// detectable. It is written inside the transaction that writes the gate state change and the
/// queue entry, so the three commit together or none of them does.
/// </summary>
public sealed record DispatchRecord
{
    public required DispatchKey Key { get; init; }
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required string DestinationCanonical { get; init; }
    public required DateTimeOffset PlannedAt { get; init; }
    public required string MetadataAndSettingsDigest { get; init; }
    public required DateTimeOffset ComposedAt { get; init; }
}

/// <summary>
/// One publication attempt, REFUSED ATTEMPTS INCLUDED (module M-023).
///
/// The five audit answers are fields rather than a reconstruction over the append-only record,
/// because the question is asked years later and a reconstruction depends on the configuration
/// that produced it still being readable. A refused attempt carries all five exactly as an
/// unrefused one does — the refused case is the one most likely to be recorded thinly, and it is
/// the one an auditor asks about.
/// </summary>
public sealed record AttemptRecord
{
    public AttemptRecord(
        Guid attemptId,
        DispatchKey key,
        ItemId item,
        ItemVersion version,
        string destinationCanonical,
        DateTimeOffset attemptedAt,
        string metadataAndSettingsDigest,
        string approvedBy,
        bool refused,
        string refusalReason,
        string refusalDetail)
    {
        if (string.IsNullOrWhiteSpace(destinationCanonical))
        {
            throw new ArgumentException(
                "Answer two: an attempt names the destination it was made to.", nameof(destinationCanonical));
        }

        if (string.IsNullOrWhiteSpace(metadataAndSettingsDigest))
        {
            throw new ArgumentException(
                "Answer four: an attempt names the metadata and settings it carried.",
                nameof(metadataAndSettingsDigest));
        }

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new ArgumentException(
                "Answer five: an attempt names whose approval it rested on, or that it rested on none.",
                nameof(approvedBy));
        }

        if (refused && string.IsNullOrWhiteSpace(refusalReason))
        {
            throw new ArgumentException(
                "A refused attempt names the condition it failed.", nameof(refusalReason));
        }

        AttemptId = attemptId;
        Key = key;
        Item = item;
        Version = version;
        DestinationCanonical = destinationCanonical;
        AttemptedAt = attemptedAt;
        MetadataAndSettingsDigest = metadataAndSettingsDigest;
        ApprovedBy = approvedBy;
        Refused = refused;
        RefusalReason = refusalReason;
        RefusalDetail = refusalDetail;
    }

    public Guid AttemptId { get; }
    public DispatchKey Key { get; }

    /// <summary>Answer one, part one: what item was attempted.</summary>
    public ItemId Item { get; }

    /// <summary>Answer one, part two: which exact version.</summary>
    public ItemVersion Version { get; }

    /// <summary>Answer two: to which destination.</summary>
    public string DestinationCanonical { get; }

    /// <summary>Answer three: when.</summary>
    public DateTimeOffset AttemptedAt { get; }

    /// <summary>Answer four: with which metadata and settings.</summary>
    public string MetadataAndSettingsDigest { get; }

    /// <summary>Answer five: on whose approval.</summary>
    public string ApprovedBy { get; }

    public bool Refused { get; }

    /// <summary>The named condition the attempt failed. Empty for an unrefused attempt.</summary>
    public string RefusalReason { get; }

    public string RefusalDetail { get; }

    /// <summary>
    /// Whether this record answers all five audit questions. Used by the verification that every
    /// recorded attempt, refused ones included, is answerable.
    /// </summary>
    public bool AnswersAllFive =>
        Item.Value != Guid.Empty
        && !string.IsNullOrWhiteSpace(DestinationCanonical)
        && AttemptedAt != default
        && !string.IsNullOrWhiteSpace(MetadataAndSettingsDigest)
        && !string.IsNullOrWhiteSpace(ApprovedBy);
}
