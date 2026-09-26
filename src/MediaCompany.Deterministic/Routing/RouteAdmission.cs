using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Deterministic.Routing;

/// <summary>Why a route was not admitted to the routing table.</summary>
public enum AdmissionRefusal
{
    ForbiddenSource = 1,
    TermsBasisStale = 2,
    TierAlreadyOccupied = 3,
}

/// <summary>The verdict on one admission attempt.</summary>
public abstract record AdmissionVerdict
{
    private AdmissionVerdict()
    {
    }

    public sealed record Admitted(Route Route) : AdmissionVerdict;

    public sealed record Refused(Route Route, AdmissionRefusal Reason, string Detail, ForbiddenSourceKind? ForbiddenKind)
        : AdmissionVerdict;
}

/// <summary>
/// Route admission (module M-002). This is the FIRST of the two independent refusals constraint
/// C-001 requires: a route whose provider account or interface matches a forbidden-source entry
/// cannot enter the table at all.
///
/// The second refusal lives in <see cref="RouteResolver"/> and screens the table again at
/// resolution time. The two are independent by construction: this function reads only the
/// candidate route and the register, and the resolver re-reads the register over whatever the
/// table actually holds, so a configuration error that let a forbidden route into the table is
/// still refused when it is reached.
/// </summary>
public static class RouteAdmission
{
    /// <summary>
    /// The default re-verification cadence for a route's commercial terms basis. A route past its
    /// cadence is not admitted, which is the mitigation recorded against risk R-001.
    /// </summary>
    public const int DefaultTermsCadenceDays = 90;

    public static AdmissionVerdict Admit(
        Route candidate,
        IReadOnlyList<ForbiddenSource> forbiddenSources,
        IReadOnlyList<Route> alreadyAdmitted,
        DateOnly asOf,
        int termsCadenceDays = DefaultTermsCadenceDays)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(forbiddenSources);
        ArgumentNullException.ThrowIfNull(alreadyAdmitted);

        var match = Match(candidate, forbiddenSources);
        if (match is not null)
        {
            return new AdmissionVerdict.Refused(
                candidate,
                AdmissionRefusal.ForbiddenSource,
                $"'{match.Identifier}' is a forbidden source: {match.Reason} ({match.EvidenceReference})",
                match.Kind);
        }

        if (!candidate.TermsCurrentOn(asOf, termsCadenceDays))
        {
            return new AdmissionVerdict.Refused(
                candidate,
                AdmissionRefusal.TermsBasisStale,
                $"Terms basis last verified {candidate.TermsVerifiedOn:O}, past the {termsCadenceDays}-day cadence.",
                null);
        }

        // Constraint C-002: exactly one primary, one secondary and one emergency route per
        // capability class. The uniqueness is a table constraint in the datastore as well; this
        // is the admission-time half.
        if (alreadyAdmitted.Any(r => r.Capability == candidate.Capability && r.Tier == candidate.Tier && r.Id != candidate.Id))
        {
            return new AdmissionVerdict.Refused(
                candidate,
                AdmissionRefusal.TierAlreadyOccupied,
                $"{candidate.Capability} already holds a {candidate.Tier} route.",
                null);
        }

        return new AdmissionVerdict.Admitted(candidate);
    }

    private static ForbiddenSource? Match(Route candidate, IReadOnlyList<ForbiddenSource> register)
    {
        var identifiers = new List<string>();
        switch (candidate.Target)
        {
            case RouteTarget.ProviderRoute provider:
                identifiers.Add(provider.ProviderAccount.Value);
                identifiers.Add(provider.Model.Value);
                break;
            case RouteTarget.NonAiSubstitute substitute:
                identifiers.Add(substitute.DeterministicTaskName);
                break;
        }

        foreach (var entry in register)
        {
            if (identifiers.Any(id => string.Equals(id, entry.Identifier, StringComparison.OrdinalIgnoreCase)))
            {
                return entry;
            }
        }

        return null;
    }
}
