using MediaCompany.Domain.Dossier;

namespace MediaCompany.Deterministic.Production;

/// <summary>
/// The term-fidelity guard (criterion A-026, constraint C-016), a member of the rule-determined
/// set under <see cref="DeterministicTaskRegistry.TermFidelityComparison"/>.
///
/// The committed library answers fuzzily. It spelling-corrects, it substitutes, and it treats a
/// multi-word query as a relevance blend rather than an intersection. So a total it reports is
/// only a COUNT when it answered the term that was actually requested; otherwise it is an upper
/// bound of unknown looseness, and recording it as a count is how a pipeline ends up writing a
/// script against supply nobody established.
///
/// This type is the single place that decision is made, so it cannot be made differently by two
/// callers.
/// </summary>
public static class TermFidelityGuard
{
    /// <summary>
    /// Classifies a requested term against what the library echoed back.
    ///
    /// <paramref name="termAnswered"/> is null when the library's surface was not reached at all,
    /// which is a different fact from a loose answer and resolves as unknown rather than as a
    /// substitution.
    /// </summary>
    public static TermFidelity Classify(string requestedTerm, string? termAnswered)
    {
        if (string.IsNullOrWhiteSpace(requestedTerm))
        {
            return TermFidelity.Unknown;
        }

        // A multi-word phrase is inadmissible on its own shape, before any comparison. The library
        // blends relevance across the words, so even an exact echo of the phrase does not mean the
        // reported total counts assets showing the thing the phrase names.
        if (IsMultiWord(requestedTerm))
        {
            return TermFidelity.MultiWordPhrase;
        }

        if (termAnswered is null)
        {
            return TermFidelity.Unknown;
        }

        if (string.Equals(requestedTerm.Trim(), termAnswered.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            return TermFidelity.LiteralSingleToken;
        }

        // The library answered a different word. Whether that is a spelling correction or an
        // outright substitution is decided by edit distance: a near-neighbour is a correction, a
        // distant word is a substitution. Both yield no count; the distinction is recorded because
        // the two have different remedies.
        return EditDistance(requestedTerm.Trim(), termAnswered.Trim()) <= 2
            ? TermFidelity.SpellingCorrected
            : TermFidelity.Substituted;
    }

    /// <summary>
    /// Whether a reported total may be recorded as a count. Only a literal single-token answer
    /// qualifies. This is deliberately the narrowest of the possible rules.
    /// </summary>
    public static bool IsAdmissibleAsCount(TermFidelity fidelity) =>
        fidelity == TermFidelity.LiteralSingleToken;

    /// <summary>
    /// Builds the entry for a subject whose count could not be obtained, requiring the reason and
    /// the remedy that make an absence a resolution rather than a blank.
    /// </summary>
    public static SupplyAuditEntry NotObtained(
        string requestedTerm,
        string library,
        TermFidelity fidelity,
        CountUnobtainedReason reason,
        string whatWouldObtainIt,
        DateTimeOffset auditedAt,
        string? termAnswered = null,
        PriorObservation? prior = null)
    {
        if (reason == CountUnobtainedReason.None)
        {
            throw new ArgumentException(
                "An unobtained count names why it was not obtained; 'none' is not a reason.",
                nameof(reason));
        }

        if (string.IsNullOrWhiteSpace(whatWouldObtainIt))
        {
            throw new ArgumentException(
                "An unobtained count names what would obtain it, so the absence is actionable rather than final.",
                nameof(whatWouldObtainIt));
        }

        return new SupplyAuditEntry
        {
            RequestedTerm = requestedTerm,
            Library = library,
            Fidelity = fidelity,
            TermAnswered = termAnswered,
            Count = null,
            UnobtainedReason = reason,
            WhatWouldObtainIt = whatWouldObtainIt,
            Prior = prior,
            AuditedAt = auditedAt,
        };
    }

    private static bool IsMultiWord(string term) =>
        term.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length > 1;

    private static int EditDistance(string a, string b)
    {
        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var substitution = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitution);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
