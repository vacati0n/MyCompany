using System.Diagnostics;
using System.Text.Json.Nodes;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// THE MC-19 BUTTON CASES. The button runs in the digest-checked headless shell on code-generated product pages that the plain-HTTP
/// page server serves on 127.0.0.1; the app runs in this process through the internal start path with the MC-18 read fixture's seam and
/// the exact fixture origin (D-004). Nothing here opens shopee.vn or any Shopee host, and no connection leaves 127.0.0.1.
/// </summary>
internal sealed partial class ButtonCases : IDisposable
{
    private readonly Runner _r;
    private readonly string _root = PilotTestEnvironment.NewFolder("w-button");
    private PageServer? _pages;

    public ButtonCases(Runner r) => _r = r;

    private PageServer Pages => _pages ??= new PageServer(ButtonSite.Pages());

    public void Add()
    {
        _r.Add("T-067", "clipboard measurement (design Q-005): the button's copy on a fixture page in the headless shell — asynchronous write on a press, the copy command when the write is refused, the visible box and Ctrl+C when both are refused, and a press with no user activation", Measure);
        AddServerCases();
        AddBrowserCases();
    }

    public void Dispose()
    {
        _app?.Dispose();
        _fixture?.Dispose();
        _pages?.Dispose();
    }

    /// <summary>The script a press runs: the bookmark address after "javascript:", percent-decoded once, as Chrome runs it.</summary>
    private static string Pressed(string address)
    {
        Expect.True(address.StartsWith("javascript:", StringComparison.Ordinal), "bookmark address starts with javascript:");
        return Uri.UnescapeDataString(address["javascript:".Length..]);
    }

    /// <summary>The headless shell with its debugging port on 127.0.0.1, opened at the address, and DevTools sockets to it.</summary>
    private sealed class Shell : IDisposable
    {
        private readonly AddressHarness _harness = new();

        public Shell(string profile, string url)
        {
            Process = BrowserCase.Launch(profile, url, ["--remote-debugging-port=0", "--window-size=1280,2000"]);
            _harness.Track(Process);
            var (port, browserPath) = Cdp.Port(profile, Process);
            Port = port;
            Browser = Cdp.Browser(port, browserPath);
            Page = Cdp.Page(port, url);
        }

        public Process Process { get; }
        public int Port { get; }
        public Cdp Browser { get; }
        public Cdp Page { get; }

        public IReadOnlyList<SocketRow> Rows => _harness.Rows;

        /// <summary>Opens the address in the page and waits for the probe (fixture pages) or the app's ready flag.</summary>
        public void Open(string url, string ready = "window.__probe")
        {
            Page.Send("Page.navigate", new { url });
            Page.Until($"location.href === {JsonValue.Create(url)!.ToJsonString()} && document.readyState === 'complete' && {ready}", url);
        }

