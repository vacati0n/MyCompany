# Hướng dẫn vận hành công cụ tạo video thí điểm (MC-16)

Tài liệu này dành cho người vận hành: CEO, hoặc nhân viên vận hành công cụ. Công cụ ghép một video dọc 1080x1920 từ bảng thông tin,
ảnh sản phẩm, bản ghi giọng thật của bạn và đoạn phim người dẫn. Công cụ không tạo giọng, không nhân bản giọng, không nhận dạng
giọng nói, không mở mạng và không đăng gì lên đâu. Đăng bài vẫn do người làm bằng tay, theo danh sách duyệt (kit 04).

> **Cách dễ hơn (MC-17):** nếu bạn không quen dòng lệnh, dùng ứng dụng web trên máy này, đi từng bước và ghi âm ngay trong
> trình duyệt. Xem `pilot-affiliate-vn/mc-17/01-huong-dan-ung-dung-web.md`. Tài liệu dưới đây vẫn đúng cho cách chạy bằng dòng lệnh.

Mọi lệnh và mọi kết quả trong tài liệu này lấy từ lần chạy mẫu ngày 2026-10-27 (thư mục `C:/Users/vuhoangcao/MediaCompanyRun/pilot-sample/`,
bản ghi đầy đủ ở `sample-transcript.txt` trong thư mục đó).

---

## 1. Cài đặt một lần

1. Chép tệp ví dụ `pilot-affiliate-vn/mc-16/vi-du-cai-dat.json` ra ngoài kho mã, ví dụ `C:/Users/<tên>/MediaCompanyRun/cai-dat.json`.
2. Sửa các đường dẫn cho đúng máy của bạn:
   - `repositoryRoot`: thư mục kho mã.
   - `pilotFolder`: thư mục chứa sổ theo dõi và các gói video. **Phải nằm ngoài kho mã.** Công cụ từ chối nếu thư mục này nằm trong kho mã.
   - `mediaTool`, `probeTool`: đường dẫn ffmpeg và ffprobe, kèm mã SHA-256 của từng tệp. Công cụ kiểm mã này trước mỗi lần chạy.
   - `face`: phông chữ Inter SemiBold, tệp kê mã băm và dòng kê của phông.
3. Không nới các số CEO đã duyệt ở mục 1308 và 1309 của sổ quyết định: vùng an toàn (`safeArea`: trái 60, phải 180, trên 200,
   dưới 480), cỡ chữ phụ đề (56), cỡ chữ lớp nhãn (40), độ lệch cho phép của đoạn phim (`clipToleranceSeconds`: 0,5 giây) và thời gian
   tối thiểu mỗi trang phụ đề (`captionPageMinimumSeconds`: 1,2 giây). Công cụ từ chối tệp cài đặt nới lỏng bất kỳ số nào trong đó
   (mã `settings-looser-than-ceo`). Đặt chặt hơn thì được: lề rộng hơn, chữ to hơn, độ lệch nhỏ hơn, trang hiện lâu hơn.
4. Dựng công cụ một lần: `dotnet build src/MediaCompany.Pilot` (trong thư mục kho mã). Tệp chạy là
   `src/MediaCompany.Pilot/bin/Debug/net10.0/MediaCompany.Pilot.exe`. Dưới đây viết tắt là `MediaCompany.Pilot`.

## 2. Thư mục đầu vào của một video

Mỗi video có một thư mục riêng, ví dụ `input-V-01/`. Trong thư mục đó có:

| Tệp | Ghi chú |
|---|---|
| `fact-sheet.json` | bảng thông tin (mục 3) |
| ảnh sản phẩm | đúng các tên ghi ở `imagesUsed` (3 ảnh) |
| ảnh chụp màn hình | một ảnh cho mỗi dòng thông tin, tên ghi ở cột `screenshot` |
| một bản ghi giọng | `.wav`, `.mp3`, `.m4a`, `.aac`, `.flac` hoặc `.ogg`; chỉ một tệp |
| một đoạn phim người dẫn | `.mp4`, `.mov`, `.m4v`, `.mkv` hoặc `.webm`; khung dọc; chỉ một tệp |
| `timing.txt` | chỉ khi cần (mục 6) |

