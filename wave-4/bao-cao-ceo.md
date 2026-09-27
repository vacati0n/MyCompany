# Wave 4 — Báo cáo CEO

**Run `run-5d5e6bd74c36` · ticket MC-5 · 2026-09-27 · Completed, 6/6 phase, 6/6 gate.**

> Mọi con số là **ĐO ĐƯỢC** trừ khi ghi rõ là giả định. §7 là phần tôi báo cáo **lỗi của chính tôi** — xin anh đọc kỹ mục đó.

---

## 1. Kết quả một câu

Wave 4 đã xây xong lớp analytics cho một kênh **chưa đăng gì cả** — và đó chính là điều khó.

Sáu chỉ số quan trọng nhất về tiền (doanh thu, RPM, lợi nhuận mỗi video, lợi nhuận mỗi kênh, ROI, chi phí trên mỗi đô doanh thu) **được xây xong nhưng tắt đèn**. Chúng chỉ sáng khi có một tham số doanh thu **quan sát được**, không phải giả định.

**Không đăng gì, không mua gì, không chi một đồng. Ngoại lệ chi tiêu anh đã duyệt vẫn còn nguyên, chưa dùng.**

---

## 2. Điều Wave 4 thực sự chứng minh: số 0 "không có gì xảy ra" khác số 0 "chưa bao giờ đo"

Đây là thứ wave này được chấm điểm, và nó đã được làm **bằng cấu trúc code chứ không phải bằng quy ước**.

Mỗi đại lượng trong analytics bắt buộc là **một trong ba trạng thái**:

| Trạng thái | Mang theo gì |
|---|---|
| Có quan sát, giá trị khác 0 | số + đơn vị |
| Có quan sát, **giá trị bằng 0** | đơn vị, **không có ô chứa số** |
| **Chưa từng đo** | lý do (đóng ở 2 khả năng) + diễn giải bắt buộc, **không có ô giá trị nào cả** |

Điểm mấu chốt: trạng thái "chưa từng đo" **không có ô nào để chứa số**. Nghĩa là không lập trình viên nào — kể cả vô tình — viết được câu lệnh "nếu chưa có số thì lấy 0". **Câu lệnh đó không diễn đạt được trong ngôn ngữ, chứ không phải bị cấm.**

Và "đo được và bằng 0" là một trạng thái **riêng**, không phải "có giá trị mà giá trị là 0". Người đọc phân biệt được hai thứ đó **mà không cần nhìn vào con số**.

Phương án thay thế (một ô giá trị có thể rỗng) đã bị architect loại **và ghi rõ lý do**: ô giá trị đó tồn tại ở mọi trạng thái, mà một ô tồn tại ở mọi trạng thái thì sớm muộn có người điền vào.

---

## 3. Sáu chỉ số tiền: **không tiếp cận được**, chứ không phải bị ẩn

Đây mạnh hơn yêu cầu ban đầu. Cách duy nhất để một chỉ số doanh thu có giá trị là gọi một hàm **bắt buộc nhận vào một tham số doanh thu đã quan sát** — và kiểu dữ liệu đó **không tạo ra được nếu thiếu nguồn quan sát và ngày quan sát**.

Khi chưa có gì được ghi nhận thì **không có đối số nào để truyền vào, nên hàm đó không gọi được**. Không có số tạm, không có số mặc định, không có số mô phỏng — **không phải vì bị giấu, mà vì không tồn tại.**

Sổ đăng ký tham số doanh thu được giao **rỗng** và wave này không ghi vào đó dòng nào.

---

## 4. Bằng chứng thực thi

| Điều kiện | Kết quả |
|---|---|
| Có PostgreSQL 17 | **489 pass, 0 fail, 0 skip** |
| Không có datastore | 450 pass, **39 skip có ghi lý do** |
| Nền trước wave này | 438 (ghi là *được báo*, không phải *tôi xác nhận*) |

Bộ kiểm tra kiến trúc: **43 → 46**.

**Ba bên đo độc lập** — agent implement, agent review, và tôi tự chạy — **ra con số giống hệt nhau**.

