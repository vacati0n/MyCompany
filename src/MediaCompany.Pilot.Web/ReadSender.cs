using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// THE TEST TRANSPORT SEAM (MC-18 design decision D-004). Where a connection for a host goes, and which certificate is trusted for it.
/// It is internal to this assembly and is passed only through the internal start path (<see cref="Program.Start"/>), which only the
/// test assembly can see. The shipped entry point never passes it, and no argument, environment variable, file or route can set it, so
/// the shipped read always resolves the real name, refuses addresses that are not public, and trusts only the system's certificates.
/// </summary>
internal sealed record ReadTransport(Func<string, int, IPEndPoint> ConnectTo, Func<X509Certificate2, bool> Trust);

/// <summary>One answer of the read: status, the redirect address, the media type, the body (empty for a redirect) and its source address.</summary>
internal sealed record ReadAnswer(int Status, string? Location, string? MediaType, byte[] Body);

/// <summary>The connect step found a name that resolves to this computer or a private network (D-006).</summary>
internal sealed class NotPublicAddressException(string host, IPAddress address) : Exception($"{host} -> {address}")
{
    public string Host { get; } = host;

    public IPAddress Address { get; } = address;
}

/// <summary>
/// THE READ SENDER (MC-18 design decisions D-001, D-002 and D-014). One fresh HTTP stack per click, disposed when the read ends. The
/// request names the tool honestly and carries nothing else: User-Agent "MediaCompanyPilot/1.0 (doc-trang-san-pham)", Accept "text/html"
/// for the page or "image/jpeg, image/png" for an image, and the Host the stack sets. There is no cookie store (a cookie the site sets is
/// dropped, never sent back), no proxy (the system or environment proxy is ignored), no automatic redirect (each hop is checked by the
/// link rule before it is requested), no decompression (so no Accept-Encoding) and no trace header. Bounds: 30 s per request, the click's
/// own 120 s, 5 MiB per page and 40 MiB per image. Every request is written to the read log. Nothing here retries.
/// </summary>
internal sealed class ReadSender : IDisposable
{
    public const string UserAgent = "MediaCompanyPilot/1.0 (doc-trang-san-pham)";
    public const string PageAccept = "text/html";
    public const string ImageAccept = "image/jpeg, image/png";
    public const long PageLimit = 5L << 20;
    public const long ImageLimit = 40L << 20;
    public static readonly TimeSpan RequestBound = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ClickBound = TimeSpan.FromSeconds(120);

    private readonly HttpClient _client;
    private readonly ReadLog _log;
    private readonly CancellationToken _click;
    private readonly HashSet<string> _localAddresses = [];

    public ReadSender(ReadTransport? transport, ReadLog log, CancellationToken click)
    {
        _log = log;
        _click = click;
        var handler = new SocketsHttpHandler
        {
            UseCookies = false,
            UseProxy = false,
            Proxy = null,
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.None,
            ActivityHeadersPropagator = null,
            ConnectTimeout = RequestBound,
            MaxConnectionsPerServer = 1,
            PooledConnectionLifetime = ClickBound,
            ConnectCallback = (context, token) => Connect(transport, context.DnsEndPoint, token),
        };
        if (transport is not null)
        {
            handler.SslOptions = new SslClientAuthenticationOptions
            {
                RemoteCertificateValidationCallback = (_, certificate, _, _) => certificate is X509Certificate2 c && transport.Trust(c),
            };
        }

        _client = new HttpClient(handler, disposeHandler: true) { Timeout = Timeout.InfiniteTimeSpan };
    }

    /// <summary>How many requests this click has sent.</summary>
    public int Requests { get; private set; }

    /// <summary>The distinct local addresses the click's connections left from (one per read; no address pool exists).</summary>
    public IReadOnlyCollection<string> LocalAddresses
    {
        get
        {
            lock (_localAddresses)
            {
                return [.. _localAddresses];
            }
        }
    }

