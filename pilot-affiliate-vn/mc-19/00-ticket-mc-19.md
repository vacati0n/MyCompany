# MC-19: one-click button in the operator's own browser fills step 1 from the open Shopee product page

- Source: `CEO-D-1318` (the CEO's own decision, 2026-10-30); local://research/ceo-decision-record.md#ceo-d-1318
- Issue type: Feature   Priority: Highest
- Labels: affiliate, shopee-video, pilot, operator-ux, web-app, fact-sheet, bookmarklet, vietnamese
- Routed as: /implement (implement-feature)
- Builds on: MC-18 (`src/MediaCompany.Pilot.Web` read-from-link and paste mode, run `run-6fa26c9657d3`, commit 936d2f0)
  on MC-17 and MC-16. None is merged to `main`. The MC-19 session branch is cut from the MC-18 branch head.

## Description

### WHY

MC-18's two live reads (`CEO-D-1316`, `CEO-D-1317`) showed that a read without running the page's scripts gets only
Shopee's 203 KB page shell, with no product data, so the read stops `no-product`. The CEO's own browser shows every
fact. Paste mode works, but the CEO's words are: "làm manual rất take time" (doing it by hand takes a lot of time).

### WHAT THE OPERATOR DOES

1. One time: in "Cài đặt", the app shows a button the operator drags to the browser's bookmark bar (a bookmarklet),
   with short Vietnamese instructions.
2. For each product: the operator opens that product's page on shopee.vn in their own browser, as today, and presses
   the button once.
3. The button reads what the page already shows:
   - product name;
   - classification (the selected variant, if any);
   - shop name and whether it is Mall;
   - the CHI TIẾT SẢN PHẨM specification rows;
   - price and description, for reference only;
   - the addresses of that product's own images;
   - the page address.
4. The data reaches step 1 of the local web app for the open video, and fills the same fields and candidate rows that
   MC-18's read and paste fill. The marks show the new source. The operator confirms each field and row, and picks up
   to 3 images. The app fetches those image files from the approved image hosts.
5. The MC-16 fact-sheet check and the MC-18 confirmation rule decide the save, unchanged.

### WHAT THE COMPANY ALREADY HOLDS: REUSE IT

- MC-18's fill record, marks, confirmation rule, candidate rows, image pick and image-host rule, the read switch and
  the "Cài đặt" view, and the paste parser.
- MC-17's request guard (exact Origin, per-start token), which every new route must pass.
- The headless browser the web tests already drive, used to test the button against a fixture page.

### HARD CONSTRAINTS (from `CEO-D-1313` and `CEO-D-1318`, unchanged)

1. One product per press. The button reads only the page it is pressed on. No crawling, no other pages, no batch runs,
   no scheduled reads.
2. No tool signs in. The app never sends or stores Shopee cookies, and affiliate.shopee.vn is never opened. The button
   takes no action on the operator's account.
3. No CAPTCHA solving, no forged headers, signatures or user agent, no IP rotation, no proxy. The app sends no request
   to shopee.vn pages for this feature. The only egress is image files of that product, from shopee.vn or
   `*.susercontent.com`, under the MC-18 rules (one request per image, no redirect followed, PNG or JPEG signature).
4. The web app keeps listening on 127.0.0.1 only. The MC-16 toolchain (`src/MediaCompany.Pilot`) stays unchanged with
   zero sockets.
5. Anything the button hands over is untrusted input. It is size-bounded, parsed as data, never executed, and shown
   only as text.
6. The read switch stays the one off switch: when it is off, the button's hand-over is refused and paste mode works.
7. Download no package (no npm, NuGet, pip, CDN) unless the CEO approves it by name. No generative model, no speech
   recognition.
8. Agents never sign in, create accounts, contact sellers, buy or publish, and never open shopee.vn. Frozen MC-13 and
   MC-14 and the `mediacompany` database are not touched.
9. Tests run offline against code-generated fixture pages served on 127.0.0.1. No media file is added to the
   repository.

### OPEN FOR THE SCOPE GATE (`CEO-D-1318` item 3)

- **Signed-in page.** Whether the button may be pressed on a page where the operator is signed in to Shopee. It reads
  only what is shown and sends nothing to Shopee.
- **Hand-over path.** Straight from the shopee.vn page to 127.0.0.1, which browsers may block for a public page calling
  a local address, or through the clipboard: the button copies a structured block and the operator pastes it once.
  The design phase measures which works in the installed browser against the fixture.
- **Live check.** No agent opens shopee.vn. The CEO tries the button once on a real product page in their own browser,
  after quality review.
- **Screenshots.** Each fact row still needs a screenshot file (`CEO-D-1314` item 4). The button cannot take one.

### DONE MEANS

- MC-19 runs through every phase and gate of `implement-feature`. The CEO decides at the Scope and Design gates.
- The button is shown working in the Browser pane against a fixture product page, ending in filled, confirmable fields
  at step 1, and with the switch off ending in a refusal.
- Committed and pushed on the MC-19 session branch, and the CEO gets the compare link.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All are in `research/framework-defects.md`:

- Every agent gets its own staging folder.
- A non-implementing role writes no repository file.
- An agent that claims a finding covers "every" case lists the cases.
- Gate rationales do not reach agents, so pass the CEO's decisions along (Finding 7).
- A `failure-envelope.json` of class `gate-approval-required` is stale once that phase's dispatch prompt exists
  (Finding 11).

## Acceptance criteria

_To be defined by the scope phase and approved at the Scope Gate._
