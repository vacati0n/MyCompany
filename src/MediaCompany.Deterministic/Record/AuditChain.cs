using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MediaCompany.Domain.Audit;

namespace MediaCompany.Deterministic.Record;

/// <summary>
/// The tamper-evident chain over the append-only record (module M-007), under the name
/// <see cref="DeterministicTaskRegistry.AuditLogging"/>.
///
/// The chain is standard-library hashing over datastore write constraints, which is the
/// reuse-extended position the design records: the store refuses update and delete, and this
/// type supplies the chaining and the verification the amendment refusal rests on.
/// </summary>
public static class AuditChain
{
    /// <summary>The previous-entry hash at the head of an empty chain.</summary>
    public const string Genesis = "";

    /// <summary>
    /// Computes the hash of one entry over its full content and the previous entry's hash, so
    /// that altering any field of any entry invalidates every entry after it.
    /// </summary>
    public static string ComputeHash(
        AuditEntryId id,
        string actor,
        string action,
        string subject,
        DateTimeOffset occurredAt,
        string reason,
        string inputsReference,
        string outputsReference,
        string decision,
        string costReference,
        string risk,
        RetentionClass retentionClass,
        string previousEntryHash)
    {
        var builder = new StringBuilder();
        builder.Append(id.Value.ToString("D")).Append('\u001f');
        builder.Append(actor).Append('\u001f');
        builder.Append(action).Append('\u001f');
        builder.Append(subject).Append('\u001f');
        builder.Append(occurredAt.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)).Append('\u001f');
        builder.Append(reason).Append('\u001f');
        builder.Append(inputsReference).Append('\u001f');
        builder.Append(outputsReference).Append('\u001f');
        builder.Append(decision).Append('\u001f');
        builder.Append(costReference).Append('\u001f');
        builder.Append(risk).Append('\u001f');
        builder.Append(retentionClass.ToString()).Append('\u001f');
        builder.Append(previousEntryHash);

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
        return Convert.ToHexStringLower(bytes);
    }

    public static string ComputeHash(AuditEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return ComputeHash(
            entry.Id,
            entry.Actor,
            entry.Action,
            entry.Subject,
            entry.OccurredAt,
            entry.Reason,
            entry.InputsReference,
            entry.OutputsReference,
            entry.Decision,
            entry.CostReference,
            entry.Risk,
            entry.RetentionClass,
            entry.PreviousEntryHash);
    }

    /// <summary>
    /// Verifies a chain in order. Returns the index of the first entry whose recorded hash does
    /// not match its content, or -1 when the chain is intact.
    /// </summary>
    public static int FirstBrokenLink(IReadOnlyList<AuditEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var expectedPrevious = Genesis;
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            if (!string.Equals(entry.PreviousEntryHash, expectedPrevious, StringComparison.Ordinal))
            {
                return i;
            }

            if (!string.Equals(entry.EntryHash, ComputeHash(entry), StringComparison.Ordinal))
            {
                return i;
            }

            expectedPrevious = entry.EntryHash;
        }

        return -1;
    }
}
