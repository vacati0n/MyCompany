```yaml
releaseNote:
  releaseId: "MC15-FINDINGS-2026-10-26"
  version: "2026.10.26"
  sourceInputs:
    - type: technical-recommendation
      reference: "runs/run-8d7e366341bf/states/recommendation-draft/artifacts/technical-recommendation.md (TR-MC15-002, the governing recommendation)"
    - type: technical-recommendation
      reference: "runs/run-8d7e366341bf/states/option-synthesis/artifacts/technical-recommendation.md (TR-MC15-001, superseded draft, read as context only)"
    - type: final-change-summary
      reference: "research/ceo-decision-record.md, entries CEO-D-1300 and CEO-D-1301 (owner decision amending the recommendation; outside the frozen context slice)"
    - type: final-change-summary
      reference: "runs/run-8d7e366341bf/state.json, Recommendation Gate decision record (decided 2026-10-26T12:33:21Z; carries the same amendment)"
    - type: verification-report
      reference: "pilot-affiliate-vn/evidence/recapture-2026-10-26.md (first-party page re-capture of 2026-10-26; outside the frozen context slice)"
  producedBy: documentation
  agentVersion: 1.2.0
  schemaVersion: 1.0.0
  status: provisional
  releaseVerdict: findings-published
  inputDigest: "sha256:1437f5e6a4cbb60857d7b91539fdedcd"
  contextDigest: "sha256:c84fb2a1bb501b514bb1ae4aa1006dae"
```

## Metadata

- Version: 2026.10.26
- Release date and time: 2026-10-26. Quyết định của Recommendation Gate được ghi lúc 2026-10-26T12:33:21Z (bản ghi cổng trong state.json của run). Đây là ngày công bố kết luận nghiên cứu, không phải ngày phát hành phần mềm.
- Environment: Không có môi trường nào được triển khai. Đây là bản công bố kết luận nghiên cứu MC-15 (run run-8d7e366341bf). Chưa có tài khoản nào được mở, chưa có video nào được đăng, chưa có gì được mua hay tải xuống.
- Release owner: Không có phát hành. Khuyến nghị do tech-lead lập (TR-MC15-002). Recommendation Gate do orchestrator quyết định theo phê duyệt của CEO. Quyết định về thí điểm thuộc về CEO (CEO-D-1301).

## Highlights

- Feature additions: Kết luận đã được duyệt, theo CEO-D-1301 và bản ghi Recommendation Gate. Đây là một quyết định, chưa phải một năng lực đã xây.
  - Con đường: Shopee Affiliate, đăng ký cá nhân làm KOC, đăng trên Shopee Video (phương án O-003 của TR-MC15-002).
  - Ngách: đồ gia dụng và nhà bếp trước (O-008), phụ kiện điện thoại sau (O-009). Mỹ phẩm và thực phẩm chức năng bị loại.
  - Dự phòng: chỉ TikTok Shop (O-002), và chỉ bắt đầu sau khi hoạt động trên Shopee kết thúc hoặc sau khi có văn bản đọc hẹp điều khoản cấm quảng bá sàn đối thủ của Shopee. Chạy cả hai cùng lúc bị loại.
  - Mục tiêu 30 ngày: bậc 2, tức có ít nhất một hoa hồng hợp lệ được ghi có trước ngày 30, kèm bộ số đo của thí điểm.
  - Bắt đầu ngay. Các câu hỏi cho luật sư (phạm vi điều khoản đối thủ, Điều 15a(3) Luật Quảng cáo, thuế TNCN, phạm vi nhãn AI, dùng lại hình ảnh của người bán) được gửi song song.
  - Hình thức nội dung, do CEO sửa so với khuyến nghị: không mua mẫu trong 30 ngày đầu; dùng hình ảnh sản phẩm thật, chỉ khi người bán cho phép; có một người dẫn AI mang nhãn "Video tạo bởi AI", chỉ đọc thông tin sản phẩm đã ghi nhận và có nguồn, và không bao giờ nói đã dùng sản phẩm (đây là giới thiệu, không phải đánh giá); một người dẫn trên một tài khoản; không có bác sĩ, người nổi tiếng hay quan chức do AI tạo; không có tuyên bố về da, tóc hay cơ thể.
  - Điều kiện của hình thức này: CEO định giá và duyệt công cụ người dẫn AI trước lần dùng đầu. Trần chi phí theo lượng dùng USD 34.42 giữ nguyên, trừ khi CEO quyết khác. Nếu luật sư kết luận người làm tiếp thị liên kết là người có ảnh hưởng theo Điều 15a(3), hình thức người dẫn AI dừng cho tới khi sản phẩm đã được dùng.
