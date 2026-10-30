// NÚT ĐỌC TRANG (MC-19, technical design D-002, D-003, D-006, D-007, D-011, D-014).
// Chạy trong trình duyệt của người vận hành, trên trang sản phẩm shopee.vn đang mở, khi người vận hành bấm nút trên thanh dấu trang.
// Chỉ đọc chữ và địa chỉ ảnh mà trang đang hiện; không gửi yêu cầu nào, không bấm, không gửi biểu mẫu, không chuyển trang,
// không đọc cookie hay bộ nhớ của trang. Kết quả là một khối chữ trên bộ nhớ tạm (clipboard): người vận hành dán ở bước 1.
// Mọi chữ hiện ra đều đặt bằng textContent. Dòng chú thích (bắt đầu bằng //) bị bỏ khi dựng nút; mọi dòng còn lại giữ nguyên.
// Ứng dụng tính dấu của nút (button mark) trên đúng tệp này, nên sửa tệp này là đổi dấu và nút cũ bị từ chối.
(function () {
  "use strict";
  var C = /*MC19-CONFIG*/null;
  var MARKER = "MC19-NUT-DOC-TRANG";
  var DETAIL = "CHI TIẾT SẢN PHẨM";
  var ABOUT = "MÔ TẢ SẢN PHẨM";
  var KIND = "PHÂN LOẠI";
  var SHOP = "XEM SHOP";
  var ENDS = ["ĐÁNH GIÁ SẢN PHẨM", "SẢN PHẨM TƯƠNG TỰ", "CÁC SẢN PHẨM KHÁC CỦA SHOP", "CÓ THỂ BẠN CŨNG THÍCH", "GỢI Ý HÔM NAY", "SẢN PHẨM LIÊN QUAN"];
  var SHOP_SKIP = /^(mall|yêu thích\+?|chat ngay|xem shop|online.*|đang hoạt động.*|shopee mall)$/i;
  var MAX_IMAGES = 30, MAX_ROWS = 100, MAX_TEXT = 4000, MAX_BYTES = 262144;
  var doc = document;
  var notes = [];

  function norm(s) { return String(s === undefined || s === null ? "" : s).replace(/\s+/g, " ").trim(); }
  function upper(s) { return norm(s).toUpperCase().normalize("NFC"); }
  function cut(s) { s = norm(s); return s.length > MAX_TEXT ? s.slice(0, MAX_TEXT) : s; }
  function shown(node) { return String(node.innerText === undefined ? node.textContent : node.innerText); }
  function lines(node) { return shown(node).split(/\n+/).map(norm).filter(function (l) { return l.length > 0; }); }
  function contains(a, b) { return !!a && !!b && (a === b || a.contains(b)); }

  // Các phần tử sâu nhất có chữ đúng bằng một trong các nhãn (so chữ hoa, đã chuẩn NFC), theo thứ tự trang.
  function labels(texts, within) {
    var all = (within || doc.body).querySelectorAll("*");
    var found = [];
    for (var i = 0; i < all.length; i++) {
      var t = all[i].textContent;
      if (t.length > 80 || texts.indexOf(upper(t)) < 0) continue;
      found.push(all[i]);
    }
    return found.filter(function (n) { return !found.some(function (m) { return m !== n && n.contains(m); }); });
  }
  function label(text, within) { return labels([text], within); }

  // Tổ tiên cao nhất của node không chứa phần tử nào trong danh sách tránh.
  function highest(node, avoid) {
    var top = node;
    for (var p = node.parentElement; p && p !== doc.body && p !== doc.documentElement; p = p.parentElement) {
      if (avoid.some(function (a) { return contains(p, a); })) break;
      top = p;
    }
    return top;
  }

  // Các phần tử đứng sau node theo thứ tự trang, dừng ở nhãn kết thúc; phần tử chứa nhãn kết thúc thì bỏ qua (chỉ xét phần bên trong).
  function after(node, stops) {
    var out = [];
    var walker = doc.createTreeWalker(doc.body, NodeFilter.SHOW_ELEMENT);
    walker.currentNode = node;
    var n;
    while ((n = walker.nextNode())) {
      if (node.contains(n)) continue;
      if (stops.indexOf(n) >= 0) break;
      if (stops.some(function (s) { return n.contains(s); })) continue;
      out.push(n);
    }
    return out;
  }

  function notice(text, keep) {
    var box = doc.createElement("div");
    var s = box.style;
    s.position = "fixed"; s.top = "16px"; s.right = "16px"; s.zIndex = "2147483647"; s.maxWidth = "420px";
    s.background = "#fffbe6"; s.color = "#222"; s.border = "2px solid #b8860b"; s.padding = "12px"; s.font = "15px/1.4 sans-serif";
    s.whiteSpace = "pre-wrap";
    box.textContent = text;
    doc.body.appendChild(box);
    if (!keep) setTimeout(function () { box.remove(); }, 10000);
    return box;
  }

  function hostAllowed() {
    if (C.origins.indexOf(location.origin) >= 0) return true;
    var h = location.hostname.toLowerCase().replace(/\.$/, "");
    var page = h === "shopee.vn" || /\.shopee\.vn$/.test(h);
    var affiliate = h === "affiliate.shopee.vn" || /\.affiliate\.shopee\.vn$/.test(h);
    return location.protocol === "https:" && page && !affiliate && location.port === "";
  }

  function today() {
    var d = new Date();
    var two = function (n) { return (n < 10 ? "0" : "") + n; };
    return d.getFullYear() + "-" + two(d.getMonth() + 1) + "-" + two(d.getDate());
  }

  function images(region, info) {
    var out = [];
    var add = function (raw) {
      if (!raw || /^(data|blob):/i.test(raw)) return;
      // Địa chỉ tương đối được giải qua một thẻ <a> không gắn vào trang: không tải gì, không gửi yêu cầu nào.
      var link = doc.createElement("a");
      link.setAttribute("href", raw);
      var a = link.href;
      if (!a) return;
      if (out.indexOf(a) >= 0) return;
      if (out.length >= MAX_IMAGES) { if (notes.indexOf("anh") < 0) notes.push("anh"); return; }
      out.push(a);
    };
    var all = region.querySelectorAll("img, [style*='background-image']");
    for (var i = 0; i < all.length; i++) {
      var n = all[i];
      if (contains(info, n)) continue;
      if (n.tagName === "IMG") add(n.currentSrc || n.getAttribute("src"));
      else {
        var m = /url\(["']?([^"')]+)["']?\)/.exec(n.style.backgroundImage || "");
        if (m) add(m[1]);
      }
    }
    return out;
  }

  function price(region, name) {
    var ls = lines(region);
    for (var i = 0; i < ls.length; i++) {
      if (ls[i] === name) continue;
      var m = /(₫\s?\d[\d.,]*(\s?-\s?₫\s?\d[\d.,]*)?|\d[\d.,]*\s?(₫|đ|đồng|VNĐ|VND)(?![\p{L}]))/u.exec(ls[i]);
      if (m) return cut(m[0]);
    }
    return null;
  }

  function classification(region) {
    var labels = label(KIND, region);
    if (labels.length === 0) return null;
    var row = labels[0];
    for (var up = 0; up < 3 && row.parentElement; up++) {
      row = row.parentElement;
      var picked = row.querySelectorAll("[aria-pressed='true'], [aria-checked='true'], [aria-selected='true']");
      if (picked.length > 0) {
        var names = [];
        for (var i = 0; i < picked.length; i++) {
          var t = norm(shown(picked[i]));
          if (t && names.indexOf(t) < 0) names.push(t);
        }
        return names.length ? cut(names.join(", ")) : null;
      }
    }
    return null;
  }

  function shop(heading, detail) {
    var controls = label(SHOP);
    if (controls.length === 0) return null;
    var block = highest(controls[0], [heading, detail]);
    var name = null;
    var ls = lines(block);
    for (var i = 0; i < ls.length; i++) {
      if (SHOP_SKIP.test(ls[i]) || !/\p{L}/u.test(ls[i])) continue;
      name = ls[i];
      break;
    }
    if (!name) return null;
    var mall = label("MALL", block).length > 0;
    var marked = block.querySelectorAll("img[alt], [aria-label]");
    for (var j = 0; !mall && j < marked.length; j++) {
      var said = (marked[j].getAttribute("alt") || "") + " " + (marked[j].getAttribute("aria-label") || "");
      if (/mall/i.test(said)) mall = true;
    }
    return { name: cut(name), mall: mall };
  }

  function rows(detail) {
    var out = [];
    var seen = [];
    var nodes = after(detail, labels([ABOUT].concat(ENDS)));
    for (var i = 0; i < nodes.length; i++) {
      var n = nodes[i];
      if (seen.some(function (s) { return s.contains(n); })) continue;
      var kids = n.children;
      var key = null, value = null;
      if (n.tagName === "TR" && kids.length === 2) { key = shown(kids[0]); value = shown(kids[1]); }
      else if (kids.length === 2 && kids[0].children.length === 0 && norm(shown(kids[0])).length > 0 && norm(shown(kids[0])).length <= 60 && lines(kids[0]).length === 1) {
        key = shown(kids[0]); value = shown(kids[1]);
      }
      if (key === null) continue;
      key = cut(key); value = cut(value);
      if (!key || !value) continue;
      seen.push(n);
      if (out.length >= MAX_ROWS) { if (notes.indexOf("dong") < 0) notes.push("dong"); break; }
      out.push({ attribute: key, value: value, section: cut("Chi tiết sản phẩm: " + key) });
    }
    return out;
  }

  function description() {
    var heads = label(ABOUT);
    if (heads.length === 0) return null;
    var nodes = after(heads[0], labels(ENDS));
    var parts = [];
    var taken = [];
    for (var i = 0; i < nodes.length; i++) {
      var n = nodes[i];
      if (taken.some(function (t) { return t.contains(n); })) continue;
      taken.push(n);
      var t = shown(n).replace(/[ \t]+/g, " ").replace(/\n\s*\n+/g, "\n").trim();
      if (t) parts.push(t);
    }
    var all = parts.join("\n").trim();
    return all ? (all.length > MAX_TEXT ? all.slice(0, MAX_TEXT) : all) : null;
  }

  function copy(text, done) {
    var visible = function () {
      var wrap = notice("Nhấn Ctrl+C để chép, rồi mở ứng dụng và dán ở bước 1.", true);
      var box = doc.createElement("textarea");
      box.readOnly = true;
      box.value = text;
      box.style.display = "block"; box.style.width = "100%"; box.style.height = "6em"; box.style.marginTop = "8px";
      var close = doc.createElement("button");
      close.type = "button";
      close.textContent = "Đóng";
      close.addEventListener("click", function () { wrap.remove(); });
      box.addEventListener("copy", function () { setTimeout(function () { wrap.remove(); notice(done.text); }, 0); });
      wrap.appendChild(box);
      wrap.appendChild(close);
      box.focus();
      box.select();
    };
    var command = function () {
      var box = doc.createElement("textarea");
      box.readOnly = true;
      box.value = text;
      box.style.position = "fixed"; box.style.left = "-10000px"; box.style.top = "0"; box.style.opacity = "0";
      doc.body.appendChild(box);
      box.focus();
      box.select();
      var ok = false;
      try { ok = doc.execCommand("copy"); } catch (e) { ok = false; }
      box.remove();
      if (ok) notice(done.text); else visible();
    };
    if (navigator.clipboard && navigator.clipboard.writeText) {
      navigator.clipboard.writeText(text).then(function () { notice(done.text); }, command);
    } else {
      command();
    }
  }

  if (!hostAllowed()) {
    notice("Nút \"Đọc trang Shopee\" chỉ đọc trang sản phẩm trên shopee.vn. Trang này không phải shopee.vn, nên không chép gì.");
    return;
  }
  var heading = doc.querySelector("h1");
  var details = label(DETAIL);
  var name = heading ? cut(shown(heading)) : "";
  if (!name || details.length === 0) {
    notice("Đây không phải trang của một sản phẩm (không thấy tên sản phẩm và mục CHI TIẾT SẢN PHẨM), nên không chép gì. Mở trang của một sản phẩm rồi bấm lại.");
    return;
  }
  var detail = details[0];
  var controls = label(SHOP);
  var region = highest(heading, [detail].concat(controls));
  var info = region;
  for (var k = 0; k < region.children.length; k++) if (region.children[k].contains(heading)) info = region.children[k];

  var block = { formVersion: C.v, buttonMark: C.mark, pageAddress: location.origin + location.pathname, pressedAt: today(), productName: name };
  var kind = classification(region);
  if (kind) block.classification = kind;
  var seller = shop(heading, detail);
  if (seller) { block.shopName = seller.name; block.mall = seller.mall; }
  var specs = rows(detail);
  if (specs.length) block.specs = specs;
  var cost = price(region, name);
  if (cost) block.price = cost;
  var about = description();
  if (about) block.description = about;
  var pictures = images(region, info);
  if (pictures.length) block.images = pictures;

  var text = MARKER + "\n" + JSON.stringify(block);
  if (new TextEncoder().encode(text).length > MAX_BYTES) {
    notice("Thông tin của trang này dài quá giới hạn 256 KiB, nên không chép gì. Dán chữ của trang hoặc gõ tay ở bước 1.");
    return;
  }
  var said = "Đã chép thông tin sản phẩm \"" + name + "\". Mở ứng dụng và dán ở bước 1.";
  if (notes.indexOf("anh") >= 0) said += " Trang có hơn " + MAX_IMAGES + " ảnh; nút chỉ chép " + MAX_IMAGES + " ảnh đầu.";
  if (notes.indexOf("dong") >= 0) said += " Trang có hơn " + MAX_ROWS + " dòng thông tin; nút chỉ chép " + MAX_ROWS + " dòng đầu.";
  copy(text, { text: said });
})();
