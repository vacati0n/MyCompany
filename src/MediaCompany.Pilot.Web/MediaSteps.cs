using System.Globalization;

namespace MediaCompany.Pilot.Web;

/// <summary>An admitted input as the page shows it: plain name, length, SHA-256 and, for a clip, its displayed size.</summary>
public sealed record MediaSummary(string Name, double Seconds, string Sha256, int Width, int Height);

/// <summary>Where an accepted recording goes: the video's input folder and code, the pilot folder whose ledger is read, a scratch folder.</summary>
public sealed record RecordingTarget(string InputFolder, string VideoCode, string PilotFolder, string ScratchFolder);

/// <summary>
/// THE RECORDING AND CLIP STEPS (design decision D-002, Design Gate option O-005). Every ffmpeg and ffprobe start goes through the
/// toolchain's MediaTool: digest-checked, argument list, file-only protocol, kill-on-close job, bounded.
/// <list type="bullet">
/// <item>A browser take is kept in the takes folder, outside the input folder, until the operator accepts it; on acceptance it is
/// converted once into mono 16-bit PCM WAV with fixed parameters, no metadata and the length capped at the held limit, and replaces
/// the video's recording. Two conversions of one take give one digest.</item>
/// <item>An uploaded recording is stored byte for byte when its extension is one the toolchain admits, and refused with the list
/// otherwise; it is never converted.</item>
/// <item>A converted take and an uploaded recording both pass the toolchain's own recording admission, with the ledger, before they
/// replace the recording: a recording longer than the held limit or used by another video is refused at step 4, in the toolchain's
/// own words, and refused again by produce.</item>
/// <item>A clip is checked with the toolchain's own clip admission against the video's recording before it replaces the clip.</item>
/// </list>
/// The toolchain still decides at produce: these are step checks that stop early, never a pass the toolchain would refuse.
/// </summary>
public static class MediaSteps
{
    public const int ConvertedRate = 48000;

    /// <summary>The conversion's argument list: one input, its first audio stream, mono 16-bit PCM WAV, bit-exact, no metadata, capped.</summary>
    public static IReadOnlyList<string> ConversionArguments(string take, string output, double limitSeconds) =>
    [
        "-hide_banner", "-nostdin", "-nostats", "-y",
        .. MediaTool.Input(take),
        "-map", "0:a:0", "-vn", "-sn", "-dn",
        "-t", limitSeconds.ToString("0.000", CultureInfo.InvariantCulture),
        "-ac", "1", "-ar", ConvertedRate.ToString(CultureInfo.InvariantCulture), "-c:a", "pcm_s16le",
        "-map_metadata", "-1", "-map_chapters", "-1", "-fflags", "+bitexact", "-flags:a", "+bitexact",
        "-f", "wav", output,
    ];