---

## 5. Ngoại lệ chi tiêu: **chưa dùng, và lý do mới là điều đáng chú ý**

Anh đã cho phép **một ngoại lệ hẹp, có tên** để ghi nhận được một reasoning tier thực sự được phục vụ. Wave 3 không dùng. **Wave 4 cũng không dùng — nhưng lần này nghĩa vụ phía sau nó đã hoàn thành.**

Khi đọc code, phát hiện ra: **tier được phục vụ là thuộc tính của tuyến đã được nạp (admitted route), không phải của phản hồi từ nhà cung cấp.** Mọi nhánh xử lý đều ghi cùng một trường đó.

Nghĩa là bằng chứng lấy được bằng cách **chạy thật lớp boundary đã giao, trên một record store thật** — **không tài khoản, không endpoint, không credential, không mua, không đăng ký dịch vụ, chi phí bằng 0.**

| | |
|---|---|
| Chi trong Wave 4 | **USD 0,00** |
| Ngoại lệ anh duyệt | **chưa dùng, vẫn còn** |
| Ngân sách tháng | **USD 77,41, không đổi** |

**Nói thẳng phần không đạt:** tiêu chí trong plan ghi *"đúng một operation có tính phí được chạy"* — **không đạt theo đúng câu chữ, vì số operation chạy là 0.** Nghĩa vụ đằng sau nó thì đạt. Cả hai vế đều được ghi vào hồ sơ phát hành. Không chỗ nào gọi ngoại lệ này là "đã dùng".

### Và đây là giới hạn quyết định giá trị thực của cả phần này

**Bảng `routes` trong cơ sở dữ liệu không có cột reasoning tier nào cả.** Registry dựng mọi route mà không có nó.

Hệ quả: **trên đường production hoàn chỉnh, mọi served tier được ghi đều là dấu hiệu vắng mặt — bất kể route nào phục vụ.**

**Cơ chế đã được chứng minh. Đường production chưa nuôi được nó.** Thêm cột đó và adapter của nó là thay đổi có giá trị cao nhất cho toàn bộ dòng công việc này, và là việc đầu tiên Wave 5 nên cân nhắc.

### Điều vẫn chưa thay đổi sau ba wave

**Tỷ lệ tier mà toàn bộ lập luận kiểm soát chi phí của công ty dựa vào vẫn chưa được đo.** Một bản ghi chứng minh **cơ chế ghi đúng thứ nó nói là nó ghi** — **nó không chứng minh tỷ lệ**. Chi phí mỗi item và ngân sách tháng vẫn là **giả định, không phải đo đạc**. Đây là wave thứ ba đóng lại với câu đó còn đúng.

---

## 6. Phần review đã cứu wave này

Đây là chỗ quy trình trả tiền cho chính nó.

**Lỗi nghiêm trọng số 1 — chính cái tính chất wave này tồn tại để bảo vệ, lại hỏng theo chiều ngược lại.** Khi ghi một đại lượng xuống database, code cũ chỉ ghi số khi giá trị **khác 0**. Nghĩa là **một số 0 đã đo được lưu thành "trống", rồi khi đọc lên lại thành "chưa bao giờ đo"**.

Scope, plan, design và cả bản brief của tôi — **không ai nghĩ tới chiều này**. Chỉ có review tìm ra.

Cách sửa còn tốt hơn thứ tôi yêu cầu: **dòng dữ liệu giờ tự mã hoá trạng thái của nó**, kèm một ràng buộc ở tầng database chỉ chấp nhận đúng ba hình dạng hợp lệ. **Hình dạng sai không ghi được**, chứ không phải "chưa ai ghi". Agent còn cố tình đưa lỗi vào lại để xem kiểm tra có bắt không, rồi mới gỡ ra.

**Lỗi nghiêm trọng số 2 là lỗi của tôi** — xem §7.

Tổng: **12 phát hiện, 8 đã đóng, 4 còn mở** (1 trung bình, 3 thấp), **không còn cái nào chặn**.

---

