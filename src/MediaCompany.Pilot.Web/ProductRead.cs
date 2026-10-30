using System.Text;
using System.Text.RegularExpressions;

namespace MediaCompany.Pilot.Web;

/// <summary>The end of one read click: the fill record (absent when the page phase stopped), the stop if any, and what was sent.</summary>
internal sealed record ReadResult(FillRecord? Fill, ReadStop? Stop, int Requests, IReadOnlyCollection<string> LocalAddresses);

/// <summary>
/// ONE READ CLICK (MC-18 design decisions D-001, D-005, D-009, D-010). The link is admitted by the link rule before anything is sent;
/// each redirect hop is checked before it is requested; the one product page is read; then only the images the page's structured data
/// or image tag declares are fetched, one request each, no redirect followed, and kept only with a PNG or JPEG signature, under names
/// the app chooses (anh-doc-01 onward). The first stop ends the read with no further request: in the page phase nothing is filled; in
/// the image phase the facts are kept and the remaining images are named for upload at step 2. No script, style sheet, font, other
/// product, shop, category or search address is ever requested.
/// </summary>
internal static class ProductRead
{
    public const string ImagePrefix = "anh-doc-";

    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];

    public static async Task<ReadResult> Run(string? rawLink, ReadTransport? transport, ReadLog log, string imageFolder, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(log);
        Uri link;
        try
        {
            link = LinkRule.Admit(rawLink);
        }
        catch (ReadStop stop)
        {
            return new ReadResult(null, stop, 0, []);
        }

        using var click = new CancellationTokenSource(ReadSender.ClickBound);
        using var sender = new ReadSender(transport, log, click.Token);
        Uri final;
        PageFacts page;
        try
        {
            (final, page) = await ReadPage(sender, link);
        }
        catch (ReadStop stop)
        {
            return new ReadResult(null, stop, sender.Requests, sender.LocalAddresses);
        }

        Directory.CreateDirectory(imageFolder);
        foreach (var old in Directory.EnumerateFiles(imageFolder))
        {
            File.Delete(old);
        }

        var saved = new List<SavedImage>();
        var skipped = new List<SkippedImage>();
        ReadStop? imageStop = null;
        for (var i = 0; i < page.Images.Count; i++)
        {
            var raw = page.Images[i];
            if (imageStop is not null)
            {
                skipped.Add(new SkippedImage(raw, "lần đọc đã dừng trước ảnh này"));
                continue;
            }

            if (i >= PageReader.MaxImages)
            {
                skipped.Add(new SkippedImage(raw, $"trang khai quá {PageReader.MaxImages} ảnh; ứng dụng chỉ lấy {PageReader.MaxImages} ảnh đầu"));
                continue;
            }

            var (address, reason) = LinkRule.Image(final, raw);
            if (address is null)
            {
                skipped.Add(new SkippedImage(raw, reason!));
                continue;
            }

            try
            {
                var answer = await sender.Get(address, image: true);
                if (answer.Status is >= 300 and < 400)
                {
                    skipped.Add(new SkippedImage(raw, "máy chủ ảnh chuyển tiếp sang chỗ khác; ứng dụng không đi theo"));
                    continue;
                }

                Require(answer.Status, image: true);
                var extension = Signature(answer.Body);
                if (extension is null)
                {
                    skipped.Add(new SkippedImage(raw, "tệp nhận về không phải ảnh PNG hoặc JPEG"));
                    continue;
                }

                var name = $"{ImagePrefix}{saved.Count + 1:00}{extension}";
                var path = Path.Combine(imageFolder, PlainName.Require(name, "Ảnh đọc từ trang", imageFolder));
                await File.WriteAllBytesAsync(path, answer.Body);
                saved.Add(new SavedImage(name, address.ToString()));
            }
            catch (ReadStop stop) when (stop.Code == "image-too-large")
            {
                skipped.Add(new SkippedImage(raw, stop.Reason));
            }
            catch (ReadStop stop)
            {
                imageStop = stop;
                skipped.Add(new SkippedImage(raw, "lần đọc dừng ở ảnh này: " + stop.Reason));
            }
        }

        var fill = FillStore.FromRead(link, final, page, now, saved, skipped);
        var stopped = imageStop is null ? null : new ReadStop(imageStop.Code,
            $"Đã điền thông tin từ trang và lưu {saved.Count} ảnh, rồi dừng khi lấy ảnh: {imageStop.Reason}",
            "Xác nhận các ô đã điền. Các ảnh chưa lấy được ghi tên ở dưới: tải chúng lên ở bước 2. Ứng dụng không thử đọc lại.");
        return new ReadResult(fill, stopped, sender.Requests, sender.LocalAddresses);
    }

    /// <summary>The link, its hops (each checked first), and the one page; every stop of D-010 is thrown.</summary>
    private static async Task<(Uri Final, PageFacts Page)> ReadPage(ReadSender sender, Uri link)
    {
        var address = link;
        var hops = 0;
        ReadAnswer answer;
        while (true)
        {
            answer = await sender.Get(address, image: false);
            if (answer.Status is 301 or 302 or 303 or 307 or 308)
            {
                hops++;
                address = LinkRule.Hop(address, answer.Location, hops);
                continue;
            }

            break;
        }

        Require(answer.Status, image: false);
        if (answer.MediaType is null || !answer.MediaType.Equals("text/html", StringComparison.OrdinalIgnoreCase))
        {
            throw new ReadStop("not-html", $"Đường dẫn trả về {answer.MediaType ?? "một tệp không rõ loại"}, không phải trang web.", LinkRule.PasteNext);
        }

        var html = Encoding.UTF8.GetString(answer.Body);
        try
        {
            if (PageReader.Marker(html) is { } marker)
            {
                throw marker;
            }

            var page = PageReader.Read(html);
            return page.Readable ? (address, page) : throw NoProduct();
        }
        catch (RegexMatchTimeoutException)
        {
            throw NoProduct();
        }
    }

    /// <summary>The status stops of D-010: 403, 429, 5xx, and any other status but 200.</summary>
    private static void Require(int status, bool image)
    {
        var what = image ? "ảnh của sản phẩm" : "trang này";
        switch (status)
        {
            case 200:
                return;
            case 403:
                throw new ReadStop("access-refused", $"Shopee từ chối cho đọc {what} (mã 403).", LinkRule.PasteNext);
            case 429:
                throw new ReadStop("too-many-requests", $"Shopee báo có quá nhiều yêu cầu (mã 429) khi đọc {what}.", "Không bấm đọc lại ngay. " + LinkRule.PasteNext);
            case >= 500:
                throw new ReadStop("server-error", $"Máy chủ Shopee đang lỗi (mã {status}) khi đọc {what}.", LinkRule.PasteNext);
            default:
                throw new ReadStop("status-unexpected", $"Shopee trả lời mã {status} khi đọc {what}, không phải nội dung cần đọc.", LinkRule.PasteNext);
        }
    }

    private static ReadStop NoProduct() =>
        new("no-product",
            "Trang trả về không có thông tin sản phẩm đọc được (trang Shopee thường chỉ hiện sản phẩm khi trình duyệt chạy mã của trang).",
            LinkRule.PasteNext);

    /// <summary>The extension the content's own signature shows, or null when it is neither PNG nor JPEG.</summary>
    public static string? Signature(byte[] body)
    {
        ArgumentNullException.ThrowIfNull(body);
        if (body.AsSpan().StartsWith(PngSignature))
        {
            return ".png";
        }

        return body.AsSpan().StartsWith(JpegSignature) ? ".jpg" : null;
    }
}
