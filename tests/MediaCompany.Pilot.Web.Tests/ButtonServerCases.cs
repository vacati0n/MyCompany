using System.Globalization;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using MediaCompany.Pilot.Tests;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>
/// THE MC-19 BUTTON CASES ON THE APP SIDE: the button builder, the block reader on its own, and the block path of /api/paste in the app
/// started in this process with the MC-18 read fixture's seam (images only; the page is never requested) and, for the shipped-address
/// case, the shipped executable. Every block here is built by the test; the browser cases press the real button.
/// </summary>
internal sealed partial class ButtonCases
{
    private static readonly JsonSerializerOptions BlockJson = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    private ReadFixture? _fixture;
    private ReadApp? _app;
    private string? _stills;

    private ReadFixture F => _fixture ??= new ReadFixture();

    /// <summary>The app in this process with the read fixture's seam and the page server's exact origin, the switch on.</summary>
    private ReadApp App
    {
        get
        {
            if (_app is null)
            {
                _app = ReadApp.Start(Path.Combine(_root, "main"), F, Path.Combine(_root, "main", "pilot"), fixtureOrigin: Pages.Origin);
                Expect.True(_app.Switch(true).Ok, "read switch turned on");
            }

            return _app;
        }
    }

    private string Stills => _stills ??= Fixtures.Stills(Path.Combine(_root, "stills"));

    private static string Today => DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>A product page address on shopee.vn, distinct per case so no two saved sheets name the same product.</summary>
    private static string Shopee(int n) => $"https://shopee.vn/binh-giu-nhiet-mau-i.100.{n}";

    /// <summary>The block the button copies from the fixture product page, with the given page address and image addresses.</summary>
    internal static JsonObject GoodBlock(string page, IEnumerable<string>? images = null) => new()
    {
        ["formVersion"] = 1,
        ["buttonMark"] = Web.ButtonBuilder.Mark,
        ["pageAddress"] = page,
        ["pressedAt"] = Today,
        ["productName"] = FixtureSite.Name,
        ["classification"] = FixtureSite.Classification,
        ["shopName"] = ButtonSite.ShopName,
        ["mall"] = true,
        ["specs"] = Rows(ButtonSite.ExpectedRows),
        ["price"] = ButtonSite.PagePrice,
        ["description"] = FixtureSite.Description,
        ["images"] = new JsonArray([.. (images ?? FixtureSite.OwnImages).Select(i => (JsonNode)JsonValue.Create(i)!)]),
    };

    private static JsonArray Rows(IEnumerable<(string Attribute, string Value)> rows) =>
        new([.. rows.Select(r => (JsonNode)new JsonObject { ["attribute"] = r.Attribute, ["value"] = r.Value, ["section"] = "Chi tiết sản phẩm: " + r.Attribute })]);

    internal static string Text(JsonNode block) => Web.BlockReader.Marker + "\n" + block.ToJsonString(BlockJson);

    private static JsonObject With(JsonObject block, Action<JsonObject> change)
    {
        var copy = block.DeepClone().AsObject();
        change(copy);
        return copy;
    }

    /// <summary>The reader's answer for one text: "accepted", or the refusal code after checking the refusal says nothing was filled.</summary>
    private static string Verdict(string text, string? fixtureOrigin = null)
    {
        try
        {
            Web.BlockReader.Read(text, fixtureOrigin);
            return "accepted";
        }
        catch (Web.WebRefusal refusal)
        {
            Expect.True(refusal.Message.Contains("Ứng dụng không điền ô nào.", StringComparison.Ordinal) && refusal.Message.Contains("Việc cần làm:", StringComparison.Ordinal),
                $"{refusal.Code}: the refusal says nothing was filled and names the next step: {refusal.Message}");
            Expect.Equal(400, refusal.Status, $"{refusal.Code}: status");
            return refusal.Code;
        }
    }

    private static string? Field(JsonElement fill, string member) =>
        fill.GetProperty("fields").EnumerateArray().Where(f => f.GetProperty("member").GetString() == member).Select(f => f.GetProperty("value").GetString()).FirstOrDefault();

    private void AddServerCases()
    {
        _r.Add("T-068", "the button builder: the mark is the first 12 hex digits of SHA-256 of the shipped script; the bookmark address is javascript: and at most 32 KiB; the shipped button carries no fixture origin; the script calls no request, navigation, click, submit, cookie or storage interface", Builder);
        _r.Add("T-069", "the block reader on its own: the 256 KiB size bound at the byte; marker line; JSON depth, root, trailing content and repeated members; version and button mark; unknown members; types; 30 images, 100 rows, 4000 characters; product name; 14 page addresses; the check order", Reader);
        _r.Add("T-070", "the block path's order (D-010): two products, then the switch off (naming Cài đặt), then no open video, each refused before the block is read, with no fill and no connection", RouteOrder);
        _r.Add("T-071", "a good block through /api/paste fills name, classification, page address, shop and Mall, and the press date, with the five rows and the five own images and no page request; non-Mall, no shop, the date rules, a block with no images; ten refusals leave the earlier fill untouched", RouteFill);
        _r.Add("T-072", "image addresses in a block (SC-14): of seven addresses two own images are fetched and five (http, a foreign host, a port, an IP address, the page's own path) get no request; a redirect and a non-image end the image phase with the facts kept", RouteImages);
        _r.Add("T-073", "the save after a button fill: an empty operator field refused by name; no confirmation and N-1 of N refused with the 'nút đọc trang' wording, N accepted; picked images placed byte-equal; price and description never saved", Save);
        _r.Add("T-074", "eight banned-term values, classes (a) to (g) and a money amount, carried in a block's rows: the save gives the typed save's code and line, 8 of 8", Banned);
        _r.Add("T-075", "the request guard on /api/paste with a block (SC-26): a foreign, other-port, localhost, \"null\" or missing Origin, a missing, wrong or earlier token, and a foreign Host are refused with 403; nothing filled, no connection", Guard);
        _r.Add("T-076", "the shipped executable: /api/button carries no fixture origin and the same mark; a block whose page is on 127.0.0.1 is refused; a shopee.vn block with no images fills with no outbound connection", Shipped);
    }

