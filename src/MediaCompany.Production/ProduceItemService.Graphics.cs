using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Work;

namespace MediaCompany.Production;

/// <summary>One graphic as the Design stage produced it: its kind, its recorded file (a still or a frame manifest) and its frames.</summary>
internal sealed record DrawnGraphic(string Position, GraphicKind Kind, StoredFile File, IReadOnlyList<FrameLine> Frames);

/// <summary>One frame of a motion graphic as its manifest lists it: its ordinal, its path in the run folder, its length and SHA-256.</summary>
internal sealed record FrameLine(long Ordinal, string RunPath, long Length, string Sha256)
{
    public string Line => string.Create(CultureInfo.InvariantCulture, $"{Ordinal} {RunPath} {Length} {Sha256}");

    public static FrameLine Parse(string line)
    {
        var parts = line.Split(' ');
        return parts.Length == 4
            ? new FrameLine(long.Parse(parts[0], CultureInfo.InvariantCulture), parts[1], long.Parse(parts[2], CultureInfo.InvariantCulture), parts[3])
            : throw new FormatException($"a frame manifest line is not an ordinal, a path, a length and a hash: \"{line}\"");
    }
}

public sealed partial class ProduceItemService
{
    /// <summary>
    /// The renderer's own client packages for a speech vendor and a speech-recognition model, installed in its dependency tree
    /// and never to be loaded: a bundle holding a module of either is refused (the graphics change, decision D-010).
    /// </summary>
    internal static readonly IReadOnlyList<string> ExcludedPackages = ["@remotion/elevenlabs", "@remotion/openai-whisper"];

    private sealed partial class ProductionRun
    {
        private static string ClipName(string id) => id.ToLowerInvariant() + "-placeholder";

        private static string StillPath(string id) => $"design/{id.ToLowerInvariant()}.png";

        private static string ManifestPath(string id) => $"design/{id.ToLowerInvariant()}-frames.txt";

        private static string FramePath(string id, long ordinal) => string.Create(CultureInfo.InvariantCulture, $"design/{id.ToLowerInvariant()}/frame-{ordinal:000000}.png");

        /// <summary>
        /// THE ONE TIMELINE (the graphics change, decision D-004 of its design): the delivered rule over the beat durations measured
        /// from the narration actually used, the graphics then the clips per beat with their planned paths; Design and the
        /// assembly read this one result, so the segment boundaries are the delivered ones by construction.
        /// </summary>
        private IReadOnlyList<TimelineSegment> ComposeTimeline(IReadOnlyList<(int Beat, TimeSpan MeasuredDuration)> beats)
        {
            var kinds = _plan.Graphics.Check!.Specifications.ToDictionary(s => s.Position, s => s.Kind, StringComparer.Ordinal);
            var visuals = new List<(string Implements, int Beat, string Still)>();
            foreach (var graphic in Material.Graphics.OrderBy(g => g.Id, StringComparer.Ordinal))
            {
                visuals.Add((graphic.Id, graphic.Beat, kinds[graphic.Id] == GraphicKind.Still ? StillPath(graphic.Id) : ManifestPath(graphic.Id)));
            }

            foreach (var clip in Material.Clips.OrderBy(c => c.Id, StringComparer.Ordinal))
            {
                visuals.Add((clip.Id, clip.Beat, $"production/{ClipName(clip.Id)}.png"));
            }

            return BeatTimeline.Compose(beats, visuals, _service._settings.FramesPerSecond);
        }

        // -------------------------------------------------------------------
        // Design: every graphic drawn by the approved renderer, after Audio, from its checked specification
        // -------------------------------------------------------------------

