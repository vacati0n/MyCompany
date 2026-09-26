using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Rights;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// Why the publish-ready transition was refused. Every member names ONE condition, because a
/// demonstration cannot be designed against a refusal that does not say what it refused
/// (sequencing constraint P-014, decision D-004).
/// </summary>
public enum PublishReadyRefusal
{
    StageOutcomeMissing = 1,
    TreatmentConditionFailed = 2,
    AudienceDesignationInvalid = 3,
    MetadataCarriesBarredTerm = 4,
    VisualNotFromAdmissibleSource = 5,
    GeneratedCutawayLacksRecordedReason = 6,
    ClipUnassessed = 7,
    ClipOriginAssessedAfterUse = 8,
    ClaimUnattributed = 9,
    ArgumentDoesNotSurviveFootageRemoval = 10,
    SupplyAuditIncomplete = 11,
    SupplyAuditAfterScriptCommit = 12,
    DeterminationUnresolved = 13,
    LibraryUnregistered = 14,
    RuntimeBelowFloor = 15,
    TransitionNotInTable = 16,
}

/// <summary>A refusal with the name of the condition and the subject that failed it.</summary>
public sealed record PublishReadyRefusalDetail(PublishReadyRefusal Refusal, string Detail);

/// <summary>The verdict of the terminal predicate on one item version.</summary>
public sealed record PublishReadyVerdict
{
    public required bool Passed { get; init; }

    public IReadOnlyList<PublishReadyRefusalDetail> Refusals { get; init; } = [];

    public static PublishReadyVerdict Pass() => new() { Passed = true };

    public static PublishReadyVerdict Refuse(IReadOnlyList<PublishReadyRefusalDetail> refusals) =>
        new() { Passed = false, Refusals = refusals };
}