        public void Dispose()
        {
            Page.Dispose();
            Browser.Dispose();
            if (!Process.HasExited)
            {
                Process.Kill(entireProcessTree: true);
                Process.WaitForExit(10000);
            }

            Process.Dispose();
            _harness.Dispose();
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // T-067: the clipboard measurement of design Q-005, made before anything else is built on the copy.
    // -----------------------------------------------------------------------------------------------------------------------------

    private void Measure()
    {
        var origin = Pages.Origin;
        var url = origin + ButtonSite.ProductPath;
        var press = Pressed(Web.ButtonBuilder.Build(origin).Address);
        const string notice = "[...document.body.children].map(n => n.textContent).filter(t => t.includes('Đã chép') || t.includes('Nhấn Ctrl+C')).join(' | ')";
        const string neutral = "(() => { const r = document.querySelector('.rating').getBoundingClientRect(); return [r.left + 5, r.top + 5]; })()";
        using var shell = new Shell(Path.Combine(_root, "measure-profile"), url);
        var page = shell.Page;
        page.Until("document.readyState === 'complete' && window.__probe", "the product page");
        page.Send("Emulation.setFocusEmulationEnabled", new { enabled = true });
        var focus = page.Text("document.hasFocus()");
        _r.Note($"fixture page {url}; document.hasFocus() = {focus}; shell {shell.Process.Id}, DevTools port {shell.Port} on 127.0.0.1");

        string Clip()
        {
            shell.Browser.Send("Browser.grantPermissions", new { origin, permissions = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" } });
            var text = page.Text("navigator.clipboard.readText()", gesture: true);
            shell.Browser.Send("Browser.resetPermissions");
            return text.Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        void Clear()
        {
            shell.Browser.Send("Browser.grantPermissions", new { origin, permissions = new[] { "clipboardReadWrite", "clipboardSanitizedWrite" } });
            page.Eval("navigator.clipboard.writeText('trống')", gesture: true);
            shell.Browser.Send("Browser.resetPermissions");
            Expect.Equal("trống", Clip(), "clipboard cleared before the run");
        }

        void Click()
        {
            var point = page.Eval(neutral)!.AsArray();
            page.Click((double)point[0]!, (double)point[1]!);
        }

        var results = new List<string>();

        // A0. The headless shell's own default: it has no permission prompt, so it refuses the asynchronous write; the chain goes on.
        Clear();
        Click();
        var direct = page.Text("navigator.clipboard.writeText('thu').then(() => 'accepted', e => 'refused: ' + e.name + ': ' + e.message)", gesture: true);
        Clear();
        Click();
        var before = page.Eval("({ c: __probe.copies })")!;
        page.Eval(press, gesture: true);
        page.Until(notice + ".length > 0", "the notice after the press (A0)");
        var a0 = (Notice: page.Text(notice), Copies: (int)page.Eval("__probe.copies")! - (int)before["c"]!, Clip: Clip());
        var defaultEnds = a0.Clip.StartsWith(Web.BlockReader.Marker + "\n", StringComparison.Ordinal) && a0.Notice.Contains("Đã chép", StringComparison.Ordinal);
        results.Add($"A0 shell default permissions: the asynchronous write alone {direct}; the press ended by {(a0.Copies == 1 ? "the copy command" : "the asynchronous write")}: {(defaultEnds ? "WORKS" : "FAILED")}; copy events {a0.Copies}; clipboard {a0.Clip.Length} chars starting \"{Head(a0.Clip)}\"");

        // A. The primary path: a press with user activation and the write permission a focused desktop Chrome page holds by default.
        shell.Open(url);
        Clear();
        shell.Browser.Send("Browser.grantPermissions", new { origin, permissions = new[] { "clipboardSanitizedWrite" } });
        Click();
        page.Eval(press, gesture: true);
        page.Until(notice + ".length > 0", "the notice after the press (A)");
        shell.Browser.Send("Browser.resetPermissions");
        var a = (Notice: page.Text(notice), Copies: (int)page.Eval("__probe.copies")!, Clip: Clip());
        var primary = a.Clip.StartsWith(Web.BlockReader.Marker + "\n", StringComparison.Ordinal) && a.Copies == 0 && a.Notice.Contains("Đã chép", StringComparison.Ordinal);
        results.Add($"A asynchronous write on a press (write permission granted): {(primary ? "WORKS" : "FAILED")}; copy events {a.Copies}; notice \"{a.Notice}\"; clipboard {a.Clip.Length} chars starting \"{Head(a.Clip)}\"");

        // B. The asynchronous write refused: the copy command on an off-screen selected box.
        shell.Open(url);
        Clear();
        page.Eval("navigator.clipboard.writeText = () => Promise.reject(new DOMException('refused for the measurement', 'NotAllowedError')); true");
        Click();
        page.Eval(press, gesture: true);
        page.Until(notice + ".length > 0", "the notice after the press (B)");
        var b = (Notice: page.Text(notice), Copies: (int)page.Eval("__probe.copies")!, Clip: Clip());
        var command = b.Clip.StartsWith(Web.BlockReader.Marker + "\n", StringComparison.Ordinal) && b.Copies == 1 && b.Notice.Contains("Đã chép", StringComparison.Ordinal);
        results.Add($"B write refused, copy command: {(command ? "WORKS" : "FAILED")}; copy events {b.Copies}; notice \"{b.Notice}\"; clipboard starting \"{Head(b.Clip)}\"");

        // C. Both refused: the visible selected box with "Nhấn Ctrl+C để chép", then the operator's Ctrl+C.
        shell.Open(url);
        Clear();
        page.Eval("navigator.clipboard.writeText = () => Promise.reject(new DOMException('refused', 'NotAllowedError')); document.execCommand = () => false; true");
        Click();
        page.Eval(press, gesture: true);
        page.Until(notice + ".includes('Nhấn Ctrl+C')", "the visible box (C)");
        var selected = page.Text("document.activeElement.tagName + ':' + (document.activeElement.selectionEnd - document.activeElement.selectionStart) + ':' + document.activeElement.value.length");
        page.Chord("c", "copy");
        page.Until(notice + ".includes('Đã chép')", "the notice after Ctrl+C (C)");
        var c = (Notice: page.Text(notice), Copies: (int)page.Eval("__probe.copies")!, Clip: Clip(), BoxLeft: page.Text("document.querySelectorAll('textarea').length"));
        var visible = c.Clip.StartsWith(Web.BlockReader.Marker + "\n", StringComparison.Ordinal) && c.Copies == 1 && c.BoxLeft == "0";
        results.Add($"C both refused, visible box and Ctrl+C: {(visible ? "WORKS" : "FAILED")}; box focus and selection {selected}; copy events {c.Copies}; boxes left {c.BoxLeft}; notice \"{c.Notice}\"; clipboard starting \"{Head(c.Clip)}\"");

        // D. A press with no user activation at all (no click, no gesture): what Chrome itself refuses, and where the chain ends.
        shell.Open(url);
        Clear();
        page.Eval(press, gesture: false);
        page.Until(notice + ".length > 0", "the notice with no activation (D)");
        var d0 = page.Text(notice);
        if (d0.Contains("Nhấn Ctrl+C", StringComparison.Ordinal))
        {
            page.Chord("c", "copy");
            page.Until(notice + ".includes('Đã chép')", "the notice after Ctrl+C (D)");
        }

        var d = (Notice: page.Text(notice), Copies: (int)page.Eval("__probe.copies")!, Clip: Clip());
        var path = d0.Contains("Nhấn Ctrl+C", StringComparison.Ordinal) ? "visible box then Ctrl+C" : d.Copies == 1 ? "copy command" : "asynchronous write";
        var unactivated = d.Clip.StartsWith(Web.BlockReader.Marker + "\n", StringComparison.Ordinal);
        results.Add($"D no activation: ended by {path}: {(unactivated ? "WORKS" : "FAILED")}; first notice \"{d0}\"; copy events {d.Copies}; clipboard starting \"{Head(d.Clip)}\"");

        foreach (var line in results)
        {
            _r.Note(line);
        }

        var outside = shell.Rows.Where(row => row.Protocol.StartsWith("tcp", StringComparison.Ordinal) && !row.IsLoopback).ToList();
        _r.Note($"page server requests: {string.Join(", ", Pages.Requests.Select(q => q.Path).Distinct())}; browser TCP rows off loopback: {outside.Count}");
        Expect.Equal(0, outside.Count, "browser TCP rows off loopback");
        Expect.True(primary || command || visible, "at least one copy path of D-007 works (else the build stops: blocked)");
        Expect.True(defaultEnds && primary && command && visible && unactivated, "the five measured runs (A0, A, B, C, D) each end with the block on the clipboard");
    }

    private static string Head(string text) => (text.Length > 40 ? text[..40] : text).Replace("\n", "\\n", StringComparison.Ordinal);
}
