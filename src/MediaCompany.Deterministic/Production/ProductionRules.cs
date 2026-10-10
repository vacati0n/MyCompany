using System.Globalization;
using MediaCompany.Domain.Production;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// The material check (the production change, decision D-011 of its design), a pure function: every quotation
/// of the item material must occur verbatim in its source, every displayed line in its quotation, every beat's
/// opening exactly once in the narration and in beat order, every graphic and clip in its beat's structure row,
/// and the narration must be the length the material records. It names the FIRST mismatch, or none.
/// </summary>
public static class MaterialCheck
{
    /// <summary>The first mismatch, or null. <paramref name="sources"/> holds each source's text with line endings normalised.</summary>
    public static string? FirstMismatch(ItemMaterial material, IReadOnlyDictionary<string, string> sources)
    {
        ArgumentNullException.ThrowIfNull(material);
        ArgumentNullException.ThrowIfNull(sources);

        string? Quoted(string what, string source, string quote) =>
            !sources.TryGetValue(source, out var text) ? $"{what}: its source {source} was not read"
            : !text.Contains(quote, StringComparison.Ordinal) ? $"{what}: the quotation is not in {source}: \"{quote}\""
            : null;

        if (!sources.TryGetValue(material.NarrationFile, out var narration))
        {
            return $"the narration file {material.NarrationFile} was not read";
        }

        if (narration.Length != material.NarrationCharacters)
        {
            return $"the narration is {narration.Length} characters and the material records {material.NarrationCharacters}";
        }

        if (Quoted("the specified runtime", material.SpecifiedRuntime.Source, material.SpecifiedRuntime.Quote) is { } runtime)
        {
            return runtime;
        }

        var previous = -1;
        foreach (var beat in material.Beats.OrderBy(b => b.Number))
        {
            if (Quoted($"beat {beat.Number}", beat.StructureSource, beat.StructureQuote) is { } row)
            {
                return row;
            }

            if (Quoted($"beat {beat.Number}'s cue", beat.CueSource, beat.CueQuote) is { } cue)
            {
                return cue;
            }

            if (!beat.StructureQuote.Contains(beat.Title, StringComparison.Ordinal))
            {
                return $"beat {beat.Number}: its title \"{beat.Title}\" is not in its structure row";
            }

            var at = narration.IndexOf(beat.Opening, StringComparison.Ordinal);
            if (at < 0 || narration.IndexOf(beat.Opening, at + 1, StringComparison.Ordinal) >= 0)
            {
                return $"beat {beat.Number}: its opening is not found exactly once in the narration: \"{beat.Opening}\"";
            }

            if (at <= previous)
            {
                return $"beat {beat.Number}: its opening is not after beat {beat.Number - 1}'s";
            }

            previous = at;
        }

        foreach (var graphic in material.Graphics)
        {
            if (Quoted(graphic.Id, graphic.Source, graphic.Quote) is { } quote)
            {
                return quote;
            }

            if (graphic.Lines.FirstOrDefault(l => !graphic.Quote.Contains(l, StringComparison.Ordinal)
                    && !graphic.Quote.Replace("`", string.Empty, StringComparison.Ordinal).Contains(l, StringComparison.Ordinal)) is { } line)
            {
                return $"{graphic.Id}: the displayed line is not in its quotation: \"{line}\"";
            }

            if (BeatRow(material, graphic.Beat) is not { } row || !row.Contains(graphic.Id, StringComparison.Ordinal))
            {
                return $"{graphic.Id}: beat {graphic.Beat}'s structure row does not name it";
            }
        }

        foreach (var clip in material.Clips)
        {
            if (Quoted(clip.Id, clip.Source, clip.Quote) is { } quote)
            {
                return quote;
            }

            if (!clip.Quote.Replace("**", string.Empty, StringComparison.Ordinal).Contains(clip.Label, StringComparison.Ordinal))
            {
                return $"{clip.Id}: its label is not in its quotation";
            }

            if (BeatRow(material, clip.Beat) is not { } row || !row.Contains(clip.Id, StringComparison.Ordinal))
            {
                return $"{clip.Id}: beat {clip.Beat}'s structure row does not name it";
            }
        }

        if (Quoted("the unsourced clips", material.UnsourcedClips.Source, material.UnsourcedClips.Quote) is { } unsourced)
        {
            return unsourced;
        }

        if (material.UnsourcedClips.Ids.FirstOrDefault(id => !material.UnsourcedClips.Quote.Contains(id, StringComparison.Ordinal)) is { } missing)
        {
            return $"{missing} is named unsourced and its quotation does not name it";
        }

        if (Quoted("the clip placeholders", material.ClipPlaceholders.Source, material.ClipPlaceholders.Quote) is { } placeholders)
        {
            return placeholders;
        }

        foreach (var thumbnail in material.Thumbnails)
        {
            if (Quoted(thumbnail.Id, thumbnail.Source, thumbnail.Quote) is { } quote)
            {
                return quote;
            }

            if (thumbnail.Lines.FirstOrDefault(l => !thumbnail.Quote.Contains(l, StringComparison.Ordinal)) is { } line)
            {
                return $"{thumbnail.Id}: the displayed line is not in its quotation: \"{line}\"";
            }

            if (thumbnail.DisplayedText is { } shown && !thumbnail.Quote.Contains(shown, StringComparison.Ordinal))
            {
                return $"{thumbnail.Id}: its displayed text is not in its quotation";
            }

            if (thumbnail.Placeholder
                && (thumbnail.PlaceholderSource is null || thumbnail.PlaceholderQuote is null
                    || Quoted($"{thumbnail.Id}'s placeholder", thumbnail.PlaceholderSource, thumbnail.PlaceholderQuote) is not null))
            {
                return $"{thumbnail.Id}: it is a placeholder and the decision that made it one is not quoted from its source";
            }
        }

        return null;
    }

