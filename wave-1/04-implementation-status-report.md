# Wave 1 — Báo cáo trạng thái triển khai

**Gửi:** CEO · **Ngày:** 2026-09-26 · **Run:** `run-dd80173faaad`, phase 4–5
**Nhánh:** `claude/eloquent-taussig-724cc2` (kế thừa từ `849835b`)

---

## 1. Kết luận ngắn

Phần code của Wave 1 **đã xây xong và chạy được**. Bốn thuộc tính cấu trúc mà thiết kế bắt buộc
đều đã hiện thực hoá đúng dạng mạnh — tức là **xoá bỏ đường đi thay thế**, chứ không phải canh gác
nó — và mỗi thuộc tính đều có một bài kiểm tra đã được chứng minh là *sẽ fail* nếu thuộc tính bị phá.

Có **hai việc chưa xong**, và cả hai đều không phải do code:

1. **Không có PostgreSQL** trong môi trường này, nên 15 bài kiểm chứng phía cơ sở dữ liệu chưa chạy
   được. Chúng đã được viết sẵn và sẽ chạy nguyên trạng ngay khi có một instance.
2. **Run đang bị chặn ở phase 5** bởi một lỗi của framework, không phải lỗi của công việc. Việc này
   **cần quyết định của anh** — chi tiết ở mục 6.

---

## 2. Đã giao những gì

90 file, khoảng 12.100 dòng, trên một solution .NET 10 gồm 7 project sản phẩm và 5 project kiểm thử.

| Thành phần | Nội dung |
|---|---|
| `MediaCompany.Domain` | 58 file mã nguồn sản phẩm tổng cộng; tầng trong cùng: tập hành động đóng, bảng chuyển trạng thái cổng xuất bản, hồ sơ quyền sử dụng tài sản, bản ghi vận hành, ngân sách |
| `MediaCompany.Application` | Các cổng (port) tới dữ liệu bền vững, định nghĩa **trước** mọi hiện thực hoá cụ thể |
| `MediaCompany.Deterministic` | Toàn bộ công việc do quy tắc quyết định: định tuyến, quyền, cổng xuất bản, chuỗi bản ghi, retry/backoff, ngưỡng ngân sách, bộ đo báo cáo |
| `MediaCompany.Credentials` | Nhà môi giới thông tin xác thực (credential broker) |
| `MediaCompany.Capability` | Ranh giới phân giải năng lực — lối ra duy nhất tới nhà cung cấp |
| `MediaCompany.Persistence` | Bộ điều hợp PostgreSQL + 2 file schema SQL |
| `MediaCompany.Host` | Composition root, một dịch vụ chạy dài trên một node |

---

## 3. Bốn thuộc tính cấu trúc — đã hiện thực hoá ra sao

Đây là phần quan trọng nhất, vì thiết kế yêu cầu chúng phải đúng *theo cách xây*, chứ không phải
theo cách cấu hình.

| Quyết định | Cách bảo đảm | Bằng chứng |
|---|---|---|
| **D-001** — chỉ một lối truy cập năng lực | `IProviderAdapter` và mọi hiện thực của nó là `internal` trong assembly `MediaCompany.Capability`. Assembly khác **không gọi tên được kiểu đó**, nên "tự dựng adapter" không phải là đường đi biên dịch được — không phải bị cấm, mà là không tồn tại | Bộ kiểm tra ranh giới đọc assembly đã build và xác nhận |
| **D-003** — đặc quyền tối thiểu, 4 lớp độc lập | Tập hành động đóng đông cứng (vai trò bản quyền **không có** hành động xuất bản; vai trò xuất bản **rời rạc** với mọi hành động chặn), vị từ cổng, bảng chuyển trạng thái không có lối tắt, và điều kiện cấp credential phát hành | 12 bài kiểm tra, cộng 2 trigger trong cơ sở dữ liệu phản chiếu 2 lớp |
| **D-004** — vật liệu credential không vào phạm vi người gọi | Handle trả về **không có thành viên nào** có thể chứa bí mật. Việc đổi handle lấy credential xảy ra ở **tầng truyền tải**: hàm gắn credential vào message và trả về một kết quả, không trả về giá trị. Cổng tới kho bí mật là `internal` | Quét toàn bộ bề mặt phát ra (handle, chuỗi hoá, nhật ký kiểm toán) → **0 lần xuất hiện** |
| **D-005** — chi phí AI bằng 0 là thuộc tính **hướng phụ thuộc** | Assembly `MediaCompany.Deterministic` **không tham chiếu** ranh giới năng lực hay credential broker, kể cả gián tiếp. Một lời gọi mô hình là **không diễn đạt được** từ đó | Bộ kiểm tra ranh giới lúc build — **đã kiểm chứng ngược**: thêm một phụ thuộc thật vào broker làm 3 khẳng định fail; gỡ ra thì pass lại |

