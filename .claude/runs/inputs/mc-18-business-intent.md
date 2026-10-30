# Business intent for MC-18 (the owner's request and decisions, verbatim)

## The CEO's request, 2026-10-27, in chat (relayed by the MC-16 session, confirmed directly in the MC-17 session)

"Tôi chấp nhận rủi ro và muốn tự động hoàn toàn. Hãy soạn quyết định CEO cho phép bot tự đọc trang sản phẩm qua link tiếp thị để tôi duyệt."
Approval of the draft: "chấp nhận nguyên văn". Confirmed in the MC-17 session: "Xác nhận, làm sau MC-17".

---

## The approved text, Vietnamese, as the CEO approved it

### CEO-D-1313 — Bot tự đọc trang sản phẩm Shopee để điền bảng thông tin

**Quyết định của CEO, 2026-10-27.** Lời CEO: "Tôi chấp nhận rủi ro và muốn tự động hoàn toàn."

1. **Được phép:** khi CEO đưa vào một link tiếp thị/link sản phẩm, công cụ mở **trang sản phẩm công khai trên
   shopee.vn** mà link đó trỏ tới, lấy tên, giá, phân loại, thông số, mô tả và ảnh của chính sản phẩm đó, rồi tự điền
   bảng thông tin và lưu ảnh vào thư mục video. Mỗi lần **một sản phẩm, do CEO bấm**; không tự dò trang khác, không
   quét danh mục, không chạy hàng loạt.
2. **Vẫn giữ:** không đăng nhập, không dùng cookie/phiên của CEO, không mở affiliate.shopee.vn (trang đó cần đăng
   nhập). Không vượt CAPTCHA, không giả trình duyệt để né chống bot, không đổi IP. Nếu Shopee chặn hoặc đòi đăng nhập
   → dừng và chuyển sang cách bán tự động (CEO dán nội dung trang vào, công cụ điền).
3. **Thay đổi:** quy định "không tải tài sản nào" (CEO-D-1303) được nới **riêng** cho ảnh sản phẩm CEO đưa vào; quy
   định chỉ kết nối nội bộ (CEO-D-1308) được nới **riêng** cho bước đọc trang tới máy chủ shopee.vn. Bước dựng video
   vẫn hoàn toàn ngoại tuyến.
4. **Kiểm tra giữ nguyên:** dữ liệu bot lấy vẫn qua phép kiểm tra bảng thông tin của MC-16 (từ cấm, cách viết giá,
   câu tự nhận đã dùng), CEO vẫn xem lại từng dữ kiện, và vẫn duyệt từng video trước khi đăng (D-002).
5. **Rủi ro CEO nhận (D-018):** điều khoản Shopee (E-105) cấm đối tác "thu thập bất kỳ thông tin nào trên Sàn TMĐT";
   bot đọc trang có thể bị coi là vi phạm → có thể bị khóa tài khoản affiliate và mất hoa hồng; E-107 phạt tới
   10 triệu đồng mỗi hành vi dùng nội dung không được phép.
6. **Điều kiện dừng:** Shopee trả lời Q-014 bất lợi hoặc gửi bất kỳ cảnh báo tài khoản nào → tắt bot ngay trong ngày,
   quay về cách bán tự động. Q-014 vẫn được gửi.

**Không đổi:** D-002, D-018; agent không tạo tài khoản, không liên hệ người bán, không mua, không đăng.

---

## CEO-D-1313 — The bot reads the public Shopee product page to fill the fact sheet

**The CEO's own decision, 2026-10-27.** The CEO's words: "Tôi chấp nhận rủi ro và muốn tự động hoàn toàn." (I accept
the risk and want it fully automatic.) The CEO approved the Vietnamese text verbatim ("chấp nhận nguyên văn") in the
MC-16 session and confirmed it directly in the MC-17 session, choosing to build it as a follow-up run after MC-17
closes. This entry is the English form.

1. **Allowed:** when the CEO supplies an affiliate link or product link, the tool opens the **public product page on
   shopee.vn** that the link points to. It takes that product's own name, price, classification, specifications,
   description and images, then fills the fact sheet and saves the images into the video folder. **One product at a
   time, on the CEO's click.** No crawling of other pages, no category scanning, no batch runs.
2. **Kept:** no sign-in, no use of the CEO's cookies or session, and affiliate.shopee.vn is not opened (it requires
   sign-in). No CAPTCHA solving, no browser impersonation to evade anti-bot measures, no IP rotation. If Shopee blocks
   the page or asks for sign-in, the tool stops and falls back to the semi-automatic mode: the CEO pastes the page
   content and the tool fills the sheet.
3. **Changed:** the "download no asset" rule (`CEO-D-1303`) is relaxed **only** for the product images of the product
   the CEO supplies. The local-connections-only rule (`CEO-D-1308`) is relaxed **only** for the page-reading step, to
   shopee.vn hosts. Video composition stays fully offline.
4. **Checks unchanged:** the data the bot takes still passes the MC-16 fact-sheet check (banned terms, price
   spellings, use claims). The CEO still reviews each fact, and still approves each video before it is published
   (`D-002`).
5. **Risk the CEO carries (`D-018`):** Shopee's terms (E-105) forbid partners from "collecting any information on the
   e-commerce platform". A bot reading pages may be judged a breach, which can mean the affiliate account is locked and
   commission is lost. E-107 sets a fine of up to 10 million dong for each unauthorised use of content.
6. **Stop condition:** if Shopee answers Q-014 unfavourably or sends any account warning, the bot is switched off the
   same day and the tool returns to the semi-automatic mode. Q-014 is still sent.

**Unchanged:** `D-002`, `D-018`. Agents create no account, contact no seller, buy nothing and publish nothing.
