using System.Diagnostics;
using System.Globalization;
using System.Text;
using Xunit;

namespace MediaCompany.Pilot.Tests;

/// <summary>
/// The end-to-end sample: the built tool run as the operator runs it, every verb from generated fixtures, under the socket-table
/// harness, followed by the six demonstrated refusals. The root is MC16_SAMPLE_ROOT when set, otherwise a temporary folder; a
/// transcript of every command line, its exit code and its output is written to the root.
/// </summary>
[Collection("media")]
[Trait("Category", "Integration")]
public sealed class SampleRunTests
{
    private readonly StringBuilder _transcript = new();

    [Fact]
    public void AllFiveVerbsRunAndEachDemonstratedRefusalNamesItsCode()
    {
        var root = Environment.GetEnvironmentVariable("MC16_SAMPLE_ROOT") ?? PilotTestEnvironment.NewFolder("sample");
        Directory.CreateDirectory(root);
        var settings = PilotTestEnvironment.Settings(Path.Combine(root, "pilot"));
        var settingsPath = PilotTestEnvironment.WriteSettings(root, settings);
        var v01 = MediaInputs.Folder(root, "V-01");
        using var harness = new SocketTableHarness();

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
        var facts = new MediaTool(settings).Probe("sample", Path.Combine(package, PackageFiles.Video));
        _transcript.AppendLine(CultureInfo.InvariantCulture, $"ffprobe video.mp4: {facts.Displayed.Width}x{facts.Displayed.Height}, {facts.DurationSeconds:0.000} s, audio {facts.HasAudio}");
        _transcript.AppendLine(CultureInfo.InvariantCulture, $"socket-table harness: {harness.Samples} samples, {harness.Rows.Count} rows owned by a watched process");
        File.WriteAllText(Path.Combine(root, "sample-transcript.txt"), _transcript.ToString(), new UTF8Encoding(false));

        Assert.Equal((1080, 1920), facts.Displayed);
        Assert.Equal(MediaTool.Sha256File(Path.Combine(package, PackageFiles.Video)), sha);
        Assert.True(File.Exists(Path.Combine(package, PackageFiles.Frame)));
        Assert.True(harness.Samples > 10);
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
