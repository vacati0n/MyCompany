using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaCompany.Pilot;

/// <summary>One recorded tool start: which tool, its digest, the exact arguments, the exit status and the time taken.</summary>
public sealed record ToolInvocation(string Purpose, string Tool, string Sha256, IReadOnlyList<string> Arguments, int ExitCode, double Seconds);

/// <summary>What the probe tool reports about one media file.</summary>
public sealed record MediaFacts(double DurationSeconds, bool HasVideo, bool HasAudio, int Width, int Height, int Rotation)
{
    /// <summary>The displayed width and height after the stream's rotation.</summary>
    public (int Width, int Height) Displayed => Math.Abs(Rotation) % 180 == 90 ? (Height, Width) : (Width, Height);
}

/// <summary>
/// THE MEDIA TOOL INVOKER (design D-005 and D-013). Every start of the installed ffmpeg or ffprobe goes through here: the
/// executable's SHA-256 is checked against the settings before the first start; arguments are an argument list, never a command
/// line; the environment is cleared to SystemRoot, windir and a PATH of the system folder; the process runs in a job that ends
/// it and anything it started on close, under the configured bound; every input is preceded by "-protocol_whitelist file", and
/// an argument naming a URL scheme is refused before any start. Each start is appended to the invoked-tool log.
/// </summary>
public sealed class MediaTool
{
    private static readonly Regex Scheme = new(@"^[A-Za-z][A-Za-z0-9+.\-]+://|^[A-Za-z]{2,}:(?![\\/])", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private readonly PilotSettings _settings;
    private readonly List<ToolInvocation> _log = [];

    public MediaTool(PilotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        MediaToolDigest = CheckDigest("mediaTool", settings.MediaTool);
        ProbeToolDigest = CheckDigest("probeTool", settings.ProbeTool);
        VersionLine = FirstLine(Run("tool version", settings.MediaTool, ["-hide_banner", "-version"], Path.GetTempPath()).Output);
    }

    public string MediaToolDigest { get; }

    public string ProbeToolDigest { get; }

    /// <summary>The first line the media tool prints for -version, recorded in every package (design D-013).</summary>
    public string VersionLine { get; }

    public IReadOnlyList<ToolInvocation> Log => _log;

    /// <summary>The arguments that open one local input file: the protocol limit, the input's own options, then the file.</summary>
    public static IEnumerable<string> Input(string file, params string[] options)
    {
        yield return "-protocol_whitelist";
        yield return "file";
        foreach (var option in options)
        {
            yield return option;
        }

        yield return "-i";
        yield return file;
    }

    public static string Sha256File(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }

    /// <summary>Runs the media tool; a non-zero exit is a tool failure naming the purpose and the tool's first error line.</summary>
    public (string Output, string Errors) Media(string purpose, IReadOnlyList<string> arguments, string workingFolder)
    {
        RequireLocalInputs(purpose, arguments, probe: false);
        var result = Run(purpose, _settings.MediaTool, arguments, workingFolder);
        if (result.ExitCode != 0)
        {
            throw new PilotToolFailure(purpose, $"ffmpeg exit {result.ExitCode}: {Trim(LastLines(result.Errors))}");
        }

        return (result.Output, result.Errors);
    }

    /// <summary>Probes one local file for its duration, streams, size and rotation.</summary>
    public MediaFacts Probe(string purpose, string file)
    {
        string[] arguments =
        [
            "-v", "error", "-protocol_whitelist", "file",
            "-show_entries", "format=duration:stream=codec_type,width,height:stream_side_data=rotation:stream_tags=rotate",
            "-of", "json", file,
        ];
        RequireLocalInputs(purpose, arguments, probe: true);
        var result = Run(purpose, _settings.ProbeTool, arguments, Path.GetDirectoryName(Path.GetFullPath(file))!);
        if (result.ExitCode != 0)
        {
            throw new PilotRefusal("media-unreadable", $"Không đọc được tệp {Path.GetFileName(file)}: {Trim(LastLines(result.Errors))}");
        }

        using var document = JsonDocument.Parse(result.Output);
        var root = document.RootElement;
        double duration = 0;
        if (root.TryGetProperty("format", out var format) && format.TryGetProperty("duration", out var d)
            && d.ValueKind == JsonValueKind.String)
        {
            duration = double.Parse(d.GetString()!, CultureInfo.InvariantCulture);
        }

        bool hasVideo = false, hasAudio = false;
        int width = 0, height = 0, rotation = 0;
        if (root.TryGetProperty("streams", out var streams))
        {
            foreach (var stream in streams.EnumerateArray())
            {
                var type = stream.TryGetProperty("codec_type", out var t) ? t.GetString() : null;
                if (type == "audio")
                {
                    hasAudio = true;
                }
                else if (type == "video" && !hasVideo)
                {
                    hasVideo = true;
                    width = stream.TryGetProperty("width", out var w) ? w.GetInt32() : 0;
                    height = stream.TryGetProperty("height", out var h) ? h.GetInt32() : 0;
                    if (stream.TryGetProperty("side_data_list", out var side))
                    {
                        foreach (var item in side.EnumerateArray())
                        {
                            if (item.TryGetProperty("rotation", out var r) && r.ValueKind == JsonValueKind.Number)
                            {
                                rotation = r.GetInt32();
                            }
                        }
                    }

                    if (rotation == 0 && stream.TryGetProperty("tags", out var tags) && tags.TryGetProperty("rotate", out var rotate)
                        && int.TryParse(rotate.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var tagged))
                    {
                        rotation = tagged;
                    }
                }
            }
        }

        return new MediaFacts(duration, hasVideo, hasAudio, width, height, rotation);
    }

    /// <summary>
    /// Refuses, before any start, an argument list that could reach anything but a local file: an input without the protocol
    /// limit, a protocol limit other than "file", or any argument that names a URL scheme.
    /// </summary>
    public static void RequireLocalInputs(string purpose, IReadOnlyList<string> arguments, bool probe)
    {
        ArgumentNullException.ThrowIfNull(arguments);
        var limits = 0;
        var inputs = 0;
        var limitedSincePreviousInput = false;
        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            if (Scheme.IsMatch(argument))
            {
                throw new PilotToolFailure(purpose, $"refused before start: argument {i} names a URL scheme");
            }

            if (argument == "-protocol_whitelist")
            {
                if (i + 1 >= arguments.Count || arguments[i + 1] != "file")
                {
                    throw new PilotToolFailure(purpose, "refused before start: the protocol limit must be exactly \"file\"");
                }

                limits++;
                limitedSincePreviousInput = true;
                i++;
            }
            else if (argument == "-i")
            {
                if (!limitedSincePreviousInput)
                {
                    throw new PilotToolFailure(purpose, $"refused before start: input {inputs + 1} has no \"-protocol_whitelist file\"");
                }

                inputs++;
                limitedSincePreviousInput = false;
            }
            else if (argument is "-f" && i + 1 < arguments.Count && arguments[i + 1] is "lavfi" or "concat")
            {
                throw new PilotToolFailure(purpose, $"refused before start: the {arguments[i + 1]} input is not used by the pilot");
            }
        }

        if (probe ? limits != 1 : limits != inputs)
        {
            throw new PilotToolFailure(purpose, "refused before start: every input must carry \"-protocol_whitelist file\"");
        }
    }

