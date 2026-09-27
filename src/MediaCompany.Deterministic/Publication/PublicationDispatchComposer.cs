using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;

namespace MediaCompany.Deterministic.Publication;

/// <summary>
/// The publication dispatch composer (module M-022).
///
/// This type is the whole of the publishing path's decision surface, and it is a TOTAL FUNCTION
/// over a closed outcome union. <see cref="Compose"/> returns either a composed descriptor or a
/// named refusal, and there is no third thing it can return, because
/// <see cref="DispatchOutcome"/> has no effected case for it to construct.
///
/// THE STOPPING SURFACE. A composed descriptor, validated and version-keyed, persisted with its
/// queue entry, is where the end-to-end exercise comes to rest. Nothing consumes it afterwards:
/// this assembly holds no dependency on any transport, on the resolution boundary or on the
/// credential broker, so a destination egress is not merely forbidden here, it is inexpressible.
/// </summary>
public static class PublicationDispatchComposer
{
    /// <summary>
    /// Composes a dispatch for one item version, or refuses with the structural reason.
    ///
    /// The order of the checks is deliberate: authority, then the gate verdict including the three
    /// conditions of first publication, then the version re-evaluation, then screening. Each
    /// refuses by its own name, so a caller repairing one does not discover the next only after
    /// the first is fixed — the refusal says which of them stopped it.
    /// </summary>
    public static DispatchOutcome Compose(
        WorkforceRole actor,
        GateVerdict gateVerdict,
        ItemId item,
        ItemVersion version,
        DestinationDescriptor destination,
        PublicationSettings settings,
        IReadOnlyList<SurfaceRecord> surfaces,
        DateTimeOffset plannedAt,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(gateVerdict);
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(surfaces);

        // The closed action set holds a dispatch action and NO egress action, so this check is the
        // furthest authority can be asked about: there is no action value meaning "may send".
        if (!ActionSet.Holds(actor, ActionKind.PublicationDispatch))
        {
            return new DispatchOutcome.Refused(
                DispatchRefusalReason.ActorHoldsNoDispatchAction,
                $"{actor} holds no PublicationDispatch action. The closed action set holds no egress "
                + "action at all, so no role holds authority to send.");
        }

        if (gateVerdict is GateVerdict.Refused refused)
        {
            var reason = refused.Reason is
                GateRefusal.FirstPublicationLibraryRegistrationUnmet or
                GateRefusal.FirstPublicationPaymentAccountUnmet or
                GateRefusal.FirstPublicationTwoStepVerificationUnmet
                ? DispatchRefusalReason.FirstPublicationConditionUnmet
                : DispatchRefusalReason.GateRefused;

            return new DispatchOutcome.Refused(reason, $"{refused.Reason}: {refused.Detail}");
        }

        var passed = (GateVerdict.Passed)gateVerdict;

        // Re-evaluated AT DISPATCH TIME against the exact version being dispatched. A retry of an
        // attempt recorded against an earlier version cannot ride that version's approval.
        var drift = GatePredicates.RefuseOnVersionDrift(passed.Token, item, version);
        if (drift is not null)
        {
            return new DispatchOutcome.Refused(
                DispatchRefusalReason.ApprovalBoundToAnotherVersion, drift.Detail);
        }

        var refusedSurfaces = surfaces.Where(s => !s.IsHoldable).ToArray();
        if (refusedSurfaces.Length > 0)
        {
            return new DispatchOutcome.Refused(
                DispatchRefusalReason.SurfaceScreeningFailed,
                string.Join("; ", refusedSurfaces.Select(s => $"{s.Kind}: {s.Reason}")));
        }

        var boundToAnotherVersion = surfaces.Where(s => !s.Version.Equals(version)).ToArray();
        if (boundToAnotherVersion.Length > 0)
        {
            return new DispatchOutcome.Refused(
                DispatchRefusalReason.ApprovalBoundToAnotherVersion,
                $"{boundToAnotherVersion.Length} surface(s) were produced from a version other than "
                + $"{version}; a surface is bound to the exact version it was produced from.");
        }

        var descriptor = new DispatchDescriptor
        {
            Item = item,
            Version = version,
            Destination = destination,
            Settings = settings,
            Surfaces = surfaces,
            PlannedAt = plannedAt,
            GatePass = passed.Token,

            // DERIVED from the item and its exact version. Never generated, so two attempts for
            // one version cannot produce two keys and a retry resolves to the existing record.
            Key = DispatchKey.Derive(item, version),
            ComposedAt = now,
        };

        return new DispatchOutcome.Composed(descriptor);
    }

    /// <summary>
    /// The attempt record for an outcome, written for refused and composed attempts alike.
    ///
    /// A refused attempt carries all five audit answers exactly as a composed one does. The refused
    /// case is the one most likely to be recorded thinly, and it is the one an auditor asks about.
    /// </summary>
    public static AttemptRecord RecordFor(
        DispatchOutcome outcome,
        ItemId item,
        ItemVersion version,
        DestinationDescriptor destination,
        string approvedBy,
        string metadataAndSettingsDigest,
        DateTimeOffset attemptedAt)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(destination);

        return outcome switch
        {
            DispatchOutcome.Composed composed => new AttemptRecord(
                Guid.NewGuid(),
                composed.Descriptor.Key,
                item,
                version,
                destination.Canonical,
                attemptedAt,
                composed.Descriptor.MetadataAndSettingsDigest,
                approvedBy,
                refused: false,
                refusalReason: string.Empty,
                refusalDetail: string.Empty),

            DispatchOutcome.Refused refused => new AttemptRecord(
                Guid.NewGuid(),

                // The key is derivable even for a refused attempt, because it depends on the item
                // and the version alone. A refused attempt is therefore filed against the same
                // dispatch a later successful composition would produce.
                DispatchKey.Derive(item, version),
                item,
                version,
                destination.Canonical,
                attemptedAt,
                metadataAndSettingsDigest,

                // Answer five stays answerable even when there was no approval: "none" is an
                // answer, and an empty field is not.
                string.IsNullOrWhiteSpace(approvedBy) ? "none (the attempt was refused)" : approvedBy,
                refused: true,
                refusalReason: refused.Reason.ToString(),
                refusalDetail: refused.Detail),

            _ => throw new InvalidOperationException(
                "Unreachable: the dispatch outcome union has exactly two cases."),
        };
    }
}