- Bug fixes: Các điểm TR-MC15-002 và bản chụp lại ngày 2026-10-26 sửa so với bản tổng hợp phương án TR-MC15-001:
  - Không tìm thấy trên trang Shopee con số "khấu trừ 10% thuế TNCN từ VND 250,000" (E-100). Hệ số thực nhận 0.89118 dựa trên con số đó đã được thay bằng khoảng 0.8902 đến 0.9902, ứng với các mức thuế 0, 2, 5 hoặc 10%.
  - Quy định đặt lịch video (E-089) đã đổi: email "KOL Booking" không được trả lời trong 3 ngày thì coi như đã đồng ý, và bài đã đặt lịch không gỡ được nếu Shopee không yêu cầu bằng văn bản.
  - Shopee có năm loại đối tác chứ không phải bốn (E-076).
- Improvements:
  - Bản chụp lại ngày 2026-10-26 trích nguyên văn năm trang chính thức: 13 mục được xác nhận, 2 mục thay đổi (E-076, E-089) và 1 mục không tìm thấy (E-100). TR-MC15-002 ghi rủi ro "quy tắc chưa có trích dẫn" (RK-005) là đã giải quyết.
  - Nhờ điều khoản E-110 đã được trích nguyên văn, TR-MC15-002 nâng điểm O-003 từ 11 lên 12 trên 12 tiêu chí: theo điều khoản, dừng mà không vi phạm thì không có khoản chi phí nào.
  - Giới hạn còn lại: bảy mục của Shopee chưa được chụp lại (E-086, E-088, E-097, E-098, E-103, E-107, E-109) và vẫn dựa trên ghi chép của người thu thập.

## Technical Changes and Compatibility

- API or contract changes: Theo CEO-D-1301, các quyết định thường trực và kế hoạch đã khuyến nghị thay đổi như sau:
  - D-005, D-007 và D-008 được sửa đúng như khuyến nghị đề xuất: tiếp thị liên kết là dòng doanh thu chính; thêm Shopee Affiliate VN với CEO là người nhận tiền; thêm một kênh thí điểm Shopee Video bằng tiếng Việt.
  - Phần 2 của D-021 (luôn công bố quảng cáo) giữ nguyên; phần 1 được gửi luật sư. D-022 chỉ còn giữ cho mục YouTube đang đóng băng.
  - CEO-D-903 và CEO-D-1004 (không dùng mô hình tạo sinh cho hình ảnh) bị thay thế, chỉ trong thí điểm tiếp thị liên kết.
  - Hình thức nội dung trong kế hoạch 30 ngày của TR-MC15-002 (quay tay thật với mẫu đã mua, giọng của CEO) được thay bằng người dẫn AI trên hình ảnh sản phẩm có phép của người bán.
  - Không đổi: D-002 (CEO duyệt từng bài đăng) và D-018 (CEO là nhà sáng tạo cá nhân). Agent không tạo tài khoản, không giữ thông tin đăng nhập, không liên hệ người bán và không đăng gì.
- Database or migration impact: None identified.
- Configuration changes:
  - Đăng ký với tư cách cá nhân. Không đăng ký kênh TikTok nào với Shopee. Không đặt liên kết của người bán trên sàn đối thủ ở bất kỳ kênh nào (biện pháp cho RK-001 trong TR-MC15-002).
  - Bật nhãn "Video tạo bởi AI" trên mọi video có người dẫn AI (CEO-D-1301; quy tắc E-108 đã được xác nhận).
  - Từ chối các chiến dịch "KOL Booking" của Shopee trong suốt thời gian thí điểm.
  - Ngân sách: trần chi phí theo lượng dùng USD 34.42 giữ nguyên, trừ khi CEO quyết khác. Công cụ người dẫn AI chưa có giá và phải được CEO duyệt trước lần dùng đầu. Theo bản ghi Recommendation Gate, dòng ngân sách mẫu tối đa USD 42 mỗi tháng (OD6) không được dùng.
