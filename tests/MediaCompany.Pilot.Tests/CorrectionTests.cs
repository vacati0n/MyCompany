using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MediaCompany.Pilot.Tests;

/// <summary>Money spellings refused as class (a) and the product facts that are not money (correction requests 3 and 5).</summary>
[Trait("Category", "Unit")]
public sealed class MoneySpellingTests
{
    private static readonly BannedTermCheck Check = BannedTermCheck.Held;

    [Theory]
    [InlineData("giá 99.000 VNĐ")]
    [InlineData("giá 99.000 vnđ")]
    [InlineData("chỉ 99 nghìn")]
    [InlineData("chỉ 120 ngàn")]
    [InlineData("1 triệu")]
    [InlineData("1,5 triệu")]
    [InlineData("còn 2 tr")]
    [InlineData("$5")]
    [InlineData("5 usd")]
    [InlineData("5 USD")]
    [InlineData("5$")]
    [InlineData("1 đồng")]
    [InlineData("150.000 đồng.")]
    [InlineData("199.000₫")]
    [InlineData("200,000 VND")]
    public void EachCommonPriceSpellingIsFoundAsClassA(string text)
    {
        Assert.Contains(Check.Find("thử", text), f => f.ClassId == "a");
    }

    [Theory]
    [InlineData("1 đồng hồ treo tường")]
    [InlineData("bộ quà gồm 1 đồng hồ")]
    [InlineData("1,5 lít")]
    [InlineData("220V")]
    [InlineData("nặng 5 kg")]
    [InlineData("2 trục xoay")]
    [InlineData("đồng hồ 1 kim")]
    public void AProductFactThatIsNotMoneyIsNotFound(string text)
    {
        Assert.DoesNotContain(Check.Find("thử", text), f => f.ClassId == "a");
    }

    [Fact]
    public void AFactRowNamingOneClockIsAccepted()
    {
        var fixture = new SheetFixture();
        fixture.Facts.Add(fixture.Row("bộ quà", "gồm", "1 đồng hồ treo tường", "Mô tả: Bộ quà gồm", "chup-2.png"));
        var sheet = fixture.Check();
        Assert.Equal("1 đồng hồ treo tường", sheet.Facts[1].Value);
    }
}

/// <summary>An operator override that speaks in the first person, or claims use or review, is refused (correction request 2).</summary>
[Trait("Category", "Unit")]
public sealed class FirstPersonOverrideTests
{
    [Theory]
    [InlineData("Mình dùng mỗi sáng, bình chứa 500 ml.", "mình")]
    [InlineData("Tôi đã dùng bình 500 ml.", "tôi")]
    [InlineData("Em thấy bình 500 ml rất vừa.", "em")]
    [InlineData("Chúng tôi giao bình 500 ml.", "tôi")]
    [InlineData("Tụi mình chọn bình 500 ml.", "mình")]
    [InlineData("Tớ mang bình 500 ml đi làm.", "tớ")]
    [InlineData("Anh chị nhớ bình 500 ml nhé.", "anh")]
    [InlineData("Bình 500 ml, xài ngon.", "xài ngon")]
    [InlineData("Cảm nhận: bình 500 ml.", "cảm nhận")]
    public void AnOverrideWithAFirstPersonWordOrAUseClaimIsRefusedNamingTheWord(string sentence, string word)
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["override"] = sentence;
        var refusal = fixture.Refused();
        Assert.Equal("override-first-person", refusal.Code);
        Assert.Contains($"\"{word}\"", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("Dòng F1", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Bình màu kem chứa được 500 ml.")]
    [InlineData("Bình chứa được 500 ml, có quai xách.")]
    [InlineData("Bình chứa được 500 ml, nắp xanh.")]
    public void AWordThatOnlyContainsAFirstPersonSyllableIsNotRefused(string sentence)
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["override"] = sentence;
        Assert.Equal(sentence, fixture.Check().Facts[0].Override);
    }

    [Fact]
    public void TheHeldFirstPersonListIsTheDecidedList()
    {
        Assert.Equal(["tôi", "mình", "em", "chúng tôi", "chúng mình", "tụi mình", "bọn mình", "tớ", "anh", "chị"], HeldData.Current.FirstPersonTerms);
        Assert.Null(BannedTermCheck.Held.FirstPerson("Bình giữ nhiệt có dung tích 500 ml."));
        Assert.Equal("mình", BannedTermCheck.Held.FirstPerson("MÌNH dùng".Normalize(System.Text.NormalizationForm.FormD)));
    }
}

/// <summary>Settings that loosen decision record entries 1308 and 1309 are refused at load; stricter values pass (correction request 4).</summary>
[Trait("Category", "Unit")]
public sealed class SettingsCeoMinimumTests
{
    private static PilotSettings Settings(Func<PilotSettings, PilotSettings> change) =>
        PilotTestEnvironment.Settings(PilotTestEnvironment.NewFolder("settings"), change);

