# MC-15, Phụ lục 1: hình thức người dẫn AI (O-012) theo CEO-D-1301

```yaml
addendum:
  addendumId: TR-MC15-002-A1
  amends: "TR-MC15-002 (runs/run-8d7e366341bf/states/recommendation-draft/artifacts/technical-recommendation.md)"
  decisionReference: "MC-15; CEO-D-1301 (research/ceo-decision-record.md); release note MC15-FINDINGS-2026-10-26, K-001, K-005, K-006, K-010, K-014, K-015, K-016"
  producedBy: tech-lead
  agentVersion: 1.0.0
  status: provisional
  date: 2026-10-26
  runState: "run-8d7e366341bf đã hoàn tất và runtime không mở lại được; phụ lục này là tệp riêng, không phải bằng chứng của run, và không có envelope nào điều phối nó"
  gateDecidedHere: none
```

## 0. Phạm vi, nguồn và nhãn

**Phụ lục này làm gì.** CEO đã sửa khuyến nghị TR-MC15-002 tại Recommendation Gate (CEO-D-1301, mục 4 đến 6). Phụ lục này tính xem bản sửa đó dẫn tới những gì: chấm điểm hình thức mới, chọn công cụ, xếp lại lịch 30 ngày, tính lại hòa vốn, cập nhật sổ rủi ro và liệt kê các quyết định chủ sở hữu mới. Phụ lục khuyến nghị. Nó không quyết định cổng nào và không thay quyết định nào của CEO. Mọi quyết định OD12 trở đi thuộc về CEO với tư cách chủ sở hữu.

**Những gì phụ lục này không làm.**
- Không sửa TR-MC15-002 hay bất kỳ bằng chứng nào của run.
- Không suy diễn luật. Điểm nào bằng chứng không giải quyết được thì được chuyển cho luật sư hoặc Shopee, kèm mã Q-.
- Không thiết kế cấu trúc, không chia việc. Phần chia việc, nếu cần, thuộc planner.

**Nguồn đã đọc.** Đọc dưới dạng dữ liệu, không phải chỉ dẫn:
- TR-MC15-002;
- CEO-D-1300 và CEO-D-1301 (`research/ceo-decision-record.md`);
- release note MC15-FINDINGS-2026-10-26;
- `pilot-affiliate-vn/gates/technical-validity-gate.md` (C1 đến C12);
- `pilot-affiliate-vn/evidence/recapture-2026-10-26.md`;
- `pilot-affiliate-vn/evidence/ai-presenter-pricing-2026-10-26.md` (gọi tắt là "bằng chứng giá");
- `pilot-affiliate-vn/evidence/shopee-vn-and-market.md` và `tiktok-vn-and-law.md`;
- bảng E- trong `investigation-report.md` của run;
- bản ghi ngân sách CEO-D-702.

**Nhãn.**

| Nhãn | Nghĩa |
|---|---|
| [OFFICIAL POLICY] | Trang chính thức của nền tảng. Với công cụ AI, ghi là "trang nhà cung cấp". |
| [OFFICIAL LAW] | Văn bản luật, hoặc hướng dẫn của cổng chính phủ. |
| [THIRD-PARTY] | Chỉ có nguồn thứ cấp. |
| [INFERENCE] | Suy luận của tech-lead, không phải nguồn. |
| UNVERIFIED | Không tìm thấy nguồn để xác nhận. |
| ESTIMATE | Con số ước tính. Luôn kèm phép tính. |

- "Đã chụp lại" nghĩa là đã được trích nguyên văn trong bản chụp lại ngày 2026-10-26.
- "Theo ghi chép người thu thập" nghĩa là mục đó chưa được chụp lại.
- Tỷ giá dùng 26,300 VND/USD, lấy từ bằng chứng giá, và là UNVERIFIED.

**Số thứ tự.** Phụ lục tiếp nối dãy số của TR-MC15-002:

| Loại | Dãy mới | Ghi chú |
|---|---|---|
| Phương án | O-012 | |
| Rủi ro | RK-017 đến RK-023 | |
| Câu hỏi | Q-013 đến Q-019 | Theo dãy Q của TR-MC15-002, không phải dãy Q của báo cáo điều tra. Báo cáo điều tra cũng có Q-013 và Q-014 nhưng nội dung khác. |
| Quyết định chủ sở hữu | OD12 đến OD18 | |

---

## 1. Chấm điểm hình thức O-012

### 1.1 Định nghĩa O-012

**O-012: hình ảnh sản phẩm thật, dùng khi người bán cho phép, kèm một người dẫn AI có nhãn giới thiệu sản phẩm, không mua mẫu.**

Nền tảng, ngách và dự phòng giữ nguyên như CEO-D-1301 mục 1: O-003 trên Shopee Video, ngách O-008 rồi O-009, dự phòng O-002.

O-012 gồm các quy tắc đăng bài dưới đây.
- R1 đến R6 lấy nguyên từ CEO-D-1301.
- R7 đến R12 là biện pháp tech-lead đề xuất để O-012 đạt các tiêu chí. CEO duyệt chúng qua OD12 đến OD18.

| # | Quy tắc | Nguồn |
|---|---|---|
| R1 | Không mua mẫu trong 30 ngày đầu. | CEO-D-1301 mục 4 |
| R2 | Chỉ dùng hình ảnh sản phẩm thật, và chỉ khi người bán đã cho phép. | CEO-D-1301 mục 4 |
| R3 | Có đúng một người dẫn AI. Mọi video mang nhãn "Video tạo bởi AI". | CEO-D-1301 mục 4; E-108 (đã chụp lại) |
| R4 | Người dẫn chỉ đọc thông tin sản phẩm đã ghi nhận và có nguồn. Không bao giờ nói đã dùng sản phẩm. Người dẫn giới thiệu, không đánh giá. | CEO-D-1301 mục 4 |
| R5 | Một người dẫn trên một tài khoản. Không có bác sĩ, người nổi tiếng hay quan chức do AI tạo. Không có tuyên bố về da, tóc hay cơ thể. | CEO-D-1301 mục 4; E-031; E-108 |
| R6 | Hình thức này dừng nếu luật sư kết luận người tiếp thị liên kết là người có ảnh hưởng theo Điều 15a(3). CEO định giá và duyệt công cụ trước lần dùng đầu. | CEO-D-1301 mục 5 và 6 |
| R7 | Lấy tệp hình ảnh và bảng thông tin sản phẩm trực tiếp từ người bán, kèm sự cho phép bằng văn bản. Sự cho phép phải nói rõ việc dùng hình trong video tiếp thị liên kết có người dẫn AI trên Shopee Video, và người bán xác nhận mình có quyền với hình đó. Không tải hình hay thu thập thông tin từ trang sản phẩm trên sàn. | Đề xuất cho E-105, E-107(n) và Q-006 (xem 1.2) |
| R8 | Mỗi video có kịch bản riêng. Mỗi câu thông tin trong kịch bản gắn với một nguồn trong bảng thông tin của người bán. Không dùng từ "nhất", "duy nhất", "tốt nhất", "số một". Không dùng các từ "review", "trải nghiệm", "mình đã dùng". | Đề xuất cho Điều 19, E-106, E-108 |
| R9 | Mỗi video được tạo riêng sau khi kịch bản của nó đã được duyệt. Không tạo theo lô, không dùng API, không tự động đăng. Tối đa 12 video trong 30 ngày và tối đa 1 video mỗi ngày. | Đề xuất cho E-107(t) |
| R10 | Mỗi sản phẩm có tối đa 2 video, và hai video có trọng tâm thông tin khác nhau. Không dùng lại phần mở đầu, ngoài các câu công bố cố định. | Đề xuất cho E-108 (nội dung lặp lại) |
| R11 | Công bố quảng cáo theo ba lớp: người dẫn nói ở đầu video; chữ trên màn hình suốt video; dòng đầu của chú thích. CEO tự bật nhãn AI trên Shopee mỗi lần đăng. | E-047, E-108; xem 2.6 |
| R12 | Sản phẩm trong hình đúng là sản phẩm được gắn thẻ, kể cả đúng phân loại. | E-109 (theo ghi chép người thu thập) |

### 1.2 Ba câu hỏi về quy tắc nền tảng mà O-012 chạm tới

#### a) Cấm "tạo video hàng loạt" bằng công cụ (E-107)

**Bằng chứng nói gì.**
- E-107 là [OFFICIAL POLICY], bài 199468, đề ngày 05/09/2024. Mục này chưa được chụp lại, nên dựa trên ghi chép người thu thập.
- Mục (t) cấm công cụ làm giả lượt xem, lượt nhấp hay đơn hàng, hoặc công cụ "tạo video hàng loạt".
- Mức phạt tới VND 10 triệu cho mỗi hành vi. Quy đổi ra khoảng USD 380 là ESTIMATE: 10,000,000 / 26,300 = 380.2, theo tỷ giá UNVERIFIED.

