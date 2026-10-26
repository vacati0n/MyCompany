using System.Text.Json;

namespace MediaCompany.Pilot;

/// <summary>One fact row of kit 03 Part A in machine-readable form (design D-006).</summary>
public sealed record FactRow(string Label, string SubjectNoun, string Attribute, string Value, string SourceSection, string Screenshot, string? Override);

/// <summary>An accepted fact sheet: every header field present, one to three complete rows, every named file in the input folder.</summary>
public sealed record FactSheet(
    string VideoCode,
    string ProductName,
    string SubjectNoun,
    string Classification,
    string PagePath,
    string ShopNameAndMall,
    string ReadDate,
    string Niche,
    IReadOnlyList<string> ImagesUsed,
    int ProductVideoNumber,
    string VideoFocus,
    IReadOnlyList<FactRow> Facts,
    int CallToAction);

/// <summary>
/// THE FACT SHEET CHECK (scope criteria 1 to 5, design D-006). Reads one UTF-8 JSON document with the eleven header fields and
/// one to three fact rows, refuses unknown members, and refuses each failure naming the field, the row, the file or the term.
/// Operator-named files are plain names resolved inside the fact sheet's own folder, the input set.
/// </summary>
public static class FactSheetCheck
{
    public const int KnownFormVersion = 1;

    /// <summary>The eleven header fields of scope criterion 3, with the Vietnamese label each refusal names.</summary>
    public static readonly IReadOnlyList<(string Member, string Label)> HeaderFields =
    [
        ("videoCode", "Mã video"),
        ("productName", "Tên sản phẩm"),
        ("subjectNoun", "Chủ thể (danh từ gọi sản phẩm)"),
        ("classification", "Phân loại"),
        ("pagePath", "Đường dẫn trang sản phẩm"),
        ("shopNameAndMall", "Tên gian hàng và Mall hay không"),
        ("readDate", "Ngày đọc trang"),
        ("niche", "Ngách"),
        ("imagesUsed", "Ảnh dùng trong video"),
        ("productVideoNumber", "Video thứ mấy của sản phẩm"),
        ("videoFocus", "Trọng tâm của video"),
    ];

    /// <summary>The five row fields of scope criteria 1 and 4, with the Vietnamese label each refusal names.</summary>
    public static readonly IReadOnlyList<(string Member, string Label)> RowFields =
    [
        ("subjectNoun", "chủ thể"),
        ("attribute", "thuộc tính"),
        ("value", "giá trị chép đúng trên trang"),
        ("sourceSection", "mục nguồn trên trang"),
        ("screenshot", "tên tệp ảnh chụp màn hình"),
    ];

    /// <summary>Reads and checks the fact sheet file; its folder is the input set.</summary>
    public static FactSheet Read(string path, BannedTermCheck banned)
    {
        ArgumentNullException.ThrowIfNull(banned);
        if (!File.Exists(path))
        {
            throw new PilotRefusal("fact-sheet-missing", $"Không thấy bảng thông tin {path}.");
        }

        var folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
        return Check(File.ReadAllBytes(path), folder, banned);
    }

    public static FactSheet Check(byte[] json, string inputFolder, BannedTermCheck banned)
    {
        ArgumentNullException.ThrowIfNull(banned);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException error)
        {
            throw new PilotRefusal("fact-sheet-unreadable", $"Bảng thông tin không phải JSON hợp lệ: {error.Message}");
        }

