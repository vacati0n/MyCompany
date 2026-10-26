using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;

namespace MediaCompany.Pilot.Tests;

[Trait("Category", "Unit")]
public sealed class CaptionLayoutTests
{
    private static readonly FaceMetrics Metrics = FaceMetrics.Read(File.ReadAllBytes(PilotTestEnvironment.FacePath));

    private static PilotSettings Settings(Func<PilotSettings, PilotSettings>? change = null) =>
        PilotTestEnvironment.Settings(PilotTestEnvironment.NewFolder("layout"), change);

    [Fact]
    public void TheDefaultLayoutPutsTheOverlayAtTheTopAndTheCaptionsAtTheBottomOfTheSafeArea()
    {
        var settings = Settings();
        var layout = CaptionLayout.Boxes(settings, Metrics, HeldData.Current);
        Assert.Equal(new RectangleSetting { X = 60, Y = 200, Width = 840, Height = 1240 }, layout.Safe);
        Assert.Equal(60, layout.OverlayBox.X);
        Assert.Equal(200, layout.OverlayBox.Y);
        Assert.Equal(840, layout.OverlayBox.Width);
        Assert.Equal(1440, layout.CaptionBox.Y + layout.CaptionBox.Height);
        Assert.Equal(840, layout.CaptionBox.Width);
        Assert.True(layout.OverlayBox.Inside(layout.Safe));
        Assert.True(layout.CaptionBox.Inside(layout.Safe));
        Assert.Equal(840 - 48, layout.CaptionLineWidth);
    }

    [Fact]
    public void EveryCaptionLineOfEveryFixedSentenceFitsTheLineWidthDerivedFromTheSafeArea()
    {
        var settings = Settings();
        var layout = CaptionLayout.Boxes(settings, Metrics, HeldData.Current);
        var texts = new List<string> { HeldData.Current.DisclosureSentence };
        texts.AddRange(HeldData.Current.CallToAction);
        foreach (var text in texts)
        {
            var lines = CaptionLayout.Lines(text, layout.CaptionLineWidth, 56, Metrics);
            Assert.All(lines, line => Assert.True(Metrics.Width(line, 56) <= layout.CaptionLineWidth, line));
            Assert.Equal(text, string.Join(" ", lines));
        }
    }

    [Fact]
    public void TheDisclosureDoesNotFitTwoLinesAt56PixelsSoItIsPaged()
    {
        var settings = Settings();
        var layout = CaptionLayout.Boxes(settings, Metrics, HeldData.Current);
        var lines = CaptionLayout.Lines(HeldData.Current.DisclosureSentence, layout.CaptionLineWidth, 56, Metrics);
        Assert.True(lines.Count > 2);
        var sentence = new ScriptSentence(1, SentenceKind.OpeningDisclosure, HeldData.Current.DisclosureSentence, "x", "y");
        var pages = CaptionLayout.Pages([sentence], [(1.0, 7.0)], layout, settings, Metrics);
        Assert.True(pages.Count >= 2);
        Assert.All(pages, p => Assert.InRange(p.Lines.Count, 1, 2));
        Assert.Equal(1.0, pages[0].Start);
        Assert.Equal(7.0, pages[^1].End);
        for (var i = 1; i < pages.Count; i++)
        {
            Assert.Equal(pages[i - 1].End, pages[i].Start);
        }
    }