**Bằng chứng không nói gì.**
- Không có định nghĩa "hàng loạt" và không có ngưỡng số lượng.
- Không nói việc dùng công cụ tạo người dẫn AI có tự nó bị coi là "tạo video hàng loạt" hay không.

**[INFERENCE].** Theo nghĩa thông thường, "hàng loạt" là tạo nhiều video cùng lúc theo kiểu dây chuyền. Một người dẫn, tối đa 12 video trong 30 ngày, mỗi video có kịch bản riêng và được CEO duyệt riêng (R8 đến R10), thì xa nghĩa đó. Nhưng đây là suy luận, không phải cách đọc của Shopee.

**Chuyển tiếp.**
- Q-013 cho Shopee: CEO hỏi bằng văn bản.
- Q-017 cho context-agent: chụp lại bài 199468 có trích dẫn trước lần đăng đầu.
- Rủi ro được ghi ở RK-018.

#### b) Các mẫu "nội dung AI kém chất lượng" của Shopee (E-108)

E-108 đã được chụp lại, [OFFICIAL POLICY]. Đối chiếu từng mẫu:

| Mẫu nêu trong E-108 hoặc ghi chép 3.5 | O-012 xử lý thế nào | Còn mở |
|---|---|---|
| Video AI không tự gắn nhãn thì bị xóa, kể từ 23/09/2026. Nhãn tự động "Video có thể chứa nội dung do AI tạo ra" không thay được việc tự gắn nhãn. | R3, R11: CEO tự bật nhãn mỗi lần đăng. | Vị trí nút bật trong ứng dụng là UNVERIFIED. CEO xác nhận ở lần đăng đầu. |
| "Nội dung lặp đi lặp lại" | R9, R10: kịch bản riêng, tối đa 2 video mỗi sản phẩm, trọng tâm khác nhau. | Shopee không nêu ngưỡng. |
| "Sử dụng cùng một AI avatar trên nhiều tài khoản khác nhau" | Một người dẫn tạo từ mô tả (prompt), không phải avatar có sẵn. Avatar có sẵn được dùng chung giữa các khách hàng, theo bằng chứng giá, mục "Possible blockers". Người dẫn chỉ dùng trên một tài khoản Shopee. | Điều khoản của HeyGen ghi "User Output may not be unique" (trang nhà cung cấp, P3). Bằng chứng không nói quy tắc này có áp dụng xuyên nền tảng hay không, ví dụ với dự phòng TikTok. Chuyển Q-015 và OD18. |
| "Nội dung thiếu tính nguyên bản" | Hình ảnh là của người bán, không phải của CEO. Phần nguyên bản là kịch bản và cách trình bày. | Bằng chứng không giải quyết được việc phần đó có đủ hay không. Chuyển Q-015. Đây là lý do O-012 trượt EC-008 (xem 1.3). |
| Đánh giá giả do AI tạo (ghi chép 3.5) | R4, R8: không nói đã dùng, không dùng từ "review". | Không còn mở. |
| Trẻ vị thành niên do AI tạo trông như thật; người nổi tiếng khi chưa có đồng ý | R5: người dẫn là một người lớn chung chung, không giống người thật. | Không còn mở. |

- Chế tài tăng dần: giảm hiển thị, rồi xóa video, rồi mất quyền dùng Shopee Video tạm thời hoặc vĩnh viễn (E-108, đã chụp lại).
- Shopee Video là kênh duy nhất của thí điểm. Vì vậy RK-009 được nâng lên high (xem 5.1).

#### c) Hình ảnh của người bán: "nội dung của Shopee" (E-105) hay của người bán?

**Bằng chứng nói gì** (E-105, đã chụp lại, [OFFICIAL POLICY], bài 122944):
- Đối tác "không được sao chép tên thương hiệu, logo, hình ảnh hoặc hiện diện của Shopee". Theo câu chữ, đối tượng bị cấm là tên thương hiệu, logo, hình ảnh và sự hiện diện **của Shopee**. Ảnh sản phẩm của người bán không được nêu.
- Cùng điều khoản còn có câu: "Shopee nghiêm cấm việc Đối Tác TTLK thu thập bất kỳ thông tin nào trên Sàn TMĐT".
- E-107(n), theo ghi chép người thu thập, cấm dùng nội dung của người khác "mà không được sự cho phép".

**Bằng chứng không nói gì.**
- Không nói ảnh trên trang sản phẩm thuộc người bán hay được cấp quyền cho Shopee.
- Không nói việc tự tay đọc hay tải ảnh từ trang sản phẩm có tính là "thu thập thông tin" hay không.
- Không nói người bán có quyền với ảnh không. Nhiều ảnh có thể là ảnh của hãng.

**[INFERENCE].**
- Nếu tệp ảnh và bảng thông tin do chính người bán gửi, kèm sự cho phép bằng văn bản (R7), thì việc dùng ảnh nằm ngoài câu chữ của E-107(n). Ảnh cũng không bị lấy từ sàn, nên câu chữ của lệnh cấm thu thập không chạm tới.
- Điều này không trả lời được việc người bán có quyền cấp phép hay không.

**Chuyển tiếp.**
- Q-014 cho Shopee: ảnh sản phẩm có phải "hình ảnh của Shopee" không; lệnh cấm thu thập có áp dụng không.
- Q-006 cho luật sư (đã mở, nay mở rộng): quyền của người bán, và sự cho phép thế nào là đủ.
- Rủi ro được ghi ở RK-010, mở lại.

### 1.3 Chấm O-012 theo EC-001 đến EC-012

**Phương pháp** giống TR-MC15-002:
- Mỗi tiêu chí chỉ có hai kết quả: đạt hoặc trượt.
- Tiêu chí nào không chứng minh được là đạt thì tính là trượt.
- Tiêu chí must-have bị trượt thì loại phương án.

**Hai nhánh của Điều 15a(3)** (E-046, [OFFICIAL LAW], Luật 75/2025):
- **Nhánh A:** luật sư kết luận người tiếp thị liên kết là người có ảnh hưởng.
- **Nhánh B:** luật sư kết luận không phải.

Nhánh B được chấm ở hai trạng thái:
- **B, đủ điều kiện:** các điều kiện P1 đến P8 ở mục 1.5 đã đạt.
- **B, hôm nay:** ngày 2026-10-26, chưa điều kiện nào đạt.

Nhánh A được chấm với giả định P1 đến P8 đã đạt.

