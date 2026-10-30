using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// THE MC-19 BUTTON CASES IN THE BROWSER: the real bookmark address from "Cài đặt", pressed (run with a user activation, as a press on
/// the bookmarks bar runs it) on the code-generated pages of the plain-HTTP page server on 127.0.0.1 in the digest-checked headless
/// shell, the copied block read back from the clipboard, and Ctrl+V on the app's own page. Images reach the app through the seam only.
/// </summary>
internal sealed partial class ButtonCases
{
    private const string NoticeOf = "[...document.body.children].filter(n => n.tagName === 'DIV' && getComputedStyle(n).position === 'fixed').map(n => n.textContent).join(' | ')";

    private void AddBrowserCases()
    {
        _r.Add("T-077", "browser, the whole flow: the button from Cài đặt (a click in the app does not run it); pressed on the product page it clicks, submits, navigates and requests nothing; Ctrl+V in the app fills N fields and rows marked 'Từ nút đọc trang', the five own images and none of the others; one screenshot for every row; three picked; N-1 refused, N accepted; switch off and no open video refused", Flow);
        _r.Add("T-078", "browser, other pages: a non-Mall page with no variant chosen; a shop page, a search page and a page with no product name copy nothing; a page off the allowed origin copies nothing; a signed-in page's account, cookie and stored values are not copied; markup is shown as text in the app; 30 images and 100 rows at most, with the notice", OtherPages);
    }