    // -----------------------------------------------------------------------------------------------------------------------------

    private void Builder()
    {
        var source = Web.ButtonBuilder.Source;
        using (var stream = typeof(Web.ButtonBuilder).Assembly.GetManifestResourceStream(Web.ButtonBuilder.ResourceName))
        {
            Expect.True(stream is not null, "the script is embedded");
        }

        var expected = Convert.ToHexStringLower(SHA256.HashData(source))[..12];
        Expect.Equal(expected, Web.ButtonBuilder.Mark, "mark = SHA-256 of the embedded script, first 12 hex digits");
        var shipped = Web.ButtonBuilder.Build(null);
        Expect.True(shipped.Address.StartsWith("javascript:", StringComparison.Ordinal), "bookmark address starts with javascript:");
        Expect.True(shipped.Bytes <= Web.ButtonBuilder.MaxAddressBytes && Encoding.UTF8.GetByteCount(shipped.Address) == shipped.Bytes, $"address {shipped.Bytes} bytes, at most {Web.ButtonBuilder.MaxAddressBytes}");
        Expect.Equal(1, shipped.FormVersion, "format version");
        Expect.Equal(expected, shipped.Mark, "view mark");
        var body = Pressed(shipped.Address);
        Expect.True(body.Contains("\"origins\":[]", StringComparison.Ordinal) && !body.Contains("127.0.0.1", StringComparison.Ordinal), "the shipped button carries no fixture origin");
        Expect.True(body.Contains($"\"mark\":\"{expected}\"", StringComparison.Ordinal) && !body.Contains(Web.ButtonBuilder.Placeholder, StringComparison.Ordinal), "the configuration replaces the placeholder");
        Expect.True(!body.Split('\n').Any(l => l.TrimStart().StartsWith("//", StringComparison.Ordinal)), "comment lines dropped");
        var test = Pressed(Web.ButtonBuilder.Build(Pages.Origin).Address);
        Expect.True(test.Contains($"\"origins\":[\"{Pages.Origin}\"]", StringComparison.Ordinal), "the test button carries exactly the fixture origin");

        var banned = new[] { "fetch(", "XMLHttpRequest", "sendBeacon", "WebSocket", "EventSource", "window.open", "location.assign", "location.replace", "location.reload", "location.href =", "location.href=",
            ".submit(", ".click(", "document.cookie", "localStorage", "sessionStorage", "indexedDB", "eval(", "Function(", "innerHTML", "outerHTML", "insertAdjacentHTML", "document.write", "import(", "postMessage", "new Image" };
        var found = banned.Where(b => body.Contains(b, StringComparison.Ordinal)).ToList();
        Expect.Equal(0, found.Count, $"interfaces the button must not call: {string.Join(", ", found)}");
        _r.Note($"mark {expected}; shipped address {shipped.Bytes} bytes of {Web.ButtonBuilder.MaxAddressBytes}; script {source.Length} bytes; {banned.Length} interfaces checked absent ({string.Join(" ", banned)})");
    }

