using System.Globalization;
using System.Text.Json;

namespace MediaCompany.Pilot.Web;

/// <summary>A header field the read or the paste filled: the fact-sheet member, the value, and whether the operator confirmed it.</summary>
internal sealed record FilledField(string Member, string Value, bool Confirmed);

/// <summary>One specification offered as a fact row (D-013): its key, attribute, value, source section, and whether it is confirmed.</summary>
internal sealed record CandidateRow(string Key, string Attribute, string Value, string SourceSection, bool Confirmed);

/// <summary>An image the read saved under a name the app chose, with the address it came from.</summary>
internal sealed record SavedImage(string Name, string Source);

/// <summary>An image the page declares that the read did not fetch, with the Vietnamese reason; the operator uploads it at step 2.</summary>
internal sealed record SkippedImage(string Address, string Reason);

/// <summary>
/// THE FILL RECORD (MC-18 design decision D-007): one per video in the web folder, beside the video's tick state, never inside the fact
/// sheet. It holds where the values came from (read or paste), the admitted link and the final page address, the time, each filled
/// field with its value and confirmation, the candidate rows, the price and description shown for reference only, the saved images
/// with their source address, the images not fetched with the reason, and the fields not found. A record that cannot be read, or of
/// another form version, is treated as absent: it blocks nothing the MC-17 save allows.
/// </summary>
internal sealed record FillRecord
{
    public const int KnownFormVersion = 1;
    public const string FromRead = "read";
    public const string FromPaste = "paste";

    public int FormVersion { get; init; } = KnownFormVersion;
    public required string Source { get; init; }
    public string? Link { get; init; }
    public string? FinalAddress { get; init; }
    public required string FilledAt { get; init; }
    public IReadOnlyList<FilledField> Fields { get; init; } = [];
    public IReadOnlyList<CandidateRow> Candidates { get; init; } = [];
    public string? Price { get; init; }
    public string? Description { get; init; }
    public IReadOnlyList<SavedImage> Images { get; init; } = [];
    public IReadOnlyList<SkippedImage> NotFetched { get; init; } = [];
    public IReadOnlyList<string> NotFound { get; init; } = [];
}

/// <summary>Builds, stores, confirms and enforces the fill record (D-007, D-008, D-013).</summary>
internal static class FillStore
{
    /// <summary>The fields the page never supplies (scope decision 8); the operator fills them, and the check names the first missing one.</summary>
    public static readonly IReadOnlyList<string> OperatorMembers = ["videoCode", "subjectNoun", "shopNameAndMall", "niche", "imagesUsed", "productVideoNumber", "videoFocus"];

    public const string SpecsLabel = "Chi tiết sản phẩm (dòng thông tin)";
    public const string PriceLabel = "Giá";
    public const string DescriptionLabel = "Mô tả sản phẩm";
    public const string ImagesLabel = "Ảnh của sản phẩm";

    public static string Label(string member) => FactSheetCheck.HeaderFields.FirstOrDefault(f => f.Member == member).Label ?? member;

