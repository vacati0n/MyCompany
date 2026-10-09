using System.Runtime.CompilerServices;
using MediaCompany.Application.Ports;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;

[assembly: InternalsVisibleTo("MediaCompany.Capability.Tests")]

// The AI-economics change (decision D-013 of its design): the month-end demonstration runs in the
// persistence test assembly, against a live throwaway store, with a stand-in provider declared there,
// so the internals of this assembly are visible to that one test assembly as well. No production
// assembly gains any visibility.
[assembly: InternalsVisibleTo("MediaCompany.Persistence.Tests")]

namespace MediaCompany.Capability;

/// <summary>
/// A provider account and the endpoint its adapter reaches. This is configuration, not an
/// implementation: the composition root names endpoints and the assembly builds the adapters, so
/// no caller anywhere holds an adapter instance or its type.
/// </summary>
public sealed record ProviderEndpoint(ProviderAccountId ProviderAccount, Uri Endpoint);

/// <summary>
/// The only public way to obtain the capability boundary.
///
/// The composition root calls this, receives an <see cref="ICapabilityGateway"/>, and registers
/// that. It never sees <c>IProviderAdapter</c>, because that type is internal to this assembly.
/// Decision D-001's requirement — that a directly constructed provider adapter must not be a
/// compiling path — is satisfied by the visibility of the type rather than by a check.
/// </summary>
public static class CapabilityGatewayFactory
{
    public static ICapabilityGateway Create(
        IUnitOfWork unitOfWork,
        ICredentialBroker broker,
        ICredentialExchange exchange,
        IClock clock,
        IReadOnlyList<ProviderEndpoint> endpoints,
        HttpClient httpClient)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(exchange);
        ArgumentNullException.ThrowIfNull(clock);

        // The boundary takes no budget reader and reads no process clock (the AI-economics change,
        // decision D-001 of its design): its headroom and readings are read on the admission
        // transaction at the booking instant the datastore reserves, and every instant it writes is
        // that one. The clock stays a parameter of this factory so its delivered callers compose it
        // unchanged (the design removes the budget reader parameter only); the boundary is given none.

        var adapters = endpoints.ToDictionary(
            e => e.ProviderAccount,
            IProviderAdapter (e) => new HttpProviderAdapter(e.ProviderAccount, e.Endpoint, httpClient, exchange));

        // The routes, forbidden sources, availability, accounts and prices are read by the admission ledger
        // in one snapshot of the admission transaction (the correction cycle), so the boundary takes none of
        // the register ports that read them on other connections.
        // The provider-call bound is the client's own timeout, which every adapter call runs under; the
        // recording of an incurred attempt after a lost transaction waits longer than it (the second
        // correction cycle).
        return new CapabilityGateway(
            unitOfWork,
            broker,
            adapters,
            httpClient.Timeout);
    }
}