- Backward compatibility notes: Ai đang dựa vào điều gì trước đây, và họ cần làm gì:
  - Wave 12 và Wave 13 (đang đóng băng) vẫn giữ CEO-D-903 và CEO-D-1004. Quyết định thay thế chỉ áp dụng cho thí điểm.
  - Bị bỏ trong kế hoạch 30 ngày của TR-MC15-002: việc mua và dùng mẫu ở ngày 4 đến 7. CEO-D-1301 không nhắc tới phần còn lại của bước này, tức chọn tối đa 6 sản phẩm ít rủi ro về tuyên bố và không nằm trong danh sách không có hoa hồng (E-103).
  - Bị thay trong kế hoạch: ngày 8 đến 28 dùng người dẫn AI có nhãn thay cho video quay tay với giọng CEO. Video vẫn công bố quảng cáo trong video và trong chú thích, và CEO vẫn duyệt từng video trước khi đăng.
  - Bị bỏ trong danh sách hành động của CEO: hành động 6 (mua mẫu bằng tiền riêng), 7 (dùng từng sản phẩm) và 8 (quay tay với giọng tiếng Việt của mình).
  - Không còn áp dụng: ràng buộc thứ tự "kiểm tra ghi nhận hoa hồng trước khi mua mẫu; dùng mẫu trước khi quay"; số đo "chi phí mẫu"; giả định chi phí của ước tính hòa vốn (6 mẫu, USD 42, chi phí theo lượng dùng USD 0); bước "giữ lại mẫu" trong kế hoạch đảo ngược.
  - Bước mới mà chưa tài liệu nào xếp ngày: xin phép người bán cho từng hình ảnh (CEO tự làm) và định giá, duyệt công cụ người dẫn AI.
  - Không đổi: trước ngày 1, CEO xác nhận điều kiện tham gia. Từ ngày 1 đến 3, CEO đăng ký, nhập thông tin thanh toán, đọc bảng tỷ lệ và kiểm tra việc ghi nhận hoa hồng trên Shopee Video. Ngày 29 và 30 đọc kết quả theo bậc 2. Kiểm tra email ít nhất 2 ngày một lần.
  - Việc phải làm: chỗ nào CEO-D-1301 và TR-MC15-002 khác nhau thì làm theo CEO-D-1301. Trình tự thay thế cho các bước bị bỏ do tech-lead lập (xem K-015).

## Operational Notes

- Deployment considerations: Không có gì được triển khai. Theo TR-MC15-002 và CEO-D-1301, đây là các việc CEO tự làm trước video đầu tiên:
  - Tự xác nhận, không chia sẻ với ai, rằng mình đủ 18 tuổi, có CCCD gắn chip, mã số thuế và tài khoản ngân hàng trong nước đứng tên mình (RK-004).
  - Tự tạo tài khoản Shopee Affiliate, đọc và chấp nhận điều khoản, nhập thông tin thanh toán, và không đăng ký kênh TikTok nào.
  - Đọc và ghi lại bảng tỷ lệ hoa hồng. Kiểm tra trong ứng dụng rằng thẻ sản phẩm trên Shopee Video ghi nhận hoa hồng cho tài khoản của mình (RK-007).
  - Xin phép người bán cho từng hình ảnh sản phẩm sẽ dùng.
  - Định giá và duyệt công cụ người dẫn AI.
  - Gửi các câu hỏi cho luật sư, song song và không chờ câu trả lời.
