namespace MediaCompany.Domain.Rights;

/// <summary>
/// The answer to "was the company permitted to use this asset". An absent, unverified or expired
/// basis resolves as <see cref="NotPermitted"/>, and an unregistered channel blocks a releasable
/// state (constraint C-022). There is no third value a reader could mistake for permission.
/// </summary>
public abstract record PermissionVerdict
{
    private PermissionVerdict()
    {
    }

    public sealed record Permitted(
        AssetId Asset,
        string Basis,
        string VerifiedByRole,
        DateOnly VerifiedOn,
        DateOnly RegisteredOn) : PermissionVerdict;

    /// <summary>
    /// Names what is missing, so the block placed on the item can name the asset and the missing
    /// basis, as acceptance criterion AC-006 requires.
    /// </summary>
    public sealed record NotPermitted(AssetId Asset, IReadOnlyList<string> MissingOrFailing) : PermissionVerdict;
}
