using MediaCompany.Domain;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;

namespace MediaCompany.Capability;

/// <summary>Why an incurred provider attempt could not be recorded after its admission transaction was lost.</summary>
public enum IncurredAttemptRecordingFailure
{
    /// <summary>
    /// A statement of the recording waited on a row hold for longer than the recording's command timeout,
    /// which exceeds the provider-call bound, and was cut there.
    /// </summary>
    RecordingTimedOut = 1,

    /// <summary>The datastore refused or failed the recording for another reason, carried as the inner exception.</summary>
    RecordingFailed = 2,
}

/// <summary>
/// A provider attempt whose cost is incurred could not be recorded, neither on its admission transaction,
/// which was lost after the call, nor on the fresh recording transaction (the AI-economics change, second
/// correction cycle). It never surfaces unnamed: it carries its reason, the one operation identifier the
/// attempt is recorded under, and the facts a later recording needs. Where the lost transaction's commit
/// did reach the datastore, the attempt is recorded under that identifier, and nowhere else.
/// </summary>
public sealed class IncurredAttemptNotRecordedException : Exception
{
    public IncurredAttemptNotRecordedException(
        IncurredAttemptRecordingFailure reason,
        OperationId operation,
        Attribution attribution,
        RouteId route,
        ModelId model,
        UnitCounts units,
        DateTimeOffset admittedAt,
        Exception lost,
        Exception failure)
        : base(
            $"{reason}: the incurred attempt {operation} on route {route} ({model}), admitted at {admittedAt:O}, "
            + $"was not recorded; its admission transaction was lost ({lost?.GetType().Name}) and its recording failed "
            + $"({failure?.GetType().Name}).",
            failure)
    {
        ArgumentNullException.ThrowIfNull(attribution);
        ArgumentNullException.ThrowIfNull(units);
        ArgumentNullException.ThrowIfNull(lost);
        Reason = reason;
        Operation = operation;
        Attribution = attribution;
        Route = route;
        Model = model;
        Units = units;
        AdmittedAt = admittedAt;
        Lost = lost;
    }

    public IncurredAttemptRecordingFailure Reason { get; }

    /// <summary>The one identifier the attempt is recorded under, minted before the provider call.</summary>
    public OperationId Operation { get; }

    public Attribution Attribution { get; }

    public RouteId Route { get; }

    public ModelId Model { get; }

    /// <summary>The units the provider reported for the attempt.</summary>
    public UnitCounts Units { get; }

    /// <summary>The booking instant the lost admission reserved.</summary>
    public DateTimeOffset AdmittedAt { get; }

    /// <summary>What ended the admission transaction after the call.</summary>
    public Exception Lost { get; }
}
