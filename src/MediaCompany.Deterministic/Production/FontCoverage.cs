using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace MediaCompany.Deterministic.Production;

/// <summary>A face's identity and character map, read from the very bytes whose SHA-256 the verifier recorded.</summary>
public sealed record FaceReading(string Family, string Subfamily, string Version, IReadOnlySet<int> CodePoints)
{
    public bool Covers(int codePoint) => CodePoints.Contains(codePoint);
}

/// <summary>
/// THE FONT COVERAGE CHECK (the graphics change, decision D-009 of its design), a pure function over a face file's bytes. It
/// reads the face's character map (formats 4 and 12 of the Unicode subtables) and its name table, and nothing else: no
/// installed system font, no operating-system call and no renderer. A drawn character the face does not map is refused,
/// naming the character, its code point and the graphic, before any process starts; no character is ever substituted.
/// </summary>
public static class FontCoverage
{
    /// <summary>Reads a face's family, subfamily, version and mapped code points; refuses a file that is not a readable face.</summary>
    public static FaceReading Read(ReadOnlySpan<byte> face)
    {
        try
        {
            return ReadChecked(face);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new FontRefusedException("the face file's tables run past its end");
        }
    }

    private static FaceReading ReadChecked(ReadOnlySpan<byte> face)
    {
        if (face.Length < 12)
        {
            throw new FontRefusedException("the face file is shorter than a font header");
        }

        var tables = new Dictionary<string, (int Offset, int Length)>(StringComparer.Ordinal);
        var count = BinaryPrimitives.ReadUInt16BigEndian(face[4..]);
        for (var i = 0; i < count; i++)
        {
            var record = 12 + (16 * i);
            if (record + 16 > face.Length)
            {
                throw new FontRefusedException("the face file's table directory runs past its end");
            }

            var tag = Encoding.ASCII.GetString(face.Slice(record, 4));
            var offset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(face[(record + 8)..]));
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(face[(record + 12)..]));
            if (offset < 0 || length < 0 || offset + length > face.Length)
            {
                throw new FontRefusedException($"the face file's {tag} table runs past its end");
            }