- Monitoring and alerts:
  - Số đo cho mỗi video (TR-MC15-002, đã bỏ "chi phí mẫu"): lượt xem, lượt nhấp sản phẩm, tỷ lệ nhấp, số đơn, tỷ lệ chuyển đổi, giá trị đơn trung bình, hoa hồng mỗi đơn, đơn bị hủy và bị trả, số phút CEO bỏ ra, nhãn, cảnh báo hoặc gỡ bài.
  - Số đo cho mỗi kỳ: tỷ lệ hoa hồng của từng sản phẩm trên bảng điều khiển, các khoản chi trả, số thuế bị khấu trừ, các khiếu nại trong khung 24 giờ (E-102), các email đặt lịch.
  - Tín hiệu đạt: có ít nhất một hoa hồng hợp lệ được ghi có trước ngày 30 (bậc 2, CEO-D-1301).
  - Cảnh báo cần xử lý ngay: thông báo xóa video hoặc thiếu nhãn AI (E-108); email "KOL Booking" phải được từ chối trong 3 ngày (E-089); kết quả hoa hồng phải được phản hồi trong 24 giờ (E-102).
- Rollback criteria:
  - Hình thức người dẫn AI dừng nếu luật sư kết luận người làm tiếp thị liên kết là người có ảnh hưởng theo Điều 15a(3), cho tới khi sản phẩm đã được dùng (CEO-D-1301 mục 6).
  - Nếu bài trên Shopee Video không ghi nhận được hoa hồng (RK-007), chuyển sang dự phòng TikTok Shop (O-002). Việc chuyển chỉ diễn ra sau khi hoạt động trên Shopee kết thúc hoặc khi đã có văn bản đọc hẹp E-104.
  - Nếu CEO không đủ điều kiện tham gia, hoãn thí điểm (O-004), theo trình tự trong TR-MC15-002.
  - Cách dừng (theo TR-MC15-002): ngừng đăng bài. Dừng mà không vi phạm thì không có khoản chi phí nào được nêu, và Shopee có thể chấm dứt tài khoản không có kết quả trong 6 tháng (E-110). Giữ đủ thông tin thanh toán để số dư trên VND 10,000 được trả (E-087). Không nhận đặt lịch, vì bài đã đặt lịch không gỡ được (E-089). Bước "giữ lại mẫu" không còn áp dụng.

## Validation Summary

- Test status: Theo task-context của run: requirement framing của business-analyst đạt 36/36; báo cáo điều tra của context-agent đạt 31/31 sau 2 lần; mỗi bản khuyến nghị của tech-lead đạt 33/33. Đây là kết quả kiểm tra cấu trúc của Validation Engine, không chứng minh các kết luận là đúng. TR-MC15-002 tự khai trạng thái provisional và khuyến nghị proceed-with-conditions. Về các cổng: Framing Gate do CEO duyệt; Technical Validity Gate do architect duyệt, kèm các điều kiện C1 đến C12 theo TR-MC15-002; Recommendation Gate do orchestrator duyệt ngày 2026-10-26 theo phê duyệt của CEO, có kèm sửa đổi. Chưa giai đoạn nào đánh giá hình thức người dẫn AI. Khi viết bản tin này, Validation Engine chưa kiểm tra nó, vì agent này không được chạy lệnh.
- Known risk acceptance: Theo CEO-D-1301, CEO (chủ sở hữu) chấp nhận: bắt đầu trước khi luật sư trả lời (mục 3); dùng hình thức người dẫn AI kèm điều kiện dừng (mục 4 và 6); thay thế CEO-D-903 và CEO-D-1004 trong thí điểm (mục 5). Orchestrator duyệt Recommendation Gate trên khuyến nghị proceed-with-conditions. Không có vai trò nào được ghi nhận là đã đánh giá lại các rủi ro được mở lại ở K-001, K-005 và K-006.
- Post-release checks:
  - Đơn hàng đầu tiên xác nhận hoa hồng được ghi nhận qua Shopee Video (RK-007).
  - Mỗi video có nhãn "Video tạo bởi AI", có công bố quảng cáo trong video và trong chú thích, đã được CEO duyệt, và không nhận thông báo xóa.
  - Có ghi lại sự cho phép của người bán cho mỗi hình ảnh.
  - CEO đã duyệt giá công cụ người dẫn AI trước video đầu tiên.
  - Ngày 30: đọc kết quả theo bậc 2.

