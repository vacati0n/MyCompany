# MC-17 — Hướng dẫn dùng ứng dụng web "Thí điểm video MediaCompany"

Dành cho: CEO và nhân viên vận hành. Không cần biết lập trình.
Phiên bản: commit ef327e9 (chưa có số phiên bản). Ngày: 2026-10-27.

Ứng dụng này là màn hình tiếng Việt đặt trước bộ công cụ MC-16. Bạn làm bảy bước trên trang; mọi kiểm tra
vẫn do bộ công cụ MC-16 quyết, giống hệt khi chạy bằng dòng lệnh. Chữ trong ngoặc kép ở hướng dẫn này là
đúng chữ hiện trên màn hình.

Đầu trang luôn ghi: "Ứng dụng chạy trên máy này (127.0.0.1). Không gửi gì ra ngoài, không đăng, không mua."

---

## 1. Những việc ứng dụng KHÔNG BAO GIỜ làm

- Không dùng mạng ngoài máy. Ứng dụng chỉ nghe ở địa chỉ 127.0.0.1; máy khác không mở được trang.
- Không tải gì về máy.
- Không dùng giọng máy (TTS), không nhân bản giọng nói. Giọng trong video là bản ghi giọng thật của bạn.
- Không đăng bài, không gửi video đi đâu. Đăng là việc CEO tự làm bằng tay, sau khi duyệt.
- Không tự chuyển tệp bạn tải lên: tệp được giữ nguyên.

---

## 2. Cài đặt lần đầu (làm một lần)

### 2.1 Việc của người phụ trách kỹ thuật

Người phụ trách kỹ thuật dựng (build) kho mã một lần, rồi mở ứng dụng lần đầu. Từ đó trở đi bạn chỉ cần
biểu tượng trên màn hình nền.

