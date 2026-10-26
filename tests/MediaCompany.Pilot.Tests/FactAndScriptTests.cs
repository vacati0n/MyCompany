using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace MediaCompany.Pilot.Tests;

/// <summary>A fact sheet in a temporary input folder with stand-in files (not media) for the named images and screenshots.</summary>
internal sealed class SheetFixture
{
    public SheetFixture()
    {
        Folder = PilotTestEnvironment.NewFolder("sheet");
        foreach (var name in new[] { "anh-1.png", "anh-2.png", "anh-3.png", "chup-1.png", "chup-2.png", "chup-3.png", "chup-4.png" })
        {
            File.WriteAllBytes(Path.Combine(Folder, name), [1, 2, 3]);
        }

        Json = JsonNode.Parse("""
        {
          "formVersion": 1,
          "videoCode": "V-01",
          "productName": "Bình giữ nhiệt mẫu 500 ml",
          "subjectNoun": "bình giữ nhiệt",
          "classification": "loại 500 ml màu xanh",
          "pagePath": "/san-pham/binh-giu-nhiet-mau",
          "shopNameAndMall": "Gian hàng mẫu, không phải Mall",
          "readDate": "2026-10-26",
          "niche": "đồ dùng nhà bếp",
          "imagesUsed": ["anh-1.png", "anh-2.png", "anh-3.png"],
          "productVideoNumber": 1,
          "videoFocus": "dung tích",
          "facts": [
            { "label": "F1", "subjectNoun": "bình giữ nhiệt", "attribute": "dung tích", "value": "500 ml", "sourceSection": "Thông số: Dung tích", "screenshot": "chup-1.png" }
          ]
        }
        """)!.AsObject();
    }

    public string Folder { get; }

    public JsonObject Json { get; }

    public JsonArray Facts => Json["facts"]!.AsArray();

    public JsonObject Row(string subject, string attribute, string value, string source, string screenshot) => new()
    {
        ["subjectNoun"] = subject,
        ["attribute"] = attribute,
        ["value"] = value,
        ["sourceSection"] = source,
        ["screenshot"] = screenshot,
    };

    public FactSheet Check() =>
        FactSheetCheck.Check(Encoding.UTF8.GetBytes(Json.ToJsonString()), Folder, BannedTermCheck.Held);

    public PilotRefusal Refused() => Assert.Throws<PilotRefusal>(Check);
}

[Trait("Category", "Unit")]
public sealed class FactSheetTests
{
    [Fact]
    public void ACompleteFactSheetIsAccepted()
    {
        var sheet = new SheetFixture().Check();
        Assert.Equal("V-01", sheet.VideoCode);
        Assert.Single(sheet.Facts);
        Assert.Equal(3, sheet.ImagesUsed.Count);
        Assert.Equal(1, sheet.CallToAction);
    }

