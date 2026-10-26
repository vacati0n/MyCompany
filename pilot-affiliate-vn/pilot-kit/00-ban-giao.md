```yaml
releaseNote:
  releaseId: "MC15-PILOT-KIT-2026-10-26"
  version: "2026.10.26"
  sourceInputs:
    - type: final-change-summary
      reference: "research/ceo-decision-record.md, entries CEO-D-1301, CEO-D-1302 and CEO-D-1303"
    - type: technical-recommendation
      reference: "pilot-affiliate-vn/mc-15-addendum-1-ai-presenter.md (TR-MC15-002-A1: rules R1 to R12, conditions P1 to P8, 30-day plan, Q-013 to Q-019, OD12 to OD18)"
    - type: technical-recommendation
      reference: ".claude/runs/run-8d7e366341bf/states/recommendation-draft/artifacts/technical-recommendation.md (TR-MC15-002: Q-001 to Q-012)"
    - type: verification-report
      reference: "pilot-affiliate-vn/evidence/recapture-2026-10-26.md, shopee-vn-and-market.md, tiktok-vn-and-law.md, ai-presenter-pricing-2026-10-26.md"
  producedBy: documentation
  agentVersion: 1.2.0
  schemaVersion: 1.0.0
  status: provisional
  releaseVerdict: handed-off
  inputDigest: "sha256:d076b0d5abe1670219ec079809a3a13e"
  contextDigest: "sha256:bc0c81226eaa0cef542a08d691ce6944"
```

## Metadata

- Version: 2026.10.26
- Release date and time: 2026-10-26. Đây là ngày bàn giao bộ tài liệu thí điểm, không phải ngày phát hành phần mềm.
- Environment: Không có môi trường nào. Không tài khoản nào được mở, không video nào được đăng, không gì được mua hay tải xuống. Lệnh `/document` (luồng `implement-feature`, pha `documentation-and-release-handoff`); `self_hosting.py classify` xác nhận các đường dẫn nằm ngoài framework. Không có run nào của runtime điều phối bộ tài liệu này và không có envelope; hai digest ở trên là SHA-256 (32 ký tự đầu) của bảy tệp 01 đến 07 nối nhau, và của phụ lục 1 nối với bản ghi quyết định.
- Release owner: Không có phát hành. CEO là người dùng bộ tài liệu và là người duyệt mọi bài đăng (`D-002`).

## Highlights

- Feature additions: Bộ tài liệu thí điểm Shopee Video, tiếng Việt, trong `pilot-affiliate-vn/pilot-kit/`:
  - `01-cau-hoi-luat-su.md`: câu hỏi cho luật sư; Q-002 và Q-016 (Điều 15a(3)) trước, rồi thuế (Q-003), nhãn AI (Q-005 và câu hỏi thêm cho người dẫn AI), hình thức công bố (Q-012), điều khoản đối thủ (Q-001), hình ảnh người bán (Q-006, Q-019).
  - `02-cau-hoi-shopee.md`: thư gửi Shopee sẵn để sao chép; Q-013, Q-014, Q-015, Q-004 (ghi nhận hoa hồng qua Shopee Video cho KOC mới), câu 5 về thời hạn đặt và hoàn tất đơn (bổ trợ cho Q-017, không có mã), Q-018.
  - `03-mau-kich-ban-nguoi-dan-ai.md`: mẫu kịch bản; chỉ dùng thông tin có nguồn; công bố ở đầu và cuối; không bao giờ nói đã dùng; không tuyên bố về sức khỏe, da, tóc, cơ thể.
  - `04-danh-sach-duyet-dang-bai.md`: danh sách duyệt từng bài (`D-002`), gồm nhãn "Video tạo bởi AI".
  - `05-bang-do-30-ngay.md`: bảng đo; đang chờ và đã ghi có ghi riêng (OD17); đọc lại Ngày 45.
  - `06-lich-hanh-dong-ceo.md`: lịch từng ngày và điều kiện dừng.
  - `07-kich-ban-thu-va-mo-ta-nguoi-dan.md`: kịch bản thử khớp môi T1/T2 và mô tả người dẫn duy nhất.
- Bug fixes: None identified.
- Improvements: Ghi CEO-D-1303 vào `research/ceo-decision-record.md`: quyết định của CEO thay điều kiện P3; bộ tài liệu bỏ bước xin phép người bán.

