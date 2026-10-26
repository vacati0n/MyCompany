using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace MediaCompany.Pilot.Tests;

/// <summary>
/// The installed tools, face and repository the tests use, and the media fixtures they generate at test time. Every fixture is
/// drawn by the installed media tool from its own sources (tones with set silences, colour grounds, test patterns) into a
/// temporary folder outside the repository; no media file is kept in the repository.
/// </summary>
internal static class PilotTestEnvironment
{
    public static string MediaTool =>
        Environment.GetEnvironmentVariable("MC16_FFMPEG") ?? @"C:\Users\vuhoangcao\AppData\Local\Microsoft\WinGet\Links\ffmpeg.exe";

    public static string ProbeTool =>
        Environment.GetEnvironmentVariable("MC16_FFPROBE") ?? @"C:\Users\vuhoangcao\AppData\Local\Microsoft\WinGet\Links\ffprobe.exe";

    public static string FontFolder =>
        Environment.GetEnvironmentVariable("MC16_FONT_FOLDER") ?? @"C:\Users\vuhoangcao\MediaCompanyRun\graphics\font";

    public static string FontManifest =>
        Environment.GetEnvironmentVariable("MC16_FONT_MANIFEST") ?? @"C:\Users\vuhoangcao\MediaCompanyRun\graphics\manifests\font.sha256";

    public const string FaceEntryName = "Inter-4.1/extras/ttf/Inter-SemiBold.ttf";

    public static string FacePath => Path.Combine(FontFolder, "Inter-4.1", "extras", "ttf", "Inter-SemiBold.ttf");

    public static string ManifestEntry =>
        File.ReadAllLines(FontManifest).First(l => l.TrimEnd().EndsWith("*" + FaceEntryName, StringComparison.Ordinal)).Trim();

    public static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
            {
                directory = directory.Parent;
            }