Tên tệp chỉ là tên, không có đường dẫn.

## 3. Bảng thông tin

Ví dụ đầy đủ: `pilot-affiliate-vn/mc-16/vi-du-bang-thong-tin.json`. Các trường:

| Trường | Ý nghĩa |
|---|---|
| `videoCode` | mã video, V-01 đến V-12 |
| `productName` | tên sản phẩm đúng như trên trang |
| `subjectNoun` | danh từ gọi sản phẩm trong câu nói, ví dụ "bình giữ nhiệt" |
| `classification` | phân loại đúng với thẻ sẽ gắn, ví dụ "loại 500 ml màu xanh" |
| `pagePath` | đường dẫn trang sản phẩm |
| `shopNameAndMall` | tên gian hàng; Mall hay không |
| `readDate` | ngày đọc trang |
| `niche` | ngách |
| `imagesUsed` | 3 tên tệp ảnh |
| `productVideoNumber` | video thứ mấy của sản phẩm này (1 hoặc 2) |
| `videoFocus` | trọng tâm, khác với video kia của cùng sản phẩm |
| `callToAction` | câu kêu gọi số 1 hoặc số 2 (kit 03, Phần B dòng 6); bỏ trống là số 1 |
| `facts` | các dòng thông tin, mỗi dòng có `label`, `subjectNoun`, `attribute`, `value` (chép đúng chữ trên trang), `sourceSection`, `screenshot` |

Một dòng thông tin trong ví dụ:

```json
{
  "label": "F1",
  "subjectNoun": "bình giữ nhiệt",
  "attribute": "dung tích",
  "value": "500 ml",
  "sourceSection": "Chi tiết sản phẩm: Dung tích",
  "screenshot": "chup-f1.png"
}
```

Câu kêu gọi không có giá tiền. Không ghi giá, khuyến mãi, số sao hay số đã bán vào bảng thông tin; công cụ từ chối (mục 7). Công cụ
nhận các cách viết giá thường gặp: "99.000đ", "99.000 VNĐ", "99k", "99 nghìn", "1 triệu", "2 tr", "$5", "5 usd". "1 đồng hồ" là
đồng hồ, không phải giá, nên không bị từ chối.

Một dòng có thể có thêm `override`: câu thay thế do bạn viết, phải chứa nguyên giá trị. Câu thay thế chỉ nói về sản phẩm: không xưng
"tôi", "mình", "em", "chúng tôi", "chúng mình", "tụi mình", "bọn mình", "tớ", "anh", "chị", và không nói đã dùng hay nhận xét. Ví dụ
"Mình dùng mỗi sáng" hay "Tôi đã dùng" bị từ chối (mã `override-first-person`).

## 4. Năm lệnh

Chạy theo thứ tự này cho mỗi video. Mỗi lệnh in một dòng bắt đầu bằng `ĐẠT [tên-lệnh]` khi thành công, hoặc `TỪ CHỐI [mã]` kèm lý do
và cách sửa bằng tiếng Việt.

1. **Kiểm bảng thông tin**

   ```
   MediaCompany.Pilot check --settings C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\settings.json --facts C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\input-V-01\fact-sheet.json
   ```

   Kết quả mẫu: `ĐẠT [check]: bảng thông tin V-01 hợp lệ: 1 dòng thông tin, 3 ảnh; sổ theo dõi có 0 video, chuỗi băm hợp lệ.`

