using System.Text.RegularExpressions;

namespace MediaCompany.Pilot.Web;

/// <summary>
/// One stop of a product read (MC-18 design decision D-010): a stable English code for the log and the tests, the Vietnamese reason and
/// the next step. Every stop ends the read at once with no further request and no retry, and the page offers paste mode.
/// </summary>
internal sealed class ReadStop(string code, string reason, string next) : Exception(reason)
{
    public string Code { get; } = code;

    public string Reason { get; } = reason;

    public string Next { get; } = next;

    public OutcomeView View() => new(Outcomes.Refused, null, Code, $"DỪNG ĐỌC [{Code}]: {Reason}\n", Reason, Next);
}

/// <summary>
/// THE LINK AND HOP RULE (MC-18 design decisions D-006 and D-015). Checked on the pasted link and on every redirect hop before any
/// connection is opened:
/// <list type="bullet">
/// <item>The field holds exactly one link; a link with no scheme or with http is taken as https before any connection. A hop that
/// leads to http stops.</item>
/// <item>No user name or password before the host, no explicit port, no IP address.</item>
/// <item>The host is compared lower-case, in its ASCII (punycode) form, without a trailing dot. Pages: shopee.vn and names ending
/// .shopee.vn, except affiliate.shopee.vn and every name under it. Images (D-005): the same names plus names ending
/// .susercontent.com.</item>
/// <item>At most five hops; a hop to a sign-in path or a verification (CAPTCHA) path stops.</item>
/// </list>
/// </summary>
internal static partial class LinkRule
{
    public const int MaxHops = 5;
    public const string PageDomain = "shopee.vn";
    public const string AffiliateHost = "affiliate.shopee.vn";
    public const string ImageDomainSuffix = ".susercontent.com";

    /// <summary>Paths that ask the visitor to sign in; a hop to one of them is a stop, never followed.</summary>
    public static readonly IReadOnlyList<string> SignInPaths = ["/buyer/login", "/buyer/signup", "/user/login", "/login", "/account/login", "/signin"];

    /// <summary>Paths of Shopee's visitor verification (CAPTCHA) pages; a hop to one of them is a stop.</summary>
    public static readonly IReadOnlyList<string> VerifyPaths = ["/verify/"];

    public const string PasteNext =
        "Mở trang sản phẩm trong trình duyệt của bạn, chọn hết chữ trên trang, chép, rồi dán vào ô \"Dán chữ của trang sản phẩm\" và bấm \"Điền từ chữ đã dán\"; hoặc gõ tay các ô. Ứng dụng không thử đọc lại.";

    /// <summary>The one link of the operator's field, as the https address the read starts from; refused with a stop otherwise.</summary>
    public static Uri Admit(string? raw)
    {
        var text = VietnameseText.Nfc((raw ?? string.Empty).Trim());
        if (text.Length == 0)
        {
            throw new ReadStop("link-empty", "Ô đường dẫn đang trống.", "Dán đường dẫn trang sản phẩm shopee.vn vào ô rồi bấm \"Đọc trang\".");
        }

        var links = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Where(LooksLikeLink).ToList();
        if (links.Count > 1 || links.Any(l => SchemeMark().Count(l) > 1))
        {
            throw new ReadStop("link-two-links",
                "Ô đường dẫn có hơn một đường dẫn. Mỗi lần bấm chỉ đọc một sản phẩm.",
                "Để lại đúng một đường dẫn trong ô rồi bấm lại; sản phẩm khác đọc ở lần bấm sau.");
        }

        if (links.Count == 0)
        {
            throw Unreadable();
        }

        var link = links[0];
        if (!link.Contains("://", StringComparison.Ordinal))
        {
            link = "https://" + link;
        }
        else if (link.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
        {
            link = "https://" + link["http://".Length..];
        }

        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
        {
            throw Unreadable();
        }

        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new ReadStop("link-not-allowed", $"Đường dẫn không phải địa chỉ trang web ({uri.Scheme}). Ứng dụng chỉ đọc trang sản phẩm trên shopee.vn.", PasteNext);
        }

        CheckPage(uri, hop: false);
        return WithoutFragment(uri);
    }

    /// <summary>
    /// The next address of a redirect answer, checked before any connection to it: a missing address, more than five hops, http, a
    /// host outside the rule, a sign-in path or a verification path each stop the read.
    /// </summary>
    public static Uri Hop(Uri current, string? location, int hopNumber)
    {
        ArgumentNullException.ThrowIfNull(current);
        if (hopNumber > MaxHops)
        {
            throw new ReadStop("too-many-hops", $"Đường dẫn chuyển tiếp quá {MaxHops} lần mà chưa tới trang sản phẩm.",
                "Mở đường dẫn trong trình duyệt của bạn, chép địa chỉ trang sản phẩm trên thanh địa chỉ rồi dán vào ô; hoặc dán chữ trang. " + PasteNext);
        }

        if (string.IsNullOrWhiteSpace(location) || !Uri.TryCreate(current, location.Trim(), out var next))
        {
            throw new ReadStop("status-unexpected", "Trang trả lời chuyển tiếp nhưng không ghi địa chỉ đến.", PasteNext);
        }

        if (next.Scheme == Uri.UriSchemeHttp)
        {
            throw new ReadStop("hop-not-secure", "Trang chuyển tiếp sang một địa chỉ không mã hoá (http). Ứng dụng không đi theo.", PasteNext);
        }

        if (next.Scheme != Uri.UriSchemeHttps)
        {
            throw new ReadStop("hop-not-allowed", $"Trang chuyển tiếp sang một địa chỉ không phải trang web ({next.Scheme}). Ứng dụng không đi theo.", PasteNext);
        }

        CheckPage(next, hop: true);
        return WithoutFragment(next);
    }