            return directory?.FullName ?? throw new InvalidOperationException("The repository root holding MediaCompany.slnx was not found.");
        }
    }

    /// <summary>A fresh folder under the system temporary folder, outside the repository.</summary>
    public static string NewFolder(string purpose)
    {
        var folder = Path.Combine(Path.GetTempPath(), "mc16-pilot-tests", $"{purpose}-{Guid.NewGuid():N}"[..(purpose.Length + 9)]);
        Directory.CreateDirectory(folder);
        return folder;
    }

    private static readonly Lazy<(string Media, string Probe)> Digests = new(() => (Pilot.MediaTool.Sha256File(MediaTool), Pilot.MediaTool.Sha256File(ProbeTool)));

    /// <summary>The settings with the safe area of decision record entry 1308 and the example values of the operator guide.</summary>
    public static PilotSettings Settings(string pilotFolder, Func<PilotSettings, PilotSettings>? change = null)
    {
        var settings = new PilotSettings
        {
            FormVersion = 1,
            RepositoryRoot = RepositoryRoot,
            PilotFolder = pilotFolder,
            MediaTool = new ToolSetting { Path = MediaTool, Sha256 = Digests.Value.Media },
            ProbeTool = new ToolSetting { Path = ProbeTool, Sha256 = Digests.Value.Probe },
            Face = new FaceSetting { Path = FacePath, Manifest = FontManifest, ManifestEntry = ManifestEntry },
            Frame = new FrameSetting { Width = 1080, Height = 1920, FrameRate = 30 },
            SafeArea = new SafeAreaSetting { Left = 60, Right = 180, Top = 200, Bottom = 480 },
            Caption = new TextBoxSetting { FontSize = 56, PaddingX = 24, PaddingY = 16, LineSpacing = 12, MaximumLinesPerPage = 2 },
            Overlay = new TextBoxSetting { FontSize = 40, PaddingX = 24, PaddingY = 12, LineSpacing = 0, MaximumLinesPerPage = 1 },
            ImagePanel = new ImagePanelSetting { X = 0, Y = 320, Width = 1080, Height = 1080, Motion = "zoom", ZoomMaximumPercent = 108 },
            PresenterInset = new RectangleSetting { X = 60, Y = 560, Width = 360, Height = 640 },
            Pause = new PauseSetting { NoiseFloorDb = -35, MinimumSilenceSeconds = 0.35 },
            ClipToleranceSeconds = 0.5,
            ProcessBoundSeconds = 600,
        };
        return change is null ? settings : change(settings);
    }

    public static string WriteSettings(string folder, PilotSettings settings)
    {
        var path = Path.Combine(folder, "settings.json");
        File.WriteAllText(path, JsonSerializer.Serialize(settings, PilotSettings.Json), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Runs the installed media tool directly to draw a fixture; the pilot's own invoker is not used for fixtures.</summary>
    public static void Draw(string folder, params string[] arguments)
    {
        var start = new ProcessStartInfo(MediaTool)
        {
            WorkingDirectory = folder,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in new[] { "-hide_banner", "-nostdin", "-loglevel", "error", "-y" }.Concat(arguments))
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        _ = process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"fixture drawing failed: {errors.Result}");
        }
    }

    /// <summary>
    /// A stand-in recording: a tone for each sentence, a set silence between sentences, no silence at either end. A tone of a
    /// different pitch per call keeps each recording's digest distinct.
    /// </summary>
    public static string Recording(string folder, int sentences, double speech, double pause, string name = "recording.wav", int pitch = 440)
    {
        var period = speech + pause;
        var duration = (sentences * period) - pause;
        var c = CultureInfo.InvariantCulture;
        Draw(folder, "-f", "lavfi", "-i",
            string.Create(c, $"aevalsrc='if(lt(mod(t,{period}),{speech}),0.4*sin(2*PI*{pitch}*t),0)':s=48000:d={duration:0.000}"),
            "-ac", "1", "-c:a", "pcm_s16le", name);
        return Path.Combine(folder, name);
    }

    /// <summary>A continuous tone of the given length: no pause anywhere.</summary>
    public static string Tone(string folder, double seconds, string name = "recording.wav", int rate = 48000, int pitch = 523)
    {
        Draw(folder, "-f", "lavfi", "-i", string.Create(CultureInfo.InvariantCulture, $"sine=f={pitch}:r={rate}:d={seconds:0.000}"),
            "-ac", "1", "-c:a", "pcm_s16le", name);
        return Path.Combine(folder, name);
    }

    /// <summary>
    /// A stand-in presenter clip: a magenta ground (strong in both colour channels, so the inset is told apart from the product
    /// images) with a moving test pattern at its centre, of the given size and length, H.264.
    /// </summary>
    public static string Clip(string folder, int width, int height, double seconds, string name = "presenter.mp4")
    {
        var c = CultureInfo.InvariantCulture;
        var square = Math.Min(width, height) * 2 / 3;
        Draw(folder, "-f", "lavfi", "-i", string.Create(c, $"color=c=magenta:s={width}x{height}:r=30:d={seconds:0.000}"),
            "-f", "lavfi", "-i", string.Create(c, $"testsrc2=s={square}x{square}:r=30:d={seconds:0.000}"),
            "-filter_complex", "[0:v][1:v]overlay=(W-w)/2:(H-h)/2:shortest=1,format=yuv420p",
            "-c:v", "libx264", "-preset", "ultrafast", name);
        return Path.Combine(folder, name);
    }

    /// <summary>A code-drawn still: a colour ground with a test-pattern square.</summary>
    public static string Image(string folder, string name, string colour)
    {
        Draw(folder, "-f", "lavfi", "-i", $"color=c={colour}:s=1000x1000:d=1", "-f", "lavfi", "-i", "testsrc=s=400x400:d=1",
            "-filter_complex", "[0:v][1:v]overlay=300:300", "-frames:v", "1", name);
        return Path.Combine(folder, name);
    }

    /// <summary>A complete input folder: the fact sheet, its images and screenshots, and optionally the script.</summary>
    public static string FactSheetFolder(string folder, string videoCode = "V-01", string pagePath = "/san-pham/binh-giu-nhiet-mau",
        string focus = "dung tích", int videoNumber = 1, IReadOnlyList<object>? facts = null)
    {
        Image(folder, "anh-1.png", "0x3a5a7a");
        Image(folder, "anh-2.png", "0x7a5a3a");
        Image(folder, "anh-3.png", "0x3a7a5a");
        Image(folder, "chup-1.png", "0x2a2a2a");
        var sheet = new Dictionary<string, object>
        {
            ["formVersion"] = 1,
            ["videoCode"] = videoCode,
            ["productName"] = "Bình giữ nhiệt mẫu 500 ml",
            ["subjectNoun"] = "bình giữ nhiệt",
            ["classification"] = "loại 500 ml màu xanh",
            ["pagePath"] = pagePath,
            ["shopNameAndMall"] = "Gian hàng mẫu, không phải Mall",
            ["readDate"] = "2026-10-26",
            ["niche"] = "đồ dùng nhà bếp",
            ["imagesUsed"] = new[] { "anh-1.png", "anh-2.png", "anh-3.png" },
            ["productVideoNumber"] = videoNumber,
            ["videoFocus"] = focus,
            ["facts"] = facts ?? new object[]
            {
                new Dictionary<string, string> { ["label"] = "F1", ["subjectNoun"] = "bình giữ nhiệt", ["attribute"] = "dung tích", ["value"] = "500 ml", ["sourceSection"] = "Thông số: Dung tích", ["screenshot"] = "chup-1.png" },
            },
        };
        var path = Path.Combine(folder, "fact-sheet.json");
        File.WriteAllText(path, JsonSerializer.Serialize(sheet, new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping }), new UTF8Encoding(false));
        return path;
    }

    /// <summary>Writes the generated script for the fact sheet, as the script verb does.</summary>
    public static Script WriteScript(string factSheetPath)
    {
        var sheet = FactSheetCheck.Read(factSheetPath, BannedTermCheck.Held);
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(factSheetPath)!, Producer.ScriptFileName), ScriptGenerator.Serialize(script));
        return script;
    }
}
