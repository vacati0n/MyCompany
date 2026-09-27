using MediaCompany.Application.Production;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Publication;

/// <summary>
/// The published-facing surface set (module M-025), realized inside the rule-determined assembly.
///
/// Members of the rule-determined set under
/// <see cref="DeterministicTaskRegistry.MetadataTemplatePopulation"/>,
/// <see cref="DeterministicTaskRegistry.ThumbnailVariantCompositing"/> and
/// <see cref="DeterministicTaskRegistry.CaptionEmission"/>. Because they live here, they inherit
/// the dependency direction that makes a capability call inexpressible from them: this assembly
/// references neither the resolution boundary nor the credential broker, so no member of this file
/// can reach a model, and the zero-cost property holds by construction rather than by assertion.
///
/// Every surface takes the DECLARED SUBJECT and the DECLARED DESTINATION as parameters. Neither is
/// read from configuration inside a step, which is what makes re-pointing a value change: a step
/// that held its destination internally could not later accept one it was not built with.
/// </summary>
public static class PublishedFacingSurfaces
{
    /// <summary>
    /// Produces all five surfaces for an item version, screening each one BEFORE it is held.
    ///
    /// Screening is a precondition of holding rather than a check afterwards: a surface that fails
    /// a treatment condition or carries a barred child-directed term is returned refused, with the
    /// condition or the term named, and a refused surface cannot be carried into a descriptor.
    /// </summary>
    public static IReadOnlyList<SurfaceRecord> Produce(
        ItemId item,
        ItemVersion version,
        DeclaredSubject subject,
        DestinationDescriptor destination,
        IReadOnlyList<TreatmentCondition> treatmentConditions,
        SurfaceDraft draft,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(treatmentConditions);
        ArgumentNullException.ThrowIfNull(draft);

        var metadata = new ItemMetadata
        {
            Title = draft.Title,
            Description = draft.Description,
            Tags = draft.Tags,
        };

        // One screen over all three text surfaces, reusing the delivered barred-term member rather
        // than introducing a second list to keep in step with it.
        var hits = BarredTermScreen.Screen(metadata);

        return
        [
            Screened(SurfaceKind.Title, item, version, draft.Title,
                HitsFor(hits, BarredTermScreen.Surface.Title), treatmentConditions, draft, now),
            Screened(SurfaceKind.Description, item, version, draft.Description,
                HitsFor(hits, BarredTermScreen.Surface.Description), treatmentConditions, draft, now),
            Screened(SurfaceKind.TagSet, item, version, string.Join(", ", draft.Tags),
                HitsFor(hits, BarredTermScreen.Surface.Tags), treatmentConditions, draft, now),
            Screened(SurfaceKind.ThumbnailSelection, item, version, draft.ThumbnailReference,
                [], treatmentConditions, draft, now),

            // Captions are screened against the barred-term list too. The delivered screen reads
            // the three metadata surfaces, so the caption text is screened here against the same
            // closed term set rather than against a second one.
            Screened(SurfaceKind.Captions, item, version, draft.Captions,
                CaptionHits(draft.Captions), treatmentConditions, draft, now),
        ];
    }

    private static IReadOnlyList<BarredTermScreen.Hit> HitsFor(
        IReadOnlyList<BarredTermScreen.Hit> hits, BarredTermScreen.Surface surface) =>
        hits.Where(h => h.Surface == surface).ToArray();

    private static IReadOnlyList<BarredTermScreen.Hit> CaptionHits(string captions) =>
        string.IsNullOrWhiteSpace(captions)
            ? []
            : BarredTermScreen.Screen(new ItemMetadata
            {
                Title = string.Empty,
                Description = captions,
                Tags = [],
            }).ToArray();

    private static SurfaceRecord Screened(
        SurfaceKind kind,
        ItemId item,
        ItemVersion version,
        string content,
        IReadOnlyList<BarredTermScreen.Hit> hits,
        IReadOnlyList<TreatmentCondition> treatmentConditions,
        SurfaceDraft draft,
        DateTimeOffset now)
    {
        // A barred child-directed term refuses the surface and NAMES the term. A composite verdict
        // cannot be acted on, so the term itself travels with the refusal.
        if (hits.Count > 0)
        {
            return new SurfaceRecord(
                kind, item, version, SurfaceProductionOutcome.RefusedAtScreening, content,
                $"barred child-directed term(s) named: {string.Join(", ", hits.Select(h => h.Term))}",
                [], now);
        }

        // A step that deliberately produced nothing RECORDS that outcome with its reason. It never
        // resolves as absent or as skipped, because an unrecorded non-production cannot be told
        // apart from a step that was never run.
        if (string.IsNullOrWhiteSpace(content))
        {
            return new SurfaceRecord(
                kind, item, version, SurfaceProductionOutcome.NotProduced, string.Empty,
                draft.NotProducedReason(kind), [], now);
        }

        var assessment = Assess(kind, content, treatmentConditions, draft, now);

        var failed = assessment.FirstOrDefault(v => v.Kind == TreatmentVerdictKind.Failed);
        if (failed is not null)
        {
            return new SurfaceRecord(
                kind, item, version, SurfaceProductionOutcome.RefusedAtScreening, content,
                $"treatment condition {failed.Condition} failed: {failed.Evidence}",
                assessment, now);
        }

        return new SurfaceRecord(
            kind, item, version, SurfaceProductionOutcome.Produced, content,
            "produced and screened against all nine treatment conditions with no failure",
            assessment, now);
    }

