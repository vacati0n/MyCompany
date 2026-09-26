using MediaCompany.Domain.Rights;

namespace MediaCompany.Deterministic.Rights;

/// <summary>
/// Resolves whether the company was permitted to use an asset, and why (module M-015, plan tasks
/// T-007 and T-008), under the names <see cref="DeterministicTaskRegistry.AssetRightsLedger"/>
/// and <see cref="DeterministicTaskRegistry.LicenceJoinProof"/>.
///
/// Constraint C-022 is realized by the return type: the function returns
/// <see cref="PermissionVerdict"/>, whose only two members are Permitted and NotPermitted. An
/// absent, unverified or expired basis produces NotPermitted naming what is missing, so the query
/// cannot return an incomplete answer that reads as a complete one.
/// </summary>
public static class PermissionBasis
{
    /// <summary>
    /// Evaluates one asset against its channel's library registrations.
    /// </summary>
    public static PermissionVerdict Resolve(
        Asset asset,
        IReadOnlyList<LibraryRegistration> channelRegistrations,
        DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(asset);
        ArgumentNullException.ThrowIfNull(channelRegistrations);

        var missing = new List<string>();

        if (string.IsNullOrWhiteSpace(asset.Source))
        {
            missing.Add("source");
        }

        if (string.IsNullOrWhiteSpace(asset.Creator))
        {
            missing.Add("creator");
        }

        if (asset.LicenceType == LicenceType.Unspecified)
        {
            missing.Add("licence type");
        }

        if (string.IsNullOrWhiteSpace(asset.LicenceReference))
        {
            missing.Add("licence reference");
        }

        if (asset.CommercialUsePermitted is null)
        {
            missing.Add("commercial-use permission");
        }
        else if (asset.CommercialUsePermitted == false)
        {
            missing.Add("commercial use is not permitted by this licence");
        }

        if (asset.ModificationPermitted is null)
        {
            missing.Add("modification permission");
        }

        if (asset.AttributionRequirement is null)
        {
            missing.Add("attribution requirement");
        }

        if (asset.PlatformRestrictions is null)
        {
            missing.Add("platform restrictions");
        }

        if (string.IsNullOrWhiteSpace(asset.ProofOfLicenceReference))
        {
            missing.Add("proof of licence");
        }

        if (asset.AssessedRisk == AssessedRisk.Unspecified)
        {
            missing.Add("assessed risk");
        }

        if (asset.VerifiedBy is null)
        {
            missing.Add("verifier identity");
        }

        if (asset.VerifiedOn is null)
        {
            missing.Add("verification date");
        }

        if (asset.Expiry is { } expiry && expiry < asOf)
        {
            missing.Add($"licence expired on {expiry:yyyy-MM-dd}");
        }

        var registration = channelRegistrations.FirstOrDefault(
            r => string.Equals(r.Library, asset.Library, StringComparison.OrdinalIgnoreCase));

        if (registration is null)
        {
            missing.Add($"channel is not registered on library '{asset.Library}'");
        }

        if (missing.Count > 0)
        {
            return new PermissionVerdict.NotPermitted(asset.Id, missing);
        }

        return new PermissionVerdict.Permitted(
            asset.Id,
            $"{asset.LicenceType} licence {asset.LicenceReference} from {asset.Source}, proof {asset.ProofOfLicenceReference}",
            asset.VerifiedBy!.Value.ToString(),
            asset.VerifiedOn!.Value,
            registration!.RegisteredOn);
    }
}

/// <summary>
/// Whether an item may reach a releasable state on rights grounds (plan task T-008, acceptance
/// criteria AC-006 and AC-007).
/// </summary>
public sealed record ReleasableRightsVerdict(bool Releasable, IReadOnlyList<PermissionVerdict.NotPermitted> Blocking);

/// <summary>
/// The rights precondition of a releasable state. An item cannot reach one while any of its
/// assets lacks a verified, unexpired permission basis, or while its channel is unregistered on
/// a library one of its assets comes from; the block names what is missing.
/// </summary>
public static class ReleasableRightsPrecondition
{
    public static ReleasableRightsVerdict Evaluate(
        IReadOnlyList<Asset> assets,
        IReadOnlyList<LibraryRegistration> channelRegistrations,
        DateOnly asOf)
    {
        ArgumentNullException.ThrowIfNull(assets);

        var blocking = new List<PermissionVerdict.NotPermitted>();
        foreach (var asset in assets)
        {
            if (PermissionBasis.Resolve(asset, channelRegistrations, asOf) is PermissionVerdict.NotPermitted refused)
            {
                blocking.Add(refused);
            }
        }

        return new ReleasableRightsVerdict(blocking.Count == 0, blocking);
    }
}
