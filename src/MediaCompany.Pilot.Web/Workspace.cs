namespace MediaCompany.Pilot.Web;

/// <summary>Per-video state the app keeps in the web folder: the script's ticked lines, the ticked checklist items, the last package.</summary>
public sealed record VideoState
{
    public const int KnownFormVersion = 1;

    public int FormVersion { get; init; } = KnownFormVersion;
    public string? ScriptSha256 { get; init; }
    public IReadOnlyList<int> ScriptTicks { get; init; } = [];
    public IReadOnlyList<string> ChecklistTicks { get; init; } = [];
    public string? LastPackage { get; init; }
}

/// <summary>
/// THE PILOT FOLDER LAYOUT (design decision D-008). Beside the toolchain's own runs, refused and ledger files the app adds: inputs/[video
/// code]/ (the per-video input folder the toolchain reads), takes/ (raw browser takes), web/ (tick state, the last package, the log,
/// uploads in progress). The video code must pass the toolchain's plain-name rule. The app never writes the ledger, a package folder or
/// the refused folder.
/// </summary>
public sealed class Workspace(AppRecord record)
{
    public const string FactSheetFileName = "bang-thong-tin.json";
    public const string ScriptFileName = PackageFiles.Script;
    public const string RecordingStem = "recording";
    public const string ClipStem = "presenter";

    public AppRecord Record { get; } = record;

    public string PilotFolder => Record.PilotFolder;

    public string InputsRoot => Path.Combine(PilotFolder, "inputs");

    public string TakesFolder => Path.Combine(PilotFolder, "takes");

    public string WebFolder => Path.Combine(PilotFolder, "web");

    public string UploadFolder => Path.Combine(WebFolder, "uploads");

    public string RunsFolder => Path.Combine(PilotFolder, "runs");

    /// <summary>The video code, NFC and trimmed, refused unless it is a plain name.</summary>
    public string Code(string? raw) => PlainName.Require(raw, "Mã video", InputsRoot);

    public string InputFolder(string code)
    {
        var folder = Path.Combine(InputsRoot, Code(code));
        Directory.CreateDirectory(folder);
        return folder;
    }

    public string FactSheetPath(string code) => Path.Combine(InputFolder(code), FactSheetFileName);

    public string ScriptPath(string code) => Path.Combine(InputFolder(code), ScriptFileName);

    public string TimingPath(string code) => Path.Combine(InputFolder(code), CaptionTiming.TimingFileName);

    public IReadOnlyList<string> VideoCodes() =>
        Directory.Exists(InputsRoot)
            ? Directory.EnumerateDirectories(InputsRoot).Select(Path.GetFileName).OfType<string>().Order(StringComparer.Ordinal).ToList()
            : [];

    public VideoState LoadState(string code)
    {
        var state = WebJson.Read<VideoState>(StatePath(code));
        return state is { FormVersion: VideoState.KnownFormVersion } ? state : new VideoState();
    }

    public void SaveState(string code, VideoState state) => WebJson.Write(StatePath(code), state);

    /// <summary>The files in the input folder whose extension is in the list (the toolchain's recording or clip extensions).</summary>
    public IReadOnlyList<string> FilesWith(string code, IReadOnlyList<string> extensions) =>
        Directory.EnumerateFiles(InputFolder(code))
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Order(StringComparer.Ordinal)
            .ToList();

    /// <summary>The raw browser take of the video, if one is held.</summary>
    public string? TakePath(string code)
    {
        var stem = $"{Code(code)}.take";
        return Directory.Exists(TakesFolder)
            ? Directory.EnumerateFiles(TakesFolder).FirstOrDefault(f => Path.GetFileNameWithoutExtension(f) == stem)
            : null;
    }

    /// <summary>The last package folder the app's own produce wrote for the video, when it is still inside the runs folder.</summary>
    public string? LastPackage(string code)
    {
        var last = LoadState(code).LastPackage;
        if (last is null)
        {
            return null;
        }

        var runs = Path.TrimEndingDirectorySeparator(Path.GetFullPath(RunsFolder));
        var full = Path.TrimEndingDirectorySeparator(Path.GetFullPath(last));
        return string.Equals(Path.GetDirectoryName(full), runs, StringComparison.OrdinalIgnoreCase) && Directory.Exists(full) ? full : null;
    }

    private string StatePath(string code) => Path.Combine(WebFolder, $"video-{Code(code)}.json");
}