    private void Reader()
    {
        var good = GoodBlock(Shopee(1069) + "?sp_atk=mau&xptdk=1#danh-gia");
        var block = Web.BlockReader.Read(Text(good), null);
        Expect.Equal(Shopee(1069), block.Page.ToString(), "query and fragment dropped from the page address");
        Expect.Equal(5, block.Specs.Count, "five rows");
        Expect.Equal("accepted", Verdict(Web.BlockReader.Marker + "\r\n" + good.ToJsonString(BlockJson)), "marker then CRLF (the Windows clipboard)");
        Expect.Equal("accepted", Verdict("  \r\n" + Text(good)), "leading white space");

        string Padded(int bytes)
        {
            var text = Text(good);
            return text + new string(' ', bytes - Encoding.UTF8.GetByteCount(text));
        }

        var cases = new List<(string Name, string Text, string? Origin, string Code)>
        {
            ("255 KiB", Padded(255 * 1024), null, "accepted"),
            ("exactly 256 KiB", Padded(Web.BlockReader.MaxBytes), null, "accepted"),
            ("256 KiB and one byte", Padded(Web.BlockReader.MaxBytes + 1), null, "block-too-large"),
            ("257 KiB", Padded(257 * 1024), null, "block-too-large"),
            ("257 KiB of broken JSON", Web.BlockReader.Marker + "\n{" + new string('x', 257 * 1024), null, "block-too-large"),
            ("marker with no line break", Web.BlockReader.Marker + good.ToJsonString(BlockJson), null, "block-unreadable"),
            ("marker and nothing", Web.BlockReader.Marker + "\n", null, "block-unreadable"),
            ("broken JSON", Web.BlockReader.Marker + "\n{\"formVersion\":1,", null, "block-unreadable"),
            ("an array, not an object", Web.BlockReader.Marker + "\n[" + good.ToJsonString(BlockJson) + "]", null, "block-unreadable"),
            ("trailing text", Text(good) + " thêm chữ", null, "block-unreadable"),
            ("two objects", Text(good) + good.ToJsonString(BlockJson), null, "block-unreadable"),
            ("repeated member", Text(good)[..^1] + ",\"productName\":\"Tên khác\"}", null, "block-unreadable"),
            ("repeated member in a row", Text(With(good, b => b["specs"] = new JsonArray())).Replace("\"specs\":[]", "\"specs\":[{\"attribute\":\"a\",\"value\":\"b\",\"section\":\"c\",\"value\":\"d\"}]", StringComparison.Ordinal), null, "block-unreadable"),
            ("depth over 8", Text(good)[..^1] + ",\"x\":[[[[[[[[[1]]]]]]]]]}", null, "block-unreadable"),
            ("no format version", Text(With(good, b => b.Remove("formVersion"))), null, "block-wrong-type"),
            ("format version as text", Text(With(good, b => b["formVersion"] = "1")), null, "block-wrong-type"),
            ("format version 0", Text(With(good, b => b["formVersion"] = 0)), null, "block-old-button"),
            ("format version 2", Text(With(good, b => b["formVersion"] = 2)), null, "block-unknown-version"),
            ("another button mark", Text(With(good, b => b["buttonMark"] = "000000000000")), null, "block-old-button"),
            ("no button mark", Text(With(good, b => b.Remove("buttonMark"))), null, "block-old-button"),
            ("unknown member", Text(With(good, b => b["cookie"] = "SPC_U=1")), null, "block-unknown-member"),
            ("unknown row member", Text(With(good, b => b["specs"]![0]!["link"] = "https://shopee.vn/")), null, "block-unknown-member"),
            ("mall as text", Text(With(good, b => b["mall"] = "true")), null, "block-wrong-type"),
            ("images not a list", Text(With(good, b => b["images"] = FixtureSite.OwnImages[0])), null, "block-wrong-type"),
            ("an image as a number", Text(With(good, b => b["images"] = new JsonArray(1))), null, "block-wrong-type"),
            ("a row as text", Text(With(good, b => b["specs"] = new JsonArray("Chất liệu: Thép"))), null, "block-wrong-type"),
            ("a row with no value", Text(With(good, b => b["specs"]![0]!.AsObject().Remove("value"))), null, "block-wrong-type"),
            ("name as a number", Text(With(good, b => b["productName"] = 5)), null, "block-wrong-type"),
            ("no page address", Text(With(good, b => b.Remove("pageAddress"))), null, "block-wrong-type"),
            ("no press date", Text(With(good, b => b.Remove("pressedAt"))), null, "block-wrong-type"),
            ("30 images", Text(With(good, b => b["images"] = new JsonArray([.. Enumerable.Range(1, 30).Select(i => (JsonNode)JsonValue.Create($"https://{FixtureSite.ImageHost}/file/a-{i}")!)]))), null, "accepted"),
            ("31 images", Text(With(good, b => b["images"] = new JsonArray([.. Enumerable.Range(1, 31).Select(i => (JsonNode)JsonValue.Create($"https://{FixtureSite.ImageHost}/file/a-{i}")!)]))), null, "block-too-many-images"),
            ("100 rows", Text(With(good, b => b["specs"] = Rows(Enumerable.Range(1, 100).Select(i => ($"Mục {i}", $"Giá trị {i}"))))), null, "accepted"),
            ("101 rows", Text(With(good, b => b["specs"] = Rows(Enumerable.Range(1, 101).Select(i => ($"Mục {i}", $"Giá trị {i}"))))), null, "block-too-many-rows"),
            ("description of 4000 characters", Text(With(good, b => b["description"] = new string('ạ', 4000))), null, "accepted"),
            ("description of 4001 characters", Text(With(good, b => b["description"] = new string('ạ', 4001))), null, "block-text-too-long"),
            ("row value of 4001 characters", Text(With(good, b => b["specs"]![0]!["value"] = new string('a', 4001))), null, "block-text-too-long"),
            ("no product name", Text(With(good, b => b.Remove("productName"))), null, "block-no-name"),
            ("blank product name", Text(With(good, b => b["productName"] = "   ")), null, "block-no-name"),
        };

        var pages = new (string Address, string? Origin, string Code)[]
        {
            ("https://shopee.vn/binh-i.1.2", null, "accepted"),
            ("https://banhang.shopee.vn/binh-i.1.2", null, "accepted"),
            ($"{Pages.Origin}{ButtonSite.ProductPath}", null, "block-page-not-allowed"),
            ($"{Pages.Origin}{ButtonSite.ProductPath}", Pages.Origin, "accepted"),
            ($"http://127.0.0.1:{(new Uri(Pages.Origin).Port == 65535 ? 1 : new Uri(Pages.Origin).Port + 1)}{ButtonSite.ProductPath}", Pages.Origin, "block-page-not-allowed"),
            ("http://shopee.vn/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://affiliate.shopee.vn/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://shopee.vn:8443/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://shopee.vn:443/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://203.0.113.7/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://user@shopee.vn/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://shopee.vn.example.test/binh-i.1.2", null, "block-page-not-allowed"),
            ("https://xshopee.vn/binh-i.1.2", null, "block-page-not-allowed"),
            ("javascript:alert(1)", null, "block-page-not-allowed"),
        };
        foreach (var (address, origin, code) in pages)
        {
            cases.Add(($"page {address}{(origin is null ? string.Empty : " (fixture origin " + origin + ")")}", Text(With(good, b => b["pageAddress"] = address)), origin, code));
        }

        // The check order: each pair fails two checks, and the earlier one answers.
        cases.Add(("order: too large and broken", Web.BlockReader.Marker + "{" + new string('x', 257 * 1024), null, "block-too-large"));
        cases.Add(("order: old version and an unknown member", Text(With(good, b => { b["formVersion"] = 0; b["x"] = 1; })), null, "block-old-button"));
        cases.Add(("order: unknown member and a wrong type", Text(With(good, b => { b["x"] = 1; b["mall"] = "co"; })), null, "block-unknown-member"));
        cases.Add(("order: wrong type and 31 images", Text(With(good, b => { b["mall"] = "co"; b["images"] = new JsonArray([.. Enumerable.Range(1, 31).Select(i => (JsonNode)JsonValue.Create("a")!)]); })), null, "block-wrong-type"));
        cases.Add(("order: 101 rows and no name", Text(With(good, b => { b["specs"] = Rows(Enumerable.Range(1, 101).Select(i => ("a", "b"))); b.Remove("productName"); })), null, "block-too-many-rows"));
        cases.Add(("order: no name and a foreign page", Text(With(good, b => { b.Remove("productName"); b["pageAddress"] = "https://example.com/x"; })), null, "block-no-name"));

        var failed = new List<string>();
        foreach (var (name, text, origin, code) in cases)
        {
            var got = Verdict(text, origin);
            if (got != code)
            {
                failed.Add($"{name}: expected {code}, got {got}");
            }

            _r.Note($"{name}: {got}");
        }

        Expect.Equal(0, failed.Count, string.Join("; ", failed));
        _r.Note($"{cases.Count} reader cases, all as expected");
    }

