using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MediaCompany.Pilot.Tests;

[CollectionDefinition("media", DisableParallelization = true)]
public sealed class MediaCollection;

/// <summary>Frame reading through the installed media tool, for the checks the scope criteria name.</summary>
internal static class Frames
{
    private static readonly Regex FrameLine = new(@"pts_time:([0-9.]+)", RegexOptions.CultureInvariant);

    /// <summary>The named signal statistic of a region, for frames sampled every half second from the first frame, plus the last frame.</summary>
    public static IReadOnlyList<(double Time, double Value)> Sampled(string video, RectangleSetting region, string statistic, double duration)
    {
        var values = HalfSeconds(Statistics(video, region, statistic, string.Empty));
        var last = Statistics(video, region, statistic, string.Empty, string.Create(CultureInfo.InvariantCulture, $"{Math.Max(0, duration - 0.05):0.000}"));
        return [.. values, (duration, last[^1].Value)];
    }

    /// <summary>The frames whose own timestamps fall on a half second (every frame is read, so no resampling relabels a time).</summary>
    public static IReadOnlyList<(double Time, double Value)> HalfSeconds(IReadOnlyList<(double Time, double Value)> frames) =>
        [.. frames.Where(f => Math.Abs((f.Time * 2) - Math.Round(f.Time * 2)) < 0.01)];

