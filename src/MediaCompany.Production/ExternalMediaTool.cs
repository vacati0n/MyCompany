using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using MediaCompany.Application.Ports;

namespace MediaCompany.Production;

/// <summary>
/// THE ONE TYPE THAT STARTS A PROCESS (the production change, decision D-013 of its design). It starts exactly two
/// executables, the renderer and the probe of the installed media tool, each located by its own setting and
/// refused before any capability call where the setting is absent or names no file.
///
/// Every invocation passes its arguments as a LIST, never a shell line; text reaches the tool through files under
/// the staging folder, never through arguments; its working directory is the staging folder or the folder of the
/// file it reads; and it runs under its own configured bound measured on the monotonic clock, with NO transaction
/// or record held open by this type. Past its bound the process tree is terminated and the invocation ends under
/// <see cref="MediaToolBoundExceededException"/>; a non-zero exit ends under <see cref="MediaToolFailedException"/>.
/// Stills and audio are produced bit-exact, on one thread, with the fixed font, so one input yields one byte sequence.
/// </summary>
public sealed class ExternalMediaTool : IMediaTool
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

    public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, CancellationToken cancellationToken) =>
        RunCheckedAsync(
            "narration concatenation",
            _settings.RendererPath,
            [
                "-hide_banner", "-nostdin", "-loglevel", "error", "-y",
                "-f", "concat", "-safe", "0", "-i", listFile,
                "-c:a", "pcm_s16le", "-threads", "1",
                "-fflags", "+bitexact", "-flags", "+bitexact", "-map_metadata", "-1",
                outputFile,
            ],
            stagingFolder,
            _settings.DecodeBound,
            cancellationToken);

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
        var seconds = decimal.Parse(
            document.RootElement.GetProperty("format").GetProperty("duration").GetString()!, CultureInfo.InvariantCulture);
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

    private async Task RunCheckedAsync(
        string invocation, string executable, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan bound, CancellationToken cancellationToken) =>
        await RunExitingAsync(invocation, executable, arguments, workingDirectory, bound, cancellationToken).ConfigureAwait(false);

    private static async Task<(int Exit, string Output, string Errors)> RunExitingAsync(
        string invocation, string executable, IReadOnlyList<string> arguments, string workingDirectory, TimeSpan bound, CancellationToken cancellationToken)
    {
        var result = await RunAsync(invocation, executable, arguments, workingDirectory, bound, cancellationToken).ConfigureAwait(false);
        if (result.Exit != 0)
        {
            throw new MediaToolFailedException(
                invocation, result.Exit, result.Errors.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault());
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
