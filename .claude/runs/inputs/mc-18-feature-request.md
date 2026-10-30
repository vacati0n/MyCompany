# MC-18: "Read from link": the web app reads one public Shopee product page and fills the fact sheet

- Source: `CEO-D-1313` (the CEO's own decision, 2026-10-27); local://research/ceo-decision-record.md#ceo-d-1313
- Issue type: Feature   Priority: Highest
- Labels: affiliate, shopee-video, pilot, operator-ux, web-app, fact-sheet, network-egress, vietnamese
- Routed as: /implement (implement-feature)
- Builds on: MC-17 (`src/MediaCompany.Pilot.Web`, run `run-022d0492a75a`, commit 3e220c0) on MC-16 (`src/MediaCompany.Pilot`).
  Neither is merged to `main`. Branch `claude/mc-18-shopee-read-link` is cut from `claude/mc-17-pilot-web-fffc59`.

## Description

### WHY

The CEO's words: "Tôi chấp nhận rủi ro và muốn tự động hoàn toàn." (I accept the risk and want it fully automatic.)
Today step 1 of the MC-17 web app asks the operator to type every fact of the fact sheet by hand from the product page,
and to save the three product images and the screenshot themselves. `CEO-D-1313` allows the tool to read the public
product page itself, one product per click, and to fill the fact sheet and save the images.

### WHAT THE OPERATOR DOES

1. At step 1, the operator pastes an affiliate link or a product link and presses one button.
2. The tool follows the link to the **public product page on shopee.vn**, reads that product's own name, price,
   classification, specifications, description and images, fills the fact sheet form and saves the images into the
   video's folder.
3. Every filled field is marked as read from the page. The operator reviews each fact, and the MC-16 fact-sheet check
   (banned terms, price spellings, use claims) runs exactly as for typed facts. Nothing is accepted without the
   operator's review.
4. If Shopee blocks the read, asks for sign-in, shows a CAPTCHA, or the page cannot be parsed, the tool stops and offers
   **paste mode**: the operator copies the page's text from their own browser and pastes it; the tool parses the paste
   and fills the same fields.

### WHAT THE COMPANY ALREADY HOLDS — REUSE IT

- MC-17's step 1 form, its live banned-term check, its image and screenshot steps, and its request guard.
- MC-16's fact sheet check, which stays the authority on what a fact sheet may hold.
- Installed on this machine: the .NET 10 SDK (`HttpClient`), and the headless browser the MC-17 tests already drive.

### HARD CONSTRAINTS (from `CEO-D-1313`, unchanged)

1. One product per operator click. No crawling of other pages, no category scan, no batch runs, no scheduled reads.
2. No sign-in, no cookies or session of the CEO, and affiliate.shopee.vn is never opened.
3. No CAPTCHA solving, no browser impersonation to evade anti-bot measures (no forged headers or signatures, no faked
   user agent), no IP rotation, no proxy. A block, a sign-in wall or a CAPTCHA means stop and offer paste mode.
4. Network egress only to shopee.vn hosts and only during the read step. The MC-16 toolchain stays at zero sockets and
   video composition stays fully offline. The web app keeps listening on 127.0.0.1 only.
5. Downloads are relaxed only for the product images of the product the operator supplied (`CEO-D-1303` relaxed per
   `CEO-D-1313` item 3).
6. Fetched facts pass the MC-16 fact-sheet check unchanged; the CEO reviews each fact and approves each video (`D-002`).
7. Stop condition (`CEO-D-1313` item 6): the read step can be switched off in one place, leaving paste mode working.
8. Download no package (no npm, NuGet or pip package, no CDN script) unless the CEO approves it by name. No generative
   model, no speech recognition, no metered service.
9. Agents never sign in, create accounts, contact sellers, buy or publish. Frozen MC-13 and MC-14 and the `mediacompany`
   database are not touched.
10. Tests run offline: product pages, blocked pages, sign-in walls and images are served by a code-generated local
    fixture server, never shopee.vn. No media file is added to the repository. Agents do not read shopee.vn during
    development unless the CEO allows it by name at the Scope Gate.

### OPEN FOR THE SCOPE GATE

- **Image hosts.** Shopee serves product images from hosts outside shopee.vn (for example `*.susercontent.com`). The
  decision names "máy chủ shopee.vn" only. The CEO decides whether the image CDN of the product is included.
- **Short links.** Affiliate links are commonly `s.shopee.vn/...` redirects; the read follows redirects only within the
  allowed hosts.
- **Live check.** Whether an agent may read one real public product page once, on the CEO's named link, to show the step
  working; otherwise the demo uses the local fixture and paste mode only.

### DONE MEANS

- MC-18 runs through every phase and gate of `implement-feature`; the CEO decides at the Scope and Design gates.
- The step is shown working in the Browser pane against the local fixture, including a block that falls back to paste
  mode.
- Committed and pushed on `claude/mc-18-shopee-read-link`; the CEO gets the compare link.

### FRAMEWORK DEFECTS TO BRIEF EVERY SUBAGENT ON

All in `research/framework-defects.md`. Every agent gets its own staging folder; a non-implementing role writes no
repository file; an agent claiming a finding covers "every" case lists the cases; a `failure-envelope.json` of class
`gate-approval-required` in a phase folder is stale once that phase's dispatch prompt exists (Finding 11).

## Acceptance criteria

_To be defined by the scope phase and approved at the Scope Gate._
