using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>One request the plain-HTTP page server answered: method, path with query, and the Cookie header it carried.</summary>
internal sealed record PageRequest(string Method, string Path, string? Cookie);

/// <summary>One page of the page server: media type, body, and extra headers (a Set-Cookie for the signed-in page).</summary>
internal sealed record PageAnswer(string MediaType, byte[] Body, IReadOnlyList<(string Name, string Value)> Headers);

/// <summary>
/// THE FIXTURE PAGE SERVER (MC-19 technical design D-004, module M-013): plain HTTP on 127.0.0.1 only, a port the system chooses, the
/// code-generated product pages and the probe script; every request is recorded. It never connects anywhere. Its origin is the exact
/// fixture origin the seam hands the app, so the button built for the test accepts these pages and the shipped button never does.
/// </summary>
internal sealed class PageServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
    private readonly Dictionary<string, PageAnswer> _pages;
    private readonly List<PageRequest> _requests = [];
    private readonly CancellationTokenSource _stop = new();

    public PageServer(Dictionary<string, PageAnswer> pages)
    {
        _pages = pages;
        _listener.Start();
        _ = Task.Run(Accept);
    }

    public string Origin => $"http://127.0.0.1:{((IPEndPoint)_listener.LocalEndpoint).Port}";

    public IReadOnlyList<PageRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    private async Task Accept()
    {
        while (!_stop.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(_stop.Token);
            }
            catch (Exception error) when (error is OperationCanceledException or ObjectDisposedException or SocketException)
            {
                return;
            }

            _ = Task.Run(() => Serve(client));
        }
    }

    private async Task Serve(TcpClient client)
    {
        using (client)
        {
            try
            {
                var stream = client.GetStream();
                var head = new StringBuilder();
                var one = new byte[1];
                while (!head.ToString().EndsWith("\r\n\r\n", StringComparison.Ordinal) && head.Length < 65536)
                {
                    if (await stream.ReadAsync(one) == 0)
                    {
                        return;
                    }

                    head.Append((char)one[0]);
                }

                var lines = head.ToString().Split("\r\n");
                var parts = lines[0].Split(' ');
                var cookie = lines.Skip(1).FirstOrDefault(l => l.StartsWith("Cookie:", StringComparison.OrdinalIgnoreCase))?[7..].Trim();
                var path = parts.Length > 1 ? parts[1] : "/";
                lock (_requests)
                {
                    _requests.Add(new PageRequest(parts[0], path, cookie));
                }

                var key = path.Split('?')[0];
                var found = _pages.TryGetValue(path, out var page) || _pages.TryGetValue(key, out page);
                var body = found ? page!.Body : Encoding.UTF8.GetBytes("không có trang này");
                var answer = new StringBuilder();
                answer.Append(found ? "HTTP/1.1 200 OK\r\n" : "HTTP/1.1 404 Not Found\r\n");
                answer.Append($"Content-Type: {(found ? page!.MediaType : "text/plain; charset=utf-8")}\r\n");
                answer.Append($"Content-Length: {body.Length}\r\nCache-Control: no-store\r\nConnection: close\r\n");
                foreach (var (name, value) in found ? page!.Headers : [])
                {
                    answer.Append($"{name}: {value}\r\n");
                }

                answer.Append("\r\n");
                await stream.WriteAsync(Encoding.ASCII.GetBytes(answer.ToString()));
                await stream.WriteAsync(body);
                await stream.FlushAsync();
            }
            catch (IOException)
            {
                // The browser closed the connection first.
            }
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        _listener.Stop();
        _stop.Dispose();
    }
}

