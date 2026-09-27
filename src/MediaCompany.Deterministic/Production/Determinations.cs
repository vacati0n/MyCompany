using MediaCompany.Domain.Dossier;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// The five compliance determinations (criterion A-019, decision D-004), members of the
/// rule-determined set under <see cref="DeterministicTaskRegistry.ComplianceDetermination"/>.
///
/// Each builder here produces a resolution that is well-formed by construction, or refuses to
/// produce one at all. The point is that there is no path to a determination that LOOKS resolved
/// but carries nothing: an honest "cannot be evidenced, here is why, here is what would fix it"
/// is a successful outcome, and a resolution without its reason is not a resolution.
/// </summary>
public static class Determinations
{
    /// <summary>
    /// Resolves a determination by recorded evidence. The evidence text is required, so an
    /// evidenced resolution cannot be empty.
    /// </summary>
    public static DeterminationResolution Evidenced(
        ComplianceDetermination determination,
        string evidence,
        DateTimeOffset resolvedAt,
        string? policyReference = null,
        DateOnly? policyVerifiedOn = null)
    {
        if (string.IsNullOrWhiteSpace(evidence))
        {
            throw new ArgumentException(
                "An evidenced determination carries its evidence; an empty one is unresolved.",
                nameof(evidence));
        }

        return new DeterminationResolution
        {
            Determination = determination,
            Outcome = DeterminationOutcome.Evidenced,
            Evidence = evidence,
            PolicyReference = policyReference,
            PolicyVerifiedOn = policyVerifiedOn,
            ResolvedAt = resolvedAt,
        };
    }

    /// <summary>
    /// Resolves a determination by recording that it cannot be evidenced. BOTH the reason and the
    /// remedy are mandatory (decision D-004): without them this resolution would become a quiet
    /// way of passing something nobody checked, which is the single failure the third resolution
    /// exists to prevent.
    /// </summary>
    public static DeterminationResolution NotEvidenceable(
        ComplianceDetermination determination,
        string reason,
        string whatWouldMakeItEvidenceable,
        DateTimeOffset resolvedAt,
        string? policyReference = null,
        DateOnly? policyVerifiedOn = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException(
                "A not-evidenceable determination names why it cannot be evidenced.",
                nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(whatWouldMakeItEvidenceable))
        {
            throw new ArgumentException(
                "A not-evidenceable determination names what would make it evidenceable.",
                nameof(whatWouldMakeItEvidenceable));
        }

        return new DeterminationResolution
        {
            Determination = determination,
            Outcome = DeterminationOutcome.RecordedNotEvidenceable,
            NotEvidenceableReason = reason,
            WhatWouldMakeItEvidenceable = whatWouldMakeItEvidenceable,
            PolicyReference = policyReference,
            PolicyVerifiedOn = policyVerifiedOn,
            ResolvedAt = resolvedAt,
        };
    }

    /// <summary>
    /// Derives the advertiser-suitability self-rating from the item's own classification
    /// (criterion A-022).
    ///
    /// A rating is produced only when every one of the fourteen categories carries a
    /// classification WITH A BASIS. A fixed answer with nothing behind it yields
    /// <see cref="AdSuitabilityRating.Absent"/>, not a green rating — because repeated inaccurate
    /// self-rating puts programme eligibility under review, and a pipeline that answers "none of
    /// the above" by default is the documented route to exactly that.
    /// </summary>
    public static AdSuitabilityRating DeriveRating(IReadOnlyList<SuitabilityClassification> classifications)
    {
        ArgumentNullException.ThrowIfNull(classifications);

        var categories = Enum.GetValues<SuitabilityCategory>();

        foreach (var category in categories)
        {
            var classification = classifications.FirstOrDefault(c => c.Category == category);

            // An unclassified category, or one classified with no basis, means there is no
            // classification behind the rating. The rating is therefore absent rather than green.
            if (classification is null || !classification.HasBasis)
            {
                return AdSuitabilityRating.Absent;
            }
        }

        var present = classifications.Where(c => c.Present).ToArray();

        return present.Length switch
        {
            0 => AdSuitabilityRating.FullAdRevenue,
            _ => AdSuitabilityRating.LimitedAdRevenue,
        };
    }

    /// <summary>
    /// Resolves the synthetic-media disclosure determination over the item's generated and altered
    /// elements (criterion A-023). An element that has not been assessed prevents the determination
    /// from resolving, which is why the element set is enumerated and every member must carry a
    /// basis.
    /// </summary>
    public static DeterminationResolution ResolveSyntheticMedia(
        IReadOnlyList<SyntheticElementAssessment> elements,
        DateTimeOffset resolvedAt,
        string policyReference,
        DateOnly policyVerifiedOn)
    {
        ArgumentNullException.ThrowIfNull(elements);

        var unassessed = elements.Where(e => !e.IsAssessed).ToArray();

        if (unassessed.Length > 0)
        {
            return new DeterminationResolution
            {
                Determination = ComplianceDetermination.SyntheticMediaDisclosure,
                Outcome = DeterminationOutcome.Unresolved,
                ResolvedAt = resolvedAt,
                PolicyReference = policyReference,
                PolicyVerifiedOn = policyVerifiedOn,
            };
        }

        var requiring = elements.Where(e => e.DisclosureRequired).ToArray();

        var evidence = requiring.Length == 0
            ? "All " + elements.Count + " generated or altered element(s) assessed; none requires disclosure. Bases: "
              + string.Join("; ", elements.Select(e => e.ElementReference + " — " + e.Basis))
            : requiring.Length + " of " + elements.Count + " element(s) require disclosure: "
              + string.Join("; ", requiring.Select(e => e.ElementReference + " — " + e.Basis));

        return Evidenced(
            ComplianceDetermination.SyntheticMediaDisclosure,
            evidence,
            resolvedAt,
            policyReference,
            policyVerifiedOn);
    }

    /// <summary>
    /// Resolves the duplicate determination (criterion A-021). The result is stated against both
    /// the company's own catalogue and sources outside it, and the DISTANCE at which the item was
    /// judged distinct is recorded with it. A result stated without a distance is not complete,
    /// because "sufficiently distinct" with no number behind it cannot be re-judged later.
    /// </summary>
    public static DeterminationResolution ResolveDuplicate(
        DuplicateResult result,
        DateTimeOffset resolvedAt,
        string? policyReference = null,
        DateOnly? policyVerifiedOn = null)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsComplete)
        {
            return new DeterminationResolution
            {
                Determination = ComplianceDetermination.DuplicateDetection,
                Outcome = DeterminationOutcome.Unresolved,
                ResolvedAt = resolvedAt,
                PolicyReference = policyReference,
                PolicyVerifiedOn = policyVerifiedOn,
            };
        }

        if (result.DuplicateAgainstOwnCatalogue || result.DuplicateAgainstExternalSources)
        {
            return new DeterminationResolution
            {
                Determination = ComplianceDetermination.DuplicateDetection,
                Outcome = DeterminationOutcome.Failed,
                Evidence = "Duplicate found at distance " + result.Distance + " by " + result.Method + ".",
                ResolvedAt = resolvedAt,
                PolicyReference = policyReference,
                PolicyVerifiedOn = policyVerifiedOn,
            };
        }

        return Evidenced(
            ComplianceDetermination.DuplicateDetection,
            "No duplicate against the company catalogue or external sources; judged distinct at distance "
            + result.Distance + " by " + result.Method + ".",
            resolvedAt,
            policyReference,
            policyVerifiedOn);
    }
}