Khi ứng dụng chạy, một cửa sổ đen hiện dòng:
"Ứng dụng thí điểm đang chạy ở http://127.0.0.1:47817 (chỉ máy này mở được). Đóng cửa sổ này để tắt ứng dụng."
Trình duyệt tự mở trang. Nếu không, cửa sổ ghi "Không tự mở được trình duyệt. Mở trình duyệt và gõ …";
gõ đúng địa chỉ đó (thường là http://127.0.0.1:47817).

### 2.2 Màn hình "Cài đặt lần đầu"

Trang ghi: "Điền hai mục dưới đây rồi bấm Lưu cài đặt. Các số khác do CEO quyết và không sửa ở đây."

| Mục trên màn hình | Điền gì |
|---|---|
| "Thư mục thí điểm (nơi chứa ảnh, bản ghi, video):" | Đường dẫn đầy đủ, ví dụ C:\Users\ten\MediaCompanyPilot. Để nguyên giá trị có sẵn nếu không có lý do đổi. |
| "Tên CEO (dùng trong dòng người duyệt):" | Họ tên CEO. Không dùng dấu "(", ")" hoặc ";". |
| Ô "Tạo biểu tượng mở ứng dụng trên màn hình nền" | Tích ô này để có biểu tượng mở ứng dụng. |

Phần "Các số CEO đã chốt (chỉ để xem)" chỉ để đọc; bạn không sửa được.

Bấm "Lưu cài đặt". Trang báo "Đã lưu cài đặt vào …".

Chỉ CEO hoặc người phụ trách mới chạy lại cài đặt: đổi tên CEO ở đây sẽ đổi tên trong mọi dòng người duyệt về sau.

### 2.3 Biểu tượng trên màn hình nền

Biểu tượng tên "Thí điểm video MediaCompany". Bấm đúp vào nó:

- nếu ứng dụng chưa chạy: ứng dụng bật lên và mở trang;
- nếu ứng dụng đang chạy: trang được mở lại. Cửa sổ có thể ghi "Ứng dụng đã đang chạy ở … Mở trang đó trong trình duyệt."

Nếu lúc cài đặt chưa tạo biểu tượng, vào "Danh sách video" và bấm "Tạo biểu tượng trên màn hình nền".

Muốn tắt ứng dụng: đóng cửa sổ đen.

---

## 3. Màn hình "Danh sách video"

- Video đã làm hiện trong danh sách; bấm vào để mở lại.
- Video mới: gõ vào "Mã video mới (ví dụ V-01):" rồi bấm "Mở video này".
- Quy tắc mã: "Mã video là tên thư mục của video: chỉ dùng chữ, số, dấu gạch ngang; không dấu cách, không dấu gạch chéo."

Khi mở một video, trên đầu có bảy thẻ bước: "1. Bảng thông tin", "2. Ảnh", "3. Kịch bản", "4. Ghi âm",
"5. Phim người dẫn", "6. Tạo video", "7. Xem và duyệt". Làm lần lượt từ 1 đến 7.

---

## 4. Bảy bước

### Bước 1 — "Bước 1. Bảng thông tin sản phẩm"

Điền các ô đầu trang:

- "Mã video" (đã điền sẵn, không sửa)
- "Tên sản phẩm"
- "Chủ thể (danh từ gọi sản phẩm)"
- "Phân loại"
- "Đường dẫn trang sản phẩm"
- "Tên gian hàng và Mall hay không"
- "Ngày đọc trang" (năm-tháng-ngày, ví dụ 2026-10-27)
- "Ngách"
- "Ảnh dùng trong video" (tên tệp, cách nhau bằng dấu phẩy, ví dụ anh-1.jpg, anh-2.jpg)
- "Video thứ mấy của sản phẩm"
- "Trọng tâm của video"

Ô "Câu kêu gọi cuối video:" là câu kết.

Phần "Dòng thông tin (1 đến 3 dòng)": bấm "Thêm dòng" để thêm (tối đa 3: "Bảng thông tin có tối đa 3 dòng."),
"Bỏ dòng này" để xoá. Mỗi dòng có: chủ thể, thuộc tính, giá trị chép đúng trên trang, mục nguồn trên trang,
tên tệp ảnh chụp màn hình, và "câu thay thế (để trống nếu không cần)". Ở "Chọn ảnh chụp màn hình:" chọn ảnh chụp
trang sản phẩm; tên tệp ảnh phải đúng tên bạn ghi trong dòng.

Khi gõ, trang cảnh báo ngay nếu có từ cấm, dạng: "…: có từ “…” (nhóm …). Bỏ hoặc thay từ này."
Lưu ý: cảnh báo ngay này chỉ xem các ô "Chủ thể", "Phân loại" và chủ thể, thuộc tính, giá trị, câu thay thế
của từng dòng. Các ô khác không được cảnh báo khi gõ, nhưng bộ công cụ vẫn kiểm khi bạn lưu và khi tạo video.

Bấm "Lưu bảng thông tin".

### Bước 2 — "Bước 2. Ảnh sản phẩm"

"Tải lên đúng các ảnh đã ghi ở bước 1. Mỗi ảnh một nút."
Mỗi ảnh hiện " — chưa có. Chọn ảnh: " hoặc " — đã có. Chọn lại nếu muốn thay: ". Chọn đúng tệp cho từng tên.

### Bước 3 — "Bước 3. Kịch bản"

Bấm "Viết kịch bản". Kịch bản do code viết từ bảng thông tin, không dùng mô hình tạo sinh. Mỗi câu có dòng
"Nguồn: …" chỉ câu đó lấy từ đâu. Đối chiếu từng câu với nguồn rồi tích. Khi đủ, trang ghi
"Đã tích … trên … câu. Đủ; sang bước 4 để ghi âm."

### Bước 4 — "Bước 4. Ghi âm giọng đọc"

1. Bấm "Bắt đầu ghi". Lần đầu, trình duyệt hỏi quyền dùng micrô: chọn Cho phép.
2. Trang ghi "Đang ghi... Đọc kịch bản. Ứng dụng tự dừng ở 60 giây." Đồng hồ chạy từ "0,0 giây".
3. Đọc kịch bản bằng giọng thật, rồi bấm "Dừng". Nếu quá 60 giây, trang ghi "Đã tự dừng ở 60 giây."
   và phần sau 60 giây bị cắt mất — hãy bấm "Dừng" trước 60 giây.
4. Nghe lại. Được thì bấm "Dùng bản ghi này". Ứng dụng nhờ bộ công cụ kiểm ngay (độ dài, bản ghi đã dùng
   cho video khác chưa). Đạt thì trang ghi "Đã dùng bản ghi này." và dòng "Bản ghi đang dùng: …".

Cách khác: mở "Hoặc tải lên bản ghi có sẵn". "Dạng được nhận: .wav .mp3 .m4a .aac .flac .ogg, không quá 60 giây.
Tệp được giữ nguyên, không chuyển."

Ghi lại hoặc tải bản ghi khác sẽ xoá thời gian phụ đề đã chỉnh ở bước 6 của video này.

### Bước 5 — "Bước 5. Đoạn phim người dẫn"

"Tải lên đoạn phim quay dọc, dài bằng bản ghi giọng (lệch không quá 0,5 giây)."
Dạng nhận: .mp4 .mov .m4v .mkv .webm. Đạt thì trang ghi "Đoạn phim đang dùng: …".

### Bước 6 — "Bước 6. Tạo video"

"Bấm Tạo video. Việc này mất vài phút; giữ trang mở." Bấm "Tạo video".
Trong lúc chạy, trang ghi "Đang tạo video... đã … giây. Giữ trang mở." (chỉ hiện thời gian đã chạy, không
hiện còn bao lâu). Xong thì có "Gói: …" và "Bước tiếp: sang bước 7 để xem video và duyệt."

Mỗi lần chỉ tạo một video. Phần "Chỉnh thời gian từng câu" (cột "Câu", "Nội dung", "Bắt đầu (giây)",
"Kết thúc (giây)") dùng khi bộ công cụ báo không tìm được chỗ ngừng hoặc một trang phụ đề hiện quá ngắn
(dưới 1,2 giây): sửa số, bấm "Lưu thời gian", rồi bấm "Tạo video" lại.

### Bước 7 — "Bước 7. Xem video, kiểm danh sách, duyệt"

1. Xem "Trạng thái gói:" và xem hết video.
2. "Danh sách kiểm (mẫu 04)": cột "Đã kiểm", "Mục", "Nội dung", "Ai kiểm". Tự kiểm và tích từng mục.
   B8 do bộ công cụ quyết ("bộ công cụ"). Trang ghi "Đã tích … trên 26 mục. Tích đủ 26 mục mới duyệt được."
3. Phần "Duyệt bằng mã băm": dòng "Mã SHA-256 của video đang xem:" là mã của đúng video bạn vừa xem.
4. "Ai bấm duyệt?" — chọn một (xem mục 6).
5. Đọc dòng "Dòng người duyệt sẽ ghi:" rồi bấm "Duyệt video này".
6. Trang ghi "Video này đã được duyệt bởi … ngày …" và "Tệp video đã duyệt: …" (đường dẫn tệp video).
7. Ngay trước khi đăng, bấm "Kiểm lại video". Dòng "Kiểm lại: …" phải đạt và mã phải đúng mã đã duyệt.

---

## 5. Đọc kết quả trên màn hình

Mỗi lần bấm, trang hiện một khung có tiêu đề, dòng của bộ công cụ, "Lý do: …" và "Việc cần làm: …".

| Tiêu đề | Nghĩa | Làm gì |
|---|---|---|
| "Đạt" | Bộ công cụ đã kiểm và đồng ý | Sang bước tiếp |
| "Giữ lại" | Gói đã tạo nhưng có kiểm tra không đạt; không duyệt được | Đọc các dòng "KHÔNG ĐẠT", sửa đầu vào, tạo video lại |
| "Từ chối" | Đầu vào chưa đúng | Làm theo "Việc cần làm" (bảng mục 7) |
| "Lỗi công cụ" | ffmpeg hoặc ffprobe không chạy được | "Báo người phụ trách kỹ thuật, kèm nguyên dòng trên." |
| "Lỗi ứng dụng" | Lỗi của ứng dụng, không phải của bạn | "Báo người phụ trách kỹ thuật, kèm nguyên dòng trên." |

---

## 6. Duyệt: ai được bấm

- "CEO tự bấm": CEO tự xem và bấm. Dòng người duyệt ghi: [tên CEO] (CEO).
- "Nhân viên bấm theo chỉ thị bằng văn bản của CEO": chỉ khi CEO đã giao bằng văn bản (tin nhắn, email).
  Gõ tên vào "Tên nhân viên:". Dòng người duyệt ghi:
  [tên CEO] (CEO; nhân viên [tên nhân viên] bấm duyệt theo chỉ thị bằng văn bản của CEO).

Nhân viên KHÔNG chọn "CEO tự bấm". Ứng dụng không kiểm được chỉ thị bằng văn bản, nên CEO đọc dòng người
duyệt của mỗi lần duyệt.

Lần duyệt gắn với mã SHA-256. Video đổi dù một byte thì lần duyệt mất hiệu lực. Ứng dụng không đăng; CEO đăng
bằng tay đúng tệp ở dòng "Tệp video đã duyệt: …".

---

## 7. Khi bị từ chối: mã, nghĩa, cách làm

Mã nằm trong ngoặc vuông, ví dụ "TỪ CHỐI [clip-not-portrait]". Chữ trong cột "Cách làm" là đúng chữ trên màn hình.

### 7.1 Nội dung và bản ghi

| Mã | Nghĩa | Cách làm |
|---|---|---|
| banned-term | "Nội dung có từ bị cấm." | "Bỏ dòng đó, hoặc viết lại không có từ bị cấm. Không ghi giá, khuyến mãi, số sao, số đã bán, từ tuyệt đối, lời về sức khỏe, lời "đã dùng"." |
| override-first-person | "Câu thay thế xưng ngôi thứ nhất, hoặc nói đã dùng hay nhận xét." | "Viết lại câu thay thế chỉ nói về sản phẩm, hoặc bỏ câu thay thế." |
| recording-too-long | "Bản ghi dài quá 60 giây CEO cho phép." | "Ghi lại ngắn hơn 60 giây." |
| recording-reused | "Bản ghi này đã dùng cho một video khác." | "Ghi một bản mới cho video này." |
| clip-not-portrait | "Đoạn phim người dẫn không phải khung dọc." | "Xuất lại đoạn phim ở khung dọc 9:16." |
| clip-length-mismatch | "Đoạn phim người dẫn dài khác bản ghi quá mức cho phép." | "Tạo lại đoạn phim từ đúng bản ghi đang dùng." |
| caption-page-too-short | "Một trang phụ đề sẽ hiện ngắn hơn thời gian tối thiểu CEO đặt." | "Ghi lại, nói câu đó chậm hơn; hoặc sửa thời gian từng câu trong ô sửa thời gian ở bước 6." |
| settings-looser-than-ceo | "Tệp cài đặt có một số nới lỏng quyết định của CEO." | "…mở màn hình cài đặt và bấm lưu để ghi lại các số của CEO." |

### 7.2 Sổ theo dõi, gói và lần duyệt — DỪNG, không đăng

| Mã | Nghĩa | Cách làm |
|---|---|---|
| ledger-truncated | "Sổ theo dõi thí điểm bị xoá hoặc bị cắt bớt." | "Dừng. Không tự tạo sổ mới; báo người phụ trách để khôi phục sổ." |
| ledger-entry-missing | "Sổ theo dõi thí điểm mất một mục." | "Dừng. Báo người phụ trách để khôi phục sổ." |
| package-file-not-plain | "Tệp kê của gói trỏ ra ngoài thư mục gói." | "Gói đã bị sửa. Không duyệt, không đăng; tạo video lại ở bước 6." |
| package-ledger-mismatch | "Gói và sổ theo dõi không khớp." | "…Không duyệt, không đăng; duyệt đúng gói vừa tạo ở bước 6." |
| approval-hash-mismatch | "Mã SHA-256 của video khác giá trị được duyệt." | "Video đã đổi sau khi tạo. Không duyệt; tạo video lại ở bước 6 và duyệt video mới." |
| approval-void | "Video đã đổi sau khi duyệt; lần duyệt cũ mất hiệu lực." | "Không đăng. Tạo video lại ở bước 6, xem lại và duyệt video mới." |

### 7.3 Thiếu bước, đang bận

| Mã | Nghĩa | Cách làm |
|---|---|---|
| facts-needed | Chưa lưu bảng thông tin | Làm bước 1 trước |
| images-missing | Còn thiếu ảnh (trang nêu tên) | Tải các ảnh đó lên ở bước 2 |
| image-not-declared | Ảnh không có trong danh sách ảnh của bước 1 | Chỉ tải đúng các ảnh đã ghi ở bước 1, hoặc sửa danh sách ở bước 1 |
| image-type-differs | Đuôi tệp ảnh khác tên ghi ở bước 1 | Chọn ảnh đúng đuôi, hoặc sửa tên ảnh ở bước 1 |
| script-needed | Chưa có kịch bản | Bấm "Viết kịch bản" ở bước 3 |
| script-not-ticked | Chưa tích đủ các câu ở bước 3 | Đối chiếu và tích đủ từng câu ở bước 3 trước khi ghi âm |
| script-changed | Kịch bản đã viết lại sau khi trang mở | Tải lại trang rồi tích lại từng dòng |
| take-empty | Bản ghi vừa thu không có tiếng | Kiểm tra micrô rồi ghi lại |
| take-needed | Chưa thu bản ghi nào | Bấm "Bắt đầu ghi" trước |
| recording-needed | Video chưa có bản ghi giọng | Làm bước 4 trước |
| clip-needed | Video chưa có đoạn phim người dẫn | Làm bước 5 trước |
| package-needed | Chưa có gói video | Làm bước 6 trước |
| checklist-incomplete | Chưa tích đủ 26 mục (trang nêu mục còn thiếu) | Kiểm và tích đủ rồi bấm duyệt; thiếu một mục thì không đăng |
| pressed-by-missing | Chưa chọn ai bấm duyệt | Chọn CEO, hoặc nhân viên theo chỉ thị bằng văn bản của CEO |
| name-invalid | Tên có dấu "(", ")" hoặc ";" | Gõ lại tên không có các dấu đó |
| produce-busy | Đang tạo một video khác | Mỗi lần chỉ tạo một video; chờ xong rồi bấm lại |
| ledger-busy | Bước 4 phải chờ vì đang tạo video | Chờ tạo video xong rồi bấm lại; bản ghi vừa thu vẫn được giữ |
| recording-check-busy | Bước 6 phải chờ vì bước 4 đang kiểm bản ghi | Chờ bước 4 báo xong rồi bấm "Tạo video" lại |
| upload-too-large | Tệp lớn quá mức cho bước này | Chọn đúng tệp, hoặc xuất lại tệp nhỏ hơn |

### 7.4 Cài đặt và bộ công cụ — báo người phụ trách kỹ thuật

| Mã | Nghĩa | Cách làm |
|---|---|---|
| setup-needed | Ứng dụng chưa được cài đặt | Mở màn hình cài đặt, điền thư mục thí điểm và tên CEO, rồi bấm lưu |
| setup-pilot-folder | Thư mục thí điểm không phải đường dẫn đầy đủ | Gõ lại, ví dụ C:\Users\ten\MediaCompanyPilot |
| setup-tool-missing, setup-tool-digest, setup-face-missing, setup-toolchain-missing, setup-example-missing, setup-repository-missing | Thiếu hoặc sai công cụ, phông chữ, bộ công cụ hoặc tệp cài đặt mẫu | Báo người phụ trách kỹ thuật |
| toolchain-changed | Bộ công cụ khác bản đã ghi lúc cài đặt | Không chạy bản lạ. Nếu người phụ trách vừa cập nhật, mở màn hình cài đặt và lưu lại |
| toolchain-missing | Không thấy bộ công cụ | Mở màn hình cài đặt và lưu lại; nếu vẫn thiếu, báo người phụ trách kỹ thuật |
| shortcut-failed | Không tạo được biểu tượng | Cài đặt không bị ảnh hưởng. Mở ứng dụng bằng tệp chạy mà thông báo chỉ ra, hoặc thử lại bằng nút tạo biểu tượng |

---

## 8. Xử lý sự cố

### 8.1 Không ghi âm được (micrô)

- Trang ghi "Không mở được micrô (…). Cho phép trang dùng micrô (biểu tượng ổ khóa cạnh địa chỉ), kiểm tra micrô
  đã cắm, rồi bấm lại." Bấm biểu tượng ổ khóa cạnh địa chỉ, cho phép Micrô, tải lại trang, bấm "Bắt đầu ghi".
- Trang ghi "Trình duyệt này không ghi âm được. Dùng Chrome hoặc Edge, hoặc tải bản ghi có sẵn lên ở phần dưới."
  Mở địa chỉ http://127.0.0.1:47817 bằng Chrome hoặc Edge, hoặc dùng "Hoặc tải lên bản ghi có sẵn".
- Micrô thật trên máy của bạn chưa được thử trước khi giao. Nếu lần đầu không ghi được, dùng cách tải bản ghi lên
  và báo người phụ trách kỹ thuật.

### 8.2 Cổng đang bị dùng

Cửa sổ đen ghi "LỖI ỨNG DỤNG [port-busy]: cổng … đang bị chương trình khác dùng (…). Đóng chương trình đó, hoặc
chạy lại với --port <số khác>." rồi tắt.

1. Có thể ứng dụng đã mở sẵn trong cửa sổ khác: tìm cửa sổ đen đó, hoặc bấm đúp biểu tượng để mở lại trang.
2. Nếu không phải, đóng chương trình đang dùng cổng đó, rồi bấm đúp biểu tượng lại.
3. Nếu vẫn lỗi, báo người phụ trách kỹ thuật để chạy với cổng khác. Lưu ý: biểu tượng trên màn hình nền luôn mở cổng mặc định 47817; khi chạy với cổng khác, người phụ trách kỹ thuật phải mở ứng dụng bằng dòng lệnh có --port, biểu tượng không tự nhớ cổng mới.

### 8.3 Gõ "localhost" bị từ chối

Ứng dụng chỉ nhận địa chỉ 127.0.0.1. Mở bằng "localhost" sẽ bị từ chối với thông báo "Yêu cầu gửi tới tên máy
"localhost", không phải địa chỉ của ứng dụng (127.0.0.1:…). Mở ứng dụng bằng biểu tượng trên màn hình nền."
Gõ http://127.0.0.1:47817, hoặc bấm đúp biểu tượng.

### 8.4 Trang báo đã cũ hoặc không gọi được ứng dụng

- "Trang này đã cũ (ứng dụng vừa khởi động lại)… Tải lại trang (phím F5) rồi làm lại." — bấm F5.
- "Ứng dụng đang khởi động. Chờ vài giây rồi tải lại trang." — chờ rồi bấm F5.
- "Không gọi được ứng dụng. Có thể cửa sổ ứng dụng đã bị đóng. Mở lại ứng dụng bằng biểu tượng trên màn hình nền
  rồi tải lại trang."

### 8.5 Khi nào DỪNG hẳn

Dừng, không đăng, và báo người phụ trách khi: có mã ở mục 7.2; "Kiểm lại video" báo mã khác mã đã duyệt; hoặc
"Lỗi ứng dụng" lặp lại. Không xoá, không sửa gì trong thư mục thí điểm.

---

## 9. Chưa có trong ứng dụng này

Đọc trang sản phẩm Shopee từ đường dẫn chưa có; đó là việc của lần sau (MC-18). Hiện tại bạn tự đọc trang và
điền bảng thông tin ở bước 1.
