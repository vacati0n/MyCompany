using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json.Nodes;

namespace MediaCompany.Pilot.Web.Tests;

/// <summary>One request the fixture server received: the connection's two addresses, the method, host, path and every header as sent.</summary>
internal sealed record FixtureRequest(DateTime At, IPEndPoint Client, string Method, string Host, string Path, IReadOnlyList<(string Name, string Value)> Headers)
{
    public string PathOnly => Path.Split('?', 2)[0];

    public bool Has(string header) => Headers.Any(h => string.Equals(h.Name, header, StringComparison.OrdinalIgnoreCase));

    public string? Header(string header) => Headers.FirstOrDefault(h => string.Equals(h.Name, header, StringComparison.OrdinalIgnoreCase)).Value;

    public override string ToString() => $"{Method} {Host}{Path}";
}

/// <summary>One answer of the fixture site: status, extra headers, media type and body.</summary>
internal sealed record FixtureAnswer(int Status, string? MediaType, byte[] Body, IReadOnlyList<(string Name, string Value)> Headers)
{
    public static FixtureAnswer Html(string html, params (string Name, string Value)[] headers) =>
        new(200, "text/html; charset=utf-8", Encoding.UTF8.GetBytes(html), headers);

    public static FixtureAnswer Redirect(int status, string location) => new(status, null, [], [("Location", location)]);

    public static FixtureAnswer Plain(int status) => new(status, "text/html; charset=utf-8", Encoding.UTF8.GetBytes($"<html><body>{status}</body></html>"), []);

    public static FixtureAnswer Png(byte[] png) => new(200, "image/png", png, []);

    /// <summary>How long the fixture waits before it answers (a slow server; each wait stays under the read's 30 s request bound).</summary>
    public TimeSpan Delay { get; init; }

    /// <summary>Send no Content-Length: the body ends when the connection closes, so a size bound must count while it reads.</summary>
    public bool NoLength { get; init; }
}

/// <summary>
/// THE READ FIXTURE (MC-18 design decision D-004; module M-017). A code-generated HTTPS server on 127.0.0.1 standing in for every host a
/// read may ask for: it presents a certificate made in code for this run, answers each request from the fixture site by host and path,
/// and logs every request with its headers and its client address. Beside it: a listener that accepts and never answers (the silent
/// server), a port that refuses connections (no connection), and a listener posing as a proxy that only counts connections. The read
/// reaches it only through the transport seam this class hands to the internal start path: the seam sends each host name to one of these
/// loopback endpoints, records the name, and trusts only this run's certificate. Nothing here resolves a name or leaves 127.0.0.1.
/// </summary>
internal sealed class ReadFixture : IDisposable
{
    private readonly TcpListener _tls = new(IPAddress.Loopback, 0);
    private readonly TcpListener _silent = new(IPAddress.Loopback, 0);
    private readonly TcpListener _proxy = new(IPAddress.Loopback, 0);
    private readonly List<TcpClient> _held = [];
    private readonly List<FixtureRequest> _requests = [];
    private readonly List<string> _connects = [];
    private readonly CancellationTokenSource _stop = new();
    private int _proxyConnections;

    public ReadFixture()
    {
        Certificate = MakeCertificate();
        Site = FixtureSite.Build();
        _tls.Start();
        _silent.Start();
        _proxy.Start();
        var closed = new TcpListener(IPAddress.Loopback, 0);
        closed.Start();
        ClosedPort = ((IPEndPoint)closed.LocalEndpoint).Port;
        closed.Stop();
        _ = Task.Run(() => Serve(_stop.Token));
        _ = Task.Run(() => Hold(_silent, _stop.Token, count: false));
        _ = Task.Run(() => Hold(_proxy, _stop.Token, count: true));
    }

    public X509Certificate2 Certificate { get; }

    public IReadOnlyDictionary<string, Func<FixtureRequest, FixtureAnswer>> Site { get; }

    public int Port => ((IPEndPoint)_tls.LocalEndpoint).Port;

    public int SilentPort => ((IPEndPoint)_silent.LocalEndpoint).Port;

    public int ProxyPort => ((IPEndPoint)_proxy.LocalEndpoint).Port;

    public int ClosedPort { get; }

    public int ProxyConnections => Volatile.Read(ref _proxyConnections);