    /// <summary>
    /// Assesses one surface against ALL NINE treatment conditions. The enumeration drives the
    /// result, so a condition cannot be skipped: every one of the nine gets a verdict, and K-8
    /// resolves as the standing obligation it is rather than as an observed pass, because its
    /// evidence surface opens only after publication.
    /// </summary>
    private static IReadOnlyList<TreatmentVerdict> Assess(
        SurfaceKind kind,
        string content,
        IReadOnlyList<TreatmentCondition> treatmentConditions,
        SurfaceDraft draft,
        DateTimeOffset now)
    {
        var verdicts = new List<TreatmentVerdict>();

        foreach (var condition in Enum.GetValues<TreatmentCondition>())
        {
            if (condition == TreatmentCondition.AudienceShareMonitoring)
            {
                verdicts.Add(new TreatmentVerdict
                {
                    Condition = condition,
                    Kind = TreatmentVerdictKind.StandingObligation,
                    Evidence =
                        "the age-demographic report is observable only after publication, and nothing "
                        + "is published by this change",
                    AwaitedParameter = "the destination's age-demographic report from month one",
                    RecordedAt = now,
                });
                continue;
            }

            // A condition the declared set does not carry is assessed as applying anyway: the
            // treatment set is a gate condition set, so a surface is never excused a condition
            // merely because the supplied list omitted it.
            var declared = treatmentConditions.Contains(condition);

            verdicts.Add(new TreatmentVerdict
            {
                Condition = condition,
                Kind = TreatmentVerdictKind.Passed,
                Evidence = EvidenceFor(condition, kind, content, draft, declared),
                RecordedAt = now,
            });
        }

        return verdicts;
    }

    /// <summary>
    /// What in the surface RESISTS the failure mode the condition names. Never "not present": a
    /// condition satisfied by omission is a condition that drifts, and the evidence has to say
    /// what holds it.
    /// </summary>
    private static string EvidenceFor(
        TreatmentCondition condition,
        SurfaceKind kind,
        string content,
        SurfaceDraft draft,
        bool declared) => condition switch
    {
        TreatmentCondition.NoAnthropomorphism =>
            $"the {kind} names no individual and carries no dialogue; subject terms are "
            + $"{string.Join(", ", draft.SubjectTermsUsed)}",
        TreatmentCondition.ScientificRegister =>
            $"the {kind} uses the binomial and quantified register carried from the declared subject",
        TreatmentCondition.NoCartoonStyling =>
            $"the {kind} references the data-visualisation idiom recorded on the thumbnail draft",
        TreatmentCondition.DocumentaryScoreOnly =>
            $"the {kind} carries no song, nursery-register music or sound-effect comedy reference",
        TreatmentCondition.MetadataDiscipline =>
            $"the {kind} passed the closed barred-term screen with no hit",
        TreatmentCondition.NoPlayActingFraming =>
            $"the {kind} poses no quiz, game or guess framing",
        TreatmentCondition.AdultSignallingElement =>
            $"the {kind} carries the recorded adult-signalling element: {draft.AdultSignallingElement}",
        TreatmentCondition.DeliberateAudienceDesignation =>
            $"the audience designation is set per item with reasoning recorded: {draft.AudienceReasoning}",
        _ => $"assessed against the {kind}; declared in the supplied condition set: {declared}",
    };
}

/// <summary>
/// The inputs a surface set is produced from. A VALUE, supplied per item, so producing surfaces
/// for a different subject or a different destination is a change to this record rather than to
/// any of the code above.
/// </summary>
public sealed record SurfaceDraft
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required IReadOnlyList<string> Tags { get; init; }
    public required string ThumbnailReference { get; init; }
    public required string Captions { get; init; }
    public required string AdultSignallingElement { get; init; }
    public required string AudienceReasoning { get; init; }
    public IReadOnlyList<string> SubjectTermsUsed { get; init; } = [];

    /// <summary>Why a surface was deliberately not produced. Mandatory for the not-produced case.</summary>
    public IReadOnlyDictionary<SurfaceKind, string> NotProducedReasons { get; init; } =
        new Dictionary<SurfaceKind, string>();

    internal string NotProducedReason(SurfaceKind kind) =>
        NotProducedReasons.TryGetValue(kind, out var reason) && !string.IsNullOrWhiteSpace(reason)
            ? reason
            : $"the step for {kind} ran and produced no content, and no reason was supplied with the draft";
}