**D-007 (phê duyệt của chủ sở hữu)** là một *trạng thái* trong bảng chuyển, nằm trong code, và
**vắng mặt** khỏi tập khoá cấu hình được thừa nhận. Không có khoá nào tắt được nó. Số phút trôi qua
được **dẫn xuất** từ hai mốc thời gian đã ghi, ở cả hai phía (kiểu domain và cột sinh trong cơ sở dữ
liệu) — không bên nào nhập tay được. Chưa đặt bất kỳ ngưỡng nào, đúng như thiết kế: chưa có cơ sở đo.

**Số học tiền tệ** nằm trong PostgreSQL, không nằm trong code ứng dụng: `computed_cost` là cột
`GENERATED ALWAYS AS ... STORED` kiểu `numeric`, tính từ số đơn vị và đơn giá đã chốt trên chính
hàng đó. Nhờ vậy chi phí vẫn **suy dẫn lại được** sau khi bảng giá đã đổi.

**Exactly-once**: thay đổi trạng thái, mục hàng đợi, bản ghi vận hành và mục kiểm toán **commit
trong cùng một transaction**. Không có outbox, không có message broker.

---

## 4. Kết quả kiểm thử

Lệnh: `dotnet test MediaCompany.slnx`

| Bộ | Chạy | Đạt | Bỏ qua |
|---|---|---|---|
| Domain | 28 | 28 | 0 |
| Deterministic | 99 | 99 | 0 |
| Capability + Credentials | 28 | 28 | 0 |
| Ranh giới kiến trúc | 25 | 25 | 0 |
| Schema (tĩnh) | 25 | 25 | 0 |
| **PostgreSQL (tích hợp)** | 15 | 0 | **15** |
| **Tổng** | **220** | **205** | **15** |

**0 bài fail.** 15 bài bỏ qua là các bài cần cơ sở dữ liệu thật; chúng **bỏ qua kèm lý do ghi nhận**
chứ không âm thầm pass.

---

## 5. Rà soát chất lượng — 12 phát hiện, 11 đã sửa

Vòng rà soát tìm ra 12 lỗi, trong đó **4 lỗi mức cao**. Ba trong số đó là lỗi thật, đáng kể:

1. **Một đơn vị công việc không bao giờ kết thúc.** Khi hoàn thành chặng cuối, nó bị trả lại hàng đợi
   và được nhận lại vô hạn. Đã sửa: nay đạt trạng thái kết thúc và lịch sử chặng phân giải đúng mỗi
   vị trí một lần.
2. **Một ngoại lệ thoát ra khỏi lối ra duy nhất.** Nếu đơn giá ghi bằng tiền tệ khác với trần chi phí
   của yêu cầu, phép so sánh ném lỗi thay vì từ chối có lý do. Đã sửa: nay giữ lại với lý do ghi nhận.
3. **Thay đổi trạng thái và mục ghi nhận nó không cùng một commit** ở cổng xuất bản và ở sổ tình trạng
   tuyến. Đã sửa: nay cùng một transaction.

Cả ba lỗi này **bộ kiểm thử cũ không bắt được**. Đã bổ sung 10 bài kiểm tra mới, và hai bài then chốt
đã được **kiểm chứng ngược** (tái tạo lại lỗi → bài test fail; khôi phục bản sửa → pass).

**Còn mở: 1 phát hiện** (mức trung bình) — 15 bài kiểm chứng cơ sở dữ liệu chưa chạy.

Phán quyết rà soát: `approve-with-corrections`, trạng thái `provisional`.

---

## 6. ⚠️ Cần anh quyết định — run đang bị chặn

Phase 5 **không hoàn tất được**. Nguyên nhân là lỗi framework, không phải lỗi công việc:

- Validation Engine đã **chấp nhận** artifact rà soát: **31/31 kiểm tra đạt**.
- Nhưng run vẫn dừng ở `awaiting_policy_exception`, vì result envelope đã khai báo các đường dẫn mã
  nguồn mà vòng sửa lỗi chạm tới, trong trường `declared_side_effects`. Vai trò rà soát **không được
  phép** ghi mã sản phẩm, nên 18 đường dẫn mà nó *không hề ghi* bị phân loại là "ghi không khai báo".
