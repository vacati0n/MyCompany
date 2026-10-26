using System.Globalization;
using System.Text;

namespace MediaCompany.Pilot;

/// <summary>Text rules shared by every check: NFC normalization, control-character refusal, and the first letter in capitals.</summary>
public static class VietnameseText
{
    public static string Nfc(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        RequireNormalization();
        return text.Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Refuses to run where NFC normalization does nothing, as under invariant globalization: the checks would then compare
    /// decomposed and composed letters as different text.
    /// </summary>
    public static void RequireNormalization()
    {
        if (Probe.Value)
        {
            return;
        }

        throw new InvalidOperationException("Unicode NFC normalization is not available in this process (invariant globalization); the pilot checks cannot run");
    }

    private static readonly Lazy<bool> Probe = new(() => "a\u0302\u0301".Normalize(NormalizationForm.FormC) == "\u1EA5");

    /// <summary>The first control or format character in the text, if any (line breaks included).</summary>
    public static Rune? FirstControl(string text)
    {
        foreach (var rune in text.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            if (category is UnicodeCategory.Control or UnicodeCategory.Format or UnicodeCategory.LineSeparator or UnicodeCategory.ParagraphSeparator)
            {
                return rune;
            }
        }

        return null;
    }

    public static string CapitalizeFirst(string text)
    {
        if (text.Length == 0)
        {
            return text;
        }

        var first = Rune.GetRuneAt(text, 0);
        return Rune.ToUpperInvariant(first).ToString() + text[first.Utf16SequenceLength..];
    }
}
