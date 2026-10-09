using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Credentials;
using MediaCompany.Deterministic.Accounting;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using MediaCompany.Persistence;
using MediaCompany.Production;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace MediaCompany.Host;

/// <summary>
/// The produce and prepare commands (the production change, decisions D-015 and D-016 of its design). Both compose
/// from configuration alone and refuse before any call, naming each unmet precondition. One build serves every mode:
/// the mode chooses the composition, and nothing else.
/// </summary>
public static class ProductionCommands
{
    /// <summary>The settings file's values and the recorded configuration it names.</summary>
    public sealed record HostProductionSettings(
        ProductionSettings Production,
        string? CompanyStoreIdentity,
        string MaterialPath,
        string PreparationPath,
        IReadOnlyList<DemonstrationProvider> Fakes,
        IReadOnlyList<ProviderEndpoint> Endpoints);

    /// <summary>Reads the settings file; every bound and the profile is required and has no default here.</summary>
    public static HostProductionSettings ReadSettings(string path)
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

        string Text(string key) => json[key]?.GetValue<string>() is { Length: > 0 } value && !value.StartsWith('<')
            ? value
            : throw new ProductionSettingRefusedException(key, "it is not configured in the settings file");
        int Number(string key) => json[key]?.GetValue<int>() ?? throw new ProductionSettingRefusedException(key, "it is not configured in the settings file");

        var production = new ProductionSettings
        {
            OutputRoot = Text("outputRoot"),
            RepositoryRoot = Text("repositoryRoot"),
            RendererPath = Text("rendererPath"),
            ProbePath = Text("probePath"),
            FontFile = Text("fontFile"),
            Width = Number("width"),
            Height = Number("height"),
            FramesPerSecond = Number("framesPerSecond"),
            RenderBound = TimeSpan.FromMinutes(Number("renderBoundMinutes")),
            DecodeBound = TimeSpan.FromMinutes(Number("decodeBoundMinutes")),
            ProbeBound = TimeSpan.FromSeconds(Number("probeBoundSeconds")),
            StillBound = TimeSpan.FromSeconds(Number("stillBoundSeconds")),
            ProviderCallBound = TimeSpan.FromSeconds(Number("providerCallBoundSeconds")),
            NarrationCharacterMaximum = Number("narrationCharacterMaximum"),
            NarrationVoice = json["narrationVoice"]?.GetValue<string>(),
        };

        var fakes = (json["demonstrationProviders"]?.AsArray() ?? [])
            .Select(f => new DemonstrationProvider(new ProviderAccountId(f!["account"]!.GetValue<string>()), Enum.Parse<VendorContract>(f["contract"]!.GetValue<string>())))
            .ToArray();
        var endpoints = (json["endpoints"]?.AsArray() ?? [])
            .Where(e => !e!["endpoint"]!.GetValue<string>().StartsWith('<'))
            .Select(e => new ProviderEndpoint(
                new ProviderAccountId(e!["account"]!.GetValue<string>()), new Uri(e["endpoint"]!.GetValue<string>()), Enum.Parse<VendorContract>(e["contract"]!.GetValue<string>())))
            .ToArray();