| Tiêu chí | Mức ưu tiên | A | B, đủ điều kiện | B, hôm nay | Bằng chứng và lý do |
|---|---|---|---|---|---|
| EC-001 Tuân thủ quy tắc nền tảng và luật VN | must-have | **trượt** | đạt | **trượt** | **A:** C2 xác định nhánh bất lợi là CEO phải đã dùng từng sản phẩm. Câu chữ Điều 15a(3)(a) là: chưa sử dụng hoặc chưa hiểu rõ thì không được giới thiệu. R1 không mua mẫu, nên không có sản phẩm nào đã được dùng. Câu "chưa hiểu rõ" có cho phép giới thiệu chỉ dựa trên hiểu biết hay không là câu hỏi luật, chuyển Q-016. Không suy diễn ở đây. **B, đủ điều kiện:** E-107(n) được đáp ứng nhờ R7. E-105 và lệnh cấm thu thập không bị chạm tới theo câu chữ, và có câu trả lời của Shopee (P4). Nhãn AI có (E-108). Hình ảnh đúng sản phẩm (E-109). Công bố quảng cáo có (E-047). Thông tin trung thực vì có nguồn (Điều 19, Điều 15a(2)(c)). Chỉ chạy Shopee, nên RK-001 vẫn giữ biện pháp. **B, hôm nay:** chưa có sự cho phép nào được ghi lại. Q-013 và Q-014 chưa có trả lời. E-107 chưa được chụp lại. |
| EC-002 Công bố quảng cáo, và nhãn AI khi áp dụng, trên mọi bài đăng | must-have | đạt | đạt | đạt | Đây là quy tắc đăng bài R3 và R11, CEO áp dụng ở mỗi lần đăng. Ở nhánh A, việc thông báo trước và trong khi quảng cáo (Điều 15a(3)(b)) cũng làm được nhờ ba lớp công bố. Mẫu công bố theo luật vẫn là Q-012 cho luật sư, như trong TR-MC15-002. |
| EC-003 Chi phí trong USD 77.41 và trần đo lường USD 34.42 | must-have | đạt | đạt | đạt | HeyGen Creator trả theo tháng USD 29 (bằng chứng giá, P1). Khoản này nằm trong dòng cố định USD 42.99 (CEO-D-702), nếu CEO duyệt OD13. Chi phí đo lường là USD 0. Tiêu chí này cho phép đề xuất thay đổi cho chủ sở hữu duyệt. **A:** công cụ được hủy gia hạn. Trong tháng có cả công cụ lẫn mẫu, tiền mẫu bị giới hạn ở USD 13.99, trừ khi chủ sở hữu nâng mức (xem OD15 và 2.2). |
| EC-004 Có bằng chứng tiếng Việt hiển thị và đọc đúng | must-have | đạt | đạt | **trượt** | Chất lượng khớp môi tiếng Việt là UNVERIFIED với mọi công cụ avatar (bằng chứng giá, mục "Possible blockers"). Chỉ đạt sau khi CEO thử và đạt bài thử T1 hoặc T2 ở mục 2.4 (điều kiện P1). |
| EC-005 Không có hành động nào của CEO trước khi đủ điều kiện tham gia, chụp lại trang và các quyết định mở lại được giải quyết | must-have | đạt | đạt | đạt | CEO-D-1301 đã quyết các quyết định được mở lại. Các bước mới (thử công cụ, liên hệ người bán) được xếp sau khi CEO xác nhận đủ điều kiện (RK-004). O-012 dựa vào E-107 và E-109, hai mục chưa được chụp lại, nên P5 yêu cầu chụp lại trước lần đăng đầu (C12, Q-010). |
| EC-006 Thời gian tới hoa hồng hợp lệ đầu tiên ngắn | high | **trượt** | đạt | đạt | **B:** bỏ bước mua và dùng mẫu (TR-MC15-002, ngày 4 đến 7). Video đầu có thể đăng từ ngày 10 (mục 3). **A:** hình thức này dừng (R6), nên không còn hoa hồng nào từ nó. |
| EC-007 Thời gian của CEO cho mỗi video thấp | high | **trượt** | đạt | đạt | **B:** không quay và không dùng sản phẩm. ESTIMATE khoảng 47.5 phút mỗi video (mục 3.4), cần đo lại khi chạy. **A:** hình thức dừng. Hình thức thay thế O-007 trượt EC-007 (TR-MC15-002). |
| EC-008 Ít rủi ro với các quy tắc về nội dung không nguyên bản, giới hạn đăng, tạo hàng loạt, hiển thị sản phẩm và nhãn AI | high | **trượt** | **trượt** | **trượt** | O-012 là nội dung AI thật, nên nằm thẳng trong phạm vi E-108: mất quyền dùng Shopee Video. Với "thiếu tính nguyên bản", hình ảnh không phải của CEO. Phạm vi E-107(t) chưa xác định. R7 đến R10 giảm rủi ro nhưng không chứng minh được rủi ro "thấp". Ở điểm này O-012 giống O-006, phương án cũng trượt EC-008. |
| EC-009 Ít rủi ro về tuyên bố sản phẩm và ngành hàng bị hạn chế | high | đạt | đạt | đạt | Ngách O-008. Quy tắc R5, R8. Không mỹ phẩm hay thực phẩm chức năng (CEO-D-1301 mục 1). E-036, E-106. |
| EC-010 Đảo ngược được với chi phí thấp | high | **trượt** | đạt | đạt | **B:** gói tháng hủy được, chi phí mất tối đa USD 29. Dừng đăng mà không vi phạm thì không có khoản chi phí nào được nêu (E-110, đã chụp lại). **A:** gỡ video không xóa được việc đã giới thiệu sản phẩm chưa dùng trước đó (K-001). Mức phạt theo luật quảng cáo sửa đổi chưa được nghiên cứu (Q-012). Việc CEO có tự gỡ được video Shopee Video của mình hay không cũng là UNVERIFIED (Q-018). Vì vậy không chứng minh được "chi phí thấp". |
| EC-011 Có bằng chứng nhu cầu, ghi rõ là bên thứ ba | medium | đạt | đạt | đạt | Ở cấp ngách, bằng chứng không đổi: E-116 [THIRD-PARTY]. Không có bằng chứng về hiệu quả theo hình thức nội dung (shopee 4.10 không tìm thấy). Tiêu chí này chấm theo ngách, như TR-MC15-002. |
| EC-012 Dùng lại quy trình dựng hình đã có | medium | trượt | trượt | trượt | Dùng công cụ bên ngoài, không dùng `src/` hay `graphics/`. Giống O-005. |

**Tổng hợp:**

| Trạng thái | Số tiêu chí đạt | Tiêu chí đạt | Tiêu chí trượt | Must-have |
|---|---|---|---|---|
| Nhánh A | 6/12 | EC-002, EC-003, EC-004, EC-005, EC-009, EC-011 | EC-001, EC-006, EC-007, EC-008, EC-010, EC-012 | Trượt EC-001, nên bị loại |
| Nhánh B, đủ điều kiện | 10/12 | EC-001, EC-002, EC-003, EC-004, EC-005, EC-006, EC-007, EC-009, EC-010, EC-011 | EC-008, EC-012 | Đạt cả năm |
| Nhánh B, hôm nay | 8/12 | EC-002, EC-003, EC-005, EC-006, EC-007, EC-009, EC-010, EC-011 | EC-001, EC-004, EC-008, EC-012 | Trượt hai, chưa khởi động được |

### 1.4 So sánh với O-005, O-006 và O-007

Điểm của O-005, O-006 và O-007 lấy từ TR-MC15-002 và không đổi giữa hai nhánh. Lý do: mỗi hình thức đó đều được tính kèm việc mua và dùng mẫu (C2).

| Phương án | Nhánh A | Nhánh B | Trượt | Ưu điểm so với O-012 | Nhược điểm so với O-012 |
|---|---|---|---|---|---|
| O-005: quay tay, có mẫu | 10/12 | 10/12 | EC-007, EC-012 | Không phụ thuộc nhánh. Không có rủi ro AI. | CEO tốn thời gian nhất. Chi phí mẫu. |
| O-006: chỉ đồ họa vẽ bằng code | 8/12 | 8/12 | EC-001, EC-004, EC-006, EC-008 | Dùng lại quy trình dựng hình đã có. | Không khởi động được: cần architect (RK-016). |
| O-007: cảnh quay trước, lớp chữ sau | 11/12 | 11/12 | EC-007 | Điểm cao nhất. Không phụ thuộc nhánh. | CEO tốn thời gian quay. Chi phí mẫu. |
| **O-012: người dẫn AI** | **6/12** | **10/12 (đủ điều kiện)** | A: EC-001, EC-006, EC-007, EC-008, EC-010, EC-012. B: EC-008, EC-012 | Nhẹ việc nhất cho CEO trong các hình thức khởi động được ngay (đạt EC-007). Không phải mua mẫu. | Phụ thuộc nhánh. Rủi ro AI và rủi ro hình ảnh người bán (EC-008). Lý do của OD9 không còn đúng. |

Đọc bảng:
- Ở nhánh B, O-012 đổi EC-007 (được) lấy EC-008 (mất) so với O-007, và kém O-007 thêm EC-012. Tổng là 10 so với 11.
- O-007 vẫn đạt điểm cao hơn và không phụ thuộc câu trả lời của luật sư.
- Ở nhánh A, O-012 bị loại vì trượt must-have EC-001. CEO-D-1301 mục 6 đã tự đặt điều kiện dừng cho trường hợp này.

### 1.5 Kết luận: O-012 có phải điểm khởi đầu thí điểm có thể bảo vệ được không?

**Có, nhưng có điều kiện. Điều kiện đó chưa đạt vào ngày 2026-10-26.**

O-012 bảo vệ được khi cả ba điều sau cùng đúng:
1. Luật sư chưa trả lời, hoặc đã trả lời theo nhánh B.
2. Tất cả các điều kiện P1 đến P8 dưới đây đã đạt trước video đầu tiên.
3. Chủ sở hữu đã chấp nhận rõ ràng phần rủi ro còn lại cho các video đăng trước khi luật sư trả lời Q-002 (RK-020, OD16).

Ở nhánh A, O-012 không bảo vệ được. Hình thức này dừng theo R6, và O-007 (cảnh quay, có mẫu) là hình thức dự phòng nếu CEO giữ nó (OD15).

Về tiêu chí, O-007 vẫn là hình thức mạnh hơn: 11/12 và không phụ thuộc nhánh. Bản sửa của CEO đổi tính bền vững giữa các nhánh lấy thời gian của CEO. Đây là quyền của chủ sở hữu. Phụ lục ghi lại cái giá của sự đánh đổi đó, không đảo ngược nó.

**Các điều kiện trước video đầu tiên:**