/// <summary>
/// The terminal predicate set (decision D-001, sequencing constraint P-012), a member of the
/// rule-determined set under <see cref="DeterministicTaskRegistry.PublishReadyPrecondition"/>.
///
/// This is the ONLY place publish-ready is decided. Every blocking condition is evaluated here
/// against the item's own dossier, so a condition is a property of the item rather than of the
/// caller that produced it (decision D-002). Every refusal names the condition it failed, and the
/// evaluation collects every refusal rather than stopping at the first, so a blocked item is
/// diagnosable in one read instead of one repair at a time.
/// </summary>
public static class PublishReadyPredicate
{
    /// <summary>
    /// The runtime floor the admissible format requires (criterion A-003). Ten minutes: below
    /// eight there are no mid-rolls, and the format's own rules set the floor at ten with a
    /// twelve-to-fourteen minute target.
    /// </summary>
    public static readonly TimeSpan RuntimeFloor = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Evaluates every precondition. <paramref name="scriptCommittedAt"/> is the moment the script
    /// was committed, against which the supply audit's ordering obligation is judged; a null value
    /// means the script has not committed and the ordering question does not yet arise.
    /// </summary>
    public static PublishReadyVerdict Evaluate(
        ItemDossier dossier,
        GateState currentState,
        IReadOnlyList<LibraryRegistration> registrations,
        ChannelId channel,
        DateTimeOffset? scriptCommittedAt)
    {
        ArgumentNullException.ThrowIfNull(dossier);
        ArgumentNullException.ThrowIfNull(registrations);

        var refusals = new List<PublishReadyRefusalDetail>();

        if (!GateTransitionTable.IsAllowed(currentState, GateState.PublishReady))
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.TransitionNotInTable,
                currentState + " to PublishReady is not a transition in the table."));
        }

        EvaluateStages(dossier, refusals);
        EvaluateTreatment(dossier, refusals);
        EvaluateMetadata(dossier, refusals);
        EvaluateVisuals(dossier, refusals);
        EvaluateClips(dossier, refusals);
        EvaluateClaims(dossier, refusals);
        EvaluateSupply(dossier, scriptCommittedAt, refusals);
        EvaluateDeterminations(dossier, refusals);
        EvaluateRegistration(dossier, registrations, channel, refusals);
        EvaluateRuntime(dossier, refusals);

        return refusals.Count == 0 ? PublishReadyVerdict.Pass() : PublishReadyVerdict.Refuse(refusals);
    }

    /// <summary>
    /// Twelve stages, each with a recorded outcome. Iterating the closed stage set rather than the
    /// recorded outcomes is what makes an absent stage a refusal rather than a gap (C-006).
    /// </summary>
    private static void EvaluateStages(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var stage in ProductionStageSet.All)
        {
            var evidence = dossier.StageFor(stage);

            if (evidence is null)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.StageOutcomeMissing,
                    "Stage " + stage + " has no recorded outcome."));
                continue;
            }

            if (evidence.Outcome is Domain.Work.StageOutcome.Pending or Domain.Work.StageOutcome.Failed)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.StageOutcomeMissing,
                    "Stage " + stage + " recorded outcome " + evidence.Outcome + "."));
            }
        }
    }

    private static void EvaluateTreatment(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var condition in Enum.GetValues<TreatmentCondition>())
        {
            var verdict = dossier.Treatment.FirstOrDefault(t => t.Condition == condition);

            if (verdict is null)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.TreatmentConditionFailed,
                    "Treatment condition " + condition + " is unresolved."));
                continue;
            }

            if (verdict.Blocks)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.TreatmentConditionFailed,
                    "Treatment condition " + condition + " resolved " + verdict.Kind + "."));
            }
        }

        if (dossier.Audience is null || !dossier.Audience.IsValid)
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.AudienceDesignationInvalid,
                dossier.Audience is null
                    ? "No per-item audience designation is recorded."
                    : "The audience designation carries no reasoning, or was taken from a channel-level default."));
        }
    }

    private static void EvaluateMetadata(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        if (dossier.Metadata is null)
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.MetadataCarriesBarredTerm,
                "No metadata set is recorded, so it cannot be screened."));
            return;
        }

        var hits = BarredTermScreen.Screen(dossier.Metadata);
        if (hits.Count > 0)
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.MetadataCarriesBarredTerm,
                BarredTermScreen.DescribeRefusal(hits)));
        }
    }

    private static void EvaluateVisuals(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var visual in dossier.Visuals)
        {
            if (visual.IsAdmissible)
            {
                continue;
            }

            if (visual.Source == VisualSource.GeneratedCutaway)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.GeneratedCutawayLacksRecordedReason,
                    "Generated cutaway '" + visual.VisualReference + "' carries no complete recorded reason."));
                continue;
            }

            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.VisualNotFromAdmissibleSource,
                "Visual '" + visual.VisualReference + "' resolves to " + visual.Source + ", which is not an admissible source."));
        }
    }

    private static void EvaluateClips(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var clip in dossier.ClipOrigins)
        {
            if (!clip.OrderingHolds)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.ClipOriginAssessedAfterUse,
                    "Clip '" + clip.ClipReference + "' was assessed after it was used."));
            }

            if (clip.Origin == ClipOrigin.Unassessed && clip.UsedAt is not null)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.ClipUnassessed,
                    "Clip '" + clip.ClipReference + "' entered the cut without an origin assessment."));
            }
        }
    }

    private static void EvaluateClaims(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var claim in dossier.Claims.Where(c => !c.IsAttributed))
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.ClaimUnattributed,
                "Claim '" + claim.ClaimId + "' resolves to no named paper, institution or database."));
        }

        if (dossier.Originality is null || !dossier.Originality.ArgumentSurvives)
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.ArgumentDoesNotSurviveFootageRemoval,
                dossier.Originality is null
                    ? "No footage-removal originality assessment is recorded."
                    : dossier.Originality.ClaimsCarriedWithoutFootage + " of " + dossier.Originality.ClaimsTotal
                      + " claims survive the removal of the licensed footage."));
        }
    }

    /// <summary>
    /// Both halves of the supply obligation: every subject resolved, and the audit dated before the
    /// script committed. The criterion compares recorded dates, so an audit made afterwards is
    /// unsatisfiable for the item it concerns and cannot be repaired (constraint C-015).
    /// </summary>
    private static void EvaluateSupply(
        ItemDossier dossier,
        DateTimeOffset? scriptCommittedAt,
        List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var entry in dossier.SupplyAudit.Where(e => !e.IsResolved))
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.SupplyAuditIncomplete,
                "Subject '" + entry.RequestedTerm
                + "' carries neither an admissible count nor a recorded reason one was not obtained."));
        }

        if (scriptCommittedAt is null)
        {
            return;
        }

        foreach (var entry in dossier.SupplyAudit.Where(e => e.AuditedAt > scriptCommittedAt.Value))
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.SupplyAuditAfterScriptCommit,
                "Subject '" + entry.RequestedTerm + "' was audited after the script committed."));
        }
    }

    private static void EvaluateDeterminations(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        foreach (var determination in Enum.GetValues<ComplianceDetermination>())
        {
            var resolution = dossier.DeterminationFor(determination);

            if (resolution is null)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.DeterminationUnresolved,
                    "Determination " + determination + " is unresolved."));
                continue;
            }

            if (resolution.Blocks)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.DeterminationUnresolved,
                    "Determination " + determination + " resolved " + resolution.Outcome + " and is not well-formed."));
            }
        }
    }

    private static void EvaluateRegistration(
        ItemDossier dossier,
        IReadOnlyList<LibraryRegistration> registrations,
        ChannelId channel,
        List<PublishReadyRefusalDetail> refusals)
    {
        var libraries = dossier.SupplyAudit
            .Select(e => e.Library)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var library in libraries)
        {
            var registered = registrations.Any(r =>
                r.Channel.Equals(channel) &&
                string.Equals(r.Library, library, StringComparison.OrdinalIgnoreCase));

            if (!registered)
            {
                refusals.Add(new PublishReadyRefusalDetail(
                    PublishReadyRefusal.LibraryUnregistered,
                    "The channel is unregistered on library '" + library + "', which the item's assets come from."));
            }
        }
    }

    private static void EvaluateRuntime(ItemDossier dossier, List<PublishReadyRefusalDetail> refusals)
    {
        if (dossier.Runtime is null || dossier.Runtime.Value < RuntimeFloor)
        {
            refusals.Add(new PublishReadyRefusalDetail(
                PublishReadyRefusal.RuntimeBelowFloor,
                dossier.Runtime is null
                    ? "The finished runtime is not recorded."
                    : "Runtime " + dossier.Runtime.Value + " is below the " + RuntimeFloor + " floor."));
        }
    }
}
