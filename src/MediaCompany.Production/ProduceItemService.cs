using System.Globalization;
using System.Text;
using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Dossier;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Work;

namespace MediaCompany.Production;

/// <summary>What one produce run is asked to do.</summary>
public sealed record ProduceRequest
{
    public required ItemId Item { get; init; }
    public required ProductionMode Mode { get; init; }
    public required LoadedItem Loaded { get; init; }
    public required DepartmentId Department { get; init; }
    public required AgentId Agent { get; init; }

    /// <summary>The store's designation, as the host's guard admitted it.</summary>
    public required StoreDesignation Designation { get; init; }

    public RunId Run { get; init; } = RunId.New();
}

/// <summary>
/// How a produce run ended: the exit code, and what it produced. THE EXIT CODES ARE DOCUMENTED (correction CR-005):
/// 0 succeeded (the file rendered, the item held short of publish-ready); 2 refused before any call; 3 a stage failed
/// or was held, its outcome recorded naming why, including any failure nobody anticipated and a narration source that
/// changed between the plan and a part; 4 stopped at the item cap; 5 an incurred attempt could not be recorded; 6 cancelled
/// by the operator, the interrupted stage recorded failed and any vendor call in flight booked at its worst case. After the
/// item version opens, every ending is one of these, and every ending but 5 records the stage it ended in.
/// </summary>
public sealed record ProduceResult(int ExitCode, ItemVersion? Version, StoredFile? Rendered, IReadOnlyList<OperationRecord> Operations)
{
    public const int Succeeded = 0;
    public const int RefusedBeforeAnyCall = 2;
    public const int StageFailed = 3;
    public const int CapStopped = 4;
    public const int AttemptNotRecorded = 5;
    public const int Cancelled = 6;
}

