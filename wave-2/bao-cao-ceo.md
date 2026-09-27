# Wave 2 — Báo cáo CEO

**Run `run-3a58551ee912` · ticket MC-3 · 2026-09-26 đến 2026-09-27 · trạng thái: Completed, 6/6 phase, 6/6 gate.**

> Mọi con số trong báo cáo này là **ĐO ĐƯỢC** trừ khi ghi rõ là ESTIMATE. Đây là lần đầu tiên công ty sở hữu số liệu thật thay vì ước lượng.

---

## 1. Kết quả một câu

Wave 2 đã sản xuất **một video hoàn chỉnh, đi hết mười hai stage**, và **giữ lại đúng như thiết kế** — không đăng, không tạo kênh, không tạo tài khoản, không chi một đồng nào. Anh đã duyệt item này lúc 02:15:57 ngày 27/09.

Nhưng kết quả quan trọng hơn cái video: **Wave 2 chứng minh được rằng bộ máy này nói thật khi sự thật bất lợi.** Chi tiết ở §4 — đó là phần tôi muốn anh đọc kỹ nhất.

---

## 2. Bốn điều wave này phải chứng minh

| # | Câu hỏi | Trả lời |
|---|---|---|
| 1 | Một item tốn bao nhiêu? | **Một phần.** Media đo chính xác; nửa token chưa đo được |
| 2 | CEO duyệt mất bao nhiêu phút? | **1 phút 58 giây** — nhưng là cận dưới, không dùng để đặt ngưỡng |
| 3 | Có chứng minh được compliance không? | **Có, và cụ thể.** 3/5 chứng minh được, 2/5 không — cả hai đều ở bề mặt upload |
| 4 | Thư viện có đủ ảnh không? | **Không.** 0 số liệu trên 14 chủ đề. **Không bịa một con số nào** |

### 2.1 Chi phí — $0,870880, và tại sao tôi KHÔNG báo cáo "tiết kiệm 67%"

| Khoản | Số lượng | Chi phí |
|---|---|---|
| Narration | 11.096 ký tự (1.929 từ) | $0,554800 |
| Đồ hoạ gốc | 22 (`GFX-01`–`GFX-22`) | $0,00 |
| Clip stock có licence | 18 (`CLIP-01`–`CLIP-18`) | $0,00 biên |
| Thumbnail | 6 (`THUMB-01`–`THUMB-06`) | $0,316080 |
| **Video AI sinh ra** | **0 giây** | **$0,00** |
| | **Tổng media** | **$0,870880** |

So với hạn mức **$2,647440/item** (option O-002): **32,9%**, còn dư $1,776560.

**Tôi từ chối tuyên bố mức tiết kiệm.** Con số đo được là **một sàn, không phải một tổng** — nửa token của production path chưa đo được vì boundary `M-001` chưa chạy. Nói "dưới ngân sách 67%" là trình bày một phép đo dở dang như thể nó đã đủ, đúng cùng một loại lỗi với việc bịa số clip.

**Một đính chính quan trọng (`C-001`):** ticket MC-3 bảo so với **$1,58**. Con số đó đã lỗi thời — nó chỉ tính research, script, fact-check, SEO, QC. Con số anh đã duyệt là **O-002: $2,647440 biến đổi, $5,95/video trọn gói ở 13 video/tháng**. So với $1,58 sẽ tạo ra một mức vượt chi 68% **không có thật**.

### 2.2 Phút duyệt — 1:58, và tại sao chưa dùng được

`RK-005` giờ có điểm dữ liệu đầu tiên: **review 1 phút 58 giây, queue 0, rework 0, duyệt ngay lần đầu.**

**Điều này chứng minh được:** máy đo tồn tại và cho ra số. `D-007` quy định phút duyệt phải suy ra từ hai mốc thời gian ghi lại — trước nay chưa ai suy ra được lần nào.

**Điều này KHÔNG chứng minh được, và không được dùng để đặt ngưỡng.** Năm lý do, tất cả đều kéo con số xuống:

1. **Đây là duyệt một item ĐANG GIỮ, không phải item sẵn sàng đăng.** Cái duyệt mà `D-002` thực sự nói tới — thả một video hoàn chỉnh lên kênh — vẫn **chưa bao giờ xảy ra**.
2. **Anh đã biết trước nội dung** — anh chọn chủ đề, anh xác nhận lỗi supply, anh thấy các phát hiện khi chúng xuất hiện.
3. **Bề mặt duyệt đã được dựng sẵn.** `D-002` yêu cầu đúng điều này, nên con số thấp một phần đo **chất lượng bề mặt duyệt** — đó là lợi ích thật và tái sử dụng được — và một phần đo việc bản tóm tắt đã đọc hộ.
4. **Không có queue time** vì anh đang ngồi đó. Ở 13 item/tháng, artifact sẽ phải chờ.
5. **n = 1.**