2. **Soạn kịch bản**

   ```
   MediaCompany.Pilot script --settings C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\settings.json --facts C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\input-V-01\fact-sheet.json
   ```

   Công cụ viết `script.json` và `script.md` vào thư mục đầu vào và in từng câu. Đọc `script.md`, kiểm từng câu với bảng nguồn.
   Kịch bản mẫu có 5 câu: câu công bố, câu giới thiệu, một câu cho mỗi dòng thông tin, câu kêu gọi, câu công bố.

3. **Ghi âm** theo mục 5, tạo đoạn phim người dẫn từ đúng bản ghi đó, đặt cả hai vào thư mục đầu vào.

4. **Tạo video**

   ```
   MediaCompany.Pilot produce --settings C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\settings.json --facts C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\input-V-01\fact-sheet.json
   ```

   Kết quả mẫu: `ĐẠT [produce]: gói V-01 ở C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\pilot\runs\V-01-20261026T175930707Z`, rồi
   đường dẫn video, `SHA-256 video: c13e81d43bc6cbf0ba5d94712b5cf90e2e467312b829d04cb02801b75abd3c29` và từng mục kiểm (`đạt ...`).
   Gói có `video.mp4`, `frame.png` (một khung hình lấy giữa trang đầu của câu công bố, để xem nhanh), `checklist.md`, `manifest.json`,
   thư mục `inputs` (bản chép của đúng các tệp đầu vào đã kiểm; video được ghép từ các bản chép này) và các tệp ghi lại lần chạy.
   Nếu dòng đầu bắt đầu bằng `GIỮ LẠI`, gói chưa sẵn sàng: xem mục `KHÔNG ĐẠT` trong danh sách, sửa, chạy lại; không duyệt gói đó.

5. **Duyệt theo mã băm.** Xem video. Đi hết `checklist.md` (các mục kit 04). Công cụ chỉ đánh dấu sẵn B8 (lớp nhãn có từ đầu đến
   cuối video). Ở B3 và B5 công cụ ghi rằng nó không thấy từ bị cấm, nhưng không đánh dấu: danh sách từ của công cụ có hạn, nên bạn
   tự nghe lại và tự tích B3, B5. Mọi mục khác do bạn kiểm. Khi đồng ý, chép mã SHA-256 video vào lệnh:

   ```
   MediaCompany.Pilot approve --package C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\pilot\runs\V-01-20261026T175930707Z --name "Người duyệt mẫu" --sha256 c13e81d43bc6cbf0ba5d94712b5cf90e2e467312b829d04cb02801b75abd3c29
   ```

   Kết quả mẫu: `ĐẠT [approve]: Người duyệt mẫu duyệt video V-01 ngày 2026-10-27, SHA-256 c13e81d4...`

6. **Kiểm lại ngay trước khi đăng**

   ```
   MediaCompany.Pilot verify --package C:\Users\vuhoangcao\MediaCompanyRun\pilot-sample\pilot\runs\V-01-20261026T175930707Z
   ```

   Kết quả mẫu: `ĐẠT [verify]: video V-01 vẫn có SHA-256 c13e81d4...; trạng thái gói ready; đã duyệt bởi Người duyệt mẫu ngày 2026-10-27.`
   Chỉ đăng đúng tệp `video.mp4` vừa được kiểm. Lệnh `approve` và `verify` còn đối chiếu gói với sổ theo dõi: gói phải nằm trong thư mục
   `runs` của thư mục thí điểm, có bản sao mục sổ của chính nó, và mục đó phải có trong sổ. Không chép, đổi tên hay sửa gói trước khi đăng.

## 5. Cách ghi âm

- Giọng thật của chính bạn (CEO hoặc nhân viên vận hành). Không dùng giọng máy, không nhân bản giọng.
- **Mỗi video một bản ghi mới.** Không dùng lại tệp của video khác, kể cả khi nội dung giống nhau; công cụ nhận ra tệp trùng.
- Đọc đúng từng câu trong `script.md`, theo thứ tự, không thêm bớt chữ.
- **Ngừng rõ giữa hai câu** (khoảng nửa giây im lặng). Công cụ tìm các khoảng lặng này để đặt phụ đề cho từng câu. Không để im lặng
  dài ở đầu hay cuối bản ghi.
