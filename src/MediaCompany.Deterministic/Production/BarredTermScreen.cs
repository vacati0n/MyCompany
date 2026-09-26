using System.Collections.Frozen;
using System.Text.RegularExpressions;
using MediaCompany.Domain.Dossier;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// The barred child-directed metadata screen (criterion A-006, treatment condition K-5), a member
/// of the rule-determined set under <see cref="DeterministicTaskRegistry.BarredTermMetadataScreen"/>.
///
/// Metadata is a named factor in the made-for-kids determination in its own right, so a title,
/// description or tag set carrying one of these terms is not a stylistic lapse — it is evidence a
/// platform reads against the item. The screen refuses with the term named, because a composite
/// verdict cannot be acted on.
/// </summary>
public static class BarredTermScreen
{
    /// <summary>
    /// The barred terms, carried from treatment rule K-5. The set is closed and held here rather
    /// than in the configuration register: a treatment condition is not a setting (constraint
    /// C-017), so widening this list is a code change the Review Gate sees.
    /// </summary>
    public static readonly FrozenSet<string> BarredTerms = new[]
    {
        "for kids",
        "for children",
        "cute",
        "funny",
        "baby animals",
        "learn animals",
        "kids",
        "nursery",
        "toddler",
        "preschool",
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>The three metadata surfaces the screen reads. All three are screened, not just the title.</summary>
    public enum Surface
    {
        Title = 1,
        Description = 2,
        Tags = 3,
    }

    /// <summary>One barred term found, naming the surface it was found in.</summary>
    public sealed record Hit(Surface Surface, string Term, string Context);

    /// <summary>
    /// Screens all three surfaces and returns every hit. The result is every hit rather than the
    /// first, so a caller repairing the metadata sees the whole problem in one pass.
    /// </summary>
    public static IReadOnlyList<Hit> Screen(ItemMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        var hits = new List<Hit>();

        foreach (var term in BarredTerms)
        {
            if (ContainsWholeTerm(metadata.Title, term))
            {
                hits.Add(new Hit(Surface.Title, term, metadata.Title));
            }

            if (ContainsWholeTerm(metadata.Description, term))
            {
                hits.Add(new Hit(Surface.Description, term, metadata.Description));
            }

            foreach (var tag in metadata.Tags)
            {
                if (ContainsWholeTerm(tag, term))
                {
                    hits.Add(new Hit(Surface.Tags, term, tag));
                }
            }
        }

        return hits;
    }

    /// <summary>Whether the metadata set passes. A set with any hit is refused.</summary>
    public static bool Passes(ItemMetadata metadata) => Screen(metadata).Count == 0;

    /// <summary>
    /// The refusal text, naming each term and the surface it was found in. A refusal that does not
    /// say what it refused cannot be designed against (sequencing constraint P-014).
    /// </summary>
    public static string DescribeRefusal(IReadOnlyList<Hit> hits) =>
        hits.Count == 0
            ? string.Empty
            : "Barred child-directed term(s) present: " +
              string.Join("; ", hits.Select(h => $"'{h.Term}' in {h.Surface}"));

    /// <summary>
    /// Matches on a word boundary so that "kids" does not fire inside an unrelated word. The screen
    /// must be precise: a screen that cries wolf gets disabled, and a disabled control is no control.
    /// </summary>
    private static bool ContainsWholeTerm(string? text, string term) =>
        !string.IsNullOrWhiteSpace(text) &&
        Regex.IsMatch(text, $@"\b{Regex.Escape(term)}\b", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
}
