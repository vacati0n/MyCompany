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

/// <summary>How a produce run ended: the exit code, and what it produced.</summary>
public sealed record ProduceResult(int ExitCode, ItemVersion? Version, StoredFile? Rendered, IReadOnlyList<OperationRecord> Operations)
{
    public const int Succeeded = 0;
    public const int RefusedBeforeAnyCall = 2;
    public const int StageFailed = 3;
    public const int CapStopped = 4;
    public const int AttemptNotRecorded = 5;
}

/// <summary>
/// The produce service (the production change, decisions D-010, D-012 and D-015 of its design): one command turns a
/// recorded item package into one rendered video file, recording every stage outcome, every operation and every
/// artifact on the datastore's clock, regenerating no script, research or claim, and publishing nothing.
///
/// THE ORDER. A production opens the next item version, records the eight carried stages citing the package's
/// version, then runs the four producing stages in EXECUTION order — Design, Audio, Thumbnail, Production — because
/// the assembly consumes the narration; readers list them in the closed stage order. A stage that cannot finish is
/// recorded failed or held, naming why, and the run makes no further call.
///
/// THE HOLDS. No transaction and no record is held open while the external tool runs or while a vendor is called
/// from here: every record is one short transaction of its own, and the capability boundary keeps its own
/// admission transaction to the call it makes. Every bound is the configured one.
/// </summary>
public sealed class ProduceItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionReader _reader;
    private readonly IMediaTool _tool;
    private readonly IArtifactStore _store;
    private readonly ProductionSettings _settings;
    private readonly TextWriter _output;

    public ProduceItemService(
        IUnitOfWork unitOfWork,
        IProductionReader reader,
        IMediaTool tool,
        IArtifactStore store,
        ProductionSettings settings,
        TextWriter output)
    {
        _unitOfWork = unitOfWork;
        _reader = reader;
        _tool = tool;
        _store = store;
        _settings = settings;
        _output = output;
    }

    /// <summary>Reads the package and composes the plan, refusing where the narration cannot be split as designed.</summary>
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

        var parts = NarrationSplitter.Split(request.Loaded.Narration, material.Beats, _settings.NarrationCharacterMaximum);
        var pricing = await _reader.PricingAsync(CapabilityClass.Narration, cancellationToken).ConfigureAwait(false);
        return ProductionPlan.Compose(package, parts, pricing);
    }

    /// <summary>
    /// Runs one production. In plan-only mode the plan is printed and nothing is called or written. Otherwise the
    /// gateway given is the one the host composed for the mode: the demonstration composition in fake mode, the
    /// vendor composition in metered mode; this service cannot tell them apart and needs no code change between them.
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

        if (request.Mode == ProductionMode.PlanOnly)
        {
            _output.WriteLine("Plan only: no capability call was made and no file was written.");
            return new ProduceResult(ProduceResult.Succeeded, null, null, []);
        }

        if (plan.Package.Cap is null)
        {
            _output.WriteLine("Refused before any call: the item has no recorded cap.");
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        if (gateway is null)
        {
            throw new InvalidOperationException("A producing mode needs the capability boundary its composition built.");
        }

        if (string.IsNullOrWhiteSpace(_settings.NarrationVoice))
        {
            _output.WriteLine("Refused before any call: no narration voice is configured (setting narrationVoice).");
            return new ProduceResult(ProduceResult.RefusedBeforeAnyCall, null, null, []);
        }

        var run = await ProductionRun.OpenAsync(this, request, plan, gateway, cancellationToken).ConfigureAwait(false);
        return await run.ExecuteAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>One production version in progress, and every record it writes.</summary>
    private sealed class ProductionRun
    {
        private readonly ProduceItemService _service;
        private readonly ProduceRequest _request;
        private readonly ProductionPlan _plan;
        private readonly ICapabilityGateway _gateway;
        private readonly List<OperationRecord> _operations = [];
        private readonly JobId _job = JobId.New();

        private ProductionRun(ProduceItemService service, ProduceRequest request, ProductionPlan plan, ICapabilityGateway gateway, ItemVersion version, string staging, string folder)
        {
            _service = service;
            _request = request;
            _plan = plan;
            _gateway = gateway;
            Version = version;
            Staging = staging;
            Folder = folder;
        }

        private ItemVersion Version { get; }

        private string Staging { get; }

        private string Folder { get; }

        private ItemMaterial Material => _request.Loaded.Material;

        private TextWriter Output => _service._output;

        public static async Task<ProductionRun> OpenAsync(
            ProduceItemService service, ProduceRequest request, ProductionPlan plan, ICapabilityGateway gateway, CancellationToken cancellationToken)
        {
            ProductionVersionOpened opened;
            await using (var transaction = await service._unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false))
            {
                opened = await transaction.Production.OpenProductionVersionAsync(request.Item, request.Mode, cancellationToken).ConfigureAwait(false);
                await transaction.Audit.AppendAsync(Entry(request.Item, opened.Version, "production.version-opened",
                    $"production version {opened.Version} opened in {request.Mode} mode", "production version record"), cancellationToken).ConfigureAwait(false);
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            var folder = $"{request.Loaded.Material.Item}/v{opened.Version}";
            var staging = service._store.BeginRun(folder);
            service._output.WriteLine($"Opened item version {opened.Version} at {opened.OpenedAt:O} (the datastore's instant); output folder {service._store.FullPath(folder)}");
            return new ProductionRun(service, request, plan, gateway, opened.Version, staging, folder);
        }

        public async Task<ProduceResult> ExecuteAsync(CancellationToken cancellationToken)
        {
            var version = await _service._tool.VersionLineAsync(cancellationToken).ConfigureAwait(false);
            Output.WriteLine($"Media tool: {version}");
            await _service._store.CopyIntoStagingAsync(Staging, _service._settings.FontFile, ExternalMediaTool.StagedFontName, cancellationToken).ConfigureAwait(false);

            await CarryAsync(cancellationToken).ConfigureAwait(false);

            try
            {
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
            var stills = new Dictionary<string, string>(StringComparer.Ordinal);
            try
            {
                foreach (var graphic in Material.Graphics)
                {
                    var name = graphic.Id.ToLowerInvariant();
                    var text = Wrap([graphic.Id, string.Empty, .. graphic.Lines]);
                    var stored = await DrawAsync(name, text, StillStyle.Graphic, $"design/{name}.png", cancellationToken).ConfigureAwait(false);
                    await RecordArtifactAsync(ProductionStage.Design, ArtifactRole.GraphicStill, graphic.Id, stored, null, cancellationToken).ConfigureAwait(false);
                    stills[graphic.Id] = stored.RelativePath;
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
        // Audio: the whole recorded narration through the boundary
        // -------------------------------------------------------------------

        private async Task<(ProduceResult? Ended, IReadOnlyList<(int Beat, TimeSpan MeasuredDuration)>? BeatDurations, StoredFile? Narration)> AudioAsync(
            CancellationToken cancellationToken)
        {
            var partFiles = new List<(NarrationPart Part, StoredFile File, TimeSpan Duration)>();
            foreach (var planned in _plan.Operations)
            {
                var part = planned.Part;
                var outcome = await CallAsync(part, cancellationToken).ConfigureAwait(false);
                if (outcome.Ended is { } ended)
                {
                    return (ended, null, null);
                }

                var content = outcome.Content!;
                var stored = await _service._store.StoreAsync(Staging, content.Bytes, $"audio/part-{part.Ordinal:000}.wav", cancellationToken).ConfigureAwait(false);
                var probe = await ProbeAsync(stored.FullPath, ProductionStage.Audio, cancellationToken).ConfigureAwait(false);
                await RecordArtifactAsync(ProductionStage.Audio, ArtifactRole.NarrationPart, $"part {part.Ordinal} of beat {part.Beat}", stored, probe.Duration, cancellationToken).ConfigureAwait(false);
                partFiles.Add((part, stored, probe.Duration));
            }

            var list = new StringBuilder();
            foreach (var (_, file, _) in partFiles)
            {
                list.Append(CultureInfo.InvariantCulture, $"file '../audio/{Path.GetFileName(file.RelativePath)}'\n");
            }

            StoredFile narration;
            MediaProbe measured;
            try
            {
                await _service._store.WriteStagingTextAsync(Staging, "narration-parts.txt", list.ToString(), cancellationToken).ConfigureAwait(false);
                await _service._tool.ConcatenateAudioAsync(Staging, "narration-parts.txt", "narration.wav", cancellationToken).ConfigureAwait(false);
                narration = await _service._store.PromoteAsync(Staging, "narration.wav", "audio/narration.wav", cancellationToken).ConfigureAwait(false);
                measured = await _service._tool.ProbeAsync(narration.FullPath, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed, $"the narration parts could not be joined: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }

            await RecordArtifactAsync(ProductionStage.Audio, ArtifactRole.Narration, "the whole recorded narration", narration, measured.Duration, cancellationToken).ConfigureAwait(false);

            var beats = partFiles.GroupBy(p => p.Part.Beat)
                .Select(g => (g.Key, TimeSpan.FromTicks(g.Sum(p => p.Duration.Ticks))))
                .ToArray();
            var characters = _plan.Operations.Sum(o => o.Part.Text.Length);

            await RecordStageAsync(ProductionStage.Audio, StageOutcome.Succeeded,
                string.Create(CultureInfo.InvariantCulture,
                    $"narration of the whole recorded text, {characters:N0} characters in {partFiles.Count} request(s), joined with no inserted silence; "
                    + $"duration {measured.Duration.TotalSeconds:0.000} s MEASURED by probing the produced audio; no rate assumption is used"),
                $"artifact:{narration.RelativePath} sha256:{narration.Sha256}", cancellationToken).ConfigureAwait(false);
            return (null, beats, narration);
        }

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
                outcome = await _gateway.ExecuteAsync(
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
                        await EndStageAsync(ProductionStage.Audio, StageOutcome.Failed,
                            $"part {part.Ordinal}'s narration call failed ({completed.Operation.FailureReason}); it is booked as operation {completed.Operation.Id} and is not retried",
                            ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
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
                    visuals.Add((clip.Id, clip.Beat, stored.RelativePath));
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
                rendered = await _service._store.PromoteAsync(Staging, name2, $"production/{name2}", cancellationToken).ConfigureAwait(false);
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
                    $"rendered {rendered.RelativePath}: one video and one audio stream, decoded end to end with 0 errors; runtime {probe.Duration.TotalSeconds:0.000} s MEASURED from the file, "
                    + $"beside the SPECIFIED {(int)specified.TotalMinutes} min {specified.Seconds} s; no music; {placed.Length} clip positions carry a labelled placeholder inside their beat's measured span; "
                    + $"held: {string.Join(", ", Material.UnsourcedClips.Ids)} are specified but not sourced, and the first video carries no stock footage"),
                $"artifact:{rendered.RelativePath} sha256:{rendered.Sha256}", cancellationToken).ConfigureAwait(false);
            return rendered;
        }

        private void Summarise(StoredFile rendered)
        {
            var booked = _operations.Aggregate(Money.Zero(), (sum, o) => sum + o.ComputedCost);
            Output.WriteLine($"Rendered: {rendered.FullPath}");
            Output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  size {rendered.Length:N0} bytes, sha256 {rendered.Sha256}"));
            Output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                $"  operations: {_operations.Count}; booked {booked.Amount:0.00} {booked.Currency}, {Label()}"));
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
            return await _service._store.PromoteAsync(Staging, name + ".png", relativePath, cancellationToken).ConfigureAwait(false);
        }

        private async Task<MediaProbe> ProbeAsync(string file, ProductionStage stage, CancellationToken cancellationToken)
        {
            try
            {
                return await _service._tool.ProbeAsync(file, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
            {
                await EndStageAsync(stage, StageOutcome.Failed, $"a produced file could not be probed: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                throw new UnreachableException();
            }
        }

        private async Task RecordArtifactAsync(
            ProductionStage stage, ArtifactRole role, string implements, StoredFile stored, TimeSpan? duration, CancellationToken cancellationToken)
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
            await transaction.Audit.AppendAsync(Entry(_request.Item, Version, "production.artifact-recorded",
                $"{role} {implements} recorded after it was written, promoted and re-read", $"artifact:{stored.RelativePath} sha256:{stored.Sha256}"),
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

        /// <summary>Records the stage's end and stops the run with the exit code named.</summary>
        private async Task EndStageAsync(ProductionStage stage, StageOutcome outcome, string summary, int exitCode, CancellationToken cancellationToken)
        {
            await RecordStageAsync(stage, outcome, summary, null, cancellationToken).ConfigureAwait(false);
            throw new StageEndedException(exitCode);
        }

        /// <summary>Records the stage's end where the store still accepts it; a failure here is printed, never hidden.</summary>
        private async Task TryEndStageAsync(ProductionStage stage, StageOutcome outcome, string summary, CancellationToken cancellationToken)
        {
            try
            {
                await RecordStageAsync(stage, outcome, summary, null, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception failure) when (failure is not OperationCanceledException)
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