| # | Điều kiện | Làm đạt tiêu chí hoặc rủi ro | Ngày (mục 3) |
|---|---|---|---|
| P1 | CEO tự làm bài thử khớp môi tiếng Việt và đạt T1 hoặc T2 (mục 2.4). | EC-004, RK-017 | ngày 2 và 3 |
| P2 | CEO định giá và duyệt công cụ, kỳ thanh toán và dòng ngân sách (OD12, OD13) trước khi trả tiền. | EC-003, RK-019 | ngày 7 |
| P3 | Mỗi hình ảnh có sự cho phép bằng văn bản của người bán, đã được lưu lại. Tệp ảnh và bảng thông tin do người bán gửi. Người bán xác nhận mình có quyền với ảnh (R7). | EC-001, RK-010 | ngày 5 đến 9 |
| P4 | Có câu trả lời bằng văn bản của Shopee cho Q-013, Q-014 và Q-015, và câu trả lời không bất lợi. Nếu tới ngày 10 vẫn chưa có, EC-001 vẫn là trượt. CEO khi đó quyết theo OD16 có chấp nhận rủi ro mà đăng hay không. | EC-001, RK-018, RK-009, RK-010 | gửi ngày 1, hạn chờ ngày 10 |
| P5 | Context-agent chụp lại có trích dẫn các bài 199468 (E-107) và 164064 (E-109), và không có thay đổi bất lợi (Q-017). | EC-005, C12, Q-010 | ngày 1 đến 3 |
| P6 | Q-002 đã được gửi luật sư, đánh dấu ưu tiên, kèm Q-016. CEO đã quyết OD16. | RK-002, RK-020 | trước ngày 1 |
| P7 | Từng video qua đủ R1 đến R12 trong danh sách duyệt D-002 (mục 3.3). | EC-001, EC-002, EC-008, EC-009 | mỗi lần đăng |
| P8 | Giữ nguyên từ TR-MC15-002: CEO đủ điều kiện tham gia (RK-004); đăng ký xong và đã kiểm tra việc ghi nhận hoa hồng trên Shopee Video (RK-007). | EC-005, EC-006 | trước ngày 1 và ngày 1 đến 3 |

---

## 2. Chọn công cụ

### 2.1 Xếp hạng cho thí điểm này

Số liệu lấy từ bằng chứng giá (trang nhà cung cấp, truy cập 2026-10-26). Các phép tính được ghi rõ.

| Tiêu chí | HeyGen Creator | D-ID Pro + Vbee Tiêu chuẩn |
|---|---|---|
| Giá theo tháng | USD 29 (P1) | USD 29 (D1), cộng 169,000 VND (B1), khoảng USD 6.43. ESTIMATE: 169,000 / 26,300 = 6.43, tỷ giá UNVERIFIED. **Tổng khoảng USD 35.43.** |
| Giá quy đổi theo năm mỗi tháng | USD 24, trả trước USD 288. Phép tính: 24 × 12 = 288. | USD 16, trả trước USD 191 (D1). Vbee: 459,000 VND/năm, khoảng USD 1.45/tháng. ESTIMATE: 459,000 / 12 / 26,300 = 1.45. **Tổng khoảng USD 17.45.** |
| Tiếng Việt | Có giọng tiếng Việt trong công cụ (P4). Chất lượng khớp môi là UNVERIFIED. | Giọng tiếng Việt bản địa của Vbee, tải lên dưới dạng âm thanh. Khớp môi với âm thanh tải lên là UNVERIFIED. |
| Hình mờ, dấu AI | Không có hình mờ (P1). | Có dấu "AI" ở góc dưới trái và không gỡ được trên gói Pro (D3). Dấu này không thay được nhãn Shopee. |
| Quyền thương mại | Người dùng sở hữu đầu ra (P3). | Được dùng thương mại, kể cả quảng cáo (D2). Quyền thương mại của Vbee theo điều khoản là UNVERIFIED. |
| Người dẫn riêng | Tạo từ mô tả, hoàn toàn tổng hợp (P5). Huấn luyện tốn 60 tín dụng (P2). | Có "Generate from text" (D5, hướng dẫn cũ khoảng 8 tháng). |
| Đủ dung lượng cho 12 video | 600 tín dụng. Với Avatar IV video look: 12 phút × 31 + 61 = 433 tín dụng, đủ. Với Avatar V: 12 × 48 + 61 = 637, vượt 600. Vì vậy dùng Avatar IV, hoặc tối đa khoảng 11.2 phút Avatar V. ESTIMATE: (600 − 61) / 48 = 11.2. | 15 phút, làm tròn theo bước 15 giây, không chuyển sang tháng sau (D1, D4). |
| Số tài khoản CEO phải giữ | 1 | 2 |
| Khung 9:16 | UNVERIFIED (không có trích dẫn từ trang chính thức) | Không có trong bằng chứng |

**Xếp hạng:**
1. **HeyGen Creator, trả theo tháng. Chỉ mua khi đạt bài thử T1.**
   - Theo tháng, đây là lựa chọn rẻ hơn: USD 29 so với khoảng USD 35.43.
   - CEO chỉ phải giữ một tài khoản và một quy trình.
   - Không có hình mờ.
   - Có tài liệu chính thức về việc tạo người dẫn tổng hợp riêng từ mô tả.
2. **D-ID Pro cộng Vbee Tiêu chuẩn, trả theo tháng, làm dự phòng.** Chỉ dùng khi T1 trượt và T2 đạt.
   - Ưu điểm duy nhất cho thí điểm 30 ngày là giọng Vbee bản địa, phòng khi giọng tiếng Việt của HeyGen trượt bài thử.
   - Mức khoảng USD 17.5/tháng trong bằng chứng giá chỉ đúng khi trả theo năm. Trả theo năm cần USD 191 cộng 459,000 VND trả trước, nên không hợp với thí điểm 30 ngày (xem 2.3).

Nếu cả T1 và T2 đều trượt, không có người dẫn AI. Thí điểm khi đó chuyển sang OD15, tức O-007 với mẫu.

### 2.2 Dòng ngân sách: đề xuất phán quyết cho chủ sở hữu (OD13)

**Sự kiện:**
- Ngân sách tháng là USD 77.41, gồm USD 34.42 trần đo lường và USD 42.99 khoản cố định. Theo CEO-D-702, khoản cố định USD 42.99 "still has no recorded home".
- Theo bản ghi Recommendation Gate, dòng mẫu OD6 không được dùng (release note).
- HeyGen và D-ID là gói thuê bao cố định, không phải chi phí đo lường (bằng chứng giá, mục "Budget classification").

**Đề xuất:**
- Ghi khoản thuê bao công cụ người dẫn AI vào dòng cố định USD 42.99.
- Giữ trần đo lường USD 34.42 nguyên vẹn. Thí điểm không dùng gì từ trần này.
- Tổng USD 77.41 không đổi.

**Phần dư trên dòng cố định (ESTIMATE):**

| Lựa chọn | Phép tính | Phần dư |
|---|---|---|
| HeyGen | 42.99 − 29 | USD 13.99 |
| D-ID + Vbee | 42.99 − 35.43 | USD 7.56 |

**Phần dư dùng để làm gì.** Phần dư che phí thẻ, chênh tỷ giá hoặc VAT với dịch vụ số nước ngoài. Tất cả các khoản này là UNVERIFIED (bằng chứng giá, mục "Billing practicalities"). Nếu số tiền thực bị trừ vượt USD 42.99, CEO báo lại trước kỳ gia hạn.

**Ở nhánh A** (dùng OD15):
- Trong tháng đã trả tiền công cụ, tiền mẫu trên dòng cố định chỉ còn USD 13.99. ESTIMATE: 42.99 − 29 = 13.99.
- Muốn mua đủ mẫu như OD6 (USD 42) thì chủ sở hữu phải nâng ngân sách tháng đó.

### 2.3 Trả theo tháng hay theo năm

Khuyến nghị **trả theo tháng**.

| Công cụ | Trả trước theo năm | Giá theo tháng | Gói năm rẻ hơn khi dùng ít nhất |
|---|---|---|---|
| HeyGen | USD 288 | USD 29 | khoảng 9.9 tháng (288 / 29 = 9.93) |
| D-ID | USD 191 | USD 29 | khoảng 6.6 tháng (191 / 29 = 6.59) |

Lý do:
- Thí điểm dài 30 ngày và có điều kiện dừng (R6). Gói năm biến khoản chi đảo ngược được thành khoản đã trả trước. Điều đó làm trượt EC-010 và vượt dòng cố định USD 42.99 ngay trong tháng đầu.
- Chính sách hoàn tiền của HeyGen và D-ID không có trong bằng chứng (UNVERIFIED).
- Tín dụng chưa dùng của gói tháng HeyGen được giữ thêm một tháng (P1).

Chỉ cân nhắc gói năm khi thí điểm được gia hạn sau ngày 30.

### 2.4 Bài thử CEO làm trước khi trả tiền