        /// <summary>
        /// THE DESIGN STAGE (the graphics change, decisions D-002, D-003, D-005 and D-006 of its design). The installation is
        /// verified and HELD from here until the last render exits; the render script, the drawing program, the style file and
        /// the specifications are staged by the one writer from the very buffers the plan hashed, re-hashed and held; the
        /// drawing program is bundled once (caching off, every folder in staging) and every bundle file hashed and held; each
        /// graphic is rendered by its own process under its own bound — a still as its final frame, a motion graphic as exactly
        /// its segment's frames — twice in a demonstration store and compared frame by frame, once in the company store with
        /// every still compared with the owner's approved bytes; frames are promoted, re-hashed, listed in a manifest and
        /// recorded with their provenance on one short transaction each. No transaction is open while anything runs.
        /// </summary>
        private async Task<IReadOnlyDictionary<string, DrawnGraphic>> DesignAsync(IReadOnlyList<TimelineSegment> timeline, CancellationToken cancellationToken)
        {
            _stage = ProductionStage.Design;
            var settings = _service._settings;
            var graphics = settings.Graphics!;
            var plan = _plan.Graphics;
            var store = _service._store;
            var renderer = _service._renderer!;
            var demonstration = _request.Designation == StoreDesignation.Demonstration;
            var verifier = new RendererInstallationVerifier(graphics);
            var drawn = new Dictionary<string, DrawnGraphic>(StringComparer.Ordinal);
            var c = CultureInfo.InvariantCulture;

            HeldInstallation? installation = null;
            var inputs = new HeldFiles();
            var bundle = new HeldFiles();
            RendererReport report;
            string bundleDigest;
            var sameness = new List<string>();
            var seconds = TimeSpan.Zero;
            try
            {
                try
                {
                    (report, installation) = await verifier.HoldAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (InstallationRefusedException refused)
                {
                    await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                        $"the graphics installation no longer verifies at the Design stage's start: {string.Join("; ", refused.Findings.Take(5))}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    throw new UnreachableException();
                }

                Output.WriteLine(string.Create(c, $"Design: {report.FilesVerified:N0} installed files verified and held read-only until the last render exits; {report.DescribeManifests()}"));

                // The staged inputs, written from the buffers the plan hashed, then re-hashed through held handles.
                var folder = store.StagingSubfolder(Staging, "graphics");
                var drawing = store.StagingSubfolder(folder, "drawing");
                var publicFolder = store.StagingSubfolder(folder, "public");
                var temporary = store.StagingSubfolder(folder, "tmp");
                var staged = new List<(string Path, string Expected, string What)>();
                await store.WriteStagingBytesAsync(folder, "render.mjs", plan.RenderScript!.Bytes, cancellationToken).ConfigureAwait(false);
                staged.Add((Path.Combine(folder, "render.mjs"), plan.RenderScript.Sha256, "the render script"));
                foreach (var file in plan.Drawing)
                {
                    await store.WriteStagingBytesAsync(drawing, file.Name, file.Bytes, cancellationToken).ConfigureAwait(false);
                    staged.Add((Path.Combine(drawing, file.Name), file.Sha256, $"the drawing program's {file.Name}"));
                }

                await store.WriteStagingBytesAsync(folder, "style.json", plan.Style!.Bytes, cancellationToken).ConfigureAwait(false);
                staged.Add((Path.Combine(folder, "style.json"), plan.Style.Sha256, "the style file"));
                foreach (var spec in plan.Check!.Specifications)
                {
                    await store.WriteStagingBytesAsync(folder, spec.FileName, spec.Bytes, cancellationToken).ConfigureAwait(false);
                    staged.Add((Path.Combine(folder, spec.FileName), spec.Sha256, $"{spec.Position}'s specification"));
                }

                await store.CopyIntoStagingAsync(publicFolder, graphics.RegularFace, "face-regular.ttf", cancellationToken).ConfigureAwait(false);
                await store.CopyIntoStagingAsync(publicFolder, graphics.ItalicFace, "face-italic.ttf", cancellationToken).ConfigureAwait(false);
                staged.Add((Path.Combine(publicFolder, "face-regular.ttf"), report.Regular!.Sha256, "the regular face's staged copy"));
                staged.Add((Path.Combine(publicFolder, "face-italic.ttf"), report.Italic!.Sha256, "the italic face's staged copy"));
                foreach (var (path, expected, what) in staged)
                {
                    var now = await inputs.HoldAsync(path, cancellationToken).ConfigureAwait(false);
                    if (!string.Equals(now, expected, StringComparison.Ordinal))
                    {
                        await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                            $"{what} was staged as sha256 {now}, and the plan hashed {expected}; the bytes rendered must be the bytes checked", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    }
                }

                // The bundle: built once, every file hashed and held while the renderer's file server serves it.
                var anchor = Path.GetFullPath(Path.Combine(graphics.Renderer.Folder, "package.json"));
                var bundleRequest = Json(new SortedDictionary<string, object>(StringComparer.Ordinal)
                {
                    ["anchor"] = anchor,
                    ["modules"] = Path.GetFullPath(Path.Combine(graphics.Renderer.Folder, "node_modules")),
                    ["entry"] = "drawing/index.jsx",
                    ["publicDir"] = "public",
                    ["outDir"] = "bundle",
                    ["moduleList"] = "bundle-modules.json",
                });
                await store.WriteStagingBytesAsync(folder, "bundle-request.json", bundleRequest, cancellationToken).ConfigureAwait(false);
                GraphicInvocation bundled;
                try
                {
                    bundled = await renderer.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Bundle, "bundle-request.json", temporary, graphics.BundleBound), cancellationToken).ConfigureAwait(false);
                }
                catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
                {
                    await EndStageAsync(ProductionStage.Design, StageOutcome.Failed, $"the drawing program could not be bundled: {failure.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    throw new UnreachableException();
                }

                seconds += bundled.Elapsed;
                var (_, moduleBytes) = await inputs.HoldBytesAsync(Path.Combine(folder, "bundle-modules.json"), cancellationToken).ConfigureAwait(false);
                var modules = JsonSerializer.Deserialize<string[]>(moduleBytes) ?? [];
                var excluded = modules.Where(m => ExcludedPackages.Any(p => m.Replace('\\', '/').Contains("/node_modules/" + p + "/", StringComparison.OrdinalIgnoreCase))).ToArray();
                if (excluded.Length > 0)
                {
                    await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                        $"the bundle holds {excluded.Length} module(s) of an excluded speech client package, the first {excluded[0]}; a graphics bundle loads none", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                }

                var bundleFolder = Path.Combine(folder, "bundle");
                var bundleFiles = new List<(string Name, string Sha256)>();
                foreach (var file in Directory.EnumerateFiles(bundleFolder, "*", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
                {
                    bundleFiles.Add((Path.GetRelativePath(bundleFolder, file).Replace('\\', '/'), await bundle.HoldAsync(file, cancellationToken).ConfigureAwait(false)));
                }

                bundleDigest = GraphicsPlan.Digest(bundleFiles);
                foreach (var (name, expected) in new[] { ("public/face-regular.ttf", report.Regular.Sha256), ("public/face-italic.ttf", report.Italic.Sha256) })
                {
                    var copy = bundleFiles.FirstOrDefault(f => f.Name == name);
                    if (!string.Equals(copy.Sha256, expected, StringComparison.Ordinal))
                    {
                        await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                            $"the bundle's face copy {name} hashes {copy.Sha256 ?? "(absent)"}, and the verified face hashes {expected}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                    }
                }

                Output.WriteLine(string.Create(c, $"  bundled the drawing program in {bundled.Elapsed.TotalSeconds:0.0} s (observed on this machine): {bundleFiles.Count} files, digest {bundleDigest}, {modules.Length} modules, none of an excluded package; held read-only"));

                // Each graphic, in position order, by its own process under its own bound.
                foreach (var spec in plan.Check.Specifications)
                {
                    var segment = timeline.Single(t => t.Implements == spec.Position);
                    var frames = spec.Kind == GraphicKind.Still ? 1 : segment.Frames;
                    var name = spec.Position.ToLowerInvariant();
                    var renders = new List<(IReadOnlyList<string> Hashes, IReadOnlyList<string> Files, string Folder, GraphicInvocation Invocation, string RequestSha)>();
                    for (var attempt = 1; attempt <= (demonstration ? 2 : 1); attempt++)
                    {
                        var output = $"frames-{name}-{attempt}";
                        var outputFolder = store.StagingSubfolder(folder, output);
                        var request = Json(new SortedDictionary<string, object>(StringComparer.Ordinal)
                        {
                            ["anchor"] = anchor,
                            ["bundle"] = "bundle",
                            ["browser"] = Path.GetFullPath(graphics.BrowserExecutable),
                            ["specification"] = spec.FileName,
                            ["style"] = "style.json",
                            ["timing"] = new SortedDictionary<string, object>(StringComparer.Ordinal)
                            {
                                ["kind"] = spec.Kind == GraphicKind.Still ? "still" : "motion",
                                ["frames"] = frames,
                                ["fps"] = settings.FramesPerSecond,
                                ["width"] = settings.Width,
                                ["height"] = settings.Height,
                            },
                            ["faces"] = new SortedDictionary<string, object>(StringComparer.Ordinal) { ["regular"] = "face-regular.ttf", ["italic"] = "face-italic.ttf" },
                            ["output"] = output,
                            ["concurrency"] = graphics.Concurrency,
                            ["frameTimeoutMs"] = (long)graphics.FrameTimeout.TotalMilliseconds,
                        });
                        var requestName = $"render-{name}-{attempt}.json";
                        await store.WriteStagingBytesAsync(folder, requestName, request, cancellationToken).ConfigureAwait(false);
                        var bound = graphics.RenderBound(frames);
                        GraphicInvocation invocation;
                        try
                        {
                            invocation = await renderer.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Render, requestName, temporary, bound), cancellationToken).ConfigureAwait(false);
                        }
                        catch (MediaToolBoundExceededException exceeded)
                        {
                            await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                                string.Create(c, $"{spec.Position} passed its configured render bound of {bound.TotalSeconds:0.0} s ({graphics.RenderFixed.TotalSeconds:0} s plus {graphics.RenderPerFrame.TotalSeconds:0.###} s for each of {frames} frames) on the monotonic clock; its job was closed and every process in it ended ({exceeded.Message})"),
                                ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                            throw new UnreachableException();
                        }
                        catch (MediaToolFailedException failed)
                        {
                            await EndStageAsync(ProductionStage.Design, StageOutcome.Failed, $"{spec.Position} could not be drawn: {failed.Message}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                            throw new UnreachableException();
                        }

                        seconds += invocation.Elapsed;
                        var files = FrameFiles(outputFolder, frames);
                        if (files is null)
                        {
                            await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                                string.Create(c, $"{spec.Position}'s render wrote {Directory.EnumerateFiles(outputFolder).Count()} file(s), not exactly its {frames} frame(s) numbered from 0"), ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                        }

                        var hashes = new List<string>();
                        foreach (var file in files!)
                        {
                            hashes.Add(await FileHashes.Sha256Async(file, cancellationToken).ConfigureAwait(false));
                        }

                        renders.Add((hashes, files, outputFolder, invocation, Convert.ToHexStringLower(SHA256.HashData(request))));
                    }

                    var first = renders[0];
                    var setDigest = FrameSetDigest(first.Hashes);
                    SamenessKind kind;
                    string statement;
                    if (demonstration)
                    {
                        var second = renders[1];
                        var differing = Enumerable.Range(0, first.Hashes.Count).FirstOrDefault(k => first.Hashes[k] != second.Hashes[k], -1);
                        (kind, statement) = differing < 0
                            ? (SamenessKind.RepeatsTwoRenders, string.Create(c, $"repeats: rendered twice in separate processes with identical inputs at concurrency {graphics.Concurrency}, all {frames} frame(s) byte-identical (frame-set sha256 {setDigest})"))
                            : (SamenessKind.DifferedTwoRenders, string.Create(c, $"not observed to repeat: rendered twice in separate processes with identical inputs gave frame-set sha256 {setDigest} and {FrameSetDigest(second.Hashes)}; the first differing frame is {differing}"));
                    }
                    else if (spec.Kind == GraphicKind.Still)
                    {
                        var approved = plan.Approval?.For(spec.Position)?.StillSha256;
                        if (!string.Equals(approved, first.Hashes[0], StringComparison.Ordinal))
                        {
                            await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                                $"{spec.Position}'s still renders as sha256 {first.Hashes[0]}, and the owner approved sha256 {approved ?? "(none recorded)"}; a still must equal its approved render",
                                ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
                        }

                        (kind, statement) = (SamenessKind.EqualsApprovedStill, $"equals the owner's approved still: sha256 {first.Hashes[0]} (approval file sha256 {plan.Approval!.Sha256}); rendered once");
                    }
                    else
                    {
                        (kind, statement) = (SamenessKind.RetimedRenderedOnce, string.Create(c, $"re-timed from this run's narration: {frames} frames rendered once and not compared (the owner's preview was timed from another narration)"));
                    }

                    sameness.Add($"{spec.Position} {kind}");
                    var drawnGraphic = await PromoteAsync(spec, segment, frames, first.Files, first.Hashes, cancellationToken).ConfigureAwait(false);
                    var changing = Enumerable.Range(1, Math.Max(0, first.Hashes.Count - 1)).Where(k => first.Hashes[k] != first.Hashes[k - 1]).ToArray();
                    var observed = renders.Aggregate(TimeSpan.Zero, (sum, r) => sum + r.Invocation.Elapsed);
                    var provenance = new GraphicProvenance
                    {
                        Item = _request.Item,
                        Version = Version,
                        RelativePath = drawnGraphic.File.RelativePath,
                        Position = spec.Position,
                        Kind = spec.Kind,
                        SpecificationPath = Path.GetRelativePath(settings.RepositoryRoot, Path.Combine(Path.GetDirectoryName(_request.Loaded.MaterialPath)!, "graphics", spec.FileName)).Replace('\\', '/'),
                        SpecificationSha256 = spec.Sha256,
                        StyleSha256 = plan.Style.Sha256,
                        DrawingProgramDigest = plan.DrawingDigest,
                        RenderScriptSha256 = plan.RenderScript.Sha256,
                        BundleDigest = bundleDigest,
                        RendererName = report.RendererName ?? "not read",
                        RendererVersion = report.RendererVersion ?? "not read",
                        Manifests = report.DescribeManifests(),
                        NodeSha256 = report.NodeSha256 ?? "not verified",
                        NodeVersion = report.NodeVersion ?? "not read",
                        BrowserSha256 = report.BrowserSha256 ?? "not verified",
                        BrowserVersion = report.BrowserVersion ?? "not read",
                        Faces = report.DescribeFaces(),
                        SegmentStartFrame = segment.StartFrame,
                        SegmentFrames = segment.Frames,
                        FramesRendered = frames,
                        FirstChangingFrame = changing.Length > 0 ? changing[0] : null,
                        LastChangingFrame = changing.Length > 0 ? changing[^1] : null,
                        FinalFrameSha256 = first.Hashes[^1],
                        FrameSetDigest = spec.Kind == GraphicKind.Still ? first.Hashes[0] : setDigest,
                        Concurrency = graphics.Concurrency,
                        Arguments = string.Join(" ", first.Invocation.Arguments.Select(a => a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a)),
                        RequestSha256 = first.RequestSha,
                        RenderSeconds = Math.Round((decimal)observed.TotalSeconds, 3),
                        LeftoverProcesses = renders.Max(r => r.Invocation.LeftoverProcesses),
                        Sameness = kind,
                        SamenessStatement = statement,
                    };
                    await RecordArtifactAsync(ProductionStage.Design, spec.Kind == GraphicKind.Still ? ArtifactRole.GraphicStill : ArtifactRole.GraphicMotion, spec.Position,
                        drawnGraphic.File, null, cancellationToken, graphic: provenance).ConfigureAwait(false);
                    Output.WriteLine(string.Create(c,
                        $"  {spec.Position}: {spec.Kind}, specification sha256 {spec.Sha256}, {frames} frame(s) in segment from frame {segment.StartFrame}, {(spec.Kind == GraphicKind.Still ? "still" : "manifest")} sha256 {drawnGraphic.File.Sha256}, "
                        + $"rendered in {observed.TotalSeconds:0.0} s observed on this machine ({renders.Count} render(s)), changing frames {(changing.Length > 0 ? $"{changing[0]} to {changing[^1]}" : "none")}, leftover processes {provenance.LeftoverProcesses}; {statement}"));
                    drawn[spec.Position] = drawnGraphic;
                }
            }
            finally
            {
                await bundle.DisposeAsync().ConfigureAwait(false);
                await inputs.DisposeAsync().ConfigureAwait(false);
                if (installation is not null)
                {
                    await installation.DisposeAsync().ConfigureAwait(false);
                }
            }

            // Released only now, after the last render exited; then verified once more, so a file a render left under an
            // installed folder (a re-created cache, an added file) or a changed file fails the stage by name.
            var after = await verifier.VerifyAsync(cancellationToken).ConfigureAwait(false);
            if (!after.Verified)
            {
                await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                    $"the graphics installation changed while the graphics were rendered: {string.Join("; ", after.Findings.Take(5))}", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }

            var stills = drawn.Values.Count(d => d.Kind == GraphicKind.Still);
            await RecordStageAsync(ProductionStage.Design, StageOutcome.Succeeded,
                string.Create(c, $"{drawn.Count} graphics drawn by code from their checked specifications with {report.RendererName} {report.RendererVersion} (browser {report.BrowserVersion}, Node {report.NodeVersion}, {report.Regular!.Reading.Family} {report.Regular.Reading.Version}): ")
                + string.Create(c, $"{stills} still and {drawn.Count - stills} motion, timed from the measured beat durations; installation verified before and after, held throughout; ")
                + string.Create(c, $"{(demonstration ? "each rendered twice and compared" : "stills compared with the owner's approval, motion re-timed")}: {sameness.Count(s => s.EndsWith(nameof(SamenessKind.RepeatsTwoRenders), StringComparison.Ordinal) || s.EndsWith(nameof(SamenessKind.EqualsApprovedStill), StringComparison.Ordinal))} repeat or equal; ")
                + string.Create(c, $"render time {seconds.TotalSeconds:0.0} s observed on this machine; no model call and no capability call; booked metered spend USD 0.00; local compute unmeasured"),
                $"artifacts:{Folder}/design", cancellationToken).ConfigureAwait(false);
            return drawn;
        }

        /// <summary>
        /// Promotes a graphic's frames by the one writer, each re-read and hashed after it is moved into place and required equal
        /// to the hash compared for sameness; a motion graphic's manifest lists every promoted frame and is stored the same way.
        /// </summary>
        private async Task<DrawnGraphic> PromoteAsync(CheckedSpecification spec, TimelineSegment segment, long frames, IReadOnlyList<string> files, IReadOnlyList<string> hashes, CancellationToken cancellationToken)
        {
            var store = _service._store;
            if (spec.Kind == GraphicKind.Still)
            {
                var still = await store.PromoteAsync(Path.GetDirectoryName(files[0])!, Path.GetFileName(files[0]), Rel(StillPath(spec.Position)), cancellationToken).ConfigureAwait(false);
                await RequireEqualAsync(spec.Position, still, hashes[0], cancellationToken).ConfigureAwait(false);
                return new DrawnGraphic(spec.Position, GraphicKind.Still, still, []);
            }

            var lines = new List<FrameLine>();
            for (var k = 0; k < files.Count; k++)
            {
                var path = FramePath(spec.Position, k);
                var frame = await store.PromoteAsync(Path.GetDirectoryName(files[k])!, Path.GetFileName(files[k]), Rel(path), cancellationToken).ConfigureAwait(false);
                await RequireEqualAsync(spec.Position, frame, hashes[k], cancellationToken).ConfigureAwait(false);
                lines.Add(new FrameLine(k, path, frame.Length, frame.Sha256));
            }

            var manifest = Encoding.UTF8.GetBytes(string.Concat(lines.Select(l => l.Line + "\n")));
            var stored = await store.StoreAsync(Staging, manifest, Rel(ManifestPath(spec.Position)), cancellationToken).ConfigureAwait(false);
            _ = segment;
            _ = frames;
            return new DrawnGraphic(spec.Position, GraphicKind.Motion, stored, lines);
        }

        private async Task RequireEqualAsync(string position, StoredFile promoted, string compared, CancellationToken cancellationToken)
        {
            if (!string.Equals(promoted.Sha256, compared, StringComparison.Ordinal))
            {
                await EndStageAsync(ProductionStage.Design, StageOutcome.Failed,
                    $"{position}'s file {promoted.RelativePath} was promoted as sha256 {promoted.Sha256}, and sha256 {compared} was hashed and compared in staging", ProduceResult.StageFailed, cancellationToken).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// The segment list the delivered encode reads (decision D-005): a still as one entry with its segment's duration, a
        /// motion graphic as one entry per frame of one frame's duration, a clip placeholder as delivered. Every graphic file is
        /// re-hashed THROUGH A HELD HANDLE against its record — each still and manifest against its artifact, each frame against
        /// its manifest line — and stays held until the encode exits; a difference fails the stage naming both hashes. The
        /// written durations may not drift half a frame from the exact frame times.
        /// </summary>
        private async Task<(string? List, long Total, int Entries, HeldFiles Held)> SegmentListAsync(
            IReadOnlyList<TimelineSegment> timeline, IReadOnlyDictionary<string, DrawnGraphic> graphics, CancellationToken cancellationToken)
        {
            var fps = _service._settings.FramesPerSecond;
            var held = new HeldFiles();
            var list = new StringBuilder("ffconcat version 1.0\n");
            long total = 0;
            var entries = 0;
            decimal written = 0m;
            string last = string.Empty;

            async Task<bool> SameAsync(string what, string path, string recorded)
            {
                var now = await held.HoldAsync(path, cancellationToken).ConfigureAwait(false);
                if (string.Equals(now, recorded, StringComparison.Ordinal))
                {
                    return true;
                }

                await TryEndStageAsync(ProductionStage.Production, StageOutcome.Failed,
                    $"{what} changed after it was recorded: sha256 {recorded} recorded and {now} when the assembly read it; nothing is encoded from it", cancellationToken).ConfigureAwait(false);
                return false;
            }

            void Entry(string runPath, long frames)
            {
                var seconds = BeatTimeline.Seconds(frames, fps);
                list.Append(CultureInfo.InvariantCulture, $"file '../{runPath}'\nduration {seconds}\n");
                written += decimal.Parse(seconds, CultureInfo.InvariantCulture);
                entries++;
                last = runPath;
            }

            foreach (var segment in timeline)
            {
                total += segment.Frames;
                if (!graphics.TryGetValue(segment.Implements, out var graphic))
                {
                    Entry(segment.Still, segment.Frames);
                    continue;
                }

                if (graphic.Kind == GraphicKind.Still)
                {
                    if (!await SameAsync($"{graphic.Position}'s still {graphic.File.RelativePath}", graphic.File.FullPath, graphic.File.Sha256).ConfigureAwait(false))
                    {
                        return (null, total, entries, held);
                    }

                    Entry(segment.Still, segment.Frames);
                    continue;
                }

                var (manifestSha, bytes) = await held.HoldBytesAsync(graphic.File.FullPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(manifestSha, graphic.File.Sha256, StringComparison.Ordinal))
                {
                    await TryEndStageAsync(ProductionStage.Production, StageOutcome.Failed,
                        $"{graphic.Position}'s frame manifest {graphic.File.RelativePath} changed after it was recorded: sha256 {graphic.File.Sha256} recorded and {manifestSha} when the assembly read it", cancellationToken).ConfigureAwait(false);
                    return (null, total, entries, held);
                }

                var lines = Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(FrameLine.Parse).ToArray();
                if (lines.Length != segment.Frames)
                {
                    await TryEndStageAsync(ProductionStage.Production, StageOutcome.Failed,
                        string.Create(CultureInfo.InvariantCulture, $"{graphic.Position}'s frame manifest lists {lines.Length} frames, and its segment holds {segment.Frames}"), cancellationToken).ConfigureAwait(false);
                    return (null, total, entries, held);
                }

                foreach (var line in lines)
                {
                    if (!await SameAsync($"{graphic.Position}'s frame {line.RunPath}", Path.Combine(_service._store.FullPath(Folder), line.RunPath), line.Sha256).ConfigureAwait(false))
                    {
                        return (null, total, entries, held);
                    }

                    Entry(line.RunPath, 1);
                }
            }

            // The last file is named once more, so the segment list's final duration is honoured.
            list.Append(CultureInfo.InvariantCulture, $"file '../{last}'\n");

            var drift = Math.Abs(written - ((decimal)total / fps));
            if (drift >= 0.5m / fps)
            {
                await TryEndStageAsync(ProductionStage.Production, StageOutcome.Failed,
                    string.Create(CultureInfo.InvariantCulture, $"the segment list's written durations drift {drift:0.000000} s from the exact frame times, half a frame or more; nothing is encoded"), cancellationToken).ConfigureAwait(false);
                return (null, total, entries, held);
            }

            return (list.ToString(), total, entries, held);
        }

        /// <summary>A render's frame files in frame order, or null where they are not exactly the frames 0 to n-1.</summary>
        private static IReadOnlyList<string>? FrameFiles(string folder, long frames)
        {
            var found = new SortedDictionary<long, string>();
            foreach (var file in Directory.EnumerateFiles(folder))
            {
                var match = Regex.Match(Path.GetFileName(file), @"^element-(\d+)\.png$", RegexOptions.CultureInvariant);
                if (!match.Success || !found.TryAdd(long.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), file))
                {
                    return null;
                }
            }

            return found.Count == frames && found.Keys.First() == 0 && found.Keys.Last() == frames - 1 ? found.Values.ToArray() : null;
        }

        private static string FrameSetDigest(IReadOnlyList<string> hashes) =>
            Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(string.Concat(hashes.Select((h, k) => string.Create(CultureInfo.InvariantCulture, $"{k} {h}\n"))))));

        private static byte[] Json(object value) => JsonSerializer.SerializeToUtf8Bytes(value);
    }
}
