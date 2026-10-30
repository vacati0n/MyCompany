using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MediaCompany.Pilot.Web;

/// <summary>One specification the page shows: the label as written, the value as written, and the source section the sheet records.</summary>
internal sealed record SpecRow(string Attribute, string Value, string SourceSection);

/// <summary>What the shared parser found in the visible text of a product page; null where the text does not supply it.</summary>
internal sealed record ProductText(string? Name, string? Classification, IReadOnlyList<SpecRow> Specs, string? Price, string? Description);

/// <summary>
/// THE SHARED TEXT PARSER (MC-18 module M-003; plan step P-004). The read path gives it the visible text of the fetched page and the
/// paste path gives it the text the operator copied from the page, so both fill the same fields by construction. It anchors on the
/// Vietnamese section labels the page shows:
/// <list type="bullet">
/// <item>Product name: a line labelled "Tên sản phẩm", else the last line of words above the first price line (rating and sales lines
/// skipped).</item>
/// <item>Price (shown only, never saved): the first line holding a number and the dong sign.</item>
/// <item>Classification: the text after "Phân loại", or the line below it.</item>
/// <item>Specifications: the pairs between "CHI TIẾT SẢN PHẨM" and "MÔ TẢ SẢN PHẨM"; a pair is "label[tab]value", "label: value", or a
/// label line followed by its value line. Each becomes a candidate row with source section "Chi tiết sản phẩm: [label]".</item>
/// <item>Description (shown only, never saved): the lines after "MÔ TẢ SẢN PHẨM" up to the next section label.</item>
/// </list>
/// Every value is NFC-normalised.
/// </summary>
internal static partial class TextParser
{
    public const string DetailLabel = "CHI TIẾT SẢN PHẨM";
    public const string DescriptionLabel = "MÔ TẢ SẢN PHẨM";
    public const string ClassificationLabel = "Phân loại";
    public const string NameLabel = "Tên sản phẩm";
    public const string SourcePrefix = "Chi tiết sản phẩm: ";
    public const int MaxDescription = 4000;

    /// <summary>Section labels that end the specification or description block.</summary>
    public static readonly IReadOnlyList<string> EndLabels =
        ["ĐÁNH GIÁ SẢN PHẨM", "SẢN PHẨM TƯƠNG TỰ", "CÁC SẢN PHẨM KHÁC CỦA SHOP", "CÓ THỂ BẠN CŨNG THÍCH", "GỢI Ý HÔM NAY", "SẢN PHẨM LIÊN QUAN"];

    public static ProductText Parse(string text)
    {
        var lines = Lines(text);
        var detail = IndexOfLabel(lines, DetailLabel, 0);
        var description = IndexOfLabel(lines, DescriptionLabel, detail < 0 ? 0 : detail);
        var top = detail >= 0 ? detail : description >= 0 ? description : lines.Count;

        string? name = null;
        string? classification = null;
        string? price = null;
        var priceLine = -1;
        for (var i = 0; i < top; i++)
        {
            var line = lines[i];
            if (name is null && Labelled(line, NameLabel) is { } named)
            {
                name = named.Length > 0 ? named : Next(lines, i, top);
            }

            if (classification is null && Labelled(line, ClassificationLabel) is { } kind)
            {
                classification = kind.Length > 0 ? kind : Next(lines, i, top);
            }

            if (priceLine < 0 && PriceLine().IsMatch(line))
            {
                priceLine = i;
                price = line;
            }
        }

        if (name is null && priceLine > 0)
        {
            for (var i = priceLine - 1; i >= 0; i--)
            {
                if (!Decoration().IsMatch(lines[i]) && lines[i].Any(char.IsLetter))
                {
                    name = lines[i];
                    break;
                }
            }
        }

        var specs = new List<SpecRow>();
        if (detail >= 0)
        {
            var end = description > detail ? description : EndOf(lines, detail + 1);
            for (var i = detail + 1; i < end; i++)
            {
                var line = lines[i];
                string label;
                string value;
                if (line.Contains('\t', StringComparison.Ordinal))
                {
                    var tab = line.IndexOf('\t', StringComparison.Ordinal);
                    (label, value) = (line[..tab], line[(tab + 1)..]);
                }
                else if (Pair().Match(line) is { Success: true } pair)
                {
                    (label, value) = (pair.Groups[1].Value, pair.Groups[2].Value);
                }
                else if (i + 1 < end)
                {
                    (label, value) = (line, lines[++i]);
                }
                else
                {
                    break;
                }

                label = Clean(label);
                value = Clean(value);
                if (label.Length > 0 && value.Length > 0)
                {
                    specs.Add(new SpecRow(label, value, SourcePrefix + label));
                }
            }
        }

        string? about = null;
        if (description >= 0)
        {
            var end = EndOf(lines, description + 1);
            var body = string.Join("\n", lines.Skip(description + 1).Take(end - description - 1));
            about = body.Length == 0 ? null : body.Length > MaxDescription ? body[..MaxDescription] : body;
        }

        return new ProductText(Optional(name), Optional(classification), specs, Optional(price), about);
    }

