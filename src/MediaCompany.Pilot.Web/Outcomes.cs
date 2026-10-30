using System.Text.RegularExpressions;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// What the page shows for one result (design decision D-010). <see cref="Line"/> is the toolchain's printed text, unchanged; the
/// reason and next step are Vietnamese. Kind is one of: dat (done), giu-lai (package held), tu-choi (refused), loi-cong-cu (tool
/// failure), loi-ung-dung (app fault).
/// </summary>
public sealed record OutcomeView(string Kind, int? ExitCode, string Code, string Line, string Reason, string NextStep);

/// <summary>
/// THE OUTCOME DISPLAY (design decision D-010). The toolchain's exit status decides the category: 0 done, 2 refused (or held), 3 tool
/// failure, 1 app fault. For the fourteen refusal codes of the operator guide's refusal table the guide's reason and next step are
/// shown; for any other code a general next step. Nothing here turns a refusal into a pass: the category comes from the exit status.
/// </summary>
public static partial class Outcomes
{
    public const string Done = "dat";
    public const string Held = "giu-lai";
    public const string Refused = "tu-choi";
    public const string ToolFailure = "loi-cong-cu";
    public const string AppFault = "loi-ung-dung";

    /// <summary>The fourteen codes of the operator guide's refusal table, with its reason and next step.</summary>
    public static readonly IReadOnlyDictionary<string, (string Reason, string Next)> Guide = new Dictionary<string, (string, string)>(StringComparer.Ordinal)
    {
        ["banned-term"] = ("Nội dung có từ bị cấm.",
            "Bỏ dòng đó, hoặc viết lại không có từ bị cấm. Không ghi giá, khuyến mãi, số sao, số đã bán, từ tuyệt đối, lời về sức khỏe, lời \"đã dùng\"."),
        ["override-first-person"] = ("Câu thay thế xưng ngôi thứ nhất, hoặc nói đã dùng hay nhận xét.",
            "Viết lại câu thay thế chỉ nói về sản phẩm, hoặc bỏ câu thay thế."),
        ["recording-too-long"] = ("Bản ghi dài quá 60 giây CEO cho phép.", "Ghi lại ngắn hơn 60 giây."),
        ["recording-reused"] = ("Bản ghi này đã dùng cho một video khác.", "Ghi một bản mới cho video này."),
        ["clip-not-portrait"] = ("Đoạn phim người dẫn không phải khung dọc.", "Xuất lại đoạn phim ở khung dọc 9:16."),
        ["clip-length-mismatch"] = ("Đoạn phim người dẫn dài khác bản ghi quá mức cho phép.", "Tạo lại đoạn phim từ đúng bản ghi đang dùng."),
        ["caption-page-too-short"] = ("Một trang phụ đề sẽ hiện ngắn hơn thời gian tối thiểu CEO đặt.",
            "Ghi lại, nói câu đó chậm hơn; hoặc sửa thời gian từng câu trong ô sửa thời gian ở bước 6."),
        ["settings-looser-than-ceo"] = ("Tệp cài đặt có một số nới lỏng quyết định của CEO.",
            "Trả số đó về giá trị CEO đã duyệt hoặc chặt hơn: mở màn hình cài đặt và bấm lưu để ghi lại các số của CEO."),
        ["ledger-truncated"] = ("Sổ theo dõi thí điểm bị xoá hoặc bị cắt bớt.", "Dừng. Không tự tạo sổ mới; báo người phụ trách để khôi phục sổ."),
        ["ledger-entry-missing"] = ("Sổ theo dõi thí điểm mất một mục.", "Dừng. Báo người phụ trách để khôi phục sổ."),
        ["package-file-not-plain"] = ("Tệp kê của gói trỏ ra ngoài thư mục gói.", "Gói đã bị sửa. Không duyệt, không đăng; tạo video lại ở bước 6."),
        ["package-ledger-mismatch"] = ("Gói và sổ theo dõi không khớp.",
            "Gói bị giữ, bị sửa hoặc bị chép ra chỗ khác. Không duyệt, không đăng; duyệt đúng gói vừa tạo ở bước 6."),
        ["approval-hash-mismatch"] = ("Mã SHA-256 của video khác giá trị được duyệt.",
            "Video đã đổi sau khi tạo. Không duyệt; tạo video lại ở bước 6 và duyệt video mới."),
        ["approval-void"] = ("Video đã đổi sau khi duyệt; lần duyệt cũ mất hiệu lực.", "Không đăng. Tạo video lại ở bước 6, xem lại và duyệt video mới."),
    };