    private (int ExitCode, string Output, string Errors) Run(string purpose, ToolSetting tool, IReadOnlyList<string> arguments, string workingFolder)
    {
        var start = new ProcessStartInfo(tool.Path)
        {
            WorkingDirectory = workingFolder,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
            StandardErrorEncoding = System.Text.Encoding.UTF8,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        start.Environment.Clear();
        foreach (var kept in new[] { "SystemRoot", "windir" })
        {
            if (Environment.GetEnvironmentVariable(kept) is { Length: > 0 } value)
            {
                start.Environment[kept] = value;
            }
        }

        start.Environment["PATH"] = Environment.SystemDirectory;
        var stopwatch = Stopwatch.StartNew();
        using var job = JobObject.Create();
        using var process = Process.Start(start) ?? throw new PilotToolFailure(purpose, "the process could not be started");
        try
        {
            job.Assign(process);
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        process.StandardInput.Close();
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(TimeSpan.FromSeconds(_settings.ProcessBoundSeconds)))
        {
            job.Close();
            process.WaitForExit();
            _log.Add(new ToolInvocation(purpose, tool.Path, ToolDigest(tool), [.. arguments], -1, stopwatch.Elapsed.TotalSeconds));
            throw new PilotToolFailure(purpose, $"the tool overran its bound of {_settings.ProcessBoundSeconds} s and was ended");
        }

        process.WaitForExit();
        job.Close();
        stopwatch.Stop();
        _log.Add(new ToolInvocation(purpose, tool.Path, ToolDigest(tool), [.. arguments], process.ExitCode, Math.Round(stopwatch.Elapsed.TotalSeconds, 3)));
        return (process.ExitCode, output.GetAwaiter().GetResult(), errors.GetAwaiter().GetResult());
    }

    private string ToolDigest(ToolSetting tool) =>
        ReferenceEquals(tool, _settings.ProbeTool) ? ProbeToolDigest : MediaToolDigest;

    private static string CheckDigest(string name, ToolSetting tool)
    {
        if (!File.Exists(tool.Path))
        {
            throw new PilotRefusal("tool-missing", $"Không thấy công cụ {name} tại {tool.Path}.");
        }

        var actual = Sha256File(tool.Path);
        if (!string.Equals(actual, tool.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new PilotRefusal("tool-digest-mismatch",
                $"Công cụ {name} ({tool.Path}) có SHA-256 {actual}, khác giá trị đã cấu hình {tool.Sha256}. Không chạy công cụ lạ.");
        }

        return actual;
    }

    private static string FirstLine(string text) =>
        text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault() ?? string.Empty;

    private static string LastLines(string text) =>
        string.Join(" | ", text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).TakeLast(2));

    private static string Trim(string text) => text.Length > 400 ? text[..400] : text;
}
