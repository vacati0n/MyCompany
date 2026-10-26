using System.Buffers.Binary;
using System.Text;

namespace MediaCompany.Pilot;

/// <summary>
/// The installed face's horizontal metrics: the advance width of each mapped character and the ascent and descent, read from
/// the face's head, hhea, hmtx and cmap tables. FontCoverage answers whether a character is mapped; it does not expose advance
/// widths, which the caption width limit derived from the safe-area width needs. Kerning is not applied, so a measured width
/// is the unkerned sum, which is not smaller than what the text filter draws for Latin and Vietnamese text.
/// </summary>
public sealed class FaceMetrics
{
    private readonly Dictionary<int, int> _glyphs;
    private readonly int[] _advances;

    private FaceMetrics(int unitsPerEm, int ascender, int descender, Dictionary<int, int> glyphs, int[] advances)
    {
        UnitsPerEm = unitsPerEm;
        Ascender = ascender;
        Descender = descender;
        _glyphs = glyphs;
        _advances = advances;
    }

    public int UnitsPerEm { get; }

    public int Ascender { get; }

    public int Descender { get; }

    /// <summary>The height one line takes at the size, in whole pixels rounded up: ascent plus descent.</summary>
    public int LineHeight(int size) => (int)Math.Ceiling((double)(Ascender - Descender) * size / UnitsPerEm);

    /// <summary>The unkerned width of the text at the size, in pixels; an unmapped character counts as the face's widest advance.</summary>
    public double Width(string text, int size)
    {
        ArgumentNullException.ThrowIfNull(text);
        double units = 0;
        var widest = _advances.Length == 0 ? UnitsPerEm : _advances.Max();
        foreach (var rune in text.EnumerateRunes())
        {
            units += _glyphs.TryGetValue(rune.Value, out var glyph) ? Advance(glyph) : widest;
        }

        return units * size / UnitsPerEm;
    }

    private int Advance(int glyph) => _advances.Length == 0 ? 0 : _advances[Math.Min(glyph, _advances.Length - 1)];

    public static FaceMetrics Read(ReadOnlySpan<byte> face)
    {
        try
        {
            return ReadChecked(face);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw new PilotRefusal("face-unreadable", "Tệp phông chữ đã cấu hình không đọc được bảng số đo (head, hhea, hmtx, cmap).");
        }
    }

    private static FaceMetrics ReadChecked(ReadOnlySpan<byte> face)
    {
        var tables = new Dictionary<string, (int Offset, int Length)>(StringComparer.Ordinal);
        var count = BinaryPrimitives.ReadUInt16BigEndian(face[4..]);
        for (var i = 0; i < count; i++)
        {
            var record = 12 + (16 * i);
            var tag = Encoding.ASCII.GetString(face.Slice(record, 4));
            var offset = checked((int)BinaryPrimitives.ReadUInt32BigEndian(face[(record + 8)..]));
            var length = checked((int)BinaryPrimitives.ReadUInt32BigEndian(face[(record + 12)..]));
            tables[tag] = (offset, length);
        }

        ReadOnlySpan<byte> Table(ReadOnlySpan<byte> all, string tag) =>
            tables.TryGetValue(tag, out var t)
                ? all.Slice(t.Offset, t.Length)
                : throw new PilotRefusal("face-unreadable", $"Tệp phông chữ không có bảng {tag}.");

        var head = Table(face, "head");
        var hhea = Table(face, "hhea");
        var hmtx = Table(face, "hmtx");
        var cmap = Table(face, "cmap");
        var unitsPerEm = BinaryPrimitives.ReadUInt16BigEndian(head[18..]);
        var ascender = BinaryPrimitives.ReadInt16BigEndian(hhea[4..]);
        var descender = BinaryPrimitives.ReadInt16BigEndian(hhea[6..]);
        var metrics = BinaryPrimitives.ReadUInt16BigEndian(hhea[34..]);
        var advances = new int[metrics];
        for (var g = 0; g < metrics; g++)
        {
            advances[g] = BinaryPrimitives.ReadUInt16BigEndian(hmtx[(g * 4)..]);
        }

        return new FaceMetrics(unitsPerEm, ascender, descender, Glyphs(cmap), advances);
    }

    private static Dictionary<int, int> Glyphs(ReadOnlySpan<byte> cmap)
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
            if (!(platform == 0 || (platform == 3 && encoding is 1 or 10)))
            {
                continue;
            }

            var format = BinaryPrimitives.ReadUInt16BigEndian(cmap[offset..]);
            if (format == 12)
            {
                format12 ??= offset;
            }
            else if (format == 4)
            {
                format4 ??= offset;
            }
        }

        var glyphs = new Dictionary<int, int>();
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
                    glyphs[(int)c] = (int)(glyph + (c - start));
                }
            }

            return glyphs;
        }

        if (format4 is not { } four)
        {
            throw new PilotRefusal("face-unreadable", "Bảng cmap của phông chữ không có bảng con Unicode dạng 4 hoặc 12.");
        }

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
                    glyph = glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
                }

                if (glyph != 0)
                {
                    glyphs[c] = glyph;
                }
            }
        }

        return glyphs;
    }
}