- **Nói chậm, rõ.** Câu dài (câu công bố luôn dài) được chia thành các trang phụ đề hai dòng, hiện nối tiếp trong lúc bạn nói câu
  đó. Mỗi trang phải hiện ít nhất 1,2 giây (mục 1309 của sổ quyết định), nên câu công bố cần ít nhất 2,4 giây. Nếu nói nhanh hơn, công
  cụ từ chối và nêu số câu (mã `caption-page-too-short`); ghi lại và nói câu đó chậm hơn.
- **Bản ghi dài tối đa 60 giây.**
- Ghi ở chỗ yên tĩnh. Tạo đoạn phim người dẫn từ đúng bản ghi này; đoạn phim phải dọc và dài bằng bản ghi (lệch tối đa 0,5 giây).

## 6. Khi công cụ không tìm được chỗ ngừng: tệp thời gian

Nếu các câu đọc liền nhau, công cụ dừng trước khi ghép phụ đề. Cách tốt nhất là ghi lại, ngừng rõ hơn giữa các câu. Nếu không ghi lại
được, đặt tệp `timing.txt` vào thư mục đầu vào: mỗi câu một dòng, theo thứ tự kịch bản, mỗi dòng là giây bắt đầu và giây kết thúc,
dùng dấu chấm thập phân. Ví dụ cho kịch bản 5 câu:

```
0.0 3.0
3.6 6.6
7.2 10.2
10.8 13.8
14.4 17.4
```

Các khoảng không được chồng lên nhau và không được vượt quá độ dài bản ghi. Mỗi dòng là đúng hai số; chữ, `NaN` hay `Infinity` bị từ
chối. Khi tệp sai, công cụ nêu câu đầu tiên bị sai (kể cả câu không có dòng, hoặc dòng thừa sau câu cuối). Khi có tệp này, công cụ dùng nó thay cho việc tìm chỗ ngừng
và ghi điều đó vào gói.

## 7. Khi công cụ từ chối

Mỗi lần từ chối, công cụ in `TỪ CHỐI [mã]: lý do và cách sửa` và không tạo video. Với lệnh `produce`, nó cũng lưu một bản ghi vào thư mục
`refused` trong thư mục thí điểm. Các trường hợp thường gặp; dòng nào có trong lần chạy mẫu thì ví dụ lấy từ lần chạy đó, dòng ghi
"(bài kiểm)" lấy từ bài kiểm tự động:

| Mã | Ví dụ | Cách sửa |
|---|---|---|
| `banned-term` | `Dòng F1 (giá trị): có từ bị cấm "nhất" (nhóm b: từ tuyệt đối).` | Bỏ dòng đó, hoặc viết lại không có từ bị cấm. Không ghi giá, khuyến mãi, số sao, số đã bán, từ tuyệt đối, lời về sức khỏe, lời "đã dùng". |
| `override-first-person` | (bài kiểm) `Dòng F1: câu thay thế có "mình".` | Viết lại câu thay thế chỉ nói về sản phẩm, hoặc bỏ câu thay thế (mục 3). |
| `recording-too-long` | `Bản ghi recording.wav dài 61.000 giây, quá 60.0 giây CEO cho phép.` | Ghi lại ngắn hơn 60 giây. |
| `recording-reused` | `Bản ghi này (SHA-256 0259b595...) đã dùng cho video V-01.` | Ghi một bản mới cho video này. |
| `clip-not-portrait` | `Đoạn phim người dẫn presenter.mp4 là 960x540, không phải khung dọc.` | Xuất lại đoạn phim ở khung dọc 9:16. |
| `clip-length-mismatch` | `Đoạn phim người dẫn dài 16.400 giây, bản ghi dài 17.400 giây: lệch 1.000 giây, quá 0.5 giây cho phép.` | Tạo lại đoạn phim từ đúng bản ghi đang dùng. |
| `caption-page-too-short` | `Câu 1 cần 2 trang phụ đề, mỗi trang ít nhất 1.2 giây, tức 2.4 giây; khoảng nói của câu chỉ có 1.50 giây.` | Ghi lại, nói câu đó chậm hơn (mục 5); hoặc sửa `timing.txt` nếu đang dùng (mục 6). |
| `settings-looser-than-ceo` | (bài kiểm) `Độ lệch cho phép của clip (clipToleranceSeconds) 0.6 giây lớn hơn mức tối đa 0.5 giây.` | Trả số đó về giá trị CEO đã duyệt hoặc chặt hơn (mục 1). |
| `ledger-truncated` | (bài kiểm) `Không thấy sổ theo dõi thí điểm ..., nhưng bản trước pilot-ledger.previous.json có 2 mục: sổ đã bị xoá.` | Dừng. Không tự tạo sổ mới; báo người phụ trách để khôi phục sổ. |
| `ledger-entry-missing` | (bài kiểm) `Gói ... có bản sao mục sổ (video V-02, mục số 2) mà sổ theo dõi ... không có: sổ đã mất mục.` | Dừng. Báo người phụ trách để khôi phục sổ. |
| `package-file-not-plain` | `Tệp kê của gói ghi tệp video "..\V-01-20261026T175930707Z\video.mp4": phải là tên tệp đơn trong thư mục gói.` | Gói đã bị sửa. Không duyệt, không đăng; chạy lại `produce`. |
| `package-ledger-mismatch` | (bài kiểm) `Gói V-01 không có bản sao mục sổ (ledger-entry.json); chỉ gói sẵn sàng mới được ghi vào sổ.` | Gói bị giữ, bị sửa hoặc bị chép ra chỗ khác. Không duyệt, không đăng; duyệt đúng gói trong `runs`. |
| `approval-hash-mismatch` | `SHA-256 của video hiện là f5970ade...; giá trị được duyệt là c13e81d4...` | Video đã đổi sau khi tạo. Không duyệt; chạy lại `produce` và duyệt video mới. |
| `approval-void` | `Video V-01 có SHA-256 f5970ade..., khác giá trị đã ghi ... Duyệt cũ mất hiệu lực; không đăng tệp này.` | Không đăng. Chạy lại `produce`, xem lại và duyệt video mới. |

Hai dòng cuối là cùng một tình huống: chỉ một byte của video đã duyệt bị đổi, và công cụ không còn coi lần duyệt là hợp lệ.
Công cụ kiểm mã băm của video trước, rồi mới đối chiếu với sổ theo dõi.

Nếu thấy một mã khác, đọc phần tiếng Việt đi kèm: nó nêu lý do và việc cần làm. Nếu dòng bắt đầu bằng `LỖI CÔNG CỤ`, công cụ ffmpeg hoặc
ffprobe không chạy được; báo cho người phụ trách kỹ thuật, kèm nguyên dòng đó.

## 8. Giới hạn của thí điểm

Sổ theo dõi trong thư mục thí điểm đếm các video đã sẵn sàng: tối đa 12 video, tối đa 2 video cho mỗi sản phẩm, và hai video của cùng
một sản phẩm phải khác trọng tâm. Lệnh `check` báo trước khi một giới hạn đã đầy. Một bản ghi đã dùng cho một video thì không dùng được
cho video khác, kể cả khi video kia đã được tạo lại. Mỗi lần đọc sổ, công cụ so sổ với bản trước của nó và với bản sao mục sổ trong
từng gói; sổ bị xoá, bị cắt bớt hay thiếu mục thì công cụ dừng. Không xoá, không sửa tệp nào trong thư mục thí điểm.
