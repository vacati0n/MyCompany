using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediaCompany.Pilot.Web;

/// <summary>The button as "Cài đặt" shows it: the bookmark address, its size, the format version and the button mark.</summary>
internal sealed record ButtonView(string Address, int Bytes, int FormVersion, string Mark);

/// <summary>
/// THE BUTTON BUILDER (MC-19 technical design module M-002; decisions D-004, D-006, D-012). The button script is a plain repository
/// file embedded in this assembly (Pages/nut-doc-trang.js, never served as a page file). The button mark is the first 12 hexadecimal
/// characters of the SHA-256 of that file as held. The builder drops the comment lines, places the configuration (format version,
/// mark, and the exact fixture origin when the internal test seam supplies one; none in the shipped app) at the configuration
/// placeholder, and percent-encodes the result after "javascript:". The bookmark loads no code when pressed.
/// </summary>
internal static class ButtonBuilder
{
    public const string ResourceName = "page:nut-doc-trang.js";
    public const string Placeholder = "/*MC19-CONFIG*/null";
    public const int MaxAddressBytes = 32 * 1024;

    private static readonly Lazy<byte[]> Script = new(() =>
    {
        using var stream = typeof(ButtonBuilder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("the embedded button script is missing");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    });

    /// <summary>The button script as held, byte for byte.</summary>
    public static byte[] Source => Script.Value;

    /// <summary>The button mark (D-006): the first 12 hexadecimal characters of the SHA-256 of the script as held.</summary>
    public static string Mark { get; } = Convert.ToHexStringLower(SHA256.HashData(Script.Value))[..12];

    /// <summary>The script with its configuration placed and its comment lines dropped, before encoding.</summary>
    public static string Body(string? fixtureOrigin)
    {
        var text = Encoding.UTF8.GetString(Source).TrimStart('﻿');
        var origins = fixtureOrigin is null ? Array.Empty<string>() : [fixtureOrigin];
        var config = JsonSerializer.Serialize(new { v = BlockReader.FormVersion, mark = Mark, origins });
        if (!text.Contains(Placeholder, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("the button script has no configuration placeholder");
        }

        text = text.Replace(Placeholder, config, StringComparison.Ordinal);
        var kept = text.Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith("//", StringComparison.Ordinal));
        return string.Join('\n', kept);
    }

    /// <summary>The bookmark address: "javascript:" and the percent-encoded body; refused when over 32 KiB (D-012).</summary>
    public static ButtonView Build(string? fixtureOrigin)
    {
        var address = "javascript:" + Uri.EscapeDataString(Body(fixtureOrigin));
        var bytes = Encoding.UTF8.GetByteCount(address);
        if (bytes > MaxAddressBytes)
        {
            throw new InvalidOperationException($"the button address is {bytes} bytes, over {MaxAddressBytes}");
        }

        return new ButtonView(address, bytes, BlockReader.FormVersion, Mark);
    }
}