    [Fact]
    public void ACaptionBoxOutsideTheSafeAreaIsRefused()
    {
        var settings = Settings();
        var layout = CaptionLayout.Boxes(settings, Metrics, HeldData.Current);
        var lowered = layout with { CaptionBox = layout.CaptionBox with { Y = layout.CaptionBox.Y + 40 } };
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionLayout.Check(lowered, settings));
        Assert.Equal("text-outside-safe-area", refusal.Code);
        var widened = layout with { OverlayBox = layout.OverlayBox with { Width = layout.OverlayBox.Width + 100 } };
        Assert.Equal("text-outside-safe-area", Assert.Throws<PilotRefusal>(() => CaptionLayout.Check(widened, settings)).Code);
    }

    [Fact]
    public void APresenterInsetOverATextBoxIsRefused()
    {
        var settings = Settings(s => s with { PresenterInset = new RectangleSetting { X = 60, Y = 1200, Width = 360, Height = 640 } });
        Assert.Equal("text-outside-safe-area", Assert.Throws<PilotRefusal>(() => CaptionLayout.Boxes(settings, Metrics, HeldData.Current)).Code);
    }

    [Fact]
    public void ASafeAreaTooNarrowForTheOverlayIsRefused()
    {
        var settings = Settings(s => s with { SafeArea = new SafeAreaSetting { Left = 300, Right = 300, Top = 200, Bottom = 480 } });
        Assert.Equal("text-outside-safe-area", Assert.Throws<PilotRefusal>(() => CaptionLayout.Boxes(settings, Metrics, HeldData.Current)).Code);
    }

    [Theory]
    [InlineData(55, 40)]
    [InlineData(56, 39)]
    public void TextSmallerThanTheCeoMinimumIsRefused(int caption, int overlay)
    {
        var settings = Settings(s => s with { Caption = s.Caption with { FontSize = caption }, Overlay = s.Overlay with { FontSize = overlay } });
        Assert.Equal("settings-invalid", Assert.Throws<PilotRefusal>(() => settings.Validate(HeldData.Current)).Code);
    }

    [Fact]
    public void APilotFolderInsideTheRepositoryIsRefused()
    {
        var settings = Settings(s => s with { PilotFolder = Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-run") });
        Assert.Equal("pilot-folder-in-repository", Assert.Throws<PilotRefusal>(() => settings.Validate(HeldData.Current)).Code);
    }

    [Fact]
    public void MeasuredWidthIsNotSmallerThanTheAdvanceSumOfTheLetters()
    {
        Assert.True(Metrics.Width("Video tạo bởi AI · Có liên kết tiếp thị", 40) > 600);
        Assert.True(Metrics.LineHeight(56) is > 56 and < 90);
    }
}

[Trait("Category", "Unit")]
public sealed class TimingTests
{
    [Fact]
    public void PausesBetweenSentencesGiveOneWindowPerSentence()
    {
        var log = "silence_start: 2.4\nsilence_end: 3.0 | silence_duration: 0.6\nsilence_start: 5.4\nsilence_end: 6.0 | silence_duration: 0.6\n";
        var windows = CaptionTiming.FromSilences(CaptionTiming.ParseSilences(log, 8.4), 3, 8.4);
        Assert.Equal([(0.0, 2.4), (3.0, 5.4), (6.0, 8.4)], windows);
    }

    [Fact]
    public void SilencesAtTheStartAndTheEndAreNotPauses()
    {
        var log = "silence_start: 0\nsilence_end: 0.5\nsilence_start: 2.4\nsilence_end: 3.0\nsilence_start: 5.0\n";
        var windows = CaptionTiming.FromSilences(CaptionTiming.ParseSilences(log, 6.0), 2, 6.0);
        Assert.Equal([(0.5, 2.4), (3.0, 5.0)], windows);
    }

    [Fact]
    public void TooFewPausesStopTheRunAndAskForATimingFile()
    {
        var log = "silence_start: 2.4\nsilence_end: 3.0\n";
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.FromSilences(CaptionTiming.ParseSilences(log, 8.4), 3, 8.4));
        Assert.Equal("pauses-not-found", refusal.Code);
        Assert.Contains(CaptionTiming.TimingFileName, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ACompleteTimingFileIsAccepted()
    {
        var windows = CaptionTiming.ReadTimingFile("0.00 2.40\n3.00 5.40\n6.00 8.40\n", 3, 8.4);
        Assert.Equal(3, windows.Count);
    }

    [Fact]
    public void ATimingFileMissingASentenceIsRefused()
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile("0.00 2.40\n3.00 5.40\n", 3, 8.4));
        Assert.Equal("timing-file-invalid", refusal.Code);
        Assert.Contains("3 câu", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATimingFileEndingAfterTheRecordingIsRefusedNamingTheSentence()
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile("0.00 2.40\n3.00 5.40\n6.00 9.00\n", 3, 8.4));
        Assert.Equal("timing-file-invalid", refusal.Code);
        Assert.Contains("câu 3", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATimingFileOutOfOrderIsRefusedNamingTheSentence()
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile("0.00 2.40\n2.00 5.40\n6.00 8.00\n", 3, 8.4));
        Assert.Contains("câu 2", refusal.Message, StringComparison.Ordinal);
    }
}

[Trait("Category", "Unit")]
public sealed class LedgerTests
{
    private static FactSheet Sheet(string code, string page, string focus, int number = 1) =>
        new(code, "Sản phẩm", "bình", "loại 1", page, "Gian hàng", "2026-10-26", "bếp", ["a.png"], number, focus, [], 1);

