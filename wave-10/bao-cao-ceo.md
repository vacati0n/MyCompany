# Báo cáo CEO — Wave 10: Giọng đọc của chính công ty

**Run `run-dfcae1756c32` · ticket MC-11 · 2026-10-10 · 6/6 phase, 6/6 gate · phiên bản 1.7.0**

---

## 1. Kết luận trong năm dòng

1. **Video 001 đã tồn tại thật trong sổ sách của công ty** (store `mediacompany`, item version 3). Đây là lần đầu tiên công ty có video với giọng đọc thật.
2. Giọng đọc do **mô hình Piper chạy ngay trên máy công ty** tạo ra, giọng `en_GB-cori-high`: không qua nhà cung cấp nào, không tính phí theo lượt, không có nội dung nào rời khỏi máy. **Chi phí theo lượt (metered) USD 0,00.**
3. Video dài 10 phút 29,7 giây, 1920×1080, giải mã từ đầu đến cuối không lỗi. Mỗi đoạn giọng đọc đều truy được về mô hình, phiên bản, giấy phép, thông số và hash của mọi file đã nạp.
4. **Đường cho bản thu của CEO đã sẵn sàng.** CEO thu 13 file (mỗi beat một file) và ký giấy đồng ý (release), tôi đăng ký vào hệ thống. Lần sản xuất tiếp theo sẽ tự dùng giọng của CEO thay giọng máy, không cần sửa code.
5. Cần nói thẳng: video vẫn là **bản khung về hình ảnh**. 22 đồ họa là thẻ chữ, 18 clip là placeholder, không có nhạc. Video **chưa đăng được** và vẫn giữ ở trạng thái chưa sẵn sàng publish.

## 2. Những gì đã giao (bằng chứng, không phải lời hứa)

| Hạng mục | Bằng chứng |
|---|---|
| Video 001 trên store công ty | `MC3-ITEM-001-v3.mp4`, 9.098.282 byte, sha256 `ce8fe1d4…e42a`, h264 + aac, 629,733 s, 0 lỗi giải mã; tôi tự đo lại sau khi chạy |
| Bản chạy thử trên store demo | Chạy trước bằng mô hình thật: exit 0, 327 s, video 634,233 s, USD 0,00 |
| Trần chi phí USD 5,95 | Tổng đã tính **giữ nguyên USD 0,007695** (lần thử sáng 10/10 với OpenAI). Lần chạy này không ghi thêm khoản phí nào |
| Kiểm thử | **934 test chạy live, đều pass** (Wave 9: 857); kiểm thử kiến trúc 88 (Wave 9: 80) |
| Chặn nhà cung cấp | Chặn ngay trong quy tắc chọn nguồn giọng, theo quyết định của CEO. Không có cấu hình nào mở lại được. Code của vendor vẫn giữ để dùng khi CEO quyết khác |
| Kiểm tra mô hình | Trước mỗi lần chạy, hệ thống tính hash của 13 file cài đặt và 1.832 file được các gói liệt kê, rồi so với giá trị tôi đã đo khi cài. Lệch một byte là từ chối chạy |
| Ghi lỗi của vendor | Khi vendor từ chối (như lỗi 429 sáng 10/10), giờ hệ thống ghi được mã lỗi và lý do, đã lọc bỏ key/token, cắt ở 200 ký tự |
| Hướng dẫn cho CEO | `wave-10/owner-guide-own-voice.md`: cách thu âm, cách nhập giấy đồng ý, các lệnh chính xác |
| Biên bản chạy | `wave-10/company-store-run/record.md` cùng toàn bộ log |

## 3. Số đo giọng đọc

| | Đo được | Kịch bản dự kiến (150 từ/phút) | Chênh lệch |
|---|---|---|---|
| Cả bài (1.929 từ) | **10:29,7** | 12:51,6 | −2:21,9 |
| Loudness | **−15,6 LUFS** | không đặt mục tiêu (CEO-D-900) | — |