    [Theory]
    [InlineData("sourceSection", "mục nguồn trên trang")]
    [InlineData("screenshot", "tên tệp ảnh chụp màn hình")]
    [InlineData("attribute", "thuộc tính")]
    [InlineData("value", "giá trị chép đúng trên trang")]
    [InlineData("subjectNoun", "chủ thể")]
    public void ARowWithABlankFieldIsRefusedNamingTheRowAndTheField(string member, string label)
    {
        var fixture = new SheetFixture();
        fixture.Facts.Add(fixture.Row("bình giữ nhiệt", "chất liệu", "thép không gỉ 304", "Thông số: Chất liệu", "chup-2.png"));
        fixture.Facts[1]![member] = "  ";
        var refusal = fixture.Refused();
        Assert.Equal("row-field-missing", refusal.Code);
        Assert.Contains("Dòng F2", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(label, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AScreenshotWithNoFileIsRefusedNamingTheRowAndTheFile()
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["screenshot"] = "khong-co.png";
        var refusal = fixture.Refused();
        Assert.Equal("file-missing", refusal.Code);
        Assert.Contains("Dòng F1", refusal.Message, StringComparison.Ordinal);
        Assert.Contains("khong-co.png", refusal.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("..\\chup-1.png")]
    [InlineData("C:\\chup-1.png")]
    [InlineData("thu-muc/chup-1.png")]
    public void AScreenshotNameThatIsNotAPlainNameIsRefused(string name)
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["screenshot"] = name;
        Assert.Equal("file-name-not-plain", fixture.Refused().Code);
    }

    public static TheoryData<string, string> HeaderFields()
    {
        var data = new TheoryData<string, string>();
        foreach (var (member, label) in FactSheetCheck.HeaderFields)
        {
            data.Add(member, label);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(HeaderFields))]
    public void EachOfTheElevenHeaderFieldsBlankedIsRefusedNamingTheField(string member, string label)
    {
        Assert.Equal(11, FactSheetCheck.HeaderFields.Count);
        var fixture = new SheetFixture();
        fixture.Json[member] = member == "imagesUsed" ? new JsonArray() : member == "productVideoNumber" ? null : "";
        var refusal = fixture.Refused();
        Assert.Equal("header-field-missing", refusal.Code);
        Assert.Contains(label, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FourFactRowsAreRefused()
    {
        var fixture = new SheetFixture();
        for (var i = 2; i <= 4; i++)
        {
            fixture.Facts.Add(fixture.Row("bình giữ nhiệt", $"thuộc tính {i}", $"giá trị {i}", "Thông số", $"chup-{i}.png"));
        }

        Assert.Equal("fact-row-count", fixture.Refused().Code);
    }

    [Fact]
    public void AnUnknownMemberIsRefused()
    {
        var fixture = new SheetFixture();
        fixture.Json["price"] = "99k";
        Assert.Equal("fact-sheet-unreadable", fixture.Refused().Code);
    }

    [Theory]
    [InlineData("value", "199.000đ", "199.000đ")]
    [InlineData("value", "bán chạy nhất", "nhất")]
    [InlineData("attribute", "đánh giá", "đánh giá")]
    [InlineData("value", "an toàn cho sức khỏe", "sức khỏe")]
    public void ABannedTermInAFactRowIsRefusedNamingTheRowAndTheTerm(string member, string text, string term)
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]![member] = text;
        var refusal = fixture.Refused();
        Assert.Equal("banned-term", refusal.Code);
        Assert.Contains("Dòng F1", refusal.Message, StringComparison.Ordinal);
        Assert.Contains(term, refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnOverrideWithABannedTermIsRefusedAndACleanOverridePasses()
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["override"] = "Bình giữ nhiệt tốt nhất, dung tích 500 ml.";
        Assert.Equal("banned-term", fixture.Refused().Code);
        fixture.Facts[0]!["override"] = "Bình giữ nhiệt này chứa được 500 ml.";
        var sheet = fixture.Check();
        Assert.Equal("Bình giữ nhiệt này chứa được 500 ml.", sheet.Facts[0].Override);
    }

    [Fact]
    public void AnOverrideThatChangesTheValueIsRefused()
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["override"] = "Bình giữ nhiệt này chứa được nửa lít.";
        Assert.Equal("override-value-changed", fixture.Refused().Code);
    }

    [Fact]
    public void AControlCharacterIsRefused()
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["value"] = "500\u200Bml";
        Assert.Equal("control-character", fixture.Refused().Code);
    }

    [Fact]
    public void ACallToActionOutsideTheHeldListIsRefused()
    {
        var fixture = new SheetFixture();
        fixture.Json["callToAction"] = 3;
        Assert.Equal("header-field-invalid", fixture.Refused().Code);
    }
}

[Trait("Category", "Unit")]
public sealed class BannedTermTests
{
    private static readonly BannedTermCheck Check = BannedTermCheck.Held;

    [Theory]
    [InlineData("nhất định", true)]
    [InlineData("nhất", false)]
    [InlineData("giá đỡ điện thoại", true)]
    [InlineData("hơn", true)]
    [InlineData("1,5 lít", true)]
    [InlineData("NHẤT", false)]
    [InlineData("giá", true)]
    [InlineData("220V", true)]
    [InlineData("nhỏ hơn", true)]
    [InlineData("dao", true)]
    public void WholeWordMatchingAfterNormalizationGivesTheStatedResult(string text, bool passes)
    {
        Assert.Equal(passes, Check.Find("thử", text).Count == 0);
    }

    [Fact]
    public void DecomposedTextIsNormalizedBeforeMatching()
    {
        var decomposed = "nha\u0302\u0301t".Normalize(NormalizationForm.FormD);
        Assert.NotEmpty(Check.Find("thử", decomposed));
    }

