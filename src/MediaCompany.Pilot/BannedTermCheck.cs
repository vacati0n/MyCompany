using System.Text.RegularExpressions;

namespace MediaCompany.Pilot;

/// <summary>One banned term found in one text: where it was, the class, and the term as held.</summary>
public sealed record BannedFinding(string Where, string ClassId, string ClassName, string Term);

/// <summary>
/// THE BANNED-TERM CHECK (design D-004, scope criteria 5 and 7). One check serves fact rows, operator overrides, script
/// sentences and the call to action. Text is NFC-normalized and lower-cased invariantly, split into syllables (runs of letters,
/// marks and digits), and a term matches only as a whole run of syllables, so "nhất định" does not contain "nhất" when the
/// exemption holds it, and "giá đỡ" never matches anything. Money amounts and ratings are matched by the held patterns under a
/// time bound, the rule BarredTermScreen carries.
/// </summary>
public sealed class BannedTermCheck
{
    private static readonly TimeSpan Bound = TimeSpan.FromSeconds(1);
    private static readonly Regex Syllable = new(@"[\p{L}\p{M}\p{N}]+", RegexOptions.CultureInvariant, Bound);

    private readonly List<(BannedClass Class, string Term, string[] Syllables)> _terms = [];
    private readonly List<(BannedClass Class, Regex Pattern)> _patterns = [];
    private readonly List<string[]> _exemptions = [];
    private readonly List<(string Term, string[] Syllables)> _firstPerson = [];

    public BannedTermCheck(HeldData held)
    {
        ArgumentNullException.ThrowIfNull(held);
        foreach (var banned in held.BannedClasses)
        {
            foreach (var term in banned.Terms)
            {
                _terms.Add((banned, term, Syllables(term)));
            }

            foreach (var pattern in banned.Patterns)
            {
                _patterns.Add((banned, new Regex(pattern, RegexOptions.CultureInvariant, Bound)));
            }
        }

        _exemptions.AddRange(held.Exemptions.Select(Syllables));

        // The words an operator's own sentence may not carry (decision on correction request 2): the held first-person words, then
        // the class (d) terms of claimed use and review.
        var useAndReview = held.BannedClasses.Where(c => c.Id == "d").SelectMany(c => c.Terms);
        _firstPerson.AddRange(held.FirstPersonTerms.Concat(useAndReview).Select(t => (t, Syllables(t))));
    }

    public static BannedTermCheck Held { get; } = new(HeldData.Current);

    /// <summary>Every banned term in the text, in held order; empty when the text passes.</summary>
    public IReadOnlyList<BannedFinding> Find(string where, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var lowered = VietnameseText.Nfc(text).ToLowerInvariant();
        var syllables = Syllable.Matches(lowered).Select(m => m.Value).ToArray();

        // A syllable inside an exemption occurrence is masked from any term lying wholly inside that occurrence.
        var exempt = new List<(int Start, int End)>();
        foreach (var exemption in _exemptions)
        {
            foreach (var at in Occurrences(syllables, exemption))
            {
                exempt.Add((at, at + exemption.Length));
            }
        }

        var findings = new List<BannedFinding>();
        foreach (var (banned, term, termSyllables) in _terms)
        {
            var hit = Occurrences(syllables, termSyllables)
                .Any(at => !exempt.Any(e => at >= e.Start && at + termSyllables.Length <= e.End));
            if (hit)
            {
                findings.Add(new BannedFinding(where, banned.Id, banned.Name, term));
            }
        }

        foreach (var (banned, pattern) in _patterns)
        {
            var match = pattern.Match(lowered);
            if (match.Success)
            {
                findings.Add(new BannedFinding(where, banned.Id, banned.Name, match.Value));
            }
        }

        return findings;
    }

    /// <summary>
    /// The first held first-person word, or use or review term, found in the text as whole syllables after NFC and lower-casing;
    /// null when there is none. "Mình dùng mỗi sáng" yields "mình"; "kem" never yields "em".
    /// </summary>
    public string? FirstPerson(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var syllables = Syllables(text);
        return _firstPerson.FirstOrDefault(t => Occurrences(syllables, t.Syllables).Any()).Term;
    }

    /// <summary>Refuses the text on its first banned term, naming where and the term.</summary>
    public void Refuse(string where, string text)
    {
        var findings = Find(where, text);
        if (findings.Count > 0)
        {
            var first = findings[0];
            throw new PilotRefusal("banned-term",
                $"{first.Where}: có từ bị cấm \"{first.Term}\" (nhóm {first.ClassId}: {first.ClassName}). Bỏ dòng này hoặc viết câu thay thế không có từ đó.");
        }
    }

    private static string[] Syllables(string text) =>
        Syllable.Matches(VietnameseText.Nfc(text).ToLowerInvariant()).Select(m => m.Value).ToArray();

    private static IEnumerable<int> Occurrences(string[] haystack, string[] needle)
    {
        if (needle.Length == 0)
        {
            yield break;
        }

        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            var all = true;
            for (var j = 0; j < needle.Length && all; j++)
            {
                all = string.Equals(haystack[i + j], needle[j], StringComparison.Ordinal);
            }

            if (all)
            {
                yield return i;
            }
        }
    }
}
