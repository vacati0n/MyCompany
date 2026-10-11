using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediaCompany.Deterministic.Production;

namespace MediaCompany.Production;

/// <summary>One verified manifest: its name, the SHA-256 computed over its bytes in this run, and the files it lists that verified.</summary>
public sealed record VerifiedManifest(string Name, string Path, string Sha256, int Files);

/// <summary>One declared face: its file, the SHA-256 of its verified bytes, and its identity and character map read from those bytes.</summary>
public sealed record VerifiedFace(string Path, string Sha256, FaceReading Reading)
{
    public string Describe() => $"{Reading.Family} {Reading.Subfamily}, {Reading.Version}, sha256 {Sha256}";
}

/// <summary>
/// What a verification of the graphics installation found (the graphics change, decision D-003 of its design). Every identity
/// here is read from bytes whose hash matched in the same pass; nothing is copied from configuration.
/// </summary>
public sealed record RendererReport
{
    public required IReadOnlyList<string> Findings { get; init; }

    public required IReadOnlyList<VerifiedManifest> Manifests { get; init; }

    public string? RendererName { get; init; }

    public string? RendererVersion { get; init; }

    /// <summary>The bundler's and the frame renderer's package names and versions, read from their verified package files.</summary>
    public string? RendererPackages { get; init; }

    public string? NodeSha256 { get; init; }

    public string? NodeVersion { get; init; }

    public string? BrowserSha256 { get; init; }

    public string? BrowserVersion { get; init; }

    public VerifiedFace? Regular { get; init; }

    public VerifiedFace? Italic { get; init; }

    public int FilesVerified => Manifests.Sum(m => m.Files);

    public bool Verified => Findings.Count == 0;

    /// <summary>Each manifest's SHA-256 and verified file count, as recorded in every graphic's provenance.</summary>
    public string DescribeManifests() => string.Join("; ", Manifests.Select(m => string.Create(CultureInfo.InvariantCulture, $"{m.Name} sha256 {m.Sha256}, {m.Files} files")));

    public string DescribeFaces() => $"regular {Regular?.Describe() ?? "not read"}; italic {Italic?.Describe() ?? "not read"}";
}

