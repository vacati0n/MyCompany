# Business intent for MC-19 (the owner's request and decisions, verbatim)

## The CEO's request, 2026-10-30, in chat (MC-18 session)

After the two live reads stopped `no-product`, the CEO sent a screenshot of the same product page open in their own
browser, with every fact visible, and wrote:

"đúng là mở trình duyệt thì có đầy đủ thông. hãy tìm cách vì làm manual rất take time"

(It is true that the browser shows all the information. Find a way, because doing it by hand takes a lot of time.)

The orchestrator laid out three ways:

1. Calling Shopee's internal API. Excluded: it needs forged anti-bot signatures.
2. Running a hidden browser in the app. Not recommended: it takes Shopee cookies and runs into CAPTCHA or sign-in walls.
3. A one-click button (bookmarklet) in the operator's own browser that hands what the page shows to the app.
   Recommended.

The orchestrator asked "Mở MC-19 làm nút một chạm trên trình duyệt không?" (open MC-19 to build the one-click browser
button?). The CEO answered:

"Mở MC-19"

## Decisions in force

- `CEO-D-1318` (this request): the one-click button, bounded as recorded, with the open points left for the Scope Gate.
- `CEO-D-1313`: no sign-in, no cookies, no CAPTCHA solving, no forged headers, signatures or user agent, no IP
  rotation, no proxy. One product per click. Paste mode stays. One off switch. The CEO reviews each fact and approves
  each video (`D-002`). The terms risk stays with the CEO (`D-018`), with the same-day stop condition.
- `CEO-D-1314`: read price and description are shown for reference only, and each fact row keeps its screenshot.
- `CEO-D-1315`: image hosts are shopee.vn and `*.susercontent.com`, for that product's image files only. The switch
  is off by default and set in "Cài đặt". The user agent is honest.
- `CEO-D-1316` and `CEO-D-1317`: on real pages an honest read yields `no-product`. This is why MC-19 exists.

Full text: `research/ceo-decision-record.md`, entries CEO-D-1313 to CEO-D-1318.
