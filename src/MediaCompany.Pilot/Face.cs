using MediaCompany.Deterministic.Production;

namespace MediaCompany.Pilot;

/// <summary>One drawn character, its code point and whether the installed face maps it.</summary>
public sealed record DrawnCharacter(string Character, string CodePoint, bool Covered);

/// <summary>The admitted face: its bytes' SHA-256, as the font manifest names it, its reading and its metrics.</summary>
public sealed record AdmittedFace(string Path, string Sha256, string ManifestLine, FaceReading Reading, FaceMetrics Metrics);

/// <summary>
/// THE FACE CHECKS (design D-010). The configured face must hash to the digest its font-manifest line carries, and every drawn
/// character (NFC) of the captions and the overlay must be mapped by the face; nothing is substituted.
/// </summary>
public static class Face
{
    public static AdmittedFace Admit(FaceSetting setting)
    {
        ArgumentNullException.ThrowIfNull(setting);
        if (!File.Exists(setting.Path) || !File.Exists(setting.Manifest))
        {
            throw new PilotRefusal("face-missing", $"Không thấy tệp phông chữ {setting.Path} hoặc tệp kê phông chữ {setting.Manifest}.");
        }

        var line = File.ReadAllLines(setting.Manifest).Select(l => l.Trim())
            .FirstOrDefault(l => string.Equals(l, setting.ManifestEntry.Trim(), StringComparison.Ordinal));
        if (line is null)
        {
            throw new PilotRefusal("face-digest-mismatch", $"Tệp kê phông chữ {setting.Manifest} không có dòng đã cấu hình \"{setting.ManifestEntry}\".");
        }

        var listed = line.Split(' ', 2)[0];
        var bytes = File.ReadAllBytes(setting.Path);
        var actual = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes));
        if (!string.Equals(listed, actual, StringComparison.OrdinalIgnoreCase))
        {
            throw new PilotRefusal("face-digest-mismatch",
                $"Phông chữ {setting.Path} có SHA-256 {actual}, khác dòng tệp kê ({listed}). Không dùng phông chữ lạ.");
        }

        FaceReading reading;
        try
        {
            reading = FontCoverage.Read(bytes);
        }
        catch (FontRefusedException refused)
        {
            throw new PilotRefusal("face-unreadable", $"Phông chữ {setting.Path} không đọc được: {refused.Message}");
        }

        return new AdmittedFace(setting.Path, actual, line, reading, FaceMetrics.Read(bytes));
    }

    /// <summary>Refuses a drawn text with a character the face does not map, naming the text and the character.</summary>
    public static void RequireCoverage(AdmittedFace face, IEnumerable<(string Where, string Text)> drawn)
    {
        ArgumentNullException.ThrowIfNull(face);
        var findings = FontCoverage.Uncovered(face.Reading, drawn.Select(d => (d.Where, VietnameseText.Nfc(d.Text))));
        if (findings.Count > 0)
        {
            throw new PilotRefusal("glyph-not-covered", $"Phông chữ thiếu ký tự: {findings[0]}");
        }
    }

    /// <summary>The distinct drawn characters (NFC), each with its code point and whether the face maps it, for the run record.</summary>
    public static IReadOnlyList<DrawnCharacter> DrawnCharacters(AdmittedFace face, IEnumerable<string> texts)
    {
        ArgumentNullException.ThrowIfNull(face);
        return texts.SelectMany(t => VietnameseText.Nfc(t).EnumerateRunes()).Distinct().OrderBy(r => r.Value)
            .Select(r => new DrawnCharacter(r.ToString(), $"U+{r.Value:X4}", face.Reading.Covers(r.Value))).ToList();
    }
}