    /// <summary>The text cut into trimmed, non-empty, NFC lines; runs of spaces become one space, a tab is kept as a pair separator.</summary>
    public static List<string> Lines(string text)
    {
        var lines = new List<string>();
        foreach (var raw in VietnameseText.Nfc(text ?? string.Empty).Replace("\r", string.Empty, StringComparison.Ordinal).Split('\n'))
        {
            var cells = raw.Split('\t').Select(Clean).Where(c => c.Length > 0).ToList();
            var line = string.Join('\t', cells);
            if (line.Length > 0)
            {
                lines.Add(line);
            }
        }

        return lines;
    }

    private static string Clean(string text) => Spaces().Replace(text, " ").Trim();

    private static string? Optional(string? text) => string.IsNullOrWhiteSpace(text) ? null : Clean(text);

    private static string? Labelled(string line, string label)
    {
        if (!line.StartsWith(label, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var rest = line[label.Length..];
        if (rest.Length > 0 && !(rest[0] is ':' or '\t' or ' '))
        {
            return null;
        }

        return rest.TrimStart(':', '\t', ' ').Trim();
    }

    private static string? Next(List<string> lines, int index, int end) => index + 1 < end ? lines[index + 1] : null;

    private static int IndexOfLabel(List<string> lines, string label, int from)
    {
        for (var i = Math.Max(from, 0); i < lines.Count; i++)
        {
            if (string.Equals(lines[i].ToUpperInvariant(), label, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static int EndOf(List<string> lines, int from)
    {
        for (var i = from; i < lines.Count; i++)
        {
            var upper = lines[i].ToUpperInvariant();
            if (EndLabels.Contains(upper) || upper == DetailLabel || upper == DescriptionLabel)
            {
                return i;
            }
        }

        return lines.Count;
    }

    [GeneratedRegex(@"[    ​]+", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Spaces();

    [GeneratedRegex(@"^(₫\s?\d[\d.,]*|\d[\d.,]*\s?(₫|đ|VND|VNĐ))(\s?[-–]\s?(₫\s?\d[\d.,]*|\d[\d.,]*\s?(₫|đ|VND|VNĐ)))?$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex PriceLine();

    /// <summary>Rating, sales and badge lines that sit between the product name and the price.</summary>
    [GeneratedRegex(@"^[\d.,\s|/★☆]*$|đánh giá|đã bán|yêu thích|^mall$|^giảm|tố cáo", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase, 1000)]
    private static partial Regex Decoration();

    [GeneratedRegex(@"^([^:]{1,60}):\s*(.+)$", RegexOptions.CultureInvariant, 1000)]
    private static partial Regex Pair();
}

/// <summary>What the read found in a fetched page: the shared parser's fields plus the structured product data and the declared images.</summary>
internal sealed record PageFacts(string? Name, string? Classification, IReadOnlyList<SpecRow> Specs, string? Price, string? Description,
    IReadOnlyList<string> Images, bool Readable);

/// <summary>
/// THE PAGE READER (MC-18 module M-003; design decisions D-008 and D-010). It runs no script and fetches nothing: it reads the page's
/// structured product block (JSON-LD, read with a depth limit), its page tags (og:), its title and its visible text, the last through
/// <see cref="TextParser"/>. Product name: the structured block, then the title, then labelled text. Own images: only those the
/// structured block or the og:image tag declares, never an image the page merely shows. Stop markers: a verification (CAPTCHA) page and
/// a sign-in wall are recognised from held markers; a page with no structured product, no product page tag and no labelled product
/// text has no readable product.
/// </summary>
internal static partial class PageReader
{
    public const int JsonDepth = 32;
    public const int MaxImages = 20;

    /// <summary>Markers of a visitor verification page.</summary>
    public static readonly IReadOnlyList<string> CaptchaMarkers = ["captcha", "không phải là robot", "xác minh để tiếp tục", "kéo thanh trượt"];

    /// <summary>Markers of a page that asks the visitor to sign in before showing the product.</summary>
    public static readonly IReadOnlyList<string> SignInMarkers = ["đăng nhập để tiếp tục", "đăng nhập để xem", "vui lòng đăng nhập"];

    public static PageFacts Read(string html)
    {
        var text = VisibleText(html);
        var parsed = TextParser.Parse(text);
        var product = StructuredProduct(html);
        var tags = MetaTags(html);
        var title = Title(html);
        var isProductTag = tags.TryGetValue("og:type", out var type) && type.Count > 0 && type[0].Contains("product", StringComparison.OrdinalIgnoreCase);
        var readable = product is not null || isProductTag || parsed.Specs.Count > 0 || parsed.Classification is not null || (parsed.Name is not null && parsed.Price is not null);

        var name = product?.Name ?? (readable ? title : null) ?? parsed.Name;
        var images = new List<string>();
        foreach (var image in (product?.Images ?? []).Concat(tags.TryGetValue("og:image", out var og) ? og : []))
        {
            if (!images.Contains(image, StringComparer.Ordinal))
            {
                images.Add(image);
            }
        }

        var price = parsed.Price ?? product?.Price;
        var description = parsed.Description ?? product?.Description;
        return new PageFacts(Nfc(name), parsed.Classification, parsed.Specs, Nfc(price), Nfc(description), images, readable);
    }

    /// <summary>
    /// The stop a marker shows (captcha or sign-in-wall), or null. Markers are looked for in the page's title and visible text, and a
    /// password field in its markup; the page's scripts are not searched, since they are not what the visitor is shown.
    /// </summary>
    public static ReadStop? Marker(string html)
    {
        html ??= string.Empty;
        var shown = ((Title(html) ?? string.Empty) + "\n" + VisibleText(html)).ToLowerInvariant();
        if (CaptchaMarkers.Any(m => shown.Contains(m, StringComparison.Ordinal)))
        {
            return new ReadStop("captcha", "Shopee đưa ra trang xác minh người dùng (CAPTCHA). Ứng dụng không giải trang xác minh.", LinkRule.PasteNext);
        }

        if (SignInMarkers.Any(m => shown.Contains(m, StringComparison.Ordinal)) || PasswordField().IsMatch(html))
        {
            return new ReadStop("sign-in-wall", "Shopee yêu cầu đăng nhập mới cho xem trang. Ứng dụng không đăng nhập, không dùng tài khoản nào.", LinkRule.PasteNext);
        }

        return null;
    }

    /// <summary>The page's visible text: scripts, styles and comments removed, block elements on their own lines, entities decoded.</summary>
    public static string VisibleText(string html)
    {
        var text = Comments().Replace(html ?? string.Empty, " ");
        text = Hidden().Replace(text, "\n");
        text = Cells().Replace(text, "\t");
        text = Blocks().Replace(text, "\n");
        text = Tags().Replace(text, string.Empty);
        text = WebUtility.HtmlDecode(text);
        return string.Join("\n", TextParser.Lines(text));
    }

    private sealed record Structured(string? Name, IReadOnlyList<string> Images, string? Price, string? Description);

    private static Structured? StructuredProduct(string html)
    {
        foreach (Match block in JsonLd().Matches(html))
        {
            try
            {
                using var document = JsonDocument.Parse(block.Groups[1].Value,
                    new JsonDocumentOptions { MaxDepth = JsonDepth, AllowTrailingCommas = true, CommentHandling = JsonCommentHandling.Skip });
                if (FindProduct(document.RootElement, 0) is { } product)
                {
                    return Describe(product);
                }
            }
            catch (JsonException)
            {
                // A block that does not parse is not product data.
            }
        }

        return null;
    }

    private static JsonElement? FindProduct(JsonElement element, int depth)
    {
        if (depth > JsonDepth)
        {
            return null;
        }

        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    if (FindProduct(item, depth + 1) is { } found)
                    {
                        return found;
                    }
                }

                return null;
            case JsonValueKind.Object:
                if (element.TryGetProperty("@type", out var type)
                    && (type.ValueKind == JsonValueKind.String ? type.GetString() == "Product" : type.ValueKind == JsonValueKind.Array && type.EnumerateArray().Any(t => t.ValueKind == JsonValueKind.String && t.GetString() == "Product")))
                {
                    return element;
                }

                return element.TryGetProperty("@graph", out var graph) ? FindProduct(graph, depth + 1) : null;
            default:
                return null;
        }
    }

    private static Structured Describe(JsonElement product)
    {
        string? Text(JsonElement owner, string name) =>
            owner.TryGetProperty(name, out var v) ? v.ValueKind switch
            {
                JsonValueKind.String => v.GetString(),
                JsonValueKind.Number => v.GetRawText(),
                _ => null,
            } : null;

        var images = new List<string>();
        if (product.TryGetProperty("image", out var image))
        {
            var items = image.ValueKind == JsonValueKind.Array ? image.EnumerateArray().ToList() : [image];
            foreach (var item in items)
            {
                var url = item.ValueKind == JsonValueKind.String ? item.GetString() : item.ValueKind == JsonValueKind.Object ? Text(item, "url") : null;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    images.Add(url.Trim());
                }
            }
        }

        string? price = null;
        if (product.TryGetProperty("offers", out var offers))
        {
            var offer = offers.ValueKind == JsonValueKind.Array ? offers.EnumerateArray().FirstOrDefault() : offers;
            if (offer.ValueKind == JsonValueKind.Object && (Text(offer, "price") ?? Text(offer, "lowPrice")) is { } amount)
            {
                price = $"{amount} {Text(offer, "priceCurrency") ?? string.Empty}".Trim();
            }
        }

        return new Structured(Text(product, "name"), images, price, Text(product, "description"));
    }

    private static Dictionary<string, List<string>> MetaTags(string html)
    {
        var tags = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (Match meta in Meta().Matches(html))
        {
            var attributes = Attributes(meta.Value);
            var key = attributes.GetValueOrDefault("property") ?? attributes.GetValueOrDefault("name");
            if (key is not null && attributes.TryGetValue("content", out var content))
            {
                if (!tags.TryGetValue(key, out var list))
                {
                    tags[key] = list = [];
                }

                list.Add(WebUtility.HtmlDecode(content).Trim());
            }
        }

        return tags;
    }

    private static Dictionary<string, string> Attributes(string tag)
    {
        var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match attribute in Attribute().Matches(tag))
        {
            found[attribute.Groups[1].Value] = attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[4].Value;
        }

        return found;
    }

    private static string? Title(string html)
    {
        var match = TitleTag().Match(html);
        if (!match.Success)
        {
            return null;
        }

        var title = WebUtility.HtmlDecode(Tags().Replace(match.Groups[1].Value, string.Empty)).Trim();
        foreach (var suffix in new[] { " | Shopee Việt Nam", " - Shopee Việt Nam", " | Shopee" })
        {
            if (title.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                title = title[..^suffix.Length].Trim();
            }
        }

        return title.Length == 0 ? null : title;
    }

    private static string? Nfc(string? text) => text is null ? null : VietnameseText.Nfc(text.Trim());

    [GeneratedRegex(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Comments();

    [GeneratedRegex(@"<(script|style|noscript|template|svg|head)\b[^>]*>.*?</\1\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Hidden();

    [GeneratedRegex(@"</t[dh]\s*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Cells();

    [GeneratedRegex(@"<br\s*/?>|</?(p|div|li|ul|ol|tr|table|h[1-6]|section|article|header|footer|nav|main|aside|label|dt|dd|dl|button|option|select|form)\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Blocks();

    [GeneratedRegex(@"<[^>]*>", RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Tags();

    [GeneratedRegex(@"<script[^>]*type\s*=\s*[""']application/ld\+json[""'][^>]*>(.*?)</script\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex JsonLd();

    [GeneratedRegex(@"<meta\b[^>]*>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Meta();

    [GeneratedRegex(@"([a-zA-Z:-]+)\s*=\s*(""([^""]*)""|'([^']*)')", RegexOptions.CultureInvariant, 2000)]
    private static partial Regex Attribute();

    [GeneratedRegex(@"<input\b[^>]*type\s*=\s*[""']?password", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex PasswordField();

    [GeneratedRegex(@"<title\b[^>]*>(.*?)</title\s*>", RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, 2000)]
    private static partial Regex TitleTag();
}