    [Fact]
    public void TheThirteenthVideoIsRefused()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        var ledger = Ledger.Open(folder, HeldData.Current);
        for (var i = 1; i <= 12; i++)
        {
            ledger.Append(Sheet($"V-{i:00}", $"/p/{i}", "trọng tâm"), $"rec{i}", $"vid{i}", "pkg", DateTimeOffset.UnixEpoch);
        }

        var reopened = Ledger.Open(folder, HeldData.Current);
        Assert.Equal("pilot-limit-reached", Assert.Throws<PilotRefusal>(() => reopened.RefuseOverLimits(Sheet("V-13", "/p/13", "x"))).Code);
        reopened.RefuseOverLimits(Sheet("V-05", "/p/5", "trọng tâm khác"));
    }

    [Fact]
    public void TheThirdVideoOfOneProductIsRefusedNamingTheProduct()
    {
        var ledger = Ledger.Open(PilotTestEnvironment.NewFolder("ledger"), HeldData.Current);
        ledger.Append(Sheet("V-01", "/p/binh", "dung tích"), "r1", "v1", "pkg", DateTimeOffset.UnixEpoch);
        ledger.Append(Sheet("V-02", "/p/binh", "chất liệu", 2), "r2", "v2", "pkg", DateTimeOffset.UnixEpoch);
        var refusal = Assert.Throws<PilotRefusal>(() => ledger.RefuseOverLimits(Sheet("V-03", "/p/binh", "màu")));
        Assert.Equal("product-limit-reached", refusal.Code);
        Assert.Contains("/p/binh", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ASecondVideoWithTheSameFocusIsRefusedAndADifferentFocusPasses()
    {
        var ledger = Ledger.Open(PilotTestEnvironment.NewFolder("ledger"), HeldData.Current);
        ledger.Append(Sheet("V-01", "/p/binh", "dung tích"), "r1", "v1", "pkg", DateTimeOffset.UnixEpoch);
        Assert.Equal("focus-repeated", Assert.Throws<PilotRefusal>(() => ledger.RefuseOverLimits(Sheet("V-02", "/p/binh", "Dung tích", 2))).Code);
        ledger.RefuseOverLimits(Sheet("V-02", "/p/binh", "chất liệu", 2));
    }

    [Fact]
    public void AReusedRecordingDigestIsRefusedNamingTheOtherVideo()
    {
        var ledger = Ledger.Open(PilotTestEnvironment.NewFolder("ledger"), HeldData.Current);
        ledger.Append(Sheet("V-01", "/p/1", "a"), "abc123", "v1", "pkg", DateTimeOffset.UnixEpoch);
        var refusal = Assert.Throws<PilotRefusal>(() => ledger.RefuseReusedRecording("ABC123", "V-02"));
        Assert.Equal("recording-reused", refusal.Code);
        Assert.Contains("V-01", refusal.Message, StringComparison.Ordinal);
        ledger.RefuseReusedRecording("abc123", "V-01");
    }

    [Fact]
    public void AChangedEntryBreaksTheChainAndIsRefusedOnTheNextRead()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        var ledger = Ledger.Open(folder, HeldData.Current);
        ledger.Append(Sheet("V-01", "/p/1", "a"), "r1", "v1", "pkg", DateTimeOffset.UnixEpoch);
        ledger.Append(Sheet("V-02", "/p/2", "b"), "r2", "v2", "pkg", DateTimeOffset.UnixEpoch);
        Assert.True(File.Exists(Path.Combine(folder, Ledger.PreviousFileName)));
        var path = Path.Combine(folder, Ledger.FileName);
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"r1\"", "\"r9\"", StringComparison.Ordinal));
        var refusal = Assert.Throws<PilotRefusal>(() => Ledger.Open(folder, HeldData.Current));
        Assert.Equal("ledger-chain-broken", refusal.Code);
        Assert.Contains("mục số 1", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AReRunSupersedesTheEarlierEntryOfItsCode()
    {
        var ledger = Ledger.Open(PilotTestEnvironment.NewFolder("ledger"), HeldData.Current);
        ledger.Append(Sheet("V-01", "/p/1", "a"), "r1", "v1", "pkg1", DateTimeOffset.UnixEpoch);
        ledger.Append(Sheet("V-01", "/p/1", "a"), "r2", "v2", "pkg2", DateTimeOffset.UnixEpoch);
        Assert.Equal(2, ledger.Entries.Count);
        Assert.Single(ledger.Current);
        Assert.Equal("pkg2", ledger.Current[0].PackageFolder);
        Assert.Equal(ledger.Entries[0].EntryHash, ledger.Entries[1].PreviousHash);
    }
}