## Technical Changes and Compatibility

- API or contract changes: None identified.
- Database or migration impact: None identified.
- Configuration changes: None identified.
- Backward compatibility notes: Phụ lục 1 không bị sửa. Theo CEO-D-1303, P3, phần xin phép người bán của R2 và R7, lệnh cấm lấy hình và thông tin từ trang sản phẩm của R7, nguồn "bảng thông tin của người bán" của R8 (hai điểm sau là suy luận từ quyết định, CEO xác nhận hoặc sửa), và bước Ngày 5 đến 9 trong lịch của phụ lục không còn áp dụng; mục 3.3 và 3.5 của phụ lục được thay bằng `04-` và `06-`.

## Operational Notes

- Deployment considerations: Agent không tạo tài khoản, không đăng nhập, không liên hệ người bán, không mua, không tải tài sản, không đăng gì (`D-002`, `D-018`). CEO gửi các bộ câu hỏi và làm mọi bước trong ứng dụng.
- Monitoring and alerts: Bảng đo `05-`; kiểm tra email 2 ngày một lần để từ chối KOL Booking trong 3 ngày; phản hồi kết quả hoa hồng trong 24 giờ.
- Rollback criteria: Điều kiện dừng trong `06-` (nhánh A của luật sư; trả lời bất lợi của Shopee; Shopee giảm hiển thị hoặc xóa video vì lý do AI lần thứ hai).

## Validation Summary

- Test status: Không có mã để kiểm thử. Mọi con số trong bộ tài liệu được đối chiếu với phụ lục 1 và các tệp bằng chứng; số không có nguồn ghi ESTIMATE hoặc UNVERIFIED.
- Known risk acceptance: CEO chấp nhận rủi ro hình ảnh người bán (CEO-D-1303, `D-018`). Rủi ro đăng trước khi có trả lời được quyết ở Ngày 10 theo OD16.
- Post-release checks: Ngày 10, phần A của danh sách duyệt; Ngày 30 và Ngày 45, Phần D của bảng đo.

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | Không có trang chính thức nào cho phép người tiếp thị liên kết dùng ảnh của người bán; E-105 cấm sao chép hình ảnh của Shopee và cấm thu thập thông tin trên Sàn; E-107(n) cấm dùng nội dung người khác khi không được phép, phạt tới VND 10 triệu mỗi hành vi. | CEO, với tư cách cá nhân: video có thể bị gỡ, bị phạt hoặc mất quyền Shopee Video. | Quyết định CEO-D-1303; hỏi Shopee (Q-014) và luật sư (Q-006, Q-019); trả lời bất lợi là sự kiện dừng. | RK-010, Q-014 |
| `K-002` | Câu công bố quảng cáo chưa được luật sư xác nhận. | Mọi video. | Dùng câu của phụ lục 1; sửa cả ba lớp khi có ý kiến luật sư. | Q-012 |
| `K-003` | E-107 và E-109 chưa được chụp lại; câu "hoàn tất trong 30 ngày" chưa được chụp lại. | Điều kiện P5; thời điểm đọc bậc 2. | Context-agent chụp lại Ngày 1 đến 3. | Q-017, P5 |
| `K-004` | Tên các chỉ số trong ứng dụng Shopee Affiliate, vị trí nút nhãn AI và kênh hỗ trợ nhận câu hỏi bằng văn bản đều UNVERIFIED. | Bảng đo và thư gửi Shopee. | CEO ghi lại ở lần đăng nhập và lần đăng đầu. | Q-018 |
| `K-005` | Thời gian CEO mỗi video (ESTIMATE 40 phút sau khi bỏ bước xin phép) chưa đo. | Kế hoạch thời gian. | Cột "Phút CEO bỏ ra" của bảng đo. | Bảng đo, Phần B |

## Communication

- Stakeholders notified: CEO, qua tin nhắn bàn giao của phiên này và đường dẫn so sánh nhánh trên GitHub.
- Support handoff notes: Việc tiếp theo không phải của CEO: context-agent chụp lại bài 199468, 164064, 122941 và đoạn "30 ngày" của bài 122944 (Q-017, P5), trước Ngày 3.