- **CEO tự làm.** CEO tự đăng nhập bằng tài khoản của mình. Agent không đăng nhập, không giữ thông tin đăng nhập và không xem thông tin thanh toán.
- **Không đăng video thử.** Đầu ra của gói miễn phí không được dùng thương mại và có hình mờ (bằng chứng giá).

**T1, ngày 2: gói miễn phí HeyGen** (3 video mỗi tháng, tối đa 1 phút, có hình mờ, theo P1).
- CEO tạo một video dài 40 đến 60 giây từ **kịch bản thử** do agent soạn sẵn. Kịch bản này là văn bản không gắn với sản phẩm thật.
- Kịch bản thử có đủ 6 thanh điệu, có số và đơn vị (ví dụ "1,5 lít", "220V", "35.000 đồng"), có một tên thương hiệu nước ngoài, và có câu công bố "Video tạo bởi AI. Video có liên kết tiếp thị, tôi nhận hoa hồng nếu bạn mua qua liên kết".
- Gói miễn phí có tạo được người dẫn từ mô tả hay không là UNVERIFIED. Bài thử dùng avatar có sẵn chỉ để kiểm tra khớp môi.

**T2, ngày 3, chỉ khi T1 trượt: dùng thử D-ID** (14 ngày, 3 phút, theo D1) với âm thanh từ gói miễn phí của Vbee (3,000 điểm mỗi ngày, theo B1).
- Gói miễn phí của Vbee có thể chèn quảng cáo âm thanh. Đây là [INFERENCE] từ B1. Nếu có, CEO ghi lại và bỏ qua đoạn đó khi chấm.

**Tiêu chí đạt.** Phải đạt cả 5 tiêu chí. CEO là người bản ngữ và tự chấm.

| # | Tiêu chí | Cách kiểm |
|---|---|---|
| 1 | Môi khớp âm tiết | Xem không tắt tiếng. Không có đoạn lệch thấy rõ. |
| 2 | Thanh điệu đúng | Nghe từng thanh. Không có từ nào bị đổi nghĩa. |
| 3 | Số, đơn vị và tên đọc đúng | Đối chiếu với kịch bản. |
| 4 | Không có lỗi hình | Mặt, răng và tay không méo. |
| 5 | Xuất được khung dọc 9:16 | Với HeyGen là UNVERIFIED. Bài thử này kiểm tra luôn. |

- Kết quả, ngày làm và công cụ được ghi vào bảng đo của thí điểm.
- Thời gian CEO bỏ ra cho bài thử (ESTIMATE): 30 đến 45 phút cho T1, thêm 45 phút cho T2 nếu phải làm. Không có nguồn, cần đo.

### 2.5 Quy tắc một người dẫn, một tài khoản

- CEO tạo **đúng một** người dẫn **từ mô tả**, vào ngày mua công cụ (ngày 7). Không dùng avatar có sẵn, vì avatar có sẵn được dùng chung giữa các khách hàng.
- Agent soạn mô tả: một người lớn chung chung, không giống người thật, không mặc áo blouse hay đồng phục y tế, không có dáng người nổi tiếng hay quan chức. Mô tả tuân theo R5, E-031 và E-108.
- Lưu lại mô tả, ngày tạo và mã người dẫn trong công cụ.
- Người dẫn chỉ xuất hiện trên một tài khoản Shopee Video của CEO. Không dùng trên bất kỳ tài khoản nào khác, kể cả tài khoản TikTok dự phòng sau này, cho tới khi Q-015 có câu trả lời (OD18).
- Còn lại một rủi ro: HeyGen ghi "User Output may not be unique" (P3). Một người dẫn tạo từ mô tả giảm được rủi ro trùng với người khác, nhưng không loại bỏ được (RK-021).

### 2.6 Ai bật nhãn "Video tạo bởi AI" trên Shopee

**CEO bật nhãn, ở mỗi lần đăng.**
- E-108 (đã chụp lại) yêu cầu "Người bán và Nhà Sáng Tạo Nội Dung cần bật tính năng Gắn nhãn thông báo 'Video tạo bởi AI'".
- Agent không đăng gì (CEO-D-1301), nên người làm việc này là CEO.

Những thứ **không** thay được việc CEO tự bật nhãn:
- nhãn tự động "Video có thể chứa nội dung do AI tạo ra" (E-108);
- dấu "AI" ở góc video của D-ID (D3);
- câu công bố người dẫn nói trong video.

Vị trí của nút bật trong ứng dụng là UNVERIFIED. Ở lần đăng đầu, CEO chụp màn hình bước bật nhãn để làm bằng chứng cho các lần sau.

---

## 3. Kế hoạch 30 ngày đã xếp lại

Các bước bị CEO-D-1301 bỏ đều bị xóa: mua mẫu, dùng mẫu, quay tay. Mọi bước mới đều có ngày. "CEO" nghĩa là CEO tự làm. "Agent" chỉ soạn văn bản; agent không đăng nhập, không liên hệ ai và không đăng gì.

### 3.1 Lịch theo khối ngày

| Ngày | Bước | Ai làm | Mới, đổi hay giữ | Điều kiện để qua bước |
|---|---|---|---|---|
| Trước ngày 1 | Tự xác nhận riêng các điều kiện: đủ 18 tuổi, CCCD gắn chip, mã số thuế, tài khoản ngân hàng đứng tên mình (RK-004). | CEO | giữ | Thiếu một mục thì chuyển sang O-004. |
| Trước ngày 1 | Quyết OD12 đến OD18. | CEO | mới | |
| Trước ngày 1 | Gửi luật sư Q-001 đến Q-006 và Q-012. Đánh dấu **Q-002 và Q-016 là ưu tiên**, đề nghị trả lời trước ngày 10. | CEO | đổi | P6 |
| Ngày 1 | Đăng ký Shopee Affiliate với tư cách KOC cá nhân. Không đăng ký kênh TikTok nào. Nhập thông tin thanh toán. Việc đăng ký kéo dài tới ngày 3. | CEO | giữ | |
| Ngày 1 | Gửi Shopee câu hỏi bằng văn bản Q-013, Q-014, Q-015 và Q-018. Agent soạn sẵn văn bản tiếng Việt. | CEO gửi, agent soạn | mới | P4 |
| Ngày 1 đến 3 | Chụp lại bài 199468, 164064 và 122941, và đoạn "30 ngày" trong bài 122944 (Q-017). | context-agent, do orchestrator giao | mới | P5 |
| Ngày 2 | Bài thử T1 (mục 2.4). | CEO | mới | P1 |
| Ngày 3 | Bài thử T2, chỉ khi T1 trượt. Đọc và ghi bảng tỷ lệ hoa hồng. Kiểm tra việc ghi nhận hoa hồng qua Shopee Video (RK-007). | CEO | mới, giữ | P1, P8 |
| Ngày 4 | Lập danh sách tối đa 10 sản phẩm ứng viên cho 6 vị trí. Điều kiện: đồ gia dụng và nhà bếp; ít rủi ro về tuyên bố; không thuộc danh sách không có hoa hồng (E-103); ưu tiên gian hàng Mall [THIRD-PARTY 4.8]; đã có tỷ lệ hoa hồng. Agent soạn bảng tiêu chí. | CEO chọn | đổi | |
| Ngày 5 | Gửi từng người bán một lời xin phép riêng, không gửi hàng loạt (E-107(z) cấm nhắn tin hàng loạt). Lời xin phép đề nghị: (1) cho phép dùng các ảnh cụ thể trong video tiếp thị liên kết có người dẫn AI và nhãn AI trên Shopee Video; (2) xác nhận người bán có quyền với ảnh; (3) gửi tệp ảnh và bảng thông tin sản phẩm. Agent soạn mẫu thư tiếng Việt. | CEO | mới | P3 |
| Ngày 6 đến 9 | Nhận trả lời. Với mỗi ảnh: lưu ảnh chụp cuộc trò chuyện có sự cho phép, kèm tên gian hàng và ngày. Sản phẩm nào không có trả lời trước ngày 9 thì bỏ. | CEO | mới | P3 |
| Ngày 6 đến 9 | Soạn kịch bản từ bảng thông tin của người bán, theo R4 và R8. Mỗi câu thông tin có một dòng nguồn. | agent soạn, CEO kiểm | mới | |
| Ngày 7 | CEO duyệt giá và trả tiền gói tháng, nếu P1 đạt, OD12 và OD13 đã quyết, và đã có ít nhất một sự cho phép. Tạo người dẫn duy nhất (mục 2.5). | CEO | mới | P2 |
| Ngày 8 và 9 | Tạo video 1 và video 2, mỗi video một lần tạo riêng (R9). CEO duyệt theo danh sách D-002 ở mục 3.3. | CEO | mới | P7 |
| **Ngày 10** | **Cổng đăng đầu tiên.** Phải đạt đủ P1, P2, P3, P5, P7 và P8. Nếu P4 (Shopee trả lời) hoặc câu trả lời Q-002 chưa có, CEO quyết theo OD16. | CEO | mới | P1 đến P8 |
| Ngày 10 đến 28 | Mỗi ngày tối đa 1 video, tổng tối đa 12 video, tối đa 2 video mỗi sản phẩm. **Đăng dồn về đầu:** ít nhất 6 video trước ngày 16, vì thời điểm hoa hồng được ghi có không có nguồn (K-016, mục 4.3). Mỗi video: CEO duyệt, bật nhãn AI, gắn thẻ sản phẩm, viết chú thích có công bố. | CEO | đổi | P7 |
| Ngày 10 đến 30 | Kiểm tra email ít nhất 2 ngày một lần và từ chối mọi KOL Booking trong 3 ngày (RK-013). Phản hồi kết quả hoa hồng trong 24 giờ (E-102). Ghi số đo mỗi ngày. | CEO | giữ | |
| Bất kỳ ngày nào | **Điều kiện dừng:** áp dụng ngay khi có câu trả lời bất lợi của luật sư, hoặc khi Shopee trả lời bất lợi cho Q-013, Q-014 hay Q-015 (mục 3.2). | CEO | mới | |
| Ngày 29 và 30 | Đọc kết quả theo bậc 2 (mục 4.3). Quyết định giữ hay hủy gói tháng trước ngày gia hạn, khoảng ngày 37. ESTIMATE: ngày mua 7 + 30 = 37. | CEO | đổi | |