    /// <summary>The record of a read: name, classification, page address (no query or fragment) and read date, plus candidates.</summary>
    public static FillRecord FromRead(Uri link, Uri final, PageFacts page, DateTimeOffset now, IReadOnlyList<SavedImage> images, IReadOnlyList<SkippedImage> skipped)
    {
        ArgumentNullException.ThrowIfNull(page);
        var fields = new List<FilledField>();
        Add(fields, "productName", page.Name);
        Add(fields, "classification", page.Classification);
        Add(fields, "pagePath", LinkRule.PagePath(final));
        Add(fields, "readDate", now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return new FillRecord
        {
            Source = FillRecord.FromRead,
            Link = link.ToString(),
            FinalAddress = final.ToString(),
            FilledAt = now.ToString("O", CultureInfo.InvariantCulture),
            Fields = fields,
            Candidates = Candidates(page.Specs),
            Price = page.Price,
            Description = page.Description,
            Images = images,
            NotFetched = skipped,
            NotFound = NotFound(fields, page.Specs, page.Price, page.Description, images.Count > 0 || skipped.Count > 0),
        };
    }

    /// <summary>The record of a paste: name, classification and candidates from the same parser; page address and date stay for the operator.</summary>
    public static FillRecord FromPaste(ProductText text, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(text);
        var fields = new List<FilledField>();
        Add(fields, "productName", text.Name);
        Add(fields, "classification", text.Classification);
        return new FillRecord
        {
            Source = FillRecord.FromPaste,
            FilledAt = now.ToString("O", CultureInfo.InvariantCulture),
            Fields = fields,
            Candidates = Candidates(text.Specs),
            Price = text.Price,
            Description = text.Description,
            NotFound = NotFound(fields, text.Specs, text.Price, text.Description, imagesFound: null),
        };
    }

    public static FillRecord? Load(Workspace workspace, string code)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        try
        {
            var record = WebJson.Read<FillRecord>(workspace.FillPath(code));
            return record is { FormVersion: FillRecord.KnownFormVersion } ? record : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static void Save(Workspace workspace, string code, FillRecord record)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        WebJson.Write(workspace.FillPath(code), record);
    }

    /// <summary>Sets one confirmation: a header member or a candidate key.</summary>
    public static FillRecord Confirm(Workspace workspace, string code, string? key)
    {
        var record = Load(workspace, code)
            ?? throw new WebRefusal("fill-missing", "Video này chưa có ô nào được điền từ trang hay từ chữ đã dán, nên không có gì để xác nhận.", 409);
        key = (key ?? string.Empty).Trim();
        FillRecord updated;
        if (record.Fields.Any(f => f.Member == key))
        {
            updated = record with { Fields = record.Fields.Select(f => f.Member == key ? f with { Confirmed = true } : f).ToList() };
        }
        else if (record.Candidates.Any(c => c.Key == key))
        {
            updated = record with { Candidates = record.Candidates.Select(c => c.Key == key ? c with { Confirmed = true } : c).ToList() };
        }
        else
        {
            throw new WebRefusal("fill-field-unknown", "Ô cần xác nhận không có trong lần điền gần nhất. Tải lại trang (phím F5) rồi bấm lại.");
        }

        Save(workspace, code, updated);
        return updated;
    }

    /// <summary>
    /// The save refusal of D-007: the first filled field (header fields in the sheet's order, then fact rows) whose submitted value still
    /// equals the filled value and is not confirmed. A value the operator edited counts as typed and needs no confirmation.
    /// </summary>
    public static void RequireConfirmed(FillRecord? record, byte[] normalisedSheet)
    {
        if (record is null)
        {
            return;
        }

        var source = record.Source == FillRecord.FromRead ? "trang sản phẩm" : "chữ đã dán";
        using var document = JsonDocument.Parse(normalisedSheet);
        var root = document.RootElement;
        foreach (var (member, label) in FactSheetCheck.HeaderFields)
        {
            var field = record.Fields.FirstOrDefault(f => f.Member == member);
            if (field is { Confirmed: false } && Same(Text(root, member), field.Value))
            {
                throw new WebRefusal("fill-unconfirmed",
                    $"Ô \"{label}\" được điền từ {source} nhưng chưa xác nhận. Đối chiếu ô đó với trang sản phẩm, bấm \"Xác nhận\" bên cạnh ô (hoặc sửa lại cho đúng), rồi bấm lưu.");
            }
        }

        if (root.TryGetProperty("facts", out var facts) && facts.ValueKind == JsonValueKind.Array)
        {
            var index = 0;
            foreach (var row in facts.EnumerateArray())
            {
                index++;
                var attribute = Text(row, "attribute");
                var value = Text(row, "value");
                var matches = record.Candidates.Where(c => Same(attribute, c.Attribute) && Same(value, c.Value)).ToList();
                if (matches.Count > 0 && !matches.Any(c => c.Confirmed))
                {
                    throw new WebRefusal("fill-unconfirmed",
                        $"Dòng F{index} ({matches[0].Attribute}: {matches[0].Value}) được điền từ {source} nhưng chưa xác nhận. Đối chiếu dòng đó với trang sản phẩm, bấm \"Xác nhận\" ở dòng (hoặc sửa lại cho đúng), rồi bấm lưu.");
                }
            }
        }
    }

    private static void Add(List<FilledField> fields, string member, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            fields.Add(new FilledField(member, VietnameseText.Nfc(value.Trim()), false));
        }
    }

    private static List<CandidateRow> Candidates(IReadOnlyList<SpecRow> specs) =>
        specs.Select((s, i) => new CandidateRow($"dong-{i + 1}", VietnameseText.Nfc(s.Attribute), VietnameseText.Nfc(s.Value), VietnameseText.Nfc(s.SourceSection), false)).ToList();

    private static List<string> NotFound(List<FilledField> fields, IReadOnlyList<SpecRow> specs, string? price, string? description, bool? imagesFound)
    {
        var missing = new[] { "productName", "classification", "pagePath", "readDate" }
            .Where(m => fields.All(f => f.Member != m)).Select(Label).ToList();
        if (specs.Count == 0)
        {
            missing.Add(SpecsLabel);
        }

        if (string.IsNullOrWhiteSpace(price))
        {
            missing.Add(PriceLabel);
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            missing.Add(DescriptionLabel);
        }

        if (imagesFound is false)
        {
            missing.Add(ImagesLabel);
        }

        return missing;
    }

    private static string? Text(JsonElement owner, string member) =>
        owner.ValueKind == JsonValueKind.Object && owner.TryGetProperty(member, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

    private static bool Same(string? submitted, string filled) =>
        submitted is not null && string.Equals(VietnameseText.Nfc(submitted.Trim()), VietnameseText.Nfc(filled.Trim()), StringComparison.Ordinal);
}

/// <summary>
/// THE READ SWITCH (MC-18 design decision D-003; scope decision 7): the one setting that turns the read on or off. It lives in its own
/// file in the app folder at form version 1, not in the app record and not in the toolchain's settings. It starts off; a missing,
/// unreadable or other-version file reads as off. The operator sets it on the settings view; paste mode and typing work either way.
/// </summary>
internal sealed record ReadSwitchFile(int FormVersion, bool ReadOn);

internal static class ReadSwitch
{
    public const string FileName = "doc-trang-san-pham.json";
    public const int KnownFormVersion = 1;

    public static string PathIn(string appFolder) => Path.Combine(appFolder, FileName);

    public static bool IsOn(string appFolder)
    {
        try
        {
            return WebJson.Read<ReadSwitchFile>(PathIn(appFolder)) is { FormVersion: KnownFormVersion, ReadOn: true };
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void Set(string appFolder, bool on) => WebJson.Write(PathIn(appFolder), new ReadSwitchFile(KnownFormVersion, on));
}
