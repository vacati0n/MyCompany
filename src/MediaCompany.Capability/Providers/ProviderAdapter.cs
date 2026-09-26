using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// What an adapter returns from one attempt against a provider.
/// </summary>
internal sealed record ProviderAttempt(
    bool Succeeded,
    UnitCounts Units,
    CostBasis CostBasis,
    TimeSpan Duration,
    string? FailureReason,
    ProviderFailureSignal? Signal);

/// <summary>The signal a provider gave, which the availability ledger turns into a recorded state.</summary>
internal enum ProviderFailureSignal
{
    Transient = 1,
    RateLimited = 2,
    QuotaExhausted = 3,
    Outage = 4,
}

/// <summary>
/// The provider adapter port (module M-003).
///
/// This interface is INTERNAL to the capability assembly, and so is every implementation of it.
/// That is the whole of decision D-001's structural claim: a consumer cannot construct a provider
/// adapter, because the type it would name is not visible outside this assembly. It is not a
/// forbidden path — it is not a compiling one. The alternative was removed rather than guarded.
///
/// An adapter never receives a credential. It receives a <see cref="ScopedHandle"/> and calls
/// <see cref="ICredentialExchange"/>, which attaches the credential to the transport message and
/// returns nothing, so the credential is never in the adapter's scope either.
/// </summary>
internal interface IProviderAdapter
{
    ProviderAccountId ProviderAccount { get; }

    Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route,
        CapabilityRequest request,
        ScopedHandle handle,
        CancellationToken cancellationToken);
}