/// <summary>
/// THE BUTTON FIXTURE PAGES (MC-19 design Q-002 at the Design Gate: grounded on the CEO's screenshot of a product page, the MC-18 paste
/// fixture and the ticket's list). The product page carries what the screenshot shows: the product name as the main heading, the price,
/// the "Phân loại" buttons with one selected, the shop block with its Mall badge, "Chat Ngay" and "Xem Shop", the "CHI TIẾT SẢN PHẨM"
/// label and value rows, "MÔ TẢ SẢN PHẨM", the image gallery, and products of other shops under "SẢN PHẨM TƯƠNG TỰ". Image addresses
/// point at the MC-18 fixture image host, which the browser cannot resolve (the shell maps every name but 127.0.0.1 to nothing) and the
/// app reaches only through the seam. Every page loads the probe, which counts clicks, form submissions, copy events and page unloads.
/// </summary>
internal static class ButtonSite
{
    public const string ProductPath = "/binh-giu-nhiet-mau-i.100.200";
    public const string PlainShopPath = "/hop-com-mau-i.100.500";
    public const string SignedInPath = "/binh-giu-nhiet-dang-nhap-i.100.210";
    public const string ShopPath = "/shop/gian-hang-mau";
    public const string SearchPath = "/search";
    public const string NamelessPath = "/san-pham-khong-ten-i.100.220";
    public const string MarkupPath = "/san-pham-co-the-i.100.230";
    public const string ManyPath = "/nhieu-anh-nhieu-dong-i.100.240";

    public const string ShopName = "Gian hàng mẫu MC";
    public const string PlainShopName = "Cửa hàng thường MC";
    public const string PagePrice = "₫189.000";
    public const string Account = "taikhoan_mau_mc19";
    public const string CookieValue = "mc19-cookie-mau-7f3a";
    public const string StoredValue = "mc19-luu-tru-mau-9c2e";

    /// <summary>The rows the product page shows under its first row "Danh Mục" (the category path the page shows, as on the screenshot).</summary>
    public static readonly IReadOnlyList<(string Attribute, string Value)> Rows = FixtureSite.Specs;

    public const string CategoryValue = "Shopee > Nhà Cửa & Đời Sống";

    /// <summary>The five rows the button must carry from the product page, in page order.</summary>
    public static IReadOnlyList<(string Attribute, string Value)> ExpectedRows => [("Danh Mục", CategoryValue), .. Rows];

    /// <summary>The probe: counts what the press must never do, and plants the stored value on the signed-in page.</summary>
    public const string Probe = """
        (function () {
          var p = { clicks: 0, submits: 0, copies: 0, unloads: 0, loadId: String(Math.random()).slice(2) };
          window.__probe = p;
          document.addEventListener("click", function () { p.clicks++; }, true);
          document.addEventListener("submit", function (e) { p.submits++; e.preventDefault(); }, true);
          document.addEventListener("copy", function () { p.copies++; }, true);
          window.addEventListener("beforeunload", function () { p.unloads++; });
          if (document.documentElement.getAttribute("data-signed-in") === "1") {
            localStorage.setItem("mc19-luu-tru", "mc19-luu-tru-mau-9c2e");
            sessionStorage.setItem("mc19-phien", "mc19-luu-tru-mau-9c2e");
          }
        })();
        """;