    public static TheoryData<string, Func<PilotSettings, PilotSettings>> Looser() => new()
    {
        { "clipToleranceSeconds", s => s with { ClipToleranceSeconds = 0.6 } },
        { "clipToleranceSeconds", s => s with { ClipToleranceSeconds = double.NaN } },
        { "safeArea.left", s => s with { SafeArea = s.SafeArea with { Left = 59 } } },
        { "safeArea.right", s => s with { SafeArea = s.SafeArea with { Right = 179 } } },
        { "safeArea.top", s => s with { SafeArea = s.SafeArea with { Top = 199 } } },
        { "safeArea.bottom", s => s with { SafeArea = s.SafeArea with { Bottom = 479 } } },
        { "caption.fontSize", s => s with { Caption = s.Caption with { FontSize = 55 } } },
        { "overlay.fontSize", s => s with { Overlay = s.Overlay with { FontSize = 39 } } },
        { "captionPageMinimumSeconds", s => s with { CaptionPageMinimumSeconds = 1.1 } },
        { "captionPageMinimumSeconds", s => s with { CaptionPageMinimumSeconds = double.NaN } },
    };

    [Theory]
    [MemberData(nameof(Looser))]
    public void ASettingLooserThanTheCeoValueIsRefusedNamingTheSetting(string setting, Func<PilotSettings, PilotSettings> change)
    {
        var refusal = Assert.Throws<PilotRefusal>(() => Settings(change).Validate(HeldData.Current));
        Assert.Equal("settings-looser-than-ceo", refusal.Code);
        Assert.Contains(setting, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void StricterValuesAreAccepted()
    {
        var settings = Settings(s => s with
        {
            ClipToleranceSeconds = 0.3,
            SafeArea = new SafeAreaSetting { Left = 80, Right = 200, Top = 220, Bottom = 500 },
            Caption = s.Caption with { FontSize = 60 },
            Overlay = s.Overlay with { FontSize = 44 },
            CaptionPageMinimumSeconds = 1.5,
        });
        settings.Validate(HeldData.Current);
    }

    [Fact]
    public void TheCeoValuesThemselvesAreAcceptedAndHeld()
    {
        Settings(s => s).Validate(HeldData.Current);
        Assert.Equal(new SafeAreaSetting { Left = 60, Right = 180, Top = 200, Bottom = 480 }, HeldData.Current.SafeAreaMinimum);
        Assert.Equal(0.5, HeldData.Current.ClipToleranceMaximumSeconds);
        Assert.Equal(1.2, HeldData.Current.CaptionPageMinimumSeconds);
    }

    [Fact]
    public void ASettingsFileLooserThanTheCeoIsRefusedWhenRead()
    {
        var folder = PilotTestEnvironment.NewFolder("settings-file");
        var path = PilotTestEnvironment.WriteSettings(folder, Settings(s => s with { ClipToleranceSeconds = 0.6 }));
        Assert.Equal("settings-looser-than-ceo", Assert.Throws<PilotRefusal>(() => PilotSettings.Read(path)).Code);
    }
}

/// <summary>Caption pages keep the minimum on-screen time and rejoin into the script (decision record entry 1309).</summary>
[Trait("Category", "Unit")]
public sealed class CaptionPageMinimumTests
{
    private static readonly FaceMetrics Metrics = FaceMetrics.Read(File.ReadAllBytes(PilotTestEnvironment.FacePath));

    private static readonly ScriptSentence Disclosure = new(1, SentenceKind.OpeningDisclosure, HeldData.Current.DisclosureSentence, "x", "y");

    private static (PilotSettings Settings, TextLayout Layout) Setup(double minimum = 1.2)
    {
        var settings = PilotTestEnvironment.Settings(PilotTestEnvironment.NewFolder("pages"), s => s with { CaptionPageMinimumSeconds = minimum });
        return (settings, CaptionLayout.Boxes(settings, Metrics, HeldData.Current));
    }

    [Theory]
    [InlineData(1.0, 3.4, 1.2)]
    [InlineData(1.0, 3.401, 1.2)]
    [InlineData(0.37, 2.95, 1.2)]
    [InlineData(1.0, 7.0, 1.2)]
    [InlineData(1.0, 4.0, 1.5)]
    public void EveryPageStaysOnScreenAtLeastTheMinimumAndThePagesFillTheWindow(double start, double end, double minimum)
    {
        var (settings, layout) = Setup(minimum);
        var pages = CaptionLayout.Pages([Disclosure], [(start, end)], layout, settings, Metrics);
        Assert.Equal(2, pages.Count);
        Assert.All(pages, p => Assert.True(p.End - p.Start >= minimum - 1e-9, $"page {p.Page}: {p.End - p.Start}"));
        Assert.Equal(start, pages[0].Start);
        Assert.Equal(end, pages[^1].End);
        Assert.Equal(pages[0].End, pages[1].Start);
    }

    [Fact]
    public void AWindowTooShortForTheDisclosuresPagesIsRefusedNamingTheSentence()
    {
        var (settings, layout) = Setup();
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionLayout.Pages([Disclosure], [(1.0, 3.39)], layout, settings, Metrics));
        Assert.Equal("caption-page-too-short", refusal.Code);
        Assert.Contains("Câu 1", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("2 trang", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AOnePageSentenceShorterThanTheMinimumIsRefusedNamingIt()
    {
        var (settings, layout) = Setup();
        var short_ = new ScriptSentence(4, SentenceKind.CallToAction, HeldData.Current.CallToAction[0], "x", "y");
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionLayout.Pages([Disclosure, short_], [(0.0, 3.0), (3.5, 4.6)], layout, settings, Metrics));
        Assert.Equal("caption-page-too-short", refusal.Code);
        Assert.Contains("Câu 4", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ThePagesOfEveryFixedSentenceAndAGeneratedScriptRejoinIntoTheScriptWordForWord()
    {
        var (settings, layout) = Setup();
        var sheet = new SheetFixture();
        sheet.Facts.Add(sheet.Row("bình giữ nhiệt", "chất liệu thân bình và nắp", "thép không gỉ 304 phủ sơn tĩnh điện, nắp nhựa PP", "Thông số", "chup-2.png"));
        var script = ScriptGenerator.Generate(sheet.Check(), HeldData.Current, BannedTermCheck.Held);
        var sentences = script.Sentences.Concat(HeldData.Current.CallToAction.Select((c, i) => new ScriptSentence(20 + i, SentenceKind.CallToAction, c, "x", "y"))).ToList();
        var windows = sentences.Select((_, i) => (i * 5.0, (i * 5.0) + 4.5)).ToList();
        var pages = CaptionLayout.Pages(sentences, windows, layout, settings, Metrics);
        Assert.All(sentences, s => Assert.Equal(s.Text, CaptionLayout.Rejoined(pages, s.Number)));
        Assert.All(pages, p => Assert.InRange(p.Lines.Count, 1, 2));
    }
}

/// <summary>A timing-file refusal names the first failing sentence and non-numeric values are refused (correction request 9).</summary>
[Trait("Category", "Unit")]
public sealed class TimingFileNamingTests
{
    [Theory]
    [InlineData("NaN 5.40")]
    [InlineData("3.00 NaN")]
    [InlineData("3.00 Infinity")]
    [InlineData("-Infinity 5.40")]
    [InlineData("∞ 5.40")]
    [InlineData("ba 5.40")]
    [InlineData("3,00 5,40")]
    [InlineData("3.00")]
    [InlineData("3.00 5.40 6.00")]
    [InlineData("1e1 5.40")]
    public void ANonNumericOrMalformedLineIsRefusedNamingItsSentence(string second)
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile($"0.00 2.40\n{second}\n6.00 8.40\n", 3, 8.4));
        Assert.Equal("timing-file-invalid", refusal.Code);
        Assert.Contains("câu 2", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("câu 1", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AMissingLineNamesTheFirstSentenceWithoutOne()
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile("0.00 2.40\n", 3, 8.4));
        Assert.Contains("câu 2 không có dòng", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEarlierBadLineIsNamedBeforeACountMismatch()
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile("0.00 2.40\n2.00 1.00\n", 3, 8.4));
        Assert.Contains("câu 2", refusal.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("không có dòng", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnExtraLineIsRefusedNamingTheLineBeyondTheLastSentence()
    {
        var refusal = Assert.Throws<PilotRefusal>(() => CaptionTiming.ReadTimingFile("0.00 2.40\n3.00 5.40\n6.00 8.40\n8.40 8.40\n", 3, 8.4));
        Assert.Equal("timing-file-invalid", refusal.Code);
        Assert.Contains("dòng 4", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("không có câu 4", refusal.Message, StringComparison.Ordinal);
    }
}

/// <summary>Ledger reuse across superseded entries and the reconciliation on every read (correction requests 6 and 7).</summary>
[Trait("Category", "Unit")]
public sealed class LedgerReconciliationTests
{
    private static FactSheet Sheet(string code, string page = "/p/1", string focus = "a") =>
        new(code, "Sản phẩm", "bình", "loại 1", page, "Gian hàng", "2026-10-26", "bếp", ["a.png"], 1, focus, [], 1);

    /// <summary>Appends one entry and, like the producer, writes its copy into a package folder under runs.</summary>
    private static LedgerEntry Produce(Ledger ledger, string folder, string code, string recording)
    {
        var package = Path.Combine(folder, "runs", $"{code}-{ledger.Entries.Count + 1}");
        Directory.CreateDirectory(package);
        var entry = ledger.Append(Sheet(code, "/p/" + code), recording, "video-" + recording, package, DateTimeOffset.UnixEpoch);
        File.WriteAllBytes(Path.Combine(package, PackageFiles.LedgerEntry), Ledger.SerializeEntry(entry));
        return entry;
    }

    private static void KeepFirst(string path, int count)
    {
        var file = JsonNode.Parse(File.ReadAllText(path))!;
        var entries = file["entries"]!.AsArray();
        while (entries.Count > count)
        {
            entries.RemoveAt(entries.Count - 1);
        }

        File.WriteAllText(path, file.ToJsonString());
    }

    [Fact]
    public void TheRecordingOfASupersededEntryIsRefusedForAnotherCode()
    {
        var ledger = Ledger.Open(PilotTestEnvironment.NewFolder("ledger"), HeldData.Current);
        ledger.Append(Sheet("V-01"), "rec-old", "v1", "pkg1", DateTimeOffset.UnixEpoch);
        ledger.Append(Sheet("V-01"), "rec-new", "v2", "pkg2", DateTimeOffset.UnixEpoch);
        Assert.Equal("rec-new", Assert.Single(ledger.Current).RecordingSha256);
        var refusal = Assert.Throws<PilotRefusal>(() => ledger.RefuseReusedRecording("REC-OLD", "V-02"));
        Assert.Equal("recording-reused", refusal.Code);
        Assert.Contains("V-01", refusal.Message, StringComparison.Ordinal);
        ledger.RefuseReusedRecording("rec-old", "V-01");
    }

    [Fact]
    public void ALedgerWithItsCopiesReopensAfterEachWrite()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        for (var i = 1; i <= 3; i++)
        {
            Produce(Ledger.Open(folder, HeldData.Current), folder, $"V-0{i}", $"r{i}");
        }

        Assert.Equal(3, Ledger.Open(folder, HeldData.Current).Entries.Count);
    }

    [Fact]
    public void ADeletedLedgerIsRefusedWhileItsPreviousGenerationRemains()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        var ledger = Ledger.Open(folder, HeldData.Current);
        ledger.Append(Sheet("V-01"), "r1", "v1", "pkg", DateTimeOffset.UnixEpoch);
        ledger.Append(Sheet("V-02", "/p/2"), "r2", "v2", "pkg", DateTimeOffset.UnixEpoch);
        File.Delete(Path.Combine(folder, Ledger.FileName));
        var refusal = Assert.Throws<PilotRefusal>(() => Ledger.Open(folder, HeldData.Current));
        Assert.Equal("ledger-truncated", refusal.Code);
        Assert.Contains("đã bị xoá", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALedgerDeletedWithItsPreviousGenerationIsRefusedByThePackageCopies()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        Produce(Ledger.Open(folder, HeldData.Current), folder, "V-01", "r1");
        Produce(Ledger.Open(folder, HeldData.Current), folder, "V-02", "r2");
        File.Delete(Path.Combine(folder, Ledger.FileName));
        File.Delete(Path.Combine(folder, Ledger.PreviousFileName));
        var refusal = Assert.Throws<PilotRefusal>(() => Ledger.Open(folder, HeldData.Current));
        Assert.Equal("ledger-entry-missing", refusal.Code);
        Assert.Contains("V-01", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AChainTruncatedBelowThePreviousGenerationIsRefused()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        var ledger = Ledger.Open(folder, HeldData.Current);
        for (var i = 1; i <= 3; i++)
        {
            ledger.Append(Sheet($"V-0{i}", $"/p/{i}"), $"r{i}", $"v{i}", "pkg", DateTimeOffset.UnixEpoch);
        }

        KeepFirst(Path.Combine(folder, Ledger.FileName), 1);
        var refusal = Assert.Throws<PilotRefusal>(() => Ledger.Open(folder, HeldData.Current));
        Assert.Equal("ledger-truncated", refusal.Code);
        Assert.Contains("1 mục", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("2 mục", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ATailTruncationThePreviousGenerationCannotSeeIsRefusedByThePackageCopy()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        Produce(Ledger.Open(folder, HeldData.Current), folder, "V-01", "r1");
        Produce(Ledger.Open(folder, HeldData.Current), folder, "V-02", "r2");
        KeepFirst(Path.Combine(folder, Ledger.FileName), 1);
        var refusal = Assert.Throws<PilotRefusal>(() => Ledger.Open(folder, HeldData.Current));
        Assert.Equal("ledger-entry-missing", refusal.Code);
        Assert.Contains("V-02", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ALedgerReplacedByAnotherChainIsRefused()
    {
        var folder = PilotTestEnvironment.NewFolder("ledger");
        var ledger = Ledger.Open(folder, HeldData.Current);
        ledger.Append(Sheet("V-01"), "r1", "v1", "pkg", DateTimeOffset.UnixEpoch);
        ledger.Append(Sheet("V-02", "/p/2"), "r2", "v2", "pkg", DateTimeOffset.UnixEpoch);
        var other = PilotTestEnvironment.NewFolder("ledger");
        var replacement = Ledger.Open(other, HeldData.Current);
        replacement.Append(Sheet("V-09", "/p/9"), "r9", "v9", "pkg", DateTimeOffset.UnixEpoch);
        replacement.Append(Sheet("V-08", "/p/8"), "r8", "v8", "pkg", DateTimeOffset.UnixEpoch);
        File.Copy(Path.Combine(other, Ledger.FileName), Path.Combine(folder, Ledger.FileName), overwrite: true);
        Assert.Equal("ledger-truncated", Assert.Throws<PilotRefusal>(() => Ledger.Open(folder, HeldData.Current)).Code);
    }
}

/// <summary>Approval accepts only a package that agrees with its ledger entry and names a plain video file (correction request 8).</summary>
[Trait("Category", "Unit")]
public sealed class ApprovalLedgerAgreementTests
{
    private static string Sha(string folder) => MediaTool.Sha256File(Path.Combine(folder, PackageFiles.Video));

    private static void EditManifest(string folder, Action<JsonNode> edit)
    {
        var path = Path.Combine(folder, PackageFiles.Manifest);
        var manifest = JsonNode.Parse(File.ReadAllText(path))!;
        edit(manifest);
        File.WriteAllText(path, manifest.ToJsonString());
    }

    [Fact]
    public void AHeldPackageWhoseStatusIsEditedToReadyIsRefused()
    {
        var folder = ApprovalAndChecklistTests.Package("held");
        EditManifest(folder, m => m["status"] = "ready");
        var refusal = Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", Sha(folder), DateTimeOffset.UnixEpoch));
        Assert.Equal("package-ledger-mismatch", refusal.Code);
        Assert.Equal("package-ledger-mismatch", Assert.Throws<PilotRefusal>(() => Approval.Verify(folder)).Code);
        Assert.False(File.Exists(Path.Combine(folder, PackageFiles.Approval)));
    }

    [Theory]
    [InlineData("../video.mp4")]
    [InlineData("..\\khac\\video.mp4")]
    [InlineData("C:\\video.mp4")]
    [InlineData("inputs/recording.wav")]
    [InlineData("")]
    [InlineData("..")]
    public void AVideoFileThatIsNotAPlainNameInThePackageIsRefused(string name)
    {
        var folder = ApprovalAndChecklistTests.Package();
        var sha = Sha(folder);
        EditManifest(folder, m => m["videoFile"] = name);
        Assert.Equal("package-file-not-plain", Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", sha, DateTimeOffset.UnixEpoch)).Code);
        Assert.Equal("package-file-not-plain", Assert.Throws<PilotRefusal>(() => Approval.Verify(folder)).Code);
    }

    [Fact]
    public void AVideoFileRenamedToAnotherFileWithMatchingManifestDigestIsRefusedByTheLedger()
    {
        var folder = ApprovalAndChecklistTests.Package();
        var other = Path.Combine(folder, "khac.mp4");
        File.WriteAllText(other, "another video");
        var otherSha = MediaTool.Sha256File(other);
        EditManifest(folder, m =>
        {
            m["videoFile"] = "khac.mp4";
            m["videoSha256"] = otherSha;
        });
        Assert.Equal("package-ledger-mismatch", Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", otherSha, DateTimeOffset.UnixEpoch)).Code);
    }

    [Fact]
    public void APackageWithoutItsLedgerEntryCopyIsRefused()
    {
        var folder = ApprovalAndChecklistTests.Package();
        File.Delete(Path.Combine(folder, PackageFiles.LedgerEntry));
        Assert.Equal("package-ledger-mismatch", Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", Sha(folder), DateTimeOffset.UnixEpoch)).Code);
    }

    [Fact]
    public void AnEditedLedgerEntryCopyIsRefused()
    {
        var folder = ApprovalAndChecklistTests.Package();
        var path = Path.Combine(folder, PackageFiles.LedgerEntry);
        File.WriteAllText(path, File.ReadAllText(path).Replace("rec-V-01", "rec-V-09", StringComparison.Ordinal));
        Assert.Equal("package-ledger-mismatch", Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", Sha(folder), DateTimeOffset.UnixEpoch)).Code);
    }

    [Fact]
    public void APackageWhosePilotLedgerLacksItsEntryIsRefused()
    {
        var folder = ApprovalAndChecklistTests.Package();
        var pilot = Path.GetDirectoryName(Path.GetDirectoryName(folder))!;
        var foreign = ApprovalAndChecklistTests.Package(code: "V-02");
        var foreignPilot = Path.GetDirectoryName(Path.GetDirectoryName(foreign))!;
        File.Copy(Path.Combine(foreignPilot, Ledger.FileName), Path.Combine(pilot, Ledger.FileName), overwrite: true);
        var refusal = Assert.Throws<PilotRefusal>(() => Approval.Approve(folder, "CEO", Sha(folder), DateTimeOffset.UnixEpoch));
        Assert.Contains(refusal.Code, new[] { "package-ledger-mismatch", "ledger-entry-missing" });
    }

    [Fact]
    public void APackageMovedOutOfItsPilotFolderIsRefused()
    {
        var folder = ApprovalAndChecklistTests.Package();
        var moved = Path.Combine(PilotTestEnvironment.NewFolder("moved"), "goi");
        Directory.Move(folder, moved);
        Assert.Equal("package-ledger-mismatch", Assert.Throws<PilotRefusal>(() => Approval.Approve(moved, "CEO", Sha(moved), DateTimeOffset.UnixEpoch)).Code);
    }

    [Fact]
    public void AnAgreeingPackageIsApprovedAndVerified()
    {
        var folder = ApprovalAndChecklistTests.Package();
        var record = Approval.Approve(folder, "CEO", Sha(folder), DateTimeOffset.UnixEpoch);
        Assert.Equal(Sha(folder), record.VideoSha256);
        var (manifest, approval, sha) = Approval.Verify(folder);
        Assert.Equal("ready", manifest.Status);
        Assert.NotNull(approval);
        Assert.Equal(record.VideoSha256, sha);
    }
}

/// <summary>The example settings carry the decision record entry 1309 page minimum.</summary>
[Trait("Category", "Unit")]
public sealed class ExampleSettingsPageMinimumTests
{
    [Fact]
    public void TheExampleSettingsCarryTheCaptionPageMinimum()
    {
        var path = Path.Combine(PilotTestEnvironment.RepositoryRoot, "pilot-affiliate-vn", "mc-16", "vi-du-cai-dat.json");
        var settings = JsonSerializer.Deserialize<PilotSettings>(File.ReadAllBytes(path), PilotSettings.Json)!;
        Assert.Equal(1.2, settings.CaptionPageMinimumSeconds);
        settings.RefuseLooserThanCeo(HeldData.Current);
    }
}
