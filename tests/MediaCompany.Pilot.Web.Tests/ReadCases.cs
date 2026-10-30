using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// THE MC-18 READ CASES (scope acceptance SC-01 to SC-33). Every read goes to the code-generated loopback fixture through the transport
/// seam of the internal start path; nothing here names a resolver or leaves 127.0.0.1. The app runs in this process for the request-level
/// cases, in a child process (this program's fixture host mode) for the socket-table case, and under the headless shell for the page case.
/// </summary>
internal sealed class ReadCases : IDisposable
{
    private const string CeoName = "Nguyễn Văn An";
    private readonly Runner _r;
    private readonly string _root = PilotTestEnvironment.NewFolder("w-read");
    private ReadFixture? _fixture;
    private ReadApp? _app;
    private string? _stills;
    private JsonElement _goodFill;

    public ReadCases(Runner r) => _r = r;

    private ReadFixture F => _fixture ??= new ReadFixture();

    private ReadApp App
    {
        get
        {
            if (_app is null)
            {
                _app = ReadApp.Start(Path.Combine(_root, "main"), F, Path.Combine(_root, "main", "pilot"));
                Expect.True(_app.Switch(true).Ok, "read switch turned on");
            }

            return _app;
        }
    }

    private string Stills => _stills ??= Fixtures.Stills(Path.Combine(_root, "stills"));

    public void Add()
    {
        _r.Add("T-050", "link rule: the six disallowed kinds (five links and a hop to each), an affiliate link and hop, and two links are refused with no connection to the refused host", LinkRule);
        _r.Add("T-051", "good read: name, classification, page address and date filled; candidates equal the page; the five own images byte-equal; only those six requests, GET, headers Host, User-Agent, Accept; no cookie sent back; one source address", GoodRead);
        _r.Add("T-052", "a short link of two hops fills exactly what the full link fills; a chain of six hops stops at the sixth with nothing filled", ShortLinkAndChain);
        _r.Add("T-053", "nine stops (403, 429, sign-in redirect, sign-in wall, CAPTCHA, no product, 5xx, closed port, silent server) each end with its code and the paste offer and no later request; a second click during a read is refused", Stops);
        _r.Add("T-054", "an image on a host outside the image hosts gets no connection and is named; a non-image ends the read (not-image) with the facts and the earlier image kept; the missing images upload at step 2", ForeignImage);
        _r.Add("T-055", "confirmations: an empty operator field is refused by name; N-1 of N confirmations refused (fill-unconfirmed), N accepted; picked read images placed byte-equal; price and description never saved; step 2 none missing", Confirmations);
        _r.Add("T-056", "paste mode: the pasted page text fills exactly what the read fills, with no connection; empty and productless pastes refused", Paste);
        _r.Add("T-057", "the switch: off by default (read refused, no connection), paste and typed saves accepted while off; a missing, garbage or other-version file reads off; on and off survive a restart; one setting", Switch);
        _r.Add("T-058", "eight banned-term pages, classes (a) to (g) and a money amount: the save of the read values gives the typed save's code and line, 8 of 8; the live warning answers within 1 s", BannedPages);
        _r.Add("T-059", "proxy variables set: the proxy listener gets no connection and the read still goes to its own host; a second product needs a second click", Proxy);
        _r.Add("T-060", "the seam is internal: the transport type and the start path are not public, the shipped options refuse a fixture or transport argument, and the fixture host lives in the test program", Seam);
        _r.Add("T-061", "socket tables of the fixture host process: no read connection while the switch is off, during paste or idle; two links refused; one read goes only to the fixture; listeners unchanged by the read; the toolchain owns no socket; proxy variables unused", Harness);
        _r.Add("T-062", "browser, step 1 against the fixture: read part hidden while off; switch on in settings; marks N of N with no read field typed; three read images picked; N-1 confirmations refused, N accepted; input folder holds the three; paste marks", Browser);
        _r.Add("T-063", "image-phase stops (CR-001): 403, 429, 5xx, 410, a redirect, a redirect to sign-in or /verify/, a CAPTCHA page, a sign-in page, a non-image and an image over 40 MiB each end the read at that image with no further request; facts and the earlier image kept; the rest named", ImageStops);
        _r.Add("T-064", "page bounds (CR-004): a page over 5 MiB, a non-HTML page, 404, 204 and a redirect without an address stop after one request with nothing filled; a read past the 120 s click bound stops read-time-over with no further request", PageBounds);
        _r.Add("T-065", "host and hop edges (CR-003): an IDN look-alike, IPv6 literals, an explicit :443, affiliate subdomains, upper-case and trailing-dot hosts refused or read by the rule; hops to http, /verify/ and each edge stop after one request; non-public addresses refused at connect", HostEdges);
        _r.Add("T-066", "request guard on the four new POST routes (CR-005): a foreign, other-port, missing or \"null\" Origin and a missing or wrong token are refused with 403 on /api/read, /api/paste, /api/read-confirm and /api/read-switch, and none reads, fills, confirms or switches", GuardedRoutes);
    }

    // -----------------------------------------------------------------------------------------------------------------------------

    private void LinkRule()
    {
        foreach (var (kind, link, host) in FixtureSite.Disallowed)
        {
            var (requests, connects, answer) = F.During(() => App.Read("V-50", link));
            Expect.Equal("link-not-allowed", answer.Code, $"{kind}: {answer}");
            Expect.Equal(0, connects.Count + requests.Count, $"{kind}: connections {string.Join(", ", connects)}");
            _r.Note($"{kind} ({host}): {answer.Code}, 0 connections");
        }

        for (var i = 0; i < FixtureSite.Disallowed.Count; i++)
        {
            var (kind, _, host) = FixtureSite.Disallowed[i];
            var (requests, connects, answer) = F.During(() => App.Read("V-50", $"https://shopee.vn/chuyen-ra-ngoai-{i + 1}"));
            Expect.Equal("hop-not-allowed", answer.Code, $"hop to {kind}: {answer}");
            Expect.Equal("shopee.vn", string.Join(",", connects), $"hop to {kind}: connections");
            Expect.Equal(1, requests.Count, $"hop to {kind}: requests");
            _r.Note($"hop to {kind} ({host}): {answer.Code}; 1 request to shopee.vn, 0 to {host}");
        }

        var affiliate = F.During(() => App.Read("V-50", FixtureSite.AffiliateLink));
        Expect.Equal("link-affiliate", affiliate.Result.Code, $"affiliate link: {affiliate.Result}");
        Expect.Equal(0, affiliate.Connects.Count, "affiliate link connections");
        var affiliateHop = F.During(() => App.Read("V-50", FixtureSite.AffiliateHopLink));
        Expect.Equal("hop-affiliate", affiliateHop.Result.Code, $"affiliate hop: {affiliateHop.Result}");
        Expect.True(!affiliateHop.Connects.Any(c => c.Contains("affiliate", StringComparison.Ordinal)), "affiliate hop: no connection to the affiliate host");
        _r.Note($"affiliate link: {affiliate.Result.Code}, 0 connections; affiliate hop: {affiliateHop.Result.Code}, connections {string.Join(",", affiliateHop.Connects)}");

        foreach (var two in new[] { FixtureSite.GoodLink + " " + FixtureSite.SecondLink, FixtureSite.GoodLink + FixtureSite.SecondLink, FixtureSite.GoodLink + "\n" + FixtureSite.ShortLink })
        {
            var (requests, connects, answer) = F.During(() => App.Read("V-50", two));
            Expect.Equal("link-two-links", answer.Code, $"two links: {answer}");
            Expect.Equal(0, connects.Count + requests.Count, "two links: connections");
        }

        foreach (var (link, code) in new[] { ("", "link-empty"), ("http://shopee.vn:8443/binh-i.1.2", "link-not-allowed"), ("ftp://shopee.vn/binh", "link-not-allowed"), ("https://user@shopee.vn/binh", "link-not-allowed"), ("https://shopee.vn/buyer/login", "sign-in-redirect") })
        {
            var (requests, connects, answer) = F.During(() => App.Read("V-50", link));
            Expect.Equal(code, answer.Code, $"\"{link}\": {answer}");
            Expect.Equal(0, connects.Count + requests.Count, $"\"{link}\": connections");
        }

        Expect.True(Web.LinkRule.IsImageHost("down-vn.img.susercontent.com") && !Web.LinkRule.IsImageHost("susercontent.com")
            && !Web.LinkRule.IsImageHost("down-vn.img.susercontent.com.example.test") && !Web.LinkRule.IsImageHost("xsusercontent.com"), "image hosts");
        Expect.True(Web.LinkRule.IsPageHost("s.shopee.vn") && !Web.LinkRule.IsPageHost("xshopee.vn") && !Web.LinkRule.IsPageHost("affiliate.shopee.vn"), "page hosts");
        Expect.Equal(0, F.ProxyConnections, "proxy listener");
        _r.Note("two links (space, glued, new line) refused with 0 connections; empty, http with a port, ftp, a user name and a sign-in path refused with 0 connections");
    }