    /// <summary>The seam for the internal start path: each name to its fixture endpoint, recorded; this run's certificate trusted only.</summary>
    public ReadTransport Transport => new(ConnectTo, c => string.Equals(c.Thumbprint, Certificate.Thumbprint, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyList<FixtureRequest> Requests
    {
        get
        {
            lock (_requests)
            {
                return [.. _requests];
            }
        }
    }

    /// <summary>Every host name the read asked the seam to connect to, in order.</summary>
    public IReadOnlyList<string> Connects
    {
        get
        {
            lock (_connects)
            {
                return [.. _connects];
            }
        }
    }

    /// <summary>The requests and connections made by an action, apart from those before it.</summary>
    public (IReadOnlyList<FixtureRequest> Requests, IReadOnlyList<string> Connects, T Result) During<T>(Func<T> action)
    {
        ArgumentNullException.ThrowIfNull(action);
        var requests = Requests.Count;
        var connects = Connects.Count;
        var result = action();
        Thread.Sleep(150);
        return ([.. Requests.Skip(requests)], [.. Connects.Skip(connects)], result);
    }

    private IPEndPoint ConnectTo(string host, int port)
    {
        lock (_connects)
        {
            _connects.Add(host);
        }

        var target = host switch
        {
            FixtureSite.SilentHost => SilentPort,
            FixtureSite.ClosedHost => ClosedPort,
            _ => Port,
        };
        return new IPEndPoint(IPAddress.Loopback, target);
    }

    private async Task Serve(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _tls.AcceptTcpClientAsync(token);
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException)
            {
                continue;
            }

            _ = Task.Run(() => Answer(client, token), token);
        }
    }

    private async Task Answer(TcpClient client, CancellationToken token)
    {
        using (client)
        {
            try
            {
                var remote = (IPEndPoint)client.Client.RemoteEndPoint!;
                await using var tls = new SslStream(client.GetStream(), leaveInnerStreamOpen: false);
                await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = Certificate }, token);
                var head = await ReadHead(tls, token);
                if (head is null)
                {
                    return;
                }

                var lines = head.Split("\r\n");
                var parts = lines[0].Split(' ');
                var headers = lines.Skip(1).Where(l => l.Length > 0).Select(l => (l[..l.IndexOf(':', StringComparison.Ordinal)].Trim(), l[(l.IndexOf(':', StringComparison.Ordinal) + 1)..].Trim())).ToList();
                var host = headers.FirstOrDefault(h => string.Equals(h.Item1, "Host", StringComparison.OrdinalIgnoreCase)).Item2?.Split(':')[0].ToLowerInvariant().TrimEnd('.') ?? string.Empty;
                var request = new FixtureRequest(DateTime.UtcNow, remote, parts[0], host, parts.Length > 1 ? parts[1] : "/", headers);
                lock (_requests)
                {
                    _requests.Add(request);
                }

                var answer = Site.TryGetValue(host + request.PathOnly, out var route) ? route(request) : FixtureAnswer.Plain(404);
                if (answer.Delay > TimeSpan.Zero)
                {
                    await Task.Delay(answer.Delay, token);
                }

                var text = new StringBuilder();
                text.Append(System.Globalization.CultureInfo.InvariantCulture, $"HTTP/1.1 {answer.Status} {Reason(answer.Status)}\r\n");
                if (answer.MediaType is not null)
                {
                    text.Append($"Content-Type: {answer.MediaType}\r\n");
                }

                foreach (var (name, value) in answer.Headers)
                {
                    text.Append($"{name}: {value}\r\n");
                }

                if (!answer.NoLength)
                {
                    text.Append(System.Globalization.CultureInfo.InvariantCulture, $"Content-Length: {answer.Body.Length}\r\n");
                }

                text.Append("Connection: close\r\n\r\n");
                await tls.WriteAsync(Encoding.ASCII.GetBytes(text.ToString()), token);
                await tls.WriteAsync(answer.Body, token);
                await tls.FlushAsync(token);
            }
            catch (Exception error) when (error is IOException or System.Security.Authentication.AuthenticationException or OperationCanceledException or SocketException)
            {
                // A client that hangs up (a stopped read) ends its own connection only.
            }
        }
    }

    private static async Task<string?> ReadHead(Stream stream, CancellationToken token)
    {
        var buffer = new List<byte>();
        var one = new byte[1];
        while (buffer.Count < 65536)
        {
            if (await stream.ReadAsync(one, token) == 0)
            {
                return null;
            }

            buffer.Add(one[0]);
            if (buffer.Count >= 4 && buffer[^4] == '\r' && buffer[^3] == '\n' && buffer[^2] == '\r' && buffer[^1] == '\n')
            {
                return Encoding.ASCII.GetString(buffer.ToArray(), 0, buffer.Count - 4);
            }
        }

        return null;
    }

    /// <summary>The silent server keeps each connection open and never answers; the proxy listener only counts connections.</summary>
    private async Task Hold(TcpListener listener, CancellationToken token, bool count)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var client = await listener.AcceptTcpClientAsync(token);
                if (count)
                {
                    Interlocked.Increment(ref _proxyConnections);
                }

                lock (_held)
                {
                    _held.Add(client);
                }
            }
            catch (Exception) when (token.IsCancellationRequested)
            {
                return;
            }
            catch (SocketException)
            {
                // Next connection.
            }
        }
    }

    private static string Reason(int status) => status switch
    {
        200 => "OK",
        301 => "Moved Permanently",
        302 => "Found",
        303 => "See Other",
        307 => "Temporary Redirect",
        308 => "Permanent Redirect",
        403 => "Forbidden",
        404 => "Not Found",
        410 => "Gone",
        429 => "Too Many Requests",
        503 => "Service Unavailable",
        _ => "Status",
    };

    /// <summary>A certificate made for this run only, reloaded so the TLS stack can use its key; nothing is installed in any store.</summary>
    private static X509Certificate2 MakeCertificate()
    {
        using var key = RSA.Create(2048);
        var request = new CertificateRequest("CN=mc18-read-fixture", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("shopee.vn");
        names.AddDnsName("*.shopee.vn");
        request.CertificateExtensions.Add(names.Build());
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid("1.3.6.1.5.5.7.3.1")], critical: false));
        using var made = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(2));
        return X509CertificateLoader.LoadPkcs12(made.Export(X509ContentType.Pfx), null);
    }

    public void Dispose()
    {
        _stop.Cancel();
        _tls.Stop();
        _silent.Stop();
        _proxy.Stop();
        lock (_held)
        {
            foreach (var client in _held)
            {
                client.Dispose();
            }

            _held.Clear();
        }

        Certificate.Dispose();
        _stop.Dispose();
    }
}