[Trait("Category", "Unit")]
public sealed class ApprovalAndChecklistTests
{
    private static string Package(string status = "ready")
    {
        var folder = PilotTestEnvironment.NewFolder("package");
        File.WriteAllBytes(Path.Combine(folder, PackageFiles.Video), Encoding.ASCII.GetBytes("not a real video, only bytes to hash"));
        var manifest = new PackageManifest
        {
            FormVersion = 1, VideoCode = "V-01", Status = status, HeldReasons = status == "ready" ? [] : ["overlay-present: 0"],
            VideoFile = PackageFiles.Video, VideoSha256 = MediaTool.Sha256File(Path.Combine(folder, PackageFiles.Video)),
            ScriptSha256 = "s", RecordingSha256 = "r", Inputs = [], MediaToolPath = "m", MediaToolSha256 = "m", ProbeToolPath = "p",
            ProbeToolSha256 = "p", MediaToolVersionLine = "v", FacePath = "f", FaceSha256 = "f", FaceManifestLine = "l", Checks = [],
            LedgerEntryHash = null, ProducedAt = "2026-10-26T00:00:00Z",
        };
        PackageFiles.WriteJson(Path.Combine(folder, PackageFiles.Manifest), manifest);
        return folder;
    }

    [Fact]
    public void AnApprovalOfTheRecomputedDigestIsAcceptedAndVerifies()
    {
        var folder = Package();
        var sha = MediaTool.Sha256File(Path.Combine(folder, PackageFiles.Video));
        var record = Approval.Approve(folder, "CEO", sha, DateTimeOffset.UnixEpoch);
        Assert.Equal(sha, record.VideoSha256);
        Assert.Equal(sha, Approval.Verify(folder).Sha256);
    }

