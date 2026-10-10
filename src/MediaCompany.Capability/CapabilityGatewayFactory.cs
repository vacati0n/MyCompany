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
/// The vendor contract an endpoint serves (the production change, decision D-002 of its design). The factory
/// builds the matching internal adapter; nothing outside this assembly names one.
/// </summary>
public enum VendorContract
{
    /// <summary>The speech path of the speech-and-image vendor.</summary>
    SpeechAudio = 1,

    /// <summary>The image path of the speech-and-image vendor.</summary>
    ImageGeneration = 2,

    /// <summary>The messages path of the reasoning vendor.</summary>
    Messages = 3,
}

/// <summary>
/// A provider account, the endpoint its adapter reaches and the vendor contract it serves. This is configuration,
/// not an implementation: the composition root names endpoints and the assembly builds the adapters, so no caller
/// anywhere holds an adapter instance or its type. One account may serve more than one contract.
/// </summary>
public sealed record ProviderEndpoint(ProviderAccountId ProviderAccount, Uri Endpoint, VendorContract Contract);

/// <summary>
/// One fake the demonstration composition builds (the production change, decision D-004 of its design): the
/// account it keys on, which is the same account the recorded routes name, and the contract it stands in for.
/// It carries no endpoint, because a fake reaches nothing.
/// </summary>
public sealed record DemonstrationProvider(ProviderAccountId ProviderAccount, VendorContract Contract);

/// <summary>
/// The only public way to obtain the capability boundary.
///
/// The composition root calls this, receives an <see cref="ICapabilityGateway"/>, and registers
/// that. It never sees <c>IProviderAdapter</c>, because that type is internal to this assembly.
/// Decision D-001's requirement — that a directly constructed provider adapter must not be a
/// compiling path — is satisfied by the visibility of the type rather than by a check.
///
/// THE PRODUCTION CHANGE (decision D-004 of its design): two members, two compositions. <see cref="Create"/>
/// builds the VENDOR adapters only, over the network client it is given, and has no fake type in reach;
/// <see cref="CreateDemonstration"/> builds the FAKES only, over a broker whose store reads no variable, and holds
/// no network client at all. Neither can build what the other builds, so a metered composition can never fall
/// back to a fake and a demonstration can never reach a vendor.
/// </summary>
public static class CapabilityGatewayFactory
{
    /// <summary>
    /// The one network client of the metered composition (correction CR-006): it follows NO REDIRECT, so a request
    /// carrying a credential never goes anywhere but the configured https endpoint, and every call is bounded by the
    /// configured provider-call bound. A redirect answer is a non-success status, booked at its worst case.
    /// </summary>
    public static HttpClient VendorClient(TimeSpan providerCallBound) =>
        new(new SocketsHttpHandler { AllowAutoRedirect = false }, disposeHandler: true) { Timeout = providerCallBound };

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

        // The production change: each endpoint names its vendor contract and the matching vendor adapter is
        // built; the delivered generic adapter, which posted a fixed body and booked failures at zero units, is
        // retired.
        var adapters = endpoints.Select(IProviderAdapter (e) => e.Contract switch
        {
            VendorContract.SpeechAudio => new SpeechAudioAdapter(e.ProviderAccount, e.Endpoint, httpClient, exchange),
            VendorContract.ImageGeneration => new ImageGenerationAdapter(e.ProviderAccount, e.Endpoint, httpClient, exchange),
            VendorContract.Messages => new MessagesAdapter(e.ProviderAccount, e.Endpoint, httpClient, exchange),
            _ => throw new ArgumentOutOfRangeException(nameof(endpoints), e.Contract, "An endpoint names one of the three vendor contracts."),
        }).ToList();

        // The provider-call bound is the client's own timeout, which every adapter call runs under; the
        // recording of an incurred attempt after a lost transaction waits longer than it (the second
        // correction cycle).
        return new CapabilityGateway(unitOfWork, broker, adapters, httpClient.Timeout);
    }

    /// <summary>
    /// The demonstration composition (the production change, decision D-004 of its design): the fakes for the
    /// named accounts, a broker over a store that reads no variable and holds no secret, and NO network client.
    /// The provider-call bound is the delivered sixty seconds, so the recording of an incurred attempt keeps its
    /// delivered timeout; no fake call waits on it.
    /// </summary>
    public static ICapabilityGateway CreateDemonstration(
        IUnitOfWork unitOfWork,
        IReadOnlyList<DemonstrationProvider> providers,
        Func<DateTimeOffset> clock)
    {
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(clock);

        var broker = CredentialBrokerFactory.CreateDemonstration(clock);
        var fakes = providers.Select(IProviderAdapter (p) => p.Contract switch
        {
            VendorContract.SpeechAudio => new FakeSpeechAudio(p.ProviderAccount),
            VendorContract.ImageGeneration => new FakeImageGeneration(p.ProviderAccount),
            VendorContract.Messages => new FakeMessages(p.ProviderAccount),
            _ => throw new ArgumentOutOfRangeException(nameof(providers), p.Contract, "A fake stands in for one of the three vendor contracts."),
        }).ToList();

        return new CapabilityGateway(unitOfWork, broker, fakes, DemonstrationCallBound);
    }

    /// <summary>The provider-call bound the demonstration composition states: the delivered client timeout.</summary>
    internal static readonly TimeSpan DemonstrationCallBound = TimeSpan.FromSeconds(60);
}