    public const string GeneralNext = "Đọc phần tiếng Việt trong dòng trên: nó nêu lý do và việc cần làm. Sửa theo đó rồi bấm lại.";
    public const string TechnicalNext = "Báo người phụ trách kỹ thuật, kèm nguyên dòng trên.";

    /// <summary>The view of a toolchain run: its printed text unchanged, categorised by its exit status.</summary>
    public static OutcomeView FromToolchain(ToolchainRun run)
    {
        ArgumentNullException.ThrowIfNull(run);
        var line = run.Output;
        var code = CodeOf(line);
        return run.ExitCode switch
        {
            PilotExit.Done => new OutcomeView(Done, run.ExitCode, code, line, "Bộ công cụ đã kiểm và đồng ý.", string.Empty),
            PilotExit.Refused when code == "package-held" => new OutcomeView(Held, run.ExitCode, code, line,
                "Gói đã tạo nhưng có kiểm tra không đạt, nên bị giữ lại; không duyệt được.",
                "Đọc các dòng \"KHÔNG ĐẠT\" ở trên, sửa đầu vào rồi tạo video lại."),
            PilotExit.Refused => Refusal(run.ExitCode, code, line),
            PilotExit.ToolFailed => new OutcomeView(ToolFailure, run.ExitCode, code.Length == 0 ? "tool-failed" : code, line,
                "Công cụ ffmpeg hoặc ffprobe không chạy được.", TechnicalNext),
            PilotExit.Usage => new OutcomeView(AppFault, run.ExitCode, "app-fault", line,
                "Ứng dụng gọi bộ công cụ sai cách. Đây là lỗi của ứng dụng, không phải của bạn.", TechnicalNext),
            _ => new OutcomeView(ToolFailure, run.ExitCode, code.Length == 0 ? "tool-failed" : code, line,
                "Bộ công cụ dừng bất thường hoặc chạy quá thời gian cho phép.", TechnicalNext),
        };
    }

    /// <summary>A refusal raised in process by the toolchain library, shown in the toolchain's own line form.</summary>
    public static OutcomeView FromRefusal(PilotRefusal refusal)
    {
        ArgumentNullException.ThrowIfNull(refusal);
        return Refusal(null, refusal.Code, $"TỪ CHỐI [{refusal.Code}]: {refusal.Message}\n");
    }

    /// <summary>A step pre-check of the app: its message already carries the reason and the next step.</summary>
    public static OutcomeView FromWeb(WebRefusal refusal)
    {
        ArgumentNullException.ThrowIfNull(refusal);
        var line = $"TỪ CHỐI [{refusal.Code}]: {refusal.Message}\n";
        return refusal.Status >= 500
            ? new OutcomeView(AppFault, null, refusal.Code, line, string.Empty, TechnicalNext)
            : Guide.ContainsKey(refusal.Code) ? Refusal(null, refusal.Code, line) : new OutcomeView(Refused, null, refusal.Code, line, string.Empty, string.Empty);
    }

    public static OutcomeView FromToolFailure(PilotToolFailure failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return new OutcomeView(ToolFailure, null, "tool-failed", $"LỖI CÔNG CỤ [tool-failed]: {failure.Message}\n",
            "Công cụ ffmpeg hoặc ffprobe không chạy được.", TechnicalNext);
    }

    private static OutcomeView Refusal(int? exit, string code, string line) =>
        Guide.TryGetValue(code, out var guide)
            ? new OutcomeView(Refused, exit, code, line, guide.Reason, guide.Next)
            : new OutcomeView(Refused, exit, code, line, "Bộ công cụ từ chối; lý do ở dòng trên.", GeneralNext);

    /// <summary>The code in square brackets on the first printed line, or empty.</summary>
    public static string CodeOf(string output)
    {
        ArgumentNullException.ThrowIfNull(output);
        var first = output.Split('\n', 2)[0];
        var match = CodePattern().Match(first);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    [GeneratedRegex(@"\[([a-z0-9-]+)\]", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex CodePattern();
}