**Khuyến nghị:** giữ nguyên máy đo sang Wave 3, đo mọi lần duyệt, **và chưa bàn ngưỡng trước item thứ mười**. Thành phần quyết định việc duyệt-từng-item có mở rộng được hay không là **rework time**, và nó đang bằng 0 ở đây.

### 2.3 Compliance — 3 chứng minh được, 2 không, 0 bỏ ngỏ

| # | Xác định | Kết quả |
|---|---|---|
| 1 | Tự đánh giá tính nguyên bản | **CHỨNG MINH ĐƯỢC** — 23/23 luận điểm do narration và đồ hoạ gánh; xoá cả 18 clip vẫn là một bài luận hoàn chỉnh |
| 2 | Phát hiện trùng lặp | **KHÔNG** — chưa có bản render để hash, catalogue của ta rỗng |
| 3 | Công bố media tổng hợp | **CHỨNG MINH ĐƯỢC** — 5/5 loại phần tử đã đánh giá, không phần tử nào cần công bố |
| 4 | Công bố affiliate / tài trợ | **CHỨNG MINH ĐƯỢC, theo hướng tích cực** — không có cái nào (`D-022`) |
| 5 | Tự xếp hạng advertiser-suitability | **KHÔNG** — bảng hỏi chỉ có khi upload; nhưng **14/14 hạng mục đã phân loại kèm căn cứ** |

**Cả hai cái không chứng minh được đều nằm ở bề mặt upload** — tức là chúng không chứng minh được *vì Wave 2 không đăng*, chứ không phải vì bộ máy thiếu năng lực. Đây là câu trả lời tốt nhất có thể có.

Điểm đáng chú ý: code **bắt buộc** điều này. `DeterminationResolution.IsWellFormed` yêu cầu cả lý do lẫn cách khắc phục mới cho phép kết luận "không chứng minh được", và `Blocks = !IsWellFormed`. Xác định số 2 **từ chối ghi một khoảng cách**, với lý do: *"ghi một con số ở đây chính là bịa ra đại lượng mà tiêu chí này tồn tại để kiểm tra."*

### 2.4 Thư viện — 0 số liệu, và đó là câu trả lời trung thực

**14 chủ đề được kiểm, 0 số liệu thu được, 0 con số bịa ra.** Lý do: thư viện cần tài khoản đăng nhập và đã từng chặn truy cập tự động; ngoài ra vai trò này không được phép tự ra ngoài internet. Mỗi dòng đều ghi **lý do** và **cách khắc phục**.

---

## 3. Chi phí vận hành bộ máy — con số chưa ai từng có

**3.146.022 token** cho toàn bộ run: sáu phase, mười lần gọi agent, ~3 giờ 27 phút.

| | Token | Tỷ lệ |
|---|---|---|
| Làm đúng lần đầu | 1.708.209 | 54,3% |
| **Làm lại** | **1.437.813** | **45,7%** |

**Gần một nửa chi phí quản trị của run này là làm lại.** Ba điều cần nói kèm:

1. **Mọi lần từ chối đều là lỗi thật. Không có lần nào là báo động giả.** Validator bắt được: một decision record bị chính người tạo ký (vi phạm Producer Exclusion), trace yêu cầu viết dưới dạng khoảng, tên nhà cung cấp lọt vào qua quy ước trích dẫn, định danh không định nghĩa, và một trường version sai. **45,7% là giá của việc artifact đúng, không phải bằng chứng lãng phí.**
2. **Lần gọi đắt nhất là vòng sửa sau review — 466.485 token**, nhiều hơn bất kỳ phase sạch nào, và nó được kích hoạt bởi một bản review tìm ra hai lỗi thật. **Đó là chi phí của việc review có tác dụng.**
3. **Resume agent tiết kiệm thời gian, không tiết kiệm token.** Xác nhận ba lần: mỗi lần sửa tốn *nhiều* token hơn lần bị sửa, nhưng nhanh hơn 5–8 lần về đồng hồ.

**QUAN TRỌNG:** đây là **capex phát triển** (`C-002`), tốn một lần cho một thay đổi, **không phải chi phí mỗi video**. Không được chia cho một item.

**Và một cảnh báo (`C-003`):** toàn bộ 12 agent đều chạy `model: inherit` — **không có tiering ở đâu cả**. Tỷ lệ 290.000/87.000 L3-so-với-L1/L2 đang gánh toàn bộ lập luận kiểm soát chi phí của công ty **chưa từng được thử nghiệm bởi bất cứ thứ gì**. `$2,647440` và `$77,41` là **giả định, không phải phép đo**, cho tới khi `M-001` chạy thật.

---

## 4. Phần tôi muốn anh đọc kỹ nhất — bộ máy nói thật khi bất lợi

Đây là kết quả có giá trị nhất của Wave 2, và nó không nằm trong bốn mục tiêu ban đầu.

