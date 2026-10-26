using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace MediaCompany.Pilot;

/// <summary>The sentence classes of scope criterion 12, in kit 03 Part B order.</summary>
public static class SentenceKind
{
    public const string OpeningDisclosure = "opening-disclosure";
    public const string Introduction = "introduction";
    public const string Fact = "fact";
    public const string CallToAction = "call-to-action";
    public const string ClosingDisclosure = "closing-disclosure";
}

/// <summary>One script sentence with its Part E source line: the fact row or fixed-sentence label and the source section.</summary>
public sealed record ScriptSentence(int Number, string Kind, string Text, string Label, string Source);

/// <summary>The approved script: the sentences in Part B order. Its file bytes are deterministic, so its SHA-256 is too.</summary>
public sealed record Script(int FormVersion, string VideoCode, IReadOnlyList<ScriptSentence> Sentences)
{
    public const int KnownFormVersion = 1;
}

/// <summary>
/// THE SCRIPT GENERATOR (scope items 3 and 4, design D-007). Fills kit 03 Part B from an accepted fact sheet with fixed frames
/// only: the opening disclosure, "Đây là [chủ thể], [phân loại].", one sentence per fact row ("[Chủ thể] có [thuộc tính] [giá trị]."
/// or the row's checked override), one call to action from the held list, and the closing disclosure. No model and no
/// paraphrase: every sentence is a frame filled with the operator's own words, and every sentence passes the banned-term check.
/// </summary>
public static class ScriptGenerator
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static Script Generate(FactSheet sheet, HeldData held, BannedTermCheck banned)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(banned);
        var sentences = new List<ScriptSentence>();
        void Add(string kind, string text, string label, string source)
        {
            var number = sentences.Count + 1;
            banned.Refuse($"Câu {number} của kịch bản", text);
            sentences.Add(new ScriptSentence(number, kind, text, label, source));
        }

        Add(SentenceKind.OpeningDisclosure, held.DisclosureSentence, "Câu cố định: công bố mở đầu", "Mẫu kịch bản 03, Phần B dòng 1");
        Add(SentenceKind.Introduction,
            held.IntroductionFrame.Replace("{subjectNoun}", sheet.SubjectNoun, StringComparison.Ordinal)
                .Replace("{classification}", sheet.Classification, StringComparison.Ordinal),
            "Phần A: chủ thể và phân loại",
            $"Phần A của bảng thông tin: tên và phân loại trên trang {sheet.PagePath}, đọc ngày {sheet.ReadDate}");
        foreach (var row in sheet.Facts)
        {
            var text = row.Override ?? held.FactFrame
                .Replace("{SubjectNoun}", VietnameseText.CapitalizeFirst(row.SubjectNoun), StringComparison.Ordinal)
                .Replace("{attribute}", row.Attribute, StringComparison.Ordinal)
                .Replace("{value}", row.Value, StringComparison.Ordinal);
            Add(SentenceKind.Fact, text, row.Label, $"{row.SourceSection} (ảnh chụp {row.Screenshot})");
        }

        Add(SentenceKind.CallToAction, held.CallToAction[sheet.CallToAction - 1], $"Câu cố định: kêu gọi số {sheet.CallToAction}", "Mẫu kịch bản 03, Phần B dòng 6");
        Add(SentenceKind.ClosingDisclosure, held.DisclosureSentence, "Câu cố định: công bố kết thúc", "Mẫu kịch bản 03, Phần B dòng 7");
        return new Script(Script.KnownFormVersion, sheet.VideoCode, sentences);
    }

    /// <summary>The script file's bytes: UTF-8 JSON, LF line ends, no time stamp, so two runs give the same SHA-256.</summary>
    public static byte[] Serialize(Script script) =>
        Encoding.UTF8.GetBytes(JsonSerializer.Serialize(script, Json).ReplaceLineEndings("\n") + "\n");

    public static string Sha256(byte[] bytes) => Convert.ToHexStringLower(SHA256.HashData(bytes));

    /// <summary>The Vietnamese script and its Part E source lines for the operator to read and tick.</summary>
    public static string Render(Script script, FactSheet sheet)
    {
        ArgumentNullException.ThrowIfNull(script);
        ArgumentNullException.ThrowIfNull(sheet);
        var text = new StringBuilder();
        text.Append("# Kịch bản ").Append(script.VideoCode).Append(" — ").Append(sheet.ProductName).Append("\n\n");
        text.Append("Người vận hành đọc đúng từng chữ, ngừng rõ giữa hai câu, bằng giọng thật của chính mình; một bản ghi mới cho video này, không quá 60 giây.\n\n");
        foreach (var sentence in script.Sentences)
        {
            text.Append(sentence.Number).Append(". ").Append(sentence.Text).Append('\n');
        }

        text.Append("\n## Phần E. Dòng nguồn cho CEO kiểm\n\n");
        text.Append("| Câu | Nội dung | Dòng F hoặc câu cố định | Nguồn | Khớp chữ trên trang? |\n|---|---|---|---|---|\n");
        foreach (var sentence in script.Sentences)
        {
            text.Append("| ").Append(sentence.Number).Append(" | ").Append(sentence.Text).Append(" | ").Append(sentence.Label)
                .Append(" | ").Append(sentence.Source).Append(" | ☐ |\n");
        }

        return text.ToString();
    }

    /// <summary>
    /// Reads the operator's script file and holds it to the generator's own output (scope criteria 6 and 12): every sentence must
    /// pass the banned-term check and be a sentence the generator writes from this fact sheet, in the same order.
    /// </summary>
    public static Script Approved(byte[] scriptBytes, FactSheet sheet, HeldData held, BannedTermCheck banned)
    {
        ArgumentNullException.ThrowIfNull(scriptBytes);
        Script? supplied;
        try
        {
            supplied = JsonSerializer.Deserialize<Script>(scriptBytes, Json);
        }
        catch (JsonException error)
        {
            throw new PilotRefusal("script-unreadable", $"Tệp kịch bản không đọc được: {error.Message}");
        }

        if (supplied is null || supplied.FormVersion != Script.KnownFormVersion || supplied.Sentences is null)
        {
            throw new PilotRefusal("script-unreadable", $"Tệp kịch bản phải có formVersion {Script.KnownFormVersion} và danh sách câu.");
        }

        var expected = Generate(sheet, held, banned);
        foreach (var sentence in supplied.Sentences)
        {
            var text = VietnameseText.Nfc(sentence.Text ?? string.Empty);
            banned.Refuse($"Câu {sentence.Number} của kịch bản", text);
            if (!expected.Sentences.Any(e => string.Equals(e.Text, text, StringComparison.Ordinal)))
            {
                throw new PilotRefusal("unmapped-sentence",
                    $"Câu {sentence.Number} của kịch bản \"{text}\" không ứng với dòng thông tin hay câu cố định nào; tạo lại kịch bản bằng lệnh script.");
            }
        }

        var same = supplied.Sentences.Count == expected.Sentences.Count
            && supplied.Sentences.Zip(expected.Sentences).All(p => string.Equals(VietnameseText.Nfc(p.First.Text), p.Second.Text, StringComparison.Ordinal));
        if (!same || !string.Equals(supplied.VideoCode, sheet.VideoCode, StringComparison.Ordinal))
        {
            throw new PilotRefusal("script-differs",
                "Tệp kịch bản khác kịch bản công cụ viết từ bảng thông tin này (thứ tự, số câu hoặc mã video); tạo lại kịch bản bằng lệnh script.");
        }

        return expected;
    }
}