/// <summary>
/// THE FIXTURE SITE: every page, redirect, block and image the read cases need, keyed by host and path, all made in code. Product
/// pages carry a structured product block, page tags, labelled visible text, links to other products, a shop, a category and a search,
/// images of other products, a style sheet and a script; none of those may be requested.
/// </summary>
internal static class FixtureSite
{
    public const string Shop = "shopee.vn";
    public const string ImageHost = "down-vn.img.susercontent.com";
    public const string SilentHost = "im-lang.shopee.vn";
    public const string ClosedHost = "dong-cua.shopee.vn";

    public const string GoodPath = "/binh-giu-nhiet-mau-i.100.200";
    public const string GoodLink = "https://shopee.vn" + GoodPath;
    public const string ShortLink = "https://s.shopee.vn/mc18-ngan";
    public const string SecondLink = "https://shopee.vn/hop-com-mau-i.100.500";
    public const string ForeignImageLink = "https://shopee.vn/coc-su-mau-i.100.400";
    public const string ChainLink = "https://shopee.vn/chuoi-0";
    public const string AffiliateLink = "https://affiliate.shopee.vn/mc18-uu-dai";
    public const string AffiliateHopLink = "https://shopee.vn/qua-affiliate";
    public const string ForeignImageHost = "cdn.anh-ngoai.example";

    public const string Name = "Bình giữ nhiệt mẫu 500 ml";
    public const string Classification = "Xanh rêu, 500 ml";
    public const string Price = "189.000₫";
    public const string Description = "Bình giữ nhiệt bằng thép không gỉ, nắp vặn kín.\nRửa bằng nước ấm, không dùng máy rửa bát.";

    public static readonly IReadOnlyList<(string Attribute, string Value)> Specs =
    [
        ("Dung tích", "500 ml"),
        ("Chất liệu", "Thép không gỉ 304"),
        ("Thương hiệu", "MC Mẫu"),
        ("Xuất xứ", "Việt Nam"),
    ];

    /// <summary>The five own images of the good product and the three images of other products its page shows.</summary>
    public static readonly IReadOnlyList<string> OwnImages = [.. Enumerable.Range(1, 5).Select(i => $"https://{ImageHost}/file/mc18-own-{i}")];

    public static readonly IReadOnlyList<string> OtherImages = [.. Enumerable.Range(1, 3).Select(i => $"https://{ImageHost}/file/mc18-other-{i}")];

    /// <summary>The nine stops of SC-12 (scope A-012): link and the code each must end with.</summary>
    public static readonly IReadOnlyList<(string Name, string Link, string Code)> Stops =
    [
        ("403", "https://shopee.vn/bi-chan-403", "access-refused"),
        ("429", "https://shopee.vn/qua-nhieu-429", "too-many-requests"),
        ("chuyển sang đăng nhập", "https://shopee.vn/can-dang-nhap", "sign-in-redirect"),
        ("tường đăng nhập", "https://shopee.vn/tuong-dang-nhap", "sign-in-wall"),
        ("CAPTCHA", "https://shopee.vn/xac-minh-mc18", "captcha"),
        ("không có sản phẩm", "https://shopee.vn/vo-trang-mc18", "no-product"),
        ("5xx", "https://shopee.vn/loi-may-chu", "server-error"),
        ("không kết nối", $"https://{ClosedHost}/sp-1", "no-connection"),
        ("không trả lời", $"https://{SilentHost}/sp-1", "no-answer"),
    ];

