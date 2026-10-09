using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Credentials;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Production;
using MediaCompany.Deterministic.Services;
using MediaCompany.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MediaCompany.Host;

/// <summary>The system clock.</summary>
public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

/// <summary>What the one long-running service needs to start.</summary>
public sealed record HostOptions
{
    public required string ConnectionString { get; init; }

    /// <summary>
    /// The provider endpoints the capability boundary may reach. This wave creates no account and
    /// commits no spend (constraint C-013), so the list is empty until the owner provisions the
    /// production accounts.
    /// </summary>
    public IReadOnlyList<ProviderEndpoint> ProviderEndpoints { get; init; } = [];

    public SecretStoreOptions SecretStore { get; init; } = new();
}

/// <summary>
/// The composition root of the one long-running service on one node (stack O-005).
///
/// Two things are worth reading here rather than in a document. First, the only capability type
/// registered is <see cref="ICapabilityGateway"/>: no provider adapter is registered, because no
/// provider adapter type is visible outside its assembly, so no consumer can take one as a
/// dependency (decision D-001). Second, no credential value passes through this method: the
/// broker is built from options and resolves secrets from the external store per use (decision
/// D-004).
/// </summary>
public static class CompositionRoot
{
    public static ServiceProvider Build(HostOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var services = new ServiceCollection();

        services.AddSingleton(NpgsqlDataSource.Create(options.ConnectionString));
        services.AddSingleton<IClock, SystemClock>();

        // Persistence — the only path to durable state (module M-017).
        services.AddSingleton<IUnitOfWork>(sp =>
            new NpgsqlUnitOfWork(sp.GetRequiredService<NpgsqlDataSource>(), sp.GetRequiredService<IClock>()));
        services.AddSingleton<IRouteRegistry>(sp => new NpgsqlRouteRegistry(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IRouteAvailabilityLedger>(sp => new NpgsqlRouteAvailabilityLedger(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IOperatingRegisters>(sp => new NpgsqlOperatingRegisters(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IAssetLedger>(sp => new NpgsqlAssetLedger(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IGateLedger>(sp => new NpgsqlGateLedger(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IConfigurationStore>(sp => new NpgsqlConfigurationStore(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton(sp => new NpgsqlCostReader(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<ICostRollupReader>(sp => sp.GetRequiredService<NpgsqlCostReader>());

        // The AI-economics change: the budget reader is retired (the boundary reads its headroom and the
        // cost controller's readings on its own admission transaction), and the benchmark record's
        // reader, which has read members only, is registered for the analytics surface.
        services.AddSingleton<IBenchmarkReader>(sp => new NpgsqlBenchmarkReader(sp.GetRequiredService<NpgsqlDataSource>()));

        // The analytics read ports. The served-tier reader is the same adapter that already reads
        // the cost rollups, because both read the accounted-operation table; the revenue register
        // is its own adapter over the register the appended schema resource creates empty.
        services.AddSingleton<IServedTierReader>(sp => sp.GetRequiredService<NpgsqlCostReader>());
        services.AddSingleton<IRevenueParameterRegister>(sp =>
            new NpgsqlRevenueParameterRegister(sp.GetRequiredService<NpgsqlDataSource>()));

        // The throughput reader over the append-only record, and the item dossier reader over the
        // dossier register the fifth schema resource creates empty. Both read upward from below
        // the ports; neither writes.
        services.AddSingleton<IThroughputReader>(sp =>
            new NpgsqlThroughputReader(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IItemDossierReader>(sp =>
            new NpgsqlItemDossierReader(sp.GetRequiredService<NpgsqlDataSource>()));

        // The multi-channel change: the item register a gate decision reads the item's channel from,
        // the channel partition reader every partitioned reading is aggregated by, and the approval
        // queue reader, which has read members only. None of them writes.
        services.AddSingleton<IItemRegister>(sp => new NpgsqlItemRegister(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IChannelPartitionReader>(sp =>
            new NpgsqlChannelPartitionReader(sp.GetRequiredService<NpgsqlDataSource>()));
        services.AddSingleton<IApprovalQueueReader>(sp =>
            new NpgsqlApprovalQueueReader(sp.GetRequiredService<NpgsqlDataSource>()));

        // The credential broker — the only module holding a dependency on the secret store.
        services.AddSingleton(sp =>
        {
            var clock = sp.GetRequiredService<IClock>();
            return CredentialBrokerFactory.Create(options.SecretStore, () => clock.UtcNow);
        });
        services.AddSingleton<ICredentialBroker>(sp => sp.GetRequiredService<CredentialBroker>());
        services.AddSingleton<ICredentialExchange>(sp => sp.GetRequiredService<CredentialBroker>());

        services.AddSingleton(_ => new HttpClient { Timeout = TimeSpan.FromSeconds(60) });

        // The capability boundary — the single egress. Nothing below it is registered, because
        // nothing below it is visible.
        services.AddSingleton<ICapabilityGateway>(sp => CapabilityGatewayFactory.Create(
            sp.GetRequiredService<IRouteRegistry>(),
            sp.GetRequiredService<IRouteAvailabilityLedger>(),
            sp.GetRequiredService<IOperatingRegisters>(),
            sp.GetRequiredService<IUnitOfWork>(),
            sp.GetRequiredService<ICredentialBroker>(),
            sp.GetRequiredService<ICredentialExchange>(),
            sp.GetRequiredService<IClock>(),
            options.ProviderEndpoints,
            sp.GetRequiredService<HttpClient>()));

        // Deterministic services. None of these takes ICapabilityGateway, and none could: their
        // assembly does not reference the one that declares it.
        services.AddSingleton<PublicationGateService>();
        services.AddSingleton<WorkLifecycleService>();
        services.AddSingleton<PublicationDispatchService>();
        services.AddSingleton<ReportingService>();
        services.AddSingleton<OperatingRegisterReport>();
        services.AddSingleton<PermissionAnswerService>();

        // The dossier recorder a production step writes through, and the publishing entry point.
        // Both sit in the rule-determined assembly, so neither can take the capability gateway or
        // the credential broker, and neither reads a configuration value.
        services.AddSingleton<ItemDossierRecorder>();
        services.AddSingleton<PublishingSequenceService>();

        // The multi-channel change: the channel profile service, the one reader of a channel key; the
        // copyright-check stage handler; and the rights-check step, the dossier recorder's first
        // production caller. All three sit in the rule-determined assembly, so none can take the
        // capability gateway or the credential broker, and none performs a metered operation.
        services.AddSingleton<ChannelProfileService>();
        services.AddSingleton<CopyrightCheckStageHandler>();
        services.AddSingleton<RightsCheckStep>();

        // The analytics surface. It takes read ports and a clock and nothing else; it could not
        // take the capability gateway, because its assembly does not reference the one that
        // declares it.
        services.AddSingleton<AnalyticsReportService>();

        return services.BuildServiceProvider();
    }
}