### 3.2 Điều kiện dừng, và số phận các video đã đăng

| Sự kiện | Việc làm | Hạn |
|---|---|---|
| Luật sư kết luận theo nhánh A (người tiếp thị liên kết là người có ảnh hưởng) | (1) Ngừng mọi lần đăng mới bằng người dẫn AI (R6). (2) CEO gỡ toàn bộ video có người dẫn AI. Lý do không sửa video: chức năng sửa bài Shopee Video là UNVERIFIED, và sửa cũng không đổi được việc đã giới thiệu sản phẩm chưa dùng. (3) Lập danh sách video đã gỡ, gồm ngày đăng và ngày gỡ. (4) Tắt gia hạn gói tháng. (5) Hỏi luật sư về rủi ro của các video đã đăng (Q-016). (6) Nếu OD15 giữ O-007, mua mẫu theo dòng mẫu, dùng sản phẩm, rồi quay. | Ngừng đăng: cùng ngày. Gỡ video: trong 24 giờ, đề xuất ở OD14 (ESTIMATE, không có nguồn về hạn luật định). |
| Shopee trả lời rằng O-012 vi phạm E-105, E-107 hoặc E-108 | Như trên, chỉ với các video thuộc phạm vi câu trả lời. Không cần hỏi Q-016. | Như trên |
| Shopee giảm hiển thị hoặc xóa một video vì lý do AI (E-108) | Ngừng đăng. CEO và tech-lead xem lại R8 đến R10 trước khi đăng tiếp. Lần thứ hai thì dừng hình thức. | Cùng ngày |
| CEO không gỡ được video (Q-018) | Ghi lại. Báo luật sư. Hỏi Shopee hỗ trợ bằng văn bản. | Trong 24 giờ |

- Hoa hồng của các đơn đã đặt trước khi gỡ: bằng chứng không nói việc gỡ video ảnh hưởng thế nào tới ghi nhận hoa hồng (UNVERIFIED). Không dựa vào khoản này.
- Không có video nào được đặt lịch, vì mọi KOL Booking đều bị từ chối. Vì vậy quy tắc "bài đặt lịch không gỡ được" (E-089) không áp dụng.

### 3.3 Danh sách duyệt từng bài đăng (D-002)

CEO duyệt riêng từng video theo danh sách này. Thiếu một mục thì không đăng.

1. Có sự cho phép đã lưu cho mọi ảnh trong video (R2, R7).
2. Mọi câu thông tin khớp với bảng nguồn (R4, R8).
3. Không có từ ngữ nói đã dùng sản phẩm, không có từ "review", không có từ ngữ tuyệt đối (R4, R8).
4. Không có tuyên bố về da, tóc hay cơ thể. Không có bác sĩ, người nổi tiếng hay quan chức (R5).
5. Người dẫn là người dẫn duy nhất đã đăng ký (R3, R5).
6. Câu công bố có ở đầu video, chữ công bố có trên màn hình suốt video (R11).
7. Sản phẩm trong hình trùng với sản phẩm được gắn thẻ (R12).
8. Sau khi đăng: nhãn "Video tạo bởi AI" đã bật (R3, R11). Dòng đầu chú thích có công bố.
9. Tổng số video không vượt R9 và R10.

### 3.4 Thời gian của CEO cho mỗi video (ESTIMATE)

Không có số đo nào. Mọi con số dưới đây là giả định của tech-lead và cần đo trong thí điểm.

| Việc | Phút |
|---|---|
| Xin phép người bán: 15 phút mỗi sản phẩm, chia cho 2 video | 7.5 |
| Kiểm kịch bản so với bảng nguồn | 10 |
| Tạo video trong công cụ, kể cả tạo lại một lần | 15 |
| Duyệt theo danh sách D-002 | 5 |
| Tải lên, gắn thẻ, bật nhãn, viết chú thích | 10 |
| **Tổng mỗi video** | **47.5** (7.5 + 10 + 15 + 5 + 10) |

- Khoảng dao động ESTIMATE: 32.5 đến 75 phút.
  - Thấp: 7.5 + 5 + 10 + 5 + 5 = 32.5.
  - Cao: 7.5 + 15 + 30 + 7.5 + 15 = 75.
- Việc làm một lần (ESTIMATE): bài thử 30 đến 90 phút; tạo người dẫn 30 đến 45 phút; lập danh sách sản phẩm khoảng 60 phút; gửi câu hỏi cho Shopee khoảng 20 phút.

### 3.5 Danh sách hành động của CEO, thay cho TR-MC15-002

Các hành động 6, 7 và 8 cũ bị bỏ. Danh sách mới:

1. Xác nhận điều kiện tham gia (giữ).
2. Quyết các quyết định chủ sở hữu, gồm OD12 đến OD18.
3. Gửi câu hỏi cho luật sư, ưu tiên Q-002 và Q-016.
4. Tự đăng ký Shopee Affiliate.
5. Gửi câu hỏi cho Shopee.
6. Đọc bảng tỷ lệ và kiểm tra việc ghi nhận hoa hồng.
7. Làm bài thử T1, và T2 nếu cần.
8. Chọn sản phẩm.
9. Xin phép từng người bán và lưu lại sự cho phép.
10. Duyệt giá, trả gói tháng, tạo người dẫn.
11. Duyệt từng video và tự đăng, có nhãn và công bố.
12. Kiểm tra email và kết quả hoa hồng.
13. Áp dụng điều kiện dừng khi có sự kiện.
14. Đọc kết quả ngày 30 và quyết định giữ hay hủy gói.

---

## 4. Hòa vốn cập nhật (ESTIMATE)

### 4.1 Công thức

Công thức của TR-MC15-002, đã kiểm lại bằng phép tính: với C = 42 thì công thức cho đúng các số 165–184, 61–68 và 31–34 của TR-MC15-002.

**N = ⌈ C / (AOV × r × f) ⌉**

| Ký hiệu | Nghĩa | Giá trị và nguồn |
|---|---|---|
| C | Chi phí thí điểm một tháng | Tiền công cụ + tiền mẫu (USD 0, theo R1) + chi phí đo lường (USD 0) |
| AOV | Giá trị đơn trung bình | USD 7. E-115 [THIRD-PARTY], độ tin cậy thấp. |
| r | Tỷ lệ hoa hồng | Giả định: 3.68% (E-093 [THIRD-PARTY]), 10%, 20%. Không có nguồn chính thức (C5). |
| f | Hệ số thực nhận của Shopee | Từ 0.9902 (tốt nhất) tới 0.8902 (xấu nhất). Lấy theo TR-MC15-002, Delivery Impact: (1 − 0.0098 − t) với t từ 0 tới 10%. |
| — | Tỷ lệ đơn bị hủy | 0 |

### 4.2 Kết quả

Phép tính chạy bằng `python -I -c` trong phiên này. Kết quả là số đơn cần có trong một tháng.