    /// <summary>The clipboard's text, read through the page with the read permission granted for the moment of reading (CRLF as LF).</summary>
    private static string Clipboard(Shell shell)
    {
        var origin = shell.Page.Text("location.origin");
        shell.Browser.Send("Browser.grantPermissions", new { origin, permissions = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" } });
        var text = shell.Page.Text("navigator.clipboard.readText()", gesture: true);
        shell.Browser.Send("Browser.resetPermissions");
        return text.Replace("\r\n", "\n", StringComparison.Ordinal);
    }

    private static void ClearClipboard(Shell shell)
    {
        var origin = shell.Page.Text("location.origin");
        shell.Browser.Send("Browser.grantPermissions", new { origin, permissions = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" } });
        shell.Page.Eval("navigator.clipboard.writeText('trống')", gesture: true);
        shell.Browser.Send("Browser.resetPermissions");
        Expect.Equal("trống", Clipboard(shell), "clipboard cleared");
    }

    /// <summary>
    /// One press: a real click on the footer (the activation), the probe read, the button script run with the write permission desktop
    /// Chrome grants a focused page, the notice awaited, the probe read again. Returns the probe before and after and the notice.
    /// </summary>
    private static (JsonNode Before, JsonNode After, string Notice) Press(Shell shell, string press)
    {
        var page = shell.Page;
        var origin = page.Text("location.origin");
        var point = page.Eval("(() => { const f = document.querySelector('footer') || document.body; f.scrollIntoView(); const r = f.getBoundingClientRect(); return [r.left + 3, r.top + 3]; })()")!.AsArray();
        page.Click((double)point[0]!, (double)point[1]!);
        var before = page.Eval("window.__probe ? ({ clicks: __probe.clicks, submits: __probe.submits, copies: __probe.copies, unloads: __probe.unloads, loadId: __probe.loadId, href: location.href }) : ({ href: location.href })")!;
        shell.Browser.Send("Browser.grantPermissions", new { origin, permissions = new[] { "clipboardSanitizedWrite" } });
        page.Eval(press, gesture: true);
        page.Until(NoticeOf + ".length > 0", "the button's notice");
        Thread.Sleep(300);
        shell.Browser.Send("Browser.resetPermissions");
        var after = page.Eval("window.__probe ? ({ clicks: __probe.clicks, submits: __probe.submits, copies: __probe.copies, unloads: __probe.unloads, loadId: __probe.loadId, href: location.href }) : ({ href: location.href })")!;
        return (before, after, page.Text(NoticeOf));
    }

    private static void Unchanged(JsonNode before, JsonNode after, string what)
    {
        foreach (var member in new[] { "clicks", "submits", "unloads", "loadId", "href" })
        {
            Expect.Equal(before[member]?.ToJsonString(), after[member]?.ToJsonString(), $"{what}: probe {member} unchanged by the press");
        }
    }

    private static void OpenApp(Shell shell, ReadApp app) => shell.Open(app.Origin + "/", "window.MC17 && MC17.ready");

    /// <summary>Ctrl+V on the app page with the niche field focused; waits for the block reply and returns it.</summary>
    private static JsonNode PasteInApp(Shell shell)
    {
        var page = shell.Page;
        page.Eval("(() => { MC17.lastBlock = null; const n = document.getElementById('fh-niche'); if (n && !document.getElementById('view-video').hidden) n.focus(); else document.body.focus(); return true; })()");
        page.Chord("v", "paste");
        page.Until("MC17.lastBlock", "the app's answer to the pasted block", 60);
        return page.Eval("MC17.lastBlock")!;
    }

    private static int Count(Cdp page, string selector) => page.Eval($"document.querySelectorAll({JsonValue.Create(selector)!.ToJsonString()}).length")!.GetValue<int>();

    private void Flow()
    {
        var app = App;
        const string code = "V-77";
        var url = Pages.Origin + ButtonSite.ProductPath;
        using var shell = new Shell(Path.Combine(_root, "flow-profile"), app.Origin + "/");
        var page = shell.Page;
        page.Until("window.MC17 && MC17.ready", "the app page");
        page.Send("Emulation.setFocusEmulationEnabled", new { enabled = true });

        // The button in "Cài đặt": the same address as /api/button; a click on it in the app runs nothing and asks for a drag.
        page.Eval("document.getElementById('nav-settings').click(); true");
        page.Until("(document.getElementById('button-link').getAttribute('href') || '').startsWith('javascript:')", "the button in Cài đặt");
        var href = page.Text("document.getElementById('button-link').getAttribute('href')");
        Expect.Equal(app.Get("/api/button")["address"].GetString(), href, "the settings link is the /api/button address");
        Expect.Equal(href, page.Text("document.getElementById('button-address').value"), "the address box shows the same address");
        Expect.True(page.Text("document.getElementById('button-version').textContent").Contains(Web.ButtonBuilder.Mark, StringComparison.Ordinal), "the version line shows the mark");
        var link = page.Eval("(() => { const b = document.getElementById('button-link'); b.scrollIntoView(); const r = b.getBoundingClientRect(); return [r.left + 5, r.top + 5]; })()")!.AsArray();
        page.Click((double)link[0]!, (double)link[1]!);
        page.Until("document.getElementById('button-outcome').textContent.includes('thanh dấu trang')", "the drag message");
        Expect.Equal(app.Origin + "/", page.Text("location.href"), "the click left the app page where it was");
        Expect.True(!page.Text(NoticeOf).Contains("Đọc trang Shopee", StringComparison.Ordinal), "the button did not run in the app");

        // The press on the product page.
        var press = Pressed(href);
        shell.Open(url);
        page.Send("Network.enable");
        ClearClipboard(shell);
        var served = Pages.Requests.Count;
        var seen = page.Requests().Count;
        var (before, after, notice) = Press(shell, press);
        Thread.Sleep(500);
        Unchanged(before, after, "product page");
        var network = page.Requests().Skip(seen).ToList();
        Expect.Equal(0, network.Count, $"requests the page made after the press: {string.Join(", ", network)}");
        Expect.Equal(served, Pages.Requests.Count, "the page server got no request from the press");
        Expect.True(notice.Contains($"Đã chép thông tin sản phẩm \"{FixtureSite.Name}\"", StringComparison.Ordinal), $"notice: {notice}");
        var clip = Clipboard(shell);
        var block = Web.BlockReader.Read(clip, Pages.Origin);
        Expect.Equal(url, block.Page.ToString(), "page address");
        Expect.Equal(FixtureSite.Name, block.ProductName, "name");
        Expect.Equal(FixtureSite.Classification, block.Classification, "classification (the pressed variant)");
        Expect.Equal(ButtonSite.ShopName, block.ShopName, "shop");
        Expect.Equal(true, block.Mall, "Mall");
        Expect.Equal(string.Join(" | ", ButtonSite.ExpectedRows.Select(r => $"{r.Attribute}: {r.Value}")), string.Join(" | ", block.Specs.Select(s => $"{s.Attribute}: {s.Value}")), "five rows in page order");
        Expect.Equal(ButtonSite.PagePrice, block.Price, "price");
        Expect.Equal(FixtureSite.Description, block.Description, "description");
        Expect.Equal(string.Join(" ", FixtureSite.OwnImages), string.Join(" ", block.Images), "the five own images, none of the similar products");
        Expect.Equal(DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), block.PressedAt, "press date");
        _r.Note($"press: notice \"{notice}\"; probe before {before.ToJsonString()}, after {after.ToJsonString()}; 0 page requests, 0 page-server requests; block {clip.Length} chars");
        _r.Note($"block rows: {string.Join(" | ", block.Specs.Select(s => $"{s.Attribute}: {s.Value} ({s.SourceSection})"))}");

        // Ctrl+V in the app, with the video open at step 1 and the niche field focused.
        OpenApp(shell, app);
        Expect.True(page.Eval($"MC17.openVideo('{code}')")!.GetValue<bool>(), "video opened");
        var imagesBefore = F.Requests.Count;
        var reply = PasteInApp(shell);
        Expect.True((bool?)reply["ok"] == true, $"paste reply: {reply.ToJsonString()}");
        var fetched = F.Requests.Skip(imagesBefore).Select(q => $"https://{q.Host}{q.PathOnly}").ToList();
        Expect.Equal(string.Join(" ", FixtureSite.OwnImages), string.Join(" ", fetched), "the app fetched the five own images and nothing else");
        Expect.True(FixtureSite.OtherImages.All(o => !fetched.Contains(o)), "no other product's image");
        Expect.Equal(string.Empty, page.Text("document.getElementById('fh-niche').value"), "the block was not pasted into the focused field");
        Expect.Equal(FixtureSite.Name, page.Text("document.getElementById('fh-productName').value"), "name field");
        Expect.Equal(FixtureSite.Classification, page.Text("document.getElementById('fh-classification').value"), "classification field");
        Expect.Equal(url, page.Text("document.getElementById('fh-pagePath').value"), "page address field");
        Expect.Equal($"{ButtonSite.ShopName}, Mall", page.Text("document.getElementById('fh-shopNameAndMall').value"), "shop field");
        Expect.Equal(block.PressedAt, page.Text("document.getElementById('fh-readDate').value"), "read date field");
        Expect.Equal(5, Count(page, "#read-candidates li"), "five rows offered");
        Expect.Equal(3, Count(page, "#facts-rows .fact-row"), "three rows filled");
        const int n = 8;
        Expect.Equal(n, Count(page, "#step-1 .fill-mark"), "N marks");
        Expect.True(page.Eval("[...document.querySelectorAll('#step-1 .fill-mark')].every(m => m.textContent.includes('Từ nút đọc trang'))")!.GetValue<bool>(), "every mark says Từ nút đọc trang");
        page.Until("document.querySelectorAll('#read-images img').length === 5 && [...document.querySelectorAll('#read-images img')].every(i => i.complete && i.naturalWidth > 0)", "five thumbnails shown");
        Expect.True(!page.Eval("document.getElementById('button-shot').hidden")!.GetValue<bool>(), "the one-screenshot chooser is shown");

        // One screenshot for every row from the button (D-008), through the file chooser.
        var root = page.Send("DOM.getDocument")["root"]!["nodeId"]!.GetValue<int>();
        var chooser = page.Send("DOM.querySelector", new { nodeId = root, selector = "#button-shot-file" })["nodeId"]!.GetValue<int>();
        page.Send("DOM.setFileInputFiles", new { nodeId = chooser, files = new[] { Path.Combine(Stills, "chup-1.png") } });
        page.Until("MC17.buttonShot", "the screenshot upload");
        var shot = page.Text("MC17.buttonShot");
        Expect.True(page.Eval($"[...document.querySelectorAll('#facts-rows input[data-member=screenshot]')].every(i => i.value === {JsonValue.Create(shot)!.ToJsonString()})")!.GetValue<bool>(), "every button row names the one screenshot");

        // Pick three read images; a fourth is refused.
        page.Eval("[...document.querySelectorAll('#read-images input[type=checkbox]')].slice(0, 3).forEach(b => b.click()); true");
        Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png", page.Text("document.getElementById('fh-imagesUsed').value"), "three picked");
        page.Eval("document.querySelectorAll('#read-images input[type=checkbox]')[3].click(); true");
        Expect.True(!page.Eval("document.querySelectorAll('#read-images input[type=checkbox]')[3].checked")!.GetValue<bool>(), "a fourth refused");

        // The operator's own fields, typed.
        page.Eval("""
            (() => {
              const set = (e, v) => { e.value = v; e.dispatchEvent(new Event('input', { bubbles: true })); };
              const id = (x) => document.getElementById(x);
              if (!id('fh-videoCode').value) set(id('fh-videoCode'), 'V-77');
              set(id('fh-subjectNoun'), 'bình giữ nhiệt');
              set(id('fh-niche'), 'đồ dùng nhà bếp');
              set(id('fh-productVideoNumber'), '1');
              set(id('fh-videoFocus'), 'dung tích');
              for (const i of document.querySelectorAll('#facts-rows input[data-member=subjectNoun]')) set(i, 'bình giữ nhiệt');
              return true;
            })()
            """);
        Expect.Equal(n, Count(page, "#step-1 .fill-mark"), "marks kept while the operator types own fields");

        // N-1 confirmations refused, N accepted.
        while (Count(page, "#step-1 .fill-mark button.confirm") > 1)
        {
            var left = Count(page, "#step-1 .fill-mark button.confirm");
            page.Eval("document.querySelector('#step-1 .fill-mark button.confirm').click(); true");
            page.Until($"document.querySelectorAll('#step-1 .fill-mark button.confirm').length < {left}", "a confirmation");
        }

        page.Eval("MC17.lastSave = null; document.getElementById('facts-save').click(); true");
        page.Until("MC17.lastSave", "the save");
        var refused = page.Eval("MC17.lastSave")!;
        Expect.Equal("fill-unconfirmed", (string?)refused["outcome"]?["code"], $"N-1: {refused.ToJsonString()}");
        page.Eval("document.querySelector('#step-1 .fill-mark button.confirm').click(); true");
        page.Until("document.querySelectorAll('#step-1 .fill-mark button.confirm').length === 0", "the last confirmation");
        page.Eval("MC17.lastSave = null; document.getElementById('facts-save').click(); true");
        page.Until("MC17.lastSave", "the save");
        var accepted = page.Eval("MC17.lastSave")!;
        Expect.True((bool?)accepted["ok"] == true, $"N: {accepted.ToJsonString()}");
        Expect.Equal(n, Count(page, "#step-1 .fill-mark.confirmed"), "N marks confirmed");
        var input = app.Workspace.InputFolder(code);
        Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png", string.Join(", ", Directory.EnumerateFiles(input, "anh-doc-*").Select(Path.GetFileName).Order(StringComparer.Ordinal)), "the input folder holds the three picked");
        var status = app.Get($"/api/video?code={code}");
        Expect.True(status["images"].EnumerateArray().All(i => i.GetProperty("present").GetBoolean()), $"step 2 none missing: {status["images"].GetRawText()}");
        var sheet = File.ReadAllText(app.Workspace.FactSheetPath(code));
        Expect.True(!sheet.Contains("189.000", StringComparison.Ordinal) && !sheet.Contains("nắp vặn", StringComparison.Ordinal), "price and description not saved");
        _r.Note($"app: N = {n} marks 'Từ nút đọc trang'; images fetched {string.Join(", ", fetched.Select(f => f[(f.LastIndexOf('/') + 1)..]))}; one screenshot {shot} on 3 rows; N-1: {(string?)refused["outcome"]?["code"]} \"{(string?)refused["outcome"]?["line"]}\"; N: accepted");

        // Switch off: the same Ctrl+V is refused naming "Cài đặt", with no request and the saved fill untouched.
        var fillBytes = File.ReadAllBytes(app.Workspace.FillPath(code));
        Expect.True(app.Switch(false).Ok, "switch off");
        var off = F.During(() => PasteInApp(shell));
        Expect.Equal("block-read-off", (string?)off.Result["outcome"]?["code"], $"switch off: {off.Result.ToJsonString()}");
        Expect.True(page.Text("document.getElementById('read-outcome').textContent").Contains("Cài đặt", StringComparison.Ordinal), "the refusal shown names Cài đặt");
        Expect.Equal(0, off.Requests.Count + off.Connects.Count, "switch off: no connection");
        Expect.True(File.ReadAllBytes(app.Workspace.FillPath(code)).AsSpan().SequenceEqual(fillBytes), "switch off: the fill record untouched");
        Expect.True(app.Switch(true).Ok, "switch on");

        // No video open: the home view, the same Ctrl+V.
        page.Eval("document.getElementById('nav-home').click(); true");
        page.Until("document.getElementById('view-video').hidden", "the home view");
        var none = F.During(() => PasteInApp(shell));
        Expect.Equal("block-no-video", (string?)none.Result["outcome"]?["code"], $"no video: {none.Result.ToJsonString()}");
        Expect.True(page.Text("document.getElementById('global-outcome').textContent").Length > 0, "the refusal is shown");
        Expect.Equal(0, none.Requests.Count + none.Connects.Count, "no video: no connection");

        var outside = shell.Rows.Where(row => row.Protocol.StartsWith("tcp", StringComparison.Ordinal) && !row.IsLoopback).ToList();
        Expect.Equal(0, outside.Count, $"browser TCP rows off loopback: {string.Join("; ", outside)}");
        _r.Note($"switch off: {(string?)off.Result["outcome"]?["code"]}, 0 requests; no video: {(string?)none.Result["outcome"]?["code"]}; browser {shell.Rows.Count} rows, 0 TCP off loopback");
    }

    private void OtherPages()
    {
        var app = App;
        var press = Pressed(Web.ButtonBuilder.Build(Pages.Origin).Address);
        Expect.Equal(app.Get("/api/button")["address"].GetString(), Web.ButtonBuilder.Build(Pages.Origin).Address, "the test app's button is this button");
        using var shell = new Shell(Path.Combine(_root, "pages-profile"), Pages.Origin + ButtonSite.PlainShopPath);
        var page = shell.Page;
        page.Until("document.readyState === 'complete' && window.__probe", "the first page");
        page.Send("Emulation.setFocusEmulationEnabled", new { enabled = true });

        ButtonBlock Copied(string path, string what)
        {
            shell.Open(Pages.Origin + path);
            ClearClipboard(shell);
            var served = Pages.Requests.Count;
            var (before, after, notice) = Press(shell, press);
            Unchanged(before, after, what);
            Expect.Equal(served, Pages.Requests.Count, $"{what}: no page-server request");
            Expect.True(notice.Contains("Đã chép", StringComparison.Ordinal), $"{what}: notice {notice}");
            var clip = Clipboard(shell);
            _r.Note($"{what}: \"{notice}\"; {clip.Length} chars");
            return Web.BlockReader.Read(clip, Pages.Origin);
        }

        // A shop that is not Mall, with no variant pressed.
        var plain = Copied(ButtonSite.PlainShopPath, "non-Mall page");
        Expect.Equal(ButtonSite.PlainShopName, plain.ShopName, "non-Mall shop");
        Expect.Equal(false, plain.Mall, "not Mall");
        Expect.Equal(null, plain.Classification, "no variant pressed, no classification");
        Expect.Equal("Danh Mục: Shopee > Nhà Cửa & Đời Sống | Dung tích: 1,2 lít | Chất liệu: Nhựa PP", string.Join(" | ", plain.Specs.Select(s => $"{s.Attribute}: {s.Value}")), "three rows");
        Expect.Equal(2, plain.Images.Count, "two images");

        // Pages that are not one product, and a page off the allowed origin: nothing copied.
        foreach (var (path, what) in new[] { (ButtonSite.ShopPath, "shop page"), (ButtonSite.SearchPath, "search page"), (ButtonSite.NamelessPath, "page with no product name") })
        {
            shell.Open(Pages.Origin + path);
            ClearClipboard(shell);
            var (before, after, notice) = Press(shell, press);
            Unchanged(before, after, what);
            Expect.True(notice.Contains("không phải trang của một sản phẩm", StringComparison.Ordinal), $"{what}: notice {notice}");
            Expect.Equal("trống", Clipboard(shell), $"{what}: nothing copied");
            Expect.Equal(before["copies"]?.ToJsonString(), after["copies"]?.ToJsonString(), $"{what}: no copy event");
            _r.Note($"{what}: nothing copied; \"{notice}\"");
        }

        OpenApp(shell, app);
        ClearClipboard(shell);
        var (_, _, offNotice) = Press(shell, press);
        Expect.True(offNotice.Contains("không phải shopee.vn", StringComparison.Ordinal), $"off-origin page: notice {offNotice}");
        Expect.Equal("trống", Clipboard(shell), "off-origin page: nothing copied");
        _r.Note($"page on the app's origin {app.Origin}: nothing copied; \"{offNotice}\"");

        // A signed-in page: the account name, the cookie and the stored values exist on the page, and none is copied.
        shell.Open(Pages.Origin + ButtonSite.SignedInPath);
        Expect.True(page.Text("document.cookie").Contains(ButtonSite.CookieValue, StringComparison.Ordinal), "the page holds the cookie");
        Expect.Equal(ButtonSite.StoredValue, page.Text("localStorage.getItem('mc19-luu-tru')"), "the page holds the stored value");
        Expect.True(page.Text("document.body.textContent").Contains(ButtonSite.Account, StringComparison.Ordinal), "the page shows the account");
        ClearClipboard(shell);
        var (signedBefore, signedAfter, signedNotice) = Press(shell, press);
        Unchanged(signedBefore, signedAfter, "signed-in page");
        var signedClip = Clipboard(shell);
        foreach (var secret in new[] { ButtonSite.Account, ButtonSite.CookieValue, ButtonSite.StoredValue, "SPC_U" })
        {
            Expect.True(!signedClip.Contains(secret, StringComparison.Ordinal), $"signed-in page: {secret} not copied");
        }

        var signed = Web.BlockReader.Read(signedClip, Pages.Origin);
        Expect.Equal(FixtureSite.Name, signed.ProductName, "signed-in page: the product read");
        Expect.Equal(5, signed.Specs.Count, "signed-in page: five rows");
        _r.Note($"signed-in page: \"{signedNotice}\"; account, cookie, stored values and SPC_U absent from {signedClip.Length} chars; accepted by the reader");

        // At most 30 images and 100 rows, with the notice.
        var many = Copied(ButtonSite.ManyPath, "page with 34 images and 106 rows");
        Expect.Equal(30, many.Images.Count, "30 images");
        Expect.Equal(100, many.Specs.Count, "100 rows");
        var manyNotice = page.Text(NoticeOf);
        Expect.True(manyNotice.Contains("30 ảnh đầu", StringComparison.Ordinal) && manyNotice.Contains("100 dòng đầu", StringComparison.Ordinal), $"the notice names both caps: {manyNotice}");
        Expect.Equal("Mục 099: Giá trị 99", $"{many.Specs[^1].Attribute}: {many.Specs[^1].Value}", "the first 100 rows, in page order");

        // Markup in every text member: copied as text, and shown as text in the app.
        var markup = Copied(ButtonSite.MarkupPath, "page with markup in its text");
        Expect.True(page.Eval("window.__mc19 === undefined")!.GetValue<bool>(), "no markup ran on the product page");
        Expect.Equal(ButtonSite.Markup.Name, markup.ProductName, "name as text");
        Expect.Equal(ButtonSite.Markup.Classification, markup.Classification, "classification as text");
        Expect.Equal(ButtonSite.Markup.Shop, markup.ShopName, "shop as text");
        Expect.Equal($"{ButtonSite.Markup.Attribute}: {ButtonSite.Markup.Value}", $"{markup.Specs[^1].Attribute}: {markup.Specs[^1].Value}", "row as text");
        OpenApp(shell, app);
        Expect.True(page.Eval("MC17.openVideo('V-78m')")!.GetValue<bool>(), "video opened");
        var reply = PasteInApp(shell);
        Expect.True((bool?)reply["ok"] == true, $"markup block pasted: {reply.ToJsonString()}");
        Thread.Sleep(500);
        Expect.True(page.Eval("window.__mc19 === undefined")!.GetValue<bool>(), "no markup ran in the app");
        Expect.Equal(0, Count(page, "[onerror], [onclick], [onload], iframe, svg, img[src='x']"), "no element made from the markup");
        Expect.Equal(ButtonSite.Markup.Name, page.Text("document.getElementById('fh-productName').value"), "the app shows the name as text");
        Expect.Equal($"{ButtonSite.Markup.Shop}, không phải Mall", page.Text("document.getElementById('fh-shopNameAndMall').value"), "the app shows the shop as text");
        Expect.True(page.Text("document.getElementById('read-candidates').textContent").Contains(ButtonSite.Markup.Value, StringComparison.Ordinal), "the app shows the row value as text");
        Expect.True(page.Text("document.getElementById('read-reference').textContent").Contains(ButtonSite.Markup.Description, StringComparison.Ordinal), "the app shows the description as text");

        var outside = shell.Rows.Where(row => row.Protocol.StartsWith("tcp", StringComparison.Ordinal) && !row.IsLoopback).ToList();
        Expect.Equal(0, outside.Count, $"browser TCP rows off loopback: {string.Join("; ", outside)}");
        _r.Note($"markup: shown as text on both pages, no element made; many: 30 images, 100 rows, \"{manyNotice}\"; browser 0 TCP rows off loopback");
    }
}