    public static Dictionary<string, PageAnswer> Pages()
    {
        var pages = new Dictionary<string, PageAnswer>(StringComparer.Ordinal)
        {
            ["/probe.js"] = new("text/javascript; charset=utf-8", Encoding.UTF8.GetBytes(Probe), []),
            [ProductPath] = Html(Product(FixtureSite.Name, FixtureSite.Classification, ShopName, mall: true, Rows, FixtureSite.Description, FixtureSite.OwnImages, signedIn: false)),
            [PlainShopPath] = Html(Product("Hộp cơm mẫu 3 ngăn", null, PlainShopName, mall: false, [("Dung tích", "1,2 lít"), ("Chất liệu", "Nhựa PP")], "Hộp cơm ba ngăn có nắp gài.",
                [.. FixtureSite.OwnImages.Take(2)], signedIn: false)),
            [SignedInPath] = Html(Product(FixtureSite.Name, FixtureSite.Classification, ShopName, mall: true, Rows, FixtureSite.Description, FixtureSite.OwnImages, signedIn: true),
                ("Set-Cookie", $"SPC_U={CookieValue}; Path=/")),
            [ShopPath] = Html(Page("Gian hàng mẫu MC | Shopee Việt Nam", false,
                $"<main><section class=\"shop-head\"><h1>{ShopName}</h1><span>Mall</span><button type=\"button\">Chat Ngay</button></section>"
                + "<section><h2>SẢN PHẨM CỦA SHOP</h2>" + string.Concat(FixtureSite.OtherImages.Select((src, i) => $"<a href=\"/sp-{i}\"><img src=\"{src}\" alt=\"\"><div>Sản phẩm {i + 1}</div></a>"))
                + "</section></main>")),
            [SearchPath] = Html(Page("Tìm kiếm | Shopee Việt Nam", false,
                "<main><div>Kết quả tìm kiếm cho 'bình giữ nhiệt'</div><ul>"
                + string.Concat(FixtureSite.OtherImages.Select((src, i) => $"<li><a href=\"/kq-{i}\"><img src=\"{src}\" alt=\"\"><div>Bình giữ nhiệt {i + 1}</div><div>₫{150 + i}.000</div></a></li>"))
                + "</ul></main>")),
            [NamelessPath] = Html(Product(null, FixtureSite.Classification, ShopName, mall: true, Rows, FixtureSite.Description, FixtureSite.OwnImages, signedIn: false)),
            [MarkupPath] = Html(Product(Markup.Name, Markup.Classification, Markup.Shop, mall: false, [(Markup.Attribute, Markup.Value)], Markup.Description, [FixtureSite.OwnImages[0]], signedIn: false)),
            [ManyPath] = Html(Product("Bộ hộp mẫu nhiều ảnh", null, ShopName, mall: true,
                [.. Enumerable.Range(1, 105).Select(i => ($"Mục {i:000}", $"Giá trị {i}"))], "Mô tả ngắn.",
                [.. Enumerable.Range(1, 34).Select(i => $"https://{FixtureSite.ImageHost}/file/mc19-nhieu-{i}")], signedIn: false)),
        };
        return pages;
    }

    /// <summary>Markup and script in each text member the button carries (SC-25): every one must be shown as text.</summary>
    public static class Markup
    {
        public const string Name = "Bình <img src=x onerror=\"window.__mc19=1\"> mẫu";
        public const string Classification = "<script>window.__mc19=2</script>Đỏ";
        public const string Shop = "Shop <b onclick=\"window.__mc19=3\">đậm</b>";
        public const string Attribute = "Chất <i>liệu</i>";
        public const string Value = "<a href=\"javascript:window.__mc19=4\">Thép</a>";
        public const string Description = "<iframe src=\"javascript:window.__mc19=5\"></iframe>Mô tả <svg onload=\"window.__mc19=6\">";
    }

    private static PageAnswer Html(string html, params (string Name, string Value)[] headers) =>
        new("text/html; charset=utf-8", Encoding.UTF8.GetBytes(html), headers);

    private static string E(string text) => WebUtility.HtmlEncode(text);