| Tỷ lệ r | Hoa hồng thực mỗi đơn (f = 0.9902 → 0.8902) | HeyGen theo tháng, C = USD 29 | D-ID + Vbee theo tháng, C = USD 35.43 | Để so sánh: mẫu cũ, C = USD 42 |
|---|---|---|---|---|
| 3.68% | USD 0.2551 → 0.2293 (7 × 0.0368 × f) | **114 đến 127** (29 / 0.2551 = 113.7; 29 / 0.2293 = 126.5) | 139 đến 155 | 165 đến 184 |
| 10% | USD 0.6931 → 0.6231 | **42 đến 47** (29 / 0.6931 = 41.8; 29 / 0.6231 = 46.5) | 52 đến 57 | 61 đến 68 |
| 20% | USD 1.3863 → 1.2463 | **21 đến 24** (29 / 1.3863 = 20.9; 29 / 1.2463 = 23.3) | 26 đến 29 | 31 đến 34 |

**Lưu ý:**
- **Mức trần mỗi đơn** (E-093 [THIRD-PARTY], độ tin cậy thấp): nguồn này nêu trần VND 30,000 mỗi đơn.
  - Ở mức 20%, một đơn USD 7 cho hoa hồng 7 × 26,300 × 0.20 = VND 36,820, vượt trần đó.
  - Nếu trần có thật, hoa hồng mỗi đơn bị chặn ở khoảng USD 1.14. ESTIMATE: 30,000 / 26,300 = 1.14.
  - Khi đó dòng 20% của HeyGen thành **26 đến 29 đơn**. Phép tính: 29 / (1.1407 × 0.9902) = 25.7; 29 / (1.1407 × 0.8902) = 28.6.
  - TR-MC15-002 chưa xét điểm này.
- **Nếu chuyển sang gói năm**, với C bằng giá quy đổi theo tháng, HeyGen cần 95–105, 35–39 và 18–20 đơn. Không khuyến nghị gói năm (mục 2.3).
- **Mọi tỷ lệ đều là giả định.** Tỷ lệ thật được đọc sau khi đăng ký (Q-007).

### 4.3 Sửa K-016: E-094 và lý do của bậc 2

**Sự kiện.**
- E-094 trong bản chụp lại chỉ trích một câu: "Thời gian lưu giữ Cookies là 7 (bảy) ngày cho cả ứng dụng di động Shopee và website" (bài 122944, đã chụp lại).
- Câu "đơn thành công hoàn tất trong 30 ngày kể từ lượt truy cập", cùng câu "đặt hàng trong vòng 7 ngày sau khi nhấp", chỉ có trong ghi chép người thu thập (shopee-vn-and-market.md mục 2.4, các bài 122944 và 122941). Bản chụp lại không trích hai câu này.

**Sửa.**
- **Bỏ** câu "orders complete within 30 days of the visit (E-094)" khỏi lý do chọn bậc 2.
- Câu này giữ trạng thái **UNVERIFIED (chưa chụp lại)** cho tới khi Q-017 chụp lại bài 122941 và đoạn liên quan của bài 122944.
- Sự kiện đã xác nhận chỉ còn: cookie kéo dài 7 ngày.

**Hệ quả cho bậc 2** ("ít nhất một hoa hồng hợp lệ được ghi có trước ngày 30", CEO-D-1301 mục 2):
- Không nguồn nào đã chụp lại cho biết bao lâu sau khi đặt, một đơn được coi là hợp lệ và hoa hồng được ghi có. Lịch chi trả E-088 cũng chưa được chụp lại.
- Vì vậy chưa biết bậc 2 có đo được trong 30 ngày hay không.
- Bậc 2 vẫn là lựa chọn của CEO. Lý do còn lại của nó là: đây là tín hiệu tiền thật sớm nhất. Lý do này không cần nguồn về thời gian.
- Cách xử lý đề xuất:
  - Đăng dồn về đầu: ít nhất 6 video trước ngày 16.
  - Ngày 30, ghi hai số riêng: "đơn đã ghi nhận, đang chờ" và "hoa hồng hợp lệ đã ghi có".
  - Nếu tới ngày 30 chỉ có đơn đang chờ, kết quả bậc 2 ghi là "chưa xác định" và được đọc lại vào ngày 45 (OD17).

---

## 5. Sổ rủi ro và quyết định chủ sở hữu

### 5.1 Các RK được mở lại, đóng hoặc đổi theo K-001, K-005, K-006 và K-010

| ID | Thay đổi | Mức độ | Khả năng | Trạng thái mới | Biện pháp mới | Người chịu |
|---|---|---|---|---|---|---|
| RK-002 | **Mở lại (K-001).** Điều 15a(3): CEO-D-1301 bỏ biện pháp mua, dùng rồi thông báo. Lý do của OD9 ("tuân thủ dù luật sư trả lời thế nào") không còn đúng. | high (giữ như cũ) | possible | **open** | Điều kiện dừng R6. Q-002 và Q-016 được gửi ưu tiên. OD16. Gỡ video theo OD14. Rủi ro của video đã đăng được tách ra RK-020. | CEO với luật sư |
| RK-009 | **Mở lại (K-005).** Nhãn AI và các mẫu nội dung AI kém chất lượng của E-108 nay áp dụng thẳng vào nội dung của thí điểm. | **high**, nâng từ medium. Lý do: chế tài là mất Shopee Video, kênh duy nhất (E-108, đã chụp lại). | possible | **open** | R3, R9, R10, R11. Một người dẫn trên một tài khoản. CEO tự bật nhãn. Q-015. Điều kiện dừng ở 3.2. | CEO |
| RK-010 | **Mở lại (K-006).** Hình ảnh của người bán: E-107(n), E-105, lệnh cấm thu thập, quyền của người bán (Q-006). | medium (giữ) | possible | **open** | R7: sự cho phép bằng văn bản, tệp do người bán gửi, người bán xác nhận quyền. Q-014 và Q-006. | CEO với luật sư |
| RK-014 | **Đóng (K-010).** Không mua mẫu trong thí điểm. Chi phí công cụ được chuyển sang RK-019. | medium | — | **resolved**. Mở lại nếu OD15 kích hoạt O-007. | OD15 | CEO |
| RK-015 | **Đổi biện pháp.** Biện pháp cũ "mẫu mua ngoài liên kết" không còn áp dụng, vì không có mẫu. | low | unlikely | mitigated | CEO không mua gì qua liên kết của mình (E-096) | CEO |
| RK-016 | **Không đổi.** O-012 không dùng quy trình dựng hình. | low | likely | open | Như cũ | architect |

Các RK khác của TR-MC15-002 không đổi: RK-001, RK-003 đến RK-008, RK-011 đến RK-013.

### 5.2 RK mới

| ID | Rủi ro | Mức độ | Khả năng | Ảnh hưởng tới việc giao | Biện pháp | Người chịu | Trạng thái |
|---|---|---|---|---|---|---|---|
| RK-017 | Chất lượng khớp môi tiếng Việt là UNVERIFIED với mọi công cụ avatar trong bằng chứng giá: HeyGen, D-ID, Synthesia, Vidnoz, Hedra, Captions, Akool. | high | possible | Trượt EC-004. Không có video. | Bài thử T1 và T2. Nếu cả hai trượt, chuyển OD15. | CEO | open |
| RK-018 | Lệnh cấm "tạo video hàng loạt" (E-107(t), chưa chụp lại) không có định nghĩa. | high | unlikely ([INFERENCE] với R9) | Phạt tới VND 10 triệu mỗi hành vi. Có thể mất tài khoản. | R9. Q-013 cho Shopee. Q-017 chụp lại bài 199468. | CEO | open |
| RK-019 | Dòng ngân sách cho công cụ chưa được quyết. Phí thẻ, tỷ giá và VAT là UNVERIFIED. | medium | certain | Chưa mua công cụ được, nên chưa có video. | OD13. Phần dư USD 13.99 (ESTIMATE). | CEO | open |
| RK-020 | Video đăng trước khi luật sư trả lời Q-002: nếu kết luận theo nhánh A, gỡ video không xóa được việc đã giới thiệu (K-001). Khả năng tự gỡ video là UNVERIFIED (Q-018). | high | possible | Rủi ro pháp lý cá nhân cho CEO. Trượt EC-010 ở nhánh A. | Gửi Q-002 ưu tiên. Chờ tới ngày 10 rồi CEO quyết OD16. Gỡ theo OD14. Hỏi Q-016. | CEO với luật sư | open |
| RK-021 | Điều khoản công cụ: đầu ra có thể không độc nhất (P3); người dùng phải tự công bố AI (P3); mức tiêu tín dụng có thể đổi; chính sách hoàn tiền là UNVERIFIED. | low | possible | Người dẫn có thể trùng với người khác (E-108). Tín dụng có thể thiếu. | Người dẫn tạo từ mô tả. Dùng Avatar IV (433 trên 600 tín dụng, ESTIMATE). Trả theo tháng. | CEO | open |
| RK-022 | Thời điểm hoa hồng được ghi có không có nguồn đã chụp lại (K-016). | medium | certain | Kết quả bậc 2 có thể là "chưa xác định" vào ngày 30. | Đăng dồn về đầu. Ghi hai số riêng. OD17. Q-017. | CEO | open |
| RK-023 | Không biết bao nhiêu người bán sẽ trả lời và cho phép. | medium | possible | Ít hơn 6 sản phẩm. Video đầu bị lùi sau ngày 10. | Danh sách 10 ứng viên. Hạn trả lời ngày 9. | CEO | open |

