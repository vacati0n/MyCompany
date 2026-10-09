using MediaCompany.Credentials;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability.Providers;

/// <summary>
/// What an adapter returns from one attempt against a provider.
///
/// THE PRODUCTION CHANGE (decisions D-001 and D-002 of its design): an attempt carries the content the vendor
/// produced, as the one buffer it was received in, and whether its CHARGE IS KNOWN. A charge is known only where
/// the vendor's response body stated its usage, or where nothing was sent; a timeout, a transport failure, a
/// non-success status and a success whose body states no usage each leave it unknown, and the boundary then books
/// the attempt at its admitted WORST CASE labelled estimate — never at zero units as a measurement. Both members
/// default to the delivered meaning, so every delivered construction compiles and behaves as before.
/// </summary>
internal sealed record ProviderAttempt(
    bool Succeeded,
    UnitCounts Units,
    CostBasis CostBasis,
    TimeSpan Duration,
    string? FailureReason,
    ProviderFailureSignal? Signal)
{
    /// <summary>The content produced, on a successful attempt only.</summary>
    public ProducedContent? Content { get; init; }

    /// <summary>Whether the vendor's charge for this attempt is known from its own response, or because nothing was sent.</summary>
    public bool ChargeKnown { get; init; } = true;
}

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

    /// <summary>
    /// Whether this adapter serves a capability (the production change): one account can serve more than one
    /// vendor contract, as the speech-and-image vendor's does. A delivered adapter serves whatever it is asked.
    /// </summary>
    bool Serves(CapabilityClass capability) => true;

    /// <summary>The vendor's key header for the key-header scheme, or null where the vendor takes a bearer value.</summary>
    string? KeyHeaderName => null;

    Task<ProviderAttempt> InvokeAsync(
        RouteTarget.ProviderRoute route,
        CapabilityRequest request,
        ScopedHandle handle,
        CancellationToken cancellationToken);
}