    /// <summary>
    /// The image-phase stops (review correction CR-001, D-010): a product page declaring own image 1, then the image that stops the read,
    /// then own image 3. Each must end the read at the stopping image with no request for own image 3 (or for a redirect's target), keep
    /// the filled facts and the one image saved before it, and name the stopping image and own image 3 for upload at step 2.
    /// </summary>
    public static readonly IReadOnlyList<(string Name, string Link, string Image, string Code)> ImageStops =
    [
        ("ảnh bị chặn 403", "https://shopee.vn/anh-dung-403-i.100.701", $"https://{ImageHost}/file/mc18-dung-403", "access-refused"),
        ("ảnh 429", "https://shopee.vn/anh-dung-429-i.100.702", $"https://{ImageHost}/file/mc18-dung-429", "too-many-requests"),
        ("ảnh lỗi máy chủ", "https://shopee.vn/anh-dung-503-i.100.703", $"https://{ImageHost}/file/mc18-dung-503", "server-error"),
        ("ảnh mã 410", "https://shopee.vn/anh-dung-410-i.100.711", $"https://{ImageHost}/file/mc18-dung-410", "status-unexpected"),
        ("ảnh chuyển tiếp", "https://shopee.vn/anh-dung-chuyen-i.100.704", $"https://{ImageHost}/file/mc18-dung-chuyen", "image-redirect"),
        ("ảnh chuyển sang đăng nhập", "https://shopee.vn/anh-dung-dang-nhap-i.100.705", $"https://{ImageHost}/file/mc18-dung-dang-nhap", "sign-in-redirect"),
        ("ảnh chuyển sang xác minh", "https://shopee.vn/anh-dung-xac-minh-i.100.706", $"https://{ImageHost}/file/mc18-dung-xac-minh", "captcha"),
        ("ảnh là trang CAPTCHA", "https://shopee.vn/anh-dung-captcha-i.100.707", $"https://{ImageHost}/file/mc18-dung-captcha", "captcha"),
        ("ảnh là trang đăng nhập", "https://shopee.vn/anh-dung-tuong-i.100.708", $"https://{ImageHost}/file/mc18-dung-tuong", "sign-in-wall"),
        ("ảnh không phải ảnh", "https://shopee.vn/anh-dung-khong-anh-i.100.709", $"https://{ImageHost}/file/mc18-dung-khong-anh", "not-image"),
        ("ảnh quá 40 MiB", "https://shopee.vn/anh-dung-qua-lon-i.100.710", $"https://{ImageHost}/file/mc18-dung-qua-lon", "image-too-large"),
        ("ảnh quá 40 MiB, không ghi độ dài", "https://shopee.vn/anh-dung-qua-lon-dai-i.100.712", $"https://{ImageHost}/file/mc18-dung-qua-lon-dai", "image-too-large"),
        ("máy chủ ảnh đóng cổng", "https://shopee.vn/anh-dung-dong-cua-i.100.713", $"https://{ClosedHost}/file/mc18-anh", "no-connection"),
        ("máy chủ ảnh không trả lời", "https://shopee.vn/anh-dung-im-lang-i.100.714", $"https://{SilentHost}/file/mc18-anh", "no-answer"),
    ];

    /// <summary>The page-phase bounds and stops of review correction CR-004: link and the code it must end with, after one request.</summary>
    public static readonly IReadOnlyList<(string Name, string Link, string Code)> PageBounds =
    [
        ("trang quá 5 MiB", "https://shopee.vn/trang-qua-lon", "page-too-large"),
        ("trang quá 5 MiB, không ghi độ dài", "https://shopee.vn/trang-qua-lon-dai", "page-too-large"),
        ("không phải trang web", "https://shopee.vn/khong-phai-trang", "not-html"),
        ("mã 404", "https://shopee.vn/khong-con-404", "status-unexpected"),
        ("mã 204", "https://shopee.vn/khong-noi-dung-204", "status-unexpected"),
        ("chuyển tiếp không ghi địa chỉ", "https://shopee.vn/chuyen-khong-dia-chi", "status-unexpected"),
    ];

    /// <summary>A product page whose five own images each answer after 25 s: the read passes the 120 s click bound at the fifth.</summary>
    public const string SlowLink = "https://shopee.vn/cham-dan-i.100.720";

    public static readonly TimeSpan SlowImageDelay = TimeSpan.FromSeconds(25);

    /// <summary>A product page declaring 22 own images: the read fetches the first 20 and names the other two (D-014).</summary>
    public const string ManyImagesLink = "https://shopee.vn/nhieu-anh-i.100.730";

    public static readonly IReadOnlyList<string> ManyImages = [.. Enumerable.Range(1, 22).Select(i => $"https://{ImageHost}/file/mc18-nhieu-{i}")];

