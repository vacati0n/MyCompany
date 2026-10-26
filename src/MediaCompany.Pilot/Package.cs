using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaCompany.Pilot;

/// <summary>One named check of a run and its result.</summary>
public sealed record RunCheck(string Name, bool Passed, string Detail);

/// <summary>One input file copied into the package, with its role and digest.</summary>
public sealed record PackageInput(string Role, string Name, string Sha256);

/// <summary>The package manifest (design D-012 and D-013): the video's digest, every input's digest, the tools, the face and the checks.</summary>
public sealed record PackageManifest
{
    public required int FormVersion { get; init; }
    public required string VideoCode { get; init; }
    public required string Status { get; init; }
    public required IReadOnlyList<string> HeldReasons { get; init; }
    public required string VideoFile { get; init; }
    public required string VideoSha256 { get; init; }
    public required string ScriptSha256 { get; init; }
    public required string RecordingSha256 { get; init; }
    public required IReadOnlyList<PackageInput> Inputs { get; init; }
    public required string MediaToolPath { get; init; }
    public required string MediaToolSha256 { get; init; }
    public required string ProbeToolPath { get; init; }
    public required string ProbeToolSha256 { get; init; }
    public required string MediaToolVersionLine { get; init; }
    public required string FacePath { get; init; }
    public required string FaceSha256 { get; init; }
    public required string FaceManifestLine { get; init; }
    public required IReadOnlyList<RunCheck> Checks { get; init; }
    public required string? LedgerEntryHash { get; init; }
    public required string ProducedAt { get; init; }
}

/// <summary>The CEO's approval of one package: the video code, the video's SHA-256 as approved, the name and the date.</summary>
public sealed record ApprovalRecord
{
    public required int FormVersion { get; init; }
    public required string VideoCode { get; init; }
    public required string VideoSha256 { get; init; }
    public required string ApprovedBy { get; init; }
    public required string ApprovedOn { get; init; }
}

public static class PackageFiles
{
    public const string Video = "video.mp4";
    public const string Manifest = "manifest.json";
    public const string Approval = "approval.json";
    public const string RunRecord = "run-record.json";
    public const string Checklist = "checklist.md";
    public const string Script = "script.json";
    public const string ScriptText = "script.md";
    public const string Captions = "captions.json";
    public const string LedgerEntry = "ledger-entry.json";
    public const string Frame = "frame.png";
    public const string Inputs = "inputs";

    public static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void WriteJson<T>(string path, T value) =>
        File.WriteAllBytes(path, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json).ReplaceLineEndings("\n") + "\n"));

    public static T ReadJson<T>(string path, string code, string what)
    {
        if (!File.Exists(path))
        {
            throw new PilotRefusal(code, $"Không thấy {what} ({path}).");
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllBytes(path), Json) ?? throw new PilotRefusal(code, $"{what} trống.");
        }
        catch (JsonException error)
        {
            throw new PilotRefusal(code, $"{what} không đọc được: {error.Message}");
        }
    }
}

/// <summary>
/// THE KIT 04 CHECKLIST, PRE-FILLED (scope criterion 34, decision record entry 1309). Every item is listed with its kit wording. The
/// tool ticks only B8 (the overlay drawn at the first and the last checked frame), and only when that check passed. For B3 (banned
/// classes b and d) and B5 (classes a and e) the tool states what its screen of every sentence found and leaves the box unticked:
/// the screen is a closed word list, so the CEO ticks those two. Every other item is left for the CEO.
/// </summary>
public static class Checklist
{
    public static string Render(HeldData held, string videoCode, IReadOnlyDictionary<string, RunCheck> decided)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(decided);
        var text = new StringBuilder();
        text.Append("# Danh sách duyệt (mẫu 04) — ").Append(videoCode).Append("\n\n");
        text.Append("Công cụ chỉ đánh dấu B8, và chỉ khi nó tự kiểm đạt. Với B3 và B5 công cụ ghi điều nó soát được nhưng không đánh dấu: CEO tự kiểm và tự tích. Mọi mục khác do CEO kiểm. Thiếu một mục thì không đăng.\n\n");
        text.Append("| # | Mục | Ai kiểm | Kết quả |\n|---|---|---|---|\n");
        foreach (var item in held.Checklist)
        {
            if (!decided.TryGetValue(item.Id, out var check))
            {
                text.Append(CultureInfo.InvariantCulture, $"| {item.Id} | {item.Wording} | CEO | ☐ |\n");
            }
            else if (held.ChecklistNoted.ContainsKey(item.Id))
            {
                text.Append(CultureInfo.InvariantCulture, $"| {item.Id} | {item.Wording} | CEO (công cụ đã soát) | ☐ {check.Detail} |\n");
            }
            else
            {
                text.Append(CultureInfo.InvariantCulture, $"| {item.Id} | {item.Wording} | công cụ | {(check.Passed ? "☑" : "☐")} {check.Detail} |\n");
            }
        }

