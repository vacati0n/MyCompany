using System.Buffers.Text;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediaCompany.Production;

/// <summary>One verified file: what it is, where, the SHA-256 configured for it and the one computed over its bytes in this run.</summary>
public sealed record VerifiedFile(string Name, string Path, string Expected, string Computed)
{
    public bool Matches => string.Equals(Expected, Computed, StringComparison.OrdinalIgnoreCase);

    public string Describe() => Matches
        ? $"{Name} {Path}: sha256 {Computed} (computed over the file in this run; equals the configured expected value)"
        : $"{Name} {Path}: sha256 {Computed} computed over the file in this run, and {Expected} configured as expected";
}

/// <summary>
/// What a verification of the in-house model's installation found (the own-voice change, decision D-006 of its design).
/// Every value here is read from a file whose bytes were hashed and matched in the same pass, or is a finding naming why not.
/// </summary>
public sealed record InstallationReport
{
    public required IReadOnlyList<string> Findings { get; init; }

    public required IReadOnlyList<VerifiedFile> Files { get; init; }

    /// <summary>The runtime's name, version and licence, read from its package metadata (a verified record entry).</summary>
    public string? RuntimeName { get; init; }

    public string? RuntimeVersion { get; init; }

    public string? RuntimeLicence { get; init; }

    /// <summary>The voice's name composed from the verified configuration's language code, dataset and quality.</summary>
    public string? VoiceName { get; init; }

    public int? VoiceSampleRate { get; init; }

    /// <summary>The voice configuration's own inference values, each by name, as the file states them.</summary>
    public IReadOnlyDictionary<string, decimal> VoiceDefaults { get; init; } = new Dictionary<string, decimal>();

    /// <summary>The dataset's licence as the verified model card states it, or that it states none.</summary>
    public string? DatasetLicence { get; init; }

    /// <summary>The hashed entries of every verified record that matched.</summary>
    public int RecordEntriesVerified { get; init; }

    /// <summary>The entries the packages list without a hash (compiled caches and the records themselves); not verifiable.</summary>
    public int RecordEntriesWithoutHash { get; init; }

    public bool Verified => Findings.Count == 0;
}

/// <summary>
/// THE INSTALLATION VERIFIER (the own-voice change, decision D-006 of its design). It READS FILES ONLY and starts no
/// process. It refuses, naming the file, a configuration path that is not the model path plus ".json" (the runtime loads
/// that one whatever its option says), any configured file inside the repository, a missing file, and a differing hash
/// with both values; it verifies every hashed entry of the runtime's and each dependency's installed-files record against
/// the file it names, and reads the runtime's name, version and licence from the package metadata, the voice's identity
/// from the verified configuration and the dataset's licence from the verified model card, each from the very bytes it
/// hashed. A value it cannot read is reported as such, never filled in.
///
/// Before each part, <see cref="HoldAsync"/> opens every core file READ-ONLY, sharing read only (so it cannot be written,
/// replaced, renamed or deleted while held), hashes each through its open handle and keeps the handles until the part's
/// process has exited: the bytes hashed are the bytes loaded.
/// </summary>
public sealed class InstallationVerifier
{
    private readonly InHouseModelSettings _settings;
    private readonly string _repositoryRoot;

