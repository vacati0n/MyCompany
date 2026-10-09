# Báo cáo CEO — Wave 7: Năng lực kinh tế AI

**Run `run-56bcc037b633` · Ticket MC-8 · Bắt đầu và đóng 09/10/2026 · Hoàn tất 6/6 phase, 6/6 cổng.**

---

## 1. Kết luận trong năm dòng

1. **Hệ thống đã sẵn sàng chọn model theo bằng chứng, nhưng chưa có bằng chứng nào.** Theo quyết định của anh (`CEO-D-600`), wave này không gọi model tính phí. Bộ benchmark vì vậy vẫn trống. Mọi thứ hạng model mà hệ thống đưa ra hiện là **thứ tự cấu hình, và được ghi rõ như vậy**, không bao giờ trình bày như bằng chứng.
2. **Đã có bộ điều khiển chi phí chạy bằng quy tắc cố định, không gọi AI:** 50% chỉ cảnh báo, 75% hạ tầng suy luận, 90% hoãn, 100% từ chối. Nếu không biết số tiền đã chi (*chưa đo*), hệ thống **từ chối việc tính phí và ghi rõ lý do riêng**, không coi là 0 và cũng không coi là còn ngân sách.
3. **Review tìm ra 4 lỗi high qua hai vòng; tất cả đã sửa và được kiểm tra lại độc lập.** Lỗi nặng nhất: một thao tác có thể bị **ghi chi phí hai lần**, và hai request đồng thời có thể **cùng vượt ngân sách**. Lỗi high thứ tư do chính phán quyết của tôi gây ra (xem mục 4).
4. **Chi tiêu vận hành: USD 0.00**, wave thứ bảy liên tiếp. Quyền chi một thao tác **vẫn chưa dùng và vẫn còn**.
5. **Chi phí phát triển: 2,912,242 token, khoảng 4 giờ agent**, nhiều hơn Wave 6 24%. Tỷ lệ làm lại 17.0%; khoảng một nửa số đó là do lỗi phán quyết của tôi. Validator không từ chối lần nào.

---

## 2. Những gì đã giao (bằng chứng, không phải lời hứa)

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|
| Bản ghi benchmark (mục 16 kế hoạch tổng) | **Giao** | Điểm chất lượng nằm trong hợp ba trường hợp (quan sát / quan sát bằng 0 / chưa đo). Chi phí và độ trễ **chỉ lấy từ thao tác thật** mà bản ghi tham chiếu, không ai được tự điền. Store kiểm tra hình dạng từng dòng. |
| Chọn model từ chối xếp hạng khi có giá trị chưa đo | **Giao** | Chỉ xếp hạng khi mọi đại lượng so sánh đều đã quan sát, cùng đơn vị. Nếu không, hệ thống trả về thứ tự cấu hình kèm nhãn. Mỗi lần xếp hạng ghi lại dữ liệu đầu vào và số quan sát. |
| Bộ điều khiển chi phí 50/75/90/100 | **Giao** | Quy tắc cố định, không cấu hình được, không gọi model; ngưỡng nghiêm nhất được áp dụng. Hạ tầng **không bao giờ xuống dưới mức sàn**, không bao giờ áp dụng cho tác vụ quan trọng, và luôn được ghi lại. |
| Một thời điểm duy nhất cho mỗi thao tác | **Giao** | Thời điểm do database cấp ở đầu giao dịch. Giá, số đọc ngân sách và bằng chứng đều đọc tại đúng thời điểm đó. Lớp lỗi "hai đồng hồ" (Wave 5, 6) **bị đóng theo cấu trúc** ở đường duyệt chi. |
| Thiếu giá không bao giờ thành 0 | **Đóng** | Trước đây một model chưa có giá được ước tính 0 đồng và **lọt qua khâu duyệt**. Giờ nó bị từ chối, và mọi báo cáo chi phí đều đánh dấu "chưa rõ" khi có thao tác không rõ chi phí. |
| Số tiền cấu hình không còn bị gắn nhãn "quan sát" | **Đóng** | Trần USD 77.41 và ngân sách kênh trước đây bị hiển thị như số đo được. Giờ chúng mang nhãn **"đã ghi"** (recorded). |
| Duyệt chi đồng thời | **Đóng** | Mỗi lần chỉ một thao tác tính phí cho cả công ty. Request đến khi đang có thao tác khác sẽ **được hoãn, kèm lý do ghi lại**, chứ không chờ rồi hỏng. |
| Năm việc mang sang từ Wave 6 | **Đóng 5** | Duyệt chi qua ranh giới tháng; header resource thứ sáu; khóa cấu hình production/publishing (chỉ ở phạm vi công ty); tính chung cuộc của tầng đã phục vụ; lỗi trùng thời điểm có tên riêng. |
| Phiên bản | **1.4.0** | Một dòng trong `Directory.Build.props`. |
| Test | **690/690 trên PostgreSQL 17** (từ 619), 0 regress; kiến trúc 58 (từ 52) | Reviewer chạy lại độc lập ba lần. Hơn 15 lần "tái tạo lỗi → thấy fail → khôi phục"; test hai phiên giữ khóa 35 giây. |

**Không có gì đăng. Không có gì mua. Không có lệnh gọi tính phí nào.** Store công ty vẫn **0 bảng** (tôi đã kiểm tra chỉ đọc). Bản phát hành RN-2026-0007 có **81 known issues**. Trong 56 issue của Wave 6: 5 đã giải quyết, 6 đã thu hẹp, 45 còn mở.

---

## 3. Việc anh cần quyết

### 3.1 Câu hỏi mới từ Wave 7 — không chặn wave nào, nhưng chặn ngày gọi model tính phí đầu tiên