    /// <summary>The host in the form the rule compares: lower-case ASCII (punycode), no trailing dot.</summary>
    public static string HostOf(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.IdnHost.ToLowerInvariant().TrimEnd('.');
    }

    public static bool IsPageHost(string host) =>
        (host == PageDomain || host.EndsWith("." + PageDomain, StringComparison.Ordinal)) && !IsAffiliate(host);

    public static bool IsAffiliate(string host) => host == AffiliateHost || host.EndsWith("." + AffiliateHost, StringComparison.Ordinal);

    public static bool IsImageHost(string host) =>
        IsPageHost(host) || (host.EndsWith(ImageDomainSuffix, StringComparison.Ordinal) && host.Length > ImageDomainSuffix.Length);

    /// <summary>
    /// An image address the page declares (D-005): resolved against the page, https, no user name, port or IP address, on an image host.
    /// Returns the address, or null with the Vietnamese reason it is not fetched.
    /// </summary>
    public static (Uri? Address, string? Reason) Image(Uri page, string raw)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (!Uri.TryCreate(page, (raw ?? string.Empty).Trim(), out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            return (null, "địa chỉ ảnh không phải https");
        }

        if (uri.UserInfo.Length > 0 || ExplicitPort(uri) || uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            return (null, "địa chỉ ảnh có tên đăng nhập, cổng riêng hoặc địa chỉ số");
        }

        var host = HostOf(uri);
        return IsImageHost(host) ? (WithoutFragment(uri), null) : (null, $"ảnh nằm ở {host}, ngoài các máy chủ ảnh được phép");
    }

    /// <summary>The address without query or fragment: the page address the fact sheet records (D-008).</summary>
    public static string PagePath(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.GetLeftPart(UriPartial.Path);
    }

    public static string PathOf(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        return uri.AbsolutePath;
    }

    private static void CheckPage(Uri uri, bool hop)
    {
        var where = hop ? "Trang chuyển tiếp sang" : "Đường dẫn trỏ tới";
        var code = hop ? "hop-not-allowed" : "link-not-allowed";
        if (uri.UserInfo.Length > 0)
        {
            throw new ReadStop(code, $"{where} một địa chỉ có tên đăng nhập hoặc mật khẩu đứng trước tên trang. Ứng dụng không mở địa chỉ như vậy.", PasteNext);
        }

        if (uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6)
        {
            throw new ReadStop(code, $"{where} một địa chỉ số ({uri.Host}) thay cho tên trang. Ứng dụng chỉ đọc trang shopee.vn.", PasteNext);
        }

        if (ExplicitPort(uri))
        {
            throw new ReadStop(code, $"{where} một địa chỉ có số cổng riêng. Ứng dụng chỉ mở địa chỉ shopee.vn thông thường.", PasteNext);
        }

        var host = HostOf(uri);
        if (host.Length == 0)
        {
            throw Unreadable();
        }

        if (IsAffiliate(host))
        {
            throw new ReadStop(hop ? "hop-affiliate" : "link-affiliate",
                $"{where} {host}. Ứng dụng không bao giờ mở trang affiliate.shopee.vn.",
                "Mở sản phẩm trong trình duyệt của bạn, chép địa chỉ trang sản phẩm (bắt đầu bằng shopee.vn) rồi dán vào ô; hoặc dán chữ trang. " + PasteNext);
        }

        if (!IsPageHost(host))
        {
            throw new ReadStop(code, $"{where} {host}, không phải trang shopee.vn. Ứng dụng chỉ đọc trang sản phẩm trên shopee.vn.", PasteNext);
        }

        var path = uri.AbsolutePath.ToLowerInvariant();
        if (SignInPaths.Any(p => path == p || path.StartsWith(p + "/", StringComparison.Ordinal)))
        {
            throw new ReadStop("sign-in-redirect", "Shopee chuyển sang trang đăng nhập. Ứng dụng không đăng nhập, không dùng tài khoản nào.", PasteNext);
        }

        if (VerifyPaths.Any(p => path.StartsWith(p, StringComparison.Ordinal)))
        {
            throw new ReadStop("captcha", "Shopee chuyển sang trang xác minh người dùng (CAPTCHA). Ứng dụng không giải trang xác minh.", PasteNext);
        }
    }

    /// <summary>True when the authority as written carries a port, even the default one (":443").</summary>
    private static bool ExplicitPort(Uri uri)
    {
        if (!uri.IsDefaultPort)
        {
            return true;
        }

        var text = uri.OriginalString;
        var start = text.IndexOf("://", StringComparison.Ordinal);
        if (start < 0)
        {
            return false;
        }

        var authority = text[(start + 3)..];
        var end = authority.IndexOfAny(['/', '?', '#', '\\']);
        authority = end < 0 ? authority : authority[..end];
        authority = authority[(authority.LastIndexOf('@') + 1)..];
        return authority.Contains(':', StringComparison.Ordinal) && !authority.StartsWith('[');
    }

    private static Uri WithoutFragment(Uri uri) => new(uri.GetComponents(UriComponents.HttpRequestUrl, UriFormat.UriEscaped));

    private static bool LooksLikeLink(string token) =>
        token.Contains("://", StringComparison.Ordinal) || HostLike().IsMatch(token);

    private static ReadStop Unreadable() =>
        new("link-unreadable", "Không hiểu được đường dẫn đã dán.", "Chép lại đường dẫn trang sản phẩm shopee.vn (cả phần đầu) rồi dán vào ô. " + PasteNext);

    [GeneratedRegex("://", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex SchemeMark();

    [GeneratedRegex(@"^([\p{L}\p{N}-]+(\.[\p{L}\p{N}-]+)+\.?|\[[0-9A-Fa-f:.]+\])(:\d+)?([/?#\\].*)?$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex HostLike();
}