    private void GoodRead()
    {
        var log = Path.Combine(App.Workspace.WebFolder, Web.ReadLog.FileName);
        var logBefore = File.Exists(log) ? File.ReadAllLines(log).Length : 0;
        var (requests, connects, answer) = F.During(() => App.Read("V-51", FixtureSite.GoodLink));
        Expect.True(answer.Ok, $"read: {answer}");
        var fill = answer["fill"];
        _goodFill = fill.Clone();
        Expect.Equal(FixtureSite.Name, Field(fill, "productName"), "product name");
        Expect.Equal(FixtureSite.Classification, Field(fill, "classification"), "classification");
        Expect.Equal(FixtureSite.GoodLink, Field(fill, "pagePath"), "page address");
        Expect.Equal(DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), Field(fill, "readDate"), "read date");
        var candidates = fill.GetProperty("candidates").EnumerateArray().Select(c => (c.GetProperty("attribute").GetString(), c.GetProperty("value").GetString())).ToList();
        Expect.Equal(string.Join(" | ", FixtureSite.Specs.Select(s => $"{s.Attribute}: {s.Value}")), string.Join(" | ", candidates.Select(c => $"{c.Item1}: {c.Item2}")), "candidates equal the page's specification rows");
        Expect.True(fill.GetProperty("fields").EnumerateArray().All(f => f.GetProperty("value").GetString()!.IsNormalized(NormalizationForm.FormC)), "filled values are NFC");

        var images = fill.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();
        Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png, anh-doc-04.png, anh-doc-05.png", string.Join(", ", images), "read image names");
        for (var i = 1; i <= 5; i++)
        {
            var saved = File.ReadAllBytes(Path.Combine(App.Workspace.ReadImageFolder("V-51"), $"anh-doc-{i:00}.png"));
            Expect.True(saved.AsSpan().SequenceEqual(FixtureSite.OwnPng(i)), $"anh-doc-{i:00}.png byte-equal to own image {i}");
            var served = App.GetBytes($"/api/read-image?code=V-51&name=anh-doc-{i:00}.png", out var status);
            Expect.True(status == 200 && served.AsSpan().SequenceEqual(saved), $"read image {i} served to the page");
        }

        App.GetBytes("/api/read-image?code=V-51&name=..%5Cdoc-V-51.json", out var escape);
        Expect.Equal(404, escape, "a name outside the read images is not served");

        var paths = requests.Select(q => q.Host + q.PathOnly).ToList();
        var expected = new[] { "shopee.vn" + FixtureSite.GoodPath }.Concat(FixtureSite.OwnImages.Select(i => new Uri(i).Host + new Uri(i).AbsolutePath)).ToList();
        Expect.Equal(string.Join(" ", expected), string.Join(" ", paths), "exactly the page and its five own images, in that order");
        Expect.Equal(6, answer["requests"].GetInt32(), "requests the app counted");
        foreach (var other in FixtureSite.OtherImages)
        {
            Expect.True(!paths.Contains(new Uri(other).Host + new Uri(other).AbsolutePath), $"other product's image {other} never requested");
        }

        Expect.True(requests.All(q => q.Method == "GET"), "GET only");
        foreach (var q in requests)
        {
            var names = string.Join(",", q.Headers.Select(h => h.Name.ToLowerInvariant()).OrderBy(n => n, StringComparer.Ordinal));
            Expect.Equal("accept,host,user-agent", names, $"header names of {q}");
            Expect.Equal(Web.ReadSender.UserAgent, q.Header("User-Agent"), $"User-Agent of {q}");
            Expect.Equal(q.PathOnly == FixtureSite.GoodPath ? Web.ReadSender.PageAccept : Web.ReadSender.ImageAccept, q.Header("Accept"), $"Accept of {q}");
            Expect.True(!q.Has("Cookie") && !q.Has("Authorization"), $"no cookie or authorization on {q}, after the page set a cookie");
        }

        Expect.Equal(string.Join(",", Enumerable.Repeat("shopee.vn", 1).Concat(Enumerable.Repeat(FixtureSite.ImageHost, 5))), string.Join(",", connects), "connections only to the page host and the image host");
        var local = answer["localAddresses"].EnumerateArray().Select(a => a.GetString()).ToList();
        Expect.Equal("127.0.0.1", string.Join(",", local), "one source address for the read");
        Expect.Equal(1, requests.Select(q => q.Client.Address.ToString()).Distinct().Count(), "one client address seen by the fixture");

