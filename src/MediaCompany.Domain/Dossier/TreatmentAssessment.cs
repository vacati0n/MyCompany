namespace MediaCompany.Domain.Dossier;

/// <summary>
/// The nine adult-framing treatment conditions, carried from the niche analysis as K-1 to K-9 and
/// recorded as gate conditions rather than guidance (fact F-016, constraint C-007).
///
/// Each condition names what it RESISTS, not merely what it omits. The subject's default styling
/// is cheerful, so a condition satisfied by omission is a condition that drifts; the verdict
/// carries the evidence that resists it.
/// </summary>
public enum TreatmentCondition
{
    /// <summary>K-1: no anthropomorphism — no named individuals, invented personalities or dialogue.</summary>
    NoAnthropomorphism = 1,

    /// <summary>K-2: scientific register throughout — binomial nomenclature, quantified claims, named attribution.</summary>
    ScientificRegister = 2,

    /// <summary>K-3: no cartoon or illustrative styling — data-visualisation idiom, not illustration idiom.</summary>
    NoCartoonStyling = 3,

    /// <summary>K-4: no songs, nursery-register music or sound-effect comedy; documentary-register score only.</summary>
    DocumentaryScoreOnly = 4,

    /// <summary>K-5: metadata discipline — no child-directed terms in title, description or tags.</summary>
    MetadataDiscipline = 5,

    /// <summary>K-6: no play-acting framing, games, quizzes or "can you guess".</summary>
    NoPlayActingFraming = 6,

    /// <summary>K-7: at least one adult-signalling element — mortality, physiology, statistics on screen.</summary>
    AdultSignallingElement = 7,

    /// <summary>
    /// K-8: monitor the age-demographic report from month one. Observable only after publication,
    /// so it resolves as a recorded standing obligation naming the parameter it waits on
    /// (assumption A-006), never as an observed pass.
    /// </summary>
    AudienceShareMonitoring = 8,

    /// <summary>K-9: set the audience designation deliberately and per item, with reasoning recorded.</summary>
    DeliberateAudienceDesignation = 9,
}

/// <summary>
/// How a treatment condition resolved. <see cref="StandingObligation"/> exists for K-8 alone: it
/// is the honest resolution for a condition whose evidence surface opens only after publication,
/// and it does not block. <see cref="Unresolved"/> and <see cref="Failed"/> both block.
/// </summary>
public enum TreatmentVerdictKind
{
    Unresolved = 0,
    Passed = 1,
    Failed = 2,
    StandingObligation = 3,
}

/// <summary>One condition's verdict, with the evidence that resists the drift it names.</summary>
public sealed record TreatmentVerdict
{
    public required TreatmentCondition Condition { get; init; }
    public required TreatmentVerdictKind Kind { get; init; }

    /// <summary>What in the item resists this condition's failure mode. Never "not present".</summary>
    public required string Evidence { get; init; }

    public required DateTimeOffset RecordedAt { get; init; }

    /// <summary>For a standing obligation, the parameter it waits on. Required for K-8.</summary>
    public string? AwaitedParameter { get; init; }

    /// <summary>A verdict blocks unless it passed, or is the standing obligation K-8 admits.</summary>
    public bool Blocks =>
        Kind switch
        {
            TreatmentVerdictKind.Passed => false,
            TreatmentVerdictKind.StandingObligation =>
                Condition != TreatmentCondition.AudienceShareMonitoring || string.IsNullOrWhiteSpace(AwaitedParameter),
            _ => true,
        };
}

/// <summary>
/// The per-item audience designation with its mandatory reasoning (decision D-013, A-005). A
/// channel-level default has no representation in this model: <see cref="Reasoning"/> is required
/// and <see cref="FromChannelDefault"/> records the one thing the criterion refuses.
/// </summary>
public sealed record AudienceDesignation
{
    public required bool MadeForKids { get; init; }
    public required string Reasoning { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }
    public bool FromChannelDefault { get; init; }

    public bool IsValid => !FromChannelDefault && !string.IsNullOrWhiteSpace(Reasoning);
}
