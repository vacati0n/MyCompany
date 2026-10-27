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