    /// <summary>
    /// Hops from an allowed page to each host and hop edge of review correction CR-003: kind, link, the hop's target, the code it must
    /// stop with. Each sends exactly one request (to the hopping page) and opens one connection (to shopee.vn).
    /// </summary>
    public static readonly IReadOnlyList<(string Kind, string Link, string Target, string Code)> HopEdges =
    [
        ("chuyển sang http", "https://shopee.vn/chuyen-http", "http://shopee.vn" + GoodPath, "hop-not-secure"),
        ("chuyển sang /verify/", "https://shopee.vn/chuyen-xac-minh", "/verify/captcha?anti_bot_tracking_id=mc18", "captcha"),
        ("chuyển sang IPv6", "https://shopee.vn/chuyen-ipv6", "https://[2001:db8::7]/binh-giu-nhiet", "hop-not-allowed"),
        ("chuyển sang cổng :443", "https://shopee.vn/chuyen-cong-443", "https://shopee.vn:443" + GoodPath, "hop-not-allowed"),
        ("chuyển sang tên giả (IDN)", "https://shopee.vn/chuyen-idn", "https://xn--shpee-kye.vn/binh-giu-nhiet", "hop-not-allowed"),
        ("chuyển sang tên con của affiliate", "https://shopee.vn/chuyen-affiliate-con", "https://m.affiliate.shopee.vn/mc18", "hop-affiliate"),
        ("chuyển sang affiliate có dấu chấm cuối", "https://shopee.vn/chuyen-affiliate-cham", "https://affiliate.shopee.vn./mc18", "hop-affiliate"),
        ("chuyển sang tên chữ hoa ngoài Shopee", "https://shopee.vn/chuyen-chu-hoa", "https://SHOPEE.VN.EXAMPLE.TEST/binh", "hop-not-allowed"),
    ];

    /// <summary>The six disallowed kinds of SC-24 (scope A-024): five links refused before any connection, and a hop to each.</summary>
    public static readonly IReadOnlyList<(string Kind, string Link, string Host)> Disallowed =
    [
        ("máy chủ không phải Shopee", "https://example.com/binh-giu-nhiet", "example.com"),
        ("tên kết thúc bằng shopee.vn", "https://xshopee.vn/binh-giu-nhiet", "xshopee.vn"),
        ("shopee.vn đứng bên trái", "https://shopee.vn.example.test/binh-giu-nhiet", "shopee.vn.example.test"),
        ("địa chỉ số", "https://203.0.113.7/binh-giu-nhiet", "203.0.113.7"),
        ("Shopee nước khác", "https://shopee.co.id/binh-giu-nhiet", "shopee.co.id"),
    ];

    /// <summary>The eight SC-10 pages (scope A-010): one specification value with one banned term of classes (a) to (g), or a money amount.</summary>
    public static IReadOnlyList<(string Id, string Link, string Value)> BannedPages =>
        [.. UnitCases.ClassExamples.Select(c => (c.ClassId, $"https://shopee.vn/tu-cam-{c.ClassId}-i.100.6{c.ClassId}", c.Text)),
            ("tien", "https://shopee.vn/tu-cam-tien-i.100.6t", "giá gốc 250.000đ")];

