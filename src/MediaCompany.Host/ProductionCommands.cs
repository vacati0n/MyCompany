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

    /// <summary>
    /// Reads the settings file; every bound and the profile is required and has no default here. The vendor members (the
    /// provider-call bound, the character maximum and the voice) are optional and required by name only by the modes that
    /// use them; the in-house model's section is optional and, where present, every member of it is required.
    /// </summary>
    public static HostProductionSettings ReadSettings(string path)
    {
        var json = JsonNode.Parse(File.ReadAllText(path))!.AsObject();

        string Text(string key) => json[key]?.GetValue<string>() is { Length: > 0 } value && !value.StartsWith('<')
            ? value
            : throw new ProductionSettingRefusedException(key, "it is not configured in the settings file");
        int Number(string key) => json[key]?.GetValue<int>() ?? throw new ProductionSettingRefusedException(key, "it is not configured in the settings file");
        int? Optional(string key) => json[key] is { } node ? node.GetValue<int>() : null;

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
            ProviderCallBound = Optional("providerCallBoundSeconds") is { } call ? TimeSpan.FromSeconds(call) : null,
            NarrationCharacterMaximum = Optional("narrationCharacterMaximum"),
            NarrationVoice = json["narrationVoice"]?.GetValue<string>(),
            InHouseModel = json["inHouseModel"] is JsonObject model ? ReadModel(model) : null,
        };

        var fakes = (json["demonstrationProviders"]?.AsArray() ?? [])
            .Select(f => new DemonstrationProvider(new ProviderAccountId(f!["account"]!.GetValue<string>()), Enum.Parse<VendorContract>(f["contract"]!.GetValue<string>())))
            .ToArray();
        // A VENDOR ENDPOINT IS REACHED OVER HTTPS ONLY (correction CR-006): an endpoint that is not an absolute https
        // address is refused by name before anything is read or called. The demonstration providers are not endpoints:
        // they name a fake, which reaches no network at all.
        var endpoints = (json["endpoints"]?.AsArray() ?? [])
            .Where(e => !e!["endpoint"]!.GetValue<string>().StartsWith('<'))
            .Select(e =>
            {
                var account = e!["account"]!.GetValue<string>();
                var address = e["endpoint"]!.GetValue<string>();
                if (!Uri.TryCreate(address, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
                {
                    throw new ProductionSettingRefusedException(
                        "endpoints", $"the endpoint configured for account {account} is not an absolute https address; a vendor endpoint is reached over https only");
                }

                return new ProviderEndpoint(new ProviderAccountId(account), uri, Enum.Parse<VendorContract>(e["contract"]!.GetValue<string>()));
            })
            .ToArray();

        return new HostProductionSettings(
            production,
            json["companyStoreIdentity"]?.GetValue<string>(),
            Text("materialPath"),
            Text("preparationPath"),
            fakes,
            endpoints);
    }

    /// <summary>
    /// Reads the in-house model's section (the own-voice change, decision D-011 of its design). Every member is required and
    /// refused by name where absent or a placeholder; no member has a default here. The licences are optional and, where
    /// given, carry the source the configuration names.
    /// </summary>
    private static InHouseModelSettings ReadModel(JsonObject model)
    {
        string Text(JsonNode? node, string key) => node?[key]?.GetValue<string>() is { Length: > 0 } value && !value.StartsWith('<')
            ? value
            : throw new ProductionSettingRefusedException($"inHouseModel.{key}", "it is not configured in the settings file");
        ExpectedFile File(string key) => model[key] is JsonObject file
            ? new ExpectedFile(Text(file, "path"), Text(file, "sha256").ToLowerInvariant())
            : throw new ProductionSettingRefusedException($"inHouseModel.{key}", "it is not configured in the settings file (a path and a sha256)");
        decimal Value(JsonNode? node, string key) => node?[key] is { } value
            ? value.GetValue<decimal>()
            : throw new ProductionSettingRefusedException($"inHouseModel.generation.{key}", "it is not configured in the settings file");
        ConfiguredLicence? Licence(string key) => model[key] is JsonObject licence
            ? new ConfiguredLicence(Text(licence, "value"), Text(licence, "source"))
            : null;

        var generation = model["generation"] as JsonObject
            ?? throw new ProductionSettingRefusedException("inHouseModel.generation", "it is not configured in the settings file");
        var bound = model["partBoundSeconds"]?.GetValue<int>()
            ?? throw new ProductionSettingRefusedException("inHouseModel.partBoundSeconds", "it is not configured in the settings file; the model part bound has no default");
        return new InHouseModelSettings
        {
            Interpreter = File("interpreter"),
            EnvironmentConfiguration = File("environmentConfiguration"),
            BaseInterpreterSha256 = Text(model, "baseInterpreterSha256").ToLowerInvariant(),
            RuntimeModule = Text(model, "runtimeModule"),
            Model = File("model"),
            Configuration = File("configuration"),
            ModelCard = File("modelCard"),
            RuntimeRecord = File("runtimeRecord"),
            DependencyRecords = (model["dependencyRecords"] as JsonArray
                    ?? throw new ProductionSettingRefusedException("inHouseModel.dependencyRecords", "it is not configured in the settings file"))
                .Select(d => new ExpectedFile(Text(d, "path"), Text(d, "sha256").ToLowerInvariant()))
                .ToArray(),
            Generation = new VoiceGenerationSettings(
                Value(generation, "lengthScale"),
                Value(generation, "noiseScale"),
                Value(generation, "noiseWidthScale"),
                Value(generation, "sentenceSilenceSeconds"),
                Value(generation, "volume"),
                generation["normalize"]?.GetValue<bool>()
                    ?? throw new ProductionSettingRefusedException("inHouseModel.generation.normalize", "it is not configured in the settings file")),
            PartBound = bound > 0
                ? TimeSpan.FromSeconds(bound)
                : throw new ProductionSettingRefusedException("inHouseModel.partBoundSeconds", "it must be a positive number of seconds"),
            WeightsLicence = Licence("weightsLicence"),
            VoiceLicence = Licence("voiceLicence"),
        };
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
    /// produce: one item in own, fake, plan-only or metered mode. The plan, with every part's narration source and the reason
    /// it was chosen, is printed before anything runs. The own mode composes the writer and the one process starter (which
    /// also runs the in-house model) and NO capability boundary, no credential broker and no network client; fake mode
    /// composes the demonstration boundary; plan-only composes neither writer nor starter; metered mode is refused by the
    /// narration source rule naming the owner's decision of 2026-10-10, before anything is composed.
    /// </summary>
    public static async Task<int> ProduceAsync(ServiceProvider provider, string[] args, TextWriter output, TextWriter errors, CancellationToken cancellationToken)
    {
        var settingsPath = Option(args, "--settings");
        var modeText = Option(args, "--mode");
        if (settingsPath is null || modeText is null || !TryMode(modeText, out var mode))
        {
            await errors.WriteLineAsync("Usage: produce --mode own|fake|plan-only|metered --settings <settings.json>").ConfigureAwait(false);
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
        if (mode == ProductionMode.Metered)
        {
            // THE VENDOR BAR (the own-voice change, decision D-002 of its design): the rule reads no setting, so no settings
            // value composes the vendor; nothing is read, composed or called.
            await errors.WriteLineAsync($"Refused before any call: {NarrationSourceRule.VendorBar}").ConfigureAwait(false);
            return 2;
        }

        if (!guard.Admitted)
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
            // Plan only reads: it builds neither the writer nor the process starter, so it can write nothing and start nothing;
            // it re-hashes registered copies and verifies the model's files by reading them.
            var planOnly = new ProduceItemService(unitOfWork, reader, new NothingStarted(), new NothingWritten(), settings.Production, output);
            return (await planOnly.RunAsync(request, null, cancellationToken).ConfigureAwait(false)).ExitCode;
        }

        IArtifactStore store;
        ExternalMediaTool tool;
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

        if (mode == ProductionMode.Own)
        {
            // The own composition: the writer and the one starter, which also runs the in-house model. No capability boundary,
            // no credential broker and no network client is constructed.
            var own = new ProduceItemService(unitOfWork, reader, tool, store, settings.Production, output, tool);
            return (await own.RunAsync(request, null, cancellationToken).ConfigureAwait(false)).ExitCode;
        }

        // The demonstration composition: the fakes, a broker reading no variable, and no network client.
        var service = new ProduceItemService(unitOfWork, reader, tool, store, settings.Production, output);
        var gateway = CapabilityGatewayFactory.CreateDemonstration(unitOfWork, settings.Fakes, () => DateTimeOffset.UtcNow);
        return (await service.RunAsync(request, gateway, cancellationToken).ConfigureAwait(false)).ExitCode;
    }

    /// <summary>
    /// register-recording: registers one folder of beat recordings against the item the settings' material names (the own-voice
    /// change, decision D-011 of its design). Exits 0 registered, 2 refused before anything is recorded naming every finding,
    /// 3 a tool or store failure named. The release fields are each optional and recorded ABSENT when not given.
    /// </summary>
    public static async Task<int> RegisterRecordingAsync(ServiceProvider provider, string[] args, TextWriter output, TextWriter errors, CancellationToken cancellationToken)
    {
        var settingsPath = Option(args, "--settings");
        var folder = Option(args, "--recordings");
        if (settingsPath is null || folder is null)
        {
            await errors.WriteLineAsync("Usage: register-recording --settings <settings.json> --recordings <folder> [--performer <name>] "
                + "[--release-reference <reference>] [--release-date yyyy-mm-dd] [--release-document <path outside the repository>] [--training-term <term>]").ConfigureAwait(false);
            return 2;
        }

        DateOnly? date = null;
        if (Option(args, "--release-date") is { } dateText)
        {
            if (!DateOnly.TryParseExact(dateText, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
            {
                await errors.WriteLineAsync($"Refused, nothing was recorded: the release date {dateText} is not a date written yyyy-mm-dd").ConfigureAwait(false);
                return 2;
            }

            date = parsed;
        }

        HostProductionSettings settings;
        LoadedItem loaded;
        PreparationRecord preparation;
        IArtifactStore store;
        ExternalMediaTool tool;
        try
        {
            settings = ReadSettings(settingsPath);
            loaded = await ItemPackageLoader.LoadAsync(settings.Production.RepositoryRoot, settings.MaterialPath, cancellationToken).ConfigureAwait(false);
            preparation = ReadPreparation(Path.Combine(settings.Production.RepositoryRoot, settings.PreparationPath));
            store = new ArtifactWriter(settings.Production.OutputRoot, settings.Production.RepositoryRoot);
            tool = new ExternalMediaTool(settings.Production);
        }
        catch (Exception refused) when (refused is ProductionSettingRefusedException or ItemMaterialRefusedException or FileNotFoundException)
        {
            await errors.WriteLineAsync($"Refused, nothing was recorded: {refused.Message}").ConfigureAwait(false);
            return 2;
        }

        var identity = await provider.GetRequiredService<IStoreDesignationReader>().ReadAsync(cancellationToken).ConfigureAwait(false);
        if (identity.Designation is null)
        {
            await errors.WriteLineAsync($"Refused, nothing was recorded: the store {identity.DatabaseName} records no designation").ConfigureAwait(false);
            return 2;
        }

        var registrar = new RecordingRegistrar(
            provider.GetRequiredService<IUnitOfWork>(), provider.GetRequiredService<IProductionReader>(), tool, store, settings.Production, output);
        var result = await registrar.RegisterAsync(new RegistrationRequest
        {
            Item = preparation.Item,
            Loaded = loaded,
            RecordingsFolder = folder,
            PerformerName = Option(args, "--performer"),
            ReleaseDocumentReference = Option(args, "--release-reference"),
            ReleaseDocumentDate = date,
            ReleaseDocumentPath = Option(args, "--release-document"),
            TrainingTerm = Option(args, "--training-term"),
        }, cancellationToken).ConfigureAwait(false);
        return result.ExitCode;
    }

    /// <summary>
    /// verify-model: verifies the in-house model's installation from the settings, READING FILES ONLY and starting no process
    /// (the own-voice change, decision D-011 of its design). Exits 0 verified, printing every file with both hashes and the
    /// runtime's metadata, or 2 refused naming every finding. It reads no datastore.
    /// </summary>
    public static async Task<int> VerifyModelAsync(string[] args, TextWriter output, TextWriter errors, CancellationToken cancellationToken)
    {
        var settingsPath = Option(args, "--settings");
        if (settingsPath is null)
        {
            await errors.WriteLineAsync("Usage: verify-model --settings <settings.json>").ConfigureAwait(false);
            return 2;
        }

        InHouseModelSettings model;
        string repository;
        try
        {
            var settings = ReadSettings(settingsPath);
            model = settings.Production.InHouseModel
                ?? throw new ProductionSettingRefusedException("inHouseModel", "no in-house model section is configured in the settings file");
            repository = settings.Production.RepositoryRoot;
        }
        catch (Exception refused) when (refused is ProductionSettingRefusedException or FileNotFoundException)
        {
            await errors.WriteLineAsync($"Refused: {refused.Message}").ConfigureAwait(false);
            return 2;
        }

        var report = await new InstallationVerifier(model, repository).VerifyAsync(entries: true, cancellationToken).ConfigureAwait(false);
        foreach (var file in report.Files)
        {
            await output.WriteLineAsync($"  {file.Describe()}").ConfigureAwait(false);
        }

        await output.WriteLineAsync($"  runtime: {report.RuntimeName ?? "not read"} {report.RuntimeVersion ?? "not read"}, licence {report.RuntimeLicence ?? "not read"} (read from its package metadata); "
            + $"voice {report.VoiceName ?? "not read"} at {report.VoiceSampleRate?.ToString(CultureInfo.InvariantCulture) ?? "no stated"} Hz; dataset licence {report.DatasetLicence ?? "not read"}; "
            + $"weights licence {model.WeightsLicence?.Describe() ?? "not stated in the installed files"}; voice licence {model.VoiceLicence?.Describe() ?? "not stated in the installed files"}").ConfigureAwait(false);
        await output.WriteLineAsync(string.Create(CultureInfo.InvariantCulture,
            $"  record entries verified: {report.RecordEntriesVerified}; listed without a hash by their packages: {report.RecordEntriesWithoutHash}; no process was started")).ConfigureAwait(false);
        if (!report.Verified)
        {
            foreach (var finding in report.Findings)
            {
                await errors.WriteLineAsync($"Refused: {finding}").ConfigureAwait(false);
            }

            return 2;
        }

        await output.WriteLineAsync("Verified: every configured file of the in-house model hashes to its expected value, outside the repository.").ConfigureAwait(false);
        return 0;
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
            ControllerRestriction = controller is not null && CostController.RestrictsToZeroCost(controller.Action)
                ? $"{controller.Action} for {controller.BookingMonth:yyyy-MM}{(controller.Reason is { } reason ? $" ({reason})" : string.Empty)}"
                : null,
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
            "own" => ProductionMode.Own,
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

        public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken) => throw Refused();

        public Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken) => throw Refused();

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
