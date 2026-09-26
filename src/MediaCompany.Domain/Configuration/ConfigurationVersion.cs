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
