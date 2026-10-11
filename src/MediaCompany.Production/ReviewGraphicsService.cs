using System.Globalization;
using System.Text;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Production;

namespace MediaCompany.Production;

/// <summary>What one review of a demonstration production's graphics asked for.</summary>
public sealed record ReviewRequest(ItemId Item, ItemVersion Version, LoadedItem Loaded, StoreDesignation? Designation, string DatabaseName);

/// <summary>
/// The review command's service (the graphics change, decision D-007 of its design): the owner's contact sheet and preview,
/// built from a DEMONSTRATION production's own graphic files and refused on any other store before any process. Each graphic's
/// recorded file is re-hashed through a held handle (every frame of a motion graphic against its manifest); each final frame is
/// copied byte for byte by the one writer beside the sheet, its copy's hash required equal; the sheet is composed by the media
/// tool from those copies, 22 tiles in four columns, each labelled with its position and its row quoted from the material in the
/// approved face; the preview is the production's own stills and frames at their recorded segment spans, video only, probed for
/// exactly one video stream and decoded with zero errors. Every hash the owner's approval file needs is printed. Exit codes:
/// 0 reviewed; 2 refused before any process; 3 a step failed, named.
/// </summary>
public sealed class ReviewGraphicsService
{
    public const int Reviewed = 0;
    public const int Refused = 2;
    public const int StepFailed = 3;

    private const int Columns = 4;
    private const int TileWidth = 480;
    private const int TileHeight = 270;
    private const int LabelHeight = 84;
    private const int LabelWidth = 58;

    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionReader _reader;
    private readonly IMediaTool _tool;
    private readonly IArtifactStore _store;
    private readonly ProductionSettings _settings;
    private readonly TextWriter _output;

    public ReviewGraphicsService(IUnitOfWork unitOfWork, IProductionReader reader, IMediaTool tool, IArtifactStore store, ProductionSettings settings, TextWriter output)
    {
        _unitOfWork = unitOfWork;
        _reader = reader;
        _tool = tool;
        _store = store;
        _settings = settings;
        _output = output;
    }

