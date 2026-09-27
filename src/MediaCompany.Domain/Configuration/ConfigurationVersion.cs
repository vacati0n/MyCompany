namespace MediaCompany.Domain.Configuration;

/// <summary>
/// One configuration value over a validity interval. Register, price, limit, budget and route
/// changes take effect as configuration without redeployment, with prior values retained and
/// validity dates carried (constraint C-011, decision D-010).
/// </summary>
public sealed record ConfigurationVersion
{
    public required string Key { get; init; }
    public required string Scope { get; init; }
    public required string Value { get; init; }
    public required int Version { get; init; }
    public required string ChangedBy { get; init; }
    public required string Reason { get; init; }
    public required DateTimeOffset ValidFrom { get; init; }
    public DateTimeOffset? ValidTo { get; init; }
}

/// <summary>
/// The configuration keys this wave admits, as a closed set.
///
/// The publication gate's owner-approval step is deliberately absent from this surface. Decision
/// D-007 places it in the gate transition table instead, so there is no key that could switch it
/// off and adding one would be a code change the Review Gate sees (risk R-012). The set being
/// closed is the enforcement; <see cref="IsAdmitted"/> is how a change is refused.
/// </summary>
public static class ConfigurationKeys
{
    public const string RouteAdmission = "route.admission";
    public const string ModelPrice = "model.price";
    public const string ProviderLimit = "provider.limit";
    public const string BudgetAmount = "budget.amount";
    public const string TermsVerificationCadenceDays = "terms.verification-cadence-days";
    public const string CredentialTtlSeconds = "credential.ttl-seconds";

    public static readonly IReadOnlySet<string> Admitted = new HashSet<string>(StringComparer.Ordinal)
    {
        RouteAdmission,
        ModelPrice,
        ProviderLimit,
        BudgetAmount,
        TermsVerificationCadenceDays,
        CredentialTtlSeconds,
    };

    public static bool IsAdmitted(string key) => Admitted.Contains(key);
}

/// <summary>
/// The configuration keys the publishing capability adds (module M-012).
///
/// Every one of them is a pure VALUE SLOT. None of them reaches a step, a refusal, a condition or
/// an egress, and <see cref="ReachesAControl"/> is the rule that makes that testable rather than
/// merely asserted: a proposed key naming any of those is inadmissible, not merely absent.
///
/// This matters more here than anywhere else in the system. The whole no-publish property rests on
/// five structural absences, and the one way it could be quietly undone is a key whose value is
/// read before an egress. There is no such key, there is no egress to read it before, and a key
/// that tried to name one would fail this rule.
/// </summary>
public static class PublishingConfigurationKeys
{
    public const string Destination = "publishing.destination";
    public const string MetadataTemplateSet = "publishing.metadata-template-set";
    public const string ThumbnailVariantSet = "publishing.thumbnail-variant-set";
    public const string CaptionProfile = "publishing.caption-profile";
    public const string TimingPolicy = "publishing.timing-policy";

    public static readonly IReadOnlySet<string> Admitted = new HashSet<string>(StringComparer.Ordinal)
    {
        Destination,
        MetadataTemplateSet,
        ThumbnailVariantSet,
        CaptionProfile,
        TimingPolicy,
    };

    /// <summary>
    /// The substrings no admitted publishing key may contain. The list is stricter than the
    /// production one, and deliberately so: a production key legitimately holds the treatment
    /// CONDITIONS as a value, whereas no publishing key has any business naming a condition, a
    /// step, a refusal or an egress.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenKeyFragments =
    [
        "gate",
        "refusal",
        "refuse",
        "approval",
        "publish-ready",
        "capability",
        "block",
        "condition",
        "egress",
        "upload",
        "dispatch",
        "transport",
        "effect",
        "enable",
        "disable",
        "trigger",
    ];

    public static bool IsAdmitted(string key) => Admitted.Contains(key);

    /// <summary>Whether a proposed key would reach a control rather than carry a value.</summary>
    public static bool ReachesAControl(string key) =>
        ForbiddenKeyFragments.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase));
}
