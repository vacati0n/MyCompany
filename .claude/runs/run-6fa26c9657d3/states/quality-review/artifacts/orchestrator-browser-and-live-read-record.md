# MC-18 orchestrator record: Browser-pane run (SC-34) and the single live read (CEO-D-1316)

Recorded 2026-10-30 by the orchestrator of run-6fa26c9657d3, on the MC-18 session branch at commit 58a22c5
(implementation plus review corrections CR-001 to CR-006). This record answers planning question Q-003 (the
orchestrator records the Browser-pane run) and design question Q-006 (yield of an unmodified read on a real page).

## 1. Browser-pane run against the fixture host (Q-007: fixture host mode)

Started as `dotnet run --project tests/MediaCompany.Pilot.Web.Tests -- --fixture-host --root <scratch folder>`,
listening on 127.0.0.1:47900 with a scratch app folder. Video V-01, step 1.

| # | Action | Observed |
|---|--------|----------|
| 1 | Open step 1 before turning the switch on | "Đọc trang đang tắt"; paste box and typed fields usable. |
| 2 | Turn the switch on in "Cài đặt" | Saved to `doc-trang-san-pham.json`; message "Đã bật đọc trang sản phẩm". See observation O-1. |
| 3 | Read the short link `https://s.shopee.vn/mc18-ngan` | Redirect followed to the fixture product page; 4 header fields and 4 specification rows offered; 5 images saved as anh-doc-01 onward; price and description shown for reference only, not placed in the fact sheet. |
| 4 | Read the blocked link `https://shopee.vn/bi-chan-403` | "DỪNG ĐỌC [access-refused]", text "Ứng dụng không thử đọc lại"; no retry request in the read log; paste box opened. |
| 5 | Paste the fixture page text (`chu-trang-mau.txt`) and press "Điền từ chữ đã dán" | 2 header fields and 4 specification rows filled from the pasted text. |
| 6 | Press "Lưu bảng thông tin" with incomplete rows | Refused by the MC-16 fact-sheet check first: `header-field-missing`, then `row-field-missing` (Dòng F1 thiếu chủ thể). See observation O-2. |

A full save needs one screenshot file per fact row; the Browser pane cannot attach a local file, so the save path is
covered by the automated headless browser cases of the web test program (53 of 53 passing at commit 58a22c5).

### Observations for qa

- O-1: in the fixture-host run the host was started with the switch on, yet step 1 first showed the read as off and
  the first checkbox click reported "Đã tắt", the second "Đã bật". In the isolated live-read app (section 2) the
  checkbox started unticked, the file said off, and one click turned it on and one click off, consistently. The
  fixture-host start-up state and the settings view did not agree; low severity, no egress effect (the server
  decides from the file on every read).
- O-2: the confirmation refusal `fill-unconfirmed` (FillRecord, raised from the save route) is evaluated after the
  MC-16 fact-sheet check (`FactSteps.CheckBeforeImages` runs first in the save route). This ordering is deliberate:
  an incomplete sheet is refused with the toolchain's own reason before the confirmation rule is applied. Not a defect.
- O-3: after a read, values the read filled (page path, read date) stay in the form when the operator then uses paste
  mode; the paste fills only what the pasted text supplies. Informational.

## 2. The single live read (CEO-D-1316)

- Link named by the CEO in chat: `https://s.shopee.vn/4qGnwJJXwI`.
- Made by the orchestrator only, once, after quality review, in an isolated app (scratch app, pilot and desktop
  folders; port 47830; listener 127.0.0.1). The switch was turned on in "Cài đặt" for the read and turned off right
  after; the file afterwards reads `"readOn": false`.
- One press of "Đọc trang". No retry.

Read log (`doc-trang.log`, complete):

| Time (UTC) | Host | Path | Status | Bytes |
|------------|------|------|--------|-------|
| 2026-10-30T02:47:57.618Z | s.shopee.vn | /4qGnwJJXwI | 301 | 0 |
| 2026-10-30T02:47:58.256Z | shopee.vn | /opaanlp/89827191/28309604497 | 200 | 203445 |

Outcome shown to the operator: "DỪNG ĐỌC [no-product]: Trang trả về không có thông tin sản phẩm đọc được (trang
Shopee thường chỉ hiện sản phẩm khi trình duyệt chạy mã của trang)", with the paste-mode next step and "Ứng dụng không
thử đọc lại". Nothing was filled; no image was requested; the app log shows one `POST /api/read` answered in 1252 ms.

Bounds held on this read: two requests in total, both to shopee.vn hosts; the short-link hop was checked and followed
(one hop of the five allowed); no cookie sent or kept; no sign-in; affiliate.shopee.vn not contacted; honest user
agent "MediaCompanyPilot/1.0 (doc-trang-san-pham)"; no image host contacted.

## 3. Answer to design question Q-006 (real-page yield)

An unmodified read of this real product page yields no product facts: the 203 KB HTML Shopee serves to a client that
does not run the page's scripts carries no readable product data, so the read stops with `no-product` and the operator
continues in paste mode. This matches the design's expectation that paste mode is the common path on real pages. The
read-from-link step still earns its place where a page does carry structured data, and it fails closed (no fill, no
retry) where it does not.
