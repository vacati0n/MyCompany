using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace MediaCompany.Pilot;

/// <summary>Everything one composition needs, all files already admitted and staged.</summary>
public sealed record CompositionPlan(
    string WorkFolder,
    IReadOnlyList<string> ImageFiles,
    AdmittedMedia Clip,
    AdmittedMedia Recording,
    TextLayout Layout,
    IReadOnlyList<CaptionPage> Pages,
    string OverlayText,
    string OutputFile);

/// <summary>The checks made on the composed video: its probed size and length, and the overlay seen at 0.5 s and at the last frame.</summary>
public sealed record CompositionCheck(int Width, int Height, double DurationSeconds, bool HasAudio, int OverlayBrightestAtStart, int OverlayBrightestAtEnd);

/// <summary>
/// THE COMPOSITION (design O-002, decision record entry 1308). One media-tool pass draws the 1080x1920 frame: a dark ground; the
/// product images in the configured panel, each held for an equal share of the recording with the configured slow motion; the
/// presenter clip scaled into its inset, starting with the recording and its last frame held if it ends up to the tolerance early;
/// the overlay at the top of the safe area for the whole video; each caption page at the bottom of the safe area inside its window;
/// the recording as the sound. Text is drawn from staged text files with expansion off, in the staged face. The delivered settings:
/// H.264 in yuv420p, AAC at 48 kHz, the index moved to the front.
/// </summary>
public static class Composition
{
    public const string FaceFileName = "face.ttf";
    public const string OverlayFileName = "overlay.txt";
    private const string Ground = "0x101418";
    private static readonly Regex Brightest = new(@"lavfi\.signalstats\.YMAX=(\d+)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    /// <summary>The argument list for the one composition pass; text files are written into the work folder first.</summary>
    public static IReadOnlyList<string> Arguments(CompositionPlan plan, PilotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(settings);
        var c = CultureInfo.InvariantCulture;
        var duration = plan.Recording.Facts.DurationSeconds;
        var fps = settings.Frame.FrameRate;
        var totalFrames = (int)Math.Round(duration * fps, MidpointRounding.AwayFromZero);
        var images = plan.ImageFiles.Count;
        var arguments = new List<string> { "-hide_banner", "-nostdin", "-nostats", "-loglevel", "error", "-y" };
        var frames = new int[images];
        for (var i = 0; i < images; i++)
        {
            frames[i] = i == images - 1 ? totalFrames - (totalFrames / images * (images - 1)) : totalFrames / images;
            arguments.AddRange(MediaTool.Input(plan.ImageFiles[i], "-loop", "1", "-framerate", fps.ToString(c),
                "-t", (frames[i] / (double)fps + (1.0 / fps)).ToString("0.000", c)));
        }

        arguments.AddRange(MediaTool.Input(plan.Clip.FullPath));
        arguments.AddRange(MediaTool.Input(plan.Recording.FullPath));
        var clipInput = images;
        var soundInput = images + 1;

        var panel = settings.ImagePanel;
        var graph = new StringBuilder();
        graph.Append(c, $"color=c={Ground}:s={settings.Frame.Width}x{settings.Frame.Height}:r={fps}:d={duration.ToString("0.000", c)},format=yuv420p[ground];");
        var zoom = panel.ZoomMaximumPercent / 100.0;
        for (var i = 0; i < images; i++)
        {
            var last = Math.Max(1, frames[i] - 1);
            var motion = panel.Motion == "zoom"
                ? $"zoompan=z='1+{(zoom - 1).ToString("0.0000", c)}*on/{last}':x='iw/2-(iw/zoom/2)':y='ih/2-(ih/zoom/2)'"
                : $"zoompan=z='{zoom.ToString("0.0000", c)}':x='(iw-iw/zoom)*on/{last}':y='ih/2-(ih/zoom/2)'";
            graph.Append(c, $"[{i}:v]scale={panel.Width}:{panel.Height}:force_original_aspect_ratio=decrease,");
            graph.Append(c, $"pad={panel.Width}:{panel.Height}:(ow-iw)/2:(oh-ih)/2:color={Ground},setsar=1,");
            graph.Append(c, $"{motion}:d=1:s={panel.Width}x{panel.Height}:fps={fps},trim=end_frame={frames[i]},setpts=PTS-STARTPTS,format=yuv420p[p{i}];");
        }

        for (var i = 0; i < images; i++)
        {
            graph.Append(c, $"[p{i}]");
        }

        graph.Append(c, $"concat=n={images}:v=1:a=0[panel];");
        graph.Append(c, $"[ground][panel]overlay={panel.X}:{panel.Y}:eof_action=pass[g1];");
        var inset = settings.PresenterInset;
        var pad = Math.Max(0, duration - plan.Clip.Facts.DurationSeconds) + 1.0;
        graph.Append(c, $"[{clipInput}:v]scale={inset.Width}:{inset.Height}:force_original_aspect_ratio=decrease,");
        graph.Append(c, $"pad={inset.Width}:{inset.Height}:(ow-iw)/2:(oh-ih)/2:color=black,setsar=1,fps={fps},");
        graph.Append(c, $"tpad=stop_mode=clone:stop_duration={pad.ToString("0.000", c)},trim=duration={duration.ToString("0.000", c)},setpts=PTS-STARTPTS,format=yuv420p[presenter];");
        graph.Append(c, $"[g1][presenter]overlay={inset.X}:{inset.Y}:eof_action=repeat[g2];");

        graph.Append("[g2]").Append(TextLayer(plan, settings)).Append("[video]");
        arguments.AddRange(["-filter_complex", graph.ToString(), "-map", "[video]", "-map", $"{soundInput}:a:0",
            "-frames:v", totalFrames.ToString(c), "-r", fps.ToString(c),
            "-c:v", "libx264", "-preset", "veryfast", "-crf", "20", "-pix_fmt", "yuv420p",
            "-c:a", "aac", "-b:a", "160k", "-ar", "48000", "-t", duration.ToString("0.000", c),
            "-movflags", "+faststart", "-map_metadata", "-1", "-fflags", "+bitexact", "-flags:v", "+bitexact", "-flags:a", "+bitexact",
            plan.OutputFile]);
        return arguments;
    }

    /// <summary>
    /// The text layer: the overlay drawn for the whole video, then each caption page drawn only inside its window. Each box is
    /// placed at the layout's numbers; the text is centred in its box.
    /// </summary>
    public static string TextLayer(CompositionPlan plan, PilotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(settings);
        var c = CultureInfo.InvariantCulture;
        var layer = new StringBuilder(DrawText(OverlayFileName, settings.Overlay, plan.Layout.OverlayBox));
        for (var p = 0; p < plan.Pages.Count; p++)
        {
            var page = plan.Pages[p];
            layer.Append(',');
            layer.Append(DrawText(PageFileName(p), settings.Caption, plan.Layout.CaptionBox));
            layer.Append(c, $":enable='gte(t,{page.Start.ToString("0.000", c)})*lt(t,{page.End.ToString("0.000", c)})'");
        }

        return layer.ToString();
    }

    public static string PageFileName(int index) => string.Create(CultureInfo.InvariantCulture, $"caption-{index + 1:000}.txt");

    /// <summary>Writes the overlay and every caption page as UTF-8 text files (NFC, LF between lines, no final line end).</summary>
    public static void WriteTextFiles(CompositionPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);
        var utf8 = new UTF8Encoding(false);
        File.WriteAllText(Path.Combine(plan.WorkFolder, OverlayFileName), VietnameseText.Nfc(plan.OverlayText), utf8);
        for (var p = 0; p < plan.Pages.Count; p++)
        {
            File.WriteAllText(Path.Combine(plan.WorkFolder, PageFileName(p)), VietnameseText.Nfc(string.Join("\n", plan.Pages[p].Lines)), utf8);
        }
    }

