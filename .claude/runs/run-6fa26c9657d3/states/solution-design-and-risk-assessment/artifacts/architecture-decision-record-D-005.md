# Architecture Decision Record

## Metadata

- ADR ID: D-005
- Title: Which hosts the read may fetch product images from
- Date: 2026-10-27
- Status: Proposed
- Owners: tech-lead (accepting Design Gate owner under the producer exclusion rule); the CEO decides at the Design Gate
- Related Work Items: MC-18, run run-6fa26c9657d3, technical design TD-MC18-READ-LINK, planner task T-006; open question Q-003

## Context

- Problem statement: CEO decision 1314 item 1 allows image files of the supplied product from Shopee image hosts outside shopee.vn, for example hosts ending .susercontent.com, image files only, never a page; SC-04 to SC-06 require only the product's own images and an upload offer for any image on a disallowed host (C-004, C-015).
- Business and technical constraints: C-004, C-009, C-015, C-024.
- Current architecture baseline: step 2 accepts only images the saved sheet names, as PNG or JPEG (F-006); screenshots wait per code and are placed on save (F-011).
- Choice for the CEO, in plain words:
  - Option A (proposed, O-015): the read fetches only the pictures the product page itself lists as this product's images, from shopee.vn or hosts ending ".susercontent.com". Any other picture is named so you can upload it at step 2.
  - Option B (O-017): shopee.vn hosts only. Safer on paper, but Shopee's real pictures are likely elsewhere, so you would upload most pictures by hand.
  - Option C is not offered: any host the page names would break your rule.
  - Separate question Q-003: is ".susercontent.com" enough, or should other Shopee image hosts be named?

## Decision

- Selected option: O-015.
- Decision statement: image files only, from shopee.vn subdomains and hosts ending .susercontent.com, only the addresses the product's structured block or page image tag declares; never a page from an image host; no redirect followed for an image; each image checked by its content signature, named by the app and bounded in size and count.
- Scope of impact: M-001, M-004, M-016.

## Alternatives Considered

1. O-016: any host the page names for an image
- Benefits: every picture arrives.
- Risks: other hosts and other products' images.
- Why not selected: eliminated by C-015.

2. O-017: shopee.vn subdomains only
- Benefits: the narrowest host list.
- Risks: real images are named for upload (A-006).
- Why not selected: ties O-015 through migration burden and loses at operability.

## Consequences

- Positive outcomes expected: SC-04 to SC-06 hold; own images arrive with the read when the page declares them.
- Tradeoffs accepted: an image the page shows but does not declare is not fetched (A-004).
- Risks introduced: R-010, R-017, R-018.

## Validation Plan

- Metrics to monitor: requests per read against the declared image list; image digests against the fixture.
- Verification checkpoints: P-005 (image saver), P-009 (SC-04, SC-05, SC-06, SC-18 cases).
- Rollback or reversal conditions: add names to the held list if the CEO answers Q-003 with more hosts or the single live read shows others; move to O-017 if the CEO withdraws the image-host allowance.

## Approval

- Architect: unsigned (producer; excluded from accepting)
- Tech Lead: unsigned
- Product Owner (if scope-impacting): unsigned; the CEO answers Q-003
