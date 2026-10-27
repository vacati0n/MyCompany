using System.Text;
using System.Text.Json;

namespace MediaCompany.Pilot.Web;

/// <summary>What setup found: the composed settings, the toolchain executable and its digest, each with where it was found.</summary>
public sealed record SetupPlan(PilotSettings Settings, string ToolchainPath, string ToolchainSha256, string ExamplePath);

/// <summary>
/// THE FIRST-RUN SETUP (design decision D-004, Design Gate option O-012, Scope Gate decision 1310 item 2). The operator gives the pilot
/// folder and the CEO's name only. ffmpeg and ffprobe are found in the WinGet links folder or on the search path and must hash to the
/// digests in the repository's example settings file; the face is found through its font-manifest line; the toolchain executable is
/// found in its own build output; every CEO number is copied from the example settings file and cannot be typed. The result is
/// validated by the toolchain's own reader before and after it is written.
/// </summary>
public static class Setup
{
    public const string SettingsFileName = "cai-dat.json";
    public const string ExampleRelativePath = "pilot-affiliate-vn/mc-16/vi-du-cai-dat.json";
    public const string ToolchainName = "MediaCompany.Pilot.exe";

    public static string DefaultPilotFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MediaCompanyPilot");

    /// <summary>The repository root: the nearest folder above the app holding the solution file.</summary>
    public static string RepositoryRoot(string from)
    {
        for (var directory = new DirectoryInfo(from); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new WebRefusal("setup-repository-missing",
            $"Không thấy thư mục kho mã (tệp MediaCompany.slnx) phía trên {from}. Chạy ứng dụng từ bản dựng trong kho mã, hoặc báo người phụ trách kỹ thuật.");
    }

    /// <summary>The folders searched for ffmpeg and ffprobe: the WinGet links folder, then the search path.</summary>
    public static IReadOnlyList<string> ToolFolders()
    {
        var folders = new List<string>
        {
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft", "WinGet", "Links"),
        };
        folders.AddRange((Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        return folders;
    }

    /// <summary>The folders searched for the font manifest.</summary>
    public static IReadOnlyList<string> FontManifestCandidates(PilotSettings example)
    {
        ArgumentNullException.ThrowIfNull(example);
        return
        [
            example.Face.Manifest,
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "MediaCompanyRun", "graphics", "manifests", "font.sha256"),
        ];
    }

    /// <summary>Composes the settings for a pilot folder; refuses, naming the item, anything missing or with a different digest.</summary>
    public static SetupPlan Compose(string repositoryRoot, string pilotFolderText, IReadOnlyList<string> toolFolders,
        IReadOnlyList<string>? manifestCandidates = null, string? configuration = null)
    {
        ArgumentNullException.ThrowIfNull(toolFolders);
        var examplePath = Path.Combine(repositoryRoot, ExampleRelativePath);
        var example = ReadExample(examplePath);
        var pilotText = VietnameseText.Nfc((pilotFolderText ?? string.Empty).Trim());
        if (pilotText.Length == 0 || VietnameseText.FirstControl(pilotText) is not null || !Path.IsPathFullyQualified(pilotText))
        {
            throw new WebRefusal("setup-pilot-folder",
                "Thư mục thí điểm phải là một đường dẫn đầy đủ, ví dụ C:\\Users\\ten\\MediaCompanyPilot. Gõ lại đường dẫn rồi bấm lưu.");
        }

        var pilotFolder = Path.TrimEndingDirectorySeparator(Path.GetFullPath(pilotText));
        var mediaTool = FindTool("ffmpeg.exe", example.MediaTool, toolFolders);
        var probeTool = FindTool("ffprobe.exe", example.ProbeTool, toolFolders);
        var face = FindFace(example, manifestCandidates ?? FontManifestCandidates(example));
        var (toolchain, toolchainSha) = FindToolchain(repositoryRoot, configuration);

        var settings = example with
        {
            RepositoryRoot = repositoryRoot,
            PilotFolder = pilotFolder,
            MediaTool = mediaTool,
            ProbeTool = probeTool,
            Face = face,
        };

        // The toolchain's own checks: the CEO's held values, the motion name, the pilot folder outside the repository.
        settings.Validate(HeldData.Current);
        _ = Pilot.Face.Admit(settings.Face);
        return new SetupPlan(settings, toolchain, toolchainSha, examplePath);
    }

    /// <summary>Writes the settings file into the pilot folder and reads it back through the toolchain's reader.</summary>
    public static string WriteSettings(PilotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(settings.PilotFolder);
        var path = Path.Combine(settings.PilotFolder, SettingsFileName);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, PilotSettings.Json).ReplaceLineEndings("\n") + "\n", new UTF8Encoding(false));
        _ = PilotSettings.Read(path);
        return path;
    }