    private static string? BeatRow(ItemMaterial material, int beat) =>
        material.Beats.FirstOrDefault(b => b.Number == beat)?.StructureQuote;
}

/// <summary>One narration request: its beat, its ordinal and its exact text.</summary>
public sealed record NarrationPart(int Beat, int Ordinal, string Text);

/// <summary>
/// The narration split (the production change, decision D-009 of its design), a pure function. The recorded text,
/// its line endings normalised to line feeds and nothing else, is cut first at the beat openings, then within a
/// beat at paragraph boundaries, then at sentence boundaries, keeping every separator at the END of the preceding
/// part, so the parts concatenate to the recorded text EXACTLY. Each part holds at most the configured character
/// maximum; a sentence longer than the maximum refuses the plan, naming it.
/// </summary>
public static class NarrationSplitter
{
    public static IReadOnlyList<NarrationPart> Split(string narration, IReadOnlyList<MaterialBeat> beats, int characterMaximum)
    {
        ArgumentNullException.ThrowIfNull(narration);
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentOutOfRangeException.ThrowIfLessThan(characterMaximum, 1);

        var ordered = beats.OrderBy(b => b.Number).ToArray();
        var starts = ordered.Select(b => narration.IndexOf(b.Opening, StringComparison.Ordinal)).ToArray();
        if (starts.Length == 0 || starts[0] != 0)
        {
            throw new NarrationSplitRefusedException("the first beat does not open the narration, so text would fall outside every beat");
        }

        var parts = new List<NarrationPart>();
        for (var b = 0; b < ordered.Length; b++)
        {
            var end = b + 1 < starts.Length ? starts[b + 1] : narration.Length;
            if (starts[b] < 0 || end < starts[b])
            {
                throw new NarrationSplitRefusedException($"beat {ordered[b].Number}'s opening is not found in order in the narration");
            }

            var span = narration[starts[b]..end];
            foreach (var text in Pack(Pieces(span, "\n\n"), characterMaximum, ordered[b].Number))
            {
                parts.Add(new NarrationPart(ordered[b].Number, parts.Count + 1, text));
            }
        }

        if (!string.Equals(string.Concat(parts.Select(p => p.Text)), narration, StringComparison.Ordinal))
        {
            throw new NarrationSplitRefusedException("the parts do not concatenate to the recorded narration");
        }

        return parts;
    }