        using (document)
        {
            return Check(document.RootElement, inputFolder, banned);
        }
    }

    private static FactSheet Check(JsonElement root, string inputFolder, BannedTermCheck banned)
    {
        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new PilotRefusal("fact-sheet-unreadable", "Bảng thông tin phải là một đối tượng JSON.");
        }

        var known = new HashSet<string>(HeaderFields.Select(h => h.Member).Concat(["formVersion", "facts", "callToAction"]), StringComparer.Ordinal);
        foreach (var member in root.EnumerateObject())
        {
            if (!known.Contains(member.Name))
            {
                throw new PilotRefusal("fact-sheet-unreadable", $"Bảng thông tin có trường lạ \"{member.Name}\"; chỉ dùng các trường của mẫu.");
            }
        }

        if (!root.TryGetProperty("formVersion", out var version) || version.ValueKind != JsonValueKind.Number
            || !version.TryGetInt32(out var versionNumber) || versionNumber != KnownFormVersion)
        {
            throw new PilotRefusal("fact-sheet-unreadable", $"Bảng thông tin phải có \"formVersion\": {KnownFormVersion}.");
        }

        foreach (var (member, label) in HeaderFields)
        {
            if (!Present(root, member))
            {
                throw new PilotRefusal("header-field-missing", $"Thiếu trường đầu bảng \"{label}\" ({member}).");
            }
        }

        var images = root.GetProperty("imagesUsed");
        if (images.ValueKind != JsonValueKind.Array)
        {
            throw new PilotRefusal("header-field-invalid", "Trường \"Ảnh dùng trong video\" (imagesUsed) phải là danh sách tên tệp.");
        }

        var imageNames = new List<string>();
        foreach (var image in images.EnumerateArray())
        {
            if (image.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(image.GetString()))
            {
                throw new PilotRefusal("header-field-missing", "Trường \"Ảnh dùng trong video\" (imagesUsed) có một tên tệp trống.");
            }

            imageNames.Add(InputFiles.Resolve(inputFolder, image.GetString()!.Trim(), "Ảnh dùng trong video"));
        }

        var number = root.GetProperty("productVideoNumber");
        if (number.ValueKind != JsonValueKind.Number || !number.TryGetInt32(out var videoNumber) || videoNumber is < 1 or > 2)
        {
            throw new PilotRefusal("header-field-invalid", "Trường \"Video thứ mấy của sản phẩm\" (productVideoNumber) phải là 1 hoặc 2.");
        }

        var callToAction = 1;
        var held = HeldData.Current;
        if (root.TryGetProperty("callToAction", out var ctaElement) && ctaElement.ValueKind != JsonValueKind.Null)
        {
            if (ctaElement.ValueKind != JsonValueKind.Number || !ctaElement.TryGetInt32(out callToAction)
                || callToAction < 1 || callToAction > held.CallToAction.Count)
            {
                throw new PilotRefusal("header-field-invalid",
                    $"Trường \"callToAction\" phải là số thứ tự trong danh sách câu kêu gọi (1 đến {held.CallToAction.Count}).");
            }
        }

        if (!root.TryGetProperty("facts", out var facts) || facts.ValueKind != JsonValueKind.Array || facts.GetArrayLength() == 0)
        {
            throw new PilotRefusal("fact-row-count", "Bảng thông tin cần từ 1 đến 3 dòng thông tin (facts).");
        }

        if (facts.GetArrayLength() > 3)
        {
            throw new PilotRefusal("fact-row-count", $"Bảng thông tin có {facts.GetArrayLength()} dòng thông tin; tối đa là 3 (F1 đến F3). Bỏ bớt dòng.");
        }

        var rows = new List<FactRow>();
        var index = 0;
        foreach (var row in facts.EnumerateArray())
        {
            index++;
            rows.Add(Row(row, $"F{index}", inputFolder, banned));
        }

        var subjectNoun = Text(root, "subjectNoun", "Đầu bảng");
        var classification = Text(root, "classification", "Đầu bảng");
        banned.Refuse("Đầu bảng (chủ thể)", subjectNoun);
        banned.Refuse("Đầu bảng (phân loại)", classification);

        return new FactSheet(
            Text(root, "videoCode", "Đầu bảng"),
            Text(root, "productName", "Đầu bảng"),
            subjectNoun,
            classification,
            Text(root, "pagePath", "Đầu bảng"),
            Text(root, "shopNameAndMall", "Đầu bảng"),
            Text(root, "readDate", "Đầu bảng"),
            Text(root, "niche", "Đầu bảng"),
            imageNames,
            videoNumber,
            Text(root, "videoFocus", "Đầu bảng"),
            rows,
            callToAction);
    }

    private static FactRow Row(JsonElement row, string label, string inputFolder, BannedTermCheck banned)
    {
        if (row.ValueKind != JsonValueKind.Object)
        {
            throw new PilotRefusal("fact-sheet-unreadable", $"Dòng {label} phải là một đối tượng JSON.");
        }

        var rowKnown = new HashSet<string>(RowFields.Select(r => r.Member).Concat(["label", "override"]), StringComparer.Ordinal);
        foreach (var member in row.EnumerateObject())
        {
            if (!rowKnown.Contains(member.Name))
            {
                throw new PilotRefusal("fact-sheet-unreadable", $"Dòng {label} có trường lạ \"{member.Name}\".");
            }
        }

        if (row.TryGetProperty("label", out var labelElement) && labelElement.ValueKind != JsonValueKind.Null
            && (labelElement.ValueKind != JsonValueKind.String || labelElement.GetString() != label))
        {
            throw new PilotRefusal("fact-sheet-unreadable", $"Dòng thứ {label[1..]} phải mang nhãn {label}.");
        }

        foreach (var (member, fieldLabel) in RowFields)
        {
            if (!Present(row, member))
            {
                throw new PilotRefusal("row-field-missing", $"Dòng {label}: thiếu {fieldLabel} ({member}).");
            }
        }

        var where = $"Dòng {label}";
        var subject = Text(row, "subjectNoun", where);
        var attribute = Text(row, "attribute", where);
        var value = Text(row, "value", where);
        var source = Text(row, "sourceSection", where);
        var screenshot = InputFiles.Resolve(inputFolder, Text(row, "screenshot", where), where);
        string? overrideSentence = null;
        if (row.TryGetProperty("override", out var overrideElement) && overrideElement.ValueKind != JsonValueKind.Null)
        {
            var written = Text(row, "override", where);
            overrideSentence = written.Length == 0 ? null : written;
        }

        banned.Refuse($"{where} (chủ thể)", subject);
        banned.Refuse($"{where} (thuộc tính)", attribute);
        banned.Refuse($"{where} (giá trị)", value);
        if (overrideSentence is not null)
        {
            banned.Refuse($"{where} (câu thay thế)", overrideSentence);
            if (!overrideSentence.Contains(value, StringComparison.Ordinal))
            {
                throw new PilotRefusal("override-value-changed",
                    $"{where}: câu thay thế phải chứa nguyên giá trị \"{value}\" như trên trang, không đổi chữ nào.");
            }
        }

        return new FactRow(label, subject, attribute, value, source, screenshot, overrideSentence);
    }

    private static string Text(JsonElement owner, string member, string where)
    {
        var element = owner.GetProperty(member);
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new PilotRefusal("fact-sheet-unreadable", $"{where}: trường {member} phải là chữ.");
        }

        var value = VietnameseText.Nfc(element.GetString()!.Trim());
        if (VietnameseText.FirstControl(value) is { } control)
        {
            throw new PilotRefusal("control-character", $"{where}: trường {member} có ký tự điều khiển U+{control.Value:X4}.");
        }

        return value;
    }

    private static bool Present(JsonElement owner, string member) =>
        owner.TryGetProperty(member, out var element) && element.ValueKind switch
        {
            JsonValueKind.String => !string.IsNullOrWhiteSpace(element.GetString()),
            JsonValueKind.Array => element.GetArrayLength() > 0,
            JsonValueKind.Number => true,
            _ => false,
        };
}

/// <summary>
/// Operator-named files (design security consideration on operator-named files): a plain file name only, resolved inside the
/// input folder, and present there. A rooted or climbing name is refused before any read.
/// </summary>
public static class InputFiles
{
    public static string Resolve(string inputFolder, string name, string where)
    {
        var plain = name.Length > 0
            && name == Path.GetFileName(name)
            && name is not "." and not ".."
            && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
            && !name.Contains(':', StringComparison.Ordinal)
            && VietnameseText.FirstControl(name) is null;
        if (!plain)
        {
            throw new PilotRefusal("file-name-not-plain", $"{where}: \"{name}\" phải là tên tệp đơn trong thư mục đầu vào, không có đường dẫn.");
        }

        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(inputFolder));
        var full = Path.GetFullPath(Path.Combine(root, name));
        if (!string.Equals(Path.GetDirectoryName(full), root, StringComparison.OrdinalIgnoreCase))
        {
            throw new PilotRefusal("file-name-not-plain", $"{where}: \"{name}\" nằm ngoài thư mục đầu vào.");
        }

        if (!File.Exists(full))
        {
            throw new PilotRefusal("file-missing", $"{where}: không thấy tệp \"{name}\" trong thư mục đầu vào.");
        }

        return name;
    }
}