    private static string DrawText(string textFile, TextBoxSetting box, RectangleSetting at) =>
        string.Create(CultureInfo.InvariantCulture,
            $"drawtext=fontfile={FaceFileName}:textfile={textFile}:expansion=none:text_shaping=1:fontsize={box.FontSize}:fontcolor=white:line_spacing={box.LineSpacing}:box=1:boxcolor=black@0.62:boxborderw=0:boxw={at.Width}:boxh={at.Height}:text_align=M+C:x={at.X}:y={at.Y}");

    /// <summary>Probes the composed video and reads the overlay box's brightest pixel at 0.5 s and at the last frame.</summary>
    public static CompositionCheck Check(MediaTool tool, string video, TextLayout layout, double duration)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(layout);
        var facts = tool.Probe("probe composed video", video);
        var (width, height) = facts.Displayed;
        return new CompositionCheck(width, height, facts.DurationSeconds, facts.HasAudio,
            BrightestIn(tool, video, layout.OverlayBox, 0.5),
            BrightestIn(tool, video, layout.OverlayBox, Math.Max(0, Math.Min(facts.DurationSeconds, duration) - 0.1)));
    }

    /// <summary>Extracts one frame of the video as a PNG.</summary>
    public static void ExtractFrame(MediaTool tool, string video, double at, string png)
    {
        ArgumentNullException.ThrowIfNull(tool);
        tool.Media("extract frame",
            ["-hide_banner", "-nostdin", "-loglevel", "error", "-y", .. MediaTool.Input(video, "-ss", at.ToString("0.000", CultureInfo.InvariantCulture)),
                "-frames:v", "1", png],
            Path.GetDirectoryName(Path.GetFullPath(png))!);
    }

    private static int BrightestIn(MediaTool tool, string video, RectangleSetting box, double at)
    {
        var c = CultureInfo.InvariantCulture;
        var (_, errors) = tool.Media("check overlay",
            ["-hide_banner", "-nostdin", "-nostats", "-loglevel", "info", .. MediaTool.Input(video, "-ss", at.ToString("0.000", c)),
                "-frames:v", "1", "-vf", string.Create(c, $"crop={box.Width}:{box.Height}:{box.X}:{box.Y},signalstats,metadata=print"),
                "-f", "null", "-"],
            Path.GetDirectoryName(Path.GetFullPath(video))!);
        var match = Brightest.Matches(errors).LastOrDefault();
        return match is null ? -1 : int.Parse(match.Groups[1].Value, c);
    }
}
