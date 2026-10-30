using System.Text;
using System.Text.Json;

namespace MediaCompany.Pilot.Web;

/// <summary>An accepted block (MC-19 D-002): the page address as admitted, the press date, the product facts the button found.</summary>
internal sealed record ButtonBlock(
    Uri Page,
    string PressedAt,
    string ProductName,
    string? Classification,
    string? ShopName,
    bool? Mall,
    IReadOnlyList<SpecRow> Specs,
    string? Price,
    string? Description,
    IReadOnlyList<string> Images);

/// <summary>
/// THE BLOCK READER (MC-19 technical design module M-001; decisions D-002, D-006, D-010, D-014, D-016). A block is the fixed ASCII
/// marker line then one strict JSON object. Checked in this order, each failure refusing the whole block with a Vietnamese reason, the
/// next step and a bracketed code, and filling nothing: the UTF-8 size (256 KiB) before any parsing; the JSON itself (depth 8, one
/// object, no trailing content, no repeated member); the format version (an earlier one, or another button mark, asks for the new
/// button; a later one is unknown); unknown members and wrong types; the bounds (30 image addresses, 100 rows, 4000 characters per
/// text member); the product name; the page address against the page-host rule. The route checks the marker count, the switch and the
/// open video before this reader runs (D-010).
/// </summary>
internal static class BlockReader
{
    public const string Marker = "MC19-NUT-DOC-TRANG";
    public const int FormVersion = 1;
    public const int MaxBytes = 256 * 1024;
    public const int MaxDepth = 8;
    public const int MaxImages = 30;
    public const int MaxRows = 100;
    public const int MaxText = 4000;

    private const string NewButton =
        "Mở mục \"Cài đặt\" của ứng dụng, kéo nút \"Đọc trang Shopee\" mới lên thanh dấu trang (thay nút cũ), rồi bấm lại trên trang sản phẩm.";

    private const string PasteInstead = "Bấm lại nút \"Đọc trang Shopee\" trên trang sản phẩm rồi dán lại; hoặc dán chữ của trang, hoặc gõ tay.";

    private static readonly IReadOnlyList<string> Members =
        ["formVersion", "buttonMark", "pageAddress", "pressedAt", "productName", "classification", "shopName", "mall", "specs", "price", "description", "images"];

    private static readonly IReadOnlyList<string> RowMembers = ["attribute", "value", "section"];

    /// <summary>True when the pasted text, after leading white space, begins with the marker: the block path (D-001).</summary>
    public static bool IsBlock(string? text) =>
        text is not null && text.TrimStart().StartsWith(Marker, StringComparison.Ordinal);

    /// <summary>How many times the marker occurs in the text: more than one is two products in one paste (D-010).</summary>
    public static int Markers(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var count = 0;
        for (var at = text.IndexOf(Marker, StringComparison.Ordinal); at >= 0; at = text.IndexOf(Marker, at + Marker.Length, StringComparison.Ordinal))
        {
            count++;
        }

        return count;
    }