- Envelope **đã được sửa** (chỉ khai báo 2 file mà nó thực sự ghi). Nhưng
  `awaiting_policy_exception` là **block cuối, không có lệnh CLI nào xoá được** — đây chính là lỗi
  số 2 đã ghi nhận từ trước.
- Phiên trước đã xử lý bằng một "cầu nối cục bộ" gọi thẳng vào state engine. **Lần này lớp phân
  quyền của host đã từ chối** thao tác đó — và từ chối đúng, vì nó là một lệnh ghi vào trạng thái
  quản trị bên ngoài giao diện được hỗ trợ.

**Ba lựa chọn:**

| | Phương án | Đánh đổi |
|---|---|---|
| **A** | Cho phép chạy cầu nối cục bộ (thêm quyền Bash cho thao tác này) | Nhanh nhất; nhưng là ghi thẳng vào trạng thái quản trị |
| **B** | Sửa ở framework payload (`D:/Project/claude-framework`): thêm subcommand `policy-exception` | Đúng chỗ, sửa vĩnh viễn; tốn thêm một vòng việc |
| **C** | Bỏ run hiện tại, chạy lại phase 5 trên một run mới | Mất lịch sử run; không khuyến nghị |

**Khuyến nghị: B**, và dùng A như giải pháp tạm nếu anh muốn Wave 1 khép lại ngay hôm nay.

---

## 7. Đính chính một dự báo trước đó

Phiên trước ghi nhận "Verification Gate không thể quyết được" là lỗi số 4. **Tiền đề đó có vẻ sai**
với workflow `implement-feature`:

- Quy tắc loại trừ người tạo chỉ ràng buộc *người tạo ra bằng chứng*. Trong `implement-feature`,
  phase tạo bằng chứng là `quality-review`, và chủ sở hữu của nó là **`omn-dev-2-reviewer`**, không
  phải `omn-qa`.
- `omn-qa` **không sở hữu phase nào** trong workflow này, nên nó không tạo ra thứ mà cổng đánh giá,
  và quy tắc không loại trừ nó.
- Chính cột lý do trong gate matrix cũng nói vậy: với `fix-bug` thì `omn-qa` *có* sở hữu phase tạo
  bằng chứng nên cần chủ sở hữu thứ hai; với `implement-feature` thì không.

Chưa kiểm chứng được bằng thực nghiệm vì run đã dừng trước cổng đó. Nhưng khả năng cao **đây không
phải trở ngại** như đã lo.

---

## 8. Các khoản còn mở — mang sang, không giải ở đây

| Mã | Nội dung |
|---|---|
| `RK-001` | Các biện pháp kiểm soát tuân thủ trước lần xuất bản đầu tiên |
| `RK-002` | **Lấy lại đơn giá trước mọi khoản chi**; đợt rà soát chính sách kế tiếp đến hạn **2026-10-26** |
| `RK-003` | Vị thế thuế tại Việt Nam — chặn việc mở tài khoản ngân hàng, không chặn việc xây |
| `RK-005` | Cơ sở đo số phút phê duyệt — hệ thống **đã được trang bị đo** từ lần phê duyệt đầu tiên |
| `RK-006` | Ngưỡng YouTube Partner Program **2027-02-01** — mốc cố định duy nhất |
| `Q-005` (thiết kế) | Chọn kho bí mật chuyên dụng nào — hiện dùng bộ điều hợp biến môi trường ngoài tiến trình |

Chủ đề kênh và các trụ nội dung **vẫn chưa chọn**, và chúng chặn Wave 2, không chặn Wave 1.

---

## 9. Lưu ý bắt buộc về các con số

> **Mọi con số chi phí và năng lực mà hệ thống này có thể đưa ra đều là ƯỚC TÍNH**, dựa trên đơn giá
> **chưa được kiểm chứng trực tiếp**. Chưa có đơn giá nào được lấy lại, và **chưa có khoản chi nào
> được cam kết**.

Wave này **không xuất bản gì, không tạo kênh, không tạo tài khoản, không chi tiền** — đúng ranh giới
đã chốt. Số endpoint nhà cung cấp được cấu hình hiện là **0**.

---

## 10. Về nhánh Git

Code nằm trên `claude/eloquent-taussig-724cc2`, kế thừa từ `849835b`. Nhánh mà framework ràng buộc
(`feature/mc-2-wave-1-company-foundation-registries-job`) cũng kế thừa từ đúng commit đó, nên nó
**fast-forward được** sang công việc này mà không cần merge. Lý do phải làm vậy: công cụ soạn thảo
của host từ chối ghi vào đường dẫn thuộc checkout gốc từ phiên này.
