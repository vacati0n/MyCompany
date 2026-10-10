using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using MediaCompany.Application.Ports;
using MediaCompany.Domain.Production;

namespace MediaCompany.Production;

/// <summary>
/// THE ONE TYPE THAT STARTS A PROCESS (the production change, decision D-013 of its design). It starts exactly three
/// executables, the renderer and the probe of the installed media tool and, from the own-voice change (decision D-005 of
/// its design), the in-house model's interpreter, each located by its own setting and refused before it is started where
/// the setting is absent or names no file.
///
/// Every invocation passes its arguments as a LIST, never a shell line; text reaches the tool through files under
/// the staging folder, never through arguments; its working directory is the staging folder or the folder of the
/// file it reads; and it runs under its own configured bound measured on the monotonic clock, with NO transaction
/// or record held open by this type. Past its bound the process tree is terminated and the invocation ends under
/// <see cref="MediaToolBoundExceededException"/>; a non-zero exit ends under <see cref="MediaToolFailedException"/>.
/// Stills and audio are produced bit-exact, on one thread, with the fixed font, so one input yields one byte sequence.
/// </summary>
public sealed partial class ExternalMediaTool : IMediaTool, IVoiceModel
{
    /// <summary>The font file's name inside a staging folder, so no drive or separator reaches a filter argument.</summary>
    public const string StagedFontName = "font.ttf";

    private readonly ProductionSettings _settings;

    public ExternalMediaTool(ProductionSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Require("rendererPath", settings.RendererPath);
        Require("probePath", settings.ProbePath);
        Require("fontFile", settings.FontFile);
        _settings = settings;
    }

    /// <summary>
    /// The tool's version line, bounded by the configured PROBE bound (probeBoundSeconds, 60 s at the Design Gate;
    /// correction CR-008, as ruled): reading a version is a probe-sized task, so it borrows that bound rather than a
    /// setting of its own, and no new setting exists for it.
    /// </summary>
    public async Task<string> VersionLineAsync(CancellationToken cancellationToken)
    {
        var (_, output, _) = await RunAsync(
            "version", _settings.RendererPath, ["-hide_banner", "-version"], Path.GetTempPath(), _settings.ProbeBound, cancellationToken).ConfigureAwait(false);
        return output.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim() ?? "no version line";
    }

    public Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(still);
        var (background, size) = still.Style switch
        {
            StillStyle.Graphic => ("0x14213D", 44),
            StillStyle.Thumbnail => ("0x0B1320", 64),
            _ => ("0x3A3A3A", 48),
        };

        var draw = string.Create(CultureInfo.InvariantCulture,
            $"drawtext=fontfile={StagedFontName}:textfile={still.TextFile}:expansion=none:fontcolor=white:fontsize={size}:line_spacing=18:x=(w-text_w)/2:y=(h-text_h)/2");