1. **Cần bao nhiêu quan sát thì xếp hạng theo bằng chứng mới thay thứ tự cấu hình?** Kế hoạch tổng (mục 18) đã ghi **"ít nhất mười lần chạy so sánh được cho mỗi tác vụ"**. **Đề xuất của tôi: lấy con số này.** Tôi chưa áp dụng, vì đó là quyết định của anh.
2. **Trần chi phí công ty tính trên số nào?** Tạm thời là **USD 34.42**, tức phần metered trong phương án O-002 mà anh đã duyệt. Phí cố định USD 42.99 vẫn chưa có chỗ ghi riêng. Nếu tính trên cả USD 77.41 thì phần metered có thể đẩy tổng chi vượt trần.
3. **Kênh chưa có số ngân sách thì có được chỉ dựa vào trần công ty không?** Hiện tại: không, kênh đó bị từ chối việc tính phí.
4. **Mối quan hệ giữa cấp độ tác vụ (L1–L4) và tầng suy luận (Light/Standard/Deep).** Không có bản ghi nào nối hai thứ này, nên tỷ lệ phân tầng giả định **không thể kiểm chứng chỉ bằng quan sát**. Cần anh xác nhận một ánh xạ.
5. **Một thao tác không rõ chi phí có nên chặn mọi việc tính phí đến hết tháng không?** Hiện tại: có. An toàn nhưng cứng.
6. **Nhà cung cấp không có giá cho phần input dùng cache** hiện không bao giờ được chọn cho việc tính phí. Giữ như vậy, hay cho phép khi request không dùng cache?
7. Một request tính phí bị hoãn **không tự được duyệt lại**; nó chỉ chuyển lên xử lý sau thời hạn giữ (tính bằng giờ). Ở khối lượng hiện tại thì chấp nhận được.

### 3.2 Vẫn chờ anh từ Wave 6

Ngân sách tháng của kênh 1, và tổng ngân sách các kênh có bắt buộc nằm trong USD 77.41 không; giá trị cấu hình của kênh 1; chỗ ghi phí cố định; xác nhận hay bác bỏ `CEO-D-400`.

### 3.3 Nợ của anh — chặn lần đăng đầu tiên, chưa cái nào xong

Đăng ký thư viện nhạc/stock; **một** tài khoản thanh toán cho cả công ty; xác minh hai bước; một phiên đăng nhập thư viện để lấy số clip thật. **Mốc cố định duy nhất: chấp nhận điều khoản trước 31/01/2027**, còn khoảng 16 tuần.

---

## 4. Lỗi của tôi tốn kém nhất từ trước đến nay

Ở vòng sửa đầu, tôi phán quyết: "hai lần duyệt chi đồng thời phải **xếp hàng chờ khóa**". Tôi đã không tính rằng lệnh chờ khóa có timeout 30 giây, trong khi người giữ khóa có thể giữ 60 giây trong lúc gọi nhà cung cấp. Reviewer phát hiện đây là lỗi high mới: request thứ hai sẽ hỏng mà không ghi lại lý do. **Cách sửa: không chờ nữa, hoãn ngay và ghi lý do.** Vòng sửa thứ hai này tốn khoảng 290,000 token.

Hai lỗi nhỏ khác của tôi cũng bị agent bắt: một phán quyết ở cổng Scope viết quá rộng, và một điều kiện ở cổng Design ("giới hạn theo timeout của request") không có gì để gắn vào.

**Bài học, lần thứ ba liên tiếp:** lỗi khó nhất nằm ở chỗ thời gian và đồng thời. Lần này nó nằm trong phán quyết của chính tôi, không phải trong code.

---

## 5. Chi phí

| | Wave 6 | Wave 7 |
|---|---|---|
| Token | 2,340,276 | **2,912,242** (+24%) |
| Thời gian agent | 2h 42m | **4h 00m** |
| Tỷ lệ làm lại | 4.4% | **17.0%** |
| Validator từ chối | 0 | **0** |
| Lãng phí hạ tầng | 0 | **0** (container DB đã dừng, bật lại trước khi dispatch) |
| Chi tiêu vận hành | USD 0.00 | **USD 0.00** |

Chi tiết: `wave-7/cost-ledger.md`.

---

## 6. 67 phản đối, 67 đúng

Bảy wave liên tiếp, chưa có phản đối nào của agent sai. Ba trong số 67 là lỗi của tôi (mục 4).

---

## 7. Wave 8 — đã tạo ticket `MC-9`, chưa bắt đầu

Kế hoạch tổng: **quản lý bằng AI**, gồm COO, CTO, CFO agent, báo cáo tuần, khuyến nghị và dashboard CEO. Kế hoạch tổng cũng nói rõ: cho đến khi có nhịp sản xuất thật, **báo cáo tuần được sinh bằng code từ dữ liệu đã ghi**, cộng một lượt L3 cho phần khuyến nghị bằng lời. Ticket MC-9 scope **báo cáo, khuyến nghị và dashboard bằng code**, mỗi con số mang nhãn trường hợp đo của nó. Ticket **không** gọi model cho phần lời khuyến nghị.

**Câu hỏi cho anh trước Wave 8:** anh có muốn duyệt **lượt L3 viết lời khuyến nghị** (gọi model tính phí, mỗi tuần một lần) không? Hiện chưa có dữ liệu thật nào, nên tôi đề xuất **không** cho đến khi có video đầu tiên.

---

## 8. Tệp đính kèm

- `wave-7/release-note.md`: RN-2026-0007, phiên bản **1.4.0**, 81 known issues.
- `wave-7/cost-ledger.md`: sổ chi phí.
- `wave-7/01`–`05-*.md`: artifact của từng phase, kèm 8 ADR.
- `research/ceo-decision-record.md`: `CEO-D-600`.
