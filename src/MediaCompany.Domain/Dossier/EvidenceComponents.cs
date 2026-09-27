namespace MediaCompany.Domain.Dossier;

/// <summary>
/// One named evidence component of a determination, and what a USED surface produced for it.
///
/// The distinction this type exists to hold: building a surface is not evidence. A component is
/// evidenced only when a surface that was actually exercised produced something for it, so a
/// component whose evidence lives inside an upload that never happened stays not-evidenceable
/// however complete the code around it is.
/// </summary>
public sealed record EvidenceComponent
{
    public EvidenceComponent(
        string name,
        DeterminationOutcome outcome,
        string producedEvidence,
        string notEvidenceableReason,
        string whatWouldMakeItEvidenceable)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("An evidence component is named.", nameof(name));
        }

        if (outcome == DeterminationOutcome.Evidenced && string.IsNullOrWhiteSpace(producedEvidence))
        {
            throw new ArgumentException(
                "An evidenced component names what a used surface produced for it.",
                nameof(producedEvidence));
        }

        if (outcome == DeterminationOutcome.RecordedNotEvidenceable
            && (string.IsNullOrWhiteSpace(notEvidenceableReason)
                || string.IsNullOrWhiteSpace(whatWouldMakeItEvidenceable)))
        {
            throw new ArgumentException(
                "A not-evidenceable component carries both its reason and its remedy.",
                nameof(notEvidenceableReason));
        }

        Name = name;
        Outcome = outcome;
        ProducedEvidence = producedEvidence;
        NotEvidenceableReason = notEvidenceableReason;
        WhatWouldMakeItEvidenceable = whatWouldMakeItEvidenceable;
    }

    public string Name { get; }
    public DeterminationOutcome Outcome { get; }

    /// <summary>What a used surface produced. Empty when nothing was produced for this component.</summary>
    public string ProducedEvidence { get; }

    public string NotEvidenceableReason { get; }
    public string WhatWouldMakeItEvidenceable { get; }
}

/// <summary>
/// The weakest-across-components rule (decision D-005, module M-020).
///
/// A determination's outcome is the WEAKEST outcome across its named evidence components. The
/// consequence is deliberate conservatism: a determination whose external half cannot be evidenced
/// reads as recorded-not-evidenceable even where its internal half was computed, and the produced
/// evidence is named alongside the mandatory reason and remedy rather than being discarded.
///
/// The opposite error is the one that cannot be caught downstream. A compliance position reading
/// as discharged when the evidence for half of it does not exist looks exactly like a discharged
/// position, and no later reader can tell the difference.
/// </summary>
public static class EvidenceComposition
{
    /// <summary>
    /// The strength order, weakest first. A determination takes the weakest outcome any of its
    /// components carries, so a single unresolved or failed component decides the whole.
    /// </summary>
    private static int Strength(DeterminationOutcome outcome) => outcome switch
    {
        DeterminationOutcome.Unresolved => 0,
        DeterminationOutcome.Failed => 1,
        DeterminationOutcome.RecordedNotEvidenceable => 2,
        DeterminationOutcome.Evidenced => 3,
        _ => 0,
    };

    /// <summary>
    /// Resolves a determination from its named components.
    ///
    /// A determination with NO components is unresolved, not evidenced: an empty component set is
    /// the absence of evidence, and reading it as sufficient would be the exact failure this rule
    /// exists to prevent.
    /// </summary>
    public static DeterminationResolution Resolve(
        ComplianceDetermination determination,
        IReadOnlyList<EvidenceComponent> components,
        DateTimeOffset resolvedAt,
        string? policyReference = null,
        DateOnly? policyVerifiedOn = null)
    {
        ArgumentNullException.ThrowIfNull(components);

        if (components.Count == 0)
        {
            return new DeterminationResolution
            {
                Determination = determination,
                Outcome = DeterminationOutcome.Unresolved,
                ResolvedAt = resolvedAt,
                PolicyReference = policyReference,
                PolicyVerifiedOn = policyVerifiedOn,
            };
        }

        var weakest = components.OrderBy(c => Strength(c.Outcome)).First().Outcome;

        var produced = components
            .Where(c => !string.IsNullOrWhiteSpace(c.ProducedEvidence))
            .Select(c => $"{c.Name}: {c.ProducedEvidence}")
            .ToArray();

        if (weakest == DeterminationOutcome.Evidenced)
        {
            return new DeterminationResolution
            {
                Determination = determination,
                Outcome = DeterminationOutcome.Evidenced,
                Evidence = string.Join("; ", produced),
                ResolvedAt = resolvedAt,
                PolicyReference = policyReference,
                PolicyVerifiedOn = policyVerifiedOn,
            };
        }

        if (weakest == DeterminationOutcome.RecordedNotEvidenceable)
        {
            var weak = components
                .Where(c => c.Outcome == DeterminationOutcome.RecordedNotEvidenceable)
                .ToArray();

            // The produced half is NAMED rather than dropped. A partially evidenced determination
            // is not evidenced, but the work that was done stays visible so a later reader knows
            // what remains rather than starting again.
            var reason = string.Join("; ", weak.Select(c => $"{c.Name}: {c.NotEvidenceableReason}"));
            if (produced.Length > 0)
            {
                reason += $". Produced evidence, which does not by itself evidence the determination: "
                          + string.Join("; ", produced);
            }

            return new DeterminationResolution
            {
                Determination = determination,
                Outcome = DeterminationOutcome.RecordedNotEvidenceable,
                Evidence = produced.Length > 0 ? string.Join("; ", produced) : null,
                NotEvidenceableReason = reason,
                WhatWouldMakeItEvidenceable =
                    string.Join("; ", weak.Select(c => $"{c.Name}: {c.WhatWouldMakeItEvidenceable}")),
                ResolvedAt = resolvedAt,
                PolicyReference = policyReference,
                PolicyVerifiedOn = policyVerifiedOn,
            };
        }

        return new DeterminationResolution
        {
            Determination = determination,
            Outcome = weakest,
            Evidence = produced.Length > 0 ? string.Join("; ", produced) : null,
            ResolvedAt = resolvedAt,
            PolicyReference = policyReference,
            PolicyVerifiedOn = policyVerifiedOn,
        };
    }
}
