namespace MediaCompany.Domain.Dossier;

/// <summary>
/// The two admissible visual sources, and the one inadmissible kind the model must still be able
/// to represent so that it can resolve as refused (constraint C-008).
/// </summary>
public enum VisualSource
{
    /// <summary>Unset. Resolves to neither admissible source, so the visual cannot be used.</summary>
    Unresolved = 0,

    /// <summary>A licensed stock asset carrying a recorded licence.</summary>
    LicensedStock = 1,

    /// <summary>An original graphic produced for this item.</summary>
    OriginalGraphic = 2,

    /// <summary>A generated cutaway. Admissible only with a recorded reason (C-009).</summary>
    GeneratedCutaway = 3,

    /// <summary>A third-party clip, on-location footage or presenter-on-camera footage. Never admissible.</summary>
    Inadmissible = 4,
}

/// <summary>
/// One visual's provenance. A visual resolving to neither admissible source cannot be used, which
/// is what <see cref="IsAdmissible"/> decides rather than a caller's discipline.
/// </summary>
public sealed record VisualProvenance
{
    public required string VisualReference { get; init; }
    public required VisualSource Source { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }

    /// <summary>Required when the source is a licensed stock asset.</summary>
    public AssetId? Asset { get; init; }

    /// <summary>Required when the source is a licensed stock asset.</summary>
    public string? LicenceReference { get; init; }

    /// <summary>
    /// Required of every generated cutaway: the recorded reason that no licensed and no original
    /// asset served, naming the alternatives considered (C-009, A-009).
    /// </summary>
    public GeneratedCutawayReason? CutawayReason { get; init; }

    public bool IsAdmissible => Source switch
    {
        VisualSource.LicensedStock => Asset is not null && !string.IsNullOrWhiteSpace(LicenceReference),
        VisualSource.OriginalGraphic => true,
        VisualSource.GeneratedCutaway => CutawayReason is not null && CutawayReason.IsComplete,
        _ => false,
    };
}

/// <summary>
/// The recorded reason a generated cutaway carries. Both fields are required: a reason that does
/// not name the alternatives considered is an assertion rather than a record.
/// </summary>
public sealed record GeneratedCutawayReason
{
    public required string WhyNoLicensedAssetServed { get; init; }
    public required IReadOnlyList<string> AlternativesConsidered { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(WhyNoLicensedAssetServed) && AlternativesConsidered.Count > 0;
}

/// <summary>What an origin assessment established about a clip.</summary>
public enum ClipOrigin
{
    /// <summary>Not assessed. A clip in this state cannot enter a cut (C-015).</summary>
    Unassessed = 0,

    /// <summary>Established as recorded footage of a real animal, place or event.</summary>
    RecordedFootage = 1,

    /// <summary>Established as generated, composited or green-screen material.</summary>
    GeneratedOrComposited = 2,

    /// <summary>A name collision: the clip does not depict the subject the term named.</summary>
    NameCollision = 3,

    /// <summary>Assessed, and the origin could not be established either way.</summary>
    Indeterminate = 4,
}

/// <summary>
/// One clip's origin assessment, recorded before the clip was used. The criterion compares
/// <see cref="AssessedAt"/> with the use, so an assessment recorded afterwards is unsatisfiable
/// for the clip it concerns (constraint C-015) — which is why the date is on the record and not
/// inferred from write order.
///
/// Library counts for scarce subjects are known to carry generated assets, green-screen
/// composites and name collisions (fact F-018), so an unassessed clip is a factual-integrity
/// exposure on a science item rather than an aesthetic one.
/// </summary>
public sealed record ClipOriginAssessment
{
    public required string ClipReference { get; init; }
    public required ClipOrigin Origin { get; init; }
    public required string Basis { get; init; }
    public required DateTimeOffset AssessedAt { get; init; }

    /// <summary>Set when the clip was bound into the cut. Null while it has not been used.</summary>
    public DateTimeOffset? UsedAt { get; init; }

    /// <summary>Whether a clip not established as recorded footage was rejected rather than used.</summary>
    public bool Rejected { get; init; }

    /// <summary>The assessment precedes the use, or the clip was never used.</summary>
    public bool OrderingHolds => UsedAt is null || AssessedAt <= UsedAt.Value;

    /// <summary>
    /// A clip may be bound into a cut only when it was assessed first and established as recorded
    /// footage. Anything else is rejected, or carried into the synthetic-media determination.
    /// </summary>
    public bool AdmissibleForCut =>
        Origin == ClipOrigin.RecordedFootage && OrderingHolds && !Rejected;
}
