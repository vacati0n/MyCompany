namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// The emergency tier is a typed union, per decision D-009 and fact F-021: a provider route, a
/// hold-and-escalate position, or a non-AI substitute. Making it a union rather than a nullable
/// provider route is what keeps a capability with fewer than three provider routes expressible
/// and visible rather than fudged (risk R-010).
/// </summary>
public abstract record RouteTarget
{
    private RouteTarget()
    {
    }

    /// <summary>A model reached through a provider account held under commercial terms.</summary>
    public sealed record ProviderRoute(ProviderAccountId ProviderAccount, ModelId Model) : RouteTarget;

    /// <summary>
    /// The recorded emergency position for high-stakes review: the item is held and the owner is
    /// escalated. No model call is made, so the operation carries zero AI cost.
    /// </summary>
    public sealed record HoldAndEscalate(string Reason) : RouteTarget;

    /// <summary>
    /// A non-AI substitute — motion graphics for a cutaway, a held licensed still, publication
    /// without music, or a manual research queue. Each routes to a member of the deterministic
    /// task set and therefore carries zero AI cost.
    /// </summary>
    public sealed record NonAiSubstitute(string DeterministicTaskName) : RouteTarget;
}

/// <summary>
/// One admitted route: a capability class at a tier, bound to a target, with the commercial
/// terms basis and the verification date risk R-001 requires.
/// </summary>
public sealed record Route
{
    public Route(
        RouteId id,
        CapabilityClass capability,
        RouteTier tier,
        RouteTarget target,
        QualityRating ratedQuality,
        ContextCapacity contextCapacity,
        string termsBasis,
        DateOnly termsVerifiedOn,
        ReasoningTier? statedReasoningTier = null)
    {
        ArgumentNullException.ThrowIfNull(target);
        if (string.IsNullOrWhiteSpace(termsBasis))
        {
            throw new ArgumentException("A route carries the commercial terms basis it is held under.", nameof(termsBasis));
        }

        Id = id;
        Capability = capability;
        Tier = tier;
        Target = target;
        RatedQuality = ratedQuality;
        ContextCapacity = contextCapacity;
        TermsBasis = termsBasis;
        TermsVerifiedOn = termsVerifiedOn;
        StatedReasoningTier = statedReasoningTier;
    }

    public RouteId Id { get; }
    public CapabilityClass Capability { get; }
    public RouteTier Tier { get; }
    public RouteTarget Target { get; }
    public QualityRating RatedQuality { get; }
    public ContextCapacity ContextCapacity { get; }
    public string TermsBasis { get; }
    public DateOnly TermsVerifiedOn { get; }

    /// <summary>
    /// The reasoning tier this route STATES it serves.
    ///
    /// Optional, and its absence means the route CANNOT STATE ONE — not that the tier is unknown
    /// and not that it is standard. An untiered route therefore stays visible as untiered instead
    /// of being silently counted at an assumed tier. The served value on an operation record is
    /// read from here, at the resolution boundary, and is never copied from the request: the
    /// request states what was asked for, and only the admitting route can say what was served.
    /// </summary>
    public ReasoningTier? StatedReasoningTier { get; }

    /// <summary>
    /// A route whose terms basis is past the re-verification cadence is not admitted, which is
    /// the mitigation recorded against risk R-001.
    /// </summary>
    public bool TermsCurrentOn(DateOnly asOf, int cadenceDays) =>
        TermsVerifiedOn.AddDays(cadenceDays) >= asOf;
}
