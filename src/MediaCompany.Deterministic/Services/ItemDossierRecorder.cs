using MediaCompany.Application.Ports;
using MediaCompany.Domain.Audit;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Dossier;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// The recording service the item dossier is written through (decision D-004 of the accepted
/// design).
///
/// Nothing in the delivered production path wrote the item dossier, the stage outcomes, the supply
/// audits or the determinations, so none of the four could answer from recorded state however its
/// home and reader were built. This is the writer a production step writes through. Each call is
/// ONE transaction holding the recorded row and the audit entry that records it, so neither
/// commits without the other, and a row the datastore refuses leaves neither behind.
///
/// No production step calls it today; no stage handler is implemented. In the company's own store
/// every source therefore reads unmeasured until a step records into it, which is the empty-store
/// behaviour the readings are built to show rather than a gap filled with a placeholder.
/// </summary>
public sealed class ItemDossierRecorder
{
    /// <summary>The audit action for an opened dossier.</summary>
    public const string OpenedAction = "dossier.opened";

    /// <summary>The audit action for a recorded dossier component, whatever its kind.</summary>
    public const string RecordedAction = "dossier.component-recorded";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public ItemDossierRecorder(IUnitOfWork unitOfWork, IClock clock)
    {
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    /// <summary>
    /// Opens the dossier of one item version. The header is the observation boundary: from here on
    /// a component of this version with no row is an observed zero rather than an unmeasured one.
    /// </summary>
    public Task OpenAsync(ItemId item, ItemVersion version, WorkforceRole recordedBy, CancellationToken cancellationToken) =>
        WriteAsync(
            item, version, recordedBy, OpenedAction, "the dossier of this item version was opened",
            (writer, now) => writer.OpenAsync(item, version, now, cancellationToken),
            cancellationToken);

    public Task RecordStageAsync(
        ItemId item,
        ItemVersion version,
        StageEvidence evidence,
        WorkforceRole recordedBy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evidence);

        return WriteAsync(
            item, version, recordedBy, RecordedAction, $"stage {evidence.Stage} recorded {evidence.Outcome}",
            (writer, _) => writer.RecordStageAsync(item, version, evidence, cancellationToken),
            cancellationToken);
    }

    public Task RecordSupplyAuditAsync(
        ItemId item,
        ItemVersion version,
        SupplyAuditEntry entry,
        WorkforceRole recordedBy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return WriteAsync(
            item, version, recordedBy, RecordedAction,
            $"supply audit of the recorded subject term at library {entry.Library} recorded with fidelity {entry.Fidelity}",
            (writer, _) => writer.RecordSupplyAuditAsync(item, version, entry, cancellationToken),
            cancellationToken);
    }

    public Task RecordDeterminationAsync(
        ItemId item,
        ItemVersion version,
        DeterminationResolution resolution,
        WorkforceRole recordedBy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        return WriteAsync(
            item, version, recordedBy, RecordedAction,
            $"determination {resolution.Determination} recorded {resolution.Outcome}",
            (writer, _) => writer.RecordDeterminationAsync(item, version, resolution, cancellationToken),
            cancellationToken);
    }

    public Task RecordComponentAsync(
        ItemId item,
        ItemVersion version,
        DossierComponent component,
        WorkforceRole recordedBy,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(component);

        return WriteAsync(
            item, version, recordedBy, RecordedAction, $"{component.Kind} recorded",
            (writer, now) => writer.RecordComponentAsync(item, version, component, now, cancellationToken),
            cancellationToken);
    }

    /// <summary>
    /// One recorded row and its entry, in one transaction. The entry carries the item version, the
    /// component recorded and who recorded it, and none of the recorded content: the dossier's own
    /// row is where the content lives.
    /// </summary>
    private async Task WriteAsync(
        ItemId item,
        ItemVersion version,
        WorkforceRole recordedBy,
        string action,
        string reason,
        Func<IDossierWriter, DateTimeOffset, Task> write,
        CancellationToken cancellationToken)
    {
        var now = _clock.UtcNow;

        await using var transaction = await _unitOfWork.BeginAsync(cancellationToken).ConfigureAwait(false);
        await write(transaction.Dossiers, now).ConfigureAwait(false);
        await transaction.Audit.AppendAsync(
            new AuditEntryDraft
            {
                Actor = recordedBy.ToString(),
                Action = action,
                Subject = $"item:{item} version:{version}",
                Reason = reason,
                InputsReference = $"dossier:{item}:{version}",
                OutputsReference = "item dossier register",
                Decision = action,
                CostReference = "none",
                Risk = "none",
                RetentionClass = RetentionClass.OperationalRecord,
            },
            cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }
}