    public static Dictionary<string, Func<FixtureRequest, FixtureAnswer>> Build()
    {
        var site = new Dictionary<string, Func<FixtureRequest, FixtureAnswer>>(StringComparer.Ordinal);
        void Page(string link, Func<FixtureRequest, FixtureAnswer> answer) => site[new Uri(link).Host + new Uri(link).AbsolutePath] = answer;

        Page(GoodLink, _ => FixtureAnswer.Html(GoodPage(), ("Set-Cookie", "SPC_F=mc18-fixture; Path=/; Secure; HttpOnly")));
        for (var i = 1; i <= 5; i++)
        {
            var png = OwnPng(i);
            Page(OwnImages[i - 1], _ => FixtureAnswer.Png(png));
        }

        for (var i = 1; i <= 3; i++)
        {
            var png = Png(48, 48, 10 + i);
            Page(OtherImages[i - 1], _ => FixtureAnswer.Png(png));
        }

        // A short link in two hops to the good page, the second with a query the page address drops.
        Page(ShortLink, _ => FixtureAnswer.Redirect(302, "https://shopee.vn/universal-link/mc18"));
        Page("https://shopee.vn/universal-link/mc18", _ => FixtureAnswer.Redirect(301, GoodLink + "?sp_atk=mc18&xptdk=ngan"));

        // Six hops: one more than the rule allows.
        for (var i = 0; i < 6; i++)
        {
            var next = $"https://shopee.vn/chuoi-{i + 1}";
            Page($"https://shopee.vn/chuoi-{i}", _ => FixtureAnswer.Redirect(302, next));
        }

        Page("https://shopee.vn/chuoi-6", _ => FixtureAnswer.Html(GoodPage()));

        // The SC-12 stops.
        Page("https://shopee.vn/bi-chan-403", _ => FixtureAnswer.Plain(403));
        Page("https://shopee.vn/qua-nhieu-429", _ => FixtureAnswer.Plain(429));
        Page("https://shopee.vn/loi-may-chu", _ => FixtureAnswer.Plain(503));
        Page("https://shopee.vn/can-dang-nhap", _ => FixtureAnswer.Redirect(302, "https://shopee.vn/buyer/login?next=%2Fcan-dang-nhap"));
        Page("https://shopee.vn/buyer/login", _ => FixtureAnswer.Html(SignInPage()));
        Page("https://shopee.vn/tuong-dang-nhap", _ => FixtureAnswer.Html(SignInPage()));
        Page("https://shopee.vn/xac-minh-mc18", _ => FixtureAnswer.Html(
            "<!DOCTYPE html><html lang=\"vi\"><head><title>Xác minh</title></head><body><h1>Xác minh để tiếp tục</h1><p>Kéo thanh trượt để ghép hình.</p>"
            + "<form method=\"post\" action=\"/verify/traffic\"><input type=\"hidden\" name=\"answer\"><button>Gửi</button></form></body></html>"));
        Page("https://shopee.vn/vo-trang-mc18", _ => FixtureAnswer.Html(
            "<!DOCTYPE html><html lang=\"vi\"><head><title>Shopee Việt Nam</title><script src=\"/mc18-goi.js\"></script></head><body><div id=\"main\"></div><noscript>Bật JavaScript</noscript></body></html>"));

        // Affiliate: a hop to it from an allowed page.
        Page(AffiliateHopLink, _ => FixtureAnswer.Redirect(302, "https://affiliate.shopee.vn/mc18-uu-dai"));

        // A hop from an allowed host to each disallowed kind.
        for (var i = 0; i < Disallowed.Count; i++)
        {
            var target = Disallowed[i].Link;
            Page($"https://shopee.vn/chuyen-ra-ngoai-{i + 1}", _ => FixtureAnswer.Redirect(302, target));
        }

        // A product whose declared images include one on a host outside the image hosts, and one that is not an image at all.
        Page(ForeignImageLink, _ => FixtureAnswer.Html(ProductPage("Cốc sứ mẫu 350 ml", "Trắng", "79.000₫", [("Dung tích", "350 ml"), ("Chất liệu", "Sứ")],
            "Cốc sứ trắng.", [$"https://{ForeignImageHost}/coc-1.png", $"https://{ImageHost}/file/mc18-coc-2", $"https://{ImageHost}/file/mc18-khong-phai-anh"], others: false)));
        var cup = Png(40, 40, 21);
        Page($"https://{ImageHost}/file/mc18-coc-2", _ => FixtureAnswer.Png(cup));
        Page($"https://{ImageHost}/file/mc18-khong-phai-anh", _ => FixtureAnswer.Html("<html><body>không phải ảnh</body></html>"));

        // A second product, read only by a second click.
        var box = Png(40, 40, 31);
        Page(SecondLink, _ => FixtureAnswer.Html(ProductPage("Hộp cơm mẫu 3 ngăn", "Xám, 3 ngăn", "125.000₫", [("Số ngăn", "3"), ("Chất liệu", "Nhựa PP")],
            "Hộp cơm ba ngăn có nắp.", [$"https://{ImageHost}/file/mc18-hop-1"], others: false)));
        Page($"https://{ImageHost}/file/mc18-hop-1", _ => FixtureAnswer.Png(box));

        // The SC-10 pages.
        foreach (var (_, link, value) in BannedPages)
        {
            Page(link, _ => FixtureAnswer.Html(ProductPage(Name, Classification, Price, [("Dung tích", "500 ml"), ("Đặc điểm", value)], "Bình mẫu.", [], others: false)));
        }

        // Review correction CR-001: image-phase stops.
        foreach (var (_, link, image, _) in ImageStops)
        {
            Page(link, _ => FixtureAnswer.Html(ProductPage(Name, Classification, Price, Specs, Description,
                [OwnImages[0], image, OwnImages[2]], others: false)));
        }

        const string captchaPage = "<!DOCTYPE html><html lang=\"vi\"><head><title>Xác minh</title></head><body><h1>Xác minh để tiếp tục</h1><p>Kéo thanh trượt để ghép hình.</p></body></html>";
        Page($"https://{ImageHost}/file/mc18-dung-403", _ => FixtureAnswer.Plain(403));
        Page($"https://{ImageHost}/file/mc18-dung-429", _ => FixtureAnswer.Plain(429));
        Page($"https://{ImageHost}/file/mc18-dung-503", _ => FixtureAnswer.Plain(503));
        Page($"https://{ImageHost}/file/mc18-dung-410", _ => FixtureAnswer.Plain(410));
        Page($"https://{ImageHost}/file/mc18-dung-chuyen", _ => FixtureAnswer.Redirect(302, OwnImages[1]));
        Page($"https://{ImageHost}/file/mc18-dung-dang-nhap", _ => FixtureAnswer.Redirect(302, "https://shopee.vn/buyer/login?next=%2Fanh"));
        Page($"https://{ImageHost}/file/mc18-dung-xac-minh", _ => FixtureAnswer.Redirect(302, "https://shopee.vn/verify/captcha?anti_bot_tracking_id=mc18"));
        Page($"https://{ImageHost}/file/mc18-dung-captcha", _ => FixtureAnswer.Html(captchaPage));
        Page($"https://{ImageHost}/file/mc18-dung-tuong", _ => FixtureAnswer.Html(SignInPage()));
        Page($"https://{ImageHost}/file/mc18-dung-khong-anh", _ => new FixtureAnswer(200, "application/json", Encoding.UTF8.GetBytes("{\"error\":90309999}"), []));
        Page($"https://{ImageHost}/file/mc18-dung-qua-lon", _ => new FixtureAnswer(200, "image/jpeg", Oversize(40L << 20, [0xFF, 0xD8, 0xFF, 0xE0]), []));
        Page($"https://{ImageHost}/file/mc18-dung-qua-lon-dai", _ => new FixtureAnswer(200, "image/jpeg", Oversize(40L << 20, [0xFF, 0xD8, 0xFF, 0xE0]), []) { NoLength = true });

        // Review correction CR-004: page-phase bounds and stops, and the slow page that passes the click bound.
        Page("https://shopee.vn/trang-qua-lon", _ => new FixtureAnswer(200, "text/html; charset=utf-8", Oversize(5L << 20, Encoding.ASCII.GetBytes("<!DOCTYPE html><html><body>")), []));
        Page("https://shopee.vn/trang-qua-lon-dai", _ => new FixtureAnswer(200, "text/html; charset=utf-8", Oversize(5L << 20, Encoding.ASCII.GetBytes("<!DOCTYPE html><html><body>")), []) { NoLength = true });
        Page(ManyImagesLink, _ => FixtureAnswer.Html(ProductPage(Name, Classification, Price, Specs, Description, ManyImages, others: false)));
        for (var i = 1; i <= ManyImages.Count; i++)
        {
            var png = Png(16, 16, 40 + i);
            Page(ManyImages[i - 1], _ => FixtureAnswer.Png(png));
        }

        Page("https://shopee.vn/khong-phai-trang", _ => new FixtureAnswer(200, "application/json", Encoding.UTF8.GetBytes("{\"name\":\"Bình\"}"), []));
        Page("https://shopee.vn/khong-con-404", _ => FixtureAnswer.Plain(404));
        Page("https://shopee.vn/khong-noi-dung-204", _ => new FixtureAnswer(204, null, [], []));
        Page("https://shopee.vn/chuyen-khong-dia-chi", _ => new FixtureAnswer(302, null, [], []));
        var slowImages = Enumerable.Range(1, 5).Select(i => $"https://{ImageHost}/file/mc18-cham-{i}").ToList();
        Page(SlowLink, _ => FixtureAnswer.Html(ProductPage(Name, Classification, Price, Specs, Description, slowImages, others: false)));
        for (var i = 1; i <= 5; i++)
        {
            var png = OwnPng(i);
            Page(slowImages[i - 1], _ => FixtureAnswer.Png(png) with { Delay = SlowImageDelay });
        }

        // Review correction CR-003: a hop from an allowed page to each host and hop edge.
        foreach (var (_, link, target, _) in HopEdges)
        {
            Page(link, _ => FixtureAnswer.Redirect(302, target));
        }

        return site;
    }

