namespace MediaCompany.Domain.Production;

/// <summary>
/// The reviewable item material held beside a recorded package (the production change, decision D-011 of its
/// design). Every entry QUOTES the record it comes from, so a reviewer, and the loader, can check it against its
/// source; nothing here is invented. Production source holds no subject term and no item identifier: they reach a
/// run only through this material.
/// </summary>
public sealed record ItemMaterial
{
    public required string Item { get; init; }
    public required int PackageVersion { get; init; }
    public required string NarrationFile { get; init; }
    public required int NarrationCharacters { get; init; }
    public required QuotedRuntime SpecifiedRuntime { get; init; }
    public required QuotedTerms SubjectTerms { get; init; }
    public required IReadOnlyList<MaterialBeat> Beats { get; init; }
    public required IReadOnlyList<MaterialGraphic> Graphics { get; init; }
    public required IReadOnlyList<MaterialClip> Clips { get; init; }
    public required QuotedIdentifiers UnsourcedClips { get; init; }
    public required Quotation ClipPlaceholders { get; init; }
    public required IReadOnlyList<MaterialThumbnail> Thumbnails { get; init; }
}

/// <summary>A verbatim quotation of a recorded file, by its path relative to the repository.</summary>
public sealed record Quotation(string Source, string Quote);

/// <summary>The runtime the package SPECIFIES, which is a configured figure and never a measurement.</summary>
public sealed record QuotedRuntime(int Seconds, string Source, string Quote);

public sealed record QuotedTerms(string Source, IReadOnlyList<string> Terms);

public sealed record QuotedIdentifiers(IReadOnlyList<string> Ids, string Source, string Quote);

/// <summary>One beat: its number and title quoted from the script's structure row, and its opening sentence quoted from the narration.</summary>
public sealed record MaterialBeat(
    int Number, string Title, string StructureSource, string StructureQuote,
    string Opening, string OpeningSource, string CueSource, string CueQuote);

/// <summary>One graphic: its identifier, its beat, and the lines it displays, every line quoted from its record.</summary>
public sealed record MaterialGraphic(string Id, int Beat, string Source, string Quote, IReadOnlyList<string> Lines);

/// <summary>One clip position: its identifier, its beat and its label, quoted from its record.</summary>
public sealed record MaterialClip(string Id, int Beat, string Label, string Source, string Quote);

/// <summary>
/// One thumbnail candidate. Its alternative text is held only where the package records it; otherwise it is
/// RECORDED ABSENT and never invented. A placeholder candidate quotes the decision that made it one.
/// </summary>
public sealed record MaterialThumbnail(
    string Id, string Source, string Quote, IReadOnlyList<string> Lines, string? DisplayedText, string? AlternativeText,
    bool Placeholder, string? PlaceholderSource, string? PlaceholderQuote);
