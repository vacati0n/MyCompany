using System.Diagnostics;
using System.Globalization;
using System.Text;
using Xunit;

namespace MediaCompany.Pilot.Tests;

/// <summary>
/// The end-to-end sample: the built tool run as the operator runs it, every verb from generated fixtures, under the socket-table
/// harness, followed by the eight demonstrated refusals. The root is MC16_SAMPLE_ROOT when set, otherwise a temporary folder; a
/// transcript of every command line, its exit code and its output is written to the root.
/// </summary>
[Collection("media")]
[Trait("Category", "Integration")]
public sealed class SampleRunTests
{
    private readonly StringBuilder _transcript = new();
    private SocketTableHarness? _harness;

    [Fact]
    public void AllFiveVerbsRunAndEachDemonstratedRefusalNamesItsCode()
    {
        var root = Environment.GetEnvironmentVariable("MC16_SAMPLE_ROOT") ?? PilotTestEnvironment.NewFolder("sample");
        Directory.CreateDirectory(root);
        var settings = PilotTestEnvironment.Settings(Path.Combine(root, "pilot"));
        var settingsPath = PilotTestEnvironment.WriteSettings(root, settings);
        var v01 = MediaInputs.Folder(root, "V-01");
        using var harness = new SocketTableHarness();
        _harness = harness;

        Expect(0, "ĐẠT [check]", "check", "--settings", settingsPath, "--facts", v01);
        Expect(0, "ĐẠT [script]", "script", "--settings", settingsPath, "--facts", v01);
        var produced = Expect(0, "ĐẠT [produce]", "produce", "--settings", settingsPath, "--facts", v01);
        var package = Line(produced, "ĐẠT [produce]: gói V-01 ở ");
        var sha = Line(produced, "  SHA-256 video: ");
        Expect(0, "ĐẠT [approve]", "approve", "--package", package, "--name", "Người duyệt mẫu", "--sha256", sha);
        Expect(0, "ĐẠT [verify]", "verify", "--package", package);

        // A banned term in a fact row.
        var banned = Path.Combine(root, "input-V-05");
        Directory.CreateDirectory(banned);
        var bannedSheet = PilotTestEnvironment.FactSheetFolder(banned, "V-05", "/san-pham/mau-5", "độ bền", facts:
        [
            new Dictionary<string, string> { ["label"] = "F1", ["subjectNoun"] = "bình giữ nhiệt", ["attribute"] = "độ bền", ["value"] = "tốt nhất", ["sourceSection"] = "Mô tả", ["screenshot"] = "chup-1.png" },
        ]);
        Expect(2, "TỪ CHỐI [banned-term]", "check", "--settings", settingsPath, "--facts", bannedSheet);

        // A recording over 60 s.
        var longSheet = Prepared(root, "V-06", "/san-pham/mau-6", recording: f => PilotTestEnvironment.Tone(f, 61.0, rate: 8000), clipSeconds: 61.0);
        Expect(2, "TỪ CHỐI [recording-too-long]", "produce", "--settings", settingsPath, "--facts", longSheet);

        // The V-01 recording, byte for byte, offered for another video.
        var reusedSheet = Prepared(root, "V-02", "/san-pham/mau-2",
            recording: f => File.Copy(Path.Combine(Path.GetDirectoryName(v01)!, "recording.wav"), Path.Combine(f, "recording.wav")));
        Expect(2, "TỪ CHỐI [recording-reused]", "produce", "--settings", settingsPath, "--facts", reusedSheet);

        // A landscape presenter clip.
        var landscapeSheet = Prepared(root, "V-03", "/san-pham/mau-3", clipWidth: 960, clipHeight: 540);
        Expect(2, "TỪ CHỐI [clip-not-portrait]", "produce", "--settings", settingsPath, "--facts", landscapeSheet);

        // A clip 1.0 s shorter than its recording.
        var shortSheet = Prepared(root, "V-04", "/san-pham/mau-4", clipSeconds: MediaInputs.Duration - 1.0);
        Expect(2, "TỪ CHỐI [clip-length-mismatch]", "produce", "--settings", settingsPath, "--facts", shortSheet);

        // Sentences spoken so fast that the disclosure's two caption pages cannot each stay 1.2 s on screen.
        var fastSheet = Prepared(root, "V-07", "/san-pham/mau-7",
            recording: f => PilotTestEnvironment.Recording(f, MediaInputs.Sentences, 1.5, MediaInputs.Pause, pitch: 610),
            clipSeconds: (MediaInputs.Sentences * (1.5 + MediaInputs.Pause)) - MediaInputs.Pause);
        Expect(2, "TỪ CHỐI [caption-page-too-short]", "produce", "--settings", settingsPath, "--facts", fastSheet);

        // A copy of the package whose manifest names a video file outside the package folder.
        var renamed = Path.Combine(root, "goi-sua-tep-ke");
        CopyFolder(package, renamed);
        var manifestPath = Path.Combine(renamed, PackageFiles.Manifest);
        var manifest = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(manifestPath))!;
        manifest["videoFile"] = Path.Combine("..", Path.GetFileName(package), PackageFiles.Video);
        File.WriteAllText(manifestPath, manifest.ToJsonString(), new UTF8Encoding(false));
        Expect(2, "TỪ CHỐI [package-file-not-plain]", "verify", "--package", renamed);