        return new HostProductionSettings(
            production,
            json["companyStoreIdentity"]?.GetValue<string>(),
            Text("materialPath"),
            Text("preparationPath"),
            fakes,
            endpoints);
    }

    /// <summary>Reads the recorded preparation configuration into the record the preparation writes.</summary>
    public static PreparationRecord ReadPreparation(string path)
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();
        static Guid Id(JsonNode? node) => Guid.Parse(node!["id"]!.GetValue<string>());
        static string S(JsonNode? node, string key) => node![key]!.GetValue<string>();
        static DateOnly D(JsonNode? node, string key) => DateOnly.ParseExact(S(node, key), "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var item = json["item"];
        return new PreparationRecord
        {
            Company = new CompanyId(Id(json["company"])),
            CompanyName = S(json["company"], "name"),
            CompanyOperatingState = S(json["company"], "operatingState"),
            Channel = new ChannelId(Id(json["channel"])),
            ChannelPlatform = S(json["channel"], "platform"),
            ChannelLanguage = S(json["channel"], "language"),
            Department = new DepartmentId(Id(json["department"])),
            DepartmentName = S(json["department"], "name"),
            DepartmentBudgetHolder = S(json["department"], "budgetHolder"),
            Agent = new AgentId(Id(json["agent"])),
            AgentName = S(json["agent"], "name"),
            AgentRole = S(json["agent"], "role"),
            Accounts = json["accounts"]!.AsArray().Select(a => new PreparedAccount(
                new ProviderAccountId(S(a, "id")), S(a, "provider"), S(a, "termsBasis"), D(a, "verifiedOn"),
                Enum.Parse<CredentialScope>(S(a, "scope")), Enum.Parse<AuthenticationScheme>(S(a, "scheme")))).ToArray(),
            Models = json["models"]!.AsArray().Select(m => new PreparedModel(
                new ModelId(S(m, "id")), new ProviderAccountId(S(m, "account")), new QualityRating(m!["ratedQuality"]!.GetValue<int>()),
                new ContextCapacity(m["contextCapacity"]!.GetValue<int>()), S(m, "modality"),
                m["billedKinds"]!.AsArray().Select(k => Enum.Parse<PriceUnitKind>(k!.GetValue<string>())).ToArray())).ToArray(),
            Routes = json["routes"]!.AsArray().Select(r => new PreparedRoute(
                new RouteId(Id(r)), Enum.Parse<CapabilityClass>(S(r, "capability")), Enum.Parse<RouteTier>(S(r, "tier")),
                new ProviderAccountId(S(r, "account")), new ModelId(S(r, "model")), new QualityRating(r!["ratedQuality"]!.GetValue<int>()),
                new ContextCapacity(r["contextCapacity"]!.GetValue<int>()), S(r, "termsBasis"), D(r, "termsVerifiedOn"),
                new RouteTermsPositions(
                    Enum.Parse<AutomatedAccessPosition>(S(r, "automatedAccess")), Enum.Parse<CustomerContentPosition>(S(r, "customerContent")),
                    S(r, "positionsEvidence"), D(r, "positionsReadOn")))).ToArray(),
            Prices = json["prices"]!.AsArray().Select(p => new PreparedPrice(
                new ModelPriceId(Id(p)), new ModelId(S(p, "model")), Enum.Parse<PriceUnitKind>(S(p, "kind")), p!["unitPrice"]!.GetValue<decimal>(),
                S(p, "currency"), S(p, "source"), D(p, "verifiedOn"), DateTimeOffset.Parse(S(p, "validFrom"), CultureInfo.InvariantCulture))).ToArray(),
            Item = new ItemId(Id(item)),
            ItemTitle = S(item, "title"),
            PackageVersion = new ItemVersion(item!["packageVersion"]!.GetValue<int>()),
            PackageStages = item["stages"]!.AsArray().Select(s => new PreparedStage(
                Enum.Parse<ProductionStage>(S(s, "stage")), Enum.Parse<StageOutcome>(S(s, "outcome")), S(s, "summary"))).ToArray(),
            Cap = new Money(json["cap"]!["amount"]!.GetValue<decimal>(), S(json["cap"], "currency")),
            CapSource = S(json["cap"], "source"),
        };
    }

    /// <summary>
    /// prepare: loads the recorded configuration and the package into the connected store under the designation
    /// named. A company preparation is refused unless the connected database is the configured company store; the
    /// orchestrator alone runs it, against the company store, immediately before the owner's go.
    /// </summary>
    public static async Task<int> PrepareAsync(ServiceProvider provider, string[] args, TextWriter output, TextWriter errors, CancellationToken cancellationToken)
    {
        var settingsPath = Option(args, "--settings");
        var designationText = Option(args, "--designation");
        if (settingsPath is null || designationText is null
            || !Enum.TryParse<StoreDesignation>(designationText, ignoreCase: true, out var designation))
        {
            await errors.WriteLineAsync("Usage: prepare --settings <settings.json> --designation demonstration|company").ConfigureAwait(false);
            return 2;
        }

        var settings = ReadSettings(settingsPath);
        var identity = await provider.GetRequiredService<IStoreDesignationReader>().ReadAsync(cancellationToken).ConfigureAwait(false);
        if (designation == StoreDesignation.Company
            && !string.Equals(identity.DatabaseName, settings.CompanyStoreIdentity, StringComparison.Ordinal))
        {
            await errors.WriteLineAsync($"Refused: a company preparation runs only against the configured company store {settings.CompanyStoreIdentity}, and this store is {identity.DatabaseName}.").ConfigureAwait(false);
            return 2;
        }

        // The material is checked against the package it quotes before anything is written.
        _ = await ItemPackageLoader.LoadAsync(settings.Production.RepositoryRoot, settings.MaterialPath, cancellationToken).ConfigureAwait(false);
        var record = ReadPreparation(Path.Combine(settings.Production.RepositoryRoot, settings.PreparationPath));
        var outcome = await provider.GetRequiredService<IPreparationWriter>().PrepareAsync(record, designation, cancellationToken).ConfigureAwait(false);
        if (outcome.Refused)
        {
            foreach (var refusal in outcome.Refusals)
            {
                await errors.WriteLineAsync($"Refused: {refusal}").ConfigureAwait(false);
            }

            return 2;
        }

        await output.WriteLineAsync($"Prepared {identity.DatabaseName} as a {designation} store: {outcome.Inserted} row(s) inserted, {outcome.AlreadyPresent} already present and identical; no budget amount and no configuration value recorded.").ConfigureAwait(false);
        return 0;
    }

    /// <summary>
    /// produce: one item in fake, plan-only or metered mode. The plan, its estimate and the cap are printed before
    /// any call; metered mode refuses before any call naming every unmet precondition.
    /// </summary>
    public static async Task<int> ProduceAsync(ServiceProvider provider, string[] args, TextWriter output, TextWriter errors, CancellationToken cancellationToken)
    {
        var settingsPath = Option(args, "--settings");
        var modeText = Option(args, "--mode");
        if (settingsPath is null || modeText is null || !TryMode(modeText, out var mode))
        {
            await errors.WriteLineAsync("Usage: produce --mode fake|plan-only|metered --settings <settings.json>").ConfigureAwait(false);
            return 2;
        }

        HostProductionSettings settings;
        LoadedItem loaded;
        PreparationRecord preparation;
        try
        {
            settings = ReadSettings(settingsPath);
            loaded = await ItemPackageLoader.LoadAsync(settings.Production.RepositoryRoot, settings.MaterialPath, cancellationToken).ConfigureAwait(false);
            preparation = ReadPreparation(Path.Combine(settings.Production.RepositoryRoot, settings.PreparationPath));
        }
        catch (Exception refused) when (refused is ProductionSettingRefusedException or ItemMaterialRefusedException or FileNotFoundException)
        {
            await errors.WriteLineAsync($"Refused before any call: {refused.Message}").ConfigureAwait(false);
            return 2;
        }

        var identity = await provider.GetRequiredService<IStoreDesignationReader>().ReadAsync(cancellationToken).ConfigureAwait(false);
        var guard = StoreGuard.Judge(mode, identity.Designation, identity.DatabaseName, settings.CompanyStoreIdentity);
        if (mode != ProductionMode.Metered && !guard.Admitted)
        {
            foreach (var refusal in guard.Refusals)
            {
                await errors.WriteLineAsync($"Refused before any call: {refusal}").ConfigureAwait(false);
            }

            return 2;
        }

        var request = new ProduceRequest
        {
            Item = preparation.Item,
            Mode = mode,
            Loaded = loaded,
            Department = preparation.Department,
            Agent = preparation.Agent,
            Designation = identity.Designation ?? StoreDesignation.Demonstration,
        };

        var unitOfWork = provider.GetRequiredService<IUnitOfWork>();
        var reader = provider.GetRequiredService<IProductionReader>();

        if (mode == ProductionMode.PlanOnly)
        {
            // Plan only reads: it builds neither the writer nor the process starter, so it can write nothing and start nothing.
            var planOnly = new ProduceItemService(unitOfWork, reader, new NothingStarted(), new NothingWritten(), settings.Production, output);
            return (await planOnly.RunAsync(request, null, cancellationToken).ConfigureAwait(false)).ExitCode;
        }

        IArtifactStore store;
        IMediaTool tool;
        try
        {
            store = new ArtifactWriter(settings.Production.OutputRoot, settings.Production.RepositoryRoot);
            tool = new ExternalMediaTool(settings.Production);
        }
        catch (ProductionSettingRefusedException refused)
        {
            await errors.WriteLineAsync($"Refused before any call: {refused.Message}").ConfigureAwait(false);
            return 2;
        }

        var service = new ProduceItemService(unitOfWork, reader, tool, store, settings.Production, output);
        ICapabilityGateway gateway;

        if (mode == ProductionMode.Fake)
        {
            // The demonstration composition: the fakes, a broker reading no variable, and no network client.
            gateway = CapabilityGatewayFactory.CreateDemonstration(unitOfWork, settings.Fakes, () => DateTimeOffset.UtcNow);
        }
        else
        {
            var plan = await service.PlanAsync(request, cancellationToken).ConfigureAwait(false);
            if (plan is null)
            {
                return 2;
            }

            var unmet = MeteredPreconditions.Unmet(await ReadinessAsync(unitOfWork, plan, guard, settings, preparation, cancellationToken).ConfigureAwait(false));
            if (unmet.Count > 0)
            {
                plan.Print(output, mode, request.Designation, settings.Production, loaded.Material);
                foreach (var condition in unmet)
                {
                    await errors.WriteLineAsync($"Refused before any call: {condition}").ConfigureAwait(false);
                }

                return 2;
            }

            var secrets = new SecretStoreOptions();
            var broker = CredentialBrokerFactory.Create(secrets, () => DateTimeOffset.UtcNow);
            var http = new HttpClient { Timeout = settings.Production.ProviderCallBound };
            gateway = CapabilityGatewayFactory.Create(unitOfWork, broker, broker, new SystemClock(), settings.Endpoints, http);
        }

        return (await service.RunAsync(request, gateway, cancellationToken).ConfigureAwait(false)).ExitCode;
    }

    /// <summary>Gathers every value the metered preconditions read, before any call, reading no secret value.</summary>
    public static async Task<MeteredReadiness> ReadinessAsync(
        IUnitOfWork unitOfWork, ProductionPlan plan, StoreGuardVerdict guard, HostProductionSettings settings, PreparationRecord preparation, CancellationToken cancellationToken)
    {
        ControllerDecision? controller = null;
        await using (var transaction = await unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false))
        {
            // Read only: the transaction reserves the datastore's instant, reads the governing readings and is rolled back.
            var reserved = await transaction.Admission.ReserveAsync(cancellationToken).ConfigureAwait(false);
            var readings = await transaction.Admission.GoverningReadingsAsync(
                new Attribution(preparation.Item, plan.Package.Channel, preparation.Department, preparation.Agent), ApprovedEnvelope.Metered, cancellationToken).ConfigureAwait(false);
            controller = CostController.Decide(readings, reserved.Instant, itemCapped: plan.Package.Cap is not null);
        }

        var planned = plan.Account is null ? [] : new[] { plan.Account };
        return new MeteredReadiness
        {
            Plan = plan,
            StoreRefusals = guard.Refusals,
            Controller = controller,
            Credentials = planned
                .Select(a => (CredentialBrokerFactory.VariableName(a.Id, a.Scope == CredentialScope.Company ? null : plan.Package.Channel),
                              CredentialBrokerFactory.IsPublished(a.Id, a.Scope == CredentialScope.Company ? null : plan.Package.Channel)))
                .ToArray(),
            FakesConfigured = planned.Where(a => settings.Fakes.Any(f => f.ProviderAccount == a.Id)).Select(a => a.Id).ToArray(),
            EndpointsMissing = planned.Where(a => !settings.Endpoints.Any(e => e.ProviderAccount == a.Id && e.Contract == VendorContract.SpeechAudio)).Select(a => a.Id).ToArray(),
            NarrationVoice = settings.Production.NarrationVoice,
        };
    }

    private static bool TryMode(string text, out ProductionMode mode)
    {
        mode = text switch
        {
            "fake" => ProductionMode.Fake,
            "plan-only" => ProductionMode.PlanOnly,
            "metered" => ProductionMode.Metered,
            _ => 0,
        };
        return mode != 0;
    }

    private static string? Option(string[] args, string name)
    {
        var at = Array.IndexOf(args, name);
        return at >= 0 && at + 1 < args.Length ? args[at + 1] : null;
    }

    /// <summary>The plan-only stand-in for the process starter: it starts nothing, and refuses if it is ever asked to.</summary>
    private sealed class NothingStarted : IMediaTool
    {
        private static InvalidOperationException Refused() => new("plan-only mode starts no process");

        public Task<string> VersionLineAsync(CancellationToken cancellationToken) => throw Refused();

        public Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken) => throw Refused();

        public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, CancellationToken cancellationToken) => throw Refused();

        public Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken) => throw Refused();

        public Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken) => throw Refused();

        public Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken) => throw Refused();
    }

    /// <summary>The plan-only stand-in for the writer: it writes nothing, and refuses if it is ever asked to.</summary>
    private sealed class NothingWritten : IArtifactStore
    {
        private static InvalidOperationException Refused() => new("plan-only mode writes no file");

        public string OutputRoot => throw Refused();

        public string BeginRun(string runFolder) => throw Refused();

        public Task WriteStagingTextAsync(string stagingFolder, string name, string text, CancellationToken cancellationToken) => throw Refused();

        public Task CopyIntoStagingAsync(string stagingFolder, string sourceFile, string name, CancellationToken cancellationToken) => throw Refused();

        public Task<StoredFile> StoreAsync(string stagingFolder, ReadOnlyMemory<byte> bytes, string relativePath, CancellationToken cancellationToken) => throw Refused();

        public Task<StoredFile> PromoteAsync(string stagingFolder, string stagingName, string relativePath, CancellationToken cancellationToken) => throw Refused();

        public string FullPath(string relativePath) => throw Refused();
    }
}
