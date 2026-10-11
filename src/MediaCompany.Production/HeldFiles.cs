using System.Security.Cryptography;

namespace MediaCompany.Production;

/// <summary>
/// Files held open READ-ONLY, sharing read only, from their hash until their reader exits (the graphics change, decisions
/// D-003 and D-005 of its design): while held, none can be written, replaced, renamed or deleted, so the bytes hashed through
/// the handle are the bytes the reader takes. It opens no file for writing.
/// </summary>
public sealed class HeldFiles : IAsyncDisposable
{
    private readonly List<FileStream> _streams = [];

    public int Count => _streams.Count;

    /// <summary>Opens and holds a file, and returns the SHA-256 computed through the held handle.</summary>
    public async Task<string> HoldAsync(string path, CancellationToken cancellationToken)
    {
        var stream = Open(path);
        _streams.Add(stream);
        return Convert.ToHexStringLower(await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false));
    }

    /// <summary>Opens and holds a small file, and returns its bytes read through the held handle with their SHA-256.</summary>
    public async Task<(string Sha256, byte[] Bytes)> HoldBytesAsync(string path, CancellationToken cancellationToken)
    {
        var stream = Open(path);
        _streams.Add(stream);
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        var bytes = buffer.ToArray();
        return (Convert.ToHexStringLower(SHA256.HashData(bytes)), bytes);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var stream in _streams)
        {
            await stream.DisposeAsync().ConfigureAwait(false);
        }

        _streams.Clear();
    }

    private static FileStream Open(string path) =>
        File.Open(path, new FileStreamOptions { Mode = FileMode.Open, Access = FileAccess.Read, Share = FileShare.Read, BufferSize = 0 });
}