    [Theory]
    [InlineData("a", "giá chỉ 199.000đ")]
    [InlineData("a", "chỉ 99k")]
    [InlineData("a", "150.000 đồng")]
    [InlineData("a", "200000 VND")]
    [InlineData("a", "đang giảm giá")]
    [InlineData("a", "khuyến mãi lớn")]
    [InlineData("a", "có mã giảm giá")]
    [InlineData("a", "đã bán 10 nghìn")]
    [InlineData("a", "freeship toàn quốc")]
    [InlineData("a", "5 sao")]
    [InlineData("a", "★★★★★")]
    [InlineData("b", "loại tốt nhất")]
    [InlineData("b", "duy nhất")]
    [InlineData("b", "số một")]
    [InlineData("c", "tốt cho sức khỏe")]
    [InlineData("c", "tốt cho sức khoẻ")]
    [InlineData("c", "kháng khuẩn")]
    [InlineData("c", "an toàn thực phẩm")]
    [InlineData("d", "mình đã dùng")]
    [InlineData("d", "review thật")]
    [InlineData("d", "trải nghiệm")]
    [InlineData("e", "rẻ hơn TikTok")]
    [InlineData("e", "Lazada")]
    [InlineData("f", "so với loại khác")]
    [InlineData("f", "hơn hẳn")]
    [InlineData("g", "chỉ hôm nay")]
    [InlineData("g", "sắp hết hàng")]
    public void EachBannedClassIsFound(string classId, string text)
    {
        Assert.Contains(Check.Find("thử", text), f => f.ClassId == classId);
    }
}

[Trait("Category", "Unit")]
public sealed class ScriptTests
{
    private const string Disclosure = "Video tạo bởi AI. Video có liên kết tiếp thị, tôi nhận hoa hồng nếu bạn mua qua liên kết.";

    private static Script Generate(SheetFixture fixture) => ScriptGenerator.Generate(fixture.Check(), HeldData.Current, BannedTermCheck.Held);