    /// <summary>
    /// Converts a raw take into the video's recording; the take is not changed. The converted file then passes the toolchain's own
    /// recording admission (length, upload ceiling, not reused by another video in the ledger) before it replaces the recording.
    /// </summary>
    public static MediaSummary Convert(MediaTool tool, string take, RecordingTarget target, HeldData held)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(held);
        var check = NewCheckFolder(target.ScratchFolder, "chuyen");
        try
        {
            var converted = Path.Combine(check, Workspace.RecordingStem + ".wav");
            tool.Media("convert browser take", ConversionArguments(take, converted, held.RecordingMaximumSeconds), check);
            var facts = tool.Probe("probe converted take", converted);
            if (!facts.HasAudio || facts.DurationSeconds <= 0)
            {
                throw new WebRefusal("take-empty", "Bản ghi vừa thu không có tiếng. Kiểm tra micrô rồi ghi lại.");
            }

            var admitted = Admission.Recording(check, tool, held, Ledger.Open(target.PilotFolder, held), target.VideoCode);
            var stored = ReplaceRecording(target.InputFolder, converted, ".wav");
            return new MediaSummary(Path.GetFileName(stored), Math.Round(admitted.Facts.DurationSeconds, 3), admitted.Sha256, 0, 0);
        }
        finally
        {
            Directory.Delete(check, recursive: true);
        }
    }

    /// <summary>
    /// Checks an uploaded recording with the toolchain's own recording admission, under the name the toolchain will see (so a
    /// refusal reads exactly as the produce verb's would), and stores it byte for byte as the video's only recording.
    /// </summary>
    public static MediaSummary AcceptUploadedRecording(MediaTool tool, string uploaded, string extension, RecordingTarget target, HeldData held)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(held);
        if (!Admission.RecordingExtensions.Contains(extension))
        {
            throw new WebRefusal("recording-format-refused",
                $"Tệp \"{extension}\" không phải dạng bản ghi bộ công cụ nhận. Dạng được nhận: {string.Join(", ", Admission.RecordingExtensions)}. Xuất bản ghi sang một trong các dạng đó rồi tải lên lại; ứng dụng không tự chuyển tệp tải lên.");
        }

        var check = NewCheckFolder(target.ScratchFolder, "kiem-ghi");
        try
        {
            var candidate = Path.Combine(check, Workspace.RecordingStem + extension);
            File.Move(uploaded, candidate);
            var admitted = Admission.Recording(check, tool, held, Ledger.Open(target.PilotFolder, held), target.VideoCode);
            var stored = ReplaceRecording(target.InputFolder, candidate, extension);
            return new MediaSummary(Path.GetFileName(stored), Math.Round(admitted.Facts.DurationSeconds, 3), admitted.Sha256, 0, 0);
        }
        finally
        {
            Directory.Delete(check, recursive: true);
        }
    }

    private static string NewCheckFolder(string scratch, string purpose)
    {
        var folder = Path.Combine(scratch, $"{purpose}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);
        return folder;
    }

    /// <summary>The video's one recording, probed, as the toolchain's clip admission needs it.</summary>
    public static AdmittedMedia Recording(MediaTool tool, string inputFolder)
    {
        ArgumentNullException.ThrowIfNull(tool);
        var files = Directory.EnumerateFiles(inputFolder)
            .Where(f => Admission.RecordingExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())).ToList();
        if (files.Count != 1)
        {
            throw new WebRefusal("recording-needed", "Video này chưa có bản ghi giọng. Làm bước 4 (ghi âm hoặc tải bản ghi lên) trước.");
        }

        return new AdmittedMedia(Path.GetFileName(files[0]), files[0], MediaTool.Sha256File(files[0]), tool.Probe("probe recording", files[0]));
    }

    /// <summary>Checks an uploaded clip with the toolchain's clip admission, then stores it as the video's only clip.</summary>
    public static MediaSummary AcceptClip(MediaTool tool, PilotSettings settings, string uploaded, string extension, string inputFolder, string scratchFolder)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(settings);
        if (!Admission.ClipExtensions.Contains(extension))
        {
            throw new WebRefusal("clip-format-refused",
                $"Tệp \"{extension}\" không phải dạng đoạn phim bộ công cụ nhận. Dạng được nhận: {string.Join(", ", Admission.ClipExtensions)}. Xuất lại đoạn phim rồi tải lên lại.");
        }

        var recording = Recording(tool, inputFolder);
        var check = Path.Combine(scratchFolder, $"kiem-clip-{Guid.NewGuid():N}");
        Directory.CreateDirectory(check);
        try
        {
            var candidate = Path.Combine(check, Workspace.ClipStem + extension);
            File.Move(uploaded, candidate);
            var clip = Admission.Clip(check, tool, recording, settings.ClipToleranceSeconds);
            foreach (var old in Directory.EnumerateFiles(inputFolder).Where(f => Admission.ClipExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
            {
                File.Delete(old);
            }

            var target = Path.Combine(inputFolder, Workspace.ClipStem + extension);
            File.Move(candidate, target);
            var (width, height) = clip.Facts.Displayed;
            return new MediaSummary(Path.GetFileName(target), Math.Round(clip.Facts.DurationSeconds, 3), clip.Sha256, width, height);
        }
        finally
        {
            Directory.Delete(check, recursive: true);
        }
    }

    /// <summary>The video's clip, if one is held.</summary>
    public static MediaSummary? Clip(MediaTool tool, string inputFolder)
    {
        ArgumentNullException.ThrowIfNull(tool);
        var file = Directory.EnumerateFiles(inputFolder)
            .FirstOrDefault(f => Admission.ClipExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));
        if (file is null)
        {
            return null;
        }

        var facts = tool.Probe("probe presenter clip", file);
        var (width, height) = facts.Displayed;
        return new MediaSummary(Path.GetFileName(file), Math.Round(facts.DurationSeconds, 3), MediaTool.Sha256File(file), width, height);
    }

    /// <summary>Removes every recording file and the timing file of the input folder and moves the new one in: one recording per video.</summary>
    private static string ReplaceRecording(string inputFolder, string source, string extension)
    {
        foreach (var old in Directory.EnumerateFiles(inputFolder).Where(f => Admission.RecordingExtensions.Contains(Path.GetExtension(f).ToLowerInvariant())))
        {
            File.Delete(old);
        }

        // A timing file belongs to the recording it was measured on; a new recording starts again from pause detection.
        var timing = Path.Combine(inputFolder, CaptionTiming.TimingFileName);
        if (File.Exists(timing))
        {
            File.Delete(timing);
        }

        var target = Path.Combine(inputFolder, Workspace.RecordingStem + extension);
        File.Move(source, target);
        return target;
    }
}