    public InstallationVerifier(InHouseModelSettings settings, string repositoryRoot)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        _repositoryRoot = ArtifactWriter.Resolve(repositoryRoot);
    }

    /// <summary>
    /// The whole verification, files only: every configured file, the base interpreter the environment names, and, where
    /// <paramref name="entries"/> is set, every hashed entry of every configured installed-files record.
    /// </summary>
    public async Task<InstallationReport> VerifyAsync(bool entries, CancellationToken cancellationToken)
    {
        var findings = new List<string>();
        var files = new List<VerifiedFile>();

        // The runtime reads the configuration at the model path plus ".json", whatever its configuration option says.
        var expectedConfiguration = _settings.Model.Path + ".json";
        if (!string.Equals(Full(_settings.Configuration.Path), Full(expectedConfiguration), StringComparison.OrdinalIgnoreCase))
        {
            findings.Add($"the configuration file {_settings.Configuration.Path} is not the model path plus \".json\" ({expectedConfiguration}), "
                + "which is the file the runtime loads; the hashed configuration would not be the loaded one");
        }

        byte[]? environment = null;
        byte[]? configuration = null;
        byte[]? card = null;
        foreach (var (name, expected) in Core())
        {
            if (Placed(name, expected.Path, findings) is not { } path)
            {
                continue;
            }

            // Small files are read once: the buffer hashed is the buffer parsed. The model is hashed as a stream.
            var small = !ReferenceEquals(expected, _settings.Model) && !ReferenceEquals(expected, _settings.Interpreter);
            byte[]? bytes = null;
            string computed;
            if (small)
            {
                bytes = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                computed = Convert.ToHexStringLower(SHA256.HashData(bytes));
            }
            else
            {
                computed = await FileHashes.Sha256Async(path, cancellationToken).ConfigureAwait(false);
            }

            var verified = new VerifiedFile(name, path, expected.Sha256, computed);
            files.Add(verified);
            if (!verified.Matches)
            {
                findings.Add($"the {name} changed: {verified.Describe()}");
                continue;
            }

            if (ReferenceEquals(expected, _settings.EnvironmentConfiguration))
            {
                environment = bytes;
            }
            else if (ReferenceEquals(expected, _settings.Configuration))
            {
                configuration = bytes;
            }
            else if (ReferenceEquals(expected, _settings.ModelCard))
            {
                card = bytes;
            }
        }

        // The base interpreter the environment's launchers start, named by the VERIFIED environment configuration.
        if (environment is not null)
        {
            if (BaseInterpreter(environment) is not { } basePath)
            {
                findings.Add($"the environment configuration {_settings.EnvironmentConfiguration.Path} names no base interpreter (no executable entry)");
            }
            else if (Placed("base interpreter", basePath, findings) is { } placed)
            {
                var computed = await FileHashes.Sha256Async(placed, cancellationToken).ConfigureAwait(false);
                var verified = new VerifiedFile("base interpreter", placed, _settings.BaseInterpreterSha256, computed);
                files.Add(verified);
                if (!verified.Matches)
                {
                    findings.Add($"the base interpreter changed: {verified.Describe()}");
                }
            }
        }

        var report = new InstallationReport { Findings = findings, Files = files };
        if (configuration is not null)
        {
            report = report with { VoiceName = null };
            report = Voice(configuration, report, findings);
        }

        if (card is not null)
        {
            report = report with { DatasetLicence = DatasetLicenceOf(card) };
        }

        if (entries)
        {
            var verifiedEntries = 0;
            var withoutHash = 0;
            foreach (var record in Records())
            {
                if (files.FirstOrDefault(f => ReferenceEquals(f.Path, record.Path) || string.Equals(f.Path, Full(record.Path), StringComparison.OrdinalIgnoreCase)) is not { Matches: true })
                {
                    continue;
                }

                var (ok, without) = await VerifyEntriesAsync(Full(record.Path), findings, cancellationToken).ConfigureAwait(false);
                verifiedEntries += ok;
                withoutHash += without;
            }

            report = report with { RecordEntriesVerified = verifiedEntries, RecordEntriesWithoutHash = withoutHash };
            report = await MetadataAsync(report, findings, cancellationToken).ConfigureAwait(false);
        }

        return report with { Findings = findings.ToArray() };
    }

    /// <summary>
    /// Opens every core file and the base interpreter READ-ONLY, sharing read only, hashes each through its open handle and
    /// compares; the returned hold keeps every handle until it is disposed, after the part's process exited. A difference
    /// refuses, naming the file and both hashes, and releases every handle.
    /// </summary>
    public async Task<HeldInstallation> HoldAsync(CancellationToken cancellationToken)
    {
        var held = new HeldInstallation();
        try
        {
            var findings = new List<string>();
            foreach (var (name, expected) in Core())
            {
                if (Placed(name, expected.Path, findings) is not { } path)
                {
                    continue;
                }

                var stream = File.OpenRead(path);
                held.Streams.Add(stream);
                var bytes = ReferenceEquals(expected, _settings.EnvironmentConfiguration) ? await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false) : null;
                var computed = bytes is not null
                    ? Convert.ToHexStringLower(SHA256.HashData(bytes))
                    : Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
                var verified = new VerifiedFile(name, path, expected.Sha256, computed);
                held.Files.Add(verified);
                if (!verified.Matches)
                {
                    findings.Add($"the {name} changed before the part's load: {verified.Describe()}");
                }
                else if (bytes is not null && BaseInterpreter(bytes) is { } basePath && Placed("base interpreter", basePath, findings) is { } placed)
                {
                    var baseStream = File.OpenRead(placed);
                    held.Streams.Add(baseStream);
                    var baseVerified = new VerifiedFile("base interpreter", placed, _settings.BaseInterpreterSha256,
                        Convert.ToHexStringLower(await SHA256.HashDataAsync(baseStream, cancellationToken).ConfigureAwait(false)));
                    held.Files.Add(baseVerified);
                    if (!baseVerified.Matches)
                    {
                        findings.Add($"the base interpreter changed before the part's load: {baseVerified.Describe()}");
                    }
                }
            }

            if (findings.Count > 0)
            {
                throw new InstallationRefusedException(findings);
            }

            return held;
        }
        catch
        {
            await held.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Every configured core file with what it is, in a fixed order.</summary>
    private IEnumerable<(string Name, ExpectedFile File)> Core()
    {
        yield return ("interpreter", _settings.Interpreter);
        yield return ("environment configuration", _settings.EnvironmentConfiguration);
        yield return ("voice model", _settings.Model);
        yield return ("voice configuration", _settings.Configuration);
        yield return ("model card", _settings.ModelCard);
        yield return ("runtime record", _settings.RuntimeRecord);
        for (var i = 0; i < _settings.DependencyRecords.Count; i++)
        {
            yield return ($"dependency record {i + 1}", _settings.DependencyRecords[i]);
        }
    }

    private IEnumerable<ExpectedFile> Records() => _settings.DependencyRecords.Prepend(_settings.RuntimeRecord);

    /// <summary>The full path of a configured file, refused (as a finding) where it is missing or lies inside the repository.</summary>
    private string? Placed(string name, string configured, List<string> findings)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            findings.Add($"the {name} is not configured");
            return null;
        }

        var full = Full(configured);
        if (ArtifactWriter.Contains(_repositoryRoot, ArtifactWriter.Resolve(full)))
        {
            findings.Add($"the {name} {full} lies inside the repository root {_repositoryRoot}; the model, its runtime and its files are kept outside the repository");
            return null;
        }

        if (!File.Exists(full))
        {
            findings.Add($"the {name} {full} does not exist");
            return null;
        }

        return full;
    }

    private static string Full(string path) => System.IO.Path.GetFullPath(path);

    /// <summary>The base interpreter an environment configuration names in its executable entry.</summary>
    internal static string? BaseInterpreter(byte[] environment) =>
        Encoding.UTF8.GetString(environment).Split('\n')
            .Select(l => l.Split('=', 2))
            .Where(p => p.Length == 2 && string.Equals(p[0].Trim(), "executable", StringComparison.OrdinalIgnoreCase))
            .Select(p => p[1].Trim())
            .FirstOrDefault(v => v.Length > 0);

    /// <summary>The voice's identity and inference values, read from the verified configuration bytes.</summary>
    private static InstallationReport Voice(byte[] configuration, InstallationReport report, List<string> findings)
    {
        try
        {
            using var json = JsonDocument.Parse(configuration);
            var root = json.RootElement;
            string? Text(params string[] path)
            {
                var at = root;
                foreach (var key in path)
                {
                    if (at.ValueKind != JsonValueKind.Object || !at.TryGetProperty(key, out at))
                    {
                        return null;
                    }
                }

                return at.ValueKind switch
                {
                    JsonValueKind.String => at.GetString(),
                    JsonValueKind.Number => at.GetRawText(),
                    _ => null,
                };
            }

            var defaults = new Dictionary<string, decimal>(StringComparer.Ordinal);
            foreach (var key in new[] { "length_scale", "noise_scale", "noise_w" })
            {
                if (Text("inference", key) is { } value && decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
                {
                    defaults[key] = number;
                }
            }

            var (code, dataset, quality) = (Text("language", "code"), Text("dataset"), Text("audio", "quality"));
            return report with
            {
                VoiceName = code is null || dataset is null || quality is null ? null : $"{code}-{dataset}-{quality}",
                VoiceSampleRate = int.TryParse(Text("audio", "sample_rate"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var rate) ? rate : null,
                VoiceDefaults = defaults,
            };
        }
        catch (JsonException unreadable)
        {
            findings.Add($"the voice configuration does not read as JSON: {unreadable.Message}");
            return report;
        }
    }

    /// <summary>The licence the verified model card states under its dataset heading, or that it states none.</summary>
    private static string DatasetLicenceOf(byte[] card)
    {
        var inDataset = false;
        foreach (var raw in Encoding.UTF8.GetString(card).Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith('#'))
            {
                inDataset = line.TrimStart('#').Trim().Equals("Dataset", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (inDataset && line.TrimStart('*', '-', ' ').StartsWith("License:", StringComparison.OrdinalIgnoreCase))
            {
                return line.TrimStart('*', '-', ' ')["License:".Length..].Trim() + " (as the installed model card states for its dataset)";
            }
        }

        return "not stated in the installed model card";
    }

    /// <summary>
    /// Verifies every hashed entry of one installed-files record against the file it names, relative to the folder that
    /// holds the record's package-information folder. A missing or changed file is a finding naming it and both hashes.
    /// </summary>
    private static async Task<(int Verified, int WithoutHash)> VerifyEntriesAsync(string record, List<string> findings, CancellationToken cancellationToken)
    {
        var packages = Directory.GetParent(Path.GetDirectoryName(record)!)!.FullName;
        var verified = 0;
        var without = 0;
        foreach (var line in (await File.ReadAllTextAsync(record, Encoding.UTF8, cancellationToken).ConfigureAwait(false)).Split('\n'))
        {
            var fields = Csv(line.TrimEnd('\r'));
            if (fields.Count < 2 || fields[0].Length == 0)
            {
                continue;
            }

            if (!fields[1].StartsWith("sha256=", StringComparison.Ordinal))
            {
                without++;
                continue;
            }

            var listed = Hex(fields[1]["sha256=".Length..]);
            var file = Path.GetFullPath(Path.Combine(packages, fields[0]));
            if (!File.Exists(file))
            {
                findings.Add($"the file {file} listed in {record} does not exist");
                continue;
            }

            var computed = await FileHashes.Sha256Async(file, cancellationToken).ConfigureAwait(false);
            if (!string.Equals(listed, computed, StringComparison.OrdinalIgnoreCase))
            {
                findings.Add($"the file {file} listed in {record} changed: sha256 {computed} computed in this run, and {listed ?? fields[1]} recorded by the package");
                continue;
            }

            verified++;
        }

        return (verified, without);
    }

    /// <summary>
    /// The runtime's name, version and licence, read from its package metadata, after its bytes hash to the value the
    /// verified runtime record lists for it; otherwise a finding, and the three read not stated.
    /// </summary>
    private async Task<InstallationReport> MetadataAsync(InstallationReport report, List<string> findings, CancellationToken cancellationToken)
    {
        var record = Full(_settings.RuntimeRecord.Path);
        if (!report.Files.Any(f => string.Equals(f.Path, record, StringComparison.OrdinalIgnoreCase) && f.Matches))
        {
            return report;
        }

        var folder = Path.GetDirectoryName(record)!;
        var metadata = Path.Combine(folder, "METADATA");
        var entry = Path.GetFileName(folder) + "/METADATA";
        var listed = (await File.ReadAllTextAsync(record, Encoding.UTF8, cancellationToken).ConfigureAwait(false)).Split('\n')
            .Select(l => Csv(l.TrimEnd('\r')))
            .FirstOrDefault(f => f.Count >= 2 && string.Equals(f[0], entry, StringComparison.Ordinal) && f[1].StartsWith("sha256=", StringComparison.Ordinal));
        if (listed is null || !File.Exists(metadata))
        {
            findings.Add($"the runtime's package metadata {metadata} is not a hashed entry of {record}, so its name, version and licence are not read");
            return report;
        }

        var bytes = await File.ReadAllBytesAsync(metadata, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(Hex(listed[1]["sha256=".Length..]), Convert.ToHexStringLower(SHA256.HashData(bytes)), StringComparison.OrdinalIgnoreCase))
        {
            findings.Add($"the runtime's package metadata {metadata} does not hash to the value {record} lists for it");
            return report;
        }

        string? Field(string name) => Encoding.UTF8.GetString(bytes).Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .TakeWhile(l => l.Length > 0)
            .Where(l => l.StartsWith(name + ":", StringComparison.Ordinal))
            .Select(l => l[(name.Length + 1)..].Trim())
            .FirstOrDefault();

        return report with
        {
            RuntimeName = Field("Name"),
            RuntimeVersion = Field("Version"),
            RuntimeLicence = Field("License") ?? Field("License-Expression"),
        };
    }

    /// <summary>A record's unpadded URL-safe base64 SHA-256 as lowercase hexadecimal, or null where it does not decode.</summary>
    private static string? Hex(string urlSafe)
    {
        try
        {
            return Convert.ToHexStringLower(Base64Url.DecodeFromChars(urlSafe));
        }
        catch (FormatException)
        {
            return null;
        }
    }

    /// <summary>One line of a record, split as comma-separated values with double-quoted fields.</summary>
    private static List<string> Csv(string line)
    {
        var fields = new List<string>();
        var current = new StringBuilder();
        var quoted = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (quoted)
            {
                if (ch == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else if (ch == '"')
                {
                    quoted = false;
                }
                else
                {
                    current.Append(ch);
                }
            }
            else if (ch == '"')
            {
                quoted = true;
            }
            else if (ch == ',')
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        fields.Add(current.ToString());
        return fields;
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }
}

/// <summary>
/// The installation's core files held open READ-ONLY, sharing read only, from their hash until the part's process exits
/// (the own-voice change, decision D-006 of its design): while held, none can be written, replaced, renamed or deleted.
/// </summary>
public sealed class HeldInstallation : IAsyncDisposable
{
    internal List<FileStream> Streams { get; } = [];

    /// <summary>Every held file with the SHA-256 computed through its open handle.</summary>
    public List<VerifiedFile> Files { get; } = [];

    public string Describe() => string.Join("; ", Files.Select(f => $"{f.Name} {f.Path} sha256 {f.Computed}"));

    public async ValueTask DisposeAsync()
    {
        foreach (var stream in Streams)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }

        Streams.Clear();
    }
}

/// <summary>The in-house model's installation does not verify; every finding is named.</summary>
public sealed class InstallationRefusedException(IReadOnlyList<string> findings)
    : Exception("the in-house model's installation is refused: " + string.Join("; ", findings))
{
    public IReadOnlyList<string> Findings { get; } = findings;
}

/// <summary>SHA-256 over a file's bytes, read through a read-only handle that shares read only.</summary>
internal static class FileHashes
{
    public static async Task<string> Sha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }
}
