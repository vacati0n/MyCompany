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
/// THE KIT 04 CHECKLIST, PRE-FILLED (scope criterion 34). Every item is listed with its kit wording. Only the items the toolchain can
/// decide are ticked, and only when its own check passed: B3 (banned classes b and d absent from every sentence), B5 (classes a and e
/// absent from every sentence), B8 (the overlay drawn at the first and the last checked frame). Every other item is left for the CEO.
/// </summary>
public static class Checklist
{
    public static string Render(HeldData held, string videoCode, IReadOnlyDictionary<string, RunCheck> decided)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(decided);
        var text = new StringBuilder();
        text.Append("# Danh sách duyệt (mẫu 04) — ").Append(videoCode).Append("\n\n");
        text.Append("Công cụ chỉ đánh dấu mục nó tự kiểm được và chỉ khi kiểm đạt (B3, B5, B8). Mọi mục khác do CEO kiểm. Thiếu một mục thì không đăng.\n\n");
        text.Append("| # | Mục | Ai kiểm | Kết quả |\n|---|---|---|---|\n");
        foreach (var item in held.Checklist)
        {
            if (decided.TryGetValue(item.Id, out var check))
            {
                text.Append(CultureInfo.InvariantCulture, $"| {item.Id} | {item.Wording} | công cụ | {(check.Passed ? "☑" : "☐")} {check.Detail} |\n");
            }
            else
            {
                text.Append(CultureInfo.InvariantCulture, $"| {item.Id} | {item.Wording} | CEO | ☐ |\n");
            }
        }

        return text.ToString();
    }

    /// <summary>The tool-decided items from the script's sentences and the overlay check.</summary>
    public static IReadOnlyDictionary<string, RunCheck> Decide(HeldData held, BannedTermCheck banned, Script script, RunCheck overlay)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(banned);
        ArgumentNullException.ThrowIfNull(script);
        var decided = new Dictionary<string, RunCheck>(StringComparer.Ordinal);
        foreach (var (item, classes) in held.ChecklistTicked)
        {
            if (classes.Count == 0)
            {
                continue;
            }

            var findings = script.Sentences.SelectMany(s => banned.Find($"Câu {s.Number}", s.Text))
                .Where(f => classes.Contains(f.ClassId, StringComparer.Ordinal)).ToList();
            decided[item] = new RunCheck(item, findings.Count == 0, findings.Count == 0
                ? $"không có từ nhóm {string.Join(", ", classes)} trong {script.Sentences.Count} câu"
                : $"có \"{findings[0].Term}\" ở {findings[0].Where}");
        }

        if (held.ChecklistTicked.ContainsKey("B8"))
        {
            decided["B8"] = overlay with { Name = "B8" };
        }

        return decided;
    }
}

/// <summary>
/// APPROVAL BY HASH (design D-012). The CEO approves a ready package by its video's SHA-256; an approval of a held package or of a
/// different digest is refused, and verify reports an approval void when the video no longer hashes to the approved digest.
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

        var video = Path.Combine(packageFolder, manifest.VideoFile);
        var actual = File.Exists(video) ? MediaTool.Sha256File(video) : "(không có tệp video)";
        if (!string.Equals(actual, sha256.Trim(), StringComparison.OrdinalIgnoreCase)
            || !string.Equals(actual, manifest.VideoSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new PilotRefusal("approval-hash-mismatch",
                $"SHA-256 của video hiện là {actual}; giá trị được duyệt là {sha256.Trim()}; tệp kê ghi {manifest.VideoSha256}. Ba giá trị phải trùng nhau; video đã đổi thì không duyệt.");
        }

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
        var video = Path.Combine(packageFolder, manifest.VideoFile);
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

        return (manifest, approval, actual);
    }
}