        return RunCheckedAsync(
            "still",
            _settings.RendererPath,
            [
                "-hide_banner", "-nostdin", "-loglevel", "error", "-y",
                "-f", "lavfi", "-i", string.Create(CultureInfo.InvariantCulture, $"color=c={background}:s={_settings.Width}x{_settings.Height}:r=1"),
                "-vf", draw,
                "-frames:v", "1", "-threads", "1",
                "-fflags", "+bitexact", "-flags", "+bitexact", "-map_metadata", "-1",
                still.OutputFile,
            ],
            still.StagingFolder,
            _settings.StillBound,
            cancellationToken);
    }

    /// <summary>
    /// Joins the narration parts, bounded by the configured DECODE bound (decodeBoundMinutes, 60 min at the Design Gate;
    /// correction CR-008, as ruled): joining reads every part through end to end, a decode-sized task, so it borrows
    /// that bound rather than a setting of its own, and no new setting exists for it.
    ///
    /// THE JOIN IS EXACT (the own-voice change, decision D-007 of its design): it writes PCM in the parts' COMMON sample
    /// format, with no filter, so it neither resamples nor requantises; the delivered join's fixed 16-bit output would have
    /// requantised a 24-bit or floating-point recording. A sample format with no PCM equivalent is refused by name.
    /// </summary>
    public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken) =>
        RunCheckedAsync(
            "narration concatenation",
            _settings.RendererPath,
            [
                "-hide_banner", "-nostdin", "-loglevel", "error", "-y",
                "-f", "concat", "-safe", "0", "-i", listFile,
                "-c:a", PcmFor(sampleFormat), "-threads", "1",
                "-fflags", "+bitexact", "-flags", "+bitexact", "-map_metadata", "-1",
                outputFile,
            ],
            stagingFolder,
            _settings.DecodeBound,
            cancellationToken);

    /// <summary>The PCM codec that stores a decoder's sample format unchanged, interleaved; refused where none exists.</summary>
    internal static string PcmFor(string sampleFormat) => sampleFormat switch
    {
        "u8" or "u8p" => "pcm_u8",
        "s16" or "s16p" => "pcm_s16le",
        "s32" or "s32p" => "pcm_s32le",
        "s64" or "s64p" => "pcm_s64le",
        "flt" or "fltp" => "pcm_f32le",
        "dbl" or "dblp" => "pcm_f64le",
        _ => throw new MediaToolFailedException("narration concatenation", 0, $"the sample format {sampleFormat} has no PCM equivalent, so the parts cannot be joined exactly"),
    };

    public Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(render);
        var fps = _settings.FramesPerSecond.ToString(CultureInfo.InvariantCulture);
        return RunCheckedAsync(
            "render",
            _settings.RendererPath,
            [
                "-hide_banner", "-nostdin", "-loglevel", "error", "-nostats", "-y",
                "-f", "concat", "-safe", "0", "-i", render.SegmentListFile,
                "-i", render.AudioFile,
                "-map", "0:v:0", "-map", "1:a:0",
                "-vf", string.Create(CultureInfo.InvariantCulture, $"fps={fps},scale={_settings.Width}:{_settings.Height},format=yuv420p"),
                "-r", fps,
                "-frames:v", render.TotalFrames.ToString(CultureInfo.InvariantCulture),
                "-c:v", "libx264", "-preset", "veryfast", "-tune", "stillimage",
                "-c:a", "aac",
                "-movflags", "+faststart",
                render.OutputFile,
            ],
            render.StagingFolder,
            _settings.RenderBound,
            cancellationToken);
    }

    public async Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken)
    {
        var (_, output, _) = await RunExitingAsync(
            "probe",
            _settings.ProbePath,
            ["-v", "error", "-show_entries", "stream=codec_type:format=duration", "-of", "json", file],
            Path.GetDirectoryName(file)!,
            _settings.ProbeBound,
            cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(output);
        var kinds = document.RootElement.TryGetProperty("streams", out var streams)
            ? streams.EnumerateArray().Select(s => s.GetProperty("codec_type").GetString() ?? string.Empty).ToArray()
            : [];
        if (!document.RootElement.TryGetProperty("format", out var format)
            || !format.TryGetProperty("duration", out var duration)
            || !decimal.TryParse(duration.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds))
        {
            throw new MediaToolFailedException("probe", 0, "the probe reported no duration for the file");
        }

        return new MediaProbe(kinds, TimeSpan.FromTicks((long)Math.Round(seconds * TimeSpan.TicksPerSecond)));
    }

    public async Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken)
    {
        var (exit, _, errors) = await RunAsync(
            "decode",
            _settings.RendererPath,
            ["-hide_banner", "-nostdin", "-v", "error", "-i", file, "-f", "null", "-"],
            Path.GetDirectoryName(file)!,
            _settings.DecodeBound,
            cancellationToken).ConfigureAwait(false);

        var lines = errors.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var count = lines.Length + (exit != 0 && lines.Length == 0 ? 1 : 0);
        return new DecodeCheck(count, lines.FirstOrDefault() ?? (exit != 0 ? $"exit code {exit}" : null));
    }

    /// <summary>
    /// Measures the file's first audio stream from DECODED audio (the own-voice change, decision D-007 of its design). The
    /// probe names the container, the codec and the decoder's rate, channels and sample format; one decode pass of the whole
    /// stream, under the configured DECODE bound, counts the decoded samples per channel and runs the tool's EBU R128 meter.
    /// No container duration field is read. A loudness the meter cannot state is recorded not measurable with its reason,
    /// never zero; a decode that reports corrupt input names each line it reported.
    /// </summary>
    public async Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken)
    {
        var (_, probed, _) = await RunExitingAsync(
            "audio probe",
            _settings.ProbePath,
            ["-v", "error", "-select_streams", "a:0", "-show_entries", "stream=codec_name,sample_rate,channels,sample_fmt:format=format_name", "-of", "json", file],
            Path.GetDirectoryName(file)!,
            _settings.ProbeBound,
            cancellationToken).ConfigureAwait(false);

        using var document = JsonDocument.Parse(probed);
        var stream = document.RootElement.TryGetProperty("streams", out var streams) && streams.GetArrayLength() > 0
            ? streams[0]
            : throw new MediaToolFailedException("audio probe", 0, "the file holds no audio stream");
        var container = document.RootElement.TryGetProperty("format", out var format) && format.TryGetProperty("format_name", out var name)
            ? name.GetString() ?? "not stated"
            : "not stated";
        var rate = stream.TryGetProperty("sample_rate", out var r) && int.TryParse(r.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hz) ? hz : 0;
        var channels = stream.TryGetProperty("channels", out var ch) && ch.TryGetInt32(out var count) ? count : 0;
        var sampleFormat = stream.TryGetProperty("sample_fmt", out var sf) ? sf.GetString() ?? "not stated" : "not stated";
        var codec = stream.TryGetProperty("codec_name", out var cn) ? cn.GetString() ?? "not stated" : "not stated";
        if (rate <= 0 || channels <= 0)
        {
            throw new MediaToolFailedException("audio probe", 0, "the decoder reported no sample rate or channel count for the audio stream");
        }

        var (exit, _, log) = await RunAsync(
            "audio measurement",
            _settings.RendererPath,
            [
                "-hide_banner", "-nostdin", "-nostats", "-loglevel", "level+info",
                "-i", file, "-map", "0:a:0",
                "-af", "astats=measure_perchannel=none:measure_overall=Number_of_samples,ebur128=framelog=quiet",
                "-f", "null", "-",
            ],
            Path.GetDirectoryName(file)!,
            _settings.DecodeBound,
            cancellationToken).ConfigureAwait(false);
        if (exit != 0)
        {
            throw new MediaToolFailedException("audio measurement", exit, log.Split('\n').FirstOrDefault(l => l.Contains("[error]", StringComparison.Ordinal))?.Trim());
        }

        var samples = SamplesLine().Match(log) is { Success: true } s
            ? long.Parse(s.Groups["n"].Value, CultureInfo.InvariantCulture)
            : throw new MediaToolFailedException("audio measurement", 0, "the decode reported no decoded sample count");

        decimal? loudness = null;
        string? notMeasurable;
        var integrated = IntegratedLine().Match(log);
        if (!integrated.Success || !decimal.TryParse(integrated.Groups["lufs"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var lufs))
        {
            notMeasurable = "the meter printed no integrated loudness for the decoded audio";
        }
        else if (lufs <= AbsoluteGate)
        {
            notMeasurable = string.Create(CultureInfo.InvariantCulture,
                $"no block of the decoded audio passed the meter's absolute gate of {AbsoluteGate:0} LUFS; the meter printed {lufs:0.0} LUFS, its floor");
        }
        else
        {
            loudness = lufs;
            notMeasurable = null;
        }

        var findings = log.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(l => l.Contains("[error]", StringComparison.Ordinal) || l.Contains("[fatal]", StringComparison.Ordinal)
                        || (l.Contains("[warning]", StringComparison.Ordinal) && l.Contains("corrupt", StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        return new AudioMeasurement
        {
            Container = container,
            Codec = codec,
            SampleRate = rate,
            Channels = channels,
            SampleFormat = sampleFormat,
            DecodedSamples = samples,
            IntegratedLoudness = loudness,
            LoudnessNotMeasurable = notMeasurable,
            DecodeFindings = findings,
        };
    }

    /// <summary>The meter's absolute gate (ITU-R BS.1770): a reading at or below it means no block was measured.</summary>
    private const decimal AbsoluteGate = -70m;

    [GeneratedRegex(@"Number of samples:\s*(?<n>\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex SamplesLine();

    [GeneratedRegex(@"Integrated loudness:\s*\r?\n\s*I:\s*(?<lufs>-?[0-9.]+|-?inf|nan)\s*LUFS", RegexOptions.CultureInvariant)]
    private static partial Regex IntegratedLine();

    /// <summary>
    /// Runs the in-house model once (the own-voice change, decision D-005 of its design): the environment's interpreter
    /// located by setting, in ISOLATED mode (no inherited interpreter variable, no user folder) with bytecode writing off,
    /// running the runtime as a module named by setting, with the explicit argument list below and nothing else. Its
    /// working directory is the staging folder, the text arrives only through the staged text file, and it runs under the
    /// configured model part bound on the monotonic clock with no transaction held by this type.
    /// </summary>
    public async Task<IReadOnlyList<string>> GenerateAsync(VoiceGeneration generation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(generation);
        var model = _settings.InHouseModel ?? throw new ProductionSettingRefusedException("inHouseModel", "no in-house model is configured");
        Require("inHouseModel.interpreterPath", model.InterpreterPath);
        if (generation.TextFile.IndexOfAny(['/', '\\', ':']) >= 0 || generation.OutputFile.IndexOfAny(['/', '\\', ':']) >= 0)
        {
            throw new ArgumentException("The model's text and output files are plain names in the staging folder.", nameof(generation));
        }

        var s = generation.Settings;
        var c = CultureInfo.InvariantCulture;
        var arguments = new List<string>
        {
            "-I", "-B", "-m", model.RuntimeModule,
            "-m", generation.ModelFile,
            "-c", generation.ConfigurationFile,
            "-i", generation.TextFile,
            "-f", generation.OutputFile,
            "--length-scale", s.LengthScale.ToString(c),
            "--noise-scale", s.NoiseScale.ToString(c),
            "--noise-w-scale", s.NoiseWidthScale.ToString(c),
            "--sentence-silence", s.SentenceSilenceSeconds.ToString(c),
            "--volume", s.Volume.ToString(c),
        };
        if (!s.Normalise)
        {
            arguments.Add("--no-normalize");
        }

        await RunExitingAsync("in-house model", model.InterpreterPath, arguments, generation.StagingFolder, generation.Bound, cancellationToken).ConfigureAwait(false);
        return [model.InterpreterPath, .. arguments];
    }

    private async Task RunCheckedAsync(
        string invocation, string executable, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan bound, CancellationToken cancellationToken) =>
        await RunExitingAsync(invocation, executable, arguments, workingDirectory, bound, cancellationToken).ConfigureAwait(false);

    private static async Task<(int Exit, string Output, string Errors)> RunExitingAsync(
        string invocation, string executable, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan bound, CancellationToken cancellationToken)
    {
        var result = await RunAsync(invocation, executable, arguments, workingDirectory, bound, cancellationToken).ConfigureAwait(false);
        if (result.Exit != 0)
        {
            // The first error line, truncated: a process's own words are bounded before they reach any record.
            var first = result.Errors.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            throw new MediaToolFailedException(invocation, result.Exit, first is { Length: > 200 } ? first[..200] : first);
        }

        return result;
    }

    /// <summary>Starts one process, waits for it under its bound on the monotonic clock, and terminates its tree past it.</summary>
    private static async Task<(int Exit, string Output, string Errors)> RunAsync(
        string invocation, string executable, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan bound, CancellationToken cancellationToken)
    {
        var start = new ProcessStartInfo(executable)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)
            ?? throw new MediaToolFailedException(invocation, -1, "the process could not be started");
        var output = process.StandardOutput.ReadToEndAsync(CancellationToken.None);
        var errors = process.StandardError.ReadToEndAsync(CancellationToken.None);

        using var bounded = new CancellationTokenSource(bound);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(bounded.Token, cancellationToken);
        try
        {
            await process.WaitForExitAsync(linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
                // It exited between the bound and the termination.
            }

            await process.WaitForExitAsync(CancellationToken.None).ConfigureAwait(false);
            if (bounded.IsCancellationRequested)
            {
                throw new MediaToolBoundExceededException(invocation, bound);
            }

            throw;
        }

        return (process.ExitCode, await output.ConfigureAwait(false), await errors.ConfigureAwait(false));
    }

    private static void Require(string setting, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ProductionSettingRefusedException(setting, "it is not configured");
        }

        if (!File.Exists(path))
        {
            throw new ProductionSettingRefusedException(setting, $"it names no file: {path}");
        }
    }
}
