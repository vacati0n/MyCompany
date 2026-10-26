namespace MediaCompany.Pilot;

/// <summary>One caption page: up to the configured number of lines of one sentence, shown inside its share of the sentence's window.</summary>
public sealed record CaptionPage(int Sentence, int Page, IReadOnlyList<string> Lines, double Start, double End);

/// <summary>The drawn text boxes: the overlay at the top of the safe area and the caption box at its bottom.</summary>
public sealed record TextLayout(RectangleSetting Safe, RectangleSetting OverlayBox, RectangleSetting CaptionBox, int CaptionLineWidth, int OverlayLineWidth);

/// <summary>
/// THE CAPTION LAYOUT AND SAFE-AREA CHECK (decision record entry 1308). The overlay box spans the safe-area width at its top; the
/// caption box spans the safe-area width at its bottom and is as tall as the configured lines per page need. The caption line width
/// is the safe-area width less the box's side padding, so a phone player's buttons and description never cover a caption.
///
/// A sentence wider than the configured lines is shown as consecutive pages inside the sentence's own window, never with a
/// smaller size (decision record entry 1309): the fixed disclosure sentence does not fit two lines at 56 px in an 840 px safe
/// area. Each page stays on screen at least the configured minimum (1.2 s or more); a window too short for that is refused.
/// </summary>
public static class CaptionLayout
{
    public static TextLayout Boxes(PilotSettings settings, FaceMetrics metrics, HeldData held)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(held);
        var safe = settings.SafeRectangle;
        if (safe.Width < 2 || safe.Height < 2)
        {
            throw new PilotRefusal("safe-area-invalid", $"Vùng an toàn ({safe}) không còn chỗ trong khung {settings.Frame.Width}x{settings.Frame.Height}.");
        }

        var overlayHeight = BoxHeight(settings.Overlay, metrics, 1);
        var captionHeight = BoxHeight(settings.Caption, metrics, settings.Caption.MaximumLinesPerPage);
        var overlay = new RectangleSetting { X = safe.X, Y = safe.Y, Width = safe.Width, Height = overlayHeight };
        var caption = new RectangleSetting { X = safe.X, Y = safe.Y + safe.Height - captionHeight, Width = safe.Width, Height = captionHeight };
        var layout = new TextLayout(safe, overlay, caption, safe.Width - (2 * settings.Caption.PaddingX), safe.Width - (2 * settings.Overlay.PaddingX));
        Check(layout, settings);
        var overlayWidth = metrics.Width(held.OverlayText, settings.Overlay.FontSize);
        if (overlayWidth > layout.OverlayLineWidth)
        {
            throw new PilotRefusal("text-outside-safe-area",
                $"Dòng nhãn \"{held.OverlayText}\" rộng {overlayWidth:0} px ở cỡ {settings.Overlay.FontSize} px, vượt bề rộng {layout.OverlayLineWidth} px trong vùng an toàn.");
        }