    /// <summary>A body one byte over the limit, starting with the given bytes.</summary>
    private static byte[] Oversize(long limit, byte[] start)
    {
        var body = new byte[limit + 1];
        start.CopyTo(body, 0);
        return body;
    }

    /// <summary>The bytes of own image number i of the good product, as the fixture serves them.</summary>
    public static byte[] OwnPng(int i) => Png(400, 400, i);

    public static string GoodPage() => ProductPage(Name, Classification, Price, Specs, Description, OwnImages, others: true);

    /// <summary>The visible text of the good page, as the browser would let the operator copy it: the paste fixture.</summary>
    public static string GoodPasteText() => PageReader.VisibleText(GoodPage());

    private static string SignInPage() =>
        "<!DOCTYPE html><html lang=\"vi\"><head><title>Đăng nhập | Shopee Việt Nam</title></head><body><h1>Vui lòng đăng nhập để xem sản phẩm</h1>"
        + "<form method=\"post\" action=\"/buyer/login\"><input name=\"user\"><input type=\"password\" name=\"pw\"><button>Đăng nhập</button></form></body></html>";

    public static string ProductPage(string name, string classification, string price, IReadOnlyList<(string Attribute, string Value)> specs, string description,
        IReadOnlyList<string> images, bool others)
    {
        var product = new JsonObject
        {
            ["@context"] = "https://schema.org",
            ["@type"] = "Product",
            ["name"] = name,
            ["description"] = description,
            ["image"] = new JsonArray([.. images.Select(i => (JsonNode)JsonValue.Create(i)!)]),
            ["offers"] = new JsonObject { ["@type"] = "Offer", ["price"] = "189000", ["priceCurrency"] = "VND" },
        };
        var html = new StringBuilder();
        html.Append("<!DOCTYPE html><html lang=\"vi\"><head><meta charset=\"utf-8\">");
        html.Append($"<title>{WebUtility.HtmlEncode(name)} | Shopee Việt Nam</title>");
        html.Append("<meta property=\"og:type\" content=\"product\">");
        html.Append($"<meta property=\"og:title\" content=\"{WebUtility.HtmlEncode(name)}\">");
        if (images.Count > 0)
        {
            html.Append($"<meta property=\"og:image\" content=\"{WebUtility.HtmlEncode(images[0])}\">");
        }

        html.Append("<script type=\"application/ld+json\">").Append(product.ToJsonString()).Append("</script>");
        html.Append("<link rel=\"stylesheet\" href=\"https://shopee.vn/mc18-kieu.css\"><script src=\"https://shopee.vn/mc18-ma.js\"></script>");
        html.Append("<link rel=\"preload\" as=\"font\" href=\"https://shopee.vn/mc18-chu.woff2\"></head><body>");
        html.Append("<nav><a href=\"https://shopee.vn/\">Trang chủ</a> &gt; <a href=\"https://shopee.vn/binh-giu-nhiet-cat.11036.1\">Bình giữ nhiệt</a></nav>");
        html.Append($"<div class=\"product\"><h1>{WebUtility.HtmlEncode(name)}</h1>");
        html.Append("<div>4.9 | 1,2k Đánh giá | 3,4k Đã bán</div>");
        html.Append($"<div>{WebUtility.HtmlEncode(price)}</div>");
        html.Append($"<div>Phân loại</div><div>{WebUtility.HtmlEncode(classification)}</div>");
        html.Append("<div><a href=\"https://shopee.vn/shop/100\">Xem shop</a></div></div>");
        html.Append("<section><h2>CHI TIẾT SẢN PHẨM</h2><table>");
        foreach (var (attribute, value) in specs)
        {
            html.Append($"<tr><td>{WebUtility.HtmlEncode(attribute)}</td><td>{WebUtility.HtmlEncode(value)}</td></tr>");
        }

        html.Append("</table></section><section><h2>MÔ TẢ SẢN PHẨM</h2>");
        foreach (var line in description.Split('\n'))
        {
            html.Append($"<p>{WebUtility.HtmlEncode(line)}</p>");
        }

        html.Append("</section>");
        if (others)
        {
            html.Append("<section><h2>SẢN PHẨM TƯƠNG TỰ</h2>");
            for (var i = 1; i <= 3; i++)
            {
                html.Append($"<a href=\"https://shopee.vn/binh-khac-i.100.30{i}\"><img src=\"{OtherImages[i - 1]}\"><div>Bình khác {i}</div></a>");
            }

            html.Append("<a href=\"https://shopee.vn/search?keyword=binh+giu+nhiet\">Tìm thêm</a></section>");
        }

        html.Append("</body></html>");
        return html.ToString();
    }