    private static string Page(string title, bool signedIn, string main) =>
        $"<!DOCTYPE html><html lang=\"vi\"{(signedIn ? " data-signed-in=\"1\"" : string.Empty)}><head><meta charset=\"utf-8\"><title>{E(title)}</title>"
        + "<script src=\"/probe.js\"></script>"
        + "<style>body{font:14px sans-serif;margin:0}section{margin:12px;padding:12px;border:1px solid #ddd}.gallery img{width:60px;height:60px}"
        + ".mall{background:#d0011b;color:#fff;padding:0 4px}button[aria-pressed=true]{border:2px solid #ee4d2d}</style></head><body>"
        + "<header class=\"top\"><a href=\"/\">Shopee</a> <form action=\"/search\"><input name=\"keyword\" value=\"\"><button type=\"submit\">Tìm</button></form>"
        + (signedIn ? $"<span class=\"account\">{Account}</span>" : "<a href=\"/buyer/login\">Đăng nhập</a>")
        + "</header>" + main + "<footer><div>Chính sách bảo mật</div><div>Shopee Việt Nam</div></footer></body></html>";

    /// <summary>A product page in the order the CEO's screenshot shows; the name null leaves the main heading out.</summary>
    public static string Product(string? name, string? selected, string shop, bool mall, IReadOnlyList<(string Attribute, string Value)> rows, string description,
        IReadOnlyList<string> images, bool signedIn)
    {
        var html = new StringBuilder("<main><nav><a href=\"/\">Shopee</a> &gt; <a href=\"/cat\">Nhà Cửa &amp; Đời Sống</a></nav>");
        html.Append("<section class=\"product\"><div class=\"gallery\">");
        if (images.Count > 0)
        {
            html.Append($"<div class=\"main-image\"><img src=\"{E(images[0])}\" alt=\"\"></div>");
        }

        html.Append("<div class=\"thumbs\">");
        foreach (var image in images)
        {
            html.Append($"<img src=\"{E(image)}\" alt=\"\">");
        }

        html.Append("</div><div class=\"share\">Chia sẻ: <button type=\"button\">Facebook</button></div></div><div class=\"info\">");
        if (name is not null)
        {
            html.Append($"<h1>{E(name)}</h1>");
        }

        html.Append("<div class=\"rating\">4,9 ★ | 1,2k Đánh Giá | 3,4k Đã Bán</div>");
        html.Append($"<div class=\"price\"><span>{E(PagePrice)}</span> <s>₫250.000</s> <span>24% GIẢM</span></div>");
        html.Append("<div class=\"ship\"><span>Vận Chuyển</span><span>Miễn phí vận chuyển</span></div>");
        html.Append("<div class=\"variations\"><div class=\"option-row\"><span class=\"label\">Phân loại</span><div class=\"options\">");
        foreach (var option in new[] { selected ?? FixtureSite.Classification, "Đen, 750 ml" }.Distinct())
        {
            var pressed = selected is not null && option == selected;
            html.Append($"<button type=\"button\" aria-pressed=\"{(pressed ? "true" : "false")}\">{E(option)}</button>");
        }

        html.Append("</div></div></div>");
        html.Append("<div class=\"quantity\"><span>Số Lượng</span><button type=\"button\">-</button><input value=\"1\"><button type=\"button\">+</button><span>120 sản phẩm có sẵn</span></div>");
        html.Append("<form action=\"/cart/add\" method=\"post\"><button type=\"submit\">Thêm Vào Giỏ Hàng</button><button type=\"submit\">Mua Ngay</button></form>");
        html.Append("</div></section>");

        html.Append("<section class=\"shop\"><div class=\"avatar\">");
        html.Append(mall ? "<span class=\"mall\">Mall</span>" : "<span class=\"fav\">Yêu Thích</span>");
        html.Append($"</div><div class=\"shop-name\">{E(shop)}</div><div class=\"online\">Online 5 Phút Trước</div>");
        html.Append("<div class=\"shop-actions\"><button type=\"button\">Chat Ngay</button><a href=\"/shop/gian-hang-mau\">Xem Shop</a></div>");
        html.Append("<div class=\"shop-stats\"><div><span>Đánh Giá</span><span>12,3k</span></div><div><span>Sản Phẩm</span><span>342</span></div></div></section>");

        html.Append("<section class=\"detail\"><h2>CHI TIẾT SẢN PHẨM</h2><div class=\"rows\">");
        html.Append("<div class=\"row\"><label>Danh Mục</label><div><a href=\"/\">Shopee</a> &gt; <a href=\"/cat\">Nhà Cửa &amp; Đời Sống</a></div></div>");
        foreach (var (attribute, value) in rows)
        {
            html.Append($"<div class=\"row\"><label>{E(attribute)}</label><div>{E(value)}</div></div>");
        }

        html.Append("</div><h2>MÔ TẢ SẢN PHẨM</h2><div class=\"about\">");
        foreach (var line in description.Split('\n'))
        {
            html.Append($"<p>{E(line)}</p>");
        }

        html.Append("</div></section><section class=\"similar\"><h2>SẢN PHẨM TƯƠNG TỰ</h2>");
        for (var i = 0; i < FixtureSite.OtherImages.Count; i++)
        {
            html.Append($"<a href=\"/binh-khac-i.100.30{i}\"><img src=\"{E(FixtureSite.OtherImages[i])}\" alt=\"\"><div>Bình khác {i + 1}</div><div>₫{120 + i}.000</div></a>");
        }

        html.Append("</section></main>");
        return Page((name ?? "Sản phẩm") + " | Shopee Việt Nam", signedIn, html.ToString());
    }
}
