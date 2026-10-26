# Architecture Decision Record: D-002

## Metadata

- ADR ID: D-002
- Title: Vertical render route through the installed media tool only
- Status: Proposed
- Date: 2026-10-26
- Owners: architect (producer); tech-lead (accepting Design Gate owner)
- Related package: technical-design.md, run run-535f3f938f32, selected option O-002, deferred item 5

## Context

- The video is 1080x1920 at 9:16 and contains:
  - images that move
  - the presenter clip in every sampled frame
  - captions inside their windows
  - the disclosure overlay in every frame (C-007, C-008, C-009)
- The sample run must open no socket (C-014). The delivered renderer must be reused, and no second renderer may be added (C-017).
- The delivered production path already renders through the installed media tool, which its settings name "the renderer executable (the external media tool)" (F-006). It encodes with fixed settings: frame rate, size, yuv420p and the fast-start flag (F-007).
- The browser path, render.mjs, serves a bundle and drives a browser (F-009). These are assumed to open loopback sockets (A-001). OffthreadVideo's compositor is installed but has not been measured under render.mjs (F-011).
- The installed media tool has these filters (F-014):
  - zoom-pan
  - text-drawing
  - subtitle
- The delivered text-drawing pattern reads text from a file, with expansion off and a staged face (F-005).
- Coverage of the installed face is complete for Vietnamese (F-015). Mark stacking still needs a drawn frame.

## Decision

- M-001 composes each video by one media tool invocation (M-013) at the configured frame rate, with the delivered encoder settings (F-007), at 1080x1920.
- Layout, top to bottom:
  - an overlay band carrying the disclosure in every frame
  - an image panel with the product images (motion per D-009 in the package) and a portrait presenter inset that overlaps neither band
  - a caption band in the lower third: white text on dark, drawn per sentence window from text files with expansion off and the staged face
- The recording is the audio track and the clip's own audio is ignored. The video's length equals the recording's length. A clip whose length differs by more than the configured tolerance is refused (Q-003).
- No Node process, browser or OffthreadVideo is started. render.mjs (M-009) and graphics/drawing (M-010) are unchanged.
- One 1080x1920 frame with stacked marks is drawn and probed before composition is relied on (P-003).
- Fallback: if that frame fails A-003, the route moves to O-003, where browser-drawn text plates are composed by the media tool. This happens only when the CEO accepts loopback sockets (Q-005).

## Alternatives Considered

- O-001: a Remotion composition with OffthreadVideo through an extended render.mjs. It gives full browser text shaping. It opens loopback sockets (C-014, A-001), changes render.mjs (M-009), and its RK-016 proof needs a full composition render (C-024). Eliminated.
- O-003: a hybrid in which the browser draws caption and overlay plates and the media tool composes. It confines the browser to text. It still opens loopback sockets (C-014). It is held as the fallback under Q-005. Eliminated as primary.
- O-004: a Python driver of the same media tool. It has the same render route, but cannot reuse FontCoverage (C-017). Eliminated.
- O-005: the same route inside MediaCompany.Production. Eliminated on C-015 and C-018 (F-004, F-017).

## Consequences

- Positive: C-014 and C-015 hold by construction. Each run starts one executable. The RK-016 part 2 proof is a single frame (C-024). The delivered encoder settings are reused (C-017).
- Negative: there is no browser text shaping, so correct marks rest on NFC precomposed letters (A-003, R-003). Filter descriptions must be built from configured numbers only (R-007).
- Risk: if product-owner reads scope decision 1 as the browser renderer only (A-002, R-001), no option meets every hard constraint (Q-001).

## Validation Plan

- Metrics to monitor:
  - Probed width, height and aspect of every package video (target 1080, 1920, 9:16).
  - Socket count of the sample run (target 0).
  - Drawn frames whose marks are mis-stacked, by review of the P-003 frame (target 0).
- Verification checkpoints:
  - P-003 frame drawn and probed before P-006.
  - Integration tests for presenter and overlay presence at 0.5 s steps, and for caption windows (M-002).
- Rollback or reversal conditions:
  - The P-003 frame fails: switch to O-003 if Q-005 is accepted, otherwise stop composition until the CEO names a component.
  - A no answer to Q-001: record a superseding decision after the CEO relaxes either scope decision 1 or criterion 34.

## Approval

- Architect: unsigned; producer, excluded from accepting
- Tech Lead: unsigned; Design Gate decision pending