**Agent implementation bác bỏ chính bản tóm tắt của nó.** Tôi bảo nó rằng `honey bee` = 1.213 clip là số liệu đáng tin. Nó đọc quy tắc gốc, phát hiện `honey bee` là **hai từ** nên không đủ điều kiện, và ghi: *"đọc lại bằng chứng cho khớp với chỉ thị sẽ phá hỏng chính việc kiểm toán."* **Lỗi là của tôi**, và nó đã lọt qua ba người đọc — tôi trình bày, anh quyết, tôi viết vào lệnh. Ai cũng kiểm tra con số so với quy tắc; **không ai kiểm tra cái truy vấn**.

**Reviewer không xác nhận mọi thứ — nó thử phá.** Tôi bảo nó kiểm tra `P8`, cái chốt chặn việc bịa số clip. Nó **mutation-test** thay vì xác nhận, chạy 6 biến thể, và **một cái thoát được**: một con số giả đặt trong dòng bị định dạng lệch khỏi regex, cộng một dòng mồi thứ 15 để khôi phục số dòng — **vượt qua cả 28 check với con số giả hiện rõ trong bảng**. Xác nhận `P8` tồn tại sẽ bỏ sót điều này.

**Agent documentation từ chối lệnh của tôi.** Tôi đề nghị nó thêm một câu giải thích vì sao hai bản báo cáo lệch nhau. Nó từ chối: lời giải thích đó không được xác lập bởi bất kỳ artifact nào trong input của nó, thêm vào sẽ trượt check `P1` và khiến nó tự viết ra cái hoà giải mà hợp đồng cấm nó viết. Nó ghi vào envelope kèm nguồn gốc thay vì công bố. **Bộ máy chống lại chính người đang điều khiển nó — đó là cách duy nhất nó có giá trị.**

**Producer Exclusion hoạt động dưới tầm chú ý của con người.** Check `D17.6` bắt được agent thiết kế **tự ký** decision record của chính nó. Quy tắc này không chỉ nằm ở cổng — nó nằm trong validation của artifact.

---

## 5. Rủi ro mang theo — 24 known issue, không giấu cái nào

Ba cái lớn nhất:

1. **Supply chưa xác minh cho cả loài lẫn hành vi** (`D-200`). Giới hạn được chặn bằng cấu trúc: chỉ 3 clip chưa có nguồn, **không luận điểm nào phụ thuộc vào chúng**, và script **từ chối** thay bằng cảnh tổ ong chung chung.
2. **Đăng ký thư viện theo kênh CHƯA làm.** Doanh thu mất trước khi đăng ký là không lấy lại được. Đây là điều kiện tiên quyết có code chặn.
3. **`R-007`** — giới hạn trung thực của chính bản sửa: chốt chặn so gói với chính nó, nên một chủ đề mà script *đáng lẽ* phải gọi nhưng không gọi thì vẫn vô hình.

**Hai định danh cần anh biết:** `T-023` ghi **không-chạy** thay vì mang theo một lần pass dự đoán, vì *"một lần pass được dự đoán không phải là một kết quả."* Và hai bản báo cáo **lệch nhau** về tổng số check — cả hai đều đúng tại thời điểm của mình, và chúng được **công bố kèm nhau** thay vì bị hoà giải.

---

## 6. Phát hiện cho framework

| | Kết quả |
|---|---|
| Defect 2 | **Đã sửa upstream** — `policy-exception` giờ là lệnh thật |
| Defect 4 | **BÁC BỎ, chứng minh hai lần.** Verification Gate quyết bởi `omn-qa`, runtime chấp nhận |
| Defect 3 | **Có biến thể thứ hai** — reviewer trung thực báo cáo path bị chặn. Là lỗ hổng thiết kế |
| Mới | **Trích dẫn định danh trần resolve NHẦM một cách im lặng** — 3 lần trong một artifact. Không check nào bắt được, vì chúng *có* định nghĩa, chỉ là định nghĩa khác |
| Mới | **Datastore có BA trạng thái**, không phải hai: biến chưa đặt → 15 skip; đặt và tới được → 318 pass; **đặt nhưng không tới được → 15 FAIL**, tức connection string cũ báo cáo một suite hỏng thay vì một suite chưa chạy |

---

## 7. Điều kiện để Wave 3 bắt đầu

1. **Đăng ký thư viện theo kênh** — điều kiện tiên quyết, chưa làm.
2. **Xác nhận two-step verification** trên kênh — nửa còn lại của tiền đề §21.3.
3. **Một phiên đăng nhập thư viện** để lấy số clip thật cho 14 chủ đề và xác nhận bằng mắt 3 clip waggle-run.
4. **Quyết định chi tiêu** để render: narration audio và tải clip.
5. **Bắt `M-001` ghi lại tier thực dùng cho mỗi operation** — nên là một acceptance criterion tường minh của wave đầu tiên chạy nó. Không có nó, lập luận chi phí vẫn không kiểm chứng được dù có sản xuất bao nhiêu video.