Giọng cori đọc nhanh hơn kịch bản giả định, khoảng **184 từ/phút**. Đúng như CEO quyết định, hệ thống chỉ báo cáo chênh lệch và không kéo giãn. Với thông số mặc định mà CEO đã chọn, mỗi lần tạo ra một file khác nhau; hồ sơ ghi rõ "không lặp lại".

## 4. Việc CEO cần quyết

### 4.1 Hướng cho Wave 11 — câu hỏi chính

Tôi đề xuất Wave 11 làm **đồ họa thật bằng code** cho 22 motion graphic (vẽ ngay trên máy, không tốn phí) để video trở thành xem được. Câu hỏi về thư viện stock/nhạc (USD 42,99/tháng) vẫn để CEO quyết sau. Tôi sẽ hỏi CEO ở cuối phiên này trước khi tạo ticket.

### 4.2 Khi CEO muốn thay giọng máy bằng giọng của mình

1. Thu 13 file theo `wave-10/owner-guide-own-voice.md` mục 1. Mỗi beat một file, đặt tên `01.wav` … `13.wav`, các file cùng một thiết lập thu.
2. Ký giấy đồng ý theo mẫu đã duyệt (cấm dùng giọng để huấn luyện mô hình). Giữ giấy này ngoài repo.
3. Báo tôi. Tôi đăng ký bản thu và chạy lại; video mới sẽ là version 4.

### 4.3 Vẫn còn mở (không chặn)

- Ngân sách riêng của kênh 1.
- Nơi ghi khoản phí cố định USD 42,99/tháng.
- Chốt giá lúc duyệt chi hay lấy lại giá ngay trước khi dùng.
- Chức năng ghi sổ quyết định trực tiếp.
- Thư viện Python gốc (`python314.dll`, thư viện chuẩn) và 863 mục không có hash trong gói vẫn chưa được kiểm hash. Đây là rủi ro còn lại đã ghi nhận.

### 4.4 Việc của CEO đang chặn lần đăng đầu tiên

Không đổi: đăng ký thư viện nhạc/stock, một tài khoản thanh toán, xác minh hai bước. **Hạn chấp nhận điều khoản: 31/01/2027, còn 113 ngày.**

## 5. Lỗi của tôi

- **Tự mâu thuẫn trong phán quyết:** Planning Gate đặt các hash kỳ vọng ở ngoài repo, nhưng briefing của tôi lại bảo implementer đưa chúng vào file mẫu trong repo. Implementer và reviewer đều phát hiện. Tôi giữ nguyên vì các hash đã công khai trong hồ sơ quyết định.
- **Bộ hash ban đầu thiếu:** Planning Gate không tính đến việc môi trường Python ảo khởi chạy trình thông dịch gốc nằm ngoài nó. Architect phát hiện, và tôi đã mở rộng bộ hash ở Design Gate.
- **Briefing lệch thời điểm:** phase Planning được yêu cầu chuẩn bị bước cài mô hình, trong khi CEO đã cho cài ngay giữa phase.
- **Định dạng trả lời của briefing khác dispatch prompt.** Lỗi này lặp lại từ Wave 9.
- Ở câu hỏi đầu tiên tôi ghi bài đọc "khoảng 1.500 từ". Đếm lại thì **1.929 từ**; tôi đã đính chính trước khi giao việc cho agent.

**51 phản đối của agent, tất cả đều đúng**, mười wave liền không có phản đối sai.

## 6. Chi phí phát triển

**2,58 triệu token** cho các agent (Wave 9: 3,13 triệu), trong đó sửa lỗi chiếm **3,0%**, thấp nhất chương trình (Wave 9: 18,9%). Bài học của Wave 9 đã được áp dụng: khi agent nói "đã sửa mọi chỗ", tôi bắt liệt kê danh sách. Implementer liệt kê 11 file, reviewer kiểm từng chỗ đọc file, không sót chỗ nào. Chi tiết trong `wave-10/cost-ledger.md`. Chi phí phát triển và chi phí mỗi video là hai đại lượng riêng, không cộng trừ với nhau.
