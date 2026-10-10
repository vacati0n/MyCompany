using MediaCompany.Domain.Work;

namespace MediaCompany.Domain.Production;

/// <summary>
/// What a store is for (the production change, decision D-005 of its design). Written once per store; an
/// absent designation refuses both production modes.
/// </summary>
public enum StoreDesignation
{
    /// <summary>A self-dropping demonstration store. Every figure read from it is labelled demonstration.</summary>
    Demonstration = 1,

    /// <summary>The company's own store, the only one a metered run may book into.</summary>
    Company = 2,
}

/// <summary>How one production run reaches its vendors (the production change, decision D-015 of its design).</summary>
public enum ProductionMode
{
    /// <summary>The demonstration composition: fake providers, no network client, no credential read.</summary>
    Fake = 1,

    /// <summary>The plan, its estimate and the cap are printed; no call is made and no file is written.</summary>
    PlanOnly = 2,

    /// <summary>The owner's billed vendors, in the company store only.</summary>
    Metered = 3,
}

/// <summary>What a recorded artifact is to the item (the production change, decision D-014 of its design).</summary>
public enum ArtifactRole
{
    GraphicStill = 1,
    NarrationPart = 2,
    Narration = 3,
    ThumbnailCandidate = 4,
    ThumbnailPlaceholder = 5,
    ClipPlaceholder = 6,
    RenderedVideo = 7,
}

/// <summary>
/// One artifact, recorded only after its file was completely written, promoted and re-read (the production
/// change, decision D-014 of its design): its relative path under the output root, the length and hash of the
/// STORED bytes, its measured duration where it is media, and the datastore's instant.
/// </summary>
public sealed record ArtifactRecord
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required ProductionStage Stage { get; init; }
    public required ArtifactRole Role { get; init; }

    /// <summary>The identifier of the material entry the artifact implements, as GFX-01 or a part ordinal.</summary>
    public required string Implements { get; init; }

    /// <summary>The path relative to the configured output root, with forward slashes.</summary>
    public required string RelativePath { get; init; }

    public required long Length { get; init; }

    /// <summary>Lowercase hexadecimal SHA-256 of the stored file, recomputed from the file after it was promoted.</summary>
    public required string Sha256 { get; init; }

    /// <summary>The duration measured by probing the stored file, or null for a still.</summary>
    public TimeSpan? MeasuredDuration { get; init; }

    /// <summary>The datastore's instant; null on a draft.</summary>
    public DateTimeOffset? RecordedAt { get; init; }
}

/// <summary>One stage outcome of a production version, as the datastore stamped it.</summary>
public sealed record ProducedStageOutcome(ProductionStage Stage, StageOutcome Outcome, string Summary, DateTimeOffset RecordedAt);