    /// <summary>A small PNG drawn in code: a pattern seeded by the number, so every image has its own bytes.</summary>
    public static byte[] Png(int width, int height, int seed)
    {
        var raw = new MemoryStream();
        for (var y = 0; y < height; y++)
        {
            raw.WriteByte(0);
            for (var x = 0; x < width; x++)
            {
                raw.WriteByte((byte)(seed * 37 + x * 5));
                raw.WriteByte((byte)(seed * 91 + y * 3));
                raw.WriteByte((byte)(seed * 53 + (x ^ y)));
            }
        }

        var packed = new MemoryStream();
        using (var zlib = new ZLibStream(packed, CompressionLevel.Optimal, leaveOpen: true))
        {
            raw.Position = 0;
            raw.CopyTo(zlib);
        }

        var png = new MemoryStream();
        png.Write([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
        var header = new byte[13];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header, width);
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height);
        header[8] = 8;
        header[9] = 2;
        Chunk(png, "IHDR", header);
        Chunk(png, "IDAT", packed.ToArray());
        Chunk(png, "IEND", []);
        return png.ToArray();
    }

    private static void Chunk(Stream png, string type, byte[] data)
    {
        var length = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteInt32BigEndian(length, data.Length);
        png.Write(length);
        var typed = Encoding.ASCII.GetBytes(type).Concat(data).ToArray();
        png.Write(typed);
        var crc = new byte[4];
        System.Buffers.Binary.BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(typed));
        png.Write(crc);
    }

    private static uint Crc32(byte[] data)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in data)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++)
            {
                crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
            }
        }

        return ~crc;
    }
}
