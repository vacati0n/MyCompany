using System.Globalization;
using System.Text.RegularExpressions;

namespace MediaCompany.Pilot;

/// <summary>An admitted input file: its plain name, full path, SHA-256 and probed facts.</summary>
public sealed record AdmittedMedia(string Name, string FullPath, string Sha256, MediaFacts Facts);

/// <summary>
/// THE INPUT ADMISSION CHECKS (scope criteria 8 to 11 and decision record entries 1305 to 1308). Each refusal names the file and the
/// measured number. The cases: exactly one recording file in the input folder; the recording no longer than the held 60.0 s; the
/// recording under the 300 s upload ceiling; the recording's digest not used by another video in the ledger; exactly one presenter
/// clip; the clip portrait; the clip's length within the configured tolerance of the recording's.
/// </summary>
public static class Admission
{
    public static readonly IReadOnlyList<string> RecordingExtensions = [".wav", ".mp3", ".m4a", ".aac", ".flac", ".ogg"];
    public static readonly IReadOnlyList<string> ClipExtensions = [".mp4", ".mov", ".m4v", ".mkv", ".webm"];

    public static AdmittedMedia Recording(string inputFolder, MediaTool tool, HeldData held, Ledger ledger, string videoCode)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(ledger);
        var file = Single(inputFolder, RecordingExtensions, "recording", "bản ghi giọng (.wav, .mp3, .m4a, .aac, .flac, .ogg)");
        var facts = tool.Probe("probe recording", file);
        if (!facts.HasAudio)
        {
            throw new PilotRefusal("recording-unreadable", $"Tệp {Path.GetFileName(file)} không có luồng âm thanh.");
        }

        var seconds = facts.DurationSeconds.ToString("0.000", CultureInfo.InvariantCulture);
        if (facts.DurationSeconds >= held.RecordingUploadCeilingSeconds)
        {
            throw new PilotRefusal("recording-over-upload-ceiling",
                $"Bản ghi {Path.GetFileName(file)} dài {seconds} giây, chạm trần tải lên {held.RecordingUploadCeilingSeconds:0.0} giây của nền tảng.");
        }

        if (facts.DurationSeconds > held.RecordingMaximumSeconds)
        {
            throw new PilotRefusal("recording-too-long",
                $"Bản ghi {Path.GetFileName(file)} dài {seconds} giây, quá {held.RecordingMaximumSeconds:0.0} giây CEO cho phép. Ghi lại ngắn hơn.");
        }

        var digest = MediaTool.Sha256File(file);
        ledger.RefuseReusedRecording(digest, videoCode);
        return new AdmittedMedia(Path.GetFileName(file), file, digest, facts);
    }

    public static AdmittedMedia Clip(string inputFolder, MediaTool tool, AdmittedMedia recording, double toleranceSeconds)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(recording);
        var file = Single(inputFolder, ClipExtensions, "clip", "đoạn phim người dẫn (.mp4, .mov, .m4v, .mkv, .webm)");
        var facts = tool.Probe("probe presenter clip", file);
        var (width, height) = facts.Displayed;
        if (!facts.HasVideo || width <= 0 || height <= 0)
        {
            throw new PilotRefusal("clip-unreadable", $"Tệp {Path.GetFileName(file)} không có hình.");
        }

        if (height <= width)
        {
            throw new PilotRefusal("clip-not-portrait",
                $"Đoạn phim người dẫn {Path.GetFileName(file)} là {width}x{height}, không phải khung dọc. Xuất lại đoạn phim dọc 9:16.");
        }

        var difference = Math.Abs(facts.DurationSeconds - recording.Facts.DurationSeconds);
        if (difference > toleranceSeconds + 0.0005)
        {
            throw new PilotRefusal("clip-length-mismatch",
                string.Create(CultureInfo.InvariantCulture,
                    $"Đoạn phim người dẫn dài {facts.DurationSeconds:0.000} giây, bản ghi dài {recording.Facts.DurationSeconds:0.000} giây: lệch {difference:0.000} giây, quá {toleranceSeconds:0.0} giây cho phép. Tạo lại đoạn phim từ đúng bản ghi này."));
        }

        return new AdmittedMedia(Path.GetFileName(file), file, MediaTool.Sha256File(file), facts);
    }

    private static string Single(string inputFolder, IReadOnlyList<string> extensions, string what, string described)
    {
        var files = Directory.EnumerateFiles(inputFolder)
            .Where(f => extensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .Order(StringComparer.Ordinal)
            .ToList();
        if (files.Count == 0)
        {
            throw new PilotRefusal($"{what}-missing", $"Thư mục đầu vào không có {described}.");
        }

        if (files.Count > 1)
        {
            throw new PilotRefusal($"{what}-count",
                $"Thư mục đầu vào có {files.Count} tệp {described}: {string.Join(", ", files.Select(Path.GetFileName))}. Chỉ để lại đúng một tệp.");
        }

        return files[0];
    }
}

