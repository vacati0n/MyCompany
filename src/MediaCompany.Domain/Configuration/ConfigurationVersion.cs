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

/// <summary>
/// The configuration keys a channel's own attributes are held under (the multi-channel change,
/// decision D-001 of its accepted design).
///
/// One legal entity runs several channels, separated by configuration only. Eight of a channel's
/// attributes are values in the temporal configuration register under these keys, each scoped to
/// ONE channel by <see cref="ScopeFor"/>, so every change carries its validity, its author and its
/// reason, and a superseded value stays retrievable. The channel's language is read from the channel
/// register row and its library registrations from the library registration register, so neither is
/// configuration, and the registration precondition stays unsatisfiable from configuration.
///
/// Every key is a pure VALUE SLOT: free text kept verbatim and interpreted by no component. The set
/// is closed, and <see cref="ReachesAControl"/> refuses any key naming the payee, a payment account,
/// a registration, a condition, the approval step, a gate state, a refusal, a block, publication,
/// dispatch, an upload, a route, a trigger, a due time, liveness or an enablement — so no per-channel
/// value can become a control, a precondition, the payee, a trigger or a routing decision. The
/// schedule and the content strategy are therefore never read as a due time or a routing target,
/// and a channel brought into being by register and configuration rows is neither live nor routed to.
///
/// No value for any channel is recorded by this change, in code, seed or migration.
/// </summary>
public static class ChannelConfigurationKeys
{
    public const string Audience = "channel.audience";
    public const string Brand = "channel.brand";
    public const string Voice = "channel.voice";
    public const string Tone = "channel.tone";
    public const string VisualIdentity = "channel.visual-identity";
    public const string ContentStrategy = "channel.content-strategy";
    public const string Schedule = "channel.schedule";
    public const string RiskNotes = "channel.risk-notes";

    /// <summary>The prefix of every channel scope; the rest of the scope is the channel's register identifier.</summary>
    public const string ScopePrefix = "channel:";

    /// <summary>The closed key set, in the order a profile presents it.</summary>
    public static IReadOnlyList<string> Ordered { get; } =
    [
        Audience,
        Brand,
        Voice,
        Tone,
        VisualIdentity,
        ContentStrategy,
        Schedule,
        RiskNotes,
    ];

    public static readonly IReadOnlySet<string> Admitted = new HashSet<string>(Ordered, StringComparer.Ordinal);

    /// <summary>
    /// The substrings no admitted channel key may contain. A key reaching any of them would make a
    /// per-channel value a control rather than a value, which the shared-payee decision and the
    /// owner-approval state both forbid.
    /// </summary>
    public static readonly IReadOnlyList<string> ForbiddenKeyFragments =
    [
        "payee",
        "payment",
        "account",
        "registration",
        "condition",
        "approval",
        "gate",
        "refusal",
        "block",
        "publish",
        "dispatch",
        "upload",
        "route",
        "routing",
        "trigger",
        "due",
        "live",
        "enable",
    ];

    public static bool IsAdmitted(string key) => Admitted.Contains(key);

    /// <summary>Whether a proposed key would reach a control rather than carry a value.</summary>
    public static bool ReachesAControl(string key) =>
        ForbiddenKeyFragments.Any(f => key.Contains(f, StringComparison.OrdinalIgnoreCase));

    /// <summary>The scope that holds one channel's values: the prefix and the channel's register identifier.</summary>
    public static string ScopeFor(ChannelId channel) => ScopePrefix + channel.Value.ToString("D");

    /// <summary>
    /// Whether a scope names exactly one channel. A channel key is admitted only under such a scope,
    /// and no other key is admitted under one.
    /// </summary>
    public static bool IsChannelScope(string scope) =>
        scope is not null
        && scope.StartsWith(ScopePrefix, StringComparison.Ordinal)
        && Guid.TryParseExact(scope[ScopePrefix.Length..], "D", out _);
}

/// <summary>
/// The company scope form (the AI-economics change, decision D-010 of its design), beside the channel
/// scope form.
///
/// A company scope names exactly ONE company register identifier. The production and publishing keys
/// are company-wide settings shared by every channel of the one legal entity — per-channel variation is
/// carried by the channel key set — so the configuration store admits each of them only under a company
/// scope, and only where that key's own delivered forbidden-fragment rule finds no control. No fragment
/// rule changes: the publishing keys carry fragments the channel rule forbids, so admitting them under a
/// channel scope would weaken that rule, and they are not admitted there.
///
/// No value for any company is recorded by this change, in code, seed or migration.
/// </summary>
public static class CompanyConfigurationScope
{
    /// <summary>The prefix of every company scope; the rest of the scope is the company's register identifier.</summary>
    public const string ScopePrefix = "company:";

    /// <summary>The scope that holds the company's production and publishing values.</summary>
    public static string ScopeFor(CompanyId company) => ScopePrefix + company.Value.ToString("D");

    /// <summary>Whether a scope names exactly one company.</summary>
    public static bool IsCompanyScope(string scope) =>
        scope is not null
        && scope.StartsWith(ScopePrefix, StringComparison.Ordinal)
        && Guid.TryParseExact(scope[ScopePrefix.Length..], "D", out _);
}