    private void RouteOrder()
    {
        const string code = "V-70";
        var good = Text(GoodBlock(Shopee(1070)));
        var two = good + "\n" + good;
        var broken = Web.BlockReader.Marker + "\n{";
        var cases = new (string Name, bool SwitchOn, bool Video, string Text, int Status, string Code)[]
        {
            ("two blocks", true, true, two, 400, "block-two-products"),
            ("two blocks, switch off, no video", false, false, two, 400, "block-two-products"),
            ("switch off", false, true, good, 409, "block-read-off"),
            ("switch off, no video, broken block", false, false, broken, 409, "block-read-off"),
            ("no video", true, false, good, 409, "block-no-video"),
            ("no video, broken block", true, false, broken, 409, "block-no-video"),
            ("broken block with a video", true, true, broken, 400, "block-unreadable"),
        };
        foreach (var (name, on, video, text, status, refusal) in cases)
        {
            Expect.True(App.Switch(on).Ok, $"{name}: switch {(on ? "on" : "off")}");
            var (requests, connects, answer) = F.During(() => video ? App.Paste(code, text) : App.Post("/api/paste", new { text }));
            Expect.Equal(refusal, answer.Code, $"{name}: {answer}");
            Expect.Equal(status, answer.Status, $"{name}: status");
            Expect.Equal(0, requests.Count + connects.Count, $"{name}: connections");
            Expect.True(answer.Line.Contains("Ứng dụng không điền ô nào", StringComparison.Ordinal), $"{name}: says nothing was filled: {answer.Line}");
            if (refusal == "block-read-off")
            {
                Expect.True(answer.Line.Contains("Cài đặt", StringComparison.Ordinal), $"{name}: names Cài đặt: {answer.Line}");
            }

            _r.Note($"{name}: {answer.Status} {answer.Code} \"{Direct.FirstLine(answer.Line)}\"");
        }

        Expect.True(App.Switch(true).Ok, "switch back on");
        Expect.True(Web.FillStore.Load(App.Workspace, code) is null && !Directory.Exists(App.Workspace.ReadImageFolder(code)), "nothing filled, no read image folder");
    }

    private void RouteFill()
    {
        const string code = "V-71";
        var page = Shopee(1071);
        var (requests, connects, answer) = F.During(() => App.Paste(code, Text(GoodBlock(page + "?sp_atk=mau"))));
        Expect.True(answer.Ok, $"good block: {answer}");
        var fill = answer["fill"];
        Expect.Equal("button", fill.GetProperty("source").GetString(), "source");
        Expect.Equal(FixtureSite.Name, Field(fill, "productName"), "product name");
        Expect.Equal(FixtureSite.Classification, Field(fill, "classification"), "classification");
        Expect.Equal(page, Field(fill, "pagePath"), "page address without the query");
        Expect.Equal($"{ButtonSite.ShopName}, Mall", Field(fill, "shopNameAndMall"), "shop and Mall");
        Expect.Equal(Today, Field(fill, "readDate"), "press date");
        var candidates = fill.GetProperty("candidates").EnumerateArray().Select(c => $"{c.GetProperty("attribute").GetString()}: {c.GetProperty("value").GetString()}").ToList();
        Expect.Equal(string.Join(" | ", ButtonSite.ExpectedRows.Select(r => $"{r.Attribute}: {r.Value}")), string.Join(" | ", candidates), "five rows in page order");
        Expect.Equal(ButtonSite.PagePrice, fill.GetProperty("price").GetString(), "price kept for reference");
        Expect.Equal(FixtureSite.Description, fill.GetProperty("description").GetString(), "description kept for reference");
        var images = fill.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString()!).ToList();
        Expect.Equal("anh-doc-01.png, anh-doc-02.png, anh-doc-03.png, anh-doc-04.png, anh-doc-05.png", string.Join(", ", images), "five own images");
        for (var i = 1; i <= 5; i++)
        {
            var saved = File.ReadAllBytes(Path.Combine(App.Workspace.ReadImageFolder(code), $"anh-doc-{i:00}.png"));
            Expect.True(saved.AsSpan().SequenceEqual(FixtureSite.OwnPng(i)), $"anh-doc-{i:00}.png byte-equal to own image {i}");
        }

