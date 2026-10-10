using System.Globalization;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Work;

namespace MediaCompany.Production;

/// <summary>What one registration is asked to register: the item, its loaded material, the folder and the release as given.</summary>
public sealed record RegistrationRequest
{
    public required ItemId Item { get; init; }

    public required LoadedItem Loaded { get; init; }

    /// <summary>The operator's folder holding one file per beat, named by the two-digit beat number.</summary>
    public required string RecordingsFolder { get; init; }

    public string? PerformerName { get; init; }

    public string? ReleaseDocumentReference { get; init; }

    public DateOnly? ReleaseDocumentDate { get; init; }

    /// <summary>The release document's path, outside the repository; it is read once to hash and copied nowhere.</summary>
    public string? ReleaseDocumentPath { get; init; }

    public string? TrainingTerm { get; init; }
}

/// <summary>How a registration ended: 0 registered, 2 refused before anything was recorded, 3 a tool or store failure named.</summary>
public sealed record RegistrationResult(int ExitCode, RecordingRegistration? Registration)
{
    public const int Registered = 0;
    public const int Refused = 2;
    public const int Failed = 3;
}

/// <summary>
/// RECORDING REGISTRATION (the own-voice change, decision D-004 of its design). One folder is registered against the item and
/// package version the material names: exactly one file per beat, named by its two-digit beat number. Each accepted file is
/// COPIED BY THE ONE WRITER into a registration folder under the output root, promoted once and hashed over the STORED bytes;
/// the stored copy is probed (exactly one audio stream and no video stream), decoded end to end (zero errors and no corrupt
/// input reported) and measured from decoded audio, then RE-HASHED, and a change since promotion refuses. Only after every
/// file passes is the registration recorded, on one short transaction: each beat's stored path, SHA-256, length and measured
/// properties, the performer's name and the release, each release field recorded ABSENT when not given. The release
/// document's hash is read from outside the repository and the document is copied nowhere. Nothing here holds a transaction
/// while a file is copied, hashed, probed or decoded.
/// </summary>
public sealed class RecordingRegistrar
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductionReader _reader;
    private readonly IMediaTool _tool;
    private readonly IArtifactStore _store;
    private readonly ProductionSettings _settings;
    private readonly TextWriter _output;

    public RecordingRegistrar(IUnitOfWork unitOfWork, IProductionReader reader, IMediaTool tool, IArtifactStore store, ProductionSettings settings, TextWriter output)
    {
        _unitOfWork = unitOfWork;
        _reader = reader;
        _tool = tool;
        _store = store;
        _settings = settings;
        _output = output;
    }

    public async Task<RegistrationResult> RegisterAsync(RegistrationRequest request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var material = request.Loaded.Material;
        var refusals = new List<string>();

        // The set, by name, before anything is copied.
        RecordingSetCheck.Verdict verdict;
        if (!Directory.Exists(request.RecordingsFolder))
        {
            refusals.Add($"the recordings folder {request.RecordingsFolder} does not exist");
            verdict = new RecordingSetCheck.Verdict(new Dictionary<int, string>(), refusals);
        }
        else
        {
            foreach (var folder in Directory.GetDirectories(request.RecordingsFolder))
            {
                refusals.Add($"the recordings folder holds a folder {Path.GetFileName(folder)}; only one file per beat is registered");
            }

            verdict = RecordingSetCheck.Check(Directory.GetFiles(request.RecordingsFolder).Select(Path.GetFileName).ToArray()!, material.Beats.Select(b => b.Number).ToArray());
            refusals.AddRange(verdict.Refusals);
        }

        // The release document: read from outside the repository to hash, copied nowhere.
        string? documentHash = null;
        if (request.ReleaseDocumentPath is { } document)
        {
            var full = Path.GetFullPath(document);
            if (ArtifactWriter.Contains(ArtifactWriter.Resolve(_settings.RepositoryRoot), ArtifactWriter.Resolve(full)))
            {
                refusals.Add($"the release document {full} lies inside the repository; a release document is kept outside it and only its hash is recorded");
            }
            else if (!File.Exists(full))
            {
                refusals.Add($"the release document {full} does not exist");
            }
            else
            {
                documentHash = await FileHashes.Sha256Async(full, cancellationToken).ConfigureAwait(false);
            }
        }

        var package = await _reader.PackageAsync(request.Item, new ItemVersion(material.PackageVersion), cancellationToken).ConfigureAwait(false);
        if (package is null)
        {
            refusals.Add($"item {request.Item} is not recorded in this store; run the preparation first");
        }

        if (refusals.Count > 0)
        {
            return Refuse(refusals);
        }

        var release = new ReleaseRecord
        {
            DocumentReference = Given(request.ReleaseDocumentReference),
            DocumentDate = request.ReleaseDocumentDate,
            PerformerName = Given(request.PerformerName),
            DocumentSha256 = documentHash,
            TrainingTerm = Given(request.TrainingTerm),
        };

        var id = Guid.NewGuid();
        var folderPath = $"{material.Item}/registrations/{id:N}";
        var beats = new List<RegisteredBeatFile>();
        try
        {
            var staging = _store.BeginRun(folderPath);
            foreach (var (beat, name) in verdict.Accepted.OrderBy(a => a.Key))
            {
                var stored = $"{beat:00}{Path.GetExtension(name).ToLowerInvariant()}";
                await _store.CopyIntoStagingAsync(staging, Path.Combine(request.RecordingsFolder, name), stored, cancellationToken).ConfigureAwait(false);
                var file = await _store.PromoteAsync(staging, stored, $"{folderPath}/{stored}", cancellationToken).ConfigureAwait(false);

                var probe = await _tool.ProbeOrNullAsync(file.FullPath, cancellationToken).ConfigureAwait(false);
                if (probe is null || probe.Count("audio") != 1 || probe.Count("video") != 0)
                {
                    refusals.Add($"beat {beat:00} ({name}) {(probe is null ? "could not be probed as media" : $"carries streams [{string.Join(", ", probe.StreamKinds)}], not exactly one audio stream and no video stream")}");
                    continue;
                }

                var decode = await _tool.DecodeAsync(file.FullPath, cancellationToken).ConfigureAwait(false);
                var measured = await _tool.MeasureAudioAsync(file.FullPath, cancellationToken).ConfigureAwait(false);
                if (decode.Errors != 0 || measured.DecodeFindings.Count > 0)
                {
                    refusals.Add($"beat {beat:00} ({name}) does not decode end to end cleanly: "
                        + string.Join("; ", (decode.FirstError is { } e ? new[] { e } : []).Concat(measured.DecodeFindings).Take(3)));
                    continue;
                }

                // The bytes measured are the bytes stored: re-hashed after measuring.
                var after = await FileHashes.Sha256Async(file.FullPath, cancellationToken).ConfigureAwait(false);
                if (!string.Equals(after, file.Sha256, StringComparison.Ordinal))
                {
                    refusals.Add($"beat {beat:00}'s stored copy {file.RelativePath} changed while it was measured: sha256 {file.Sha256} at promotion and {after} after measuring");
                    continue;
                }

                beats.Add(new RegisteredBeatFile { Beat = beat, StoredPath = file.RelativePath, Length = file.Length, Sha256 = file.Sha256, Measured = measured });
                _output.WriteLine(string.Create(CultureInfo.InvariantCulture,
                    $"  beat {beat:00}: {name} stored as {file.RelativePath}, {file.Length:N0} bytes, sha256 {file.Sha256}; {measured.Container}/{measured.Codec}, {measured.Format}; "
                    + $"{measured.DescribeDuration()}; {measured.DescribeLoudness()}"));
            }
        }
        catch (Exception failure) when (failure is MediaToolBoundExceededException or MediaToolFailedException or IOException)
        {
            _output.WriteLine($"Registration failed, nothing was recorded: {failure.Message}");
            return new RegistrationResult(RegistrationResult.Failed, null);
        }

        refusals.AddRange(RecordingSetCheck.MixedFormats(beats.Select(b => (b.Beat, b.Measured)).ToArray()));
        if (refusals.Count > 0)
        {
            return Refuse(refusals);
        }

        RecordingRegistration recorded;
        try
        {
            await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
            recorded = await transaction.Production.RecordRegistrationAsync(new RecordingRegistration
            {
                Id = id,
                Item = request.Item,
                PackageVersion = new ItemVersion(material.PackageVersion),
                Release = release,
                Beats = beats,
            }, cancellationToken).ConfigureAwait(false);
            await transaction.Audit.AppendAsync(new AuditEntryDraft
            {
                Actor = WorkforceRole.Producer.ToString(),
                Action = "production.recording-registered",
                Subject = $"item:{request.Item} version:{material.PackageVersion}",
                Reason = $"recording registration {id} of {beats.Count} beat files, each stored, decoded end to end and measured before it was recorded",
                InputsReference = $"registration:{id}",
                OutputsReference = $"artifacts:{folderPath}",
                Decision = "production.recording-registered",
                CostReference = "none",
                Risk = "none",
                RetentionClass = RetentionClass.OperationalRecord,
            }, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception failure) when (failure is not OperationCanceledException)
        {
            _output.WriteLine($"Registration failed at the store, nothing was recorded ({failure.GetType().Name}): {failure.Message}");
            return new RegistrationResult(RegistrationResult.Failed, null);
        }

        _output.WriteLine($"Registered recording {recorded.Id} for item {material.Item} version {material.PackageVersion} at {recorded.RecordedAt:O} (the datastore's instant): {beats.Count} beat files.");
        _output.WriteLine($"  {release.Describe()}");
        _output.WriteLine(release.Complete
            ? "  the release is complete: a production may narrate from this recording"
            : $"  the release is NOT complete, so a production passes this recording over; absent: {string.Join(", ", release.AbsentFields)}");
        return new RegistrationResult(RegistrationResult.Registered, recorded);
    }

    /// <summary>
    /// The state of the latest registration, re-hashing EVERY stored copy (files only, no process started): absent, an
    /// incomplete release naming each absent field, verified, or a released registration whose file is missing or changed,
    /// naming the file and both hashes.
    /// </summary>
    public static async Task<RegistrationReading> ReadAsync(RecordingRegistration? latest, string outputRoot, CancellationToken cancellationToken)
    {
        if (latest is null)
        {
            return RegistrationReading.None;
        }

        if (!latest.Release.Complete)
        {
            return new RegistrationReading(RegistrationState.ReleaseIncomplete, latest.Id, latest.Release.AbsentFields);
        }

        var findings = new List<string>();
        foreach (var beat in latest.Beats.OrderBy(b => b.Beat))
        {
            var path = ArtifactWriter.StoredPath(outputRoot, beat.StoredPath);
            if (!File.Exists(path))
            {
                findings.Add($"beat {beat.Beat:00}'s registered file {path} is missing (registered sha256 {beat.Sha256})");
                continue;
            }

            var now = await FileHashes.Sha256Async(path, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(now, beat.Sha256, StringComparison.Ordinal))
            {
                findings.Add($"beat {beat.Beat:00}'s registered file {path} changed after registration: sha256 {beat.Sha256} registered and {now} now");
            }
        }

        return findings.Count == 0
            ? new RegistrationReading(RegistrationState.Verified, latest.Id, [])
            : new RegistrationReading(RegistrationState.FilesChanged, latest.Id, findings);
    }

    private RegistrationResult Refuse(IReadOnlyList<string> refusals)
    {
        foreach (var refusal in refusals)
        {
            _output.WriteLine($"Refused, nothing was recorded: {refusal}");
        }

        return new RegistrationResult(RegistrationResult.Refused, null);
    }

    private static string? Given(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

/// <summary>A probe that names a file that is not media as such, instead of failing the registration as a tool failure.</summary>
internal static class MediaToolProbing
{
    public static async Task<MediaProbe?> ProbeOrNullAsync(this IMediaTool tool, string file, CancellationToken cancellationToken)
    {
        try
        {
            return await tool.ProbeAsync(file, cancellationToken).ConfigureAwait(false);
        }
        catch (MediaToolFailedException)
        {
            return null;
        }
    }
}