        return text.ToString();
    }

    /// <summary>
    /// The tool's entries: B8 from the overlay check (ticked when it passed); B3 and B5 from the banned-term screen of the script's
    /// sentences, never ticked, each detail saying what the screen found.
    /// </summary>
    public static IReadOnlyDictionary<string, RunCheck> Decide(HeldData held, BannedTermCheck banned, Script script, RunCheck overlay)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(banned);
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(overlay);
        var decided = new Dictionary<string, RunCheck>(StringComparer.Ordinal);
        foreach (var (item, classes) in held.ChecklistNoted)
        {
            var findings = script.Sentences.SelectMany(s => banned.Find($"Câu {s.Number}", s.Text))
                .Where(f => classes.Contains(f.ClassId, StringComparer.Ordinal)).ToList();
            decided[item] = new RunCheck(item, false, findings.Count == 0
                ? $"Công cụ không thấy từ nhóm {string.Join(", ", classes)} trong {script.Sentences.Count} câu; danh sách từ có hạn, CEO tự kiểm và tự tích."
                : $"Công cụ thấy \"{findings[0].Term}\" ở {findings[0].Where}; CEO kiểm, không đăng khi còn từ này.");
        }

        if (held.ChecklistTicked.Contains("B8", StringComparer.Ordinal))
        {
            decided["B8"] = overlay with { Name = "B8" };
        }

        return decided;
    }
}

/// <summary>
/// APPROVAL BY HASH (design D-012). The CEO approves a ready package by its video's SHA-256; an approval of a held package or of a
/// different digest is refused, and verify reports an approval void when the video no longer hashes to the approved digest. The
/// manifest is not trusted alone (correction request 8): its video file must be a plain name inside the package folder, and after
/// the hash checks the package must agree with its ledger entry (see RequireLedgerAgreement).
/// </summary>
public static class Approval
{
    public static ApprovalRecord Approve(string packageFolder, string approvedBy, string sha256, DateTimeOffset on)
    {
        var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(packageFolder, PackageFiles.Manifest), "package-unreadable", "tệp kê của gói");
        if (!string.Equals(manifest.Status, "ready", StringComparison.Ordinal))
        {
            throw new PilotRefusal("package-held",
                $"Gói {manifest.VideoCode} đang bị giữ ({string.Join("; ", manifest.HeldReasons)}); không duyệt được.");
        }

        if (string.IsNullOrWhiteSpace(approvedBy))
        {
            throw new PilotRefusal("approval-name-missing", "Cần tên người duyệt (--name).");
        }

        var video = VideoPath(packageFolder, manifest);
        var actual = File.Exists(video) ? MediaTool.Sha256File(video) : "(không có tệp video)";
        if (!string.Equals(actual, sha256.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(actual, manifest.VideoSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new PilotRefusal("approval-hash-mismatch",
                $"SHA-256 của video hiện là {actual}; giá trị được duyệt là {sha256.Trim()}; tệp kê ghi {manifest.VideoSha256}. Ba giá trị phải trùng nhau; video đã đổi thì không duyệt.");
        }

        RequireLedgerAgreement(packageFolder, manifest, actual);
        var record = new ApprovalRecord
        {
            FormVersion = 1,
            VideoCode = manifest.VideoCode,
            VideoSha256 = actual,
            ApprovedBy = approvedBy.Trim(),
            ApprovedOn = on.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        };
        PackageFiles.WriteJson(Path.Combine(packageFolder, PackageFiles.Approval), record);
        return record;
    }

    /// <summary>Recomputes the video's digest; refuses with approval-void when it differs from the manifest or the approval.</summary>
    public static (PackageManifest Manifest, ApprovalRecord? Approval, string Sha256) Verify(string packageFolder)
    {
        var manifest = PackageFiles.ReadJson<PackageManifest>(Path.Combine(packageFolder, PackageFiles.Manifest), "package-unreadable", "tệp kê của gói");
        var video = VideoPath(packageFolder, manifest);
        var actual = File.Exists(video) ? MediaTool.Sha256File(video) : "(không có tệp video)";
        var approvalPath = Path.Combine(packageFolder, PackageFiles.Approval);
        var approval = File.Exists(approvalPath)
            ? PackageFiles.ReadJson<ApprovalRecord>(approvalPath, "package-unreadable", "tệp duyệt")
            : null;
        if (!string.Equals(actual, manifest.VideoSha256, StringComparison.OrdinalIgnoreCase)
            || (approval is not null && !string.Equals(actual, approval.VideoSha256, StringComparison.OrdinalIgnoreCase)))
        {
            throw new PilotRefusal("approval-void",
                $"Video {manifest.VideoCode} có SHA-256 {actual}, khác giá trị đã ghi ({manifest.VideoSha256}{(approval is null ? string.Empty : $", đã duyệt {approval.VideoSha256}")}). Duyệt cũ mất hiệu lực; không đăng tệp này.");
        }

        RequireLedgerAgreement(packageFolder, manifest, actual);
        return (manifest, approval, actual);
    }

    /// <summary>The manifest's video file, refused unless it is a plain file name that stays inside the package folder.</summary>
    public static string VideoPath(string packageFolder, PackageManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        var name = manifest.VideoFile ?? string.Empty;
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(packageFolder));
        var plain = name.Length > 0 && name == Path.GetFileName(name) && name is not "." and not ".."
            && name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 && !name.Contains(':', StringComparison.Ordinal)
            && VietnameseText.FirstControl(name) is null;
        var full = plain ? Path.GetFullPath(Path.Combine(root, name)) : string.Empty;
        if (!plain || !string.Equals(Path.GetDirectoryName(full), root, StringComparison.OrdinalIgnoreCase))
        {
            throw new PilotRefusal("package-file-not-plain", $"Tệp kê của gói ghi tệp video \"{name}\": phải là tên tệp đơn trong thư mục gói. Gói đã bị sửa; không duyệt.");
        }

        return full;
    }

