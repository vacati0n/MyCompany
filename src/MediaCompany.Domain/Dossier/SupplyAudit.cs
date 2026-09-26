namespace MediaCompany.Domain.Dossier;

/// <summary>
/// Whether the library answered the term that was actually requested.
///
/// The committed library's search is fuzzy and spelling-corrected rather than literal. The
/// recorded proof is that the query `saola` returns a page whose own result line reads "181
/// results found for saona" — a Caribbean island — while the true count is zero; and that the
/// nonsense query `xyzzy monkey` returns 26 results, so a multi-word query is a relevance blend
/// rather than an intersection.
///
/// A total reported for a term the library substituted, spelling-corrected or matched loosely is
/// therefore NO COUNT, not a count (constraint C-016, criterion A-026). That is the whole purpose
/// of this type: it makes the difference between "answered literally" and "answered somehow" a
/// property of the record rather than of the reader's care.
/// </summary>
public enum TermFidelity
{
    /// <summary>Not established. Resolves as no count.</summary>
    Unknown = 0,

    /// <summary>
    /// A single, distinctive, unambiguous token the library echoed back unchanged. The only class
    /// that yields an admissible count.
    /// </summary>
    LiteralSingleToken = 1,

    /// <summary>The library echoed a different term than the one requested. No count.</summary>
    Substituted = 2,

    /// <summary>The library spelling-corrected the term to a different word. No count.</summary>
    SpellingCorrected = 3,

    /// <summary>
    /// A multi-word phrase. Its reported total is a relevance blend and an upper bound of unknown
    /// looseness, so it is inadmissible as a count however plausible the figure looks.
    /// </summary>
    MultiWordPhrase = 4,
}

/// <summary>Why a subject's count was not obtained. Every member names a real, distinct cause.</summary>
public enum CountUnobtainedReason
{
    None = 0,

    /// <summary>The term was not answered literally, so the reported total is not a count (C-016).</summary>
    TermNotAnsweredLiterally = 1,

    /// <summary>The library's search surface was not reachable — authentication, or a refused request.</summary>
    LibrarySurfaceUnreachable = 2,

    /// <summary>
    /// Supply can be established only by confirming clips by eye, and that confirmation has not
    /// been made. Applies to every behaviour named by a phrase.
    /// </summary>
    RequiresPerClipConfirmation = 3,

    /// <summary>The library answered literally and reported zero holdings. This IS a count, of zero.</summary>
    LibraryReportsNone = 4,
}

/// <summary>
/// One subject's supply position, recorded before the script commits (constraint C-015).
///
/// A count is present only where the library answered the term actually requested. Where it is
/// absent, <see cref="UnobtainedReason"/> names why and <see cref="WhatWouldObtainIt"/> names what
/// would obtain it. An absent count is never rendered as a zero, and a reported total for a
/// loosely matched term is never rendered as a count: both would be the same failure, which is
/// acting on a supply figure nobody established.
/// </summary>
public sealed record SupplyAuditEntry
{
    public required string RequestedTerm { get; init; }
    public required string Library { get; init; }
    public required TermFidelity Fidelity { get; init; }
    public required DateTimeOffset AuditedAt { get; init; }

    /// <summary>The term the library actually answered, as it echoed it back. Null when unreachable.</summary>
    public string? TermAnswered { get; init; }

    /// <summary>
    /// The count, present only when the library answered literally. Null is "not obtained", and is
    /// a different fact from zero.
    /// </summary>
    public int? Count { get; init; }

    public CountUnobtainedReason UnobtainedReason { get; init; } = CountUnobtainedReason.None;

    /// <summary>Mandatory wherever the count is absent (decision D-004's not-evidenceable form).</summary>
    public string? WhatWouldObtainIt { get; init; }

    /// <summary>
    /// A prior recorded observation carried with its date, cited as a prior observation and never
    /// re-presented as a fresh measurement.
    /// </summary>
    public PriorObservation? Prior { get; init; }

    /// <summary>Whether the subject was recorded unavailable, which changes the script (A-025).</summary>
    public bool RecordedUnavailable { get; init; }

    /// <summary>
    /// A count is admissible only from a literal single-token answer. This is the term-fidelity
    /// guard, and it is a property of the record rather than a check a caller may forget.
    /// </summary>
    public bool HasAdmissibleCount =>
        Fidelity == TermFidelity.LiteralSingleToken && Count is not null;

    /// <summary>
    /// An entry resolves when it either carries an admissible count or states why one was not
    /// obtained and what would obtain it. An entry that does neither leaves the subject unaudited.
    /// </summary>
    public bool IsResolved =>
        HasAdmissibleCount ||
        (UnobtainedReason != CountUnobtainedReason.None && !string.IsNullOrWhiteSpace(WhatWouldObtainIt));
}

/// <summary>
/// A count read on an earlier date, carried as a prior recorded observation. It is never a
/// substitute for this audit's own count: <see cref="SupplyAuditEntry.HasAdmissibleCount"/> does
/// not read it, and it carries its own admissibility so a prior that was itself inadmissible
/// cannot be laundered into evidence by age.
/// </summary>
public sealed record PriorObservation
{
    public required int ReportedTotal { get; init; }
    public required DateOnly ObservedOn { get; init; }
    public required string Source { get; init; }
    public required TermFidelity FidelityAtObservation { get; init; }

    /// <summary>True only where the prior read was itself a literal single-token answer.</summary>
    public bool WasAdmissible => FidelityAtObservation == TermFidelity.LiteralSingleToken;
}

/// <summary>
/// One factual claim bound to a named paper, institution or database (constraint C-011, A-011).
/// A claim with no such binding resolves as unattributed and blocks the item.
/// </summary>
public sealed record ClaimAttribution
{
    public required string ClaimId { get; init; }
    public required string Claim { get; init; }
    public required string Source { get; init; }
    public required DateTimeOffset RecordedAt { get; init; }

    /// <summary>Whether the claim survives with the licensed footage set aside (A-010).</summary>
    public bool CarriedByScriptOrGraphics { get; init; } = true;

    public bool IsAttributed => !string.IsNullOrWhiteSpace(Source);
}

/// <summary>
/// The originality assessment made by setting the licensed footage aside and judging whether the
/// script and the original graphics alone carry the recorded claim set (decision D-005, A-010).
///
/// This is a structural question, not a similarity number: licensing does not cure reused content
/// (fact F-017), so the question a criterion must decide is whether an original artefact exists.
/// </summary>
public sealed record FootageRemovalAssessment
{
    public required int ClaimsTotal { get; init; }
    public required int ClaimsCarriedWithoutFootage { get; init; }
    public required string Judgement { get; init; }
    public required DateTimeOffset AssessedAt { get; init; }

    /// <summary>The argument survives only when every recorded claim survives the removal.</summary>
    public bool ArgumentSurvives =>
        ClaimsTotal > 0 && ClaimsCarriedWithoutFootage == ClaimsTotal;
}
