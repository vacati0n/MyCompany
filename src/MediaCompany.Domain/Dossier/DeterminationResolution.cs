namespace MediaCompany.Domain.Dossier;

/// <summary>
/// The five compliance determinations an item must resolve (constraint C-012, criterion A-019).
/// The set is closed, so a sixth is a code change the Review Gate sees and a missing one is a
/// refusal rather than an omission.
/// </summary>
public enum ComplianceDetermination
{
    /// <summary>Whether the item's argument holds with its licensed footage set aside.</summary>
    OriginalitySelfAssessment = 1,

    /// <summary>Exact and near-duplicate detection, with the distance at which it was judged distinct.</summary>
    DuplicateDetection = 2,

    /// <summary>Whether each generated or altered element requires disclosure, and on what basis.</summary>
    SyntheticMediaDisclosure = 3,

    /// <summary>Whether an affiliate link or a paid placement is present, and the disclosure that follows.</summary>
    AffiliateAndPaidPromotion = 4,

    /// <summary>The self-rating derived from the item's own classification against the fourteen categories.</summary>
    AdvertiserSuitability = 5,
}

/// <summary>
/// The three resolutions a determination admits (decision D-004). Only <see cref="Unresolved"/>
/// refuses the terminal state.
///
/// <see cref="RecordedNotEvidenceable"/> is a first-class resolution, not a failure dressed up as
/// one. Some determinations have an evidence surface that opens only at upload, and an honest
/// negative answer recorded with its reason is the outcome this model is built for. It carries a
/// MANDATORY reason and a mandatory statement of what would make it evidenceable, so that it can
/// never become a quiet way of passing something nobody checked.
/// </summary>
public enum DeterminationOutcome
{
    /// <summary>Neither evidenced nor recorded as not evidenceable. Refuses the terminal state.</summary>
    Unresolved = 0,

    /// <summary>Resolved by recorded evidence.</summary>
    Evidenced = 1,

    /// <summary>Resolved by a recorded statement that it cannot be evidenced, with its reason.</summary>
    RecordedNotEvidenceable = 2,

    /// <summary>Assessed and failed. Refuses the terminal state.</summary>
    Failed = 3,
}

/// <summary>
/// One determination's resolution, with the policy text it rests on and that text's verification
/// date. The date is carried because a determination answered against policy text nobody dated
/// cannot be re-judged after the policy changes (NFR-5).
/// </summary>
public sealed record DeterminationResolution
{
    public required ComplianceDetermination Determination { get; init; }
    public required DeterminationOutcome Outcome { get; init; }
    public required DateTimeOffset ResolvedAt { get; init; }

    /// <summary>The recorded evidence. Required when the outcome is <see cref="DeterminationOutcome.Evidenced"/>.</summary>
    public string? Evidence { get; init; }

    /// <summary>Mandatory when the outcome is <see cref="DeterminationOutcome.RecordedNotEvidenceable"/>.</summary>
    public string? NotEvidenceableReason { get; init; }

    /// <summary>Mandatory when the outcome is <see cref="DeterminationOutcome.RecordedNotEvidenceable"/>.</summary>
    public string? WhatWouldMakeItEvidenceable { get; init; }

    /// <summary>The policy text this rests on.</summary>
    public string? PolicyReference { get; init; }

    /// <summary>The date that policy text was verified.</summary>
    public DateOnly? PolicyVerifiedOn { get; init; }

    /// <summary>
    /// Whether the resolution is well-formed for the outcome it claims. A not-evidenceable
    /// resolution missing its reason or its remedy is not a resolution; it is an unresolved
    /// determination wearing a resolution's name, and it blocks.
    /// </summary>
    public bool IsWellFormed => Outcome switch
    {
        DeterminationOutcome.Evidenced => !string.IsNullOrWhiteSpace(Evidence),
        DeterminationOutcome.RecordedNotEvidenceable =>
            !string.IsNullOrWhiteSpace(NotEvidenceableReason) &&
            !string.IsNullOrWhiteSpace(WhatWouldMakeItEvidenceable),
        _ => false,
    };

    /// <summary>Only an unresolved, failing or malformed determination blocks (D-004).</summary>
    public bool Blocks => !IsWellFormed;
}

/// <summary>
/// The duplicate result, stated against both the company's own record and sources outside it,
/// with the distance at which the item was judged sufficiently distinct (criterion A-021).
/// </summary>
public sealed record DuplicateResult
{
    public required bool DuplicateAgainstOwnCatalogue { get; init; }
    public required bool DuplicateAgainstExternalSources { get; init; }

    /// <summary>The recorded distance. Absent means the result was stated without one.</summary>
    public required double? Distance { get; init; }

    public required string Method { get; init; }
    public required DateTimeOffset ComputedAt { get; init; }

    public bool IsComplete => Distance is not null && !string.IsNullOrWhiteSpace(Method);
}

/// <summary>
/// The fourteen advertiser-friendly guideline categories. The self-rating is derived from a
/// classification against each of them; a fixed answer with no classification behind it resolves
/// as ABSENT rather than as a rating (criterion A-022), because repeated inaccurate self-rating
/// puts programme eligibility under review (fact F-020).
/// </summary>
public enum SuitabilityCategory
{
    InappropriateLanguage = 1,
    Violence = 2,
    AdultContent = 3,
    ShockingContent = 4,
    HarmfulActsAndUnreliableContent = 5,
    HatefulAndDerogatoryContent = 6,
    RecreationalDrugs = 7,
    FirearmsRelatedContent = 8,
    ControversialIssues = 9,
    SensitiveEvents = 10,
    EnablingDishonestBehaviour = 11,
    InappropriateForKidsAndFamilies = 12,
    IncendiaryAndDemeaning = 13,
    TobaccoRelatedContent = 14,
}

/// <summary>One category's classification against the item's own content.</summary>
public sealed record SuitabilityClassification
{
    public required SuitabilityCategory Category { get; init; }
    public required bool Present { get; init; }

    /// <summary>What in the item was classified. A classification with no basis is not one.</summary>
    public required string Basis { get; init; }

    public bool HasBasis => !string.IsNullOrWhiteSpace(Basis);
}

/// <summary>The three ad states a self-rating resolves to.</summary>
public enum AdSuitabilityRating
{
    /// <summary>No rating: no classification stands behind it (A-022).</summary>
    Absent = 0,
    FullAdRevenue = 1,
    LimitedAdRevenue = 2,
    NoAdRevenue = 3,
}

/// <summary>
/// One generated or altered element and whether disclosure is required of it (criterion A-023).
/// An unassessed element prevents the determination from resolving, which is why the element set
/// is enumerated rather than sampled.
/// </summary>
public sealed record SyntheticElementAssessment
{
    public required string ElementReference { get; init; }
    public required bool DisclosureRequired { get; init; }
    public required string Basis { get; init; }
    public required DateTimeOffset AssessedAt { get; init; }

    public bool IsAssessed => !string.IsNullOrWhiteSpace(Basis);
}
