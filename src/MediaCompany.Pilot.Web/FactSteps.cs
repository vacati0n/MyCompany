using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MediaCompany.Pilot.Web;

/// <summary>One banned term the live check found in one field: the field, the term, the class letter and name.</summary>
public sealed record LiveFinding(string Where, string Term, string ClassId, string ClassName);

/// <summary>One field the page sends to the live check; an override is also checked for first-person and use words.</summary>
public sealed record LiveField(string Where, string Text, bool IsOverride);

/// <summary>
/// THE FACT SHEET STEPS (scope items 1 to 3; design decisions D-008 and D-015).
/// <list type="bullet">
/// <item>The live check answers from the toolchain's own banned-term check and list: every finding in every field, and for an
/// override the first first-person or use word. The page holds no copy of the list.</item>
/// <item>The fact sheet is saved in the toolchain's own form, every text NFC-normalised, only after the toolchain's fact sheet check
/// accepts it. The images named in the sheet arrive at step 2, so for this check each image name is first held to the plain-name rule
/// and then stands in as an empty placeholder; the screenshots must already be attached. The check verb decides again before the
/// script is written, with the real images.</item>
/// </list>
/// </summary>
public static class FactSteps
{
    public static readonly IReadOnlyList<string> ImageExtensions = [".png", ".jpg", ".jpeg"];

    public static IReadOnlyList<LiveFinding> LiveCheck(IReadOnlyList<LiveField> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);
        var banned = BannedTermCheck.Held;
        var findings = new List<LiveFinding>();
        foreach (var field in fields)
        {
            var text = field.Text ?? string.Empty;
            if (field.IsOverride && banned.FirstPerson(text) is { } person)
            {
                findings.Add(new LiveFinding(field.Where, person, "ngôi thứ nhất", "câu thay thế không được xưng ngôi thứ nhất, nói đã dùng hay nhận xét"));
            }

            findings.AddRange(banned.Find(field.Where, text).Select(f => new LiveFinding(f.Where, f.Term, f.ClassId, f.ClassName)));
        }

        return findings;
    }

    /// <summary>Every string of the document NFC-normalised, so the stored text equals the typed text after normalisation.</summary>
    public static byte[] Normalise(byte[] json)
    {
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(json);
        }
        catch (JsonException error)
        {
            throw new WebRefusal("fact-sheet-unreadable", $"Biểu mẫu gửi lên không đọc được ({error.Message}). Tải lại trang rồi gõ lại.");
        }

        if (root is not JsonObject)
        {
            throw new WebRefusal("fact-sheet-unreadable", "Biểu mẫu gửi lên không đúng dạng. Tải lại trang rồi gõ lại.");
        }

        var normalised = Walk(root)!;
        return Encoding.UTF8.GetBytes(normalised.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping })
            .ReplaceLineEndings("\n") + "\n");
    }

    /// <summary>
    /// Checks the sheet with the toolchain's fact sheet check against the input folder, with the declared image names standing in as
    /// placeholders when they are not yet attached; returns the accepted sheet.
    /// </summary>
    public static FactSheet CheckBeforeImages(byte[] json, string inputFolder, string scratchFolder)
    {
        var check = Path.Combine(scratchFolder, $"kiem-bang-{Guid.NewGuid():N}");
        Directory.CreateDirectory(check);
        try
        {
            using (var document = JsonDocument.Parse(json))
            {
                var root = document.RootElement;
                if (root.TryGetProperty("imagesUsed", out var images) && images.ValueKind == JsonValueKind.Array)
                {
                    foreach (var image in images.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.String))
                    {
                        Placeholder(check, image.GetString()!, "Ảnh dùng trong video", inputFolder: null);
                    }
                }

                if (root.TryGetProperty("facts", out var facts) && facts.ValueKind == JsonValueKind.Array)
                {
                    var index = 0;
                    foreach (var row in facts.EnumerateArray())
                    {
                        index++;
                        if (row.ValueKind == JsonValueKind.Object && row.TryGetProperty("screenshot", out var shot) && shot.ValueKind == JsonValueKind.String)
                        {
                            Placeholder(check, shot.GetString()!, $"Dòng F{index}", inputFolder);
                        }
                    }
                }
            }

            return FactSheetCheck.Check(json, check, BannedTermCheck.Held);
        }
        finally
        {
            Directory.Delete(check, recursive: true);
        }
    }

    /// <summary>The declared images of a saved sheet, each with whether it is attached.</summary>
    public static IReadOnlyList<(string Name, bool Present)> DeclaredImages(string factSheetPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllBytes(factSheetPath));
        if (!document.RootElement.TryGetProperty("imagesUsed", out var images) || images.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var folder = Path.GetDirectoryName(factSheetPath)!;
        return images.EnumerateArray().Where(i => i.ValueKind == JsonValueKind.String)
            .Select(i => VietnameseText.Nfc(i.GetString()!.Trim()))
            .Select(n => (n, File.Exists(Path.Combine(folder, n)))).ToList();
    }

    private static void Placeholder(string checkFolder, string raw, string where, string? inputFolder)
    {
        var name = VietnameseText.Nfc(raw.Trim());
        try
        {
            // The toolchain's rule first: a name that is not plain is left for the check to refuse, never created.
            InputFiles.Resolve(checkFolder, name, where);
            return;
        }
        catch (PilotRefusal refusal) when (refusal.Code == "file-missing")
        {
            // Plain.
        }
        catch (PilotRefusal)
        {
            return;
        }

        if (inputFolder is null || File.Exists(Path.Combine(inputFolder, name)))
        {
            File.WriteAllBytes(Path.Combine(checkFolder, name), []);
        }
    }

    private static JsonNode? Walk(JsonNode? node)
    {
        switch (node)
        {
            case JsonObject obj:
                var copy = new JsonObject();
                foreach (var (key, value) in obj)
                {
                    copy[key] = Walk(value);
                }

                return copy;
            case JsonArray array:
                var list = new JsonArray();
                foreach (var item in array)
                {
                    list.Add(Walk(item));
                }

                return list;
            case JsonValue value when value.TryGetValue<string>(out var text):
                return JsonValue.Create(VietnameseText.Nfc(text));
            default:
                return node?.DeepClone();
        }
    }
}