**Đếm lại các mục chặn** (mức độ critical hoặc high, trạng thái open, tính trên sổ TR-MC15-002 cộng phụ lục):

| Nguồn | Mục |
|---|---|
| TR-MC15-002 | RK-003, RK-004, RK-006 (RK-006 đã được CEO-D-1301 quyết; trạng thái chính thức chưa đổi) |
| Phụ lục | RK-002, RK-009, RK-017, RK-018, RK-020 |

Vì vậy vị trí sẵn sàng của O-012 chỉ có thể là "tiến hành có điều kiện", với các điều kiện P1 đến P8. Không thể là "tiến hành" vô điều kiện.

### 5.3 Câu hỏi mở mới

| ID | Câu hỏi | Người trả lời | Ảnh hưởng tới |
|---|---|---|---|
| Q-013 | Video người dẫn AI tạo riêng từng cái, tối đa 12 video trong 30 ngày, có bị coi là "tạo video hàng loạt" theo E-107(t) không? | Shopee, CEO hỏi bằng văn bản | EC-001, RK-018 |
| Q-014 | Ảnh sản phẩm trên trang của người bán có phải "hình ảnh của Shopee" theo E-105 không? Lệnh cấm "thu thập bất kỳ thông tin nào trên Sàn" có áp dụng cho ảnh và thông tin do người bán gửi trực tiếp, hoặc cho việc tự đọc trang sản phẩm, không? | Shopee | EC-001, RK-010 |
| Q-015 | Người dẫn AI trên ảnh của người bán, với kịch bản riêng, có đạt yêu cầu "tính nguyên bản" của E-108 không? Quy tắc "cùng một AI avatar trên nhiều tài khoản" có áp dụng xuyên nền tảng, ví dụ với một tài khoản TikTok dự phòng sau này, không? | Shopee | EC-008, RK-009, OD18 |
| Q-016 | Nếu người tiếp thị liên kết là người có ảnh hưởng: (a) các video đã đăng trước câu trả lời có rủi ro gì, và gỡ video có khắc phục được không; (b) cụm "chưa hiểu rõ" trong Điều 15a(3)(a) có cho phép giới thiệu chỉ dựa trên hiểu biết không? Mục (b) chỉ dành cho luật sư; phụ lục không suy diễn. | Luật sư | RK-002, RK-020, OD14 |
| Q-017 | Chụp lại có trích dẫn: bài 199468 (E-107), bài 164064 (E-109), bài 122941, và đoạn "30 ngày" của bài 122944 (E-094). | context-agent, do orchestrator giao | P5, EC-005, RK-018, RK-022 |
| Q-018 | Nhà sáng tạo có tự gỡ hoặc sửa được video Shopee Video của mình không? Nút bật nhãn AI nằm ở đâu trong ứng dụng? | CEO trong ứng dụng, nếu không thì Shopee | mục 3.2, RK-020 |
| Q-019 | Thế nào là sự cho phép đủ của người bán (tin nhắn trên Shopee, email hay văn bản ký)? Khi ảnh thuộc về hãng thì cần gì thêm? (Mở rộng Q-006.) | Luật sư | RK-010, P3 |

### 5.4 Quyết định chủ sở hữu mới

Mỗi quyết định có một đề xuất. CEO quyết.

| ID | Quyết định | Đề xuất của tech-lead |
|---|---|---|
| OD12 | Công cụ và kỳ thanh toán | HeyGen Creator, **trả theo tháng USD 29**, chỉ mua sau khi đạt T1. Nếu T1 trượt và T2 đạt: D-ID Pro theo tháng USD 29 cộng Vbee Tiêu chuẩn theo tháng 169,000 VND, tổng khoảng USD 35.43 (ESTIMATE). Nếu cả hai trượt: không dùng người dẫn AI, chuyển OD15. |
| OD13 | Dòng ngân sách | Ghi khoản thuê bao vào dòng cố định USD 42.99, không vào trần đo lường USD 34.42. Tổng USD 77.41 không đổi. Nếu số tiền thực bị trừ vượt USD 42.99, CEO được báo trước kỳ gia hạn. |
| OD14 | Video đã đăng khi luật sư kết luận bất lợi | CEO **gỡ toàn bộ** video có người dẫn AI trong 24 giờ sau khi có văn bản của luật sư. Không sửa video. Lập danh sách. Hỏi Q-016. |
| OD15 | Giữ cảnh quay O-007 làm hình thức dự phòng không | **Giữ.** O-007 với mẫu là dự phòng khi kết luận theo nhánh A, hoặc khi T1 và T2 cùng trượt. Kích hoạt lại dòng mẫu OD6, tối đa USD 42 mỗi tháng. Trong tháng đã trả tiền công cụ, tiền mẫu chỉ còn USD 13.99, trừ khi CEO nâng ngân sách. |
| OD16 | Đăng trước khi có câu trả lời của luật sư (Q-002) và của Shopee (Q-013 đến Q-015) | Chờ tới ngày 10. Nếu vẫn chưa có trả lời, CEO quyết rõ ràng có chấp nhận RK-018 và RK-020 hay không. Quyết định này **thay lý do của OD9**: đây là chấp nhận rủi ro, không còn là "tuân thủ dù luật sư trả lời thế nào". |
| OD17 | Cách đọc bậc 2 sau khi sửa K-016 | Giữ bậc 2. Ngày 30 ghi hai số "đơn đã ghi nhận, đang chờ" và "hoa hồng hợp lệ đã ghi có". Nếu chỉ có đơn đang chờ, ghi "chưa xác định" và đọc lại vào ngày 45. |
| OD18 | Phạm vi của quy tắc một người dẫn | Người dẫn chỉ dùng trên một tài khoản Shopee Video. Không dùng trên tài khoản nào khác, kể cả tài khoản TikTok dự phòng, cho tới khi Q-015 có câu trả lời. Khi cần, hình thức cho kênh dự phòng (K-008) sẽ được chấm riêng. |

---

## 6. Phạm vi của các chữ "mọi" và "cả hai" trong phụ lục

| Cụm từ | Gồm những trường hợp nào |
|---|---|
| "cả hai nhánh" | Nhánh A (người tiếp thị liên kết là người có ảnh hưởng theo Điều 15a(3)) và nhánh B (không phải). Không có nhánh thứ ba trong C1 và C2. |
| "mọi bài đăng" | Mỗi video trên Shopee Video, chú thích của nó, thẻ sản phẩm của nó, và mọi lần đăng lại. Thí điểm không có bài TikTok. Livestream nằm ngoài thí điểm. |
| "mọi công cụ avatar" (RK-017) | HeyGen, D-ID, Synthesia, Vidnoz, Hedra, Captions và Akool. Vbee và FPT.AI chỉ là công cụ giọng nói. |
| "mọi tỷ lệ" (mục 4) | 3.68%, 10% và 20%, mỗi tỷ lệ với f = 0.9902 và f = 0.8902. |
| "mọi hình ảnh" (P3) | Mỗi ảnh tĩnh hoặc đoạn video của người bán xuất hiện trong bất kỳ video nào. |

## 7. Tự kiểm tra trước khi giao

| Kiểm tra | Kết quả |
|---|---|
| Mọi mã E-, EC-, O-, RK-, Q-, OD- được dùng đều được định nghĩa ở đây hoặc trong TR-MC15-002 | đạt |
| Mọi con số có nguồn, hoặc ghi ESTIMATE kèm phép tính | đạt |
| Không ghi quyết định cổng nào là đã được đưa ra. Mọi OD là đề xuất cho CEO. | đạt |
| Không suy diễn luật. Các điểm luật chuyển Q-002, Q-006, Q-016, Q-019; các điểm quy tắc nền tảng chuyển Q-013, Q-014, Q-015, Q-018. | đạt |
| Không có hành động nào của agent cần đăng nhập, liên hệ người bán hay đăng bài | đạt |
| Đếm lại các mục chặn khớp với sổ ở 5.1 và 5.2 | đạt |
| Hòa vốn tái lập đúng số cũ với C = 42, nên công thức được dùng đúng | đạt |
| Đã kiểm tra bằng Validation Engine | **chưa**. Phụ lục không phải artifact của run và không có envelope. Không có bằng chứng của run nào được lưu cho phụ lục này. |
