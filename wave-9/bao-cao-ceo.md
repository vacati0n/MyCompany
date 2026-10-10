# Báo cáo CEO — Wave 9: Năng lực sản xuất video

**Run `run-7ae81c0de400` · ticket MC-10 · 2026-10-09 đến 2026-10-10 · 6/6 phase, 6/6 gate · phiên bản 1.6.0**

---

## 1. Kết luận trong năm dòng

1. Theo quyết định của anh đầu wave, Wave 9 **đổi từ "tự vận hành" sang "sản xuất video"**: phần tự vận hành để sau.
2. Công ty giờ có **một lệnh duy nhất (`produce`) biến gói item 001 thành file video thật**: 1920×1080, 30 fps, dài 11 phút 06 giây, đo từ chính file, giải mã trọn vẹn không lỗi, render ba lần độc lập ra **cùng một file từng byte**.
3. Toàn bộ chạy bằng **provider giả, chi phí USD 0.00**. Chưa có lệnh gọi nhà cung cấp thật nào, chưa đọc key thật nào.
4. **Video thật đầu tiên đã sẵn sàng chạy**, ước tính **USD 0.17** (chỉ phần giọng đọc), trần cứng USD 5.95. Chỉ còn chờ anh đặt key và nói "chạy".
5. Cần nói thẳng: video hiện là **bản khung**. 22 đồ họa đang là thẻ chữ mô tả, 18 clip stock là placeholder, không nhạc. Nó chứng minh pipeline, **chưa phải video đăng được**.

## 2. Những gì đã giao (bằng chứng, không phải lời hứa)

| Hạng mục | Bằng chứng |
|---|---|
| File video demo | 9,185,227 byte, sha256 `396692af…1679`, h264 + aac, 665.77 s, 0 lỗi giải mã; reviewer tự render và đo lại |
| Kiểm thử | **857 test chạy live, đều qua** (Wave 8: 736); kiến trúc 80 (Wave 8: 63) |
| Trần chi phí | Mỗi lệnh có tính phí được **giữ chỗ theo chi phí tối đa trước khi gọi**; lệnh có thể vượt trần USD 5.95 bị từ chối trước; chi phí không rõ thì tính theo mức tối đa (trần "đóng" chứ không "mở") |
| Chống nhầm giả/thật | Chế độ thật **không bao giờ** rơi về provider giả; mọi số liệu từ provider giả gắn nhãn `[demonstration]` trên mọi báo cáo và dashboard |
| An toàn | Chế độ thật từ chối **trước mọi lệnh gọi** nếu thiếu: key, giọng đọc, endpoint https, store công ty, trần; key không bao giờ bị in, ghi log hay lưu |
| Hủy giữa chừng | Ctrl+C khi đang gọi nhà cung cấp vẫn ghi sổ lệnh đó theo chi phí tối đa (có thể đã bị tính tiền) |
| Chặn đăng | Vẫn không có đường upload; thêm chặn mới: stage ở trạng thái Held không thể "sẵn sàng đăng" |
| Hướng dẫn cho anh | `wave-9/owner-guide-metered-run.md` |

## 3. Việc anh cần quyết

### 3.1 Chạy video thật đầu tiên — câu hỏi chính

Để chạy, cần đúng **ba bước**:

1. **Anh tự đặt key** (tôi không được đọc hay nhập key). Mở PowerShell trên máy này, gõ:
   ```powershell
   [Environment]::SetEnvironmentVariable("MEDIACOMPANY_SECRET_OPENAI__GLOBAL", "<key OpenAI của anh>", "User")
   ```
   Chỉ cần key **OpenAI** (dùng cho giọng đọc). Key Anthropic chưa cần cho video này.
2. **Tôi kiểm tra trực tiếp** trên trang của OpenAI: endpoint, định dạng request, giá mỗi ký tự, điều khoản (không lấy quyền trên nội dung của công ty), và giọng `onyx` còn tồn tại.
3. **Tôi cài schema vào store công ty `mediacompany`** (lần đầu tiên có dữ liệu thật), chạy `plan-only` để anh xem kế hoạch và ước tính, rồi chạy `metered` khi anh nói "chạy".

Nếu vượt trần, lệnh dừng (exit code 4), không bao giờ vượt. Sau khi chạy xong, anh có thể xóa key bằng lệnh trong hướng dẫn.

### 3.2 Hướng cho Wave 10

Sau video thật đầu tiên, tôi đề xuất Wave 10 làm **đồ họa thật** cho 22 motion graphic (vẽ bằng code, không tốn phí) để video trở thành xem được. Câu hỏi về thư viện stock và nhạc (USD 42.99/tháng) vẫn để anh quyết sau.