    /// <summary>The block, read strictly; any failure is a <see cref="WebRefusal"/> and nothing has been filled.</summary>
    public static ButtonBlock Read(string text, string? fixtureOrigin)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Encoding.UTF8.GetByteCount(text) > MaxBytes)
        {
            throw Refuse("block-too-large", $"Khối thông tin đã dán lớn hơn {MaxBytes / 1024} KiB, nên ứng dụng không đọc.", PasteInstead);
        }

        var body = text.TrimStart();
        body = body[Marker.Length..];
        if (!body.StartsWith('\n') && !body.StartsWith("\r\n", StringComparison.Ordinal))
        {
            throw Unreadable();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body, new JsonDocumentOptions { MaxDepth = MaxDepth });
        }
        catch (JsonException)
        {
            throw Unreadable();
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw Unreadable();
            }

            NoRepeats(root);
            CheckVersion(root);
            foreach (var member in root.EnumerateObject())
            {
                if (!Members.Contains(member.Name, StringComparer.Ordinal))
                {
                    throw Refuse("block-unknown-member", $"Khối thông tin có mục lạ \"{Short(member.Name)}\" mà nút \"Đọc trang Shopee\" không bao giờ chép.", PasteInstead);
                }
            }

            var page = Required(root, "pageAddress");
            var pressed = Required(root, "pressedAt");
            var name = Optional(root, "productName");
            var classification = Optional(root, "classification");
            var shopName = Optional(root, "shopName");
            bool? mall = null;
            if (root.TryGetProperty("mall", out var mallValue))
            {
                mall = mallValue.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => throw WrongType("mall"),
                };
            }

            var price = Optional(root, "price");
            var description = Optional(root, "description");
            var images = new List<string>();
            if (root.TryGetProperty("images", out var imageList))
            {
                if (imageList.ValueKind != JsonValueKind.Array)
                {
                    throw WrongType("images");
                }

                if (imageList.GetArrayLength() > MaxImages)
                {
                    throw Refuse("block-too-many-images", $"Khối thông tin có {imageList.GetArrayLength()} địa chỉ ảnh, quá {MaxImages}.", PasteInstead);
                }

                foreach (var item in imageList.EnumerateArray())
                {
                    images.Add(item.ValueKind == JsonValueKind.String ? Bounded(item.GetString()!, "images") : throw WrongType("images"));
                }
            }

            var specs = new List<SpecRow>();
            if (root.TryGetProperty("specs", out var rowList))
            {
                if (rowList.ValueKind != JsonValueKind.Array)
                {
                    throw WrongType("specs");
                }

                if (rowList.GetArrayLength() > MaxRows)
                {
                    throw Refuse("block-too-many-rows", $"Khối thông tin có {rowList.GetArrayLength()} dòng thông tin, quá {MaxRows}.", PasteInstead);
                }

                foreach (var row in rowList.EnumerateArray())
                {
                    if (row.ValueKind != JsonValueKind.Object)
                    {
                        throw WrongType("specs");
                    }

                    NoRepeats(row);
                    foreach (var member in row.EnumerateObject())
                    {
                        if (!RowMembers.Contains(member.Name, StringComparer.Ordinal))
                        {
                            throw Refuse("block-unknown-member", $"Một dòng thông tin trong khối có mục lạ \"{Short(member.Name)}\".", PasteInstead);
                        }
                    }

                    var attribute = Required(row, "attribute", "specs");
                    var value = Required(row, "value", "specs");
                    var section = Required(row, "section", "specs");
                    if (attribute.Trim().Length > 0 && value.Trim().Length > 0)
                    {
                        specs.Add(new SpecRow(VietnameseText.Nfc(attribute.Trim()), VietnameseText.Nfc(value.Trim()), VietnameseText.Nfc(section.Trim())));
                    }
                }
            }

            if (name is null || name.Trim().Length == 0)
            {
                throw Refuse("block-no-name", "Khối thông tin không có tên sản phẩm.", PasteInstead);
            }

            var address = PageAddress(page, fixtureOrigin);
            return new ButtonBlock(address, pressed.Trim(), VietnameseText.Nfc(name.Trim()), Clean(classification), Clean(shopName), shopName is null ? null : mall,
                specs, Clean(price), description is null ? null : Clean(description), images);
        }
    }

    /// <summary>
    /// The page-address rule (D-010, D-016): an absolute https address on a page host of the MC-18 link rule (shopee.vn and names
    /// under it, never affiliate.shopee.vn), with no user name, port or IP address; or, through the internal test seam only, an address
    /// whose origin is exactly the fixture origin. Query and fragment are dropped.
    /// </summary>
    public static Uri PageAddress(string raw, string? fixtureOrigin)
    {
        if (!Uri.TryCreate((raw ?? string.Empty).Trim(), UriKind.Absolute, out var uri) || uri.Scheme is not ("https" or "http"))
        {
            throw PageNotAllowed(raw);
        }

        if (fixtureOrigin is not null && string.Equals(uri.GetLeftPart(UriPartial.Authority), fixtureOrigin, StringComparison.Ordinal) && uri.UserInfo.Length == 0)
        {
            return new Uri(uri.GetLeftPart(UriPartial.Path));
        }

        if (uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0 || !uri.IsDefaultPort || HasPort(raw!)
            || uri.HostNameType is UriHostNameType.IPv4 or UriHostNameType.IPv6 || !LinkRule.IsPageHost(LinkRule.HostOf(uri)))
        {
            throw PageNotAllowed(raw);
        }

        return new Uri(uri.GetLeftPart(UriPartial.Path));
    }

    private static bool HasPort(string raw)
    {
        var text = raw.Trim();
        var start = text.IndexOf("://", StringComparison.Ordinal);
        var authority = start < 0 ? text : text[(start + 3)..];
        var end = authority.IndexOfAny(['/', '?', '#', '\\']);
        authority = end < 0 ? authority : authority[..end];
        authority = authority[(authority.LastIndexOf('@') + 1)..];
        return authority.Contains(':', StringComparison.Ordinal);
    }

    private static void CheckVersion(JsonElement root)
    {
        if (!root.TryGetProperty("formVersion", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number))
        {
            throw WrongType("formVersion");
        }

        if (number > FormVersion)
        {
            throw Refuse("block-unknown-version", $"Khối thông tin có dạng số {number}, mới hơn dạng ứng dụng này đọc được ({FormVersion}).",
                "Dùng nút \"Đọc trang Shopee\" lấy từ mục \"Cài đặt\" của chính ứng dụng này; hoặc dán chữ của trang, hoặc gõ tay.");
        }

        var mark = root.TryGetProperty("buttonMark", out var given) && given.ValueKind == JsonValueKind.String ? given.GetString() : null;
        if (number < FormVersion || !string.Equals(mark, ButtonBuilder.Mark, StringComparison.Ordinal))
        {
            throw Refuse("block-old-button", "Khối thông tin được chép bằng một nút \"Đọc trang Shopee\" cũ hoặc khác với nút của ứng dụng này.", NewButton);
        }
    }

    private static void NoRepeats(JsonElement element)
    {
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var member in element.EnumerateObject())
        {
            if (!names.Add(member.Name))
            {
                throw Refuse("block-unreadable", $"Khối thông tin ghi mục \"{Short(member.Name)}\" hai lần.", PasteInstead);
            }
        }
    }

    private static string Required(JsonElement owner, string member, string? where = null)
    {
        if (!owner.TryGetProperty(member, out var value) || value.ValueKind != JsonValueKind.String)
        {
            throw WrongType(where ?? member);
        }

        return Bounded(value.GetString()!, where ?? member);
    }

    private static string? Optional(JsonElement owner, string member)
    {
        if (!owner.TryGetProperty(member, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.String ? Bounded(value.GetString()!, member) : throw WrongType(member);
    }

    private static string Bounded(string text, string member) =>
        text.Length <= MaxText
            ? text
            : throw Refuse("block-text-too-long", $"Mục \"{member}\" của khối thông tin dài {text.Length} ký tự, quá {MaxText}.", PasteInstead);

    private static string? Clean(string? text) =>
        string.IsNullOrWhiteSpace(text) ? null : VietnameseText.Nfc(text.Trim());

    private static string Short(string name) => name.Length <= 40 ? name : name[..40] + "...";

    private static WebRefusal WrongType(string member) =>
        Refuse("block-wrong-type", $"Mục \"{member}\" của khối thông tin thiếu hoặc không đúng loại.", PasteInstead);

    private static WebRefusal Unreadable() =>
        Refuse("block-unreadable", "Khối thông tin đã dán không đọc được (thiếu phần sau dòng đầu, sai dạng, hoặc có chữ thừa ở cuối).", PasteInstead);

    private static WebRefusal PageNotAllowed(string? raw) =>
        Refuse("block-page-not-allowed",
            $"Địa chỉ trang trong khối thông tin ({Short((raw ?? string.Empty).Trim())}) không phải trang shopee.vn. Nút chỉ đọc trang sản phẩm trên shopee.vn.",
            "Mở trang sản phẩm trên shopee.vn, bấm nút \"Đọc trang Shopee\" ở đó rồi dán lại; hoặc gõ tay.");

    private static WebRefusal Refuse(string code, string reason, string next) => new(code, reason + " Ứng dụng không điền ô nào. Việc cần làm: " + next);
}