## 7. Ba lần tôi sai trong wave này, và ai bắt được

Tôi báo cáo mục này vì nó là cơ chế duy nhất từ trước đến nay bắt được lỗi của chính người điều phối — và nó bắt được **mọi lần**.

**1. Tôi đã coi một ràng buộc cứng là chuyện chữ nghĩa.** Repo không có hệ thống chạy kiểm tra tự động. Tôi phát hiện điều đó, rồi dặn các agent rằng câu *"build sẽ từ chối"* là **cách diễn đạt quá lời cần sửa**.

Reviewer chỉ ra: thiết kế đã duyệt **yêu cầu tính chất đó phải được chứng minh ở thời điểm build**, tiêu chí trong plan yêu cầu kiểm tra **chạy như một phần của build**, và chính ticket cũng nói vậy. **Cả ba đều không đúng.** Đây là một ràng buộc cứng **bị vô hiệu**, không phải một câu viết lỏng.

Tôi đã quyết định **làm cho nó đúng thay vì viết lại cho khớp**. Giờ mỗi lần build toàn solution, 46 kiểm tra kiến trúc tự chạy và **làm hỏng build nếu vi phạm**. Tôi đã tự thử: cố tình nhét một con số trần vào code — **build fail, nêu đích danh kiểm tra đã từ chối**; gỡ ra thì build sạch.

*Bài học tôi ghi lại:* **người phát hiện ra một sự thật bất tiện lại là người ít có khả năng nhận ra rằng nó phá vỡ một ràng buộc, chứ không chỉ làm một câu văn khó viết.** Tôi xếp nó vào ngăn "chuyện diễn đạt" vì ngăn đó không đòi hỏi phải thay đổi gì.

**2. Tôi đưa sai số lượng file.** Planner đối chiếu và chỉ ra mâu thuẫn. Hoá ra cả hai con số đều đúng — đếm hai thứ khác nhau (89 gồm cả file project, 82 là file mã nguồn).

**3. Tôi khẳng định một điều về code mà không có nguồn.** Agent scope **kiểm chứng thấy tôi nói đúng, nhưng vẫn từ chối dùng nó làm căn cứ**, vì nó không có trong bất kỳ tài liệu đầu vào nào.

**Và một lần nữa ở phase cuối:** agent documentation **từ chối công bố con số tỷ lệ 290.000/87.000** mà tôi đưa trong brief, vì con số đó không xuất hiện trong bất kỳ artifact nào của run. Nó chỉ ghi rằng tỷ lệ chưa được đo. **Đúng.** Đây là lần thứ ba trong chương trình một agent từ chối công bố thứ người điều phối khẳng định — và cả ba lần đều đúng.

Agent review cũng **tự sửa lỗi đo của chính nó** thay vì giấu: phép thử đầu tiên của nó chạy qua ống dẫn nên đọc nhầm mã thoát, suýt tạo ra một phát hiện thổi phồng. Nó chạy lại và báo cáo.

**Tổng kết: mọi phản đối được ghi lại trong chương trình này cho tới nay đều đúng. Con số đã lên hai chữ số và chưa có ngoại lệ nào.**

---

## 8. Chi phí wave này

| | Wave 2 | Wave 3 | **Wave 4** |
|---|---|---|---|
| Tổng token | 1.836.117 | 1.930.677 | **1.859.405** |
| Làm lại | 7,0% | 7,1% | **9,9%** |
| Phase qua validation ngay lần đầu | 2/6 | 4/6 | **6/6** |
| Làm lại do validator từ chối | phần lớn | phần lớn | **không có** |

**Hai con số này đi ngược chiều nhau và cả hai đều đi đúng hướng.** Tỷ lệ làm lại **tăng**, nhưng số lần bị validator từ chối về **0**. Toàn bộ phần làm lại của Wave 4 là do **review tìm ra lỗi thật** — một lỗi nghiêm trọng trong code đã giao, và một ràng buộc cứng bị vô hiệu.

So sánh: phần làm lại của Wave 3 **phần lớn là sửa cách trích dẫn do chính tôi chỉ dẫn sai** — tốn tiền mà không mua được gì.