    /// <summary>
    /// Refuses a package that does not agree with its ledger entry. The cases, each refused with package-ledger-mismatch: no
    /// ledger-entry copy in the package (a held package has none); a copy whose entry hash does not recompute; a copy whose video
    /// digest differs from the video's; a manifest whose status is not ready or whose ledger-entry hash differs from the copy's; no
    /// ledger in the pilot folder that holds the package (the folder above runs); that ledger's chain not holding the entry.
    /// </summary>
    public static void RequireLedgerAgreement(string packageFolder, PackageManifest manifest, string videoSha256)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        static void Refuse(string why) => throw new PilotRefusal("package-ledger-mismatch", why + " Gói và sổ theo dõi không khớp; không duyệt, không đăng.");
        var copyPath = Path.Combine(packageFolder, PackageFiles.LedgerEntry);
        if (!File.Exists(copyPath))
        {
            Refuse($"Gói {manifest.VideoCode} không có bản sao mục sổ ({PackageFiles.LedgerEntry}); chỉ gói sẵn sàng mới được ghi vào sổ.");
        }

        var entry = PackageFiles.ReadJson<LedgerEntry>(copyPath, "package-ledger-mismatch", "bản sao mục sổ của gói");
        if (!string.Equals(entry.EntryHash, entry.ComputeHash(), StringComparison.Ordinal))
        {
            Refuse($"Bản sao mục sổ của gói {manifest.VideoCode} đã bị sửa (mã băm mục không khớp).");
        }

        if (!string.Equals(entry.VideoSha256, videoSha256, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(entry.VideoCode, manifest.VideoCode, StringComparison.Ordinal))
        {
            Refuse($"Mục sổ ghi video {entry.VideoCode} với SHA-256 {entry.VideoSha256}; gói là {manifest.VideoCode} với SHA-256 {videoSha256}.");
        }

        if (!string.Equals(manifest.Status, "ready", StringComparison.Ordinal)
            || !string.Equals(manifest.LedgerEntryHash, entry.EntryHash, StringComparison.Ordinal))
        {
            Refuse($"Tệp kê ghi trạng thái \"{manifest.Status}\" và mục sổ {manifest.LedgerEntryHash ?? "(không có)"}; mục sổ của gói là {entry.EntryHash}.");
        }

        var runs = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(Path.GetFullPath(packageFolder)));
        var pilot = runs is null || !string.Equals(Path.GetFileName(runs), "runs", StringComparison.OrdinalIgnoreCase) ? null : Path.GetDirectoryName(runs);
        if (pilot is null || !File.Exists(Path.Combine(pilot, Ledger.FileName)))
        {
            Refuse($"Gói {manifest.VideoCode} không nằm trong thư mục runs của một thư mục thí điểm có sổ theo dõi.");
        }

        var ledger = Ledger.Open(pilot!, HeldData.Current);
        if (!ledger.Entries.Any(e => string.Equals(e.EntryHash, entry.EntryHash, StringComparison.Ordinal)))
        {
            Refuse($"Sổ theo dõi {ledger.FilePath} không có mục của gói {manifest.VideoCode} ({entry.EntryHash}).");
        }
    }
}
