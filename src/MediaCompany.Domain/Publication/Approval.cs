using MediaCompany.Domain.Authority;

namespace MediaCompany.Domain.Publication;

public enum ApprovalVerdict
{
    Approved = 1,
    SentBack = 2,
}

/// <summary>
/// One owner verdict, bound to an exact item version. An approval presented for another version
/// is refused (constraint C-016), which is why <see cref="ItemVersion"/> is part of the identity
/// rather than a recorded attribute.
///
/// Elapsed minutes are derived from the two recorded timestamps rather than entered, so the
/// figure is a measurement from the very first approval onward — the instrumentation risk RK-005
/// asks for, with no threshold proposed here because no supplied source establishes one (F-013).
/// </summary>
public sealed record Approval
{
    public Approval(
        ItemId item,
        ItemVersion itemVersion,
        string gate,
        WorkforceRole approver,
        ApprovalVerdict verdict,
        string reason,
        DateTimeOffset presentedAt,
        DateTimeOffset decidedAt)
    {
        if (string.IsNullOrWhiteSpace(gate))
        {
            throw new ArgumentException("An approval names the gate it was taken at.", nameof(gate));
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An approval carries its reason, including a send-back.", nameof(reason));
        }

        if (decidedAt < presentedAt)
        {
            throw new ArgumentException("An approval cannot be decided before it was presented.", nameof(decidedAt));
        }

        Item = item;
        ItemVersion = itemVersion;
        Gate = gate;
        Approver = approver;
        Verdict = verdict;
        Reason = reason;
        PresentedAt = presentedAt;
        DecidedAt = decidedAt;
    }

    public ItemId Item { get; }
    public ItemVersion ItemVersion { get; }
    public string Gate { get; }
    public WorkforceRole Approver { get; }
    public ApprovalVerdict Verdict { get; }
    public string Reason { get; }
    public DateTimeOffset PresentedAt { get; }
    public DateTimeOffset DecidedAt { get; }

    /// <summary>
    /// Derived from the two recorded timestamps, never entered. This is the baseline measure the
    /// clean-record threshold is later proposed from; no threshold is set here.
    /// </summary>
    public int ElapsedMinutes => (int)Math.Round((DecidedAt - PresentedAt).TotalMinutes, MidpointRounding.AwayFromZero);
}

/// <summary>
/// A copyright block. Mutated only by the actions the copyright role's closed set holds.
/// </summary>
public sealed record Block
{
    public Block(Guid id, ItemId item, WorkforceRole placedBy, string reason, AssetId? asset, bool open)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("A block names what is missing.", nameof(reason));
        }

        Id = id;
        Item = item;
        PlacedBy = placedBy;
        Reason = reason;
        Asset = asset;
        Open = open;
    }

    public Guid Id { get; }
    public ItemId Item { get; }
    public WorkforceRole PlacedBy { get; }
    public string Reason { get; }
    public AssetId? Asset { get; }
    public bool Open { get; }
}

/// <summary>
/// Evidence that the gate passed for an exact item version. Layer four of decision D-003 makes
/// this token a precondition of release-credential issuance, so a release credential is not
/// reachable before the gate passes.
/// </summary>
public sealed record GatePassToken(ItemId Item, ItemVersion ItemVersion, string Gate, DateTimeOffset IssuedAt);