        var paths = requests.Select(q => q.Host + q.PathOnly).ToList();
        Expect.Equal(string.Join(" ", FixtureSite.OwnImages.Select(i => new Uri(i).Host + new Uri(i).AbsolutePath)), string.Join(" ", paths), "exactly the five own images; the page is never requested");
        Expect.True(connects.All(c => c == FixtureSite.ImageHost), $"connections only to the image host: {string.Join(",", connects)}");
        Expect.Equal(5, answer["requests"].GetInt32(), "requests the app counted");
        Expect.True(requests.All(q => q.Method == "GET" && !q.Has("Cookie") && !q.Has("Referer")), "GET with no cookie or referrer");
        _r.Note($"good block: {string.Join("; ", fill.GetProperty("fields").EnumerateArray().Select(f => $"{f.GetProperty("member").GetString()}={f.GetProperty("value").GetString()}"))}; requests {string.Join(", ", requests)}");
        _r.Note($"message: {answer["message"].GetString()}");

        var variants = new (string Name, JsonObject Block, Func<JsonElement, bool> Check, string Expect)[]
        {
            ("not Mall", With(GoodBlock(Shopee(1072), []), b => { b["shopName"] = ButtonSite.PlainShopName; b["mall"] = false; }), f => Field(f, "shopNameAndMall") == $"{ButtonSite.PlainShopName}, không phải Mall", "shop, không phải Mall"),
            ("no shop found", With(GoodBlock(Shopee(1073), []), b => { b.Remove("shopName"); b.Remove("mall"); }), f => Field(f, "shopNameAndMall") is null, "no shop field"),
            ("no classification", With(GoodBlock(Shopee(1074), []), b => b.Remove("classification")), f => Field(f, "classification") is null, "no classification field"),
            ("press date tomorrow", With(GoodBlock(Shopee(1075), []), b => b["pressedAt"] = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)), f => Field(f, "readDate") is null, "no read date"),
            ("press date not yyyy-MM-dd", With(GoodBlock(Shopee(1076), []), b => b["pressedAt"] = "30/10/2026"), f => Field(f, "readDate") is null, "no read date"),
            ("press date earlier", With(GoodBlock(Shopee(1077), []), b => b["pressedAt"] = "2026-10-01"), f => Field(f, "readDate") == "2026-10-01", "2026-10-01"),
            ("no images member", With(GoodBlock(Shopee(1078)), b => b.Remove("images")), f => f.GetProperty("images").GetArrayLength() == 0 && f.GetProperty("notFetched").GetArrayLength() == 0, "no image"),
        };
        foreach (var (name, block, check, expect) in variants)
        {
            var got = F.During(() => App.Paste(code + "b", Text(block)));
            Expect.True(got.Result.Ok, $"{name}: {got.Result}");
            Expect.Equal(0, got.Requests.Count + got.Connects.Count, $"{name}: no connection");
            Expect.True(check(got.Result["fill"]), $"{name}: expected {expect}: {got.Result["fill"].GetRawText()}");
            _r.Note($"{name}: {expect}; 0 requests");
        }

        // Ten refusals through the route: each leaves the fill record of the good block byte-for-byte as it was, with no connection.
        var good = GoodBlock(Shopee(1079));
        var before = File.ReadAllBytes(App.Workspace.FillPath(code));
        var imagesBefore = Directory.EnumerateFiles(App.Workspace.ReadImageFolder(code)).Count();
        var refusals = new (string Name, string Text, string Code)[]
        {
            ("over 256 KiB", Text(good) + new string(' ', Web.BlockReader.MaxBytes), "block-too-large"),
            ("unreadable", Web.BlockReader.Marker + "\n{\"formVersion\":", "block-unreadable"),
            ("old button", Text(With(good, b => b["buttonMark"] = "0123456789ab")), "block-old-button"),
            ("format version 2", Text(With(good, b => b["formVersion"] = 2)), "block-unknown-version"),
            ("unknown member", Text(With(good, b => b["account"] = ButtonSite.Account)), "block-unknown-member"),
            ("wrong type", Text(With(good, b => b["specs"] = "Chất liệu: Thép")), "block-wrong-type"),
            ("31 images", Text(With(good, b => b["images"] = new JsonArray([.. Enumerable.Range(1, 31).Select(i => (JsonNode)JsonValue.Create(FixtureSite.OwnImages[0])!)]))), "block-too-many-images"),
            ("101 rows", Text(With(good, b => b["specs"] = Rows(Enumerable.Range(1, 101).Select(i => ($"Mục {i}", "x"))))), "block-too-many-rows"),
            ("text over 4000", Text(With(good, b => b["classification"] = new string('x', 4001))), "block-text-too-long"),
            ("no name", Text(With(good, b => b.Remove("productName"))), "block-no-name"),
            ("page on 127.0.0.1 off the fixture origin", Text(With(good, b => b["pageAddress"] = "http://127.0.0.1:1/binh-i.1.2")), "block-page-not-allowed"),
        };
        foreach (var (name, text, refusal) in refusals)
        {
            var got = F.During(() => App.Paste(code, text));
            Expect.Equal(refusal, got.Result.Code, $"{name}: {got.Result}");
            Expect.Equal(400, got.Result.Status, $"{name}: status");
            Expect.Equal(0, got.Requests.Count + got.Connects.Count, $"{name}: no connection");
            Expect.True(File.ReadAllBytes(App.Workspace.FillPath(code)).AsSpan().SequenceEqual(before), $"{name}: the fill record is unchanged");
            Expect.Equal(imagesBefore, Directory.EnumerateFiles(App.Workspace.ReadImageFolder(code)).Count(), $"{name}: the read images are unchanged");
            _r.Note($"{name}: {got.Result.Status} {got.Result.Code}");
        }
    }

    private void RouteImages()
    {
        const string code = "V-72";
        var page = Shopee(1080);
        var seven = new[]
        {
            FixtureSite.OwnImages[0],
            FixtureSite.OwnImages[1],
            $"http://{FixtureSite.ImageHost}/file/mc18-own-3",
            $"https://{FixtureSite.ForeignImageHost}/coc-1.png",
            $"https://{FixtureSite.ImageHost}:8443/file/mc18-own-4",
            "https://203.0.113.7/file/mc18-own-5",
            page,
        };
        var (requests, connects, answer) = F.During(() => App.Paste(code, Text(GoodBlock(page, seven))));
        Expect.True(answer.Ok, $"seven addresses: {answer}");
        var fill = answer["fill"];
        Expect.Equal("anh-doc-01.png, anh-doc-02.png", string.Join(", ", fill.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString())), "two fetched");
        var skipped = fill.GetProperty("notFetched").EnumerateArray().Select(s => (Address: s.GetProperty("address").GetString()!, Reason: s.GetProperty("reason").GetString()!)).ToList();
        Expect.Equal(string.Join(" ", seven.Skip(2)), string.Join(" ", skipped.Select(s => s.Address)), "five named");
        Expect.Equal(2, requests.Count, $"two requests: {string.Join(", ", requests)}");
        Expect.True(connects.All(c => c == FixtureSite.ImageHost), $"connections: {string.Join(",", connects)}");
        Expect.True(skipped[^1].Reason.Contains("trùng địa chỉ trang", StringComparison.Ordinal), $"the page's own address is refused as such: {skipped[^1].Reason}");
        foreach (var s in skipped)
        {
            _r.Note($"not fetched {s.Address}: {s.Reason}");
        }

        foreach (var (image, stopCode) in new[] { ($"https://{FixtureSite.ImageHost}/file/mc18-dung-chuyen", "image-redirect"), ($"https://{FixtureSite.ImageHost}/file/mc18-dung-khong-anh", "not-image") })
        {
            var addresses = new[] { FixtureSite.OwnImages[0], image, FixtureSite.OwnImages[2] };
            var got = F.During(() => App.Paste(code, Text(GoodBlock(page, addresses))));
            Expect.True(!got.Result.Ok && got.Result.Status == 200, $"{stopCode}: {got.Result}");
            Expect.Equal(stopCode, got.Result.Code, $"{stopCode}: code");
            var stopped = got.Result["fill"];
            Expect.Equal(FixtureSite.Name, Field(stopped, "productName"), $"{stopCode}: facts kept");
            Expect.Equal(5, stopped.GetProperty("candidates").GetArrayLength(), $"{stopCode}: rows kept");
            Expect.Equal("anh-doc-01.png", string.Join(",", stopped.GetProperty("images").EnumerateArray().Select(i => i.GetProperty("name").GetString())), $"{stopCode}: the earlier image kept");
            Expect.Equal(string.Join(" ", addresses.Skip(1)), string.Join(" ", stopped.GetProperty("notFetched").EnumerateArray().Select(s => s.GetProperty("address").GetString())), $"{stopCode}: the rest named");
            Expect.Equal(2, got.Requests.Count, $"{stopCode}: requests {string.Join(", ", got.Requests)}");
            Expect.True(got.Result.Line.Contains("nút đọc trang", StringComparison.Ordinal), $"{stopCode}: line {got.Result.Line}");
            var after = F.Requests.Count;
            Thread.Sleep(1000);
            Expect.Equal(after, F.Requests.Count, $"{stopCode}: no request after the stop");
            Expect.True(Web.FillStore.Load(App.Workspace, code)!.Source == "button", $"{stopCode}: the fill is saved");
            _r.Note($"{stopCode}: \"{Direct.FirstLine(got.Result.Line)}\"; requests {string.Join(", ", got.Requests)}");
        }
    }

    private void Save()
    {
        const string code = "V-73";
        var answer = App.Paste(code, Text(GoodBlock(Shopee(1081))));
        Expect.True(answer.Ok, $"block: {answer}");
        var fill = answer["fill"];
        Expect.True(App.PostBytes($"/api/screenshot?code={code}&name=chup-1.png", File.ReadAllBytes(Path.Combine(Stills, "chup-1.png"))).Ok, "screenshot");
        var picked = new[] { "anh-doc-02.png", "anh-doc-04.png", "anh-doc-05.png" };
        var sheet = ReadCases.SheetFrom(code, fill, picked, "bình giữ nhiệt");

        var empty = sheet.DeepClone().AsObject();
        empty["subjectNoun"] = "";
        var refusedEmpty = App.Post($"/api/facts?code={code}", empty);
        Expect.True(!refusedEmpty.Ok && refusedEmpty.Body.GetRawText().Contains("Chủ thể", StringComparison.Ordinal), $"empty operator field named: {refusedEmpty}");

        var keys = fill.GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("member").GetString()!)
            .Concat(fill.GetProperty("candidates").EnumerateArray().Take(3).Select(c => c.GetProperty("key").GetString()!)).ToList();
        Expect.Equal(8, keys.Count, "N = five filled fields and three rows");
        var none = App.Post($"/api/facts?code={code}", sheet);
        Expect.Equal("fill-unconfirmed", none.Code, $"no confirmation: {none}");
        Expect.True(none.Line.Contains("nút đọc trang", StringComparison.Ordinal), $"the refusal names the button: {none.Line}");
        foreach (var key in keys.Take(keys.Count - 1))
        {
            Expect.True(App.Post($"/api/read-confirm?code={code}", new { field = key }).Ok, $"confirm {key}");
        }

        var almost = App.Post($"/api/facts?code={code}", sheet);
        Expect.Equal("fill-unconfirmed", almost.Code, $"N-1: {almost}");
        Expect.True(App.Post($"/api/read-confirm?code={code}", new { field = keys[^1] }).Ok, "confirm the last");
        var accepted = App.Post($"/api/facts?code={code}", sheet);
        Expect.True(accepted.Ok, $"N: {accepted}");

        var input = App.Workspace.InputFolder(code);
        foreach (var name in picked)
        {
            Expect.True(File.ReadAllBytes(Path.Combine(input, name)).AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(App.Workspace.ReadImageFolder(code), name))), $"{name} placed byte-equal");
        }

        Expect.Equal(string.Join(", ", picked), string.Join(", ", Directory.EnumerateFiles(input, "anh-doc-*").Select(Path.GetFileName).Order(StringComparer.Ordinal)), "only the picked images placed");
        var saved = File.ReadAllText(App.Workspace.FactSheetPath(code));
        Expect.True(!saved.Contains("189.000", StringComparison.Ordinal) && !saved.Contains("thép không gỉ, nắp vặn", StringComparison.OrdinalIgnoreCase), "price and description not saved");
        var status = App.Get($"/api/video?code={code}");
        Expect.True(status["images"].EnumerateArray().All(i => i.GetProperty("present").GetBoolean()), $"step 2 none missing: {status["images"].GetRawText()}");
        _r.Note($"empty field: {refusedEmpty.Code} \"{Direct.FirstLine(refusedEmpty.Line)}\"");
        _r.Note($"N = {keys.Count} ({string.Join(", ", keys)}); none: \"{Direct.FirstLine(none.Line)}\"; N-1: {almost.Code} \"{Direct.FirstLine(almost.Line)}\"; N: accepted");
    }

    private void Banned()
    {
        var equal = 0;
        var n = 1100;
        foreach (var (id, _, value) in FixtureSite.BannedPages)
        {
            var code = $"V-74{id}";
            n++;
            Expect.True(App.PostBytes($"/api/screenshot?code={code}&name=chup-1.png", File.ReadAllBytes(Path.Combine(Stills, "chup-1.png"))).Ok, $"{id}: screenshot");
            var block = With(GoodBlock(Shopee(n), []), b => b["specs"] = Rows([("Dung tích", "500 ml"), ("Đặc điểm", value)]));
            var probe = App.Paste(code + "p", Text(block));
            Expect.True(probe.Ok, $"{id}: probe {probe}");
            var sheet = ReadCases.SheetFrom(code, probe["fill"], ["anh-1.png", "anh-2.png", "anh-3.png"], "bình giữ nhiệt", rows: 2);
            sheet["pagePath"] = "/san-pham/nut-tu-cam-" + id;

            var typed = App.Post($"/api/facts?code={code}", sheet);
            if (typed.Ok)
            {
                File.Delete(App.Workspace.FactSheetPath(code));
            }

            var filled = App.Paste(code, Text(block));
            Expect.True(filled.Ok, $"{id}: block {filled}");
            foreach (var key in filled["fill"].GetProperty("fields").EnumerateArray().Select(f => f.GetProperty("member").GetString()!)
                .Concat(filled["fill"].GetProperty("candidates").EnumerateArray().Select(c => c.GetProperty("key").GetString()!)))
            {
                Expect.True(App.Post($"/api/read-confirm?code={code}", new { field = key }).Ok, $"{id}: confirm {key}");
            }

            var saved = App.Post($"/api/facts?code={code}", sheet);
            Expect.Equal(typed.Ok, saved.Ok, $"{id}: accepted alike");
            Expect.Equal(typed.Code, saved.Code, $"{id}: code");
            Expect.Equal(Direct.FirstLine(typed.Line), Direct.FirstLine(saved.Line), $"{id}: line");
            if (id != "tien")
            {
                Expect.True(!typed.Ok, $"{id}: the banned term is refused");
            }

            equal++;
            _r.Note($"({id}) \"{value}\": typed {(typed.Ok ? "accepted" : typed.Code)}, from the block {(saved.Ok ? "accepted" : saved.Code)}, line equal");
        }

        Expect.Equal(8, equal, "8 of 8");
    }

    private void Guard()
    {
        const string code = "V-75";
        var text = Text(GoodBlock(Shopee(1090)));
        string earlier;
        var other = ReadApp.Start(Path.Combine(_root, "earlier"), F, null, fixtureOrigin: Pages.Origin);
        try
        {
            earlier = other.Token;
        }
        finally
        {
            other.Dispose();
        }

        Expect.True(earlier != App.Token, "the earlier run's token differs");
        var forms = new (string Name, string? Origin, string? Token, string? Host, string Code)[]
        {
            ("foreign Origin", "http://shop-la.example", App.Token, null, "origin-not-allowed"),
            ("another port of this computer", $"http://127.0.0.1:{(App.Port == 65535 ? 1 : App.Port + 1)}", App.Token, null, "origin-not-allowed"),
            ("localhost in place of 127.0.0.1", $"http://localhost:{App.Port}", App.Token, null, "origin-not-allowed"),
            ("\"null\" Origin", "null", App.Token, null, "origin-not-allowed"),
            ("no Origin", null, App.Token, null, "origin-not-allowed"),
            ("no token", App.Origin, null, null, "token-not-valid"),
            ("wrong token", App.Origin, new string('0', 64), null, "token-not-valid"),
            ("an earlier run's token", App.Origin, earlier, null, "token-not-valid"),
            ("foreign Host", App.Origin, App.Token, "shop-la.example", "host-not-allowed"),
        };
        foreach (var (name, origin, token, host, refusal) in forms)
        {
            var (requests, connects, answer) = F.During(() => Raw($"/api/paste?code={code}", text, origin, token, host));
            Expect.Equal(403, answer.Status, $"{name}: {answer}");
            Expect.Equal(refusal, answer.Code, $"{name}: {answer}");
            Expect.Equal(0, requests.Count + connects.Count, $"{name}: connections");
            _r.Note($"{name}: 403 {answer.Code}");
        }

        Expect.True(Web.FillStore.Load(App.Workspace, code) is null && !Directory.Exists(App.Workspace.ReadImageFolder(code)), "nothing filled, no read image folder");
        var accepted = F.During(() => Raw($"/api/paste?code={code}", text, App.Origin, App.Token, null));
        Expect.True(accepted.Result.Ok, $"the same request with the page's Origin and token is accepted: {accepted.Result}");
        _r.Note($"{forms.Length} refusals; the guarded request accepted with {accepted.Requests.Count} image requests");
    }

    private Answer Raw(string path, string text, string? origin, string? token, string? host)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { text }), Encoding.UTF8, "application/json"),
        };
        if (origin is not null)
        {
            request.Headers.TryAddWithoutValidation("Origin", origin);
        }

        if (token is not null)
        {
            request.Headers.TryAddWithoutValidation(GuardState.TokenHeader, token);
        }

        if (host is not null)
        {
            request.Headers.Host = host;
        }

        using var response = App.Http.SendAsync(request).GetAwaiter().GetResult();
        var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        JsonElement element;
        try
        {
            element = body.Length == 0 ? default : JsonSerializer.Deserialize<JsonElement>(body);
        }
        catch (JsonException)
        {
            element = JsonSerializer.SerializeToElement(new { raw = body });
        }

        return new Answer((int)response.StatusCode, element);
    }

    private void Shipped()
    {
        var root = Path.Combine(_root, "shipped");
        using var harness = new AddressHarness();
        using var app = AppProcess.Start(Path.Combine(root, "app"), Path.Combine(root, "desktop"), harness);
        Expect.True(app.Post("/api/setup", new { pilotFolder = Path.Combine(root, "pilot"), ceoName = "Nguyễn Văn An", createShortcut = false }).Ok, "setup");
        var button = app.Get("/api/button");
        Expect.True(button.Ok, $"button: {button}");
        var body = Pressed(button["address"].GetString()!);
        Expect.True(body.Contains("\"origins\":[]", StringComparison.Ordinal) && !body.Contains("127.0.0.1", StringComparison.Ordinal), "the shipped app's button carries no fixture origin");
        Expect.Equal(Web.ButtonBuilder.Mark, button["mark"].GetString(), "the same mark as the script in this build");
        Expect.Equal(Web.ButtonBuilder.Build(null).Address, button["address"].GetString(), "the same address as the builder with no origin");

        var fixturePage = Text(GoodBlock(Pages.Origin + ButtonSite.ProductPath, []));
        Expect.Equal("block-read-off", app.Post("/api/paste?code=V-76", new { text = fixturePage }).Code, "switch off by default");
        Expect.True(app.Post("/api/read-switch", new { on = true }).Ok, "switch on");
        var local = app.Post("/api/paste?code=V-76", new { text = fixturePage });
        Expect.Equal("block-page-not-allowed", local.Code, $"a block whose page is on 127.0.0.1: {local}");
        var video = app.Get("/api/video?code=V-76");
        Expect.True(video.Ok && video["fill"].ValueKind != JsonValueKind.Object, $"nothing filled: {video}");
        var shopee = app.Post("/api/paste?code=V-76", new { text = Text(GoodBlock(Shopee(1095), [])) });
        Expect.True(shopee.Ok, $"a shopee.vn block with no images: {shopee}");
        Expect.Equal(0, shopee["requests"].GetInt32(), "no request");
        Expect.Equal("button", shopee["fill"].GetProperty("source").GetString(), "source");
        Thread.Sleep(500);
        var outside = harness.Rows.Where(row => row.Owner == app.Process.Id && row.Protocol.StartsWith("tcp", StringComparison.Ordinal) && !row.IsLoopback).ToList();
        Expect.Equal(0, outside.Count, $"app TCP rows off loopback: {string.Join("; ", outside)}");
        _r.Note($"shipped /api/button: {button["bytes"].GetInt32()} bytes, mark {button["mark"].GetString()}, origins []; 127.0.0.1 page: {local.Code}; shopee.vn block: accepted, 0 requests; app rows {harness.Rows.Count(r => r.Owner == app.Process.Id)}, 0 off loopback");
    }
}
