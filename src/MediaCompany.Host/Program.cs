using MediaCompany.Deterministic.Services;
using MediaCompany.Host;
using MediaCompany.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

// The one long-running service on one node (stack O-005).
//
// This wave publishes nothing, creates no channel and no account, and commits no spend
// (constraint C-013). The entry point therefore does three things and stops: it installs the
// record store, it produces the operating registers from the company's own records, and it prints
// the measurable-now report with every deferred measure named. Nothing here reaches a provider.

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

        Console.WriteLine("Composition root resolved.");
        Console.WriteLine("  capability egress: ICapabilityGateway only; no provider adapter is registered or registrable.");
        Console.WriteLine("  credentials:       opaque scoped handles; no secret value passes through this process boundary.");
        Console.WriteLine("  provider endpoints configured: 0 (this wave creates no account and commits no spend).");
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
        var period = reporting.CurrentPeriod();

        Console.WriteLine($"Measurable now, period {period:yyyy-MM}:");
        foreach (var measure in await reporting.MeasurableNowAsync(period, cancellation.Token))
        {
            var label = measure.IsEstimate ? " [ESTIMATE — unit prices not verified first-hand]" : string.Empty;
            Console.WriteLine($"  {measure.Name}: {measure.Value} ({measure.Source}){label}");
        }

        Console.WriteLine("Deferred, each naming the parameter it waits on:");
        foreach (var deferred in reporting.DeferredMeasures())
        {
            Console.WriteLine($"  {deferred}");
        }

        return 0;
    }

    default:
        Console.Error.WriteLine($"Unknown command '{command}'. Known commands: check, install, registers, report.");
        return 2;
}
