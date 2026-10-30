// Thí điểm video MediaCompany: trang một màn hình, chỉ nói chuyện với chính ứng dụng trên máy này.
// Mọi chữ hiện ra đều đặt bằng textContent; không chèn HTML từ dữ liệu.
"use strict";

(function () {
  const token = document.querySelector('meta[name="mc-token"]').content;
  const $ = (id) => document.getElementById(id);
  const el = (tag, props, children) => {
    const node = document.createElement(tag);
    Object.assign(node, props || {});
    for (const child of children || []) node.append(child);
    return node;
  };
  const seconds = (s) => Number(s).toFixed(1).replace(".", ",");

  // Chữ đậm kiểu **...** trong chữ của mẫu 04: phần giữa hai dấu ** thành chữ đậm, đặt bằng textContent; dấu ** lẻ bị bỏ.
  // Không có HTML nào được dựng từ dữ liệu.
  function richText(text) {
    const parts = String(text === undefined || text === null ? "" : text).split("**");
    const nodes = [];
    parts.forEach((part, i) => {
      if (part.length === 0) return;
      const bold = i % 2 === 1 && i < parts.length - 1;
      nodes.push(bold ? el("b", { textContent: part }) : document.createTextNode(part));
    });
    return nodes;
  }

  const MC17 = {
    ready: false,
    code: null,
    step: 1,
    limit: 60,
    ceoName: "",
    state: "idle",
    lastTake: null,
    lastAccept: null,
    lastUpload: null,
    status: null,
  };
  window.MC17 = MC17;

  // ---------------------------------------------------------------------------------------------
  // Gọi ứng dụng
  // ---------------------------------------------------------------------------------------------

  async function api(method, path, body, contentType) {
    const headers = {};
    if (method !== "GET") headers["X-MC-Token"] = token;
    let payload;
    if (body !== undefined) {
      if (body instanceof Blob) {
        payload = body;
        headers["Content-Type"] = contentType || body.type || "application/octet-stream";
      } else {
        payload = JSON.stringify(body);
        headers["Content-Type"] = "application/json";
      }
    }
    let response;
    try {
      response = await fetch(path, { method, headers, body: payload, cache: "no-store", credentials: "same-origin" });
    } catch (error) {
      return { ok: false, outcome: appFault("Không gọi được ứng dụng. Có thể cửa sổ ứng dụng đã bị đóng. Mở lại ứng dụng bằng biểu tượng trên màn hình nền rồi tải lại trang.") };
    }
    try {
      return await response.json();
    } catch (error) {
      return { ok: false, outcome: appFault("Ứng dụng trả lời không đúng dạng (mã " + response.status + "). Tải lại trang (phím F5) rồi làm lại.") };
    }
  }

  function appFault(text) {
    return { kind: "loi-ung-dung", code: "app-unreachable", line: text, reason: "", nextStep: "" };
  }

  const q = (path) => path + (path.includes("?") ? "&" : "?") + "code=" + encodeURIComponent(MC17.code);

  // ---------------------------------------------------------------------------------------------
  // Hiển thị kết quả: loại, dòng của bộ công cụ, lý do, việc cần làm
  // ---------------------------------------------------------------------------------------------

  const kindTitle = { "dat": "Đạt", "giu-lai": "Giữ lại", "tu-choi": "Từ chối", "loi-cong-cu": "Lỗi công cụ", "loi-ung-dung": "Lỗi ứng dụng" };

  function showOutcome(target, outcome, extraLines) {
    const box = typeof target === "string" ? $(target) : target;
    box.replaceChildren();
    if (!outcome) return;
    const node = el("div", { className: "outcome " + outcome.kind });
    node.append(el("div", { className: "title", textContent: kindTitle[outcome.kind] || outcome.kind }));
    if (outcome.line) node.append(el("div", { className: "line", textContent: outcome.line.trim() }));
    if (outcome.reason) node.append(el("div", { textContent: "Lý do: " + outcome.reason }));
    if (outcome.nextStep) node.append(el("div", { textContent: "Việc cần làm: " + outcome.nextStep }));
    for (const line of extraLines || []) node.append(el("div", { textContent: line }));
    box.append(node);
  }

  function showMessage(target, text) {
    showOutcome(target, { kind: "dat", code: "", line: "", reason: "", nextStep: "" }, [text]);
  }

  function showReply(target, reply, okText) {
    if (reply.outcome) showOutcome(target, reply.outcome);
    else if (reply.ok) showMessage(target, okText || reply.message || "Xong.");
  }

  // ---------------------------------------------------------------------------------------------
  // Cài đặt
  // ---------------------------------------------------------------------------------------------

  function showView(name) {
    for (const view of document.querySelectorAll(".view")) view.hidden = view.id !== "view-" + name;
    $("top-nav").hidden = name === "setup";
  }

  async function loadSetup() {
    const reply = await api("GET", "/api/setup");
    if (!reply.ok) {
      showView("setup");
      showOutcome("setup-outcome", reply.outcome);
      return false;
    }
    if (reply.configured) return true;
    showView("setup");
    $("setup-folder").value = reply.pilotFolder || "";
    $("setup-ceo").value = reply.ceoName || "";
    const list = $("setup-numbers");
    list.replaceChildren();
    if (reply.problem) showOutcome("setup-problem", { kind: "tu-choi", code: "", line: reply.problem, reason: "", nextStep: "Báo người phụ trách kỹ thuật." });
    if (reply.ceoNumbers) {
      const n = reply.ceoNumbers;
      for (const text of [
        "Khung hình: " + n.frame,
        "Vùng an toàn: " + n.safeArea,
        "Cỡ chữ phụ đề: " + n.captionFontSize + "; cỡ chữ chú thích: " + n.overlayFontSize,
        "Phim người dẫn lệch bản ghi tối đa: " + String(n.clipToleranceSeconds).replace(".", ",") + " giây",
        "Mỗi trang phụ đề ít nhất: " + String(n.captionPageMinimumSeconds).replace(".", ",") + " giây",
        "Bản ghi giọng dài tối đa: " + n.recordingMaximumSeconds + " giây",
      ]) list.append(el("li", { textContent: text }));
    }
    return false;
  }

  async function saveSetup() {
    const reply = await api("POST", "/api/setup", {
      pilotFolder: $("setup-folder").value,
      ceoName: $("setup-ceo").value,
      createShortcut: $("setup-shortcut").checked,
    });
    if (!reply.ok) {
      showOutcome("setup-outcome", reply.outcome);
      return;
    }
    showMessage("setup-outcome", reply.message + (reply.shortcut ? " Đã tạo biểu tượng: " + reply.shortcut : ""));
    await loadHome();
  }

  // ---------------------------------------------------------------------------------------------
  // Danh sách video
  // ---------------------------------------------------------------------------------------------

  let catalogue = null;

  async function loadHome() {
    const reply = await api("GET", "/api/videos");
    if (!reply.ok) {
      showOutcome("global-outcome", reply.outcome);
      return;
    }
    catalogue = reply;
    MC17.limit = reply.recordingMaximumSeconds;
    MC17.ceoName = reply.ceoName;
    $("rec-limit").textContent = String(reply.recordingMaximumSeconds);
    const list = $("video-list");
    list.replaceChildren();
    if (reply.codes.length === 0) list.append(el("li", { textContent: "Chưa có video nào. Gõ mã video mới ở dưới." }));
    for (const code of reply.codes) {
      const button = el("button", { type: "button", className: "link", textContent: code });
      button.addEventListener("click", () => openVideo(code));
      list.append(el("li", {}, [button]));
    }
    $("nav-video").textContent = "";
    showView("home");
  }

  // ---------------------------------------------------------------------------------------------
  // Một video
  // ---------------------------------------------------------------------------------------------

  async function openVideo(code) {
    if (!catalogue) await loadHome();
    MC17.code = code;
    $("global-outcome").replaceChildren();
    const status = await api("GET", q("/api/video"));
    if (!status.ok) {
      showOutcome("global-outcome", status.outcome);
      return false;
    }
    MC17.status = status;
    $("nav-video").textContent = " › Video " + status.code;
    showView("video");
    renderFacts(status);
    renderRead(status);
    renderImages(status);
    renderScript(status.script, status.scriptTicks);
    renderRecording(status);
    renderClip(status);
    markSteps(status);
    showStep(firstOpenStep(status));
    return true;
  }

  async function refreshStatus() {
    const status = await api("GET", q("/api/video"));
    if (status.ok) {
      MC17.status = status;
      markSteps(status);
    }
    return status;
  }

  function firstOpenStep(s) {
    if (!s.facts) return 1;
    if (s.images.some((i) => !i.present)) return 2;
    if (!s.script || s.scriptTicks.length !== s.script.sentences.length) return 3;
    if (!s.recording) return 4;
    if (!s.clip) return 5;
    if (!s.lastPackage) return 6;
    return 7;
  }

  function markSteps(s) {
    const done = {
      1: !!s.facts,
      2: !!s.facts && s.images.every((i) => i.present),
      3: !!s.script && s.scriptTicks.length === s.script.sentences.length,
      4: !!s.recording,
      5: !!s.clip,
      6: !!s.lastPackage,
      7: false,
    };
    for (const button of document.querySelectorAll("#step-tabs button")) {
      button.classList.toggle("done", !!done[button.dataset.step]);
    }
  }

  function showStep(step) {
    MC17.step = Number(step);
    for (const node of document.querySelectorAll(".step")) node.hidden = node.id !== "step-" + step;
    for (const button of document.querySelectorAll("#step-tabs button")) button.classList.toggle("current", button.dataset.step === String(step));
    if (MC17.step === 6) checkProgress();
    if (MC17.step === 7) loadChecklist();
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 1: bảng thông tin và kiểm từ cấm ngay khi gõ
  // ---------------------------------------------------------------------------------------------

  const rowFieldsExtra = [["override", "câu thay thế (để trống nếu không cần)"]];

  function renderFacts(status) {
    const facts = status.facts || {};
    const header = $("facts-header");
    header.replaceChildren();
    for (const field of catalogue.headerFields) {
      const input = el("input", { type: field.member === "productVideoNumber" ? "number" : "text", id: "fh-" + field.member, autocomplete: "off" });
      input.dataset.where = field.label;
      let value = facts[field.member];
      if (field.member === "imagesUsed") value = Array.isArray(value) ? value.join(", ") : "";
      if (field.member === "videoCode") {
        value = MC17.code;
        input.readOnly = true;
      }
      input.value = value === undefined || value === null ? "" : String(value);
      const hint = field.member === "imagesUsed" ? " (tên tệp, cách nhau bằng dấu phẩy, ví dụ anh-1.jpg, anh-2.jpg)" : field.member === "readDate" ? " (năm-tháng-ngày, ví dụ 2026-10-27)" : "";
      header.append(el("label", { textContent: field.label + hint + ":" }, [input]));
      if (field.member !== "imagesUsed" && field.member !== "productVideoNumber" && field.member !== "videoCode") input.addEventListener("input", liveCheckSoon);
    }
    const cta = $("facts-cta");
    cta.replaceChildren();
    catalogue.callToAction.forEach((text, i) => cta.append(el("option", { value: String(i + 1), textContent: (i + 1) + ". " + text })));
    cta.value = String(facts.callToAction || 1);
    const rows = $("facts-rows");
    rows.replaceChildren();
    const list = Array.isArray(facts.facts) && facts.facts.length > 0 ? facts.facts : [{}];
    list.forEach((row) => addFactRow(row));
    $("facts-live").replaceChildren();
    $("facts-outcome").replaceChildren();
    liveCheckSoon();
  }

  function addFactRow(row) {
    const rows = $("facts-rows");
    if (rows.children.length >= 3) {
      showOutcome("facts-outcome", { kind: "tu-choi", code: "", line: "Bảng thông tin có tối đa 3 dòng.", reason: "", nextStep: "Bỏ bớt một dòng nếu cần thêm dòng khác." });
      return;
    }
    const index = rows.children.length + 1;
    const box = el("div", { className: "fact-row" });
    box.append(el("h4", { textContent: "Dòng F" + index }));
    for (const [member, label] of catalogue.rowFields.map((f) => [f.member, f.label]).concat(rowFieldsExtra)) {
      const input = el("input", { type: "text", className: "wide", autocomplete: "off" });
      input.dataset.member = member;
      input.value = row[member] || "";
      const lineLabel = el("label", { textContent: label + ":" }, [input]);
      if (member === "screenshot") {
        const file = el("input", { type: "file", accept: ".png,.jpg,.jpeg" });
        file.addEventListener("change", () => uploadScreenshot(file, input));
        lineLabel.append(el("span", { className: "small", textContent: " Chọn ảnh chụp màn hình: " }), file);
      } else {
        input.addEventListener("input", liveCheckSoon);
      }
      box.append(lineLabel);
    }
    const remove = el("button", { type: "button", textContent: "Bỏ dòng này" });
    remove.addEventListener("click", () => {
      if (rows.children.length > 1) {
        box.remove();
        renumberRows();
        liveCheckSoon();
      }
    });
    box.append(remove);
    rows.append(box);
  }

  function renumberRows() {
    [...$("facts-rows").children].forEach((box, i) => { box.querySelector("h4").textContent = "Dòng F" + (i + 1); });
  }

  async function uploadScreenshot(fileInput, nameInput) {
    const file = fileInput.files[0];
    if (!file) return;
    const reply = await api("POST", q("/api/screenshot") + "&name=" + encodeURIComponent(file.name), file, "application/octet-stream");
    if (reply.ok) nameInput.value = reply.name;
    showReply("facts-outcome", reply);
  }

  let liveTimer = null;
  function liveCheckSoon() {
    clearTimeout(liveTimer);
    liveTimer = setTimeout(liveCheck, 250);
  }

  // Exactly the fields the toolchain's fact sheet check scans for banned terms (FactSheetCheck: header subject and classification;
  // each row's subject, attribute, value and override sentence), so the page never warns where the check would pass, or the reverse.
  const liveHeader = ["fh-subjectNoun", "fh-classification"];
  const liveRow = ["subjectNoun", "attribute", "value", "override"];

  function liveFields() {
    const fields = [];
    for (const input of $("facts-header").querySelectorAll("input")) {
      if (!liveHeader.includes(input.id)) continue;
      fields.push({ where: input.dataset.where, text: input.value, isOverride: false });
    }
    [...$("facts-rows").children].forEach((box, i) => {
      for (const input of box.querySelectorAll("input[type=text]")) {
        if (!liveRow.includes(input.dataset.member)) continue;
        const label = input.parentElement.firstChild.textContent.replace(/:$/, "");
        fields.push({ where: "Dòng F" + (i + 1) + ", " + label, text: input.value, isOverride: input.dataset.member === "override" });
      }
    });
    return fields;
  }

  async function liveCheck() {
    const reply = await api("POST", "/api/live-check", { fields: liveFields() });
    const box = $("facts-live");
    box.replaceChildren();
    if (reply.findings === undefined) {
      if (reply.outcome) showOutcome(box, reply.outcome);
      return;
    }
    for (const f of reply.findings) {
      box.append(el("div", { className: "finding", textContent: f.where + ": có từ “" + f.term + "” (nhóm " + f.classId + ": " + f.className + "). Bỏ hoặc thay từ này." }));
    }
    MC17.lastFindings = reply.findings;
  }

  function collectFacts() {
    const sheet = { formVersion: 1 };
    for (const field of catalogue.headerFields) {
      const raw = $("fh-" + field.member).value;
      if (field.member === "imagesUsed") sheet.imagesUsed = raw.split(",").map((s) => s.trim()).filter((s) => s.length > 0);
      else if (field.member === "productVideoNumber") sheet.productVideoNumber = raw.trim() === "" ? null : Number(raw);
      else sheet[field.member] = raw;
    }
    sheet.callToAction = Number($("facts-cta").value);
    sheet.facts = [...$("facts-rows").children].map((box, i) => {
      const row = { label: "F" + (i + 1) };
      for (const input of box.querySelectorAll("input[type=text]")) {
        const member = input.dataset.member;
        if (member === "override") {
          if (input.value.trim().length > 0) row.override = input.value;
        } else {
          row[member] = input.value;
        }
      }
      return row;
    });
    return sheet;
  }

  async function saveFacts() {
    const reply = await api("POST", q("/api/facts"), collectFacts());
    MC17.lastSave = reply;
    showReply("facts-outcome", reply);
    if (reply.ok) {
      const status = await refreshStatus();
      if (status.ok) renderImages(status);
    }
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 1 (MC-18): đọc trang sản phẩm, dán chữ, xác nhận từng ô, chọn ảnh đọc từ trang.
  // Chỉ đọc khi bấm nút "Đọc trang"; dán chữ hay gõ không bao giờ tự đọc.
  // ---------------------------------------------------------------------------------------------

  const same = (a, b) => String(a || "").trim().normalize("NFC") === String(b || "").trim().normalize("NFC");
  const sourceNames = { read: "Đọc từ trang", paste: "Từ chữ đã dán", button: "Từ nút đọc trang" };
  const sourceName = (fill) => sourceNames[fill && fill.source] || "Từ chữ đã dán";

  function renderRead(status) {
    MC17.readOn = !!status.readOn;
    $("read-link-part").hidden = !MC17.readOn;
    $("read-off-note").hidden = MC17.readOn;
    $("read-status").textContent = "";
    $("read-outcome").replaceChildren();
    $("paste-part").open = !MC17.readOn;
    MC17.fill = status.fill || null;
    renderFill();
  }

  // Đưa giá trị đã điền vào các ô đầu bảng; các dòng gợi ý đầu tiên vào các dòng thông tin còn trống (tối đa 3).
  function applyFill(fill) {
    for (const field of fill.fields) {
      const input = $("fh-" + field.member);
      if (input) input.value = field.value;
    }
    for (const candidate of fill.candidates.slice(0, 3)) useCandidate(candidate, true);
    const used = $("fh-imagesUsed");
    if (used) {
      const kept = used.value.split(",").map((s) => s.trim()).filter((s) => s.length > 0 && !s.startsWith("anh-doc-"));
      used.value = kept.join(", ");
    }
    liveCheckSoon();
  }

  function renderFill() {
    const fill = MC17.fill;
    $("read-found").hidden = !fill;
    const candidates = $("read-candidates");
    candidates.replaceChildren();
    $("read-reference").replaceChildren();
    $("read-images").replaceChildren();
    $("read-not-fetched").replaceChildren();
    $("read-not-found").textContent = "";
    $("read-images-part").hidden = true;
    $("button-shot").hidden = !(fill && fill.source === "button");
    if (!fill) {
      updateMarks();
      return;
    }
    if (fill.candidates.length === 0) candidates.append(el("li", { textContent: "Không tìm thấy dòng CHI TIẾT SẢN PHẨM nào. Gõ tay dòng thông tin." }));
    for (const candidate of fill.candidates) {
      const use = el("button", { type: "button", textContent: "Dùng dòng này" });
      use.addEventListener("click", () => { useCandidate(candidate, false); liveCheckSoon(); updateMarks(); });
      candidates.append(el("li", {}, [
        el("span", { textContent: candidate.attribute + ": " + candidate.value + " (" + candidate.sourceSection + ") " }),
        use,
      ]));
    }
    const reference = $("read-reference");
    if (fill.price || fill.description) {
      reference.append(el("h4", { textContent: "Chỉ để tham khảo (không đưa vào bảng thông tin)" }));
      if (fill.price) reference.append(el("p", { textContent: "Giá trên trang: " + fill.price }));
      if (fill.description) reference.append(el("p", { className: "description", textContent: "Mô tả sản phẩm: " + fill.description }));
    }
    if (fill.images.length > 0) {
      $("read-images-part").hidden = false;
      const picked = pickedImages();
      for (const image of fill.images) {
        const box = el("input", { type: "checkbox", checked: picked.includes(image.name) });
        box.dataset.name = image.name;
        box.addEventListener("change", () => pickImage(box));
        const thumb = el("img", { alt: image.name, src: q("/api/read-image") + "&name=" + encodeURIComponent(image.name) });
        $("read-images").append(el("label", { className: "read-image" }, [thumb, el("span", {}, [box, document.createTextNode(" " + image.name)])]));
      }
    }
    for (const skipped of fill.notFetched) {
      $("read-not-fetched").append(el("li", { textContent: "Chưa lấy ảnh " + skipped.address + ": " + skipped.reason + ". Tải ảnh này lên ở bước 2 nếu cần." }));
    }
    if (fill.notFound.length > 0) $("read-not-found").textContent = "Không tìm thấy trên trang: " + fill.notFound.join(", ") + ". Gõ tay các mục này.";
    updateMarks();
  }

  function pickedImages() {
    const used = $("fh-imagesUsed");
    return used ? used.value.split(",").map((s) => s.trim()).filter((s) => s.length > 0) : [];
  }

  function pickImage(box) {
    let names = pickedImages().filter((n) => n !== box.dataset.name);
    if (box.checked) {
      if (names.filter((n) => n.startsWith("anh-doc-")).length >= 3) {
        box.checked = false;
        showOutcome("read-outcome", { kind: "tu-choi", code: "", line: "Chỉ chọn tối đa 3 ảnh đọc từ trang.", reason: "", nextStep: "Bỏ chọn một ảnh khác trước." });
        return;
      }
      names = names.concat([box.dataset.name]);
    }
    $("fh-imagesUsed").value = names.join(", ");
  }

  // Một dòng gợi ý vào dòng thông tin trống đầu tiên, hoặc một dòng mới nếu còn chỗ (bảng có tối đa 3 dòng).
  function useCandidate(candidate, quiet) {
    const rows = [...$("facts-rows").children];
    const value = (box, member) => box.querySelector('input[data-member="' + member + '"]');
    if (rows.some((box) => same(value(box, "attribute").value, candidate.attribute) && same(value(box, "value").value, candidate.value))) return;
    let target = rows.find((box) => ["attribute", "value", "sourceSection"].every((m) => value(box, m).value.trim() === ""));
    if (!target) {
      if (rows.length >= 3) {
        if (!quiet) showOutcome("read-outcome", { kind: "tu-choi", code: "", line: "Bảng thông tin đã có 3 dòng.", reason: "", nextStep: "Bỏ một dòng ở dưới rồi bấm \"Dùng dòng này\" lại." });
        return;
      }
      addFactRow({});
      renumberRows();
      target = $("facts-rows").lastElementChild;
    }
    value(target, "attribute").value = candidate.attribute;
    value(target, "value").value = candidate.value;
    value(target, "sourceSection").value = candidate.sourceSection;
    const shot = value(target, "screenshot");
    if (MC17.fill && MC17.fill.source === "button" && MC17.buttonShot && shot.value.trim() === "") shot.value = MC17.buttonShot;
  }

  // Dấu "Đọc từ trang" hoặc "Từ chữ đã dán" cạnh mỗi ô còn đúng giá trị đã điền; ô đã sửa coi như gõ tay và không còn dấu.
  function updateMarks() {
    for (const old of document.querySelectorAll("#step-1 .fill-mark")) old.remove();
    const fill = MC17.fill;
    if (!fill) return;
    for (const field of fill.fields) {
      const input = $("fh-" + field.member);
      if (!input || !same(input.value, field.value)) continue;
      input.parentElement.append(mark(fill, field.confirmed, field.member));
    }
    for (const box of $("facts-rows").children) {
      const attribute = box.querySelector('input[data-member="attribute"]').value;
      const value = box.querySelector('input[data-member="value"]').value;
      const matches = fill.candidates.filter((c) => same(attribute, c.attribute) && same(value, c.value));
      if (matches.length === 0) continue;
      const confirmed = matches.some((c) => c.confirmed);
      box.querySelector("h4").append(mark(fill, confirmed, matches[0].key));
    }
  }

  function mark(fill, confirmed, key) {
    const node = el("span", { className: "fill-mark" + (confirmed ? " confirmed" : "") });
    node.dataset.key = key;
    node.append(el("span", { textContent: " " + sourceName(fill) + (confirmed ? " — đã xác nhận" : " ") }));
    if (!confirmed) {
      const button = el("button", { type: "button", className: "confirm", textContent: "Xác nhận" });
      button.addEventListener("click", (event) => { event.preventDefault(); confirmFill(key); });
      node.append(button);
    }
    return node;
  }

  async function confirmFill(key) {
    const reply = await api("POST", q("/api/read-confirm"), { field: key });
    if (!reply.ok) {
      showOutcome("read-outcome", reply.outcome);
      return;
    }
    MC17.fill = reply.fill;
    updateMarks();
  }

  async function readProduct() {
    $("read-run").disabled = true;
    $("read-status").textContent = "Đang đọc trang...";
    $("read-outcome").replaceChildren();
    const reply = await api("POST", q("/api/read"), { link: $("read-link").value });
    $("read-run").disabled = false;
    $("read-status").textContent = "";
    MC17.lastRead = reply;
    if (reply.fill) {
      MC17.fill = reply.fill;
      applyFill(reply.fill);
      renderFill();
    }
    if (reply.ok) {
      showMessage("read-outcome", reply.message);
      return;
    }
    showOutcome("read-outcome", reply.outcome, reply.offerPaste ? ["Ứng dụng không thử đọc lại. Dán chữ của trang vào ô \"Dán chữ của trang sản phẩm\", hoặc gõ tay."] : []);
    if (reply.offerPaste) $("paste-part").open = true;
  }

  async function pasteProduct() {
    const reply = await api("POST", q("/api/paste"), { text: $("paste-text").value });
    MC17.lastPaste = reply;
    if (!reply.ok) {
      showOutcome("read-outcome", reply.outcome);
      return;
    }
    MC17.fill = reply.fill;
    applyFill(reply.fill);
    renderFill();
    showMessage("read-outcome", reply.message);
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 1 (MC-19): khối thông tin của nút "Đọc trang Shopee", dán ở bất kỳ chỗ nào trên trang.
  // Chữ dán bắt đầu bằng dấu của khối được gửi ngay cho ứng dụng cùng mã video đang mở; chữ khác dán như bình thường.
  // ---------------------------------------------------------------------------------------------

  const BLOCK_MARKER = "MC19-NUT-DOC-TRANG";

  function onPaste(event) {
    const text = event.clipboardData ? event.clipboardData.getData("text/plain") : "";
    if (!text || !text.trimStart().startsWith(BLOCK_MARKER)) return;
    event.preventDefault();
    event.stopPropagation();
    pasteBlock(text);
  }

  async function pasteBlock(text) {
    const open = !$("view-video").hidden && !!MC17.code;
    const path = open ? "/api/paste?code=" + encodeURIComponent(MC17.code) : "/api/paste";
    if (open) showStep(1);
    const target = open ? "read-outcome" : "global-outcome";
    $(target).replaceChildren();
    if (open) $("read-status").textContent = "Đang điền từ nút đọc trang và lấy ảnh...";
    const reply = await api("POST", path, { text });
    if (open) $("read-status").textContent = "";
    MC17.lastBlock = reply;
    if (reply.fill) {
      MC17.fill = reply.fill;
      MC17.buttonShot = null;
      applyFill(reply.fill);
      renderFill();
    }
    if (reply.ok) {
      showMessage(target, reply.message);
      return;
    }
    showOutcome(target, reply.outcome);
  }

  // Một ảnh chụp cho mọi dòng điền từ nút (D-008): tải lên một lần, ghi vào mọi dòng từ nút còn trống ảnh chụp.
  async function uploadButtonShot() {
    const file = $("button-shot-file").files[0];
    if (!file) return;
    const reply = await api("POST", q("/api/screenshot") + "&name=" + encodeURIComponent(file.name), file, "application/octet-stream");
    showReply("facts-outcome", reply);
    if (!reply.ok) return;
    MC17.buttonShot = reply.name;
    const fill = MC17.fill;
    for (const box of $("facts-rows").children) {
      const attribute = box.querySelector('input[data-member="attribute"]').value;
      const value = box.querySelector('input[data-member="value"]').value;
      const shot = box.querySelector('input[data-member="screenshot"]');
      const fromButton = fill && fill.source === "button" && fill.candidates.some((c) => same(attribute, c.attribute) && same(value, c.value));
      if (fromButton && shot.value.trim() === "") shot.value = reply.name;
    }
  }

  // ---------------------------------------------------------------------------------------------
  // Cài đặt: bật hoặc tắt đọc trang sản phẩm
  // ---------------------------------------------------------------------------------------------

  async function openSettings() {
    const reply = await api("GET", "/api/read-switch");
    if (!reply.ok) {
      showOutcome("global-outcome", reply.outcome);
      return;
    }
    $("settings-read").checked = reply.readOn;
    $("settings-file").textContent = "Lưu ở tệp " + reply.switchFile;
    $("settings-outcome").replaceChildren();
    $("button-outcome").replaceChildren();
    const button = await api("GET", "/api/button");
    if (button.ok) {
      $("button-link").href = button.address;
      $("button-address").value = button.address;
      $("button-version").textContent = "Dạng khối " + button.formVersion + ", dấu của nút " + button.mark + ". Khi ứng dụng được cập nhật mà dấu này đổi, kéo nút mới lên thanh dấu trang thay cho nút cũ.";
    } else {
      showOutcome("button-outcome", button.outcome);
    }
    showView("settings");
  }

  async function setReadSwitch() {
    const reply = await api("POST", "/api/read-switch", { on: $("settings-read").checked });
    showReply("settings-outcome", reply);
    if (reply.ok) MC17.readOn = reply.readOn;
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 2: ảnh
  // ---------------------------------------------------------------------------------------------

  function renderImages(status) {
    const list = $("images-list");
    list.replaceChildren();
    if (!status.facts) {
      list.append(el("li", { textContent: "Chưa có bảng thông tin. Làm bước 1 trước." }));
      return;
    }
    for (const image of status.images) {
      const file = el("input", { type: "file", accept: ".png,.jpg,.jpeg" });
      file.addEventListener("change", () => uploadImage(image.name, file));
      list.append(el("li", {}, [
        el("b", { textContent: image.name }),
        el("span", { textContent: image.present ? " — đã có. Chọn lại nếu muốn thay: " : " — chưa có. Chọn ảnh: " }),
        file,
      ]));
    }
  }

  async function uploadImage(name, fileInput) {
    const file = fileInput.files[0];
    if (!file) return;
    const reply = await api("POST", q("/api/image") + "&name=" + encodeURIComponent(name) + "&source=" + encodeURIComponent(file.name), file, "application/octet-stream");
    showReply("images-outcome", reply);
    const status = await refreshStatus();
    if (status.ok) renderImages(status);
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 3: kịch bản, tích từng câu theo dòng nguồn
  // ---------------------------------------------------------------------------------------------

  let script = null;

  async function runScript() {
    $("script-run").disabled = true;
    $("script-outcome").replaceChildren(el("p", { className: "status", textContent: "Bộ công cụ đang kiểm và viết kịch bản..." }));
    const reply = await api("POST", q("/api/script"));
    $("script-run").disabled = false;
    showOutcome("script-outcome", reply.outcome);
    if (reply.ok && reply.extra) renderScript({ sha256: reply.extra.sha256, sentences: reply.extra.sentences }, (await refreshStatus()).scriptTicks || []);
  }

  function renderScript(value, ticks) {
    script = value;
    const list = $("script-sentences");
    list.replaceChildren();
    if (!value) {
      $("script-count").textContent = "";
      return;
    }
    for (const sentence of value.sentences) {
      const box = el("input", { type: "checkbox", checked: ticks.includes(sentence.number) });
      box.dataset.number = String(sentence.number);
      box.addEventListener("change", saveScriptTicks);
      list.append(el("li", {}, [
        el("label", { className: "check" }, [box, el("span", { textContent: sentence.text })]),
        el("div", { className: "source", textContent: "Nguồn: " + sentence.label + " — " + sentence.source }),
      ]));
    }
    countScript();
  }

  function scriptTicked() {
    return [...$("script-sentences").querySelectorAll("input:checked")].map((b) => Number(b.dataset.number));
  }

  function countScript() {
    if (!script) return;
    const n = scriptTicked().length;
    $("script-count").textContent = "Đã tích " + n + " trên " + script.sentences.length + " câu." + (n === script.sentences.length ? " Đủ; sang bước 4 để ghi âm." : "");
  }

  async function saveScriptTicks() {
    countScript();
    const reply = await api("POST", q("/api/script-ticks"), { scriptSha256: script.sha256, ticked: scriptTicked() });
    if (!reply.ok) showOutcome("script-outcome", reply.outcome);
    await refreshStatus();
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 4: ghi âm trong trình duyệt (dừng cứng ở giới hạn), nghe lại, ghi lại, hoặc tải lên
  // ---------------------------------------------------------------------------------------------

  const rec = { recorder: null, stream: null, chunks: [], startedAt: 0, timer: null, stopTimer: null, stoppedBy: null, elapsed: 0 };

  function setRecState(state, text) {
    MC17.state = state;
    $("rec-status").textContent = text || "";
    $("rec-start").disabled = state === "recording" || state === "uploading" || state === "accepting";
    $("rec-stop").disabled = state !== "recording";
    $("rec-accept").disabled = state !== "ready";
  }

  function chooseType() {
    for (const type of ["audio/webm;codecs=opus", "audio/webm", "audio/ogg;codecs=opus", "audio/mp4"]) {
      if (window.MediaRecorder && MediaRecorder.isTypeSupported(type)) return type;
    }
    return "";
  }

  async function startRecording() {
    $("rec-outcome").replaceChildren();
    if (!navigator.mediaDevices || !window.MediaRecorder) {
      setRecState("error", "Trình duyệt này không ghi âm được. Dùng Chrome hoặc Edge, hoặc tải bản ghi có sẵn lên ở phần dưới.");
      return;
    }
    try {
      rec.stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    } catch (error) {
      setRecState("error", "Không mở được micrô (" + error.name + "). Cho phép trang dùng micrô (biểu tượng ổ khóa cạnh địa chỉ), kiểm tra micrô đã cắm, rồi bấm lại.");
      return;
    }
    const type = chooseType();
    rec.recorder = type ? new MediaRecorder(rec.stream, { mimeType: type }) : new MediaRecorder(rec.stream);
    rec.chunks = [];
    rec.stoppedBy = null;
    rec.recorder.addEventListener("dataavailable", (event) => { if (event.data && event.data.size > 0) rec.chunks.push(event.data); });
    rec.recorder.addEventListener("stop", finishRecording);
    rec.recorder.start(1000);
    rec.startedAt = performance.now();
    $("rec-audio").hidden = true;
    $("rec-audio").removeAttribute("src");
    setRecState("recording", "Đang ghi... Đọc kịch bản. Ứng dụng tự dừng ở " + MC17.limit + " giây.");
    rec.timer = setInterval(() => { $("rec-timer").textContent = seconds((performance.now() - rec.startedAt) / 1000) + " giây"; }, 100);
    rec.stopTimer = setTimeout(() => stopRecording("gioi-han"), MC17.limit * 1000);
  }

  function stopRecording(by) {
    if (!rec.recorder || rec.recorder.state !== "recording") return;
    rec.stoppedBy = by;
    rec.elapsed = Math.min((performance.now() - rec.startedAt) / 1000, MC17.limit);
    clearTimeout(rec.stopTimer);
    clearInterval(rec.timer);
    rec.recorder.stop();
  }

  async function finishRecording() {
    for (const track of rec.stream.getTracks()) track.stop();
    $("rec-timer").textContent = seconds(rec.elapsed) + " giây";
    const blob = new Blob(rec.chunks, { type: rec.recorder.mimeType || "audio/webm" });
    const audio = $("rec-audio");
    audio.src = URL.createObjectURL(blob);
    audio.hidden = false;
    setRecState("uploading", (rec.stoppedBy === "gioi-han" ? "Đã tự dừng ở " + MC17.limit + " giây. " : "Đã dừng. ") + "Đang lưu bản ghi vào máy...");
    const reply = await api("POST", q("/api/take"), blob, blob.type.split(";")[0]);
    if (!reply.ok) {
      setRecState("error", "Bản ghi chưa được lưu.");
      showOutcome("rec-outcome", reply.outcome);
      return;
    }
    MC17.lastTake = { seconds: Math.round(rec.elapsed * 10) / 10, stoppedBy: rec.stoppedBy, sha256: reply.sha256, bytes: reply.bytes, type: blob.type };
    setRecState("ready", reply.message);
  }

  async function acceptTake() {
    setRecState("accepting", "Đang chuyển bản ghi sang dạng bộ công cụ nhận...");
    const reply = await api("POST", q("/api/take-accept"));
    if (!reply.ok) {
      setRecState("ready", "Bản ghi chưa được dùng.");
      showOutcome("rec-outcome", reply.outcome);
      return;
    }
    MC17.lastAccept = reply.recording;
    showCurrentRecording(reply.recording);
    setRecState("accepted", "Đã dùng bản ghi này.");
    showMessage("rec-outcome", reply.message);
    const status = await refreshStatus();
    if (status.ok) renderRecording(status);
  }

  async function uploadRecording() {
    const file = $("rec-upload").files[0];
    if (!file) return;
    $("rec-outcome").replaceChildren(el("p", { className: "status", textContent: "Đang tải bản ghi lên..." }));
    const reply = await api("POST", q("/api/recording") + "&name=" + encodeURIComponent(file.name), file, "application/octet-stream");
    MC17.lastUpload = reply;
    if (reply.ok && reply.recording) showCurrentRecording(reply.recording);
    showReply("rec-outcome", reply);
    $("rec-upload").value = "";
    const status = await refreshStatus();
    if (status.ok) renderRecording(status);
  }

  // Dòng "bản ghi đang dùng" đổi ngay khi ứng dụng nhận bản ghi, theo chính câu trả lời đó, rồi theo trạng thái video đọc lại.
  function showCurrentRecording(r) {
    $("rec-current").textContent = r ? "Bản ghi đang dùng: " + r.name + ", dài " + seconds(r.seconds) + " giây, SHA-256 " + r.sha256 + "." : "Video này chưa có bản ghi giọng.";
  }

  function renderRecording(status) {
    showCurrentRecording(status.recording);
    if (MC17.state === "idle" || MC17.state === "error") setRecState(MC17.state, $("rec-status").textContent);
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 5: phim người dẫn
  // ---------------------------------------------------------------------------------------------

  async function uploadClip() {
    const file = $("clip-upload").files[0];
    if (!file) return;
    $("clip-outcome").replaceChildren(el("p", { className: "status", textContent: "Đang tải và kiểm đoạn phim..." }));
    const reply = await api("POST", q("/api/clip") + "&name=" + encodeURIComponent(file.name), file, "application/octet-stream");
    showReply("clip-outcome", reply);
    $("clip-upload").value = "";
    const status = await refreshStatus();
    if (status.ok) renderClip(status);
  }

  function renderClip(status) {
    const c = status.clip;
    $("clip-current").textContent = c ? "Đoạn phim đang dùng: " + c.name + ", " + c.width + "x" + c.height + ", dài " + Number(c.seconds).toFixed(3).replace(".", ",") + " giây." : "Video này chưa có đoạn phim người dẫn.";
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 6: tạo video, tiến độ, chỉnh thời gian
  // ---------------------------------------------------------------------------------------------

  let progressTimer = null;

  async function startProduce() {
    $("produce-outcome").replaceChildren();
    $("produce-output").hidden = true;
    $("timing-editor").hidden = true;
    const reply = await api("POST", q("/api/produce"));
    if (!reply.ok) {
      showOutcome("produce-outcome", reply.outcome);
      return;
    }
    $("produce-run").disabled = true;
    checkProgress();
  }

  async function checkProgress() {
    clearTimeout(progressTimer);
    const reply = await api("GET", q("/api/progress"));
    if (!reply.ok) {
      showOutcome("produce-outcome", reply.outcome);
      return;
    }
    if (!reply.started) {
      $("produce-progress").textContent = "";
      $("produce-run").disabled = false;
      return;
    }
    if (reply.running) {
      $("produce-run").disabled = true;
      $("produce-progress").textContent = "Đang tạo video... đã " + seconds(reply.elapsedSeconds) + " giây. Giữ trang mở.";
      progressTimer = setTimeout(checkProgress, 1000);
      return;
    }
    $("produce-run").disabled = false;
    $("produce-progress").textContent = "Xong sau " + seconds(reply.elapsedSeconds) + " giây.";
    showOutcome("produce-outcome", reply.outcome, reply.package ? ["Gói: " + reply.package, "Bước tiếp: sang bước 7 để xem video và duyệt."] : []);
    if (reply.output) {
      $("produce-output").textContent = reply.output;
      $("produce-output").hidden = false;
    }
    MC17.lastProduce = reply;
    if (reply.timingEditor) await openTimingEditor(reply.timingEditor);
    await refreshStatus();
  }

  async function openTimingEditor(mode) {
    const reply = await api("GET", q("/api/timing"));
    if (!reply.ok) {
      showOutcome("timing-outcome", reply.outcome);
      return;
    }
    $("timing-hint").textContent = mode === "goi-y"
      ? "Có trang phụ đề hiện chưa đủ 1,2 giây. Bạn có thể ghi lại chậm hơn, hoặc chỉnh thời gian từng câu dưới đây cho câu đó dài hơn."
      : "Bộ công cụ không tìm được chỗ nghỉ giữa các câu khớp với kịch bản. Nghe bản ghi và ghi thời gian từng câu dưới đây, rồi tạo video lại.";
    $("timing-audio").src = q("/api/input/recording");
    const rows = $("timing-rows");
    rows.replaceChildren();
    reply.sentences.forEach((sentence, i) => {
      const parts = (reply.lines[i] || "").split(/\s+/);
      const start = el("input", { type: "text", value: parts[0] || "", className: "mono" });
      const end = el("input", { type: "text", value: parts[1] || "", className: "mono" });
      start.style.minWidth = end.style.minWidth = "90px";
      rows.append(el("tr", {}, [
        el("td", { textContent: String(sentence.number) }),
        el("td", { textContent: sentence.text }),
        el("td", {}, [start]),
        el("td", {}, [end]),
      ]));
    });
    $("timing-editor").hidden = false;
  }

  async function saveTiming() {
    const lines = [...$("timing-rows").children].map((tr) => {
      const inputs = tr.querySelectorAll("input");
      return inputs[0].value.trim().replace(",", ".") + " " + inputs[1].value.trim().replace(",", ".");
    });
    const reply = await api("POST", q("/api/timing"), { lines });
    showReply("timing-outcome", reply);
  }

  // ---------------------------------------------------------------------------------------------
  // Bước 7: xem, danh sách kiểm, duyệt bằng mã băm, kiểm lại
  // ---------------------------------------------------------------------------------------------

  let checklist = null;

  async function loadChecklist() {
    const reply = await api("GET", q("/api/checklist"));
    if (!reply.ok) {
      $("review-body").hidden = true;
      showOutcome("review-outcome", reply.outcome);
      return;
    }
    checklist = reply;
    $("review-outcome").replaceChildren();
    if (!reply.matchesHeld) showOutcome("review-outcome", { kind: "tu-choi", code: "checklist-differs", line: reply.problem || "", reason: "Danh sách kiểm của gói khác mẫu 04.", nextStep: "Không duyệt; tạo video lại ở bước 6." });
    if (reply.status !== "ready") showOutcome("review-outcome", { kind: "giu-lai", code: "package-held", line: (reply.heldReasons || []).join("; "), reason: "Gói đang bị giữ lại nên không duyệt được.", nextStep: "Sửa theo lý do trên rồi tạo video lại ở bước 6." });
    $("review-body").hidden = false;
    const stamp = "&t=" + Date.now();
    $("review-video").src = q("/api/package/video") + stamp;
    $("review-frame").src = q("/api/package/frame") + stamp;
    $("review-status").textContent = reply.status === "ready" ? "sẵn sàng để duyệt" : "bị giữ lại";
    $("review-package").textContent = reply.package;
    $("approve-sha").value = reply.sha256;
    const rows = $("checklist-rows");
    rows.replaceChildren();
    for (const row of reply.rows) {
      const box = el("input", { type: "checkbox" });
      box.dataset.id = row.id;
      if (row.operatorItem) {
        box.checked = reply.ticks.includes(row.id);
        box.addEventListener("change", saveChecklistTicks);
      } else {
        box.checked = row.toolTicked;
        box.disabled = true;
      }
      const wording = el("td", {}, richText(row.wording));
      if (row.note) wording.append(el("div", { className: "note" }, [document.createTextNode("Ghi chú: "), ...richText(row.note)]));
      const tr = el("tr", { className: row.operatorItem ? "" : "tool" }, [
        el("td", {}, [box]),
        el("td", { textContent: row.id }),
        wording,
        el("td", { textContent: row.operatorItem ? row.who : "bộ công cụ" }),
      ]);
      rows.append(tr);
    }
    if (reply.approval) showMessage("approve-outcome", "Video này đã được duyệt bởi " + reply.approval.approvedBy + " ngày " + reply.approval.approvedOn + ".");
    else $("approve-outcome").replaceChildren();
    $("approve-path").textContent = "";
    updateApproval();
  }

  function checklistTicked() {
    return [...$("checklist-rows").querySelectorAll("input:not(:disabled):checked")].map((b) => b.dataset.id);
  }

  async function saveChecklistTicks() {
    updateApproval();
    const reply = await api("POST", q("/api/checklist-ticks"), { ticked: checklistTicked() });
    if (!reply.ok) showOutcome("approve-outcome", reply.outcome);
  }

  function approverText() {
    const ceo = MC17.ceoName || "";
    const by = document.querySelector('input[name="pressed-by"]:checked');
    if (!by) return null;
    if (by.value === "ceo") return ceo + " (CEO)";
    const employee = $("employee-name").value.trim();
    return employee ? ceo + " (CEO; nhân viên " + employee + " bấm duyệt theo chỉ thị bằng văn bản của CEO)" : null;
  }

  function updateApproval() {
    if (!checklist) return;
    const n = checklistTicked().length;
    const total = checklist.operatorItems;
    $("checklist-count").textContent = "Đã tích " + n + " trên " + total + " mục." + (n === total ? "" : " Tích đủ " + total + " mục mới duyệt được.");
    const by = document.querySelector('input[name="pressed-by"]:checked');
    $("employee-line").hidden = !by || by.value !== "nhan-vien";
    const text = approverText();
    $("approver-preview").textContent = text || "(chọn ai bấm duyệt)";
    $("approve-run").disabled = !(n === total && checklist.approvable && text && $("approve-sha").value.trim().length > 0);
  }

  async function approve() {
    const by = document.querySelector('input[name="pressed-by"]:checked');
    $("approve-run").disabled = true;
    const reply = await api("POST", q("/api/approve"), {
      sha256: $("approve-sha").value,
      ticked: checklistTicked(),
      pressedBy: by ? by.value : "",
      employeeName: $("employee-name").value,
    });
    const extra = reply.extra || {};
    const lines = [];
    if (extra.verify) lines.push("Kiểm lại: " + extra.verify.line.trim());
    showOutcome("approve-outcome", reply.outcome, lines);
    if (extra.videoPath) $("approve-path").textContent = (reply.ok ? "Tệp video đã duyệt: " : "Tệp video: ") + extra.videoPath;
    MC17.lastApprove = reply;
    updateApproval();
  }

  async function verify() {
    const reply = await api("POST", q("/api/verify"));
    showOutcome("approve-outcome", reply.outcome);
    MC17.lastVerify = reply;
  }

  // ---------------------------------------------------------------------------------------------
  // Gắn nút
  // ---------------------------------------------------------------------------------------------

  function wire() {
    $("setup-save").addEventListener("click", saveSetup);
    $("nav-home").addEventListener("click", loadHome);
    $("home-shortcut").addEventListener("click", async () => {
      const reply = await api("POST", "/api/shortcut");
      showReply("global-outcome", reply, reply.ok ? reply.message + " " + reply.shortcut : "");
    });
    $("new-open").addEventListener("click", () => {
      const code = $("new-code").value.trim();
      if (code) openVideo(code);
    });
    for (const button of document.querySelectorAll("#step-tabs button")) button.addEventListener("click", () => showStep(button.dataset.step));
    $("facts-add-row").addEventListener("click", () => { addFactRow({}); renumberRows(); });
    $("facts-save").addEventListener("click", saveFacts);
    $("read-run").addEventListener("click", readProduct);
    $("paste-run").addEventListener("click", pasteProduct);
    $("nav-settings").addEventListener("click", openSettings);
    $("settings-read").addEventListener("change", setReadSwitch);
    $("button-link").addEventListener("click", (event) => {
      event.preventDefault();
      showMessage("button-outcome", "Nút chỉ chạy trên trang sản phẩm shopee.vn. Hãy kéo nút này lên thanh dấu trang (Ctrl+Shift+B để hiện thanh), rồi bấm nó trên trang sản phẩm.");
    });
    $("button-shot-file").addEventListener("change", uploadButtonShot);
    document.addEventListener("paste", onPaste, true);
    $("facts-header").addEventListener("input", updateMarks);
    $("facts-rows").addEventListener("input", updateMarks);
    $("script-run").addEventListener("click", runScript);
    $("rec-start").addEventListener("click", startRecording);
    $("rec-stop").addEventListener("click", () => stopRecording("nguoi-dung"));
    $("rec-accept").addEventListener("click", acceptTake);
    $("rec-upload").addEventListener("change", uploadRecording);
    $("clip-upload").addEventListener("change", uploadClip);
    $("produce-run").addEventListener("click", startProduce);
    $("timing-save").addEventListener("click", saveTiming);
    for (const radio of document.querySelectorAll('input[name="pressed-by"]')) radio.addEventListener("change", updateApproval);
    $("employee-name").addEventListener("input", updateApproval);
    $("approve-sha").addEventListener("input", updateApproval);
    $("approve-run").addEventListener("click", approve);
    $("verify-run").addEventListener("click", verify);
  }

  MC17.openVideo = openVideo;
  MC17.richText = richText;
  MC17.showStep = showStep;
  MC17.loadHome = loadHome;

  async function start() {
    wire();
    if (await loadSetup()) await loadHome();
    MC17.ready = true;
    document.dispatchEvent(new Event("mc17-ready"));
  }

  start();
})();