    [Fact]
    public void TheOpeningAndClosingSentencesAreTheDisclosureByteForByte()
    {
        var script = Generate(new SheetFixture());
        Assert.Equal(Encoding.UTF8.GetBytes(Disclosure), Encoding.UTF8.GetBytes(script.Sentences[0].Text));
        Assert.Equal(Encoding.UTF8.GetBytes(Disclosure), Encoding.UTF8.GetBytes(script.Sentences[^1].Text));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void TheOrderIsDisclosureIntroductionFactsCallToActionDisclosure(int rows)
    {
        var fixture = new SheetFixture();
        for (var i = 2; i <= rows; i++)
        {
            fixture.Facts.Add(fixture.Row("bình giữ nhiệt", $"thuộc tính {i}", $"giá trị {i}", "Thông số", $"chup-{i}.png"));
        }

        var kinds = Generate(fixture).Sentences.Select(s => s.Kind).ToList();
        var expected = new List<string> { SentenceKind.OpeningDisclosure, SentenceKind.Introduction };
        expected.AddRange(Enumerable.Repeat(SentenceKind.Fact, rows));
        expected.AddRange([SentenceKind.CallToAction, SentenceKind.ClosingDisclosure]);
        Assert.Equal(expected, kinds);
    }

    [Fact]
    public void EachSentenceClassIsItsFrameOverTheRowWithTheValueUnchanged()
    {
        var script = Generate(new SheetFixture());
        Assert.Equal("Đây là bình giữ nhiệt, loại 500 ml màu xanh.", script.Sentences[1].Text);
        Assert.Equal("Bình giữ nhiệt có dung tích 500 ml.", script.Sentences[2].Text);
        Assert.Equal("Bấm vào giỏ hàng hoặc link sản phẩm để xem chi tiết.", script.Sentences[3].Text);
    }

    [Fact]
    public void TheSecondCallToActionIsUsedWhenTheSheetChoosesIt()
    {
        var fixture = new SheetFixture();
        fixture.Json["callToAction"] = 2;
        Assert.Equal("Thông tin chi tiết, bạn xem trong thẻ sản phẩm của video.", Generate(fixture).Sentences[3].Text);
    }

    [Fact]
    public void AnOverrideReplacesTheFrameSentence()
    {
        var fixture = new SheetFixture();
        fixture.Facts[0]!["override"] = "Bình này chứa được 500 ml.";
        Assert.Equal("Bình này chứa được 500 ml.", Generate(fixture).Sentences[2].Text);
    }

    [Fact]
    public void TwoRunsGiveTheSameScriptDigest()
    {
        var fixture = new SheetFixture();
        var first = ScriptGenerator.Sha256(ScriptGenerator.Serialize(Generate(fixture)));
        var second = ScriptGenerator.Sha256(ScriptGenerator.Serialize(Generate(fixture)));
        Assert.Equal(first, second);
    }

    [Fact]
    public void EveryCallToActionEntryPassesTheBannedTermCheckAndAMoneyAmountIsRefused()
    {
        Assert.NotEmpty(HeldData.Current.CallToAction);
        Assert.All(HeldData.Current.CallToAction, entry => Assert.Empty(BannedTermCheck.Held.Find("kêu gọi", entry)));
        var refusal = Assert.Throws<PilotRefusal>(() => BannedTermCheck.Held.Refuse("kêu gọi", "Bấm vào giỏ hàng, chỉ 99k."));
        Assert.Equal("banned-term", refusal.Code);
    }

    [Fact]
    public void PartEHasOneRowPerSentenceWithNoBlankSourceCell()
    {
        var fixture = new SheetFixture();
        fixture.Facts.Add(fixture.Row("bình giữ nhiệt", "chất liệu", "thép không gỉ 304", "Thông số: Chất liệu", "chup-2.png"));
        var sheet = fixture.Check();
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        var rendered = ScriptGenerator.Render(script, sheet);
        var part = rendered[rendered.IndexOf("## Phần E", StringComparison.Ordinal)..];
        var rows = part.Split('\n').Where(l => l.StartsWith("| ", StringComparison.Ordinal) && char.IsAsciiDigit(l[2])).ToList();
        Assert.Equal(script.Sentences.Count, rows.Count);
        Assert.All(rows, row =>
        {
            var cells = row.Split('|').Select(c => c.Trim()).ToArray();
            Assert.False(string.IsNullOrWhiteSpace(cells[3]));
            Assert.False(string.IsNullOrWhiteSpace(cells[4]));
        });
    }

    [Fact]
    public void AnInjectedUnmappedSentenceIsRefusedNamingTheSentence()
    {
        var fixture = new SheetFixture();
        var sheet = fixture.Check();
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        var sentences = script.Sentences.ToList();
        sentences.Insert(3, new ScriptSentence(4, SentenceKind.Fact, "Gian hàng này giao trong ngày.", "F9", "không có"));
        var bytes = ScriptGenerator.Serialize(script with { Sentences = sentences });
        var refusal = Assert.Throws<PilotRefusal>(() => ScriptGenerator.Approved(bytes, sheet, HeldData.Current, BannedTermCheck.Held));
        Assert.Equal("unmapped-sentence", refusal.Code);
        Assert.Contains("Gian hàng này giao trong ngày.", refusal.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ABannedTermWrittenIntoTheScriptFileIsRefused()
    {
        var fixture = new SheetFixture();
        var sheet = fixture.Check();
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        var sentences = script.Sentences.ToList();
        sentences[2] = sentences[2] with { Text = "Bình giữ nhiệt tốt nhất có dung tích 500 ml." };
        var bytes = ScriptGenerator.Serialize(script with { Sentences = sentences });
        Assert.Equal("banned-term", Assert.Throws<PilotRefusal>(() => ScriptGenerator.Approved(bytes, sheet, HeldData.Current, BannedTermCheck.Held)).Code);
    }

    [Fact]
    public void TheGeneratedScriptIsApprovedAsWritten()
    {
        var sheet = new SheetFixture().Check();
        var script = ScriptGenerator.Generate(sheet, HeldData.Current, BannedTermCheck.Held);
        var approved = ScriptGenerator.Approved(ScriptGenerator.Serialize(script), sheet, HeldData.Current, BannedTermCheck.Held);
        Assert.Equal(script.Sentences.Select(s => s.Text), approved.Sentences.Select(s => s.Text));
        _ = JsonSerializer.Serialize(approved);
    }
}