/// <summary>
/// The produce service (the production change, decisions D-010, D-012 and D-015 of its design): one command turns a
/// recorded item package into one rendered video file, recording every stage outcome, every operation and every
/// artifact on the datastore's clock, regenerating no script, research or claim, and publishing nothing.
///
/// THE NARRATION SOURCE (the own-voice change, decisions D-001 to D-003 of its design). One source narrates a whole
/// production, chosen by <see cref="NarrationSourceRule"/> and printed per part with its reason before anything runs. The
/// own mode narrates one part per beat from a registered recording or the verified in-house model, books no operation and
/// needs no cap and no capability boundary; fake mode narrates through the demonstration composition as delivered; the
/// vendor is never chosen by the rule, and its path below the rule is reached only through the internal entry the vendor
/// path's own tests use. Before every own-source part the source is re-derived, and a run whose source would differ from
/// the planned one records the Audio stage failed naming both and ends with exit 3 before producing that part.
///
/// THE ORDER. A production opens the next item version, records the eight carried stages citing the package's
/// version, then runs the four producing stages in EXECUTION order — Design, Audio, Thumbnail, Production — because
/// the assembly consumes the narration; readers list them in the closed stage order. A stage that cannot finish is
/// recorded failed or held, naming why, and the run makes no further call.
///
/// THE HOLDS. No transaction and no record is held open while the external tool or the model runs, while a file is
/// hashed or while a vendor is called from here: every record is one short transaction of its own, after the work it
/// records, and the capability boundary keeps its own admission transaction to the call it makes. Every bound is the
/// configured one.
/// </summary>
public sealed class ProduceItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionReader _reader;
    private readonly IMediaTool _tool;
    private readonly IArtifactStore _store;
    private readonly ProductionSettings _settings;
    private readonly TextWriter _output;
    private readonly IVoiceModel? _voice;

    public ProduceItemService(
        IUnitOfWork unitOfWork,
        IProductionReader reader,
        IMediaTool tool,
        IArtifactStore store,
        ProductionSettings settings,
        TextWriter output,
        IVoiceModel? voice = null)
    {
        _unitOfWork = unitOfWork;
        _reader = reader;
        _tool = tool;
        _store = store;
        _settings = settings;
        _output = output;
        _voice = voice;
    }

    /// <summary>
    /// Reads the package, selects the narration source by the rule and composes the plan, refusing where the narration
    /// cannot be split as designed. Plan-only previews the own mode: it reads files only and starts no process.
    /// </summary>
    public async Task<ProductionPlan?> PlanAsync(ProduceRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var material = request.Loaded.Material;
        var package = await _reader.PackageAsync(request.Item, new ItemVersion(material.PackageVersion), cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            _output.WriteLine($"Refused before any call: item {request.Item} is not recorded in this store; run the preparation first.");
            return null;
        }

        switch (request.Mode)
        {
            case ProductionMode.Fake:
            {
                var selection = NarrationSourceRule.Select(request.Mode, request.Designation, RegistrationReading.None, ModelReading.NotConfigured);
                return await BoundaryPlanAsync(request, package, selection, cancellationToken).ConfigureAwait(false);
            }

            case ProductionMode.Metered:
            {
                // The rule refuses: no plan with a vendor estimate is composed above it.
                var selection = NarrationSourceRule.Select(request.Mode, request.Designation, RegistrationReading.None, ModelReading.NotConfigured);
                return ProductionPlan.ComposeOwn(package, NarrationSplitter.SplitByBeat(request.Loaded.Narration, material.Beats), selection, null, null, request.Loaded);
            }

            default:
            {
                var (selection, recording, installation) = await SelectOwnAsync(request, cancellationToken).ConfigureAwait(false);
                return ProductionPlan.ComposeOwn(package, NarrationSplitter.SplitByBeat(request.Loaded.Narration, material.Beats), selection, recording, installation, request.Loaded);
            }
        }
    }

    /// <summary>
    /// The plan of a boundary source (the fake, or the vendor below the rule): the narration split under the configured
    /// character maximum, estimated at the prices in force, as delivered.
    /// </summary>
    private async Task<ProductionPlan> BoundaryPlanAsync(ProduceRequest request, RecordedPackage package, NarrationSelection selection, CancellationToken cancellationToken)
    {
        var maximum = _settings.NarrationCharacterMaximum
            ?? throw new NarrationSplitRefusedException("no narration character maximum is configured (setting narrationCharacterMaximum); a boundary source needs it");
        var parts = NarrationSplitter.Split(request.Loaded.Narration, request.Loaded.Material.Beats, maximum);
        var pricing = await _reader.PricingAsync(CapabilityClass.Narration, cancellationToken).ConfigureAwait(false);
        return ProductionPlan.Compose(package, parts, pricing) with { Selection = selection };
    }

    /// <summary>
    /// The own sources' readings and the rule's selection over them: the latest registration with every stored copy
    /// re-hashed, and, only where no released registration decides it, the model's whole verification from the files.
    /// </summary>
    private async Task<(NarrationSelection Selection, RecordingRegistration? Recording, InstallationReport? Installation)> SelectOwnAsync(
        ProduceRequest request, CancellationToken cancellationToken)
    {
        var material = request.Loaded.Material;
        var latest = await _reader.LatestRegistrationAsync(request.Item, new ItemVersion(material.PackageVersion), cancellationToken).ConfigureAwait(false);
        var registration = await RecordingRegistrar.ReadAsync(latest, _settings.OutputRoot, cancellationToken).ConfigureAwait(false);

        InstallationReport? installation = null;
        var model = ModelReading.NotConfigured;
        if (registration.State is not (RegistrationState.Verified or RegistrationState.FilesChanged) && _settings.InHouseModel is { } configured)
        {
            installation = await new InstallationVerifier(configured, _settings.RepositoryRoot).VerifyAsync(entries: true, cancellationToken).ConfigureAwait(false);
            model = installation.Verified ? new ModelReading(ModelState.Verified, []) : new ModelReading(ModelState.Refused, installation.Findings);
        }

        var selection = NarrationSourceRule.Select(request.Mode, request.Designation, registration, model);
        return (selection, latest, installation);
    }

    /// <summary>
    /// Runs one production. In plan-only mode the plan is printed and nothing is started or written. A refused selection
    /// ends before anything is recorded, naming every reason. The own mode composes no capability boundary; fake mode is
    /// given the demonstration composition by the host.
    /// </summary>
    public async Task<ProduceResult> RunAsync(ProduceRequest request, ICapabilityGateway? gateway, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        ProductionPlan? plan;
        try
        {
            plan = await PlanAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (NarrationSplitRefusedException refused)
        {
            _output.WriteLine($"Refused before any call: {refused.Message}");
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        if (plan is null)
        {
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        plan.Print(_output, request.Mode, request.Designation, _settings, request.Loaded.Material);
        if (plan.Selection is not { Chosen: true } selection)
        {
            foreach (var refusal in plan.Selection?.Refusals ?? [])
            {
                _output.WriteLine($"Refused before any call: {refusal}");
            }

            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        if (request.Mode == ProductionMode.PlanOnly)
        {
            _output.WriteLine("Plan only: no capability call was made, no process was started and no file was written.");
            return new ProduceResult(ProduceResult.Succeeded, null, null, []);
        }

        return await StartAsync(request, plan, selection.Source!.Value, gateway, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// THE VENDOR PATH UNDER TEST, BELOW THE RULE (the own-voice change, decision D-002 of its design, as the Planning Gate
    /// ruled): runs a production with the source named explicitly, bypassing the selection rule, so the vendor path's own
    /// tests can keep every assertion on its requests and bookings. Internal: no command and no composition reaches it.
    /// </summary>
    internal async Task<ProduceResult> RunBelowTheRuleAsync(ProduceRequest request, NarrationSource source, ICapabilityGateway? gateway, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (source is not (NarrationSource.Vendor or NarrationSource.Fake))
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, "Only a boundary source runs below the rule.");
        }

        var package = await _reader.PackageAsync(request.Item, new ItemVersion(request.Loaded.Material.PackageVersion), cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        ProductionPlan plan;
        try
        {
            plan = await BoundaryPlanAsync(request, package,
                new NarrationSelection(source, "named explicitly below the narration source rule: the vendor path under test", [], []), cancellationToken).ConfigureAwait(false);
        }
        catch (NarrationSplitRefusedException refused)
        {
            _output.WriteLine($"Refused before any call: {refused.Message}");
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        plan.Print(_output, request.Mode, request.Designation, _settings, request.Loaded.Material);
        return await StartAsync(request, plan, source, gateway, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ProduceResult> StartAsync(ProduceRequest request, ProductionPlan plan, NarrationSource source, ICapabilityGateway? gateway, CancellationToken cancellationToken)
    {
        if (source is NarrationSource.Fake or NarrationSource.Vendor)
        {
            if (plan.Package.Cap is null)
            {
                _output.WriteLine("Refused before any call: the item has no recorded cap.");
                return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
            }

            if (gateway is null)
            {
                throw new InvalidOperationException("A boundary source needs the capability boundary its composition built.");
            }

            if (string.IsNullOrWhiteSpace(_settings.NarrationVoice))
            {
                _output.WriteLine("Refused before any call: no narration voice is configured (setting narrationVoice).");
                return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
            }
        }
        else if (source == NarrationSource.InHouseModel && (_voice is null || _settings.InHouseModel is null))
        {
            _output.WriteLine("Refused before any call: the in-house model is selected and no model process is composed for this run.");
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        var run = await ProductionRun.OpenAsync(this, request, plan, source, gateway, cancellationToken).ConfigureAwait(false);
        return await run.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>One produced narration part: the part, its stored file, its decoded measurement and its provenance where own-sourced.</summary>
    private sealed record ProducedPart(NarrationPart Part, StoredFile File, AudioMeasurement Measured);

    /// <summary>One production version in progress, and every record it writes.</summary>
    private sealed class ProductionRun
    {
        private readonly ProduceItemService _service;
        private readonly ProduceRequest _request;
        private readonly ProductionPlan _plan;
        private readonly NarrationSource _source;
        private readonly ICapabilityGateway? _gateway;
        private readonly List<OperationRecord> _operations = [];
        private readonly JobId _job = JobId.New();

        private ProductionRun(ProduceItemService service, ProduceRequest request, ProductionPlan plan, NarrationSource source, ICapabilityGateway? gateway, ItemVersion version, string folder)
        {
            _service = service;
            _request = request;
            _plan = plan;
            _source = source;
            _gateway = gateway;
            Version = version;
            Folder = folder;
        }

        private ItemVersion Version { get; }

        /// <summary>The run's staging folder, begun inside the run so a failure to begin it is a recorded outcome too.</summary>
        private string Staging { get; set; } = string.Empty;

        /// <summary>The producing stage in progress, which an unanticipated failure or a cancellation is recorded against.</summary>
        private ProductionStage _stage = ProductionStage.Design;

        private string Folder { get; }

        private ItemMaterial Material => _request.Loaded.Material;

        private bool OwnSource => _source is NarrationSource.Recording or NarrationSource.InHouseModel;

        /// <summary>A path inside this production's own folder under the output root.</summary>
        private string Rel(string inRun) => $"{Folder}/{inRun}";

        private TextWriter Output => _service._output;

        public static async Task<ProductionRun> OpenAsync(
            ProduceItemService service, ProduceRequest request, ProductionPlan plan, NarrationSource source, ICapabilityGateway? gateway, CancellationToken cancellationToken)
        {
            ProductionVersionOpened opened;
            await using (var transaction = await service._unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false))
            {
                opened = await transaction.Production.OpenProductionVersionAsync(request.Item, request.Mode, source, cancellationToken).ConfigureAwait(false);
                await transaction.Audit.AppendAsync(Entry(request.Item, opened.Version, "production.version-opened",
                    $"production version {opened.Version} opened in {request.Mode} mode, narrated from the {source} source", "production version record"), cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            var folder = $"{request.Loaded.Material.Item}/v{opened.Version}";
            service._output.WriteLine($"Opened item version {opened.Version} at {opened.OpenedAt:O} (the datastore's instant), narration source {source}; output folder {service._store.FullPath(folder)}");
            return new ProductionRun(service, request, plan, source, gateway, opened.Version, folder);
        }

        /// <summary>
        /// Runs the opened version to one of the documented endings (correction CR-005): from here on no exception leaves
        /// the run unrecorded. A cancellation by the operator records the interrupted stage failed and ends with exit code
        /// 6; any failure nobody anticipated records the stage in progress failed, naming the failure's type and message,
        /// and ends with exit code 3. Those records are written on a token that cannot be cancelled, because they record
        /// what already happened.
        /// </summary>
        public async Task<ProduceResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            try
            {
                Staging = _service._store.BeginRun(Folder);
                var version = await _service._tool.VersionLineAsync(cancellationToken).ConfigureAwait(false);
                Output.WriteLine($"Media tool: {version}");
                await _service._store.CopyIntoStagingAsync(Staging, _service._settings.FontFile, ExternalMediaTool.StagedFontName, cancellationToken).ConfigureAwait(false);

                await CarryAsync(cancellationToken).ConfigureAwait(false);

                var graphics = await DesignAsync(version, cancellationToken).ConfigureAwait(false);

                var audio = await AudioAsync(cancellationToken).ConfigureAwait(false);
                if (audio.Ended is { } ended)
                {
                    return ended;
                }

                await ThumbnailAsync(cancellationToken).ConfigureAwait(false);

                var rendered = await AssembleAsync(graphics, audio.BeatDurations!, audio.Narration!, cancellationToken).ConfigureAwait(false);
                if (rendered is null)
                {
                    return new ProduceResult(ProduceResult.StageFailed, Version, null, _operations);
                }

                Summarise(rendered);
                return new ProduceResult(ProduceResult.Succeeded, Version, rendered, _operations);
            }
            catch (StageEndedException stopped)
            {
                return new ProduceResult(stopped.ExitCode, Version, null, _operations);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                await TryEndStageAsync(_stage, StageOutcome.Failed,
                    $"cancelled by the operator during the {_stage} stage; no further call is made and nothing after it ran", CancellationToken.None).ConfigureAwait(false);
                return new ProduceResult(ProduceResult.Cancelled, Version, null, _operations);
            }
            catch (Exception unanticipated)
            {
                await TryEndStageAsync(_stage, StageOutcome.Failed,
                    $"the {_stage} stage ended on a failure nobody anticipated ({unanticipated.GetType().Name}: {unanticipated.Message}); no further call is made",
                    CancellationToken.None).ConfigureAwait(false);
                return new ProduceResult(ProduceResult.StageFailed, Version, null, _operations);
            }
        }

        // -------------------------------------------------------------------
        // The eight carried stages
        // -------------------------------------------------------------------

        private async Task CarryAsync(CancellationToken cancellationToken)
        {
            var produced = new[] { ProductionStage.Design, ProductionStage.Audio, ProductionStage.Thumbnail, ProductionStage.Production };
            foreach (var stage in ProductionStageSet.All.Where(s => !produced.Contains(s)))
            {
                var recorded = _plan.Package.PackageStages.FirstOrDefault(s => s.Stage == stage);
                if (recorded is null)
                {
                    continue;
                }

                await RecordStageAsync(stage, recorded.Outcome,
                    $"carried from version {_plan.Package.PackageVersion} as recorded, not regenerated: {recorded.Summary}",
                    $"dossier:{_request.Item}:{_plan.Package.PackageVersion}", cancellationToken).ConfigureAwait(false);
            }
        }

        // -------------------------------------------------------------------
        // Design: one still per specified graphic, no capability call
        // -------------------------------------------------------------------

        private async Task<IReadOnlyDictionary<string, string>> DesignAsync(string toolVersion, CancellationToken cancellationToken)
        {
            _stage = ProductionStage.Design;
            var stills = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                foreach (var graphic in Material.Graphics)
                {
                    var name = graphic.Id.ToLowerInvariant();
                    var text = Wrap([graphic.Id, string.Empty, .. graphic.Lines]);
                    var stored = await DrawAsync(name, text, StillStyle.Graphic, $"design/{name}.png", cancellationToken).ConfigureAwait(false);
                    await RecordArtifactAsync(ProductionStage.Design, ArtifactRole.GraphicStill, graphic.Id, stored, null, cancellationToken).ConfigureAwait(false);
                    stills[graphic.Id] = $"design/{name}.png";
                }
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await EndStageAsync(ProductionStage.Design, StageOutcome.Failed, $"a graphic still could not be drawn: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            await RecordStageAsync(ProductionStage.Design, StageOutcome.Succeeded,
                $"{stills.Count} graphic stills drawn from the item material, each displaying only lines quoted from its record; no capability call; {toolVersion}",
                $"artifacts:{Folder}/design", cancellationToken).ConfigureAwait(false);
            return stills;
        }

        // -------------------------------------------------------------------
        // Audio: the whole recorded narration from the one selected source
        // -------------------------------------------------------------------

        private async Task<(ProduceResult? Ended, IReadOnlyList<(int Beat, TimeSpan MeasuredDuration)>? BeatDurations, StoredFile? Narration)> AudioAsync(
            CancellationToken cancellationToken)
        {
            _stage = ProductionStage.Audio;
            var parts = _source switch
            {
                NarrationSource.Recording => await RecordingPartsAsync(cancellationToken).ConfigureAwait(false),
                NarrationSource.InHouseModel => await ModelPartsAsync(cancellationToken).ConfigureAwait(false),
                _ => await BoundaryPartsAsync(cancellationToken).ConfigureAwait(false),
            };
            if (parts.Ended is { } ended)
            {
                return (ended, null, null);
            }

            var produced = parts.Parts!;

            // The join is exact only over one rate, one channel count and one sample format; nothing is converted.
            var mixed = RecordingSetCheck.MixedFormats(produced.Select(p => (p.Part.Beat, p.Measured)).ToArray());
            if (mixed.Count > 0)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed, string.Join("; ", mixed), ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }

            var list = new StringBuilder();
            foreach (var part in produced)
            {
                list.Append(CultureInfo.InvariantCulture, $"file '../audio/{Path.GetFileName(part.File.RelativePath)}'\n");
            }

            StoredFile narration;
            AudioMeasurement joined;
            try
            {
                await _service._store.WriteStagingTextAsync(Staging, "narration-parts.txt", list.ToString(), cancellationToken).ConfigureAwait(false);
                await _service._tool.ConcatenateAudioAsync(Staging, "narration-parts.txt", "narration.wav", produced[0].Measured.SampleFormat, cancellationToken).ConfigureAwait(false);
                narration = await _service._store.PromoteAsync(Staging, "narration.wav", Rel("audio/narration.wav"), cancellationToken).ConfigureAwait(false);
                joined = await _service._tool.MeasureAudioAsync(narration.FullPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed, $"the narration parts could not be joined: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            // The joined narration holds exactly its parts' decoded samples, to within one sample.
            var sum = produced.Sum(p => p.Measured.DecodedSamples);
            if (Math.Abs(joined.DecodedSamples - sum) > 1)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                    string.Create(CultureInfo.InvariantCulture, $"the joined narration decodes to {joined.DecodedSamples:N0} samples and its parts to {sum:N0}; the join is not exact"),
                    ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }

            // Every part artifact is re-hashed after the join read it: the bytes joined are the bytes recorded.
            foreach (var part in produced)
            {
                var now = await FileHashes.Sha256Async(part.File.FullPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(now, part.File.Sha256, StringComparison.Ordinal))
                {
                    await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                        $"part {part.Part.Ordinal}'s file {part.File.RelativePath} changed while it was joined: sha256 {part.File.Sha256} recorded and {now} after the join",
                        ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                }
            }

            var rate = _request.Loaded.Rate;
            var words = produced.Sum(p => DurationExpectation.Words(p.Part.Text));
            var expected = DurationExpectation.ExpectedSeconds(words, rate.WordsPerMinute);
            await RecordArtifactAsync(ProductionStage.Audio, ArtifactRole.Narration, "the whole recorded narration", narration, joined.Duration, cancellationToken,
                Measured(narration, joined, null, words, expected)).ConfigureAwait(false);

            var report = DurationExpectation.Report("the whole narration", joined.DurationSeconds, words, expected, rate)
                + (rate.DurationQuote is { } quoted ? $" (the script's own figure: {quoted})" : string.Empty);
            Output.WriteLine($"  {report}; {joined.DescribeLoudness()}");

            var beats = produced.GroupBy(p => p.Part.Beat)
                .Select(g => (g.Key, TimeSpan.FromTicks(g.Sum(p => p.Measured.Duration.Ticks))))
                .ToArray();
            var characters = _plan.Operations.Sum(o => o.Part.Text.Length);

            var spend = OwnSource
                ? $"no operation is booked and no metered spend is planned or made, USD 0.00; {Unmeasured()}"
                : "each request booked through the capability boundary";
            await RecordStageAsync(ProductionStage.Audio, StageOutcome.Succeeded,
                string.Create(CultureInfo.InvariantCulture,
                    $"narration of the whole recorded text from the {_source} source, {characters:N0} characters in {produced.Count} part(s), joined with no inserted silence "
                    + $"and no level change; duration {joined.DurationSeconds:0.000} s MEASURED from decoded audio ({AudioMeasurement.DecodedBasis}); {joined.DescribeLoudness()}; ")
                + $"{report}; {spend}",
                $"artifact:{narration.RelativePath} sha256:{narration.Sha256}", cancellationToken).ConfigureAwait(false);
            return (null, beats, narration);
        }

        /// <summary>What local work an own source leaves unmeasured; never zero.</summary>
        private string Unmeasured() => _source == NarrationSource.Recording
            ? "recording time unmeasured; local compute unmeasured"
            : "local compute unmeasured";

        /// <summary>The fake's or the vendor's parts through the boundary, as delivered, each measured from decoded audio.</summary>
        private async Task<(ProduceResult? Ended, IReadOnlyList<ProducedPart>? Parts)> BoundaryPartsAsync(CancellationToken cancellationToken)
        {
            var produced = new List<ProducedPart>();
            foreach (var planned in _plan.Operations)
            {
                var part = planned.Part;
                var outcome = await CallAsync(part, cancellationToken).ConfigureAwait(false);
                if (outcome.Ended is { } ended)
                {
                    return (ended, null);
                }

                var stored = await _service._store.StoreAsync(Staging, outcome.Content!.Bytes, Rel($"audio/part-{part.Ordinal:000}.wav"), cancellationToken).ConfigureAwait(false);
                var measured = await MeasureAsync(stored, part, cancellationToken).ConfigureAwait(false);
                await RecordArtifactAsync(ProductionStage.Audio, ArtifactRole.NarrationPart, $"part {part.Ordinal} of beat {part.Beat}", stored, measured.Duration, cancellationToken,
                    Measured(stored, measured, null, DurationExpectation.Words(part.Text), null)).ConfigureAwait(false);
                produced.Add(new ProducedPart(part, stored, measured));
            }

            return (null, produced);
        }

        /// <summary>
        /// The registered recording's parts: before each, the source is re-derived; then the registered stored copy is copied
        /// by the one writer into this run and promoted, and the promoted part must carry the REGISTERED hash, or the stage
        /// fails naming both. The part is measured from its own decoded audio.
        /// </summary>
        private async Task<(ProduceResult? Ended, IReadOnlyList<ProducedPart>? Parts)> RecordingPartsAsync(CancellationToken cancellationToken)
        {
            var registration = _plan.Recording ?? throw new InvalidOperationException("A recording-sourced plan names its registration.");
            var produced = new List<ProducedPart>();
            foreach (var planned in _plan.Operations)
            {
                var part = planned.Part;
                await RederiveAsync(part, cancellationToken).ConfigureAwait(false);

                var beat = registration.Beats.Single(b => b.Beat == part.Beat);
                var source = ArtifactWriter.StoredPath(_service._settings.OutputRoot, beat.StoredPath);
                var extension = Path.GetExtension(beat.StoredPath);
                await _service._store.CopyIntoStagingAsync(Staging, source, $"part-{part.Ordinal:000}{extension}", cancellationToken).ConfigureAwait(false);
                var stored = await _service._store.PromoteAsync(Staging, $"part-{part.Ordinal:000}{extension}", Rel($"audio/part-{part.Ordinal:000}{extension}"), cancellationToken).ConfigureAwait(false);
                if (!string.Equals(stored.Sha256, beat.Sha256, StringComparison.Ordinal))
                {
                    await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                        $"part {part.Ordinal}'s copy of beat {beat.Beat:00}'s registered file is not the registered bytes: sha256 {beat.Sha256} registered and {stored.Sha256} copied into this run; nothing narrates from it",
                        ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                }

                var measured = await MeasureAsync(stored, part, cancellationToken).ConfigureAwait(false);
                var words = DurationExpectation.Words(part.Text);
                var expected = DurationExpectation.ExpectedSeconds(words, _request.Loaded.Rate.WordsPerMinute);
                await RecordArtifactAsync(ProductionStage.Audio, ArtifactRole.NarrationPart, $"part {part.Ordinal} of beat {part.Beat}", stored, measured.Duration, cancellationToken,
                    Measured(stored, measured, part.Beat, words, expected),
                    new NarrationProvenance
                    {
                        Item = _request.Item,
                        Version = Version,
                        RelativePath = stored.RelativePath,
                        Source = NarrationSource.Recording,
                        Registration = registration.Id,
                        Beat = beat.Beat,
                        PerformerName = registration.Release.PerformerName,
                        Release = registration.Release.Describe(),
                    }).ConfigureAwait(false);
                Output.WriteLine($"  {DurationExpectation.Report($"beat {part.Beat:00}", measured.DurationSeconds, words, expected, _request.Loaded.Rate)}; {measured.DescribeLoudness()}");
                produced.Add(new ProducedPart(part, stored, measured));
            }

            return (null, produced);
        }

        /// <summary>
        /// The in-house model's parts. The whole installation, every record entry included, is verified before the first part
        /// and after the last; before each part the source is re-derived, the core files are HELD read-only and hashed through
        /// their handles, the part's text is staged by the one writer, and the one starter runs the model under its bound with
        /// no transaction open; the handles are released only after the process exited. The first part is generated twice and
        /// the bytes compared, so repeatability is recorded only as observed.
        /// </summary>
        private async Task<(ProduceResult? Ended, IReadOnlyList<ProducedPart>? Parts)> ModelPartsAsync(CancellationToken cancellationToken)
        {
            var settings = _service._settings.InHouseModel!;
            var verifier = new InstallationVerifier(settings, _service._settings.RepositoryRoot);
            var before = await verifier.VerifyAsync(entries: true, cancellationToken).ConfigureAwait(false);
            if (!before.Verified)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                    $"the narration source changed between the plan and part 1: the plan named {NarrationSource.InHouseModel}, and the installation no longer verifies: {string.Join("; ", before.Findings)}",
                    ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }

            var produced = new List<ProducedPart>();
            foreach (var planned in _plan.Operations)
            {
                var part = planned.Part;
                var text = $"part-{part.Ordinal:000}.txt";
                var output = $"part-{part.Ordinal:000}.wav";
                await _service._store.WriteStagingTextAsync(Staging, text, part.Text, cancellationToken).ConfigureAwait(false);

                // The core files are held from their hash until the process exits; the hold is also the model's reading
                // for the re-derivation, so the files hashed for the rule are the files the process loads.
                IReadOnlyList<string> arguments;
                string repeatability;
                HeldInstallation? held = null;
                ModelReading reading;
                try
                {
                    held = await verifier.HoldAsync(cancellationToken).ConfigureAwait(false);
                    reading = new ModelReading(ModelState.Verified, []);
                }
                catch (InstallationRefusedException refused)
                {
                    reading = new ModelReading(ModelState.Refused, refused.Findings);
                }

                string hashes;
                await using (held)
                {
                    await RederiveAsync(part, cancellationToken, reading).ConfigureAwait(false);
                    hashes = held!.Describe();
                    try
                    {
                        arguments = await Generate(settings, text, output, cancellationToken).ConfigureAwait(false);
                        if (part.Ordinal == 1)
                        {
                            var first = await FileHashes.Sha256Async(Path.Combine(Staging, output), cancellationToken).ConfigureAwait(false);
                            await Generate(settings, text, "part-001-repeat.wav", cancellationToken).ConfigureAwait(false);
                            var second = await FileHashes.Sha256Async(Path.Combine(Staging, "part-001-repeat.wav"), cancellationToken).ConfigureAwait(false);
                            repeatability = string.Equals(first, second, StringComparison.Ordinal)
                                ? $"repeats: part 1 generated twice with identical settings gave identical bytes (sha256 {first})"
                                : $"not observed to repeat: part 1 generated twice with identical settings gave sha256 {first} and {second}";
                        }
                        else
                        {
                            repeatability = "not observed to repeat: generated once";
                        }
                    }
                    catch (MediaToolBoundExceededException bound)
                    {
                        await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                            $"part {part.Ordinal} (beat {part.Beat}) passed the configured model part bound of {settings.PartBound.TotalSeconds:0} s on the monotonic clock; "
                            + $"its process tree was terminated and nothing of it is recorded ({bound.Message})",
                            ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                        throw new UnreachableException();
                    }
                    catch (MediaToolFailedException failed)
                    {
                        await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                            $"part {part.Ordinal} (beat {part.Beat}) could not be generated: {failed.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                        throw new UnreachableException();
                    }
                }

                var stored = await _service._store.PromoteAsync(Staging, output, Rel($"audio/part-{part.Ordinal:000}.wav"), cancellationToken).ConfigureAwait(false);
                var measured = await MeasureAsync(stored, part, cancellationToken).ConfigureAwait(false);
                var words = DurationExpectation.Words(part.Text);
                var expected = DurationExpectation.ExpectedSeconds(words, _request.Loaded.Rate.WordsPerMinute);
                await RecordArtifactAsync(ProductionStage.Audio, ArtifactRole.NarrationPart, $"part {part.Ordinal} of beat {part.Beat}", stored, measured.Duration, cancellationToken,
                    Measured(stored, measured, part.Beat, words, expected),
                    Provenance(stored, settings, before, hashes, arguments, repeatability)).ConfigureAwait(false);
                Output.WriteLine($"  {DurationExpectation.Report($"beat {part.Beat:00}", measured.DurationSeconds, words, expected, _request.Loaded.Rate)}; {measured.DescribeLoudness()}; {repeatability}");
                produced.Add(new ProducedPart(part, stored, measured));
            }

            var after = await verifier.VerifyAsync(entries: true, cancellationToken).ConfigureAwait(false);
            if (!after.Verified)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                    $"the installation changed while the parts were generated: {string.Join("; ", after.Findings)}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }

            return (null, produced);
        }

        private Task<IReadOnlyList<string>> Generate(InHouseModelSettings settings, string text, string output, CancellationToken cancellationToken) =>
            _service._voice!.GenerateAsync(
                new VoiceGeneration(Staging, text, output, Path.GetFullPath(settings.Model.Path), Path.GetFullPath(settings.Configuration.Path), settings.Generation, settings.PartBound),
                cancellationToken);

        /// <summary>The model part's provenance: every value from the verified files and this run; licences from configuration say so.</summary>
        private NarrationProvenance Provenance(
            StoredFile stored, InHouseModelSettings settings, InstallationReport report, string hashes, IReadOnlyList<string> arguments, string repeatability)
        {
            const string NotStated = "not stated in the installed files";
            var defaults = report.VoiceDefaults;
            string Beside(decimal passed, string key) =>
                defaults.TryGetValue(key, out var value) ? string.Create(CultureInfo.InvariantCulture, $"{passed} (the voice configuration states {value})") : string.Create(CultureInfo.InvariantCulture, $"{passed} (the voice configuration states none)");
            var g = settings.Generation;
            return new NarrationProvenance
            {
                Item = _request.Item,
                Version = Version,
                RelativePath = stored.RelativePath,
                Source = NarrationSource.InHouseModel,
                ModelName = report.RuntimeName ?? "not read",
                ModelVersion = report.RuntimeVersion ?? "not read",
                CodeLicence = (report.RuntimeLicence ?? "not stated") + " (read from the runtime's installed package metadata)",
                VoiceName = (report.VoiceName ?? "not read") + " (composed from the verified voice configuration's language code, dataset and quality)",
                DatasetLicence = report.DatasetLicence ?? "not read",
                WeightsLicence = settings.WeightsLicence?.Describe() ?? NotStated,
                VoiceLicence = settings.VoiceLicence?.Describe() ?? NotStated,
                GenerationSettings = string.Create(CultureInfo.InvariantCulture,
                    $"length scale {Beside(g.LengthScale, "length_scale")}; noise scale {Beside(g.NoiseScale, "noise_scale")}; noise width scale {Beside(g.NoiseWidthScale, "noise_w")}; ")
                    + string.Create(CultureInfo.InvariantCulture, $"sentence silence {g.SentenceSilenceSeconds} s; volume {g.Volume}; output normalisation {(g.Normalise ? "on" : "off")} (all passed explicitly)"),
                FileHashes = hashes,
                RecordEntriesVerified = report.RecordEntriesVerified,
                Arguments = string.Join(" ", arguments.Select(a => a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a)),
                Repeatability = repeatability,
            };
        }

        /// <summary>
        /// Re-derives the narration source before a part, from the store and the files as they are now: the latest
        /// registration with every stored copy re-hashed and, where it decides, the model's core files. A source or a
        /// registration that differs from the plan's records the Audio stage failed naming both, before the part is produced.
        /// </summary>
        private async Task RederiveAsync(NarrationPart part, CancellationToken cancellationToken, ModelReading? held = null)
        {
            var material = Material;
            var latest = await _service._reader.LatestRegistrationAsync(_request.Item, new ItemVersion(material.PackageVersion), cancellationToken).ConfigureAwait(false);
            var registration = await RecordingRegistrar.ReadAsync(latest, _service._settings.OutputRoot, cancellationToken).ConfigureAwait(false);
            var model = held ?? ModelReading.NotConfigured;
            if (held is null && registration.State is not (RegistrationState.Verified or RegistrationState.FilesChanged) && _service._settings.InHouseModel is { } configured)
            {
                // The core files only: the record entries are verified before the first part and after the last.
                try
                {
                    await using var hold = await new InstallationVerifier(configured, _service._settings.RepositoryRoot).HoldAsync(cancellationToken).ConfigureAwait(false);
                    model = new ModelReading(ModelState.Verified, []);
                }
                catch (InstallationRefusedException refused)
                {
                    model = new ModelReading(ModelState.Refused, refused.Findings);
                }
            }

            var derived = NarrationSourceRule.Select(_request.Mode, _request.Designation, registration, model);
            var sameRegistration = _source != NarrationSource.Recording || registration.Registration == _plan.Recording?.Id;
            if (derived.Source != _source || !sameRegistration)
            {
                var why = derived.Chosen ? derived.Reason : string.Join("; ", derived.Refusals);
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                    $"the narration source changed between the plan and part {part.Ordinal}: the plan named {_source}"
                    + (_plan.Recording is { } planned ? $" (registration {planned.Id})" : string.Empty)
                    + $", and the store and files now derive {(derived.Source?.ToString() ?? "no source")}"
                    + (registration.Registration is { } now ? $" (registration {now})" : string.Empty)
                    + $": {why}; no source is mixed into one production",
                    ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>Measures a stored part from its own decoded audio; a measurement that fails, or reports corrupt input, fails the stage by name.</summary>
        private async Task<AudioMeasurement> MeasureAsync(StoredFile stored, NarrationPart part, CancellationToken cancellationToken)
        {
            AudioMeasurement measured;
            try
            {
                measured = await _service._tool.MeasureAudioAsync(stored.FullPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed, $"part {part.Ordinal}'s file could not be measured from decoded audio: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            if (measured.DecodeFindings.Count > 0)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                    $"part {part.Ordinal}'s file does not decode cleanly: {string.Join("; ", measured.DecodeFindings.Take(3))}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }

            return measured;
        }

        private NarrationMeasurement Measured(StoredFile stored, AudioMeasurement measured, int? beat, int words, decimal? expected) => new()
        {
            Item = _request.Item,
            Version = Version,
            RelativePath = stored.RelativePath,
            Measured = measured,
            Beat = beat,
            Words = words,
            ExpectedSeconds = expected,
            ExpectationBasis = expected is null
                ? (beat is null && !OwnSource ? "no expectation: a boundary part need not be one beat" : _request.Loaded.Rate.Describe())
                : $"words times 60 over {_request.Loaded.Rate.Describe()}, to one decimal; a reported reference, never a target",
        };

        /// <summary>One narration call through the boundary, and the outcome a production run takes from it.</summary>
        private async Task<(ProduceResult? Ended, ProducedContent? Content)> CallAsync(NarrationPart part, CancellationToken cancellationToken)
        {
            var package = _plan.Package;
            var request = new CapabilityRequest(
                CapabilityClass.Narration,
                ReasoningTier.Light,
                new QualityRating(0),
                new ContextCapacity(0),
                package.Cap!.Cap,
                Criticality.Routine,
                new Attribution(_request.Item, package.Channel, _request.Department, _request.Agent),
                new EstimatedUnits(0, 0, 0) { CharacterUnits = part.Text.Length },
                TimeSpan.Zero,
                ReducedFloorPolicy.Forbidden)
            {
                Payload = new CapabilityPayload.Narration(part.Text, _service._settings.NarrationVoice!, "wav"),
            };

            CapabilityOutcome outcome;
            try
            {
                outcome = await _gateway!.ExecuteAsync(
                    request,
                    new CapabilityInvocationContext
                    {
                        Run = _request.Run,
                        Job = _job,
                        Stage = LifecyclePosition.Assembly,
                        Actor = WorkforceRole.Producer,
                        Attempt = 1,
                    },
                    cancellationToken).ConfigureAwait(false);
            }
            catch (IncurredAttemptNotRecordedException unrecorded)
            {
                Output.WriteLine($"An incurred attempt was NOT RECORDED: {unrecorded.Message}");
                Output.WriteLine($"  operation {unrecorded.Operation}, route {unrecorded.Route}, model {unrecorded.Model}, admitted at {unrecorded.AdmittedAt:O}; "
                    + "its worst-case reservation stays durable in the store and keeps counting against the cap");
                await TryEndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                    $"an incurred attempt could not be recorded ({unrecorded.Reason}); operation {unrecorded.Operation} keeps counting at its worst case; production stopped",
                    cancellationToken).ConfigureAwait(false);
                return (new ProduceResult(ProduceResult.AttemptNotRecorded, Version, null, _operations), null);
            }

            switch (outcome)
            {
                case CapabilityOutcome.Completed completed:
                    _operations.Add(completed.Operation);
                    Output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                        $"  operation {completed.Operation.Id}: part {part.Ordinal}, {completed.Operation.Outcome}, booked {completed.Operation.ComputedCost.Amount:0.000000} {completed.Operation.ComputedCost.Currency} "
                        + $"({completed.Operation.CostBasis}; {Label()}), {completed.Operation.Units.CharacterUnits} character units"));
                    if (completed.Recovered)
                    {
                        await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                            $"part {part.Ordinal}'s attempt was booked after its admission transaction was lost (operation {completed.Operation.Id}); production makes no further call",
                            ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    }

                    if (completed.Operation.Outcome != OperationOutcome.Succeeded || completed.Content is null)
                    {
                        // An operator's cancellation during the call (correction CR-003): the boundary booked the attempt at
                        // its worst case; the stage is recorded failed and the run ends with the cancellation's exit code.
                        await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                            $"part {part.Ordinal}'s narration call failed ({completed.Operation.FailureReason}); it is booked as operation {completed.Operation.Id} and is not retried",
                            cancellationToken.IsCancellationRequested ? ProduceResult.Cancelled : ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    }

                    return (null, completed.Content);

                case CapabilityOutcome.Refused { Reason: RefusalReason.ItemCapExceeded } capped:
                    await EndStageAsync(ProductionStage.Audio, StageOutcome.Held,
                        $"stopped at the item cap before part {part.Ordinal}'s call: {capped.Detail}", ProduceResult.CapStopped, cancellationToken).ConfigureAwait(false);
                    break;

                case CapabilityOutcome.Refused refused:
                    await EndStageAsync(ProductionStage.Audio, StageOutcome.Held,
                        $"part {part.Ordinal}'s call was refused before it was made: {refused.Reason}: {refused.Detail}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    break;

                case CapabilityOutcome.Held held:
                    await EndStageAsync(ProductionStage.Audio, StageOutcome.Held,
                        $"part {part.Ordinal}'s call was held ({held.Reason}); a production never waits on a held outcome", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    break;

                default:
                    await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                        $"part {part.Ordinal}'s call ended in an outcome that carries no narration", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    break;
            }

            throw new UnreachableException();
        }

        // -------------------------------------------------------------------
        // Thumbnail: the drawn candidates and the labelled placeholder
        // -------------------------------------------------------------------

        private async Task ThumbnailAsync(CancellationToken cancellationToken)
        {
            _stage = ProductionStage.Thumbnail;
            var hits = new List<string>();
            var placeholders = new List<string>();
            try
            {
                foreach (var thumbnail in Material.Thumbnails)
                {
                    var name = thumbnail.Id.ToLowerInvariant() + (thumbnail.Placeholder ? "-placeholder" : string.Empty);
                    var lines = thumbnail.Placeholder
                        ? (IReadOnlyList<string>)["PLACEHOLDER", thumbnail.Id, "not generated", string.Empty, .. thumbnail.Lines]
                        : thumbnail.DisplayedText is { } shown ? [shown, string.Empty, thumbnail.Id] : [thumbnail.Id, string.Empty, .. thumbnail.Lines];
                    var stored = await DrawAsync(name, Wrap(lines), thumbnail.Placeholder ? StillStyle.Placeholder : StillStyle.Thumbnail,
                        $"thumbnails/{name}.png", cancellationToken).ConfigureAwait(false);
                    await RecordArtifactAsync(ProductionStage.Thumbnail,
                        thumbnail.Placeholder ? ArtifactRole.ThumbnailPlaceholder : ArtifactRole.ThumbnailCandidate, thumbnail.Id, stored, null, cancellationToken).ConfigureAwait(false);

                    // The delivered barred-term screen over the file name and the alternative text, where recorded.
                    var screened = BarredTermScreen.Screen(new ItemMetadata
                    {
                        Title = Path.GetFileName(stored.RelativePath),
                        Description = thumbnail.AlternativeText ?? string.Empty,
                    });
                    hits.AddRange(screened.Select(h => $"{thumbnail.Id}: {h.Term}"));
                    if (thumbnail.Placeholder)
                    {
                        placeholders.Add(thumbnail.Id);
                    }
                }
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await EndStageAsync(ProductionStage.Thumbnail, StageOutcome.Failed, $"a thumbnail still could not be drawn: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            var drawn = Material.Thumbnails.Count - placeholders.Count;
            var alternative = Material.Thumbnails.All(t => t.AlternativeText is null)
                ? "no alternative text is recorded in the package, so it is recorded absent and the file names alone are screened"
                : "the recorded alternative text and the file names are screened";
            var summary = $"{drawn} candidates drawn with no capability call; {string.Join(", ", placeholders)} held as a labelled placeholder, not generated "
                + $"(the owner's decision of 2026-10-09); {alternative}; barred-term hits: {hits.Count}{(hits.Count > 0 ? " (" + string.Join("; ", hits) + ")" : string.Empty)}";
            await RecordStageAsync(ProductionStage.Thumbnail, hits.Count > 0 ? StageOutcome.Failed : StageOutcome.Held, summary,
                $"artifacts:{Folder}/thumbnails", cancellationToken).ConfigureAwait(false);
        }

        // -------------------------------------------------------------------
        // Production: the assembly and the render
        // -------------------------------------------------------------------

        private async Task<StoredFile?> AssembleAsync(
            IReadOnlyDictionary<string, string> graphics,
            IReadOnlyList<(int Beat, TimeSpan MeasuredDuration)> beatDurations,
            StoredFile narration,
            CancellationToken cancellationToken)
        {
            _stage = ProductionStage.Production;
            var settings = _service._settings;
            var visuals = new List<(string Implements, int Beat, string Still)>();
            StoredFile rendered;
            MediaProbe probe;
            DecodeCheck decode;
            IReadOnlyList<TimelineSegment> timeline;
            try
            {
                foreach (var graphic in Material.Graphics.OrderBy(g => g.Id, StringComparer.Ordinal))
                {
                    visuals.Add((graphic.Id, graphic.Beat, graphics[graphic.Id]));
                }

                foreach (var clip in Material.Clips.OrderBy(c => c.Id, StringComparer.Ordinal))
                {
                    var name = clip.Id.ToLowerInvariant() + "-placeholder";
                    var unsourced = Material.UnsourcedClips.Ids.Contains(clip.Id, StringComparer.Ordinal);
                    var text = Wrap(["PLACEHOLDER", $"{clip.Id}, beat {clip.Beat}", clip.Label, string.Empty,
                        unsourced ? "specified, not sourced" : "licensed clip not included in this video"]);
                    var stored = await DrawAsync(name, text, StillStyle.Placeholder, $"production/{name}.png", cancellationToken).ConfigureAwait(false);
                    await RecordArtifactAsync(ProductionStage.Production, ArtifactRole.ClipPlaceholder, clip.Id, stored, null, cancellationToken).ConfigureAwait(false);
                    visuals.Add((clip.Id, clip.Beat, $"production/{name}.png"));
                }

                timeline = BeatTimeline.Compose(beatDurations, visuals, settings.FramesPerSecond);
                var total = timeline.Sum(s => s.Frames);
                var list = new StringBuilder("ffconcat version 1.0\n");
                foreach (var segment in timeline)
                {
                    list.Append(CultureInfo.InvariantCulture, $"file '../{segment.Still}'\nduration {BeatTimeline.Seconds(segment.Frames, settings.FramesPerSecond)}\n");
                }

                // The last still is named once more, so the segment list's final duration is honoured.
                list.Append(CultureInfo.InvariantCulture, $"file '../{timeline[^1].Still}'\n");
                await _service._store.WriteStagingTextAsync(Staging, "segments.txt", list.ToString(), cancellationToken).ConfigureAwait(false);

                Output.WriteLine($"Rendering {timeline.Count} segments, {total} frames, under the configured render bound of {settings.RenderBound.TotalMinutes:0} min; no transaction is open.");
                var name2 = $"{Material.Item}-v{Version}.mp4";
                await _service._tool.RenderAsync(
                    new RenderSpecification(Staging, "segments.txt", $"../audio/{Path.GetFileName(narration.RelativePath)}", name2, total),
                    cancellationToken).ConfigureAwait(false);
                rendered = await _service._store.PromoteAsync(Staging, name2, Rel($"production/{name2}"), cancellationToken).ConfigureAwait(false);
                probe = await _service._tool.ProbeAsync(rendered.FullPath, cancellationToken).ConfigureAwait(false);
                decode = await _service._tool.DecodeAsync(rendered.FullPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await TryEndStageAsync(ProductionStage.Production, StageOutcome.Failed, $"the assembly did not complete: {failure.Message}", cancellationToken).ConfigureAwait(false);
                return null;
            }

            var problems = new List<string>();
            if (probe.Count("video") != 1 || probe.Count("audio") != 1 || probe.StreamKinds.Count != 2)
            {
                problems.Add($"the file carries streams [{string.Join(", ", probe.StreamKinds)}], not exactly one video and one audio");
            }

            if (decode.Errors != 0)
            {
                problems.Add($"decoding it end to end reported {decode.Errors} error(s), the first: {decode.FirstError}");
            }

            await RecordArtifactAsync(ProductionStage.Production, ArtifactRole.RenderedVideo, Material.Item, rendered, probe.Duration, cancellationToken).ConfigureAwait(false);

            if (problems.Count > 0)
            {
                await TryEndStageAsync(ProductionStage.Production, StageOutcome.Failed, string.Join("; ", problems), cancellationToken).ConfigureAwait(false);
                return null;
            }

            var specified = TimeSpan.FromSeconds(Material.SpecifiedRuntime.Seconds);
            var placed = timeline.Where(s => Material.Clips.Any(c => c.Id == s.Implements)).ToArray();
            await RecordStageAsync(ProductionStage.Production, StageOutcome.Held,
                string.Create(CultureInfo.InvariantCulture,
                    $"rendered {rendered.RelativePath}: one video and one audio stream, decoded end to end with 0 errors; runtime {probe.Duration.TotalSeconds:0.000} s MEASURED from the file "
                    + $"(the container duration of a file decoded end to end with zero errors), beside the SPECIFIED {(int)specified.TotalMinutes} min {specified.Seconds} s; narration from the {_source} source; no music; "
                    + $"{placed.Length} clip positions carry a labelled placeholder inside their beat's measured span; "
                    + $"held: {string.Join(", ", Material.UnsourcedClips.Ids)} are specified but not sourced, and the first video carries no stock footage"),
                $"artifact:{rendered.RelativePath} sha256:{rendered.Sha256}", cancellationToken).ConfigureAwait(false);
            return rendered;
        }

        private void Summarise(StoredFile rendered)
        {
            var booked = _operations.Aggregate(Money.Zero(), (sum, o) => sum + o.ComputedCost);
            Output.WriteLine($"Rendered: {rendered.FullPath}");
            Output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  size {rendered.Length:N0} bytes, sha256 {rendered.Sha256}"));
            Output.WriteLine(OwnSource
                ? string.Create(CultureInfo.InvariantCulture, $"  operations: {_operations.Count}; booked metered spend {booked.Amount:0.00} {booked.Currency} (no operation is booked by an own source); {Unmeasured()}")
                : string.Create(CultureInfo.InvariantCulture, $"  operations: {_operations.Count}; booked {booked.Amount:0.00} {booked.Currency}, {Label()}"));
            Output.WriteLine("  nothing was published, uploaded or configured; the item stays held short of publish-ready.");
        }

        private string Label() => _request.Designation == StoreDesignation.Demonstration
            ? "DEMONSTRATION, never an observation of the company's spend"
            : "booked in the company store";

        // -------------------------------------------------------------------
        // The one way each record is written: one short transaction, the datastore's instant
        // -------------------------------------------------------------------

        private async Task<StoredFile> DrawAsync(string name, string text, StillStyle style, string relativePath, CancellationToken cancellationToken)
        {
            await _service._store.WriteStagingTextAsync(Staging, name + ".txt", text, cancellationToken).ConfigureAwait(false);
            await _service._tool.DrawStillAsync(new StillSpecification(Staging, name + ".txt", name + ".png", style), cancellationToken).ConfigureAwait(false);
            return await _service._store.PromoteAsync(Staging, name + ".png", Rel(relativePath), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Records one artifact, and with a narration file its decoded measurement and, for an own source, its provenance, on
        /// ONE short transaction after the file work they record: the three rows of one file commit together or not at all.
        /// </summary>
        private async Task RecordArtifactAsync(
            ProductionStage stage, ArtifactRole role, string implements, StoredFile stored, TimeSpan? duration, CancellationToken cancellationToken,
            NarrationMeasurement? measurement = null, NarrationProvenance? provenance = null)
        {
            await using var transaction = await _service._unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
            var record = await transaction.Production.RecordArtifactAsync(
                new ArtifactRecord
                {
                    Item = _request.Item,
                    Version = Version,
                    Stage = stage,
                    Role = role,
                    Implements = implements,
                    RelativePath = stored.RelativePath,
                    Length = stored.Length,
                    Sha256 = stored.Sha256,
                    MeasuredDuration = duration,
                },
                cancellationToken).ConfigureAwait(false);
            if (measurement is not null)
            {
                await transaction.Production.RecordMeasurementAsync(measurement, cancellationToken).ConfigureAwait(false);
            }

            if (provenance is not null)
            {
                await transaction.Production.RecordProvenanceAsync(provenance, cancellationToken).ConfigureAwait(false);
            }

            await transaction.Audit.AppendAsync(Entry(_request.Item, Version, "production.artifact-recorded",
                $"{role} {implements} recorded after it was written, promoted and re-read{(measurement is null ? string.Empty : ", with its decoded measurement")}{(provenance is null ? string.Empty : " and its provenance")}",
                $"artifact:{stored.RelativePath} sha256:{stored.Sha256}"),
                cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            Output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  artifact {stored.RelativePath}: {stored.Length:N0} bytes, sha256 {stored.Sha256}{(duration is { } d ? $", {d.TotalSeconds:0.000} s measured" : string.Empty)}, recorded at {record.RecordedAt:O}"));
        }

        private async Task RecordStageAsync(ProductionStage stage, StageOutcome outcome, string summary, string? evidence, CancellationToken cancellationToken)
        {
            await using var transaction = await _service._unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
            var at = await transaction.Dossiers.RecordStageStampedAsync(_request.Item, Version, stage, outcome, summary, evidence, cancellationToken).ConfigureAwait(false);
            await transaction.Audit.AppendAsync(Entry(_request.Item, Version, ItemDossierRecorderAction, $"stage {stage} recorded {outcome}", evidence ?? "item dossier register"),
                cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            Output.WriteLine($"Stage {stage}: {outcome} at {at:O} - {summary}");
        }

        /// <summary>
        /// Records the stage's end and stops the run with the exit code named. The record is written on a token that
        /// cannot be cancelled (correction CR-003), because it records what already happened; the caller's token is
        /// accepted only so every call site reads alike.
        /// </summary>
        private async Task EndStageAsync(ProductionStage stage, StageOutcome outcome, string summary, int exitCode, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            await RecordStageAsync(stage, outcome, summary, null, CancellationToken.None).ConfigureAwait(false);
            throw new StageEndedException(exitCode);
        }

        /// <summary>Records the stage's end where the store still accepts it; a failure here is printed, never hidden.</summary>
        private async Task TryEndStageAsync(ProductionStage stage, StageOutcome outcome, string summary, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            try
            {
                // Not cancellable (correction CR-003): it records what already happened.
                await RecordStageAsync(stage, outcome, summary, null, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception failure)
            {
                Output.WriteLine($"Stage {stage} could not be recorded {outcome} ({failure.GetType().Name}): {summary}");
            }
        }

        private static AuditEntryDraft Entry(ItemId item, ItemVersion version, string action, string reason, string outputs) => new()
        {
            Actor = WorkforceRole.Producer.ToString(),
            Action = action,
            Subject = $"item:{item} version:{version}",
            Reason = reason,
            InputsReference = $"production:{item}:{version}",
            OutputsReference = outputs,
            Decision = action,
            CostReference = "none",
            Risk = "none",
            RetentionClass = RetentionClass.OperationalRecord,
        };

        private const string ItemDossierRecorderAction = "dossier.component-recorded";
    }

    /// <summary>Wraps lines for a still at word boundaries, so no line runs off the frame; nothing is added or removed.</summary>
    internal static string Wrap(IReadOnlyList<string> lines, int width = 52)
    {
        var text = new StringBuilder();
        foreach (var line in lines)
        {
            var current = new StringBuilder();
            foreach (var word in line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            {
                if (current.Length > 0 && current.Length + 1 + word.Length > width)
                {
                    text.Append(current).Append('\n');
                    current.Clear();
                }

                current.Append(current.Length > 0 ? " " : string.Empty).Append(word);
            }

            text.Append(current).Append('\n');
        }

        return text.ToString().TrimEnd('\n');
    }

    /// <summary>A stage ended the run; its outcome is already recorded.</summary>
    private sealed class StageEndedException(int exitCode) : Exception
    {
        public int ExitCode { get; } = exitCode;
    }

    private sealed class UnreachableException : Exception;
}
