using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using MediaCompany.Application.Ports;

namespace MediaCompany.Production;

/// <summary>
/// THE ONE TYPE THAT WRITES FILES (the production change, decision D-014 of its design), under one configured
/// output root OUTSIDE the repository.
///
/// The root is refused before anything is written where it is inside, equal to or containing the configured
/// repository root, by comparing full normalised paths, links resolved first, case-insensitively on this
/// platform; no process is started to decide it. Every file is written to the run's staging folder, flushed to
/// disk, closed, MOVED into place, and RE-READ to hash; only that hash, of the stored bytes, is ever returned, so
/// an artifact can be recorded only after its file is complete, and only with the hash of the file that is there.
/// A file the tool wrote into staging is promoted the same way after the tool exited. An interrupted write leaves
/// a staging file and no record; the next run's start clears its staging.
/// </summary>
public sealed class ArtifactWriter : IArtifactStore
{
    private const string StagingFolderName = ".staging";

    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    private static readonly UTF8Encoding Utf8NoMark = new(encoderShouldEmitUTF8Identifier: false);

    public ArtifactWriter(string outputRoot, string repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(outputRoot))
        {
            throw new ProductionSettingRefusedException("outputRoot", "no output root is configured");
        }

        if (string.IsNullOrWhiteSpace(repositoryRoot))
        {
            throw new ProductionSettingRefusedException("repositoryRoot", "no repository root is configured");
        }

        var root = Resolve(outputRoot);
        var repository = Resolve(repositoryRoot);
        if (Contains(repository, root) || Contains(root, repository))
        {
            throw new ProductionSettingRefusedException(
                "outputRoot",
                $"the output root {root} is inside, equal to or containing the repository root {repository}; produced files are written outside the repository only");
        }

        OutputRoot = root;
        Directory.CreateDirectory(OutputRoot);
    }

    public string OutputRoot { get; }

    public string BeginRun(string runFolder)
    {
        var run = Inside(runFolder);
        var staging = Path.Combine(run, StagingFolderName);
        if (Directory.Exists(staging))
        {
            // What an interrupted earlier run left: never a promoted file, and never recorded.
            Directory.Delete(staging, recursive: true);
        }

        Directory.CreateDirectory(staging);
        return staging;
    }

    public async Task WriteStagingTextAsync(string stagingFolder, string name, string text, CancellationToken cancellationToken)
    {
        var path = InStaging(stagingFolder, name);
        await File.WriteAllTextAsync(path, text, Utf8NoMark, cancellationToken).ConfigureAwait(false);
    }

    public Task CopyIntoStagingAsync(string stagingFolder, string sourceFile, string name, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        File.Copy(sourceFile, InStaging(stagingFolder, name), overwrite: true);
        return Task.CompletedTask;
    }

    public async Task<StoredFile> StoreAsync(string stagingFolder, ReadOnlyMemory<byte> bytes, string relativePath, CancellationToken cancellationToken)
    {
        var staged = InStaging(stagingFolder, Guid.NewGuid().ToString("N") + ".part");
        await using (var stream = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, FileOptions.WriteThrough))
        {
            await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
            stream.Flush(flushToDisk: true);
        }

        return await PromoteAsync(stagingFolder, Path.GetFileName(staged), relativePath, cancellationToken).ConfigureAwait(false);
    }

    public async Task<StoredFile> PromoteAsync(string stagingFolder, string stagingName, string relativePath, CancellationToken cancellationToken)
    {
        var staged = InStaging(stagingFolder, stagingName);
        var final = Inside(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(final)!);

        // Promoted once: a produced file is never overwritten, so a recorded hash always names the bytes there.
        File.Move(staged, final, overwrite: false);

        // The hash and the length are of the STORED file, re-read after the move.
        await using var stored = new FileStream(final, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.SequentialScan);
        var hash = await SHA256.HashDataAsync(stored, cancellationToken).ConfigureAwait(false);
        return new StoredFile(relativePath.Replace('\\', '/'), final, stored.Length, Convert.ToHexStringLower(hash));
    }

    public string FullPath(string relativePath) => Inside(relativePath);

    /// <summary>A path under the output root, refused where it would leave it.</summary>
    private string Inside(string relativePath) => Under(OutputRoot, relativePath);

    /// <summary>
    /// The full path a stored file has under a configured output root, refused where it would leave it; nothing is created
    /// or written (the own-voice change: the plan re-hashes registered copies without composing this writer).
    /// </summary>
    internal static string StoredPath(string outputRoot, string relativePath) => Under(Resolve(outputRoot), relativePath);

    private static string Under(string root, string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("A produced file is named relative to the output root.", nameof(relativePath));
        }

        var full = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!Contains(root, full) || string.Equals(full, root, PathComparison))
        {
            throw new ArgumentException($"{relativePath} would leave the output root.", nameof(relativePath));
        }

        return full;
    }

    private static string InStaging(string stagingFolder, string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.IndexOfAny(['/', '\\', ':']) >= 0 || name.Contains("..", StringComparison.Ordinal))
        {
            throw new ArgumentException("A staging file is named by a plain file name.", nameof(name));
        }

        return Path.Combine(stagingFolder, name);
    }

    /// <summary>
    /// The full path, every existing link on it resolved and, on this platform, every existing component expanded from
    /// its short name to its full long name (correction CR-007), with no trailing separator. Without the expansion a
    /// short-name spelling of a folder inside the repository would compare unequal to the repository's long spelling and
    /// pass the containment check.
    /// </summary>
    internal static string Resolve(string path)
    {
        var full = Path.GetFullPath(path);
        var root = Path.GetPathRoot(full)!;
        var resolved = root;
        foreach (var part in full[root.Length..].Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries))
        {
            resolved = Path.Combine(resolved, part);
            var info = new DirectoryInfo(resolved);
            if (info.Exists && info.LinkTarget is not null)
            {
                resolved = info.ResolveLinkTarget(returnFinalTarget: true)?.FullName
                    ?? throw new ProductionSettingRefusedException("outputRoot", $"the link {resolved} on the path cannot be resolved");
            }

            if (OperatingSystem.IsWindows() && (Directory.Exists(resolved) || File.Exists(resolved)))
            {
                resolved = LongName(resolved);
            }
        }

        return resolved.TrimEnd(Path.DirectorySeparatorChar);
    }

    /// <summary>
    /// The long-name spelling of an existing path, as the file system records it. A path the system cannot expand is
    /// refused rather than compared in a spelling that might hide where it is.
    /// </summary>
    private static string LongName(string existing)
    {
        var buffer = new StringBuilder(1024);
        var length = GetLongPathNameW(existing, buffer, (uint)buffer.Capacity);
        if (length > buffer.Capacity)
        {
            buffer = new StringBuilder((int)length);
            length = GetLongPathNameW(existing, buffer, (uint)buffer.Capacity);
        }

        return length == 0 || length > buffer.Capacity
            ? throw new ProductionSettingRefusedException("outputRoot", $"the full long name of {existing} could not be read, so its place cannot be decided")
            : buffer.ToString();
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetLongPathNameW(string shortPath, StringBuilder longPath, uint bufferLength);

    /// <summary>Whether <paramref name="inner"/> is <paramref name="outer"/> or lies under it.</summary>
    internal static bool Contains(string outer, string inner) =>
        string.Equals(outer, inner, PathComparison)
        || inner.StartsWith(outer.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison);
}
