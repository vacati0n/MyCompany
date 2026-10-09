using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Services;
using MediaCompany.Host;
using MediaCompany.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

// The one long-running service on one node (stack O-005).
//
// This wave publishes nothing, creates no channel and no account, and commits no spend
// (constraint C-013). The entry point installs the record store, produces the operating registers
// from the company's own records, prints the measurable-now report with every deferred measure
// named, and, from the AI-management change, prints the weekly management reports and the CEO brief
// and the read-only dashboard from one read of the company's records. Nothing here reaches a
// provider, calls a model, writes a file or opens a listener.

var connectionString = Environment.GetEnvironmentVariable("MEDIACOMPANY_CONNECTION_STRING");
if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.Error.WriteLine(
        "MEDIACOMPANY_CONNECTION_STRING is not set. The service needs the PostgreSQL datastore selected at the Design Gate.");
    return 2;
}

var command = args.Length > 0 ? args[0] : "report";

await using var provider = CompositionRoot.Build(new HostOptions
{
    ConnectionString = connectionString,
    // Empty until the owner provisions the production accounts. Risk RK-002 requires every unit
    // price to be re-fetched immediately before any spend, and this wave commits none.
    ProviderEndpoints = [],
});

var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

switch (command)
{
    case "install":
    {
        var dataSource = provider.GetRequiredService<NpgsqlDataSource>();
        await SchemaInstaller.InstallAsync(dataSource, cancellation.Token);
        Console.WriteLine("Record store created. Establish the backup and restore position before the first entry is written.");
        return 0;
    }

    case "check":
    {
        // Resolves every registration without opening a connection, so the composition root is
        // verifiable before a datastore exists. It also states what is NOT registered: no provider
        // adapter, because no provider adapter type is visible outside its assembly (decision
        // D-001), and no secret value, because the broker holds none (decision D-004).
        _ = provider.GetRequiredService<MediaCompany.Capability.ICapabilityGateway>();
        _ = provider.GetRequiredService<PublicationGateService>();
        _ = provider.GetRequiredService<WorkLifecycleService>();
        _ = provider.GetRequiredService<ReportingService>();
        _ = provider.GetRequiredService<OperatingRegisterReport>();
        _ = provider.GetRequiredService<PermissionAnswerService>();

        // The analytics surface and the two read ports registered beside it. A registration
        // defect here would otherwise surface only against a datastore, which is the one thing
        // this command exists to make unnecessary.
        _ = provider.GetRequiredService<AnalyticsReportService>();
        _ = provider.GetRequiredService<IServedTierReader>();
        _ = provider.GetRequiredService<IRevenueParameterRegister>();

        // The sustained-rate capability: the two readers, the dossier recorder and the publishing
        // entry point, each resolved here so a wiring defect surfaces without a datastore.
        _ = provider.GetRequiredService<IThroughputReader>();
        _ = provider.GetRequiredService<IItemDossierReader>();
        _ = provider.GetRequiredService<ItemDossierRecorder>();
        _ = provider.GetRequiredService<PublishingSequenceService>();

        // The multi-channel capability: the three read ports, the channel profile service, the
        // copyright-check stage handler and the rights-check step.
        _ = provider.GetRequiredService<IItemRegister>();
        _ = provider.GetRequiredService<IChannelPartitionReader>();
        _ = provider.GetRequiredService<IApprovalQueueReader>();
        _ = provider.GetRequiredService<ChannelProfileService>();
        _ = provider.GetRequiredService<MediaCompany.Deterministic.Production.CopyrightCheckStageHandler>();
        _ = provider.GetRequiredService<RightsCheckStep>();

        // The AI-economics capability: the benchmark record's reader, which has read members only. The
        // admission ledger and the benchmark writer are reached only from a transaction, so there is no
        // registration of either to resolve.
        _ = provider.GetRequiredService<IBenchmarkReader>();

        // The AI-management capability: the company record reader, read members only, and the management
        // surface over it, which takes that reader and nothing else.
        _ = provider.GetRequiredService<ICompanyRecordReader>();
        _ = provider.GetRequiredService<ManagementReportService>();

        Console.WriteLine("Composition root resolved.");
        Console.WriteLine("  capability egress: ICapabilityGateway only; no provider adapter is registered or registrable.");
        Console.WriteLine("  credentials:       opaque scoped handles; no secret value passes through this process boundary.");
        Console.WriteLine("  provider endpoints configured: 0 (this wave creates no account and commits no spend).");
        Console.WriteLine("  analytics surface:  resolved, with its read ports; it writes nothing and reaches no capability.");
        Console.WriteLine("  publishing sequence: resolved; it ends at composition and reaches no capability.");
        Console.WriteLine("  channel readings:   resolved; every reading partitions by channel, and nothing records a channel value.");
        Console.WriteLine("  benchmark record:   resolved, read members only; nothing records an observation, and no metered call exists to make one.");
        Console.WriteLine("  management surface: resolved over one read-only snapshot; it writes nothing, calls no model and offers no action.");
        Console.WriteLine($"  declared version:   {typeof(CompositionRoot).Assembly.GetName().Version}");
        return 0;
    }

    case "registers":
    {
        var registers = await provider.GetRequiredService<OperatingRegisterReport>().ProduceAsync(cancellation.Token);
        Console.WriteLine($"Channels:    {registers.Channels.Count}");
        Console.WriteLine($"Departments: {registers.Departments.Count}");
        Console.WriteLine($"Agents:      {registers.Agents.Count}");
        Console.WriteLine("Role permissions (the closed action set, mirrored from code):");
        foreach (var (role, actions) in registers.RolePermissions.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            Console.WriteLine($"  {role}: {string.Join(", ", actions)}");
        }

        Console.WriteLine("Capability prices presented as current (an entry with no verification date is omitted):");
        foreach (var entry in registers.Capabilities)
        {
            Console.WriteLine($"  {entry.Model} {entry.UnitKind} {entry.UnitPrice} {entry.Currency} — {entry.Source}, verified {entry.VerifiedOn}");
        }

        return 0;
    }

    case "report":
    {
        var reporting = provider.GetRequiredService<ReportingService>();

        // The month on the DATASTORE'S clock, never the process clock (the AI-management change, decision D-015).
        var period = await reporting.CurrentPeriodAsync(cancellation.Token);

        Console.WriteLine($"Measurable now, period {period:yyyy-MM}:");
        foreach (var measure in await reporting.MeasurableNowAsync(period, cancellation.Token))
        {
            var label = measure.IsEstimate ? " [ESTIMATE — unit prices not verified first-hand]" : string.Empty;
            Console.WriteLine($"  {measure.Name}: {measure.Quantity.Describe()} ({measure.Source}){label}");
        }

        Console.WriteLine("Deferred, each naming the parameter it waits on:");
        foreach (var deferred in reporting.DeferredMeasures())
        {
            Console.WriteLine($"  {deferred}");
        }

        return 0;
    }

    case "weekly":
    case "dashboard":
    {
        // ONE READ of the company's records, composed into every surface (the AI-management change). An optional
        // second argument names an ISO week, as 2026-W41; without it the week is the one containing the
        // datastore's instant. A malformed week and a week that does not exist are refused by name with exit 2.
        // Console text only: no file is written and no listener is opened.
        var exit = await ManagementConsole.RunAsync(
            provider.GetRequiredService<ManagementReportService>(),
            command,
            args.Length > 1 ? args[1] : null,
            Console.Out,
            Console.Error,
            cancellation.Token);
        return (int)exit;
    }

    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Known commands: check, install, registers, report, weekly, dashboard.");
        return 2;
}