    /// <summary>
    /// One part per beat (the own-voice change, decision D-001 of its design): the own sources narrate a beat as one part,
    /// matching recordings registered one file per beat; the vendor's character maximum does not apply. The parts concatenate
    /// to the recorded text exactly, or the split is refused.
    /// </summary>
    public static IReadOnlyList<NarrationPart> SplitByBeat(string narration, IReadOnlyList<MaterialBeat> beats) =>
        Split(narration, beats, int.MaxValue);

    /// <summary>Greedy packing of pieces under the maximum; a piece over the maximum is cut at sentences.</summary>
    private static IEnumerable<string> Pack(IEnumerable<string> pieces, int maximum, int beat)
    {
        var current = string.Empty;
        foreach (var piece in pieces.SelectMany(p => p.Length <= maximum ? [p] : Sentences(p, maximum, beat)))
        {
            if (current.Length + piece.Length > maximum && current.Length > 0)
            {
                yield return current;
                current = string.Empty;
            }

            current += piece;
        }

        if (current.Length > 0)
        {
            yield return current;
        }
    }

    private static IEnumerable<string> Sentences(string paragraph, int maximum, int beat)
    {
        var sentences = new List<string>();
        var start = 0;
        for (var i = 0; i < paragraph.Length; i++)
        {
            var endsSentence = paragraph[i] is '.' or '?' or '!'
                && (i + 1 == paragraph.Length || char.IsWhiteSpace(paragraph[i + 1]));
            if (!endsSentence)
            {
                continue;
            }

            var next = i + 1;
            while (next < paragraph.Length && char.IsWhiteSpace(paragraph[next]))
            {
                next++;
            }

            sentences.Add(paragraph[start..next]);
            start = next;
            i = next - 1;
        }

        if (start < paragraph.Length)
        {
            sentences.Add(paragraph[start..]);
        }

        foreach (var sentence in sentences.Where(s => s.Length > maximum))
        {
            throw new NarrationSplitRefusedException(
                $"a sentence of beat {beat} holds {sentence.Length} characters, above the configured maximum of {maximum}: \"{sentence[..Math.Min(80, sentence.Length)]}\"");
        }

        return sentences;
    }

    /// <summary>The text cut after each separator, the separator kept at the end of the piece before it.</summary>
    private static IEnumerable<string> Pieces(string text, string separator)
    {
        var start = 0;
        while (start < text.Length)
        {
            var at = text.IndexOf(separator, start, StringComparison.Ordinal);
            if (at < 0)
            {
                yield return text[start..];
                yield break;
            }

            var end = at + separator.Length;
            while (end < text.Length && text[end] == '\n')
            {
                end++;
            }

            yield return text[start..end];
            start = end;
        }
    }
}

/// <summary>The narration cannot be split as the design requires; the plan is refused, naming why.</summary>
public sealed class NarrationSplitRefusedException(string reason) : Exception(reason);

/// <summary>One segment of the rendered timeline: what it shows, from which frame, for how many frames.</summary>
public sealed record TimelineSegment(string Implements, int Beat, string Still, long StartFrame, long Frames);

/// <summary>
/// The beat timeline (the production change, decision D-011 of its design), a pure function. Each beat's span is
/// its MEASURED narration duration, from probes of its produced audio; its graphics and then its clip placeholders,
/// in shot-list order, divide the span EQUALLY, in whole frames at the render profile's rate, so every
/// placeholder sits inside its beat's measured span by construction. No rate assumption derives any duration.
/// </summary>
public static class BeatTimeline
{
    public static IReadOnlyList<TimelineSegment> Compose(
        IReadOnlyList<(int Beat, TimeSpan MeasuredDuration)> beats,
        IReadOnlyList<(string Implements, int Beat, string Still)> visuals,
        int framesPerSecond)
    {
        ArgumentNullException.ThrowIfNull(beats);
        ArgumentNullException.ThrowIfNull(visuals);
        ArgumentOutOfRangeException.ThrowIfLessThan(framesPerSecond, 1);

        var segments = new List<TimelineSegment>();
        var elapsed = TimeSpan.Zero;
        foreach (var (beat, duration) in beats.OrderBy(b => b.Beat))
        {
            var first = Frame(elapsed, framesPerSecond);
            elapsed += duration;
            var last = Frame(elapsed, framesPerSecond);
            var span = last - first;

            var shown = visuals.Where(v => v.Beat == beat).ToArray();
            if (shown.Length == 0)
            {
                throw new InvalidOperationException($"beat {beat} has no graphic and no clip position, so its span would show nothing");
            }

            for (var j = 0; j < shown.Length; j++)
            {
                var from = first + (span * j / shown.Length);
                var to = first + (span * (j + 1) / shown.Length);
                segments.Add(new TimelineSegment(shown[j].Implements, beat, shown[j].Still, from, to - from));
            }
        }

        return segments;
    }