**7,1% công sức lãng phí tệ hơn 9,9% công sức tìm ra lỗi.** Xin anh đọc hai dòng đó cùng nhau.

**Chi phí vận hành: USD 0,00.** Bốn wave đã đóng mà chưa tiêu một xu ngân sách vận hành.

---

## 9. Những gì vẫn cần anh, không ai khác làm được

**Chưa cái nào được giải quyết. Mỗi cái đều cần anh đăng nhập hoặc xác minh danh tính.**

1. **Đăng ký kênh trên mọi thư viện nhạc và stock.** Doanh thu mất trước khi đăng ký là **không lấy lại được**.
2. **Tài khoản thanh toán**, theo cấu trúc payee đã chốt.
3. **Xác thực hai bước** trên kênh.
4. **Một phiên đăng nhập thư viện** để lấy số lượng clip thật cho 14 chủ đề đã audit — **Wave 2 lấy được 0, và không ai được phép bịa ra một con số nào.**

---

## 10. Ba quyết định là của anh, tôi không tự quyết

1. **Bất kỳ khoản chi nào vượt ngoài ngoại lệ đã duyệt.** Tôi đã trả lời câu hỏi chặn của phase scope theo hướng **dè dặt nhất** — không chi thêm — dựa trên chính quyết định của anh, vốn đã yêu cầu ghi dấu hiệu vắng mặt khi route không nêu được tier. **Nếu anh muốn khác, xin nói.**
2. **Xuất bản, tạo tài khoản, tạo kênh.**
3. **Ngưỡng "hồ sơ sạch"** để nới lỏng việc anh duyệt từng lần đăng. **Vẫn không suy ra được** từ một phép đo 1 phút 58 giây duy nhất — đo trên một item đang bị giữ, do chính anh duyệt khi đã biết nội dung, không có thời gian chờ, không có lần sửa nào. Câu hỏi này còn treo cho tới khi có một chuỗi đo có ít nhất một lần duyệt bị trả lại.

---

## 11. Những gì mang sang Wave 5

**20 known issue** đi kèm bản phát hành, tất cả còn mở. 9 cái kèm câu hỏi mở đã định danh người chịu trách nhiệm.

Đáng chú ý nhất:

- **Bảng `routes` thiếu cột reasoning tier** — cho tới khi có, mọi served tier trên đường production đều là dấu hiệu vắng mặt.
- **4 trong 5 nguồn dữ liệu chưa có bảng, chưa có port, chưa có adapter, chưa có nơi gọi** trong hệ thống hoàn chỉnh. Đây là quyết định của architect. Reviewer **từ chối hạ mức nghiêm trọng** chỉ vì tôi quyết định để lại sau — lập luận của nó: *quyết định lùi lịch không phải là bằng chứng, và người điều phối hoãn lại không đồng nghĩa với người chịu trách nhiệm chấp nhận rủi ro*. Tôi chấp nhận lập luận đó hoàn toàn.
- **16 known issue của Wave 3** vẫn chưa giải quyết; 1 trong số đó nay đã có câu trả lời.
- Repo **chưa khai báo version ở đâu cả**; agent documentation đã suy ra `1.1.0` và **công bố luôn việc nó suy ra**, thay vì khẳng định như một sự thật.

---

## 12. Điều tôi muốn anh giữ lại từ wave này

Wave 4 xây một thứ **tự nó không nói dối được**. Không phải nhờ ai cẩn thận, mà nhờ những câu nói dối đó **không diễn đạt được**: không có ô để điền số 0 giả, không có đối số để bật một chỉ số doanh thu chưa quan sát, không có hình dạng dòng dữ liệu nào làm một số 0 đã đo biến thành chưa đo.

Và quy trình đã bắt được **hai lỗi mà không công cụ tự động nào bắt được** — trong đó một lỗi là của chính tôi, và tôi đã ghi nó lại ở §7 thay vì làm nhẹ đi.

**Đó là thứ đáng giá hơn bất kỳ dòng code nào trong wave này.**