### 3.3 Vẫn còn mở (không chặn)

Chốt giá lúc duyệt chi hay lấy lại giá ngay trước khi dùng; chức năng ghi sổ quyết định trực tiếp; ngân sách riêng cho kênh 1; cách xử lý một khoản giữ chỗ không bao giờ được đối soát (hiện tính theo mức tối đa). Tất cả hiện trên dashboard.

### 3.4 Nợ của anh — chặn lần đăng đầu tiên

Không đổi: đăng ký thư viện nhạc/stock, một tài khoản thanh toán, xác minh hai bước. **Hạn chấp nhận điều khoản: 31/01/2027, còn 113 ngày.**

## 4. Lỗi của tôi

- **Ticket để mơ hồ "store nào cho lần chạy thật"**, và yêu cầu "đồ họa vẽ từ shot list" mâu thuẫn với "không gắn với chủ đề". Agent scope bắt được cả hai.
- **Tôi viết khảo sát rằng bộ điều khiển chi phí "chỉ xét chi phí đã ghi sổ"**. Điều này đúng một phần; agent kiến trúc đã sửa lại.
- **Tôi yêu cầu agent tài liệu ghi tên biến key** vào artifact của framework, trong khi validator cấm tên nhà cung cấp ở đó. Agent đã phản đối đúng; tên biến nằm trong hướng dẫn và báo cáo này.
- **Lượt sửa đầu tiên (do tôi yêu cầu trước review) sửa chưa hết**: báo cáo tuần và dashboard đã được sửa, nhưng lệnh `report` cũ thì chưa. Review bắt được; đây là lần sửa tốn nhất chương trình (khoảng 514 nghìn token).

## 5. Chi phí

| | |
|---|---|
| Chi tiêu có tính phí trong wave | **USD 0.00** |
| Token phát triển | **3,131,782** (Wave 8: 2,517,350) |
| Tỷ lệ làm lại | **18.9%** (Wave 8: 9.6%) — phần lớn là 10 yêu cầu sửa của review |
| Runtime từ chối | **0** (Wave 8: 1) |

Chi tiết: `wave-9/cost-ledger.md`.

## 6. 66 phản đối, 66 đúng

Chín wave liền, mọi phản đối của agent đều đúng. Ít nhất năm phản đối là lỗi của tôi (mục 4).

## 7. Tệp đính kèm

`wave-9/00-ticket-mc-10.md` đến `05-review-package.md`, `release-note.md` (113 vấn đề đã biết), `owner-guide-metered-run.md`, `cost-ledger.md`, và quyết định `CEO-D-800` đến `CEO-D-804` trong `research/ceo-decision-record.md`.

---

## 8. Phụ lục (10/10/2026): lần chạy thật đầu tiên và quyết định mới

**Đã làm theo đồng ý của anh:**
- Kiểm tra trực tiếp trên trang của OpenAI: giá, endpoint, điều khoản, giọng `onyx`. Kết quả ghi vào cấu hình (commit `66a546c`).
- Cài store công ty `mediacompany` lần đầu và nạp 26 dòng cấu hình.
- Chạy `plan-only` để xem trước: ước tính USD 0.166.
- Sao lưu store trước khi chạy thật.

**Lần chạy thật (07:50 UTC):**
- Hệ thống mở item version 2 và vẽ xong 22 hình.
- Request giọng đọc đầu tiên bị OpenAI trả về **HTTP 429**. Hệ thống dừng, không thử lại.
- Lệnh lỗi được ghi theo mức tối đa: **USD 0.0077** (ước tính; chi phí thực có lẽ là 0).
- **Chưa có video thật.**
- Nội dung lỗi OpenAI trả về chưa được lưu, nên chưa biết nguyên nhân. Đây là thiếu sót, sẽ sửa ở Wave 10.

**Quyết định mới của anh (`CEO-D-805`):** không phụ thuộc giọng đọc của OpenAI. Giọng đọc là của công ty, theo cả hai cách:
- Người thật thu âm, pipeline nhập file đã thu.
- Khi chưa có bản thu, dùng một mô hình giọng đọc mã nguồn mở chạy trên máy công ty.

Việc này là **Wave 10 (`MC-11`)**: ticket đã tạo và lên kế hoạch; xem `wave-10/HANDOFF.md`. Giọng `onyx` hết hiệu lực cùng với OpenAI. Biến môi trường chứa key OpenAI không còn cần nữa, anh có thể xóa.