    /// <summary>The frame an instant falls on, rounded to the nearest frame.</summary>
    public static long Frame(TimeSpan at, int framesPerSecond) =>
        (long)Math.Round(at.TotalSeconds * framesPerSecond, MidpointRounding.AwayFromZero);

    /// <summary>A frame count as the seconds a segment list states, invariant and exact to the microsecond.</summary>
    public static string Seconds(long frames, int framesPerSecond) =>
        ((decimal)frames / framesPerSecond).ToString("0.000000", CultureInfo.InvariantCulture);
}

/// <summary>Why a store refuses a production mode (the production change, decision D-005 of its design).</summary>
public sealed record StoreGuardVerdict(bool Admitted, IReadOnlyList<string> Refusals);

/// <summary>
/// The store guard (the production change, decision D-005 of its design), a pure function over the recorded
/// designation, the connected database's name and the configured company identity. Fake mode is admitted ONLY on a
/// demonstration store; metered mode ONLY on the company store whose database name matches the configured identity;
/// an absent designation refuses both; plan-only reads and writes nothing, and is admitted on any designated store. The own
/// mode (the own-voice change) is admitted on a demonstration store and on the company store matching the configured
/// identity; metered mode keeps its delivered verdicts here and is refused by the narration source rule instead.
/// </summary>
public static class StoreGuard
{
    public static StoreGuardVerdict Judge(ProductionMode mode, StoreDesignation? designation, string databaseName, string? companyIdentity)
    {
        var refusals = new List<string>();
        if (designation is null)
        {
            refusals.Add($"the store {databaseName} records no designation (the ninth schema resource and the preparation write it), so no production mode is admitted");
            return new StoreGuardVerdict(false, refusals);
        }

        switch (mode)
        {
            case ProductionMode.Fake when designation != StoreDesignation.Demonstration:
                refusals.Add($"fake mode runs only against a demonstration store, and {databaseName} is designated {designation}");
                break;
            case ProductionMode.Metered when designation != StoreDesignation.Company:
                refusals.Add($"metered mode books only into the company store, and {databaseName} is designated {designation}");
                break;
            case ProductionMode.Metered when string.IsNullOrWhiteSpace(companyIdentity):
                refusals.Add("metered mode needs the company store's identity configured, and none is");
                break;
            case ProductionMode.Metered when !string.Equals(companyIdentity, databaseName, StringComparison.Ordinal):
                refusals.Add($"metered mode books only into the configured company store {companyIdentity}, and this store is {databaseName}");
                break;

            // The own mode (the own-voice change, decision D-003 of its design): a demonstration store, or the company store
            // whose database name matches the configured identity. Which source narrates is the narration source rule's.
            case ProductionMode.Own when designation == StoreDesignation.Company && string.IsNullOrWhiteSpace(companyIdentity):
                refusals.Add("the own mode records into the company store only under the company store's configured identity, and none is configured");
                break;
            case ProductionMode.Own when designation == StoreDesignation.Company && !string.Equals(companyIdentity, databaseName, StringComparison.Ordinal):
                refusals.Add($"the own mode records into the configured company store {companyIdentity} only, and this store is {databaseName}");
                break;
        }

        return new StoreGuardVerdict(refusals.Count == 0, refusals);
    }
}
