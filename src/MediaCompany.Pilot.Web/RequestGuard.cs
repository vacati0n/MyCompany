using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace MediaCompany.Pilot.Web;

/// <summary>The listener's own address and the per-start token, held in memory only and never logged or stored.</summary>
public sealed class GuardState
{
    public const string TokenHeader = "X-MC-Token";

    public GuardState()
    {
        Token = Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(32));
    }

    public string Token { get; }

    public int Port { get; set; }

    public string Host => $"127.0.0.1:{Port}";

    public string Origin => $"http://{Host}";
}

/// <summary>
/// THE REQUEST GUARD (design decision D-003, Design Gate option O-009). The listener is bound to 127.0.0.1 in code and the bound
/// addresses are checked at start-up (see Program). On every request: the peer must be the loopback address and the Host header must be
/// exactly 127.0.0.1 and the app's port (a foreign host name that resolves to 127.0.0.1 is refused). On every state-changing request
/// (every POST): the Origin header must be exactly the app's own origin, and the per-start token must be in its header. GET and HEAD
/// change nothing; every other method is refused. No cross-origin policy is granted.
/// </summary>
public static class RequestGuard
{
    public static (int Status, string Code, string Message)? Check(string method, string? host, string? origin, string? token, IPAddress? peer, GuardState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        if (state.Port == 0)
        {
            return (503, "not-ready", "Ứng dụng đang khởi động. Chờ vài giây rồi tải lại trang.");
        }

        if (peer is null || !IPAddress.IsLoopback(peer))
        {
            return (403, "peer-not-local", "Yêu cầu không đến từ chính máy này. Ứng dụng chỉ phục vụ máy này.");
        }

        if (!string.Equals(host, state.Host, StringComparison.Ordinal))
        {
            return (403, "host-not-allowed",
                $"Yêu cầu gửi tới tên máy \"{host}\", không phải địa chỉ của ứng dụng ({state.Host}). Mở ứng dụng bằng biểu tượng trên màn hình nền.");
        }

        if (method is "GET" or "HEAD")
        {
            return null;
        }

        if (method != "POST")
        {
            return (405, "method-not-allowed", "Ứng dụng không nhận kiểu yêu cầu này. Dùng các nút trên trang.");
        }

        if (!string.Equals(origin, state.Origin, StringComparison.Ordinal))
        {
            return (403, "origin-not-allowed",
                "Yêu cầu thay đổi dữ liệu không đến từ trang của ứng dụng nên bị từ chối. Chỉ thao tác trên trang mở từ biểu tượng của ứng dụng.");
        }

        if (token is null || !CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(token), Encoding.ASCII.GetBytes(state.Token)))
        {
            return (403, "token-not-valid",
                "Trang này đã cũ (ứng dụng vừa khởi động lại) nên yêu cầu bị từ chối. Tải lại trang (phím F5) rồi làm lại.");
        }

        return null;
    }

    /// <summary>The response headers of every answer: own-origin content only, no framing, no referrer, microphone for this origin only.</summary>
    public static void SecurityHeaders(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);
        headers["Content-Security-Policy"] =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' blob:; media-src 'self' blob:; connect-src 'self'; "
            + "font-src 'self'; object-src 'none'; base-uri 'none'; form-action 'self'; frame-ancestors 'none'";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "microphone=(self), camera=(), geolocation=()";
        headers["X-Content-Type-Options"] = "nosniff";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Cache-Control"] = "no-store";
    }
}