/// <summary>
/// THE GRAPHICS INSTALLATION VERIFIER (the graphics change, decision D-003 of its design). It READS FILES ONLY and starts no
/// process. Each manifest is hashed against the SHA-256 the settings name and parsed from those very bytes; every entry is
/// relative (an absolute entry, or one climbing out of its folder, is refused); every listed file is hashed and compared,
/// naming both values; any file under the renderer's packages, the browser folder or the font folder that its manifest does
/// not list is refused (a re-created cache, a planted module); Node is the one file its manifest lists. The renderer's name
/// and version are read from its verified package files, the browser's and Node's versions from the version resource of their
/// verified executables, and each face's family, version and character map from its verified bytes.
///
/// THE HOLD. <see cref="HoldAsync"/> opens every listed file read-only, sharing read only, hashes it THROUGH THAT HANDLE and
/// keeps it open: while held, no listed file can be written, replaced, renamed or deleted, so the bytes hashed are the bytes
/// Node, the bundler, the browser and the compositor load until the hold is released after the last render exits.
/// </summary>
public sealed class RendererInstallationVerifier
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private readonly GraphicsSettings _settings;

    public RendererInstallationVerifier(GraphicsSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
    }

    /// <summary>Verifies the whole installation without holding it (verify-graphics and plan-only), under the hashing bound.</summary>
    public async Task<RendererReport> VerifyAsync(CancellationToken cancellationToken)
    {
        var (report, held) = await RunAsync(hold: false, cancellationToken).ConfigureAwait(false);
        await held.DisposeAsync().ConfigureAwait(false);
        return report;
    }

    /// <summary>
    /// Verifies the installation and HOLDS every listed file until the returned hold is disposed; refuses, releasing everything,
    /// on any finding. The caller disposes the hold only after the last render process of the stage exited.
    /// </summary>
    public async Task<(RendererReport Report, HeldInstallation Held)> HoldAsync(CancellationToken cancellationToken)
    {
        var (report, held) = await RunAsync(hold: true, cancellationToken).ConfigureAwait(false);
        if (!report.Verified)
        {
            await held.DisposeAsync().ConfigureAwait(false);
            throw new InstallationRefusedException(report.Findings);
        }

        return (report, held);
    }

    private async Task<(RendererReport Report, HeldInstallation Held)> RunAsync(bool hold, CancellationToken cancellationToken)
    {
        using var bounded = new CancellationTokenSource(_settings.HashBound);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(bounded.Token, cancellationToken);
        var held = new HeldInstallation();
        try
        {
            return (await VerifyAllAsync(hold ? held : null, linked.Token).ConfigureAwait(false), held);
        }
        catch (OperationCanceledException) when (bounded.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            await held.DisposeAsync().ConfigureAwait(false);
            return (new RendererReport
            {
                Findings = [string.Create(CultureInfo.InvariantCulture, $"the installation could not be hashed within the configured hashing bound of {_settings.HashBound.TotalSeconds:0} s on the monotonic clock")],
                Manifests = [],
            }, new HeldInstallation());
        }
        catch
        {
            await held.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<RendererReport> VerifyAllAsync(HeldInstallation? held, CancellationToken cancellationToken)
    {
        var findings = new List<string>();
        var manifests = new List<VerifiedManifest>();
        var listed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var bytesOf = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

        // The files whose bytes are read for an identity are kept from the pass that hashed them.
        var keep = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Full(_settings.NodeExecutable),
            Full(_settings.BrowserExecutable),
            Full(_settings.RegularFace),
            Full(_settings.ItalicFace),
            Full(Path.Combine(_settings.Renderer.Folder, "node_modules", "remotion", "package.json")),
            Full(Path.Combine(_settings.Renderer.Folder, "node_modules", "@remotion", "renderer", "package.json")),
            Full(Path.Combine(_settings.Renderer.Folder, "node_modules", "@remotion", "bundler", "package.json")),
        };

        foreach (var (name, manifest, extras) in new[]
                 {
                     ("renderer", _settings.Renderer, (string?)"node_modules"),
                     ("browser", _settings.Browser, (string?)string.Empty),
                     ("font", _settings.Font, (string?)string.Empty),
                     ("node", _settings.Node, (string?)null),
                 })
        {
            var verified = await ManifestAsync(name, manifest, extras, held, keep, listed, bytesOf, findings, cancellationToken).ConfigureAwait(false);
            if (verified is not null)
            {
                manifests.Add(verified);
            }
        }

        // The executables and faces the settings name must be files a manifest listed.
        foreach (var (setting, path, folder) in new[]
                 {
                     ("graphics.nodeExecutable", _settings.NodeExecutable, _settings.Node.Folder),
                     ("graphics.browserExecutable", _settings.BrowserExecutable, _settings.Browser.Folder),
                     ("graphics.regularFace", _settings.RegularFace, _settings.Font.Folder),
                     ("graphics.italicFace", _settings.ItalicFace, _settings.Font.Folder),
                 })
        {
            if (!Inside(Full(folder), Full(path)) || !listed.ContainsKey(Full(path)))
            {
                findings.Add($"{setting}: {path} is not a file its manifest lists inside {folder}");
            }
        }

        string? Read(string path, Func<byte[], string?> reader) => bytesOf.TryGetValue(Full(path), out var bytes) ? reader(bytes) : null;
        var renderer = Read(Path.Combine(_settings.Renderer.Folder, "node_modules", "remotion", "package.json"), Package);
        var packages = string.Join(", ", new[] { "renderer", "bundler" }
            .Select(p => Read(Path.Combine(_settings.Renderer.Folder, "node_modules", "@remotion", p, "package.json"), Package) ?? "not read"));
        var browserVersion = Read(_settings.BrowserExecutable, ExecutableVersion);
        var nodeVersion = Read(_settings.NodeExecutable, ExecutableVersion);

        VerifiedFace? Face(string path)
        {
            if (!bytesOf.TryGetValue(Full(path), out var bytes))
            {
                return null;
            }

            try
            {
                return new VerifiedFace(path, Convert.ToHexStringLower(SHA256.HashData(bytes)), FontCoverage.Read(bytes));
            }
            catch (FontRefusedException refused)
            {
                findings.Add($"{path}: {refused.Message}");
                return null;
            }
        }

        var regular = Face(_settings.RegularFace);
        var italic = Face(_settings.ItalicFace);
        if (renderer is null)
        {
            findings.Add("the renderer's package file was not verified, so its name and version cannot be read");
        }

        return new RendererReport
        {
            Findings = findings,
            Manifests = manifests,
            RendererName = renderer?.Split(' ')[0],
            RendererVersion = renderer?.Split(' ').ElementAtOrDefault(1),
            RendererPackages = packages,
            NodeSha256 = listed.GetValueOrDefault(Full(_settings.NodeExecutable)),
            NodeVersion = nodeVersion,
            BrowserSha256 = listed.GetValueOrDefault(Full(_settings.BrowserExecutable)),
            BrowserVersion = browserVersion,
            Regular = regular,
            Italic = italic,
        };
    }

    /// <summary>
    /// Verifies one manifest: its own hash, then every entry. <paramref name="extrasUnder"/> names the folder, relative to the
    /// manifest's, under which an unlisted file is refused; null for Node, which is verified as the one file listed.
    /// </summary>
    private static async Task<VerifiedManifest?> ManifestAsync(
        string name, InstalledManifest manifest, string? extrasUnder, HeldInstallation? held, HashSet<string> keep,
        Dictionary<string, string> listed, Dictionary<string, byte[]> bytesOf, List<string> findings, CancellationToken cancellationToken)
    {
        if (!File.Exists(manifest.Manifest))
        {
            findings.Add($"the {name} manifest {manifest.Manifest} is missing");
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(manifest.Manifest, cancellationToken).ConfigureAwait(false);
        var computed = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (!string.Equals(computed, manifest.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add($"the {name} manifest {manifest.Manifest}: sha256 {computed} computed over the file in this run, and {manifest.Sha256} configured as expected");
            return null;
        }

        var folder = Full(manifest.Folder);
        var files = 0;
        var mine = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in Encoding.UTF8.GetString(bytes).Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var trimmed = line.TrimEnd('\r');
            var space = trimmed.IndexOf(' ', StringComparison.Ordinal);
            if (space != 64 || trimmed.Length < 67)
            {
                findings.Add($"the {name} manifest holds a line that is not a hash and a path: \"{(trimmed.Length > 80 ? trimmed[..80] : trimmed)}\"");
                continue;
            }

            var expected = trimmed[..64].ToLowerInvariant();
            var entry = trimmed[(space + 1)..].TrimStart('*', ' ');
            if (Path.IsPathRooted(entry) || entry.StartsWith('/') || entry.StartsWith('\\') || entry.Contains(':', StringComparison.Ordinal)
                || entry.Split('/', '\\').Any(part => part == ".."))
            {
                findings.Add($"the {name} manifest entry {entry} is absolute or climbs out of its folder; every entry is relative to {manifest.Folder}");
                continue;
            }

            var path = Full(Path.Combine(folder, entry));
            if (!Inside(folder, path))
            {
                findings.Add($"the {name} manifest entry {entry} lies outside {manifest.Folder}");
                continue;
            }

            if (!File.Exists(path))
            {
                findings.Add($"{path}: listed by the {name} manifest and missing");
                continue;
            }

            string actual;
            if (held is not null)
            {
                var stream = File.Open(path, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.Read, BufferSize = 0 });
                held.Streams.Add(stream);
                if (keep.Contains(path))
                {
                    var all = await ReadAllAsync(stream, cancellationToken).ConfigureAwait(false);
                    bytesOf[path] = all;
                    actual = Convert.ToHexStringLower(SHA256.HashData(all));
                }
                else
                {
                    actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
                }

                held.Files.Add(new VerifiedFile(name, path, expected, actual));
            }
            else if (keep.Contains(path))
            {
                var all = await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false);
                bytesOf[path] = all;
                actual = Convert.ToHexStringLower(SHA256.HashData(all));
            }
            else
            {
                await using var stream = File.OpenRead(path);
                actual = Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
            }

            if (!string.Equals(actual, expected, StringComparison.Ordinal))
            {
                findings.Add($"{path}: sha256 {actual} computed over the file in this run, and {expected} listed by the {name} manifest");
                bytesOf.Remove(path);
                continue;
            }

            listed[path] = actual;
            mine.Add(path);
            files++;
        }

        if (extrasUnder is not null)
        {
            var root = extrasUnder.Length == 0 ? folder : Full(Path.Combine(folder, extrasUnder));
            if (Directory.Exists(root))
            {
                foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
                {
                    var full = Full(file);
                    if (!mine.Contains(full) && !listed.ContainsKey(full))
                    {
                        findings.Add($"{full}: a file under the {name} folder that its manifest does not list (a re-created cache or an added file is refused)");
                    }
                }
            }
        }

        return new VerifiedManifest(name, manifest.Manifest, computed, files);
    }

    /// <summary>A package file's name and version, read from its verified bytes.</summary>
    private static string? Package(byte[] bytes)
    {
        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            return root.TryGetProperty("name", out var n) && root.TryGetProperty("version", out var v) ? $"{n.GetString()} {v.GetString()}" : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// An executable's file version, read from the fixed version information in its verified bytes (the signature 0xFEEF04BD
    /// followed by the structure version and the most and least significant halves of the file version).
    /// </summary>
    internal static string? ExecutableVersion(byte[] bytes)
    {
        ReadOnlySpan<byte> signature = [0xBD, 0x04, 0xEF, 0xFE];
        var at = bytes.AsSpan().IndexOf(signature);
        if (at < 0 || at + 16 > bytes.Length)
        {
            return null;
        }

        var high = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at + 8));
        var low = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(at + 12));
        return string.Create(CultureInfo.InvariantCulture, $"{high >> 16}.{high & 0xFFFF}.{low >> 16}.{low & 0xFFFF}");
    }

    private static async Task<byte[]> ReadAllAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer.ToArray();
    }

    private static string Full(string path) => Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar);

    private static bool Inside(string folder, string path) =>
        path.StartsWith(folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison);
}