        var logLines = File.ReadAllLines(log).Skip(logBefore).ToList();
        Expect.Equal(6, logLines.Count, "six read-log lines");
        Expect.True(logLines.All(l => l.Split('\t')[1] is "shopee.vn" or FixtureSite.ImageHost), $"read-log hosts: {string.Join(" | ", logLines)}");
        Expect.True(!File.ReadAllText(log).Contains("SPC_F", StringComparison.Ordinal), "the cookie value is not logged");
        _r.Note($"fields: {string.Join("; ", fill.GetProperty("fields").EnumerateArray().Select(f => $"{f.GetProperty("member").GetString()}={f.GetProperty("value").GetString()}"))}");
        _r.Note($"requests: {string.Join(", ", requests)}; headers {string.Join(",", requests[0].Headers.Select(h => h.Name))}; source {string.Join(",", local)}");
        _r.Note($"read log: {logLines[0]}");
    }

    private void ShortLinkAndChain()
    {
        EnsureGoodRead();
        var (requests, _, answer) = F.During(() => App.Read("V-52", FixtureSite.ShortLink));
        Expect.True(answer.Ok, $"short link read: {answer}");
        var fill = answer["fill"];
        foreach (var member in new[] { "productName", "classification", "pagePath" })
        {
            Expect.Equal(Field(_goodFill, member), Field(fill, member), $"short link {member}");
        }

        Expect.Equal(_goodFill.GetProperty("candidates").GetRawText(), fill.GetProperty("candidates").GetRawText(), "short link candidates");
        Expect.Equal(_goodFill.GetProperty("images").GetArrayLength(), fill.GetProperty("images").GetArrayLength(), "short link images");
        Expect.Equal(8, requests.Count, $"two hops, the page and five images: {string.Join(", ", requests)}");
        _r.Note($"short link: {string.Join(" -> ", requests.Take(3))}; fields equal the full link's; page address {Field(fill, "pagePath")}");

        var before = Web.FillStore.Load(App.Workspace, "V-52");
        var chain = F.During(() => App.Read("V-52", FixtureSite.ChainLink));
        Expect.Equal("too-many-hops", chain.Result.Code, $"six hops: {chain.Result}");
        Expect.Equal(6, chain.Requests.Count, $"six hop requests: {string.Join(", ", chain.Requests)}");
        Expect.True(!chain.Requests.Any(q => q.PathOnly == "/chuoi-6"), "the sixth hop's target never requested");
        Expect.True(chain.Result["fill"].ValueKind == JsonValueKind.Null, "nothing filled");
        Expect.Equal(before?.FilledAt, Web.FillStore.Load(App.Workspace, "V-52")?.FilledAt, "the earlier fill record is kept");
        _r.Note($"chain: {chain.Result.Code} after {chain.Requests.Count} requests ({chain.Requests[0]} .. {chain.Requests[^1]})");
    }

    private void Stops()
    {
        foreach (var (name, link, code) in FixtureSite.Stops)
        {
            var watch = Stopwatch.StartNew();
            Task<Answer>? second = null;
            (IReadOnlyList<FixtureRequest> Requests, IReadOnlyList<string> Connects, Answer Result) during;
            if (code == "no-answer")
            {
                // The silent server: while this read waits, a second click is refused and opens nothing.
                during = F.During(() =>
                {
                    var running = Task.Run(() => App.Read("V-53", link));
                    Thread.Sleep(2000);
                    second = Task.FromResult(F.During(() => App.Read("V-53b", FixtureSite.GoodLink)).Result);
                    return running.GetAwaiter().GetResult();
                });
            }
            else
            {
                during = F.During(() => App.Read("V-53", link));
            }

            watch.Stop();
            var answer = during.Result;
            Expect.Equal(200, answer.Status, $"{name} status");
            Expect.True(!answer.Ok, $"{name} not ok");
            Expect.Equal(code, answer.Code, $"{name}: {answer}");
            Expect.True(answer["offerPaste"].GetBoolean(), $"{name}: paste offered");
            Expect.True(answer.Line.StartsWith($"DỪNG ĐỌC [{code}]: ", StringComparison.Ordinal), $"{name} line: {answer.Line}");
            Expect.True(answer.Outcome!.Value.GetProperty("nextStep").GetString()!.Contains("Dán chữ của trang sản phẩm", StringComparison.Ordinal), $"{name}: next step names paste mode");
            Expect.True(during.Requests.All(q => q.Method == "GET"), $"{name}: GET only");
            var expectedRequests = code is "no-connection" or "no-answer" ? 0 : 1;
            Expect.Equal(expectedRequests, during.Requests.Count, $"{name}: requests {string.Join(", ", during.Requests)}");
            var count = F.Requests.Count;
            Thread.Sleep(1000);
            Expect.Equal(count, F.Requests.Count, $"{name}: no request after the stop");
            if (code == "no-answer")
            {
                Expect.True(watch.Elapsed.TotalSeconds is >= 29 and <= 40, $"silent server stopped after {watch.Elapsed.TotalSeconds:0.0} s");
                var busy = second!.GetAwaiter().GetResult();
                Expect.Equal(409, busy.Status, $"second click during a read: {busy}");
                Expect.Equal("read-busy", busy.Code, "second click code");
            }

            _r.Note($"{name}: {answer.Line.Trim()} ({during.Requests.Count} request, {during.Connects.Count} connection, {watch.Elapsed.TotalSeconds:0.0} s)");
        }

        Expect.True(Web.FillStore.Load(App.Workspace, "V-53") is null, "no stop in the page phase filled anything");
        Expect.True(!Directory.Exists(App.Workspace.ReadImageFolder("V-53b")) || !Directory.EnumerateFiles(App.Workspace.ReadImageFolder("V-53b")).Any(), "the refused second click read nothing");
    }

    private void ForeignImage()
    {
        var (requests, connects, answer) = F.During(() => App.Read("V-54", FixtureSite.ForeignImageLink));
        // Review correction CR-001: the non-image (an HTML page answered for an image) ends the read; the facts and the image saved
        // before it are kept.
        Expect.True(!answer.Ok, $"read: {answer}");
        Expect.Equal("not-image", answer.Code, $"the non-image ends the read: {answer}");
        Expect.True(answer["offerPaste"].GetBoolean(), "paste offered");
        Expect.True(!connects.Contains(FixtureSite.ForeignImageHost), $"no connection to {FixtureSite.ForeignImageHost}: {string.Join(",", connects)}");
        var fill = answer["fill"];
        var skipped = fill.GetProperty("notFetched").EnumerateArray().Select(s => (Address: s.GetProperty("address").GetString()!, Reason: s.GetProperty("reason").GetString()!)).ToList();
        Expect.True(skipped.Any(s => s.Address.Contains(FixtureSite.ForeignImageHost, StringComparison.Ordinal) && s.Reason.Contains(FixtureSite.ForeignImageHost, StringComparison.Ordinal)), "the foreign image is named with its host");
        Expect.True(skipped.Any(s => s.Address.EndsWith("mc18-khong-phai-anh", StringComparison.Ordinal) && s.Reason.Contains("không phải ảnh", StringComparison.Ordinal)), "the non-image is named");
        Expect.Equal("anh-doc-01.png", string.Join(",", fill.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString())), "one image kept");
        Expect.Equal(3, requests.Count, $"the page, the image and the non-image: {string.Join(", ", requests)}");
        var after = F.Requests.Count;
        Thread.Sleep(1000);
        Expect.Equal(after, F.Requests.Count, "no request after the stop");

        UploadScreenshot("V-54");
        ConfirmAll("V-54", fill);
        var sheet = SheetFrom("V-54", fill, ["anh-doc-01.png", "anh-1.png", "anh-2.png"], "cốc sứ");
        var saved = App.Post("/api/facts?code=V-54", sheet);
        Expect.True(saved.Ok, $"save: {saved}");
        foreach (var name in new[] { "anh-1.png", "anh-2.png" })
        {
            Expect.True(App.PostBytes($"/api/image?code=V-54&name={name}&source={name}", File.ReadAllBytes(Path.Combine(Stills, name))).Ok, $"step 2 upload {name}");
        }

        var status = App.Get("/api/video?code=V-54");
        Expect.True(status["images"].EnumerateArray().All(i => i.GetProperty("present").GetBoolean()), $"step 2 none missing: {status["images"].GetRawText()}");
        _r.Note($"not fetched: {string.Join(" | ", skipped.Select(s => $"{s.Address}: {s.Reason}"))}");
        _r.Note($"requests {string.Join(", ", requests)}; step 2 after two uploads: {status["images"].GetRawText()}");
    }

    private void Confirmations()
    {
        EnsureGoodRead();
        var read = App.Read("V-55", FixtureSite.GoodLink);
        Expect.True(read.Ok, $"read: {read}");
        var fill = read["fill"];
        UploadScreenshot("V-55");
        var picked = new[] { "anh-doc-01.png", "anh-doc-03.png", "anh-doc-05.png" };
        var sheet = SheetFrom("V-55", fill, picked, "bình giữ nhiệt");

        var empty = SheetFrom("V-55", fill, picked, "bình giữ nhiệt");
        empty["subjectNoun"] = "";
        var refusedEmpty = App.Post("/api/facts?code=V-55", empty);
        Expect.True(!refusedEmpty.Ok, $"empty operator field: {refusedEmpty}");
        Expect.True(refusedEmpty.Body.GetRawText().Contains("Chủ thể", StringComparison.Ordinal), $"the refusal names the field: {refusedEmpty}");

        var keys = fill.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("member").GetString()!)
            .Concat(fill.GetProperty("candidates").EnumerateArray().Take(3).Select(c => c.GetProperty("key").GetString()!)).ToList();
        Expect.Equal(7, keys.Count, "N = four filled fields and three rows");
        var unconfirmed = App.Post("/api/facts?code=V-55", sheet);
        Expect.Equal("fill-unconfirmed", unconfirmed.Code, $"no confirmation: {unconfirmed}");
        foreach (var key in keys.Take(keys.Count - 1))
        {
            Expect.True(App.Post("/api/read-confirm?code=V-55", new { field = key }).Ok, $"confirm {key}");
        }

        var almost = App.Post("/api/facts?code=V-55", sheet);
        Expect.Equal("fill-unconfirmed", almost.Code, $"N-1 confirmations: {almost}");
        Expect.True(almost.Line.Contains("F3", StringComparison.Ordinal), $"the refusal names the unconfirmed row: {almost.Line}");
        Expect.Equal("fill-field-unknown", App.Post("/api/read-confirm?code=V-55", new { field = "khong-co" }).Code, "unknown field");

        var edited = sheet.DeepClone().AsObject();
        edited["facts"]![2]!["value"] = "giá trị gõ lại";
        Expect.True(App.Post("/api/facts?code=V-55", edited).Ok, "an edited value counts as typed and needs no confirmation");
        Expect.True(App.Post("/api/read-confirm?code=V-55", new { field = keys[^1] }).Ok, "confirm the last");
        var accepted = App.Post("/api/facts?code=V-55", sheet);
        Expect.True(accepted.Ok, $"N confirmations: {accepted}");

        var input = App.Workspace.InputFolder("V-55");
        for (var i = 0; i < picked.Length; i++)
        {
            var source = File.ReadAllBytes(Path.Combine(App.Workspace.ReadImageFolder("V-55"), picked[i]));
            Expect.True(File.ReadAllBytes(Path.Combine(input, picked[i])).AsSpan().SequenceEqual(source), $"{picked[i]} placed byte-equal");
        }

        var savedSheet = File.ReadAllText(App.Workspace.FactSheetPath("V-55"));
        Expect.True(!savedSheet.Contains("189.000", StringComparison.Ordinal) && !savedSheet.Contains("189000", StringComparison.Ordinal), "price not saved");
        Expect.True(!savedSheet.Contains("thép không gỉ, nắp vặn", StringComparison.OrdinalIgnoreCase) && !savedSheet.Contains("máy rửa bát", StringComparison.Ordinal), "description not saved");
        var status = App.Get("/api/video?code=V-55");
        Expect.True(status["images"].EnumerateArray().All(i => i.GetProperty("present").GetBoolean()), $"step 2 none missing: {status["images"].GetRawText()}");
        Expect.True(status["fill"].ValueKind == JsonValueKind.Object && status["readOn"].GetBoolean(), "the video status carries the fill record and the switch");
        _r.Note($"empty field: {refusedEmpty.Code} {refusedEmpty.Line.Trim()}");
        _r.Note($"N = {keys.Count} ({string.Join(", ", keys)}); none: {unconfirmed.Code}; N-1: {almost.Code} \"{almost.Line.Trim()}\"; N: accepted ({accepted["message"].GetString()})");
        _r.Note($"input folder: {string.Join(", ", Directory.EnumerateFiles(input).Select(Path.GetFileName).Order(StringComparer.Ordinal))}");
    }

    private void Paste()
    {
        EnsureGoodRead();
        var (requests, connects, answer) = F.During(() => App.Paste("V-56", FixtureSite.GoodPasteText()));
        Expect.True(answer.Ok, $"paste: {answer}");
        Expect.Equal(0, requests.Count + connects.Count, "paste opens no connection");
        var fill = answer["fill"];
        Expect.Equal("paste", fill.GetProperty("source").GetString(), "source");
        foreach (var member in new[] { "productName", "classification" })
        {
            Expect.Equal(Field(_goodFill, member), Field(fill, member), $"paste {member} equals the read");
        }

        Expect.Equal(_goodFill.GetProperty("candidates").GetRawText(), fill.GetProperty("candidates").GetRawText(), "paste candidates equal the read");
        Expect.Equal(_goodFill.GetProperty("price").GetString(), fill.GetProperty("price").GetString(), "paste price equals the read");
        Expect.Equal(_goodFill.GetProperty("description").GetString(), fill.GetProperty("description").GetString(), "paste description equals the read");
        Expect.True(Field(fill, "pagePath") is null && Field(fill, "readDate") is null, "page address and read date left for the operator");
        Expect.Equal("paste-empty", App.Paste("V-56", "  ").Code, "empty paste");
        Expect.Equal("paste-no-product", App.Paste("V-56", "Trang chủ\nĐăng nhập\nGiỏ hàng").Code, "paste with no product");
        _r.Note($"paste: {string.Join("; ", fill.GetProperty("fields").EnumerateArray().Select(f => $"{f.GetProperty("member").GetString()}={f.GetProperty("value").GetString()}"))}; {fill.GetProperty("candidates").GetArrayLength()} candidates; 0 connections");
    }

    private void Switch()
    {
        var root = Path.Combine(_root, "switch");
        var pilot = Path.Combine(root, "pilot");
        var file = Web.ReadSwitch.PathIn(Path.Combine(root, "app"));
        using (var app = ReadApp.Start(root, F, pilot))
        {
            Expect.True(!File.Exists(file), "no switch file before the operator sets one");
            Expect.True(!app.Get("/api/read-switch")["readOn"].GetBoolean(), "off by default");
            Expect.True(!app.Get("/api/video?code=V-57")["readOn"].GetBoolean(), "step 1 is told the read is off");
            var (requests, connects, refused) = F.During(() => app.Read("V-57", FixtureSite.GoodLink));
            Expect.Equal(409, refused.Status, $"read while off: {refused}");
            Expect.Equal("read-off", refused.Code, "read-off code");
            Expect.Equal(0, requests.Count + connects.Count, "no connection while off");
            var paste = F.During(() => app.Paste("V-57", FixtureSite.GoodPasteText()));
            Expect.True(paste.Result.Ok && paste.Requests.Count + paste.Connects.Count == 0, $"paste while off: {paste.Result}");
            app.PostBytes("/api/screenshot?code=V-57&name=chup-1.png", File.ReadAllBytes(Path.Combine(Stills, "chup-1.png")));
            var pasted = paste.Result["fill"];
            foreach (var key in pasted.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("member").GetString()!)
                .Concat(pasted.GetProperty("candidates").EnumerateArray().Take(3).Select(c => c.GetProperty("key").GetString()!)))
            {
                Expect.True(app.Post("/api/read-confirm?code=V-57", new { field = key }).Ok, $"confirm {key}");
            }

            var pasteSheet = SheetFrom("V-57", pasted, ["anh-1.png", "anh-2.png", "anh-3.png"], "bình giữ nhiệt");
            pasteSheet["pagePath"] = "/san-pham/binh-giu-nhiet-mau";
            pasteSheet["readDate"] = "2026-10-30";
            Expect.True(app.Post("/api/facts?code=V-57", pasteSheet).Ok, "paste save accepted while off");
            app.PostBytes("/api/screenshot?code=V-57t&name=chup-1.png", File.ReadAllBytes(Path.Combine(Stills, "chup-1.png")));
            Expect.True(app.Post("/api/facts?code=V-57t", Fixtures.Sheet("V-57t", "/san-pham/binh-go-tay")).Ok, "typed save accepted while off");

            foreach (var (what, text) in new[] { ("garbage", "{không phải json"), ("other version", "{\"formVersion\": 2, \"readOn\": true}"), ("empty", "") })
            {
                File.WriteAllText(file, text);
                Expect.True(!app.Get("/api/read-switch")["readOn"].GetBoolean(), $"{what} file reads off");
                var read = F.During(() => app.Read("V-57", FixtureSite.GoodLink));
                Expect.Equal("read-off", read.Result.Code, $"{what} file: read refused");
                Expect.Equal(0, read.Connects.Count, $"{what} file: no connection");
            }

            File.Delete(file);
            Expect.True(app.Switch(true).Ok, "switch on");
            var written = JsonNode.Parse(File.ReadAllText(file))!.AsObject();
            Expect.Equal("formVersion,readOn", string.Join(",", written.Select(p => p.Key)), "the switch file holds one setting and its form version");
            Expect.True(written["readOn"]!.GetValue<bool>() && written["formVersion"]!.GetValue<int>() == 1, "on at form version 1");
            _r.Note($"switch file {file}: {written.ToJsonString()}");
        }

        using (var app = ReadApp.Start(root, F, pilot))
        {
            Expect.True(app.Get("/api/read-switch")["readOn"].GetBoolean(), "on after a restart");
            Expect.True(app.Read("V-57r", FixtureSite.SecondLink).Ok, "read after a restart");
            Expect.True(app.Switch(false).Ok, "switch off");
        }

        using (var app = ReadApp.Start(root, F, pilot))
        {
            Expect.True(!app.Get("/api/read-switch")["readOn"].GetBoolean(), "off after a restart");
            var refused = F.During(() => app.Read("V-57", FixtureSite.GoodLink));
            Expect.Equal("read-off", refused.Result.Code, "read refused after the restart");
            Expect.Equal(0, refused.Connects.Count, "no connection");
        }

        var appFiles = Directory.EnumerateFiles(Path.Combine(root, "app")).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        _r.Note($"app folder: {string.Join(", ", appFiles)}; off by default, refused with 0 connections; on and off each survived a restart");
    }

    private void BannedPages()
    {
        var equal = 0;
        foreach (var (id, link, value) in FixtureSite.BannedPages)
        {
            var code = $"V-58{id}";
            UploadScreenshot(code);
            var probe = App.Read(code + "p", link);
            Expect.True(probe.Ok, $"{id}: read {probe}");
            var sheet = SheetFrom(code, probe["fill"], ["anh-1.png", "anh-2.png", "anh-3.png"], "bình giữ nhiệt", rows: 2);
            sheet["pagePath"] = "/san-pham/tu-cam-" + id;

            var typed = App.Post($"/api/facts?code={code}", sheet);
            if (typed.Ok)
            {
                File.Delete(App.Workspace.FactSheetPath(code));
            }

            var read = App.Read(code, link);
            Expect.True(read.Ok, $"{id}: read {read}");
            ConfirmAll(code, read["fill"]);
            var filled = App.Post($"/api/facts?code={code}", sheet);
            Expect.Equal(typed.Ok, filled.Ok, $"{id}: accepted alike");
            Expect.Equal(typed.Code, filled.Code, $"{id}: code");
            Expect.Equal(Direct.FirstLine(typed.Line), Direct.FirstLine(filled.Line), $"{id}: line");
            if (id != "tien")
            {
                Expect.True(!typed.Ok, $"{id}: the banned term is refused");
            }

            var watch = Stopwatch.StartNew();
            var live = App.Post("/api/live-check", new { fields = new[] { new { where = "Dòng F2, Giá trị", text = value, isOverride = false } } });
            watch.Stop();
            Expect.True(watch.Elapsed.TotalSeconds < 1.0, $"{id}: live answer in {watch.Elapsed.TotalMilliseconds:0} ms");
            var findings = live["findings"].EnumerateArray().Select(f => f.GetProperty("classId").GetString()).ToList();
            if (id != "tien")
            {
                Expect.True(findings.Contains(id), $"{id}: live finding class {string.Join(",", findings)}");
            }

            equal++;
            _r.Note($"({id}) \"{value}\": typed {(typed.Ok ? "accepted" : typed.Code)}, read {(filled.Ok ? "accepted" : filled.Code)}, line equal; live {watch.Elapsed.TotalMilliseconds:0} ms, classes {string.Join(",", findings)}");
        }

        Expect.Equal(8, equal, "8 of 8");
    }

    private void Proxy()
    {
        var names = new[] { "HTTPS_PROXY", "HTTP_PROXY", "ALL_PROXY", "https_proxy", "http_proxy", "all_proxy" };
        var saved = names.ToDictionary(n => n, Environment.GetEnvironmentVariable);
        var proxy = $"http://127.0.0.1:{F.ProxyPort}";
        try
        {
            foreach (var name in names)
            {
                Environment.SetEnvironmentVariable(name, proxy);
            }

            var (requests, connects, answer) = F.During(() => App.Read("V-59", FixtureSite.GoodLink));
            Expect.True(answer.Ok, $"read with proxy variables: {answer}");
            Expect.Equal(0, F.ProxyConnections, "the proxy listener got no connection");
            Expect.Equal(6, requests.Count, "the read went to its own hosts");
            Expect.True(connects.All(c => c is "shopee.vn" or FixtureSite.ImageHost), "connections to the page and image hosts only");
            var second = F.During(() => App.Read("V-59b", FixtureSite.SecondLink));
            Expect.True(second.Result.Ok, $"second product: {second.Result}");
            Expect.True(!requests.Any(q => q.PathOnly == new Uri(FixtureSite.SecondLink).AbsolutePath), "the first click asked nothing of the second product");
            Expect.Equal(2, second.Requests.Count, "the second click: its page and its one image");
            _r.Note($"proxy variables -> {proxy}: {F.ProxyConnections} proxy connections; first click {requests.Count} requests, second click {second.Requests.Count} ({string.Join(", ", second.Requests)})");
        }
        finally
        {
            foreach (var (name, value) in saved)
            {
                Environment.SetEnvironmentVariable(name, value);
            }
        }
    }

    private void Seam()
    {
        var web = typeof(Web.Program).Assembly;
        Expect.True(!typeof(ReadTransport).IsPublic && !typeof(ReadTransport).IsNestedPublic, "the transport type is internal");
        var start = typeof(Web.Program).GetMethod("Start", BindingFlags.Static | BindingFlags.NonPublic);
        Expect.True(start is { IsAssembly: true }, "the start path is internal");
        var exposed = web.GetExportedTypes()
            .SelectMany(t => t.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(m => m switch
            {
                MethodBase method => method.GetParameters().Any(p => p.ParameterType == typeof(ReadTransport)),
                PropertyInfo property => property.PropertyType == typeof(ReadTransport) && property.GetMethod?.IsPublic == true,
                _ => false,
            })
            .Select(m => $"{m.DeclaringType?.Name}.{m.Name}").ToList();
        Expect.Equal(0, exposed.Count, $"public members taking or giving the seam: {string.Join(", ", exposed)}");
        foreach (var args in new[] { new[] { "--fixture-host" }, ["--transport", "x"], ["--fixture"], ["--read-host", "127.0.0.1"] })
        {
            Expect.True(!WebOptions.TryParse(args, out _), $"the shipped options refuse {string.Join(" ", args)}");
        }

        Expect.True(typeof(FixtureHost).Assembly != web && typeof(ReadFixture).Assembly != web, "the fixture lives in the test program");
        Expect.True(!web.GetTypes().Any(t => t.Name.Contains("Fixture", StringComparison.Ordinal)), "no fixture type in the app");
        _r.Note($"ReadTransport {(typeof(ReadTransport).IsNotPublic ? "internal" : "public")}; Program.Start {(start!.IsAssembly ? "internal" : "public")}; 4 argument forms refused");
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // The socket-table case: this program in fixture host mode, as a child process inside the harness job.
    // -----------------------------------------------------------------------------------------------------------------------------

    private void Harness()
    {
        var root = Path.Combine(_root, "harness");
        Directory.CreateDirectory(root);
        var idle = int.TryParse(Environment.GetEnvironmentVariable("MC18_IDLE_SECONDS"), out var seconds) && seconds > 0 ? seconds : 20;
        using var proxy = new TcpListener(IPAddress.Loopback, 0);
        proxy.Start();
        var proxyPort = ((IPEndPoint)proxy.LocalEndpoint).Port;
        var proxyHits = 0;
        using var stopProxy = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!stopProxy.IsCancellationRequested)
            {
                try
                {
                    using var client = await proxy.AcceptTcpClientAsync(stopProxy.Token);
                    Interlocked.Increment(ref proxyHits);
                }
                catch (Exception)
                {
                    return;
                }
            }
        });

        var start = new ProcessStartInfo(Environment.ProcessPath!)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };
        foreach (var argument in new[] { FixtureHost.Argument, "--port", "0", "--switch", "off", "--root", root })
        {
            start.ArgumentList.Add(argument);
        }

        foreach (var name in new[] { "HTTPS_PROXY", "HTTP_PROXY", "ALL_PROXY" })
        {
            start.Environment[name] = $"http://127.0.0.1:{proxyPort}";
        }

        using var harness = new AddressHarness();
        using var host = Process.Start(start) ?? throw new CaseFailure("the fixture host did not start");
        try
        {
            harness.Track(host);
            _ = host.StandardError.ReadToEndAsync();
            string? ready = null;
            var reading = Task.Run(() =>
            {
                while (host.StandardOutput.ReadLine() is { } line)
                {
                    if (line.StartsWith(FixtureHost.ReadyPrefix, StringComparison.Ordinal))
                    {
                        ready = line[FixtureHost.ReadyPrefix.Length..];
                        return;
                    }
                }
            });
            Expect.True(reading.Wait(TimeSpan.FromSeconds(60)) && ready is not null, "the fixture host printed its ready line");
            _ = host.StandardOutput.ReadToEndAsync();
            var parts = ready!.Split(' ');
            var origin = parts[0];
            var ports = parts.Skip(1).Select(p => p.Split('=')).Where(p => p[0].EndsWith("-port", StringComparison.Ordinal)).ToDictionary(p => p[0], p => int.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture));
            var fixturePorts = new HashSet<int> { ports["fixture-port"], ports["silent-port"], ports["closed-port"], ports["proxy-port"] };
            var appPort = new Uri(origin).Port;
            using var client = new HttpClient(new SocketsHttpHandler { UseProxy = false }) { BaseAddress = new Uri(origin), Timeout = TimeSpan.FromMinutes(5) };
            var token = System.Text.RegularExpressions.Regex.Match(client.GetStringAsync("/").GetAwaiter().GetResult(), "name=\"mc-token\" content=\"([0-9a-f]{64})\"").Groups[1].Value;
            Answer Post(string path, object body)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json") };
                request.Headers.TryAddWithoutValidation("Origin", origin);
                request.Headers.TryAddWithoutValidation(GuardState.TokenHeader, token);
                using var response = client.SendAsync(request).GetAwaiter().GetResult();
                return new Answer((int)response.StatusCode, JsonSerializer.Deserialize<JsonElement>(response.Content.ReadAsStringAsync().GetAwaiter().GetResult()));
            }

            var readLog = Path.Combine(root, "pilot", "web", Web.ReadLog.FileName);
            int LogLines() => File.Exists(readLog) ? File.ReadAllLines(readLog).Length : 0;
            List<SocketRow> ReadRows() => [.. harness.Rows.Where(row => row.Owner == host.Id && fixturePorts.Contains(row.RemotePort))];
            List<SocketRow> Listeners() => [.. harness.Rows.Where(row => row.Owner == host.Id && row.State == SocketRow.Listen && !fixturePorts.Contains(row.LocalPort))];
            Thread.Sleep(500);
            var listenersBefore = Listeners();

            // Switch off: the read is refused and nothing reaches the fixture.
            var off = Post("/api/read?code=V-61", new { link = FixtureSite.GoodLink });
            Expect.Equal("read-off", off.Code, $"read while off: {off}");
            // Paste: nothing reaches the fixture.
            Expect.True(Post("/api/paste?code=V-61", new { text = FixtureSite.GoodPasteText() }).Ok, "paste");
            Thread.Sleep(300);
            Expect.Equal(0, ReadRows().Count, $"rows to the fixture while off and during paste: {string.Join("; ", ReadRows())}");
            Expect.Equal(0, LogLines(), "no read-log line while off");

            // Idle with the switch on: nothing is sent until the button is pressed.
            Expect.True(Post("/api/read-switch", new { on = true }).Ok, "switch on");
            Thread.Sleep(idle * 1000);
            Expect.Equal(0, ReadRows().Count, $"rows to the fixture while idle {idle} s with the switch on");
            Expect.Equal(0, LogLines(), "no read-log line while idle");
            var two = Post("/api/read?code=V-61", new { link = FixtureSite.GoodLink + " " + FixtureSite.SecondLink });
            Expect.Equal("link-two-links", two.Code, "two links");
            Thread.Sleep(300);
            Expect.Equal(0, ReadRows().Count, "rows after the two-link refusal");

            // One read.
            var read = Post("/api/read?code=V-61", new { link = FixtureSite.GoodLink });
            Expect.True(read.Ok, $"read: {read}");
            Thread.Sleep(500);
            var readRows = ReadRows();
            Expect.True(readRows.All(row => row.IsLoopback && row.RemotePort == ports["fixture-port"]), $"read rows: {string.Join("; ", readRows)}");
            var logged = File.ReadAllLines(readLog);
            Expect.Equal(6, logged.Length, "six read-log lines");
            Expect.True(logged.All(l => l.Split('\t')[1] is "shopee.vn" or FixtureSite.ImageHost), "read-log hosts allowed");
            var listenersAfter = Listeners();
            Expect.Equal(string.Join("; ", listenersBefore.Select(l => $"{l.Local}:{l.LocalPort}")), string.Join("; ", listenersAfter.Select(l => $"{l.Local}:{l.LocalPort}")), "the read added no listener");
            Expect.True(listenersAfter.Count == 1 && listenersAfter[0].Local.Equals(IPAddress.Loopback) && listenersAfter[0].LocalPort == appPort, $"one app listener on 127.0.0.1: {string.Join("; ", listenersAfter)}");

            // The toolchain runs on the read values and owns no socket.
            var fill = read["fill"];
            using (var shot = new ByteArrayContent(File.ReadAllBytes(Path.Combine(Stills, "chup-1.png"))))
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, "/api/screenshot?code=V-61&name=chup-1.png") { Content = shot };
                request.Headers.TryAddWithoutValidation("Origin", origin);
                request.Headers.TryAddWithoutValidation(GuardState.TokenHeader, token);
                client.SendAsync(request).GetAwaiter().GetResult().Dispose();
            }

            foreach (var key in fill.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("member").GetString()!)
                .Concat(fill.GetProperty("candidates").EnumerateArray().Take(3).Select(c => c.GetProperty("key").GetString()!)))
            {
                Expect.True(Post("/api/read-confirm?code=V-61", new { field = key }).Ok, $"confirm {key}");
            }

            var saved = Post("/api/facts?code=V-61", SheetFrom("V-61", fill, ["anh-doc-01.png", "anh-doc-02.png", "anh-doc-03.png"], "bình giữ nhiệt"));
            Expect.True(saved.Ok, $"save: {saved}");
            var script = Post("/api/script?code=V-61", new { });
            _r.Note($"script on the read values: {(script.Ok ? "accepted" : script.Code + " " + script.Line.Trim())}");
            Thread.Sleep(300);
            var members = harness.Members.Where(m => m.Id != host.Id).ToList();
            Expect.True(members.Any(m => m.Name == "MediaCompany.Pilot"), $"the toolchain ran: {string.Join(", ", members.Select(m => m.Name))}");
            var foreign = harness.Rows.Where(row => row.Owner != host.Id).ToList();
            Expect.Equal(0, foreign.Count, $"rows owned by the toolchain or its tools: {string.Join("; ", foreign)}");
            var outside = harness.Rows.Where(row => !row.IsLoopback).ToList();
            Expect.Equal(0, outside.Count, $"rows off loopback: {string.Join("; ", outside)}");
            Expect.Equal(0, Volatile.Read(ref proxyHits), "the proxy listener named by the variables got no connection");
            _r.Note($"{harness.Samples} samples; idle {idle} s; listeners {string.Join("; ", listenersAfter.Select(l => $"{l.Local}:{l.LocalPort}"))}; read rows {readRows.Count}, all to fixture port {ports["fixture-port"]}; members {string.Join(", ", members.Select(m => m.Name).Distinct())}; 0 member rows; proxy 0");
        }
        finally
        {
            stopProxy.Cancel();
            if (!host.HasExited)
            {
                host.Kill(entireProcessTree: true);
                host.WaitForExit(10000);
            }
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // The page case: step 1 in the headless shell against the in-process app and the fixture.
    // -----------------------------------------------------------------------------------------------------------------------------

    private void Browser()
    {
        var root = Path.Combine(_root, "browser");
        var resultPath = Path.Combine(root, "result.json");
        Directory.CreateDirectory(root);
        using var app = ReadApp.Start(root, F, Path.Combine(root, "pilot"), testMode: true, resultPath: resultPath);
        const string code = "V-62";
        const string pasteCode = "V-62p";
        app.PostBytes($"/api/screenshot?code={code}&name=chup-1.png", File.ReadAllBytes(Path.Combine(Stills, "chup-1.png")));
        var url = $"{app.Origin}/#test-read={Uri.EscapeDataString(code)}&paste={Uri.EscapeDataString(pasteCode)}&link={Uri.EscapeDataString(FixtureSite.GoodLink)}&text={Uri.EscapeDataString(FixtureSite.GoodPasteText())}";
        var browserHarness = new AddressHarness();
        Process? browser = null;
        try
        {
            browser = BrowserCase.Launch(Path.Combine(root, "profile"), url, []);
            browserHarness.Track(browser);
            var watch = Stopwatch.StartNew();
            while (!File.Exists(resultPath) && watch.Elapsed < TimeSpan.FromMinutes(4) && !browser.HasExited)
            {
                Thread.Sleep(300);
            }

            Expect.True(File.Exists(resultPath), $"the read driver posted no result in {watch.Elapsed.TotalSeconds:0} s (browser exited: {browser.HasExited})");
            Thread.Sleep(200);
            using var result = JsonDocument.Parse(File.ReadAllText(resultPath));
            var o = result.RootElement;
            _r.Note($"driver result: {o.GetRawText()}");
            Expect.True(o.GetProperty("ok").GetBoolean(), $"driver error: {(o.TryGetProperty("error", out var e) ? e.GetString() : "?")}");
            Expect.True(o.GetProperty("offLinkHidden").GetBoolean() && o.GetProperty("offNoteShown").GetBoolean(), "read part hidden and the off note shown while off");
            Expect.True(!o.GetProperty("settingsBefore").GetBoolean() && o.GetProperty("onLinkShown").GetBoolean(), "switched on in the settings view; read part shown");
            var n = o.GetProperty("expectedMarks").GetInt32();
            Expect.Equal(7, n, "N");
            Expect.Equal(n, o.GetProperty("marksAfterRead").GetInt32(), "marks N of N after the read");
            Expect.Equal(0, o.GetProperty("typedReadFields").GetInt32(), "no read field typed");
            Expect.Equal(n, o.GetProperty("marksAfterTyping").GetInt32(), "marks kept while the operator types own fields");
            Expect.True(o.GetProperty("markText").GetString()!.Contains("Đọc từ trang", StringComparison.Ordinal), "mark names the read");
            Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png", o.GetProperty("imagesUsed").GetString(), "three read images picked");
            Expect.True(!o.GetProperty("fourthPicked").GetBoolean(), "a fourth read image refused");
            Expect.Equal(5, o.GetProperty("thumbnails").GetInt32(), "five thumbnails shown");
            Expect.Equal("fill-unconfirmed", o.GetProperty("saveBefore").GetProperty("code").GetString(), "N-1 confirmations refused");
            Expect.True(o.GetProperty("saveAfter").GetProperty("ok").GetBoolean(), $"N confirmations accepted: {o.GetProperty("saveAfter").GetRawText()}");
            Expect.Equal(n, o.GetProperty("marksConfirmed").GetInt32(), "N marks confirmed");
            Expect.True(o.GetProperty("pasteOk").GetBoolean(), "paste");
            Expect.Equal(o.GetProperty("pasteExpectedMarks").GetInt32(), o.GetProperty("pasteMarks").GetInt32(), "paste marks");
            Expect.True(o.GetProperty("pasteMarkText").GetString()!.Contains("Từ chữ đã dán", StringComparison.Ordinal), "paste mark names the paste");

            var input = app.Workspace.InputFolder(code);
            var placed = Directory.EnumerateFiles(input, "anh-doc-*").Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
            Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png", string.Join(", ", placed), "the input folder holds the three picked");
            var status = app.Get($"/api/video?code={code}");
            Expect.True(status["images"].EnumerateArray().All(i => i.GetProperty("present").GetBoolean()), $"step 2 none missing: {status["images"].GetRawText()}");
            var outside = browserHarness.Rows.Where(row => row.Protocol.StartsWith("tcp", StringComparison.Ordinal) && !row.IsLoopback).ToList();
            Expect.Equal(0, outside.Count, $"browser TCP rows off loopback: {string.Join("; ", outside)}");
            _r.Note($"input folder: {string.Join(", ", Directory.EnumerateFiles(input).Select(Path.GetFileName).Order(StringComparer.Ordinal))}; browser {browserHarness.Rows.Count} rows, TCP all loopback");
        }
        finally
        {
            if (browser is { HasExited: false })
            {
                browser.Kill(entireProcessTree: true);
                browser.WaitForExit(10000);
            }

            browser?.Dispose();
            browserHarness.Dispose();
        }
    }

    // -----------------------------------------------------------------------------------------------------------------------------
    // Review corrections CR-001, CR-003, CR-004 and CR-005.
    // -----------------------------------------------------------------------------------------------------------------------------

    private void ImageStops()
    {
        const string code = "V-63";
        var log = Path.Combine(App.Workspace.WebFolder, Web.ReadLog.FileName);
        foreach (var (name, link, image, stopCode) in FixtureSite.ImageStops)
        {
            var logBefore = File.Exists(log) ? File.ReadAllLines(log).Length : 0;
            var (requests, _, answer) = F.During(() => App.Read(code, link));
            Expect.Equal(200, answer.Status, $"{name} status");
            Expect.True(!answer.Ok, $"{name} not ok");
            Expect.Equal(stopCode, answer.Code, $"{name}: {answer}");
            Expect.True(answer["offerPaste"].GetBoolean(), $"{name}: paste offered");
            Expect.True(answer.Line.StartsWith($"DỪNG ĐỌC [{stopCode}]: ", StringComparison.Ordinal), $"{name} line: {answer.Line}");
            Expect.True(answer.Outcome!.Value.GetProperty("nextStep").GetString()!.Contains("Dán chữ của trang sản phẩm", StringComparison.Ordinal), $"{name}: next step names paste mode");

            var imageUri = new Uri(image);
            var answered = imageUri.Host is not (FixtureSite.SilentHost or FixtureSite.ClosedHost);
            var expected = new[] { "shopee.vn" + new Uri(link).AbsolutePath, FixtureSite.ImageHost + "/file/mc18-own-1" }.Concat(answered ? new[] { imageUri.Host + imageUri.AbsolutePath } : Array.Empty<string>()).ToList();
            Expect.Equal(string.Join(" ", expected), string.Join(" ", requests.Select(q => q.Host + q.PathOnly)), $"{name}: the requests end at the stopping image");
            Expect.Equal(3, answer["requests"].GetInt32(), $"{name}: requests the app counted");
            var count = F.Requests.Count;
            Thread.Sleep(1000);
            Expect.Equal(count, F.Requests.Count, $"{name}: no request after the stop");

            var fill = answer["fill"];
            Expect.Equal(FixtureSite.Name, Field(fill, "productName"), $"{name}: the filled facts are kept");
            Expect.Equal("anh-doc-01.png", string.Join(",", fill.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString())), $"{name}: the image saved before the stop is kept");
            var folder = App.Workspace.ReadImageFolder(code);
            Expect.Equal("anh-doc-01.png", string.Join(",", Directory.EnumerateFiles(folder).Select(Path.GetFileName)), $"{name}: read image folder");
            Expect.True(File.ReadAllBytes(Path.Combine(folder, "anh-doc-01.png")).AsSpan().SequenceEqual(FixtureSite.OwnPng(1)), $"{name}: kept image byte-equal");
            var named = fill.GetProperty("notFetched").EnumerateArray().Select(s => (Address: s.GetProperty("address").GetString()!, Reason: s.GetProperty("reason").GetString()!)).ToList();
            Expect.Equal(2, named.Count, $"{name}: named for step 2: {string.Join(" | ", named)}");
            Expect.True(named[0].Address == image && named[0].Reason.StartsWith("lần đọc dừng ở ảnh này", StringComparison.Ordinal), $"{name}: the stopping image is named: {named[0]}");
            Expect.True(named[1].Address == FixtureSite.OwnImages[2] && named[1].Reason.StartsWith("lần đọc đã dừng trước ảnh này", StringComparison.Ordinal), $"{name}: the later image is named: {named[1]}");
            Expect.True(Web.FillStore.Load(App.Workspace, code) is { } stored && stored.Images.Count == 1, $"{name}: the fill record is saved with the kept image");
            var lines = File.ReadAllLines(log).Skip(logBefore).ToList();
            Expect.Equal(3, lines.Count, $"{name}: read-log lines");
            Expect.Equal(stopCode is "image-too-large" or "no-connection" or "no-answer" ? stopCode : "-", lines[^1].Split('\t')[5], $"{name}: the sender's code on the last log line");
            _r.Note($"{name}: {answer.Line.Trim()}; requests {string.Join(", ", requests.Select(q => q.PathOnly))}; kept anh-doc-01.png; named {named.Count}");
        }
    }

    private void PageBounds()
    {
        foreach (var (name, link, code) in FixtureSite.PageBounds)
        {
            var (requests, _, answer) = F.During(() => App.Read("V-64", link));
            Expect.True(!answer.Ok && answer.Status == 200, $"{name}: {answer}");
            Expect.Equal(code, answer.Code, $"{name}: {answer}");
            Expect.True(answer["offerPaste"].GetBoolean(), $"{name}: paste offered");
            Expect.True(answer.Outcome!.Value.GetProperty("nextStep").GetString()!.Contains("Dán chữ của trang sản phẩm", StringComparison.Ordinal), $"{name}: next step names paste mode");
            Expect.Equal("shopee.vn" + new Uri(link).AbsolutePath, string.Join(" ", requests.Select(q => q.Host + q.PathOnly)), $"{name}: one request");
            Expect.True(answer["fill"].ValueKind == JsonValueKind.Null, $"{name}: nothing filled");
            var count = F.Requests.Count;
            Thread.Sleep(1000);
            Expect.Equal(count, F.Requests.Count, $"{name}: no request after the stop");
            _r.Note($"{name}: {answer.Line.Trim()} (1 request)");
        }

        Expect.True(Web.FillStore.Load(App.Workspace, "V-64") is null, "no page-phase bound filled anything");

        // The image count bound (D-014): of 22 declared images the first 20 are fetched and the other two named, never requested.
        var (manyRequests, _, many) = F.During(() => App.Read("V-64n", FixtureSite.ManyImagesLink));
        Expect.True(many.Ok, $"22 declared images: {many}");
        Expect.Equal(21, manyRequests.Count, $"the page and 20 images: {manyRequests.Count}");
        Expect.True(!manyRequests.Any(q => q.PathOnly.EndsWith("mc18-nhieu-21", StringComparison.Ordinal) || q.PathOnly.EndsWith("mc18-nhieu-22", StringComparison.Ordinal)), "images 21 and 22 never requested");
        Expect.Equal(20, many["fill"].GetProperty("images").GetArrayLength(), "20 images saved");
        var over = many["fill"].GetProperty("notFetched").EnumerateArray().Select(s => (Address: s.GetProperty("address").GetString()!, Reason: s.GetProperty("reason").GetString()!)).ToList();
        Expect.True(over.Count == 2 && over.All(s => s.Reason.Contains($"quá {Web.PageReader.MaxImages} ảnh", StringComparison.Ordinal)), $"images 21 and 22 named: {string.Join(" | ", over)}");
        _r.Note($"22 declared images: {manyRequests.Count} requests, 20 saved, 2 named ({over[0].Reason})");

        // The structured-data depth bound (D-014): a product block nested deeper than the limit is not read, and nothing fails.
        static string Nested(int levels) => "<html><head><script type=\"application/ld+json\">" + string.Concat(Enumerable.Repeat("{\"@graph\":[", levels))
            + "{\"@type\":\"Product\",\"name\":\"Sản phẩm lồng sâu\"}" + string.Concat(Enumerable.Repeat("]}", levels)) + "</script></head><body></body></html>";
        var shallow = Web.PageReader.Read(Nested(10));
        Expect.Equal("Sản phẩm lồng sâu", shallow.Name, "a product block 10 levels deep is read");
        var depthWatch = Stopwatch.StartNew();
        var deep = Web.PageReader.Read(Nested(Web.PageReader.JsonDepth + 8));
        depthWatch.Stop();
        Expect.True(deep.Name is null && !deep.Readable, $"a product block {Web.PageReader.JsonDepth + 8} levels deep is not read: {deep.Name}");
        Expect.True(depthWatch.Elapsed.TotalSeconds < 1, $"the deep block was set aside in {depthWatch.Elapsed.TotalMilliseconds:0} ms");
        _r.Note($"depth bound {Web.PageReader.JsonDepth}: 10 levels read; {Web.PageReader.JsonDepth + 8} levels set aside in {depthWatch.Elapsed.TotalMilliseconds:0} ms");

        // The click bound: five images that each answer after 25 s (each under the 30 s request bound) pass 120 s at the fifth.
        var watch = Stopwatch.StartNew();
        var (slowRequests, _, slow) = F.During(() => App.Read("V-64t", FixtureSite.SlowLink));
        watch.Stop();
        Expect.Equal("read-time-over", slow.Code, $"slow read: {slow}");
        Expect.True(watch.Elapsed.TotalSeconds is >= 119 and <= 132, $"stopped after {watch.Elapsed.TotalSeconds:0.0} s");
        Expect.True(slow["offerPaste"].GetBoolean(), "slow read: paste offered");
        Expect.Equal(6, slowRequests.Count, $"the page and five image requests: {string.Join(", ", slowRequests)}");
        var images = slow["fill"].GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString()).ToList();
        Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png, anh-doc-04.png", string.Join(", ", images), "the four images that arrived in time are kept");
        var named = slow["fill"].GetProperty("notFetched").EnumerateArray().Select(s => s.GetProperty("address").GetString()).ToList();
        Expect.True(named.Count == 1 && named[0]!.EndsWith("mc18-cham-5", StringComparison.Ordinal), $"the fifth image is named: {string.Join(", ", named)}");
        var after = F.Requests.Count;
        Thread.Sleep(2000);
        Expect.Equal(after, F.Requests.Count, "no request after the click bound");
        _r.Note($"click bound: {slow.Line.Trim()} after {watch.Elapsed.TotalSeconds:0.0} s; {slowRequests.Count} requests; kept {images.Count} images");
    }

    private void HostEdges()
    {
        // Links the rule refuses before any connection.
        var refused = new (string Kind, string Link, string Code)[]
        {
            ("IDN look-alike (Cyrillic o)", "https://shоpee.vn" + FixtureSite.GoodPath, "link-not-allowed"),
            ("IDN look-alike as punycode", "https://xn--shpee-kye.vn" + FixtureSite.GoodPath, "link-not-allowed"),
            ("IDN look-alike with no scheme", "shоpee.vn" + FixtureSite.GoodPath, "link-not-allowed"),
            ("IPv6 literal", "https://[2001:db8::7]/binh-giu-nhiet", "link-not-allowed"),
            ("IPv6 loopback", "https://[::1]/binh-giu-nhiet", "link-not-allowed"),
            ("IPv4-mapped IPv6", "https://[::ffff:127.0.0.1]/binh-giu-nhiet", "link-not-allowed"),
            ("explicit :443", "https://shopee.vn:443" + FixtureSite.GoodPath, "link-not-allowed"),
            ("explicit :443 with no scheme", "shopee.vn:443" + FixtureSite.GoodPath, "link-not-allowed"),
            ("affiliate subdomain", "https://m.affiliate.shopee.vn/mc18", "link-affiliate"),
            ("affiliate upper-case", "https://AFFILIATE.Shopee.VN/mc18", "link-affiliate"),
            ("affiliate with trailing dot", "https://affiliate.shopee.vn./mc18", "link-affiliate"),
            ("outside Shopee, upper-case", "https://XSHOPEE.VN/binh", "link-not-allowed"),
            ("outside Shopee, trailing dot", "https://xshopee.vn./binh", "link-not-allowed"),
            ("shopee.vn on the left, upper-case", "https://SHOPEE.VN.EXAMPLE.TEST/binh", "link-not-allowed"),
            ("a /verify/ path", "https://shopee.vn/verify/captcha?anti_bot_tracking_id=mc18", "captcha"),
        };
        foreach (var (kind, link, code) in refused)
        {
            var (requests, connects, answer) = F.During(() => App.Read("V-65", link));
            Expect.Equal(code, answer.Code, $"{kind}: {answer}");
            Expect.Equal(0, requests.Count + connects.Count, $"{kind}: connections {string.Join(", ", connects)}");
            _r.Note($"{kind} \"{link}\": {answer.Code}, 0 connections");
        }

        // Links the rule reads: the host compares lower-case and without a trailing dot.
        EnsureGoodRead();
        foreach (var (kind, link) in new[] { ("upper-case host", "HTTPS://SHOPEE.VN" + FixtureSite.GoodPath), ("trailing-dot host", "https://shopee.vn." + FixtureSite.GoodPath) })
        {
            var (requests, connects, answer) = F.During(() => App.Read("V-65", link));
            Expect.True(answer.Ok, $"{kind}: {answer}");
            Expect.Equal(Field(_goodFill, "productName"), Field(answer["fill"], "productName"), $"{kind}: product name");
            Expect.Equal(6, requests.Count, $"{kind}: the page and five images");
            Expect.True(connects.All(c => c.TrimEnd('.').ToLowerInvariant() is "shopee.vn" or FixtureSite.ImageHost), $"{kind}: connections {string.Join(", ", connects)}");
            _r.Note($"{kind}: read; connections {string.Join(",", connects.Distinct())}; page address {Field(answer["fill"], "pagePath")}");
        }

        // Hops: each is checked before it is requested.
        foreach (var (kind, link, target, code) in FixtureSite.HopEdges)
        {
            var (requests, connects, answer) = F.During(() => App.Read("V-65", link));
            Expect.Equal(code, answer.Code, $"{kind}: {answer}");
            Expect.Equal("shopee.vn" + new Uri(link).AbsolutePath, string.Join(" ", requests.Select(q => q.Host + q.PathOnly)), $"{kind}: one request, to the hopping page");
            Expect.Equal("shopee.vn", string.Join(",", connects), $"{kind}: connections");
            Expect.True(answer["fill"].ValueKind == JsonValueKind.Null, $"{kind}: nothing filled");
            _r.Note($"{kind} -> {target}: {answer.Code}; 1 request, 0 to the target");
        }

        // The connect step of the shipped stack (no seam): names that resolve to this computer are refused before a socket connects.
        // Two addresses are literals, which the resolver returns without asking any server; "localhost" is a name the system resolves
        // itself (to ::1 and 127.0.0.1) with no query sent. A listener on 127.0.0.1 counts any connection that slips through.
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var accepted = 0;
        using var stopListening = new CancellationTokenSource();
        _ = Task.Run(async () =>
        {
            while (!stopListening.IsCancellationRequested)
            {
                try
                {
                    using var client = await listener.AcceptTcpClientAsync(stopListening.Token);
                    Interlocked.Increment(ref accepted);
                }
                catch (Exception)
                {
                    return;
                }
            }
        });
        var logFolder = Path.Combine(_root, "not-public");
        foreach (var host in new[] { "127.0.0.1", "127.0.0.2", "localhost" })
        {
            using var click = new CancellationTokenSource(Web.ReadSender.ClickBound);
            using var sender = new Web.ReadSender(null, new Web.ReadLog(logFolder), click.Token);
            var stop = Expect.Throws<Web.ReadStop>(() => sender.Get(new Uri($"https://{host}:{port}/binh-giu-nhiet"), image: false).GetAwaiter().GetResult(), $"{host}: refused");
            Expect.Equal("address-not-public", stop.Code, $"{host}: {stop.Reason}");
            Expect.Equal(0, sender.LocalAddresses.Count, $"{host}: no connection made");
            _r.Note($"{host}: {stop.Code} \"{stop.Reason}\"");
        }

        Thread.Sleep(300);
        stopListening.Cancel();
        Expect.Equal(0, Volatile.Read(ref accepted), "the listener received no connection");
        Expect.True(File.ReadAllLines(Path.Combine(logFolder, Web.ReadLog.FileName)).All(l => l.EndsWith("\taddress-not-public", StringComparison.Ordinal)), "read log codes");

        var notPublic = new[]
        {
            "127.0.0.1", "127.255.0.9", "10.1.2.3", "172.16.0.1", "172.31.255.255", "192.168.1.1", "169.254.1.1", "100.64.0.1", "100.127.255.255",
            "0.0.0.0", "224.0.0.1", "239.255.255.250", "255.255.255.255", "192.0.0.8", "::1", "::", "fe80::1", "fec0::1", "fc00::1", "fd12:3456::1",
            "ff02::1", "::ffff:127.0.0.1", "::ffff:10.0.0.1", "::ffff:192.168.0.1",
        };
        var isPublic = new[] { "1.1.1.1", "8.8.8.8", "172.32.0.1", "100.128.0.1", "192.0.1.1", "203.0.113.7", "2001:4860:4860::8888", "::ffff:8.8.8.8" };
        var wrong = notPublic.Where(a => Web.ReadSender.IsPublic(IPAddress.Parse(a))).Concat(isPublic.Where(a => !Web.ReadSender.IsPublic(IPAddress.Parse(a)))).ToList();
        Expect.Equal(0, wrong.Count, $"address classes wrong: {string.Join(", ", wrong)}");
        _r.Note($"address rule: {notPublic.Length} not public, {isPublic.Length} public, as expected; the listener got 0 connections");
    }

    private void GuardedRoutes()
    {
        const string code = "V-66";
        Expect.True(App.Paste(code, FixtureSite.GoodPasteText()).Ok, "a fill record to confirm against, made through the guarded route");
        var key = Web.FillStore.Load(App.Workspace, code)!.Fields[0].Member;
        Expect.True(App.Get("/api/read-switch")["readOn"].GetBoolean(), "the switch is on before the case");
        var log = Path.Combine(App.Workspace.WebFolder, Web.ReadLog.FileName);
        var logBefore = File.Exists(log) ? File.ReadAllLines(log).Length : 0;

        var routes = new (string Path, object Body)[]
        {
            ($"/api/read?code={code}r", new { link = FixtureSite.GoodLink }),
            ($"/api/paste?code={code}p", new { text = FixtureSite.GoodPasteText() }),
            ($"/api/read-confirm?code={code}", new { field = key }),
            ("/api/read-switch", new { on = false }),
        };
        var forms = new (string Name, string? Origin, string? Token, string Code)[]
        {
            ("foreign Origin", "http://shop-la.example", App.Token, "origin-not-allowed"),
            ("another port of this computer", $"http://127.0.0.1:{(App.Port == 65535 ? 1 : App.Port + 1)}", App.Token, "origin-not-allowed"),
            ("localhost in place of 127.0.0.1", $"http://localhost:{App.Port}", App.Token, "origin-not-allowed"),
            ("\"null\" Origin", "null", App.Token, "origin-not-allowed"),
            ("no Origin", null, App.Token, "origin-not-allowed"),
            ("no token", App.Origin, null, "token-not-valid"),
            ("wrong token", App.Origin, new string('0', 64), "token-not-valid"),
        };
        var refusals = 0;
        foreach (var (path, body) in routes)
        {
            foreach (var (name, origin, token, refusal) in forms)
            {
                var (requests, connects, answer) = F.During(() => App.PostAs(path, body, origin, token));
                Expect.Equal(403, answer.Status, $"{path}, {name}: {answer}");
                Expect.Equal(refusal, answer.Code, $"{path}, {name}: {answer}");
                Expect.Equal(0, requests.Count + connects.Count, $"{path}, {name}: connections");
                refusals++;
            }
        }

        Expect.True(App.Get("/api/read-switch")["readOn"].GetBoolean(), "the switch is still on");
        Expect.True(Web.FillStore.Load(App.Workspace, code + "r") is null, "no read filled anything");
        Expect.True(!Directory.Exists(App.Workspace.ReadImageFolder(code + "r")), "no read image folder");
        Expect.True(Web.FillStore.Load(App.Workspace, code + "p") is null, "no paste filled anything");
        Expect.True(Web.FillStore.Load(App.Workspace, code)!.Fields.All(f => !f.Confirmed), "no field confirmed");
        Expect.Equal(logBefore, File.Exists(log) ? File.ReadAllLines(log).Length : 0, "no read-log line");

        // The same requests with the page's own Origin and token are accepted: the refusals came from the guard, not the route.
        Expect.True(App.Post($"/api/read-confirm?code={code}", new { field = key }).Ok, "the guarded confirm is accepted");
        Expect.True(Web.FillStore.Load(App.Workspace, code)!.Fields.First(f => f.Member == key).Confirmed, "and confirms");
        _r.Note($"{refusals} refusals (4 routes x {forms.Length} forms: {string.Join(", ", forms.Select(f => $"{f.Name} -> {f.Code}"))}); 0 connections; switch on, no fill, no confirmation, no read-log line");
    }

    // -----------------------------------------------------------------------------------------------------------------------------

    private void EnsureGoodRead()
    {
        if (_goodFill.ValueKind != JsonValueKind.Object)
        {
            var answer = App.Read("V-51", FixtureSite.GoodLink);
            Expect.True(answer.Ok, $"good read: {answer}");
            _goodFill = answer["fill"].Clone();
        }
    }

    private void UploadScreenshot(string code) =>
        Expect.True(App.PostBytes($"/api/screenshot?code={code}&name=chup-1.png", File.ReadAllBytes(Path.Combine(Stills, "chup-1.png"))).Ok, $"{code} screenshot");

    private void ConfirmAll(string code, JsonElement fill)
    {
        foreach (var key in fill.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("member").GetString()!)
            .Concat(fill.GetProperty("candidates").EnumerateArray().Select(c => c.GetProperty("key").GetString()!)))
        {
            Expect.True(App.Post($"/api/read-confirm?code={code}", new { field = key }).Ok, $"{code} confirm {key}");
        }
    }

    private static string? Field(JsonElement fill, string member) =>
        fill.GetProperty("fields").EnumerateArray().Where(f => f.GetProperty("member").GetString() == member).Select(f => f.GetProperty("value").GetString()).FirstOrDefault();

    /// <summary>The sheet the operator saves after a fill: the filled values unchanged, the operator's own fields typed, up to three rows.</summary>
    internal static JsonObject SheetFrom(string code, JsonElement fill, IReadOnlyList<string> images, string subject, int rows = 3)
    {
        var sheet = Fixtures.Sheet(code);
        sheet["subjectNoun"] = subject;
        foreach (var field in fill.GetProperty("fields").EnumerateArray())
        {
            sheet[field.GetProperty("member").GetString()!] = field.GetProperty("value").GetString();
        }

        var facts = new JsonArray();
        foreach (var candidate in fill.GetProperty("candidates").EnumerateArray().Take(rows))
        {
            facts.Add(new JsonObject
            {
                ["label"] = $"F{facts.Count + 1}",
                ["subjectNoun"] = subject,
                ["attribute"] = candidate.GetProperty("attribute").GetString(),
                ["value"] = candidate.GetProperty("value").GetString(),
                ["sourceSection"] = candidate.GetProperty("sourceSection").GetString(),
                ["screenshot"] = "chup-1.png",
            });
        }

        sheet["facts"] = facts;
        sheet["imagesUsed"] = new JsonArray([.. images.Select(i => (JsonNode)JsonValue.Create(i))]);
        return sheet;
    }

    public void Dispose()
    {
        _app?.Dispose();
        _fixture?.Dispose();
    }
}