        return layout;
    }

    /// <summary>
    /// Refuses a layout whose text boxes leave the safe area or the frame, overlap each other, or are covered by the presenter inset.
    /// The cases checked: the overlay box inside the safe area; the caption box inside the safe area; the safe area inside the frame;
    /// the overlay and caption boxes apart; the presenter inset apart from each text box; each line width positive.
    /// </summary>
    public static void Check(TextLayout layout, PilotSettings settings)
    {
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(settings);
        var frame = new RectangleSetting { X = 0, Y = 0, Width = settings.Frame.Width, Height = settings.Frame.Height };
        void Refuse(string what) => throw new PilotRefusal("text-outside-safe-area", what);
        if (!layout.Safe.Inside(frame))
        {
            Refuse($"Vùng an toàn ({layout.Safe}) nằm ngoài khung hình.");
        }

        if (!layout.OverlayBox.Inside(layout.Safe))
        {
            Refuse($"Khung lớp nhãn ({layout.OverlayBox}) nằm ngoài vùng an toàn ({layout.Safe}).");
        }

        if (!layout.CaptionBox.Inside(layout.Safe))
        {
            Refuse($"Khung phụ đề ({layout.CaptionBox}) nằm ngoài vùng an toàn ({layout.Safe}).");
        }

        if (layout.OverlayBox.Intersects(layout.CaptionBox))
        {
            Refuse("Khung lớp nhãn và khung phụ đề chồng lên nhau; vùng an toàn quá thấp.");
        }

        if (settings.PresenterInset.Intersects(layout.OverlayBox) || settings.PresenterInset.Intersects(layout.CaptionBox))
        {
            Refuse($"Khung người dẫn ({settings.PresenterInset}) che khung chữ; đổi presenterInset.");
        }

        if (layout.CaptionLineWidth < 1 || layout.OverlayLineWidth < 1)
        {
            Refuse("Khoảng đệm hai bên lớn hơn bề rộng vùng an toàn.");
        }
    }

    public static int BoxHeight(TextBoxSetting box, FaceMetrics metrics, int lines)
    {
        ArgumentNullException.ThrowIfNull(box);
        ArgumentNullException.ThrowIfNull(metrics);
        return (lines * metrics.LineHeight(box.FontSize)) + ((lines - 1) * box.LineSpacing) + (2 * box.PaddingY);
    }

    /// <summary>Breaks a sentence into lines no wider than the line width, at spaces only; a single word wider than the line is refused.</summary>
    public static IReadOnlyList<string> Lines(string sentence, int lineWidth, int size, FaceMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(sentence);
        ArgumentNullException.ThrowIfNull(metrics);
        var lines = new List<string>();
        var current = string.Empty;
        foreach (var word in sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (metrics.Width(word, size) > lineWidth)
            {
                throw new PilotRefusal("text-outside-safe-area",
                    $"Từ \"{word}\" rộng hơn một dòng phụ đề ({lineWidth} px ở cỡ {size} px); không thể đặt trong vùng an toàn.");
            }

            var candidate = current.Length == 0 ? word : current + " " + word;
            if (metrics.Width(candidate, size) <= lineWidth)
            {
                current = candidate;
            }
            else
            {
                lines.Add(current);
                current = word;
            }
        }

        if (current.Length > 0)
        {
            lines.Add(current);
        }

        return lines;
    }

    /// <summary>
    /// The caption pages of every sentence, each inside its sentence window (decision record entry 1309). Each page is given the
    /// configured minimum on-screen time and the rest of the window is shared in proportion to each page's characters; the page
    /// boundaries are rounded down to the millisecond, so no page is shorter than the minimum. A window shorter than its pages times
    /// the minimum is refused, naming the sentence.
    /// </summary>
    public static IReadOnlyList<CaptionPage> Pages(
        IReadOnlyList<ScriptSentence> sentences, IReadOnlyList<(double Start, double End)> windows, TextLayout layout, PilotSettings settings, FaceMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(sentences);
        ArgumentNullException.ThrowIfNull(windows);
        ArgumentNullException.ThrowIfNull(layout);
        ArgumentNullException.ThrowIfNull(settings);
        if (sentences.Count != windows.Count)
        {
            throw new ArgumentException("one window per sentence is required", nameof(windows));
        }

        var minimum = settings.CaptionPageMinimumSeconds;
        var pages = new List<CaptionPage>();
        for (var s = 0; s < sentences.Count; s++)
        {
            var lines = Lines(sentences[s].Text, layout.CaptionLineWidth, settings.Caption.FontSize, metrics);
            var groups = lines.Chunk(settings.Caption.MaximumLinesPerPage).Select(g => (IReadOnlyList<string>)g).ToList();
            var (start, end) = windows[s];
            var needed = groups.Count * minimum;
            if (end - start < needed - 1e-9)
            {
                throw new PilotRefusal("caption-page-too-short", string.Create(System.Globalization.CultureInfo.InvariantCulture,
                    $"Câu {sentences[s].Number} cần {groups.Count} trang phụ đề, mỗi trang ít nhất {minimum:0.0#} giây, tức {needed:0.0#} giây; khoảng nói của câu chỉ có {end - start:0.00} giây. Ghi lại và nói câu {sentences[s].Number} chậm hơn, hoặc sửa tệp {CaptionTiming.TimingFileName} cho khoảng của câu dài hơn."));
            }

            var extra = Math.Max(0, (end - start) - needed);
            var total = Math.Max(1, groups.Sum(g => g.Sum(l => l.Length)));
            var characters = 0;
            var at = start;
            for (var p = 0; p < groups.Count; p++)
            {
                characters += groups[p].Sum(l => l.Length);
                var share = Math.Floor(extra * characters / total * 1000) / 1000;
                var pageEnd = p == groups.Count - 1 ? end : Math.Round(start + ((p + 1) * minimum) + share, 3);
                pages.Add(new CaptionPage(sentences[s].Number, p + 1, groups[p], Math.Round(at, 3), Math.Round(pageEnd, 3)));
                at = pageEnd;
            }
        }

        return pages;
    }

    /// <summary>The sentence a caption's pages show, rejoined: equal to the script sentence when no word was lost or changed.</summary>
    public static string Rejoined(IReadOnlyList<CaptionPage> pages, int sentence)
    {
        ArgumentNullException.ThrowIfNull(pages);
        return string.Join(" ", pages.Where(p => p.Sentence == sentence).OrderBy(p => p.Page).SelectMany(p => p.Lines));
    }
}