    /// <summary>One GET of a page or an image; a stop is thrown for a connection, time or size failure, never retried.</summary>
    public async Task<ReadAnswer> Get(Uri address, bool image)
    {
        var host = LinkRule.HostOf(address);
        var path = LinkRule.PathOf(address);
        using var request = new HttpRequestMessage(HttpMethod.Get, address) { Version = HttpVersion.Version11, VersionPolicy = HttpVersionPolicy.RequestVersionExact };
        request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        request.Headers.TryAddWithoutValidation("Accept", image ? ImageAccept : PageAccept);
        using var bound = CancellationTokenSource.CreateLinkedTokenSource(_click);
        bound.CancelAfter(RequestBound);
        Requests++;
        var status = 0;
        long bytes = 0;
        try
        {
            using var response = await _client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, bound.Token);
            status = (int)response.StatusCode;
            var location = response.Headers.Location?.OriginalString;
            var media = response.Content.Headers.ContentType?.MediaType;
            if (status != 200)
            {
                _log.Write(host, path, status, 0, null);
                return new ReadAnswer(status, location, media, []);
            }

            var limit = image ? ImageLimit : PageLimit;
            if (response.Content.Headers.ContentLength is { } length && length > limit)
            {
                throw TooLarge(image);
            }

            await using var stream = await response.Content.ReadAsStreamAsync(bound.Token);
            using var memory = new MemoryStream();
            var buffer = new byte[81920];
            int read;
            while ((read = await stream.ReadAsync(buffer, bound.Token)) > 0)
            {
                if (memory.Length + read > limit)
                {
                    throw TooLarge(image);
                }

                memory.Write(buffer, 0, read);
            }

            bytes = memory.Length;
            _log.Write(host, path, status, bytes, null);
            return new ReadAnswer(status, location, media, memory.ToArray());
        }
        catch (ReadStop stop)
        {
            _log.Write(host, path, status, bytes, stop.Code);
            throw;
        }
        catch (OperationCanceledException) when (_click.IsCancellationRequested)
        {
            var stop = new ReadStop("read-time-over", $"Lần đọc này đã quá {ClickBound.TotalSeconds:0} giây nên dừng lại.", LinkRule.PasteNext);
            _log.Write(host, path, status, bytes, stop.Code);
            throw stop;
        }
        catch (OperationCanceledException)
        {
            var stop = new ReadStop("no-answer", $"{host} không trả lời trong {RequestBound.TotalSeconds:0} giây.", LinkRule.PasteNext);
            _log.Write(host, path, status, bytes, stop.Code);
            throw stop;
        }
        catch (Exception error) when (error is HttpRequestException or IOException or AuthenticationException or SocketException)
        {
            var stop = Find<NotPublicAddressException>(error) is { } local
                ? new ReadStop("address-not-public",
                    $"Tên {local.Host} trỏ về một địa chỉ trong máy này hoặc mạng nội bộ ({local.Address}), không phải trang công khai. Ứng dụng không kết nối.",
                    LinkRule.PasteNext)
                : new ReadStop("no-connection",
                    $"Không kết nối được tới {host} (không có mạng, không tìm thấy tên trang, hoặc kết nối an toàn không hợp lệ).",
                    "Kiểm tra mạng của máy. " + LinkRule.PasteNext);
            _log.Write(host, path, status, bytes, stop.Code);
            throw stop;
        }
    }

    public void Dispose() => _client.Dispose();

    /// <summary>
    /// The connect step. Shipped: resolve the name, refuse the whole name when any address is loopback, private, link-local,
    /// unspecified or multicast, then connect to its addresses in order. With the test seam: connect where the seam says.
    /// </summary>
    private async ValueTask<Stream> Connect(ReadTransport? transport, DnsEndPoint target, CancellationToken token)
    {
        IReadOnlyList<IPEndPoint> endpoints;
        if (transport is null)
        {
            var addresses = await Dns.GetHostAddressesAsync(target.Host, token);
            if (addresses.Length == 0)
            {
                throw new SocketException((int)SocketError.HostNotFound);
            }

            if (addresses.FirstOrDefault(a => !IsPublic(a)) is { } local)
            {
                throw new NotPublicAddressException(target.Host, local);
            }

            endpoints = addresses.Select(a => new IPEndPoint(a, target.Port)).ToList();
        }
        else
        {
            endpoints = [transport.ConnectTo(target.Host, target.Port)];
        }

        SocketException? last = null;
        foreach (var endpoint in endpoints)
        {
            var socket = new Socket(endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
            try
            {
                await socket.ConnectAsync(endpoint, token);
                if (socket.LocalEndPoint is IPEndPoint mine)
                {
                    lock (_localAddresses)
                    {
                        _localAddresses.Add(mine.Address.ToString());
                    }
                }

                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException error)
            {
                socket.Dispose();
                last = error;
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        throw last ?? new SocketException((int)SocketError.HostNotFound);
    }

    /// <summary>A public address: not loopback, private, shared, link-local, unspecified, multicast, broadcast or reserved.</summary>
    public static bool IsPublic(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var b = address.GetAddressBytes();
            return !(b[0] == 0 || b[0] == 10 || b[0] == 127 || b[0] >= 224
                || (b[0] == 100 && b[1] >= 64 && b[1] <= 127)
                || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31)
                || (b[0] == 192 && b[1] == 168)
                || (b[0] == 192 && b[1] == 0 && b[2] == 0));
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var b = address.GetAddressBytes();
            return !(address.Equals(IPAddress.IPv6Any) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast
                || (b[0] & 0xFE) == 0xFC);
        }

        return false;
    }

    private static ReadStop TooLarge(bool image) => image
        ? new ReadStop("image-too-large", $"Một ảnh lớn hơn {ImageLimit >> 20} MiB.", "Ảnh đó được ghi tên để bạn tải lên ở bước 2.")
        : new ReadStop("page-too-large", $"Trang trả về lớn hơn {PageLimit >> 20} MiB, quá mức ứng dụng đọc.", LinkRule.PasteNext);

    private static T? Find<T>(Exception? error)
        where T : Exception
    {
        for (var e = error; e is not null; e = e.InnerException)
        {
            if (e is T found)
            {
                return found;
            }
        }

        return null;
    }
}

/// <summary>
/// THE READ LOG (MC-18 design decision D-011), beside the app's route log in the web folder: one line per outbound request with the
/// time, host, path without query, status, byte count and stop code. Never a header, a cookie or page text.
/// </summary>
internal sealed class ReadLog(string folder)
{
    public const string FileName = "doc-trang.log";
    private static readonly object Gate = new();

    public string FilePath => Path.Combine(folder, FileName);

    public void Write(string host, string path, int status, long bytes, string? code)
    {
        var line = string.Create(CultureInfo.InvariantCulture,
            $"{DateTimeOffset.UtcNow:yyyy-MM-ddTHH:mm:ss.fffZ}\t{host}\t{path}\t{status}\t{bytes}\t{code ?? "-"}\n");
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(folder);
                File.AppendAllText(FilePath, line, new UTF8Encoding(false));
            }
            catch (IOException)
            {
                // A log line that cannot be written does not stop the operator's work.
            }
            catch (UnauthorizedAccessException)
            {
                // As above.
            }
        }
    }
}
