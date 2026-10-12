# Báo cáo CEO — Wave 11: Đồ hoạ vẽ bằng code

**Run `run-9d83536ecd16` · ticket MC-12 · 11–12/10/2026 · 6/6 phase, 6/6 gate · phiên bản 1.8.0**

---

## 1. Kết luận trong năm dòng

1. **22 graphic của item 001 giờ được vẽ thật bằng code** thay cho các thẻ chữ: sơ đồ tổ và nguồn mật, điệu nhảy hình số 8, góc θ, đường dao động, hình nón sai số… CEO đã **duyệt cả 22 graphic** cùng bảng màu (CEO-D-1003).
2. Bộ vẽ là **Remotion** (CEO chọn), cùng trình duyệt Chrome Headless Shell và phông Inter, chạy **ngay trên máy công ty**. Remotion dùng gói miễn phí (công ty ≤ 3 người). **Chi phí 0 USD, không gọi model AI nào.**
3. Mỗi graphic **dựng hai lần đều giống hệt từng byte**. Mọi chữ và số trên hình đều được trích nguyên văn từ hồ sơ có nguồn; số không có nguồn thì hệ thống tự từ chối.
4. **Không sản xuất trên store công ty trong wave này** (CEO-D-1005). Item 001 trên store công ty vẫn là phiên bản 3. Wave 12 sẽ sản xuất bằng **hoạt hình 3D**.
5. Nói thẳng: CEO nhận xét video vẫn **khá nhàm chán**. 22 graphic là sơ đồ 2D giải thích, 18 vị trí clip vẫn là placeholder, chưa có nhạc. Vì vậy Wave 12 chuyển sang 3D (CEO-D-1004).

## 2. Những gì đã giao (bằng chứng, không phải lời hứa)

| Hạng mục | Bằng chứng |
|---|---|
| 22 graphic đã duyệt | Contact sheet sha256 `51ce3f5b…0f28`, preview 395,6 giây sha256 `e2553213…66c6`. Hash khung cuối của từng graphic được ghi trong hồ sơ quyết định (CEO-D-1003) |
| Video demo item 001 đủ 22 graphic | 665,8 giây, 1 luồng hình và 1 luồng tiếng, giải mã không lỗi; dựng trên store demo với giọng đọc giả lập |
| Kiểm thử | **1076 test chạy live, đều pass** (Wave 10: 934); kiểm thử kiến trúc 101 (Wave 10: 88) |
| Bộ cài đặt được kiểm soát | 15.117 file (Remotion, trình duyệt, phông, Node) có manifest hash; mỗi lần dựng đều kiểm tra lại từng file, và không lần nào ghi đè lên chúng |
| Kiểm chứng số liệu trên hình | Chỉ chấp nhận trích từ 4 hồ sơ của item (bảng claim, shot list, lời dẫn, hồ sơ quyết định), và chỉ khớp nguyên con số: "5 Hz" cắt từ "13–15 Hz" bị từ chối |
| Mạng | Khi dựng hình chỉ có kết nối nội bộ trong máy (loopback); đã truy được từng tiến trình |
| Lệnh cho CEO | `review-graphics` dựng contact sheet và preview, chỉ chạy được trên store demo. File duyệt dùng để so khớp khi chạy trên store công ty |
| Hướng dẫn | `wave-11/owner-guide-graphics.md` |

## 3. Chất lượng quy trình

- **Ba vòng sửa sau review**, tất cả xoay quanh một lỗi mức cao: bộ kiểm tra số liệu ban đầu còn kẽ hở. 22 graphic đã duyệt không bị ảnh hưởng; đây là lớp chặn cho các graphic sau này. Phần token dành cho sửa lỗi là **15,8%** (Wave 10: 3,0%).
- **64 objection của các agent, tất cả đúng hoặc được chấp nhận**, nối tiếp 11 wave. Có ít nhất 7 lỗi là của chính tôi (orchestrator), ghi chi tiết trong `wave-11/cost-ledger.md`. Ví dụ: lượt chạy thử của tôi để lại cache trong thư mục cài đặt, và manifest ban đầu ghi đường dẫn sai dạng.
- **Một sự cố nhỏ đã xử lý:** trình duyệt từng ghi một file log 2,8 KB vào thư mục cài đặt. Tôi đã chuyển file ra làm bằng chứng và mã đã được sửa. Từ đó thư mục cài đặt khớp manifest qua mọi lượt dựng.

## 4. Chi phí

| | |
|---|---|
| Chi phí theo lượt (metered) | **USD 0,00**; không gọi model nào |
| Trần USD 5,95 của item | tổng đã tính **giữ nguyên USD 0,007695** (ước tính) |
| Tải về (CEO duyệt) | Remotion (~236 MB), Chrome Headless Shell (126 MB), Inter (34 MB); miễn phí |
| Máy tính | chưa đo (khoảng 15–16 phút cho mỗi lần dựng đủ 22 graphic hai lần) |
| Token phát triển | ít nhất 2,38 triệu (con số thấp hơn thực tế vì một phần ngữ cảnh của implementer bị nén) |

## 5. Việc CEO cần quyết

1. **Gộp Wave 11 vào `main`:** tôi đã mở PR và chờ CEO nói "merge".
2. **Wave 12 (MC-13): hoạt hình 3D bằng code.** Ticket và file bàn giao đã sẵn. Việc đầu tiên của Wave 12 là CEO chọn **nhân vật hoặc thế giới riêng** cho kênh (Scope sẽ đưa phương án kèm hình xem trước), rồi duyệt tải các công cụ 3D miễn phí. Wave 12 chạy trong **phiên mới**.

## 6. Rủi ro còn mở (không chặn)

- Máy không có GPU rời; tốc độ dựng 3D chưa đo. Đây là việc đầu tiên Wave 12 phải đo.
- Giọng đọc trong máy (Piper) mỗi lần tạo cho ra file khác nhau, nên graphic động phải canh lại thời gian mỗi lần sản xuất. Graphic tĩnh và khung cuối vẫn so được bằng hash.
- Release note ghi 144 vấn đề đã biết, trong đó 19 vấn đề mới, đều không chặn.