## Known Issues

| ID | Issue | Impact | Workaround | Tracking |
|---|---|---|---|---|
| `K-001` | Nghĩa vụ của người có ảnh hưởng theo Điều 15a(3) (RK-002): CEO-D-1301 bỏ biện pháp của TR-MC15-002 (mua, dùng, rồi thông báo trước và trong video). Lý do của OD9, rằng kế hoạch tuân thủ dù luật sư trả lời thế nào, không còn đúng với câu hỏi này | CEO cá nhân: nếu luật sư kết luận điều này áp dụng, các video đăng trước câu trả lời đã được làm khi chưa dùng sản phẩm. CEO-D-1301 không nói gì về các video đó | Điều kiện dừng ở CEO-D-1301 mục 6; gửi câu hỏi cho luật sư sớm. Không có cách khắc phục cho video đã đăng | RK-002 và Q-002 (luật sư). Chưa có nơi theo dõi việc xử lý video đã đăng; câu hỏi mở cho CEO và luật sư |
| `K-002` | Phân loại, mức thuế TNCN và bên khấu trừ chưa rõ (RK-003). Trang Shopee chỉ nói khấu trừ theo luật, không nêu mức | CEO: số tiền thực nhận không chắc chắn. Nếu đây là thu nhập kinh doanh thì mẫu đăng ký và việc quyết toán hằng năm sẽ khác | Đăng ký với tư cách cá nhân (CEO-D-1301). Ước tính thực nhận bằng 0.8902 đến 0.9902 hoa hồng. Luật sư trả lời trước kỳ quyết toán đầu tiên | RK-003 và Q-003 (luật sư) |
| `K-003` | Chưa nguồn nào xác nhận CEO đủ điều kiện tham gia (RK-004) | CEO: thiếu điều kiện thì không mở được tài khoản và không được trả tiền. Nếu thiếu thông tin thanh toán 6 tháng, Shopee có thể từ chối trả và coi như CEO đồng ý chấm dứt (E-087) | Không có cách thay thế. CEO tự xác nhận riêng trước khi đăng ký; agent không xem các số này | RK-004 (CEO) |
| `K-004` | Không có tỷ lệ hoa hồng chính thức nào trên cả hai nền tảng. Lịch chi trả E-088 chưa được chụp lại (RK-008) | CEO: không dự báo được doanh thu; mọi con số doanh thu chỉ là ƯỚC TÍNH | Đọc bảng tỷ lệ sau khi đăng ký và ghi lại | RK-008 và Q-007 (CEO) |
| `K-005` | Nhãn AI (RK-009): CEO-D-1301 thay biện pháp "không nội dung tạo sinh, không giọng tổng hợp trong 30 ngày". Trang AI của Shopee (E-108, đã xác nhận) xóa video AI không nhãn đăng từ 23/09/2026. Trang này cũng xếp nội dung lặp lại, dùng cùng một avatar AI trên nhiều tài khoản, và nội dung thiếu nguyên bản vào loại nội dung AI kém chất lượng | CEO: video có thể bị giảm hiển thị hoặc bị xóa, và CEO có thể mất quyền dùng Shopee Video, kênh duy nhất của thí điểm | Bật nhãn trên mọi video; một người dẫn trên một tài khoản. Nhãn tự động "Video có thể chứa nội dung do AI tạo ra" không thay được việc tự gắn nhãn | RK-009 (tech-lead đánh giá lại) và Q-005 (luật sư) |
| `K-006` | Dùng hình ảnh của người bán (RK-010): biện pháp "chỉ dùng cảnh quay của CEO, không dùng media của người bán" được thay bằng "dùng khi người bán cho phép". Câu hỏi Q-006 cho luật sư vẫn mở | CEO: có thể bị gắn cờ nội dung đánh cắp và bị phạt tới VND 10 triệu (E-107, chưa chụp lại). Shopee kiểm tra nội dung sao chép khi duyệt và kiểm tra định kỳ (E-084) | Chỉ dùng hình đã có ghi lại sự cho phép. CEO tự xin phép, vì agent không liên hệ người bán | RK-010 (tech-lead đánh giá lại) và Q-006 (luật sư) |
| `K-007` | Dự phòng TikTok Shop cần 1,000 người theo dõi mới có hoa hồng (RK-011, nguồn có độ tin cậy thấp) | CEO: nếu phải chuyển sang dự phòng, sẽ không có hoa hồng trong ngắn hạn | Coi dự phòng là giai đoạn xây lượng người theo dõi, không phải nguồn thu trong 30 ngày | RK-011 |
| `K-008` | Giới hạn đăng bài và điểm sức khỏe nhà sáng tạo trên TikTok (RK-012). Biện pháp ghi trong TR-MC15-002 dựa trên cảnh quay có tương tác, không dựa trên người dẫn AI | CEO: trên kênh dự phòng có thể bị giảm hiển thị hoặc mất tài khoản. CEO-D-1301 không nói kênh dự phòng dùng hình thức nào | Chưa có | RK-012. Chưa có nơi theo dõi hình thức cho kênh dự phòng; câu hỏi mở cho tech-lead |
| `K-009` | Điều khoản đặt lịch và chấm dứt của Shopee (RK-013): im lặng 3 ngày trước email đặt lịch là đồng ý; bài đã đặt lịch không gỡ được; vi phạm bị đòi 30% số tiền đã trả | CEO: nếu bỏ lỡ email, CEO bị ràng buộc vào một dự án và có bài không gỡ được | Kiểm tra email ít nhất 2 ngày một lần; từ chối mọi KOL Booking (CEO-D-1301) | RK-013 |
| `K-010` | Chi phí: không còn dùng dòng ngân sách mẫu (RK-014), nhưng công cụ người dẫn AI chưa có giá. Ước tính hòa vốn của TR-MC15-002 (6 mẫu, USD 42, chi phí theo lượng dùng USD 0) không còn phản ánh chi phí thí điểm | CEO: chưa đăng được video AI đầu tiên khi giá chưa được duyệt, và chưa biết cần bao nhiêu đơn để hòa vốn | Chưa có cho tới khi có giá. Mục tiêu bậc 2 là có một hoa hồng hợp lệ, không phụ thuộc vào hòa vốn | RK-014 và Q-008 (đã bị thay). Chưa có nơi theo dõi việc tính lại hòa vốn; câu hỏi mở cho tech-lead |
| `K-011` | Chưa kiểm tra quy trình dựng hình hiện có với video dọc và chữ tiếng Việt (RK-016) | Lớp đồ họa thông tin bị hoãn, nằm ngoài 30 ngày | Không dùng lớp đồ họa trong 30 ngày | RK-016 và Q-009 (architect) |
| `K-012` | Q-010: bản chụp lại năm trang có thỏa điều kiện chụp lại C12 hay không, khi bảy mục của Shopee chưa được chụp lại | CEO: bảy quy tắc đó vẫn dựa trên ghi chép cũ, trong đó có mức phạt nội dung đánh cắp | Bản ghi Recommendation Gate ghi RK-005 đã giải quyết và bảy mục được đánh dấu ở chỗ dùng, nhưng không trả lời Q-010 bằng lời | Q-010 (orchestrator) |
| `K-013` | Hình thức công bố quảng cáo và mức phạt theo luật quảng cáo sửa đổi cho bài trên Shopee Video (Q-012). Phần 1 của D-021 đã được gửi luật sư | CEO: cách công bố hiện tại có thể chưa đúng mẫu luật yêu cầu | Luôn công bố trong video và trong chú thích (phần 2 của D-021) | Q-012 (luật sư) |
| `K-014` | Hình thức người dẫn AI không nằm trong ba hình thức TR-MC15-002 đã chấm điểm (O-005, O-006, O-007). TR-MC15-002 loại hình thức duy nhất không có cảnh quay thật (O-006) vì trượt EC-001, EC-004, EC-006 và EC-008 | CEO: đang làm theo một hình thức chưa được chấm trên 12 tiêu chí, kể cả các tiêu chí bắt buộc về tuân thủ và về tiếng Việt hiển thị đúng | Áp dụng các quy tắc của CEO-D-1301: gắn nhãn, chỉ đọc thông tin có nguồn, không nói đã dùng sản phẩm, một người dẫn, có điều kiện dừng | Chưa có nơi theo dõi; câu hỏi mở cho tech-lead |
| `K-015` | Kế hoạch 30 ngày và danh sách hành động của CEO trong TR-MC15-002 có các bước bị CEO-D-1301 bỏ hoặc thay, nhưng chưa tài liệu nào xếp lại trình tự. Hai bước mới chưa có ngày: xin phép người bán và duyệt giá công cụ | CEO: chưa có lịch theo ngày cho các bước mới trong 30 ngày | Làm các bước không đổi. Xin phép người bán và duyệt giá trước video đầu tiên, như CEO-D-1301 yêu cầu | Chưa có nơi theo dõi; câu hỏi mở cho tech-lead (cho planner nếu cần chia việc) |
| `K-016` | TR-MC15-002 dẫn E-094 làm lý do chọn bậc 2, với câu "đơn hàng hoàn tất trong 30 ngày kể từ lượt truy cập". Bản chụp lại E-094 chỉ trích quy định cookie 7 ngày | CEO: lý do được nêu cho bậc 2 không có nguồn đã chụp lại. Bậc 2 vẫn là quyết định của CEO | Không dựa vào câu đó; dùng quy định cookie 7 ngày đã được xác nhận | Chưa có nơi theo dõi; câu hỏi mở cho tech-lead |
| `K-017` | Sửa đổi của CEO không đến giai đoạn này qua đầu vào chính thức. Envelope chỉ cung cấp hai bản khuyến nghị. CEO-D-1301 và bản ghi cổng trong state.json được đọc theo chỉ dẫn giao việc và nằm ngoài inputDigest | Người đọc sau: runtime không đối chiếu phần sửa đổi trong bản tin này bằng digest. Nếu bản ghi quyết định thay đổi, bản tin này không tự cập nhật | Đối chiếu với CEO-D-1301 và bản ghi Recommendation Gate | research/framework-defects.md, Finding 7 (orchestrator) |