            tables[tag] = (offset, length);
        }

        if (!tables.TryGetValue("cmap", out var cmap))
        {
            throw new FontRefusedException("the face file holds no character map");
        }

        if (!tables.TryGetValue("name", out var name))
        {
            throw new FontRefusedException("the face file holds no name table");
        }

        var codePoints = CharacterMap(face.Slice(cmap.Offset, cmap.Length));
        var names = Names(face.Slice(name.Offset, name.Length));
        return new FaceReading(
            names.GetValueOrDefault(16) ?? names.GetValueOrDefault(1) ?? throw new FontRefusedException("the face file's name table states no family"),
            names.GetValueOrDefault(17) ?? names.GetValueOrDefault(2) ?? "not stated",
            names.GetValueOrDefault(5) ?? "not stated",
            codePoints);
    }

    /// <summary>Every drawn character the face does not map, each named with its code point and where it is drawn.</summary>
    public static IReadOnlyList<string> Uncovered(FaceReading face, IEnumerable<(string Where, string Text)> drawn)
    {
        ArgumentNullException.ThrowIfNull(face);
        ArgumentNullException.ThrowIfNull(drawn);
        var findings = new List<string>();
        foreach (var (where, text) in drawn)
        {
            var seen = new HashSet<int>();
            foreach (var rune in text.EnumerateRunes())
            {
                if (!face.Covers(rune.Value) && seen.Add(rune.Value))
                {
                    findings.Add(string.Create(CultureInfo.InvariantCulture,
                        $"{where}: the character '{rune}' (U+{rune.Value:X4}) is not mapped by the face {face.Family} {face.Subfamily}, so another font would draw it; nothing is substituted"));
                }
            }
        }

        return findings;
    }

    private static HashSet<int> CharacterMap(ReadOnlySpan<byte> cmap)
    {
        var subtables = BinaryPrimitives.ReadUInt16BigEndian(cmap[2..]);
        int? format12 = null;
        int? format4 = null;
        for (var i = 0; i < subtables; i++)
        {
            var record = 4 + (8 * i);
            var platform = BinaryPrimitives.ReadUInt16BigEndian(cmap[record..]);
            var encoding = BinaryPrimitives.ReadUInt16BigEndian(cmap[(record + 2)..]);
            var offset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(cmap[(record + 4)..]));
            var format = BinaryPrimitives.ReadUInt16BigEndian(cmap[offset..]);
            var unicode = platform == 0 || (platform == 3 && encoding is 1 or 10);
            if (!unicode)
            {
                continue;
            }

            if (format == 12)
            {
                format12 ??= offset;
            }
            else if (format == 4)
            {
                format4 ??= offset;
            }
        }

        if (format12 is null && format4 is null)
        {
            throw new FontRefusedException("the face file's character map holds no Unicode subtable of format 4 or 12");
        }

        var mapped = new HashSet<int>();
        if (format12 is { } twelve)
        {
            var groups = BinaryPrimitives.ReadUInt32BigEndian(cmap[(twelve + 12)..]);
            for (var g = 0; g < groups; g++)
            {
                var at = twelve + 16 + (g * 12);
                var start = BinaryPrimitives.ReadUInt32BigEndian(cmap[at..]);
                var end = BinaryPrimitives.ReadUInt32BigEndian(cmap[(at + 4)..]);
                var glyph = BinaryPrimitives.ReadUInt32BigEndian(cmap[(at + 8)..]);
                for (var c = start; c <= end && c <= 0x10FFFF; c++)
                {
                    if (glyph + (c - start) != 0)
                    {
                        mapped.Add((int)c);
                    }
                }
            }
        }

        if (format4 is { } four)
        {
            var segments = BinaryPrimitives.ReadUInt16BigEndian(cmap[(four + 6)..]) / 2;
            var ends = four + 14;
            var starts = ends + (segments * 2) + 2;
            var deltas = starts + (segments * 2);
            var ranges = deltas + (segments * 2);
            for (var s = 0; s < segments; s++)
            {
                int end = BinaryPrimitives.ReadUInt16BigEndian(cmap[(ends + (s * 2))..]);
                int start = BinaryPrimitives.ReadUInt16BigEndian(cmap[(starts + (s * 2))..]);
                var delta = BinaryPrimitives.ReadInt16BigEndian(cmap[(deltas + (s * 2))..]);
                int rangeOffset = BinaryPrimitives.ReadUInt16BigEndian(cmap[(ranges + (s * 2))..]);
                for (var c = start; c <= end && c != 0xFFFF; c++)
                {
                    int glyph;
                    if (rangeOffset == 0)
                    {
                        glyph = (c + delta) & 0xFFFF;
                    }
                    else
                    {
                        var at = ranges + (s * 2) + rangeOffset + ((c - start) * 2);
                        glyph = BinaryPrimitives.ReadUInt16BigEndian(cmap[at..]);
                        if (glyph != 0)
                        {
                            glyph = (glyph + delta) & 0xFFFF;
                        }
                    }

                    if (glyph != 0)
                    {
                        mapped.Add(c);
                    }
                }
            }
        }

        return mapped;
    }

    private static Dictionary<int, string> Names(ReadOnlySpan<byte> table)
    {
        var count = BinaryPrimitives.ReadUInt16BigEndian(table[2..]);
        var strings = BinaryPrimitives.ReadUInt16BigEndian(table[4..]);
        var names = new Dictionary<int, string>();
        for (var i = 0; i < count; i++)
        {
            var record = 6 + (12 * i);
            var platform = BinaryPrimitives.ReadUInt16BigEndian(table[record..]);
            var language = BinaryPrimitives.ReadUInt16BigEndian(table[(record + 4)..]);
            var id = BinaryPrimitives.ReadUInt16BigEndian(table[(record + 6)..]);
            var length = BinaryPrimitives.ReadUInt16BigEndian(table[(record + 8)..]);
            var offset = BinaryPrimitives.ReadUInt16BigEndian(table[(record + 10)..]);

            // The Windows Unicode records in US English only, UTF-16 big-endian.
            if (platform != 3 || language != 0x0409 || names.ContainsKey(id))
            {
                continue;
            }

            names[id] = Encoding.BigEndianUnicode.GetString(table.Slice(strings + offset, length));
        }

        return names;
    }
}

/// <summary>A face file cannot be read as a font; refused before any process, naming why.</summary>
public sealed class FontRefusedException(string reason) : Exception($"the face is refused: {reason}");
