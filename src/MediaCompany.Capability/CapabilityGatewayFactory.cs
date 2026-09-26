using System.Runtime.CompilerServices;
using MediaCompany.Application.Ports;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;

[assembly: InternalsVisibleTo("MediaCompany.Capability.Tests")]

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
        IRouteRegistry routes,
        IRouteAvailabilityLedger availability,
        IOperatingRegisters registers,
        IBudgetReader budgets,
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

        var adapters = endpoints.ToDictionary(
            e => e.ProviderAccount,
            IProviderAdapter (e) => new HttpProviderAdapter(e.ProviderAccount, e.Endpoint, httpClient, exchange));

        return new CapabilityGateway(
            routes,
            availability,
            registers,
            budgets,
            unitOfWork,
            broker,
            clock,
            adapters);
    }
}