/// <summary>
/// THE CAPTION TIMING (design D-008). The sentence windows come from the recording's pauses: the media tool's silence detector at the
/// configured noise floor and minimum length; a silence touching the start or the end is not a pause between sentences; the pauses
/// left must number one less than the sentences, or the run stops and asks for a timing file. A timing file in the input folder,
/// one "start end" line per sentence in seconds, replaces the detection.
/// </summary>
public static class CaptionTiming
{
    public const string TimingFileName = "timing.txt";
    private static readonly Regex Start = new(@"silence_start:\s*(-?[0-9.]+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex End = new(@"silence_end:\s*(-?[0-9.]+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    public static (IReadOnlyList<(double Start, double End)> Windows, string Source) Windows(
        string inputFolder, AdmittedMedia recording, int sentences, MediaTool tool, PilotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(recording);
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(settings);
        var timing = Path.Combine(inputFolder, TimingFileName);
        if (File.Exists(timing))
        {
            return (ReadTimingFile(File.ReadAllText(timing), sentences, recording.Facts.DurationSeconds), "timing-file");
        }

        var filter = string.Create(CultureInfo.InvariantCulture,
            $"silencedetect=noise={settings.Pause.NoiseFloorDb}dB:d={settings.Pause.MinimumSilenceSeconds}");
        var (_, errors) = tool.Media("detect pauses",
            ["-hide_banner", "-nostdin", "-nostats", .. MediaTool.Input(recording.FullPath), "-af", filter, "-f", "null", "-"],
            Path.GetDirectoryName(recording.FullPath)!);
        var silences = ParseSilences(errors, recording.Facts.DurationSeconds);
        return (FromSilences(silences, sentences, recording.Facts.DurationSeconds), "pause-detection");
    }

    /// <summary>The silences the detector reported; a silence that runs to the end of the file ends at the duration.</summary>
    public static IReadOnlyList<(double Start, double End)> ParseSilences(string detectorLog, double duration)
    {
        ArgumentNullException.ThrowIfNull(detectorLog);
        var silences = new List<(double, double)>();
        double? open = null;
        foreach (var line in detectorLog.Split('\n'))
        {
            var s = Start.Match(line);
            if (s.Success)
            {
                open = Math.Max(0, double.Parse(s.Groups[1].Value, CultureInfo.InvariantCulture));
                continue;
            }

            var e = End.Match(line);
            if (e.Success && open is { } began)
            {
                silences.Add((began, double.Parse(e.Groups[1].Value, CultureInfo.InvariantCulture)));
                open = null;
            }
        }

        if (open is { } unfinished)
        {
            silences.Add((unfinished, duration));
        }

        return silences;
    }

    /// <summary>The sentence windows from the silences: speech from the end of one pause to the start of the next.</summary>
    public static IReadOnlyList<(double Start, double End)> FromSilences(IReadOnlyList<(double Start, double End)> silences, int sentences, double duration)
    {
        ArgumentNullException.ThrowIfNull(silences);
        const double Edge = 0.02;
        var leading = silences.Where(s => s.Start <= Edge).Select(s => s.End).DefaultIfEmpty(0).Max();
        var trailing = silences.Where(s => s.End >= duration - Edge).Select(s => s.Start).DefaultIfEmpty(duration).Min();
        var interior = silences.Where(s => s.Start > Edge && s.End < duration - Edge).OrderBy(s => s.Start).ToList();
        if (interior.Count != sentences - 1)
        {
            throw new PilotRefusal("pauses-not-found",
                $"Bản ghi có {interior.Count} khoảng ngừng giữa câu, kịch bản có {sentences} câu nên cần {sentences - 1}. "
                + $"Ghi lại và ngừng rõ giữa hai câu, hoặc đặt tệp {TimingFileName} (mỗi câu một dòng \"giây-bắt-đầu giây-kết-thúc\") vào thư mục đầu vào.");
        }

        var windows = new List<(double, double)>();
        for (var i = 0; i < sentences; i++)
        {
            var start = i == 0 ? leading : interior[i - 1].End;
            var end = i == sentences - 1 ? trailing : interior[i].Start;
            windows.Add((Math.Round(start, 3), Math.Round(end, 3)));
        }

        return windows;
    }

    /// <summary>
    /// Reads the operator's timing file: one line per sentence, in order, "start end" in seconds with a dot for decimals, each start
    /// before its end, no window overlapping the one before, every window inside the recording. The refusal names the line.
    /// </summary>
    public static IReadOnlyList<(double Start, double End)> ReadTimingFile(string text, int sentences, double duration)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lines = text.ReplaceLineEndings("\n").Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        if (lines.Count != sentences)
        {
            throw new PilotRefusal("timing-file-invalid", $"Tệp {TimingFileName} có {lines.Count} dòng; kịch bản có {sentences} câu, cần đúng {sentences} dòng.");
        }

        var windows = new List<(double, double)>();
        var previousEnd = 0.0;
        for (var i = 0; i < lines.Count; i++)
        {
            var parts = lines[i].Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2
                || !double.TryParse(parts[0], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var start)
                || !double.TryParse(parts[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var end))
            {
                throw new PilotRefusal("timing-file-invalid", $"Tệp {TimingFileName}, dòng của câu {i + 1}: cần hai số giây \"bắt-đầu kết-thúc\", ví dụ \"0.00 3.20\".");
            }

            if (end <= start || start < previousEnd || end > duration + 0.0005)
            {
                throw new PilotRefusal("timing-file-invalid",
                    string.Create(CultureInfo.InvariantCulture,
                        $"Tệp {TimingFileName}, câu {i + 1}: khoảng {start:0.00}–{end:0.00} giây phải bắt đầu trước khi kết thúc, sau câu trước ({previousEnd:0.00}) và trong bản ghi ({duration:0.00} giây)."));
            }

            windows.Add((start, end));
            previousEnd = end;
        }

        return windows;
    }
}
