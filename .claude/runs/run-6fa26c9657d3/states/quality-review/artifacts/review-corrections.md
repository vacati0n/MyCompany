# MC-18 review corrections — run-6fa26c9657d3

Recorded by the orchestrator after the quality review RP-2026-003 (verdict approve-with-corrections). All six
corrections were applied by dev-1-implement in commit 58a22c5 on the MC-18 session branch, with no new file and no
package; the change account IR-2026-0035 lists them under "Review corrections".

| CR | Finding | Resolution | Evidence |
|---|---|---|---|
| CR-001 (blocking) | F-001 image-phase stops did not end the read | Every image-phase stop ends the read at that image with no further request and no retry: 403, 429, 5xx, other status, a redirect (image-redirect; sign-in-redirect or captcha when it points at those paths), a CAPTCHA or sign-in page, a non-image, an over-size image. The filled facts and the images already saved (the product's own declared images, signature-checked) are kept; the stopping image and every later one are named for upload at step 2. | 58a22c5; program case 063; case 054 updated; mutation check: ProductRead.cs as in 6ab01fb fails case 063 |
| CR-002 | F-002 case 008 failed open | Case 008 denies every networking namespace and outbound type except the exact set ReadSender.cs declares, by metadata and by source scan of every other file, with a self-test. | 58a22c5; case 008; mutation check: a planted TcpClient outside ReadSender.cs fails case 008 |
| CR-003 | F-003 host and hop edges untested | Cases for an IDN look-alike, IPv6 literals, explicit :443, affiliate subdomains, upper-case and trailing-dot hosts, hops to http and /verify/, and non-public addresses refused at connect. | 58a22c5; program case 065 |
| CR-004 | F-004 stops and bounds untested | Cases for page over 5 MiB, non-HTML page, 404, 204, a redirect without an address, and the 120 s click bound (read-time-over); image bounds in case 063. | 58a22c5; program cases 064 and 063 |
| CR-005 | F-005 guard on new routes untested | A foreign, other-port, missing or "null" Origin and a missing or wrong token are refused with 403 on /api/read, /api/paste, /api/read-confirm and /api/read-switch, with no read, fill, confirmation or switch change. | 58a22c5; program case 066 |
| CR-006 | F-006 "Unverified areas: none" overstated | The change account's verification method now records the correction; each area F-003 to F-005 named has an executed check. | IR-2026-0035, Verification Results and Review corrections |

Not changed (reviewer residual risks, carried to the release note as known limits): the IPv6 prefixes 64:ff9b::/96
and 2002::/16 are not classed as not public; any host under .susercontent.com is an image host; turning the switch off
during a read does not stop that read; a trailing-dot link is read and recorded with the dot; the source scan cannot
see a networking name assembled from string pieces at run time.

Open questions closed by the orchestrator for qa (as in MC-17):
- Q-001 (case 008 narrowed against SC-17 "additions only"): accepted, now that CR-002 makes it fail closed.
- Q-002 (case 061 runs read plus script; produce under the harness stays covered by case 042, SC-26): accepted. The
  toolchain is byte-unchanged and the script verb starts the same executable produce does.
- Q-003 (who records SC-34 and SC-33): the orchestrator; see `orchestrator-browser-and-live-read-record.md` in this
  folder.

Verification after corrections: see the test rerun at commit 58a22c5 recorded with the Review Gate decision.
`src/MediaCompany.Pilot` has no change in any MC-18 commit (`git log f61d10f~1..HEAD -- src/MediaCompany.Pilot/` is empty).