## Communication

- Stakeholders notified: Người nhận chính là CEO (chủ sở hữu, đọc tiếng Việt). Đối tượng này do bản giao việc nêu; envelope không cung cấp stakeholder-list. Bản tin được giao cho orchestrator để chuyển đi; agent này không gửi cho ai. Câu hỏi mở được chuyển tới tech-lead (K-005, K-006, K-008, K-010, K-014, K-015, K-016), orchestrator (K-012, K-017), luật sư qua CEO (K-001, K-002, K-005, K-006, K-013) và architect (K-011).
- Support handoff notes:
  - Dễ hiểu lầm: thấy nhãn tự động "Video có thể chứa nội dung do AI tạo ra" không có nghĩa là video đã được gắn nhãn đúng, và video đó vẫn có thể bị xóa (E-108). Người tạo phải tự bật nhãn "Video tạo bởi AI".
  - Thời hạn cần nhớ: phản hồi kết quả hoa hồng trong 24 giờ, im lặng là chấp nhận (E-102); cookie kéo dài 7 ngày (E-094); mức chi trả tối thiểu là VND 10,000 (E-087).
  - Mọi con số doanh thu và hòa vốn chỉ là ƯỚC TÍNH, vì không có tỷ lệ hoa hồng chính thức.
  - Nên xem trước: CEO-D-1301 cho quyết định; TR-MC15-002 cho các phương án, tiêu chí và sổ rủi ro; pilot-affiliate-vn/evidence/recapture-2026-10-26.md cho các trích dẫn nguyên văn.
  - Phiên bản 2026.10.26 là ngày quyết định; không tài liệu nào cấp số phiên bản. Giai đoạn này không đọc hai tệp bằng chứng shopee-vn-and-market.md và tiktok-vn-and-law.md; các sự kiện trong đó đến bản tin này thông qua TR-MC15-002.
