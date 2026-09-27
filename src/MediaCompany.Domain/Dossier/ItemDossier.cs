using MediaCompany.Domain.Production;

namespace MediaCompany.Domain.Dossier;

/// <summary>
/// The item-scoped, version-scoped evidence record (module M-020, decision D-002).
///
/// Every blocking predicate the publish-ready transition evaluates reads this one record, so a
/// blocking condition is a property of the item rather than of the caller that produced it. That
/// singularity is the whole reason the record exists: with evidence distributed across the
/// modules that produce it, an out-of-order supply audit or an unassessed clip is discoverable
/// but not preventable, and the criteria compare recorded dates (constraint C-015).
///
/// Every collection is empty rather than absent by default. An incomplete dossier must be
/// representable so that it resolves as refusing, rather than being rejected at construction and
/// never reaching the predicate at all.
/// </summary>
public sealed record ItemDossier
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }

    /// <summary>One outcome per production stage. A stage absent here is a refusal (C-006).</summary>
    public IReadOnlyList<StageEvidence> Stages { get; init; } = [];

    /// <summary>The nine adult-framing verdicts (C-007).</summary>
    public IReadOnlyList<TreatmentVerdict> Treatment { get; init; } = [];

    public AudienceDesignation? Audience { get; init; }

    /// <summary>One entry per visual. A visual resolving to neither admissible source cannot be used (C-008).</summary>
    public IReadOnlyList<VisualProvenance> Visuals { get; init; } = [];

    /// <summary>One entry per clip, recorded before the clip was bound to the cut (C-015).</summary>
    public IReadOnlyList<ClipOriginAssessment> ClipOrigins { get; init; } = [];

    /// <summary>One entry per factual claim (C-011).</summary>
    public IReadOnlyList<ClaimAttribution> Claims { get; init; } = [];

    /// <summary>One entry per subject the script calls for, dated before the script commit (C-015, C-016).</summary>
    public IReadOnlyList<SupplyAuditEntry> SupplyAudit { get; init; } = [];

    /// <summary>The five compliance determinations (C-012).</summary>
    public IReadOnlyList<DeterminationResolution> Determinations { get; init; } = [];

    /// <summary>The finished runtime the terminal predicate reads against the format's floor.</summary>
    public TimeSpan? Runtime { get; init; }

    /// <summary>The metadata surfaces screened against the barred-term list.</summary>
    public ItemMetadata? Metadata { get; init; }

    /// <summary>The originality assessment made with the licensed footage set aside (A-010).</summary>
    public FootageRemovalAssessment? Originality { get; init; }

    public StageEvidence? StageFor(ProductionStage stage) =>
        Stages.FirstOrDefault(s => s.Stage == stage);

    public DeterminationResolution? DeterminationFor(ComplianceDetermination determination) =>
        Determinations.FirstOrDefault(d => d.Determination == determination);
}

/// <summary>The recorded outcome of one production stage.</summary>
public sealed record StageEvidence
{
    public required ProductionStage Stage { get; init; }
    public required Work.StageOutcome Outcome { get; init; }
    public required string Summary { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public string? EvidenceReference { get; init; }
}

/// <summary>The title, description and tag set the barred-term screen reads.</summary>
public sealed record ItemMetadata
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public IReadOnlyList<string> Tags { get; init; } = [];
}