    public static IReadOnlyList<(double Time, double Value)> Statistics(string video, RectangleSetting region, string statistic, string sampling, string? seek = null)
    {
        var arguments = new List<string> { "-hide_banner", "-nostdin", "-nostats", "-loglevel", "info" };
        if (seek is not null)
        {
            arguments.AddRange(["-ss", seek]);
        }

        arguments.AddRange(["-i", video]);
        if (seek is not null)
        {
            arguments.AddRange(["-frames:v", "1"]);
        }

        arguments.AddRange(["-vf", string.Create(CultureInfo.InvariantCulture, $"{sampling}crop={region.Width}:{region.Height}:{region.X}:{region.Y},signalstats,metadata=print"), "-f", "null", "-"]);
        var errors = Run(arguments);
        var results = new List<(double, double)>();
        double time = 0;
        foreach (var line in errors.Split('\n'))
        {
            var frame = FrameLine.Match(line);
            if (frame.Success)
            {
                time = double.Parse(frame.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            var marker = $"lavfi.signalstats.{statistic}=";
            var at = line.IndexOf(marker, StringComparison.Ordinal);
            if (at >= 0)
            {
                results.Add((time, double.Parse(line[(at + marker.Length)..].Trim(), CultureInfo.InvariantCulture)));
            }
        }

        return results;
    }

    /// <summary>The grey bytes of one region of one frame.</summary>
    public static byte[] Grey(string video, double at, RectangleSetting region)
    {
        var file = Path.Combine(PilotTestEnvironment.NewFolder("grey"), "frame.raw");
        Run(["-hide_banner", "-nostdin", "-loglevel", "error", "-y", "-ss", at.ToString("0.000", CultureInfo.InvariantCulture), "-i", video, "-frames:v", "1",
            "-vf", string.Create(CultureInfo.InvariantCulture, $"crop={region.Width}:{region.Height}:{region.X}:{region.Y},format=gray"), "-f", "rawvideo", file]);
        return File.ReadAllBytes(file);
    }

    public static string Run(IEnumerable<string> arguments)
    {
        var start = new ProcessStartInfo(PilotTestEnvironment.MediaTool) { RedirectStandardError = true, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
        foreach (var argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)!;
        var errors = process.StandardError.ReadToEndAsync();
        _ = process.StandardOutput.ReadToEndAsync();
        process.WaitForExit();
        return errors.Result;
    }
}

/// <summary>A complete input folder with generated media: three images, one screenshot, a paused recording and a portrait clip.</summary>
internal static class MediaInputs
{
    public const int Sentences = 5;
    public const double Speech = 3.0;
    public const double Pause = 0.6;
    public const double Duration = (Sentences * (Speech + Pause)) - Pause;

    public static string Folder(string root, string videoCode = "V-01", string pagePath = "/san-pham/binh-giu-nhiet-mau", string focus = "dung tích",
        int pitch = 440, double clipSeconds = Duration, int clipWidth = 540, int clipHeight = 960)
    {
        var folder = Path.Combine(root, $"input-{videoCode}");
        Directory.CreateDirectory(folder);
        var sheet = PilotTestEnvironment.FactSheetFolder(folder, videoCode, pagePath, focus);
        PilotTestEnvironment.Recording(folder, Sentences, Speech, Pause, pitch: pitch);
        PilotTestEnvironment.Clip(folder, clipWidth, clipHeight, clipSeconds);
        return sheet;
    }
}

/// <summary>One produced package shared by the composition checks.</summary>
public sealed class ProducedPackage
{
    public ProducedPackage()
    {
        Root = PilotTestEnvironment.NewFolder("produce");
        Settings = PilotTestEnvironment.Settings(Path.Combine(Root, "pilot"));
        FactSheet = MediaInputs.Folder(Root);
        Script = PilotTestEnvironment.WriteScript(FactSheet);
        var producer = new Producer(Settings, HeldData.Current, BannedTermCheck.Held, () => DateTimeOffset.Now);
        Result = producer.Produce(FactSheet);
        Video = Path.Combine(Result.PackageFolder, PackageFiles.Video);
        Record = JsonSerializer.Deserialize<JsonElement>(File.ReadAllBytes(Path.Combine(Result.PackageFolder, PackageFiles.RunRecord)));
        Pages = JsonSerializer.Deserialize<List<CaptionPage>>(File.ReadAllBytes(Path.Combine(Result.PackageFolder, PackageFiles.Captions)), PackageFiles.Json)!;
        Layout = CaptionLayout.Boxes(Settings, FaceMetrics.Read(File.ReadAllBytes(PilotTestEnvironment.FacePath)), HeldData.Current);
    }

    public string Root { get; }
    public PilotSettings Settings { get; }
    public string FactSheet { get; }
    public Script Script { get; }
    public ProduceResult Result { get; }
    public string Video { get; }
    public JsonElement Record { get; }
    public IReadOnlyList<CaptionPage> Pages { get; }
    public TextLayout Layout { get; }
}

[Collection("media")]
[Trait("Category", "Integration")]
public sealed class ProducedVideoTests(ProducedPackage package) : IClassFixture<ProducedPackage>
{
    [Fact]
    public void ThePackageIsReadyAndTheVideoIs1080By1920()
    {
        Assert.Equal("ready", package.Result.Manifest.Status);
        var facts = new MediaTool(package.Settings).Probe("test", package.Video);
        Assert.Equal((1080, 1920), facts.Displayed);
        Assert.Equal(9.0 / 16.0, (double)facts.Displayed.Width / facts.Displayed.Height, 6);
        Assert.InRange(facts.DurationSeconds, MediaInputs.Duration - 0.1, MediaInputs.Duration + 0.1);
    }

    [Fact]
    public void EachOfTheThreeProductImagesShowsMotionInItsInterval()
    {
        var panel = new RectangleSetting { X = 460, Y = 330, Width = 600, Height = 900 };
        var share = MediaInputs.Duration / 3;
        for (var i = 0; i < 3; i++)
        {
            var early = Frames.Grey(package.Video, (i * share) + 0.3, panel);
            var late = Frames.Grey(package.Video, ((i + 1) * share) - 0.3, panel);
            Assert.Equal(early.Length, late.Length);
            Assert.True(early.Zip(late).Count(p => p.First != p.Second) > 0, $"image {i + 1} shows no motion");
        }
    }

    [Fact]
    public void ThePresenterIsInEveryHalfSecondSample()
    {
        var samples = Frames.Sampled(package.Video, package.Settings.PresenterInset, "VAVG", MediaInputs.Duration);
        Assert.True(samples.Count >= (int)(MediaInputs.Duration * 2));
        Assert.All(samples, s => Assert.True(s.Value > 170, string.Create(CultureInfo.InvariantCulture, $"no presenter at {s.Time:0.0} s (V average {s.Value})")));
    }

    [Fact]
    public void TheOverlayIsInEveryHalfSecondSampleAndTheLastFrame()
    {
        var samples = Frames.Sampled(package.Video, package.Layout.OverlayBox, "YMAX", MediaInputs.Duration);
        Assert.True(samples.Count >= (int)(MediaInputs.Duration * 2) + 1);
        Assert.All(samples, s => Assert.True(s.Value >= 200, string.Create(CultureInfo.InvariantCulture, $"no overlay at {s.Time:0.0} s")));
        Assert.Equal("Video tạo bởi AI · Có liên kết tiếp thị", HeldData.Current.OverlayText);
    }

    [Fact]
    public void EachCaptionIsOnScreenOnlyWithinItsWindow()
    {
        var samples = Frames.HalfSeconds(Frames.Statistics(package.Video, package.Layout.CaptionBox, "YMAX", string.Empty));
        var violations = new List<string>();
        foreach (var (time, value) in samples)
        {
            var near = package.Pages.Any(p => Math.Abs(time - p.Start) < 0.07 || Math.Abs(time - p.End) < 0.07);
            if (near)
            {
                continue;
            }

            var inside = package.Pages.Any(p => time >= p.Start && time < p.End);
            if (inside != value >= 200)
            {
                violations.Add(string.Create(CultureInfo.InvariantCulture, $"{time:0.0} s: caption {(inside ? "missing" : "shown outside a window")}"));
            }
        }

        Assert.Empty(violations);
        Assert.Contains(samples, s => s.Value < 200);
    }

    [Fact]
    public void TheDetectedPausesGiveOneWindowPerSentenceFromThePauses()
    {
        Assert.Equal("pause-detection", package.Record.GetProperty("timingSource").GetString());
        var sentences = package.Pages.Select(p => p.Sentence).Distinct().Count();
        Assert.Equal(MediaInputs.Sentences, sentences);
        for (var s = 1; s <= MediaInputs.Sentences; s++)
        {
            var first = package.Pages.First(p => p.Sentence == s);
            var expected = (s - 1) * (MediaInputs.Speech + MediaInputs.Pause);
            Assert.InRange(first.Start, expected - 0.1, expected + 0.1);
        }
    }

    [Fact]
    public void TheCaptionListEqualsTheApprovedScriptAndEverySentenceHasItsSource()
    {
        var captions = package.Record.GetProperty("captions").EnumerateArray().Select(e => e.GetString()).ToList();
        Assert.Equal(package.Script.Sentences.Select(s => s.Text), captions);
        var provenance = package.Record.GetProperty("provenance").EnumerateArray().ToList();
        Assert.Equal(captions.Count, provenance.Count);
        Assert.All(provenance, p =>
        {
            Assert.False(string.IsNullOrWhiteSpace(p.GetProperty("label").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(p.GetProperty("source").GetString()));
        });
    }

    [Fact]
    public void EveryDrawnCharacterIsCoveredAndListed()
    {
        var drawn = package.Record.GetProperty("drawnCharacters").EnumerateArray().ToList();
        Assert.NotEmpty(drawn);
        Assert.All(drawn, d => Assert.True(d.GetProperty("covered").GetBoolean(), d.GetProperty("codePoint").GetString()));
        var listed = drawn.Select(d => d.GetProperty("character").GetString()).ToHashSet();
        var texts = package.Script.Sentences.Select(s => s.Text).Append(HeldData.Current.OverlayText);
        Assert.All(texts.SelectMany(t => t.EnumerateRunes()).Select(r => r.ToString()).Distinct(), c => Assert.Contains(c, listed));
    }

    [Fact]
    public void ThePackageDigestsEqualTheRecomputedDigests()
    {
        var manifest = package.Result.Manifest;
        Assert.Equal(MediaTool.Sha256File(package.Video), manifest.VideoSha256);
        var roles = manifest.Inputs.Select(i => i.Role).ToHashSet();
        Assert.Superset(new HashSet<string> { "fact-sheet", "image", "recording", "presenter-clip", "script" }, roles);
        Assert.Equal(3, manifest.Inputs.Count(i => i.Role == "image"));
        Assert.All(manifest.Inputs, input => Assert.Equal(MediaTool.Sha256File(Path.Combine(package.Result.PackageFolder, PackageFiles.Inputs, input.Name)), input.Sha256));
        var ledgerEntry = JsonSerializer.Deserialize<LedgerEntry>(File.ReadAllBytes(Path.Combine(package.Result.PackageFolder, PackageFiles.LedgerEntry)), PackageFiles.Json)!;
        Assert.Equal(manifest.VideoSha256, ledgerEntry.VideoSha256);
        Assert.Equal(ledgerEntry.ComputeHash(), ledgerEntry.EntryHash);
    }

    [Fact]
    public void ThePackageChecklistTicksOnlyB8()
    {
        var text = File.ReadAllText(Path.Combine(package.Result.PackageFolder, PackageFiles.Checklist));
        var ticked = text.Split('\n').Where(l => l.Contains('☑', StringComparison.Ordinal)).Select(l => l.Split('|')[1].Trim()).ToList();
        Assert.Equal(["B8"], ticked);
    }

    [Fact]
    public void EveryCaptionPageStaysOnScreenAtLeastTheMinimumAndTheDisclosureIsPaged()
    {
        Assert.Equal(1.2, package.Settings.CaptionPageMinimumSeconds);
        Assert.All(package.Pages, p => Assert.True(p.End - p.Start >= 1.2 - 1e-9,
            string.Create(CultureInfo.InvariantCulture, $"sentence {p.Sentence} page {p.Page}: {p.End - p.Start:0.000} s")));
        Assert.True(package.Pages.Count(p => p.Sentence == 1) >= 2);
        Assert.True(package.Pages.Count(p => p.Sentence == MediaInputs.Sentences) >= 2);
    }

    [Fact]
    public void ThePagedCaptionsWereCheckedAtRunTimeToRejoinIntoTheScript()
    {
        var checks = package.Record.GetProperty("checks").EnumerateArray().ToList();
        var check = checks.Single(c => c.GetProperty("name").GetString() == "caption-text");
        Assert.True(check.GetProperty("passed").GetBoolean());
        foreach (var sentence in package.Script.Sentences)
        {
            Assert.Equal(sentence.Text, CaptionLayout.Rejoined(package.Pages, sentence.Number));
        }
    }

    [Fact]
    public void TheProofFrameOfThePackageIsTakenDuringTheDisclosuresFirstPage()
    {
        var frame = Path.Combine(package.Result.PackageFolder, PackageFiles.Frame);
        var caption = Frames.Statistics(frame, package.Layout.CaptionBox, "YMAX", string.Empty);
        Assert.True(caption.Single().Value >= 200, "no caption in frame.png");
        var first = package.Pages.First(p => p.Sentence == 1 && p.Page == 1);
        var extracted = package.Record.GetProperty("toolLog").EnumerateArray()
            .Where(e => e.GetProperty("purpose").GetString() == "extract frame")
            .Select(e => e.GetProperty("arguments").EnumerateArray().Select(a => a.GetString()!).ToList())
            .Single();
        var at = double.Parse(extracted[extracted.IndexOf("-ss") + 1], CultureInfo.InvariantCulture);
        Assert.InRange(at, first.Start, first.End);
    }

    [Fact]
    public void TheVideoIsComposedFromThePackageCopiesWhoseBytesWereHashed()
    {
        var manifest = package.Result.Manifest;
        var inputs = Path.Combine(package.Result.PackageFolder, PackageFiles.Inputs);
        var recording = manifest.Inputs.Single(i => i.Role == "recording");
        Assert.Equal(manifest.RecordingSha256, recording.Sha256);
        var source = Path.GetDirectoryName(package.FactSheet)!;
        foreach (var input in manifest.Inputs)
        {
            Assert.Equal(File.ReadAllBytes(Path.Combine(source, input.Name)), File.ReadAllBytes(Path.Combine(inputs, input.Name)));
        }

        var compose = package.Record.GetProperty("toolLog").EnumerateArray()
            .Single(e => e.GetProperty("purpose").GetString() == "compose video")
            .GetProperty("arguments").EnumerateArray().Select(a => a.GetString()!).ToList();
        var read = compose.Where((a, i) => i > 0 && compose[i - 1] == "-i").ToList();
        Assert.Equal(manifest.Inputs.Count(i => i.Role == "image") + 2, read.Count);
        Assert.All(read, path => Assert.StartsWith(inputs, path, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void TheRunOpenedNoSocketAndTheToolLogHoldsOnlyTheMediaToolsWithLocalInputs()
    {
        Assert.Equal(0, package.Record.GetProperty("socketEvents").GetInt32());
        var log = package.Record.GetProperty("toolLog").EnumerateArray().ToList();
        Assert.NotEmpty(log);
        Assert.All(log, entry =>
        {
            var tool = Path.GetFileName(entry.GetProperty("tool").GetString()!);
            Assert.Contains(tool, new[] { "ffmpeg.exe", "ffprobe.exe" });
            var arguments = entry.GetProperty("arguments").EnumerateArray().Select(a => a.GetString()!).ToList();
            Assert.Equal(arguments.Count(a => a == "-i"), arguments.Count(a => a == "-protocol_whitelist") - (tool == "ffprobe.exe" ? 1 : 0));
        });
        var words = new Regex(@"whisper|vosk|piper|onnx|tts|speech|llm|openai|gpt|vbee|elevenlabs|azure|transformers", RegexOptions.IgnoreCase);
        var deps = File.ReadAllText(Path.ChangeExtension(typeof(Program).Assembly.Location, ".deps.json"));
        Assert.DoesNotMatch(words, deps);
        Assert.DoesNotMatch(words, string.Join(" ", log.Select(l => l.GetProperty("tool").GetString())));
        Assert.True(package.Record.GetProperty("guard").GetProperty("passed").GetBoolean());
    }
}

[Collection("media")]
[Trait("Category", "Integration")]
public sealed class AdmissionTests
{
    private static (MediaTool Tool, Ledger Ledger, string Folder) Setup()
    {
        var folder = PilotTestEnvironment.NewFolder("admit");
        var settings = PilotTestEnvironment.Settings(Path.Combine(folder, "pilot"));
        return (new MediaTool(settings), Ledger.Open(settings.PilotFolder, HeldData.Current), Path.Combine(folder, "in"));
    }

    [Theory]
    [InlineData(60.0, null)]
    [InlineData(61.0, "recording-too-long")]
    [InlineData(299.9, "recording-too-long")]
    [InlineData(300.0, "recording-over-upload-ceiling")]
    public void TheRecordingLengthLimitsHold(double seconds, string? refusal)
    {
        var (tool, ledger, folder) = Setup();
        Directory.CreateDirectory(folder);
        PilotTestEnvironment.Tone(folder, seconds, rate: 8000);
        if (refusal is null)
        {
            var admitted = Admission.Recording(folder, tool, HeldData.Current, ledger, "V-01");
            Assert.Equal(60.0, admitted.Facts.DurationSeconds, 2);
        }
        else
        {
            var refused = Assert.Throws<PilotRefusal>(() => Admission.Recording(folder, tool, HeldData.Current, ledger, "V-01"));
            Assert.Equal(refusal, refused.Code);
        }
    }

    [Fact]
    public void TwoRecordingFilesAreRefused()
    {
        var (tool, ledger, folder) = Setup();
        Directory.CreateDirectory(folder);
        PilotTestEnvironment.Tone(folder, 3, "a.wav");
        PilotTestEnvironment.Tone(folder, 3, "b.m4a".Replace(".m4a", ".wav", StringComparison.Ordinal).Replace("b", "c", StringComparison.Ordinal));
        Assert.Equal("recording-count", Assert.Throws<PilotRefusal>(() => Admission.Recording(folder, tool, HeldData.Current, ledger, "V-01")).Code);
    }

    [Fact]
    public void ARecordingUsedByAnotherVideoIsRefusedNamingIt()
    {
        var (tool, ledger, folder) = Setup();
        Directory.CreateDirectory(folder);
        var file = PilotTestEnvironment.Tone(folder, 3);
        var sheet = new FactSheet("V-01", "p", "b", "c", "/p/1", "s", "d", "n", ["a.png"], 1, "f", [], 1);
        ledger.Append(sheet, MediaTool.Sha256File(file), "v", "pkg", DateTimeOffset.UnixEpoch);
        var refusal = Assert.Throws<PilotRefusal>(() => Admission.Recording(folder, tool, HeldData.Current, ledger, "V-02"));
        Assert.Equal("recording-reused", refusal.Code);
        Assert.Contains("V-01", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(960, 540, 5.0, "clip-not-portrait")]
    [InlineData(540, 960, 4.0, "clip-length-mismatch")]
    [InlineData(540, 960, 6.0, "clip-length-mismatch")]
    [InlineData(540, 960, 5.4, null)]
    [InlineData(540, 960, 4.6, null)]
    public void TheClipMustBePortraitAndWithinTheToleranceOfTheRecording(int width, int height, double seconds, string? refusal)
    {
        var (tool, ledger, folder) = Setup();
        Directory.CreateDirectory(folder);
        PilotTestEnvironment.Tone(folder, 5.0);
        PilotTestEnvironment.Clip(folder, width, height, seconds);
        var recording = Admission.Recording(folder, tool, HeldData.Current, ledger, "V-01");
        if (refusal is null)
        {
            Admission.Clip(folder, tool, recording, 0.5);
            return;
        }

        var refused = Assert.Throws<PilotRefusal>(() => Admission.Clip(folder, tool, recording, 0.5));
        Assert.Equal(refusal, refused.Code);
        if (refusal == "clip-length-mismatch")
        {
            Assert.Contains("1.0", refused.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ARecordingWhoseSentencesRunTogetherStopsBeforeCaptioning()
    {
        var (tool, _, folder) = Setup();
        Directory.CreateDirectory(folder);
        var file = PilotTestEnvironment.Tone(folder, 12.0);
        var recording = new AdmittedMedia("recording.wav", file, MediaTool.Sha256File(file), tool.Probe("t", file));
        var settings = PilotTestEnvironment.Settings(PilotTestEnvironment.NewFolder("pilot"));
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.Windows(folder, recording, 5, tool, settings));
        Assert.Equal("pauses-not-found", refusal.Code);
    }

    [Fact]
    public void ARecordingWithPausesGivesOneWindowPerSentence()
    {
        var (tool, _, folder) = Setup();
        Directory.CreateDirectory(folder);
        var file = PilotTestEnvironment.Recording(folder, 5, 2.4, 0.6);
        var recording = new AdmittedMedia("recording.wav", file, MediaTool.Sha256File(file), tool.Probe("t", file));
        var settings = PilotTestEnvironment.Settings(PilotTestEnvironment.NewFolder("pilot"));
        var (windows, source, timingBytes) = CaptionTiming.Windows(folder, recording, 5, tool, settings);
        Assert.Null(timingBytes);
        Assert.Equal("pause-detection", source);
        Assert.Equal(5, windows.Count);
        for (var i = 0; i < 5; i++)
        {
            Assert.InRange(windows[i].Start, (i * 3.0) - 0.1, (i * 3.0) + 0.1);
            Assert.InRange(windows[i].End, (i * 3.0) + 2.3, (i * 3.0) + 2.5);
        }
    }

    [Fact]
    public void AToolWhoseDigestDiffersIsRefusedBeforeAnyStart()
    {
        var settings = PilotTestEnvironment.Settings(PilotTestEnvironment.NewFolder("pilot"),
            s => s with { MediaTool = s.MediaTool with { Sha256 = new string('0', 64) } });
        Assert.Equal("tool-digest-mismatch", Assert.Throws<PilotRefusal>(() => new MediaTool(settings)).Code);
    }
}

[Collection("media")]
[Trait("Category", "Integration")]
public sealed class FrameAndSafeAreaTests
{
    private static string SampleRoot()
    {
        var root = Environment.GetEnvironmentVariable("MC16_SAMPLE_ROOT") ?? PilotTestEnvironment.NewFolder("sample");
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>The proof frame of design P-003: every Vietnamese vowel with every tone, both cases, and đ, drawn in the face.</summary>
    [Fact]
    public void TheProofFrameDrawsEveryVietnameseLetterInTheFace()
    {
        var bases = new[] { "a", "ă", "â", "e", "ê", "i", "o", "ô", "ơ", "u", "ư", "y" };
        var tones = new[] { "", "̀", "́", "̉", "̃", "̣" };
        var lines = bases.Select(b => string.Concat(tones.Select(t => (b + t).Normalize(NormalizationForm.FormC))) + " " + string.Concat(tones.Select(t => (b.ToUpperInvariant() + t).Normalize(NormalizationForm.FormC)))).ToList();
        lines.Add("đ Đ");
        lines.Add(HeldData.Current.OverlayText);
        var root = SampleRoot();
        var work = PilotTestEnvironment.NewFolder("proof");
        File.Copy(PilotTestEnvironment.FacePath, Path.Combine(work, "face.ttf"));
        File.WriteAllText(Path.Combine(work, "text.txt"), string.Join("\n", lines), new UTF8Encoding(false));
        var face = Face.Admit(PilotTestEnvironment.Settings(work).Face);
        Face.RequireCoverage(face, lines.Select((l, i) => ($"dòng {i + 1}", l)));
        var frame = Path.Combine(root, "proof-frame.png");
        PilotTestEnvironment.Draw(work, "-f", "lavfi", "-i", "color=c=0x101418:s=1080x1920:d=1",
            "-vf", "drawtext=fontfile=face.ttf:textfile=text.txt:expansion=none:fontsize=56:fontcolor=white:line_spacing=18:x=60:y=200",
            "-frames:v", "1", frame);
        var facts = new MediaTool(PilotTestEnvironment.Settings(work)).Probe("proof", frame);
        Assert.Equal((1080, 1920), facts.Displayed);
    }

    /// <summary>
    /// The safe area of decision record entry 1308, checked in pixels: the overlay and every caption page of the longest fixed
    /// sentence drawn on black with the composition's own text layer; every pixel outside the safe area stays black in every frame.
    /// </summary>
    [Fact]
    public void NoDrawnTextLeavesTheSafeArea()
    {
        var work = PilotTestEnvironment.NewFolder("safe");
        var settings = PilotTestEnvironment.Settings(work);
        var metrics = FaceMetrics.Read(File.ReadAllBytes(PilotTestEnvironment.FacePath));
        var layout = CaptionLayout.Boxes(settings, metrics, HeldData.Current);
        var sentences = new List<ScriptSentence> { new(1, SentenceKind.OpeningDisclosure, HeldData.Current.DisclosureSentence, "x", "y") };
        sentences.AddRange(HeldData.Current.CallToAction.Select((c, i) => new ScriptSentence(i + 2, SentenceKind.CallToAction, c, "x", "y")));
        var pages = CaptionLayout.Pages(sentences, [(0.0, 2.4), (2.4, 3.6), (3.6, 6.0)], layout, settings, metrics);
        var plan = new CompositionPlan(work, [], null!, null!, layout, pages, HeldData.Current.OverlayText, Path.Combine(work, "out.mp4"));
        Composition.WriteTextFiles(plan);
        File.Copy(PilotTestEnvironment.FacePath, Path.Combine(work, Composition.FaceFileName));
        PilotTestEnvironment.Draw(work, "-f", "lavfi", "-i", "color=c=black:s=1080x1920:r=10:d=6",
            "-vf", Composition.TextLayer(plan, settings) + ",format=yuv420p", "-c:v", "libx264", "-preset", "ultrafast", "-qp", "0", "out.mp4");
        var outside = new[]
        {
            new RectangleSetting { X = 0, Y = 0, Width = 1080, Height = 200 },
            new RectangleSetting { X = 0, Y = 1440, Width = 1080, Height = 480 },
            new RectangleSetting { X = 0, Y = 0, Width = 60, Height = 1920 },
            new RectangleSetting { X = 900, Y = 0, Width = 180, Height = 1920 },
        };
        foreach (var region in outside)
        {
            var samples = Frames.Statistics(plan.OutputFile, region, "YMAX", string.Empty);
            Assert.Equal(60, samples.Count);
            Assert.All(samples, s => Assert.True(s.Value <= 17, string.Create(CultureInfo.InvariantCulture, $"text outside the safe area ({region}) at {s.Time:0.0} s: {s.Value}")));
        }

        var inside = Frames.Statistics(plan.OutputFile, layout.CaptionBox, "YMAX", string.Empty);
        Assert.All(inside, s => Assert.True(s.Value >= 200));
    }

    /// <summary>A landscape target: composition does not start and the refused run record names the vertical check.</summary>
    [Fact]
    public void ALandscapeTargetStopsBeforeCompositionAndTheRecordNamesTheCheck()
    {
        var root = PilotTestEnvironment.NewFolder("landscape");
        var settings = PilotTestEnvironment.Settings(Path.Combine(root, "pilot"), s => s with { Frame = new FrameSetting { Width = 1920, Height = 1080, FrameRate = 30 } });
        var sheet = PilotTestEnvironment.FactSheetFolder(Directory.CreateDirectory(Path.Combine(root, "in")).FullName);
        PilotTestEnvironment.WriteScript(sheet);
        var refusal = Assert.Throws<PilotRefusal>(() => new Producer(settings, HeldData.Current, BannedTermCheck.Held, () => DateTimeOffset.Now).Produce(sheet));
        Assert.Equal("frame-not-vertical", refusal.Code);
        Assert.False(Directory.Exists(Path.Combine(settings.PilotFolder, "runs")));
        var record = Directory.GetFiles(Path.Combine(settings.PilotFolder, "refused")).Single();
        Assert.Contains("frame-not-vertical", File.ReadAllText(record), StringComparison.Ordinal);
    }

    [Fact]
    public void TheRepositoryHoldsNoMc16MediaFile()
    {
        var root = PilotTestEnvironment.RepositoryRoot;
        var media = new[] { ".png", ".jpg", ".jpeg", ".webp", ".gif", ".bmp", ".wav", ".mp3", ".m4a", ".aac", ".flac", ".ogg", ".mp4", ".mov", ".m4v", ".mkv", ".webm", ".raw" };
        var folders = new[] { Path.Combine(root, "src", "MediaCompany.Pilot"), Path.Combine(root, "tests", "MediaCompany.Pilot.Tests"), Path.Combine(root, "pilot-affiliate-vn", "mc-16") };
        var found = folders.SelectMany(f => Directory.EnumerateFiles(f, "*", SearchOption.AllDirectories))
            .Where(f => !f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(f => media.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .ToList();
        Assert.Empty(found);
    }

    [Fact]
    public void TheSocketHarnessSeesASocketOpenedByAJobMember()
    {
        using var harness = new SocketTableHarness();
        harness.TrackCurrentProcess();
        using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, System.Net.Sockets.ProtocolType.Udp);
        socket.Bind(new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 0));
        var waited = Stopwatch.StartNew();
        while (harness.Rows.Count == 0 && waited.Elapsed < TimeSpan.FromSeconds(5))
        {
            Thread.Sleep(20);
        }

        Assert.NotEmpty(harness.Rows);
    }
}