    [Fact]
    public void OneChangedByteBeforeApprovalIsRefusedAndAfterApprovalMakesItVoid()
    {
        var folder = Package();
        var video = Path.Combine(folder, PackageFiles.Video);
        var sha = MediaTool.Sha256File(video);
        Approval.Approve(folder, "CEO", sha, DateTimeOffset.UnixEpoch);
        var bytes = File.ReadAllBytes(video);
        bytes[5] ^= 0x01;
        File.WriteAllBytes(video, bytes);
        Assert.Equal("approval-void", Assert.Throws<PilotRefusal>(() => Approval.Verify(folder)).Code);
        Assert.Equal("approval-hash-mismatch", Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", sha, DateTimeOffset.UnixEpoch)).Code);
    }

    [Fact]
    public void AHeldPackageIsNotApproved()
    {
        var folder = Package("held");
        var sha = MediaTool.Sha256File(Path.Combine(folder, PackageFiles.Video));
        Assert.Equal("package-held", Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", sha, DateTimeOffset.UnixEpoch)).Code);
    }

    [Fact]
    public void TheChecklistTicksExactlyB3B5AndB8AndLeavesTheTwentyFourOthersUnticked()
    {
        var sheet = new SheetFixture().Check();
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        var decided = Checklist.Decide(HeldData.Current, BannedTermCheck.Held, script, new RunCheck("overlay-present", true, "chữ lớp nhãn có ở 0,5 giây và khung cuối"));
        var text = Checklist.Render(HeldData.Current, "V-01", decided);
        var rows = text.Split('\n').Where(l => Regex.IsMatch(l, @"^\| [A-D]\d+ \|")).ToList();
        Assert.Equal(27, rows.Count);
        var ticked = rows.Where(r => r.Contains('☑', StringComparison.Ordinal)).Select(r => r.Split('|')[1].Trim()).ToList();
        Assert.Equal(["B3", "B5", "B8"], ticked);
        var unticked = rows.Where(r => !r.Contains('☑', StringComparison.Ordinal)).Select(r => r.Split('|')[1].Trim()).ToList();
        Assert.Equal(["A1", "A2", "A3", "A4", "A5", "A6", "A7", "A8", "B1", "B2", "B4", "B6", "B7", "B9", "B10", "B11", "B12", "B13", "C1", "C2", "C3", "C4", "D1", "D2"], unticked);
        Assert.All(rows.Where(r => ticked.Contains(r.Split('|')[1].Trim())), r => Assert.Contains("công cụ", r, StringComparison.Ordinal));
    }

    [Fact]
    public void AFailedOverlayCheckLeavesB8Unticked()
    {
        var sheet = new SheetFixture().Check();
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        var decided = Checklist.Decide(HeldData.Current, BannedTermCheck.Held, script, new RunCheck("overlay-present", false, "không thấy"));
        Assert.False(decided["B8"].Passed);
    }
}

[Trait("Category", "Unit")]
public sealed class KitConsistencyTests
{
    private static string Kit(string name) => File.ReadAllText(Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-affiliate-vn", "pilot-kit", name));

    [Fact]
    public void TheCallToActionListEqualsKit03PartBRowSix()
    {
        var row = Kit("03-mau-kich-ban-nguoi-dan-ai.md").Split('\n').Single(l => l.StartsWith("| 6. ", StringComparison.Ordinal));
        var quoted = Regex.Matches(row.Split('|')[2], "\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
        Assert.Equal(HeldData.Current.CallToAction, quoted);
    }

    [Fact]
    public void TheChecklistEqualsKit04ItemForItem()
    {
        var rows = Kit("04-danh-sach-duyet-dang-bai.md").Split('\n')
            .Select(l => l.TrimEnd('\r'))
            .Where(l => Regex.IsMatch(l, @"^\| [A-D]\d+ \|"))
            .Select(l => l.Split('|'))
            .Select(c => new ChecklistItem(c[1].Trim(), c[2].Trim()))
            .ToList();
        Assert.Equal(27, rows.Count);
        Assert.Equal(rows, HeldData.Current.Checklist);
    }

    [Fact]
    public void TheDisclosureAndOverlayEqualKit03()
    {
        var kit = Kit("03-mau-kich-ban-nguoi-dan-ai.md");
        Assert.Contains(HeldData.Current.DisclosureSentence, kit, StringComparison.Ordinal);
        Assert.Contains(HeldData.Current.OverlayText, Kit("04-danh-sach-duyet-dang-bai.md"), StringComparison.Ordinal);
    }

    [Fact]
    public void TheExampleSettingsCarryTheCeoSafeAreaAndSizes()
    {
        var path = Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-affiliate-vn", "mc-16", "vi-du-cai-dat.json");
        var settings = JsonSerializer.Deserialize<PilotSettings>(File.ReadAllBytes(path), PilotSettings.Json)!;
        Assert.Equal(new SafeAreaSetting { Left = 60, Right = 180, Top = 200, Bottom = 480 }, settings.SafeArea);
        Assert.Equal(56, settings.Caption.FontSize);
        Assert.Equal(40, settings.Overlay.FontSize);
        Assert.Equal(0.5, settings.ClipToleranceSeconds);
        Assert.Equal(new FrameSetting { Width = 1080, Height = 1920, FrameRate = 30 }, settings.Frame);
    }

    [Fact]
    public void TheExampleFactSheetIsAcceptedShapeForShape()
    {
        var path = Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-affiliate-vn", "mc-16", "vi-du-bang-thong-tin.json");
        var folder = PilotTestEnvironment.NewFolder("example");
        using var document = JsonDocument.Parse(File.ReadAllBytes(path));
        var names = document.RootElement.GetProperty("imagesUsed").EnumerateArray().Select(e => e.GetString()!)
            .Concat(document.RootElement.GetProperty("facts").EnumerateArray().Select(f => f.GetProperty("screenshot").GetString()!));
        foreach (var name in names)
        {
            File.WriteAllBytes(Path.Combine(folder, name), [0]);
        }

        File.Copy(path, Path.Combine(folder, "fact-sheet.json"));
        var sheet = FactSheetCheck.Read(Path.Combine(folder, "fact-sheet.json"), BannedTermCheck.Held);
        Assert.InRange(sheet.Facts.Count, 1, 3);
    }
}

[Trait("Category", "Unit")]
public sealed class NetworkGuardTests
{
    [Fact]
    public void TheToolchainClosureIsTheAllowedSetWithNoNetworkingAndNoVoiceModel()
    {
        var result = NetworkGuard.Check();
        Assert.True(result.Passed, string.Join("; ", result.Findings));
        Assert.Equal(["MediaCompany.Application", "MediaCompany.Deterministic", "MediaCompany.Domain", "MediaCompany.Pilot"], result.Closure);
    }

    [Fact]
    public void AnAssemblyOutsideTheAllowedSetIsFound()
    {
        var result = NetworkGuard.Check(typeof(NetworkGuardTests).Assembly, NetworkGuard.AllowedOwn);
        Assert.False(result.Passed);
        Assert.Contains(result.Findings, f => f.Contains("MediaCompany.Pilot.Tests", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("-i|a.wav")]
    [InlineData("-protocol_whitelist|file,http|-i|a.wav")]
    [InlineData("-protocol_whitelist|file|-i|http://example.invalid/a.wav")]
    [InlineData("-protocol_whitelist|file|-f|lavfi|-i|sine")]
    [InlineData("-protocol_whitelist|file|-i|a.wav|-i|b.wav")]
    public void AnArgumentListThatCouldReachBeyondALocalFileIsRefusedBeforeAnyStart(string joined)
    {
        Assert.Throws<PilotToolFailure>(() => MediaTool.RequireLocalInputs("test", joined.Split('|'), probe: false));
    }

    [Fact]
    public void LocalInputsEachWithTheProtocolLimitPass()
    {
        MediaTool.RequireLocalInputs("test", [.. MediaTool.Input(@"C:\a.wav"), .. MediaTool.Input(@"C:\b.png", "-loop", "1"), "-y", @"C:\out.mp4"], probe: false);
    }
}

[Trait("Category", "Unit")]
public sealed class OperatorGuideTests
{
    private static readonly string[] Verbs = ["check", "script", "produce", "approve", "verify"];

    private static string Guide() =>
        File.ReadAllText(Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-affiliate-vn", "mc-16", "03-huong-dan-van-hanh.md"));

    private static string Source() => string.Concat(Directory.EnumerateFiles(Path.Combine(PilotTestEnvironment.RepositoryRoot, "src", "MediaCompany.Pilot"), "*.cs")
        .Select(File.ReadAllText));

    [Fact]
    public void TheGuideGivesACommandLineForEachOfTheFiveVerbsAndNoOtherVerb()
    {
        var guide = Guide();
        Assert.All(Verbs, verb => Assert.Matches(new Regex($@"(?m)^\s*MediaCompany\.Pilot {verb} --"), guide));
        var named = Regex.Matches(guide, @"(?m)^\s*MediaCompany\.Pilot (\w+)").Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.All(named, verb => Assert.Contains(verb, Verbs));
    }

    [Fact]
    public void EveryRefusalCodeTheGuideListsIsOneTheToolEmits()
    {
        var guide = Guide();
        var section = guide[guide.IndexOf("## 7.", StringComparison.Ordinal)..guide.IndexOf("## 8.", StringComparison.Ordinal)];
        var rows = section.Split('\n').Where(l => Regex.IsMatch(l, @"^\| `[a-z-]+` \|")).ToList();
        var codes = rows.Select(l => Regex.Match(l, @"`([a-z-]+)`").Groups[1].Value).ToList();
        Assert.Equal(["banned-term", "recording-too-long", "recording-reused", "clip-not-portrait", "clip-length-mismatch", "approval-hash-mismatch", "approval-void"], codes);
        var source = Source();
        Assert.All(codes, code => Assert.Contains($"\"{code}\"", source, StringComparison.Ordinal));
    }

    [Fact]
    public void TheGuideAndKit03StateTheHeldRecordingCeilingAndTheRealVoice()
    {
        var ceiling = HeldData.Current.RecordingMaximumSeconds.ToString("0", System.Globalization.CultureInfo.InvariantCulture);
        var kit = File.ReadAllText(Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-affiliate-vn", "pilot-kit", "03-mau-kich-ban-nguoi-dan-ai.md"));
        var partF = kit[kit.IndexOf("## Phần F.", StringComparison.Ordinal)..];
        Assert.Contains($"không quá {ceiling} giây", partF, StringComparison.Ordinal);
        Assert.Contains("giọng thật của chính mình", partF, StringComparison.Ordinal);
        Assert.Contains($"tối đa {ceiling} giây", Guide(), StringComparison.Ordinal);
        Assert.Contains("Không dùng giọng máy, không nhân bản giọng", Guide(), StringComparison.Ordinal);
    }
}