    public async Task<int> RunAsync(ReviewRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var c = CultureInfo.InvariantCulture;
        var verdict = StoreGuard.JudgeReview(request.Designation, request.DatabaseName);
        if (!verdict.Admitted)
        {
            foreach (var refusal in verdict.Refusals)
            {
                _output.WriteLine($"Refused before any process: {refusal}");
            }

            return Refused;
        }

        if (_settings.Graphics is not { } graphics)
        {
            _output.WriteLine("Refused before any process: no graphics section is configured (setting graphics)");
            return Refused;
        }

        var artifacts = await _reader.ArtifactsAsync(request.Item, request.Version, cancellationToken).ConfigureAwait(false);
        var provenance = await _reader.GraphicProvenanceAsync(request.Item, request.Version, cancellationToken).ConfigureAwait(false);
        var material = request.Loaded.Material;
        var refusals = new List<string>();
        if (artifacts.Any(a => a.Role is ArtifactRole.ContactSheet or ArtifactRole.ContactSheetFrame or ArtifactRole.GraphicPreview))
        {
            refusals.Add($"version {request.Version} already holds a review set; a review is made once per version");
        }

        var graphicsRecorded = artifacts.Where(a => a.Role is ArtifactRole.GraphicStill or ArtifactRole.GraphicMotion).ToDictionary(a => a.Implements, StringComparer.Ordinal);
        foreach (var graphic in material.Graphics.Where(g => !graphicsRecorded.ContainsKey(g.Id) || provenance.All(p => p.Position != g.Id)))
        {
            refusals.Add($"version {request.Version} records no drawn graphic with its provenance for {graphic.Id}");
        }

        // The labels are drawn in the approved face: every character is checked against its character map, read from the
        // verified face's bytes, before anything starts.
        var installation = await new RendererInstallationVerifier(graphics).VerifyAsync(cancellationToken).ConfigureAwait(false);
        refusals.AddRange(installation.Findings);
        var labels = material.Graphics.OrderBy(g => g.Id, StringComparer.Ordinal)
            .ToDictionary(g => g.Id, g => ProduceItemService.Wrap([g.Id, .. g.Lines.Take(1)], LabelWidth), StringComparer.Ordinal);
        if (installation.Regular is { } face)
        {
            refusals.AddRange(FontCoverage.Uncovered(face.Reading, labels.Select(l => ($"the sheet label of {l.Key}", l.Value.Replace("\n", string.Empty, StringComparison.Ordinal)))));
        }

        if (refusals.Count > 0)
        {
            foreach (var refusal in refusals)
            {
                _output.WriteLine($"Refused before any process: {refusal}");
            }

            return Refused;
        }

        var folder = $"{material.Item}/v{request.Version}";
        var staging = _store.BeginRun($"{folder}/review");
        await using var held = new HeldFiles();
        var tiles = new List<string>();
        var labelFiles = new List<string>();
        var finals = new List<(string Position, string Sha256, string Copy)>();
        var preview = new StringBuilder("ffconcat version 1.0\n");
        long total = 0;
        string last = string.Empty;
        try
        {
            foreach (var graphic in material.Graphics.OrderBy(g => g.Id, StringComparer.Ordinal))
            {
                var artifact = graphicsRecorded[graphic.Id];
                var record = provenance.Single(p => p.Position == graphic.Id);
                var full = _store.FullPath(artifact.RelativePath);
                string finalPath;
                if (artifact.Role == ArtifactRole.GraphicStill)
                {
                    var now = await held.HoldAsync(full, cancellationToken).ConfigureAwait(false);
                    Require(now == artifact.Sha256, $"{graphic.Id}'s still {artifact.RelativePath} hashes {now}, and {artifact.Sha256} is recorded");
                    finalPath = full;
                    preview.Append(c, $"file '{Escape(full)}'\nduration {BeatTimeline.Seconds(record.SegmentFrames, _settings.FramesPerSecond)}\n");
                    last = full;
                }
                else
                {
                    var (manifestSha, bytes) = await held.HoldBytesAsync(full, cancellationToken).ConfigureAwait(false);
                    Require(manifestSha == artifact.Sha256, $"{graphic.Id}'s frame manifest {artifact.RelativePath} hashes {manifestSha}, and {artifact.Sha256} is recorded");
                    var lines = Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(FrameLine.Parse).ToArray();
                    Require(lines.Length == record.SegmentFrames, $"{graphic.Id}'s manifest lists {lines.Length} frames and its provenance records {record.SegmentFrames}");
                    finalPath = string.Empty;
                    foreach (var line in lines)
                    {
                        var frame = Path.Combine(_store.FullPath(folder), line.RunPath);
                        var now = await held.HoldAsync(frame, cancellationToken).ConfigureAwait(false);
                        Require(now == line.Sha256, $"{graphic.Id}'s frame {line.RunPath} hashes {now}, and its manifest lists {line.Sha256}");
                        preview.Append(c, $"file '{Escape(frame)}'\nduration {BeatTimeline.Seconds(1, _settings.FramesPerSecond)}\n");
                        finalPath = frame;
                        last = frame;
                    }
                }

                total += record.SegmentFrames;
                var finalSha = await FileHashes.Sha256Async(finalPath, cancellationToken).ConfigureAwait(false);
                Require(finalSha == record.FinalFrameSha256, $"{graphic.Id}'s final frame hashes {finalSha}, and its provenance records {record.FinalFrameSha256}");

                // The final frame, copied byte for byte by the one writer beside the sheet; the copy must hash the same.
                var name = graphic.Id.ToLowerInvariant();
                await _store.CopyIntoStagingAsync(staging, finalPath, $"{name}-final.png", cancellationToken).ConfigureAwait(false);
                var copy = await _store.PromoteAsync(staging, $"{name}-final.png", $"{folder}/review/frames/{name}-final.png", cancellationToken).ConfigureAwait(false);
                Require(copy.Sha256 == finalSha, $"{graphic.Id}'s final-frame copy hashes {copy.Sha256}, and the final frame {finalSha}");
                await RecordAsync(request, ArtifactRole.ContactSheetFrame, graphic.Id, copy, null, cancellationToken).ConfigureAwait(false);
                tiles.Add($"../frames/{name}-final.png");
                await _store.WriteStagingTextAsync(staging, $"label-{name}.txt", labels[graphic.Id], cancellationToken).ConfigureAwait(false);
                labelFiles.Add($"label-{name}.txt");
                finals.Add((graphic.Id, finalSha, copy.RelativePath));
            }

            preview.Append(c, $"file '{Escape(last)}'\n");

            // The sheet, composed from the byte copies, which are re-hashed and held while the media tool reads them.
            foreach (var (position, sha, copy) in finals)
            {
                var now = await held.HoldAsync(_store.FullPath(copy), cancellationToken).ConfigureAwait(false);
                Require(now == sha, $"{position}'s final-frame copy changed before the sheet read it: {sha} copied and {now} now");
            }

            await _store.CopyIntoStagingAsync(staging, graphics.RegularFace, "face.ttf", cancellationToken).ConfigureAwait(false);
            var stagedFace = await held.HoldAsync(Path.Combine(staging, "face.ttf"), cancellationToken).ConfigureAwait(false);
            Require(stagedFace == installation.Regular!.Sha256, $"the face staged for the labels hashes {stagedFace}, and the verified face {installation.Regular.Sha256}");
            await _tool.ComposeSheetAsync(new SheetSpecification(staging, tiles, labelFiles, "face.ttf", "contact-sheet.png", Columns, TileWidth, TileHeight, LabelHeight), cancellationToken).ConfigureAwait(false);
            var sheet = await _store.PromoteAsync(staging, "contact-sheet.png", $"{folder}/review/contact-sheet.png", cancellationToken).ConfigureAwait(false);
            await RecordAsync(request, ArtifactRole.ContactSheet, "contact sheet of " + string.Join(", ", finals.Select(f => f.Position)), sheet, null, cancellationToken).ConfigureAwait(false);

            // The preview: the production's own stills and frames at their recorded spans, video only.
            await _store.WriteStagingTextAsync(staging, "preview-segments.txt", preview.ToString(), cancellationToken).ConfigureAwait(false);
            await _tool.RenderPreviewAsync(new PreviewSpecification(staging, "preview-segments.txt", "graphics-preview.mp4", total), cancellationToken).ConfigureAwait(false);
            var video = await _store.PromoteAsync(staging, "graphics-preview.mp4", $"{folder}/review/graphics-preview.mp4", cancellationToken).ConfigureAwait(false);
            var probe = await _tool.ProbeAsync(video.FullPath, cancellationToken).ConfigureAwait(false);
            var decode = await _tool.DecodeAsync(video.FullPath, cancellationToken).ConfigureAwait(false);
            Require(probe.Count("video") == 1 && probe.StreamKinds.Count == 1, $"the preview carries streams [{string.Join(", ", probe.StreamKinds)}], not exactly one video stream");
            Require(decode.Errors == 0, $"decoding the preview reported {decode.Errors} error(s), the first: {decode.FirstError}");
            await RecordAsync(request, ArtifactRole.GraphicPreview, "preview of every graphic at its production span", video, probe.Duration, cancellationToken).ConfigureAwait(false);

            _output.WriteLine($"Contact sheet: {sheet.FullPath}");
            _output.WriteLine(string.Create(c, $"  {sheet.Length:N0} bytes, sha256 {sheet.Sha256}; {finals.Count} tiles in {Columns} columns, each a scaled view of its graphic's full-size final frame, which is stored beside the sheet and named below"));
            _output.WriteLine($"Preview: {video.FullPath}");
            _output.WriteLine(string.Create(c, $"  {video.Length:N0} bytes, sha256 {video.Sha256}; one video stream, no audio, decoded end to end with 0 errors; {total:N0} frames, {probe.Duration.TotalSeconds:0.000} s MEASURED from the file; each graphic at its production span"));
            _output.WriteLine("For the owner's approval file, every graphic as reviewed:");
            foreach (var (position, sha, copy) in finals)
            {
                var record = provenance.Single(p => p.Position == position);
                _output.WriteLine(string.Create(c,
                    $"  {position}: {record.Kind}, specification sha256 {record.SpecificationSha256}, final frame sha256 {sha}{(record.Kind == GraphicKind.Still ? $", still sha256 {sha}" : string.Empty)}, "
                    + $"render {record.RenderSeconds:0.0} s observed on this machine, {record.Sameness}: {record.SamenessStatement}; copy {copy}"));
            }

            _output.WriteLine($"  style sha256 {provenance[0].StyleSha256}; manifests {provenance[0].Manifests}");
            _output.WriteLine("Nothing was published or uploaded; no model call was made; booked metered spend USD 0.00; local compute unmeasured.");
            return Reviewed;
        }
        catch (ReviewStepFailedException failed)
        {
            _output.WriteLine($"Review step failed: {failed.Message}");
            return StepFailed;
        }
        catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException)
        {
            _output.WriteLine($"Review step failed: {failure.Message}");
            return StepFailed;
        }
    }

    private static void Require(bool holds, string otherwise)
    {
        if (!holds)
        {
            throw new ReviewStepFailedException(otherwise);
        }
    }

    /// <summary>A path as the segment list's quoted form needs it: forward slashes, a single quote escaped.</summary>
    private static string Escape(string path) => path.Replace('\\', '/').Replace("'", "'\\''", StringComparison.Ordinal);

    private async Task RecordAsync(ReviewRequest request, ArtifactRole role, string implements, StoredFile stored, TimeSpan? duration, CancellationToken cancellationToken)
    {
        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        var record = await transaction.Production.RecordArtifactAsync(
            new ArtifactRecord
            {
                Item = request.Item,
                Version = request.Version,
                Stage = ProductionStage.Design,
                Role = role,
                Implements = implements,
                RelativePath = stored.RelativePath,
                Length = stored.Length,
                Sha256 = stored.Sha256,
                MeasuredDuration = duration,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(new AuditEntryDraft
        {
            Actor = WorkforceRole.Producer.ToString(),
            Action = "production.artifact-recorded",
            Subject = $"item:{request.Item} version:{request.Version}",
            Reason = $"{role} recorded after it was written, promoted and re-read, for the owner's contact-sheet review",
            InputsReference = $"production:{request.Item}:{request.Version}",
            OutputsReference = $"artifact:{stored.RelativePath} sha256:{stored.Sha256}",
            Decision = "production.artifact-recorded",
            CostReference = "none",
            Risk = "none",
            RetentionClass = RetentionClass.OperationalRecord,
        }, cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        _output.WriteLine(string.Create(CultureInfo.InvariantCulture, $"  artifact {stored.RelativePath}: {stored.Length:N0} bytes, sha256 {stored.Sha256}, recorded at {record.RecordedAt:O}"));
    }

    private sealed class ReviewStepFailedException(string reason) : Exception(reason);
}
