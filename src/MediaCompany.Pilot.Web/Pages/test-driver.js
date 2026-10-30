// Recording-test driver. Served only when the app was started with --test-mode by the test program; the browser cannot turn it on.
// It drives the real page with the browser's fake microphone: record until the hard stop, accept, play back to the end, record again
// for about three seconds, stop by hand, accept, and post what it saw to /test/result. It sends nothing anywhere else.
"use strict";

(function () {
  const match = /^#test-record=([^&]+)$/.exec(location.hash);
  if (!match) return;
  const code = decodeURIComponent(match[1]);
  const $ = (id) => document.getElementById(id);
  const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  const token = document.querySelector('meta[name="mc-token"]').content;
  const log = [];

  async function until(test, limitMs, what) {
    const started = performance.now();
    while (!test()) {
      if (performance.now() - started > limitMs) throw new Error("timed out waiting for " + what + " (state " + window.MC17.state + ")");
      await sleep(100);
    }
  }

  async function post(result) {
    await fetch("/test/result", {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-MC-Token": token },
      body: JSON.stringify(result),
    });
  }

  async function playToEnd() {
    const audio = $("rec-audio");
    await until(() => audio.src && audio.readyState >= 1, 10000, "playback metadata");
    audio.muted = true;
    audio.playbackRate = 16;
    const ended = new Promise((resolve) => audio.addEventListener("ended", resolve, { once: true }));
    await audio.play();
    await Promise.race([ended, sleep(30000)]);
    return { ended: audio.ended, currentTime: audio.currentTime };
  }

  async function run() {
    const result = { ok: false, log };
    try {
      await until(() => window.MC17 && window.MC17.ready, 20000, "page ready");
      const MC17 = window.MC17;
      if (!(await MC17.openVideo(code))) throw new Error("video did not open");
      MC17.showStep(4);
      result.limit = MC17.limit;

      // First take: the page must stop it at the limit by itself.
      $("rec-start").click();
      await until(() => MC17.state === "recording", 10000, "recording start");
      log.push("first take started");
      await until(() => MC17.state === "ready" || MC17.state === "error", (MC17.limit + 30) * 1000, "first take stop");
      result.first = Object.assign({}, MC17.lastTake);
      result.firstTimer = $("rec-timer").textContent;
      log.push("first take stopped by " + (MC17.lastTake && MC17.lastTake.stoppedBy));
      result.firstPlayback = await playToEnd();
      $("rec-accept").click();
      await until(() => MC17.state === "accepted" || MC17.state === "ready", 60000, "first accept");
      await until(() => MC17.state !== "accepting", 60000, "first accept end");
      result.firstAccept = MC17.lastAccept;
      result.firstCurrent = $("rec-current").textContent;

      // Re-record: about three seconds, stopped by hand.
      MC17.lastAccept = null;
      $("rec-start").click();
      await until(() => MC17.state === "recording", 10000, "second recording start");
      await sleep(3000);
      $("rec-stop").click();
      await until(() => MC17.state === "ready" || MC17.state === "error", 30000, "second take stop");
      result.second = Object.assign({}, MC17.lastTake);
      result.secondPlayback = await playToEnd();
      $("rec-accept").click();
      await until(() => MC17.state === "accepted", 60000, "second accept");
      result.secondAccept = MC17.lastAccept;
      result.secondCurrent = $("rec-current").textContent;

      // The step-7 bold rendering, on a sample that also carries markup the page must show as text.
      const sample = document.createElement("div");
      sample.append(...MC17.richText("Kiểm **giá** và <b>x</b> **đậm"));
      result.richSample = sample.innerHTML;
      result.ok = true;
    } catch (error) {
      result.error = String(error && error.message || error);
    }
    await post(result);
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", run);
  else run();
})();

// Read-test driver (MC-18). Served only in test mode, like the recording driver above. Against the app started by the test program
// with its loopback fixture, it drives step 1 as the operator would: with the switch off it notes the read part is hidden; it turns the
// switch on in the settings view; it reads the link, fills only the operator's own fields, picks three read images, confirms all but
// one mark and saves (expecting the refusal), confirms the last and saves; then it pastes the page text on a second video. It posts
// what it saw to /test/result and sends nothing anywhere else.
(function () {
  const match = /^#test-read=([^&]+)&paste=([^&]+)&link=([^&]+)&text=([^&]+)$/.exec(location.hash);
  if (!match) return;
  const code = decodeURIComponent(match[1]);
  const pasteCode = decodeURIComponent(match[2]);
  const link = decodeURIComponent(match[3]);
  const text = decodeURIComponent(match[4]);
  const $ = (id) => document.getElementById(id);
  const sleep = (ms) => new Promise((resolve) => setTimeout(resolve, ms));
  const token = document.querySelector('meta[name="mc-token"]').content;
  const log = [];
  const marks = () => [...document.querySelectorAll("#step-1 .fill-mark")];
  const confirms = () => [...document.querySelectorAll("#step-1 .fill-mark button.confirm")];

  async function until(test, limitMs, what) {
    const started = performance.now();
    while (!test()) {
      if (performance.now() - started > limitMs) throw new Error("timed out waiting for " + what);
      await sleep(100);
    }
  }

  function type(input, value) {
    input.value = value;
    input.dispatchEvent(new Event("input", { bubbles: true }));
  }

  async function save() {
    window.MC17.lastSave = null;
    $("facts-save").click();
    await until(() => window.MC17.lastSave, 60000, "save answer");
    const reply = window.MC17.lastSave;
    return { ok: !!reply.ok, code: reply.outcome ? reply.outcome.code : "" };
  }

  async function confirmOne() {
    const before = confirms().length;
    confirms()[0].click();
    await until(() => confirms().length === before - 1, 10000, "confirmation " + before);
  }

  async function run() {
    const result = { ok: false, log };
    try {
      await until(() => window.MC17 && window.MC17.ready, 20000, "page ready");
      const MC17 = window.MC17;
      if (!(await MC17.openVideo(code))) throw new Error("video did not open");
      MC17.showStep(1);
      result.offLinkHidden = $("read-link-part").hidden;
      result.offNoteShown = !$("read-off-note").hidden;

      // The switch, in the settings view.
      $("nav-settings").click();
      await until(() => !$("view-settings").hidden, 10000, "settings view");
      result.settingsBefore = $("settings-read").checked;
      $("settings-read").checked = true;
      $("settings-read").dispatchEvent(new Event("change"));
      await until(() => $("settings-outcome").children.length > 0, 10000, "switch answer");
      result.settingsMessage = $("settings-outcome").textContent;
      if (!(await MC17.openVideo(code))) throw new Error("video did not reopen");
      MC17.showStep(1);
      result.onLinkShown = !$("read-link-part").hidden;

      // One read click.
      MC17.lastRead = null;
      $("read-link").value = link;
      $("read-run").click();
      await until(() => MC17.lastRead, 150000, "read answer");
      const fill = MC17.lastRead.fill;
      result.readOk = !!MC17.lastRead.ok;
      if (!fill) throw new Error("the read filled nothing: " + JSON.stringify(MC17.lastRead.outcome));
      result.fields = fill.fields.length;
      result.candidates = fill.candidates.length;
      result.images = fill.images.length;
      result.expectedMarks = fill.fields.length + Math.min(3, fill.candidates.length);
      await sleep(300);
      result.marksAfterRead = marks().length;
      result.markText = marks().length > 0 ? marks()[0].textContent : "";
      result.typedReadFields = fill.fields.filter((f) => $("fh-" + f.member).value.trim().normalize("NFC") !== f.value.trim().normalize("NFC")).length;

      // Only the operator's own fields are typed.
      type($("fh-subjectNoun"), "bình giữ nhiệt");
      type($("fh-shopNameAndMall"), "Gian hàng mẫu, không phải Mall");
      type($("fh-niche"), "đồ dùng nhà bếp");
      type($("fh-productVideoNumber"), "1");
      type($("fh-videoFocus"), "dung tích");
      for (const box of $("facts-rows").children) {
        type(box.querySelector('input[data-member="subjectNoun"]'), "bình giữ nhiệt");
        type(box.querySelector('input[data-member="screenshot"]'), "chup-1.png");
      }
      result.marksAfterTyping = marks().length;

      // Three read images picked; a fourth is refused.
      const boxes = [...$("read-images").querySelectorAll("input[type=checkbox]")];
      for (const box of boxes.slice(0, 3)) box.click();
      if (boxes.length > 3) {
        boxes[3].click();
        result.fourthPicked = boxes[3].checked;
      }
      result.imagesUsed = $("fh-imagesUsed").value;
      result.thumbnails = [...$("read-images").querySelectorAll("img")].filter((img) => img.complete && img.naturalWidth > 0).length;

      // All marks but one confirmed: the save is refused. The last confirmed: the save is accepted.
      while (confirms().length > 1) await confirmOne();
      result.confirmsLeft = confirms().length;
      result.saveBefore = await save();
      await confirmOne();
      result.saveAfter = await save();
      result.marksConfirmed = marks().filter((m) => m.classList.contains("confirmed")).length;

      // Paste mode on a second video: the same marks, named for the pasted text.
      if (!(await MC17.openVideo(pasteCode))) throw new Error("second video did not open");
      MC17.showStep(1);
      MC17.lastPaste = null;
      $("paste-part").open = true;
      $("paste-text").value = text;
      $("paste-run").click();
      await until(() => MC17.lastPaste, 20000, "paste answer");
      result.pasteOk = !!MC17.lastPaste.ok;
      const pasted = MC17.lastPaste.fill;
      result.pasteExpectedMarks = pasted ? pasted.fields.length + Math.min(3, pasted.candidates.length) : -1;
      await sleep(300);
      result.pasteMarks = marks().length;
      result.pasteMarkText = marks().length > 0 ? marks()[0].textContent : "";
      result.ok = true;
    } catch (error) {
      result.error = String(error && error.message || error);
    }
    await fetch("/test/result", {
      method: "POST",
      headers: { "Content-Type": "application/json", "X-MC-Token": token },
      body: JSON.stringify(result),
    });
  }

  if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", run);
  else run();
})();