    public static PilotSettings ReadExample(string examplePath)
    {
        if (!File.Exists(examplePath))
        {
            throw new WebRefusal("setup-example-missing",
                $"Không thấy tệp cài đặt mẫu {examplePath}, nơi giữ các số CEO đã duyệt. Báo người phụ trách kỹ thuật.");
        }

        try
        {
            return JsonSerializer.Deserialize<PilotSettings>(File.ReadAllBytes(examplePath), PilotSettings.Json)
                ?? throw new WebRefusal("setup-example-unreadable", $"Tệp cài đặt mẫu {examplePath} trống. Báo người phụ trách kỹ thuật.");
        }
        catch (JsonException error)
        {
            throw new WebRefusal("setup-example-unreadable", $"Tệp cài đặt mẫu {examplePath} không đọc được ({error.Message}). Báo người phụ trách kỹ thuật.");
        }
    }

    private static ToolSetting FindTool(string fileName, ToolSetting approved, IReadOnlyList<string> folders)
    {
        var candidates = folders.Select(f => Path.Combine(f, fileName)).Where(File.Exists).ToList();
        if (candidates.Count == 0)
        {
            throw new WebRefusal("setup-tool-missing",
                $"Không thấy {fileName} trong thư mục WinGet Links hay trên đường dẫn tìm kiếm ({string.Join("; ", folders.Take(3))}…). Cài ffmpeg theo hướng dẫn, hoặc báo người phụ trách kỹ thuật.");
        }

        foreach (var candidate in candidates)
        {
            if (string.Equals(MediaTool.Sha256File(candidate), approved.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return new ToolSetting { Path = Path.GetFullPath(candidate), Sha256 = approved.Sha256 };
            }
        }

        throw new WebRefusal("setup-tool-digest",
            $"{fileName} tìm thấy ở {candidates[0]} có SHA-256 {MediaTool.Sha256File(candidates[0])}, khác bản đã duyệt ({approved.Sha256}). Không dùng công cụ lạ; báo người phụ trách kỹ thuật.");
    }

    private static FaceSetting FindFace(PilotSettings example, IReadOnlyList<string> manifests)
    {
        var entry = example.Face.ManifestEntry.Trim();
        var entryPath = entry.Split('*', 2) is [_, var relative] ? relative : string.Empty;
        foreach (var manifest in manifests.Where(File.Exists))
        {
            if (!File.ReadAllLines(manifest).Any(l => string.Equals(l.Trim(), entry, StringComparison.Ordinal)))
            {
                continue;
            }

            var font = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(manifest)!, "..", "font", entryPath));
            var path = File.Exists(example.Face.Path) ? example.Face.Path : font;
            return new FaceSetting { Path = path, Manifest = manifest, ManifestEntry = entry };
        }

        throw new WebRefusal("setup-face-missing",
            $"Không thấy tệp kê phông chữ có dòng \"{entry}\" (đã tìm: {string.Join("; ", manifests)}). Cài phông chữ theo hướng dẫn, hoặc báo người phụ trách kỹ thuật.");
    }

    /// <summary>The toolchain executable in its own build output, preferring the configuration this app was built in.</summary>
    public static (string Path, string Sha256) FindToolchain(string repositoryRoot, string? configuration = null)
    {
        var own = configuration ?? (AppContext.BaseDirectory.Contains($"{Path.DirectorySeparatorChar}Release{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase) ? "Release" : "Debug");
        var candidates = new[] { own, own == "Debug" ? "Release" : "Debug" }
            .Select(c => Path.Combine(repositoryRoot, "src", "MediaCompany.Pilot", "bin", c, "net10.0", ToolchainName))
            .ToList();
        var found = candidates.FirstOrDefault(File.Exists)
            ?? throw new WebRefusal("setup-toolchain-missing",
                $"Không thấy bộ công cụ {ToolchainName} ở {candidates[0]}. Người phụ trách kỹ thuật cần dựng (build) kho mã một lần, rồi bấm lưu lại.");
        return (found, MediaTool.Sha256File(found));
    }
}