        // One byte of an approved video changed, in a copy of the package.
        var copy = Path.Combine(root, "goi-da-sua-1-byte");
        CopyFolder(package, copy);
        var video = Path.Combine(copy, PackageFiles.Video);
        var bytes = File.ReadAllBytes(video);
        bytes[bytes.Length / 2] ^= 0x01;
        File.WriteAllBytes(video, bytes);
        Expect(2, "TỪ CHỐI [approval-void]", "verify", "--package", copy);
        Expect(2, "TỪ CHỐI [approval-hash-mismatch]", "approve", "--package", copy, "--name", "Người duyệt mẫu", "--sha256", sha);

        harness.Dispose();
        _harness = null;
        var facts = new MediaTool(settings).Probe("sample", Path.Combine(package, PackageFiles.Video));
        _transcript.AppendLine(CultureInfo.InvariantCulture, $"ffprobe video.mp4: {facts.Displayed.Width}x{facts.Displayed.Height}, {facts.DurationSeconds:0.000} s, audio {facts.HasAudio}");
        var names = harness.MemberNames.GroupBy(n => n, StringComparer.OrdinalIgnoreCase).Select(g => $"{g.Key} x{g.Count()}").Order(StringComparer.Ordinal);
        _transcript.AppendLine(CultureInfo.InvariantCulture, $"socket-table harness: {harness.Samples} samples, {harness.MemberCount} processes seen in the harness job ({string.Join(", ", names)}), {harness.Rows.Count} rows owned by a job member");
        File.WriteAllText(Path.Combine(root, "sample-transcript.txt"), _transcript.ToString(), new UTF8Encoding(false));

        Assert.Equal((1080, 1920), facts.Displayed);
        Assert.Equal(MediaTool.Sha256File(Path.Combine(package, PackageFiles.Video)), sha);
        Assert.True(File.Exists(Path.Combine(package, PackageFiles.Frame)));
        Assert.True(harness.Samples > 10);
        Assert.Contains("ffmpeg", harness.MemberNames);
        Assert.Contains("ffprobe", harness.MemberNames);
        Assert.Contains("MediaCompany.Pilot", harness.MemberNames);
        Assert.Empty(harness.Rows);
    }

    private static string Prepared(string root, string code, string page, Action<string>? recording = null, double clipSeconds = MediaInputs.Duration,
        int clipWidth = 540, int clipHeight = 960)
    {
        var folder = Path.Combine(root, $"input-{code}");
        Directory.CreateDirectory(folder);
        var sheet = PilotTestEnvironment.FactSheetFolder(folder, code, page);
        PilotTestEnvironment.WriteScript(sheet);
        if (recording is null)
        {
            PilotTestEnvironment.Recording(folder, MediaInputs.Sentences, MediaInputs.Speech, MediaInputs.Pause, pitch: 300 + (code[^1] * 7));
        }
        else
        {
            recording(folder);
        }

        PilotTestEnvironment.Clip(folder, clipWidth, clipHeight, clipSeconds);
        return sheet;
    }

    private static string Line(string output, string prefix) =>
        output.Split('\n').Select(l => l.TrimEnd('\r')).Single(l => l.StartsWith(prefix, StringComparison.Ordinal))[prefix.Length..].Trim();

    private static void CopyFolder(string from, string to)
    {
        foreach (var file in Directory.EnumerateFiles(from, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(to, Path.GetRelativePath(from, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target);
        }
    }

    private string Expect(int exit, string start, params string[] arguments)
    {
        var tool = Path.Combine(AppContext.BaseDirectory, "MediaCompany.Pilot.exe");
        var info = new ProcessStartInfo(tool) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true, StandardOutputEncoding = Encoding.UTF8 };
        foreach (var argument in arguments)
        {
            info.ArgumentList.Add(argument);
        }

        using var process = Process.Start(info)!;
        _harness?.Track(process);
        var output = process.StandardOutput.ReadToEndAsync();
        var errors = process.StandardError.ReadToEndAsync();
        process.WaitForExit();
        _transcript.AppendLine("> MediaCompany.Pilot " + string.Join(" ", arguments.Select(a => a.Contains(' ', StringComparison.Ordinal) ? $"\"{a}\"" : a)));
        _transcript.Append(output.Result).Append(errors.Result);
        _transcript.AppendLine(CultureInfo.InvariantCulture, $"(exit {process.ExitCode})").AppendLine();
        Assert.True(process.ExitCode == exit && output.Result.StartsWith(start, StringComparison.Ordinal), $"{string.Join(" ", arguments)}: exit {process.ExitCode}: {output.Result}{errors.Result}");
        return output.Result;
    }
}
