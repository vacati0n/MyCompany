using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MediaCompany.Domain.Publication;

/// <summary>
/// The declared destination (module M-028).
///
/// The destination is a VALUE supplied as a parameter of every publishing step, taking its place
/// beside the subject, the pillars, the treatment conditions and the library set. It carries no
/// endpoint, no address and no credential reference, because nothing in this build reaches one:
/// it names where a publication WOULD go, which is what the attempt record has to answer, and
/// nothing that could be dialled.
/// </summary>
public sealed record DestinationDescriptor
{
    public DestinationDescriptor(string platform, string channelHandle, string locale)
    {
        if (string.IsNullOrWhiteSpace(platform))
        {
            throw new ArgumentException("A destination names its platform.", nameof(platform));
        }

        if (string.IsNullOrWhiteSpace(channelHandle))
        {
            throw new ArgumentException("A destination names the channel it addresses.", nameof(channelHandle));
        }

        if (string.IsNullOrWhiteSpace(locale))
        {
            throw new ArgumentException("A destination names its locale.", nameof(locale));
        }

        Platform = platform;
        ChannelHandle = channelHandle;
        Locale = locale;
    }

    public string Platform { get; }
    public string ChannelHandle { get; }
    public string Locale { get; }

    /// <summary>The stable text the dispatch key is derived over.</summary>
    public string Canonical => $"{Platform}|{ChannelHandle}|{Locale}";
}

/// <summary>
/// The settings a publication would carry. Values only: visibility, audience designation and
/// comment policy are recorded so the attempt is answerable, and none of them is read by anything
/// that acts.
/// </summary>
public sealed record PublicationSettings
{
    public required string Visibility { get; init; }
    public required bool MadeForKids { get; init; }
    public required string CommentPolicy { get; init; }
    public required string CategoryName { get; init; }

    public string Canonical =>
        $"{Visibility}|{MadeForKids}|{CommentPolicy}|{CategoryName}";
}

/// <summary>
/// The publication dispatch key (decision D-004).
///
/// DERIVED from the item and its exact version, never generated per attempt. That is what makes a
/// retry resolve to the existing record instead of producing a second one: two attempts for one
/// item version cannot produce two keys, because neither attempt contributes anything to the key.
/// </summary>
public readonly record struct DispatchKey
{
    private DispatchKey(string value) => Value = value;

    public string Value { get; }

    /// <summary>
    /// Derives the key. The inputs are the item and the exact version and nothing else — no clock,
    /// no attempt ordinal, no random source — so the function is total, pure and repeatable.
    /// </summary>
    public static DispatchKey Derive(ItemId item, ItemVersion version)
    {
        var material = string.Create(
            CultureInfo.InvariantCulture, $"publication-dispatch:{item.Value:D}:{version.Value}");
        var digest = SHA256.HashData(Encoding.UTF8.GetBytes(material));
        return new DispatchKey(Convert.ToHexStringLower(digest));
    }

    public override string ToString() => Value;
}

/// <summary>
/// The composed dispatch descriptor (module M-022): everything a publication would need, assembled,
/// validated, version-keyed and ready to be persisted.
///
/// THIS IS THE STOPPING SURFACE. The descriptor, persisted with its queue entry, is where the
/// end-to-end exercise comes to rest. Nothing consumes it to act, because nothing that acts exists.
/// </summary>
public sealed record DispatchDescriptor
{
    public required ItemId Item { get; init; }
    public required ItemVersion Version { get; init; }
    public required DestinationDescriptor Destination { get; init; }
    public required PublicationSettings Settings { get; init; }

    /// <summary>The screened published-facing surfaces, held only after they passed screening.</summary>
    public required IReadOnlyList<SurfaceRecord> Surfaces { get; init; }

    /// <summary>
    /// The planned publication time. An ATTRIBUTE of the descriptor and nothing more: no component
    /// reads a due time, so there is nothing an elapsed planned time could trigger.
    /// </summary>
    public required DateTimeOffset PlannedAt { get; init; }

    /// <summary>The approval this dispatch rests on, bound to the exact version above.</summary>
    public required GatePassToken GatePass { get; init; }

    public required DispatchKey Key { get; init; }
    public required DateTimeOffset ComposedAt { get; init; }

    /// <summary>
    /// The metadata and settings the attempt record answers with. Rendered once here so the record
    /// and the descriptor cannot drift apart.
    /// </summary>
    public string MetadataAndSettingsDigest => DigestOf(Destination, Settings, Surfaces);

    /// <summary>
    /// The digest rule, stated once. A REFUSED attempt answers "with which metadata and settings"
    /// too, and it has no descriptor to read the digest from, so the rule is exposed here rather
    /// than restated at the caller that persists a refusal: the refused attempt and a later
    /// composed one for the same inputs then carry the same digest by construction.
    /// </summary>
    public static string DigestOf(
        DestinationDescriptor destination,
        PublicationSettings settings,
        IReadOnlyList<SurfaceRecord> surfaces)
    {
        ArgumentNullException.ThrowIfNull(destination);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(surfaces);

        var material = new StringBuilder();
        material.Append(destination.Canonical).Append('\n');
        material.Append(settings.Canonical).Append('\n');
        foreach (var surface in surfaces.OrderBy(s => s.Kind))
        {
            material.Append(surface.Kind).Append('=').Append(surface.Content).Append('\n');
        }

        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(material.ToString())));
    }
}

/// <summary>
/// Why a dispatch was refused. Every member of this set is a STRUCTURAL reason or a named gate
/// condition; there is no member meaning "a setting said no", because no setting is consulted.
/// </summary>
public enum DispatchRefusalReason
{
    /// <summary>The publication gate refused; its own named refusal is carried in the detail.</summary>
    GateRefused = 1,

    /// <summary>One or more of the three conditions of first publication does not hold.</summary>
    FirstPublicationConditionUnmet = 2,

    /// <summary>A published-facing surface failed screening.</summary>
    SurfaceScreeningFailed = 3,

    /// <summary>The actor holds no publication-dispatch action.</summary>
    ActorHoldsNoDispatchAction = 4,

    /// <summary>The approval presented binds to a version other than the one being dispatched.</summary>
    ApprovalBoundToAnotherVersion = 5,

    /// <summary>
    /// The composed descriptor is the last thing this build can produce. There is no component
    /// that effects a dispatch, so a caller asking for one is refused with the structural reason.
    /// </summary>
    NoEffectingComponentExists = 6,
}

/// <summary>
/// The dispatch outcome: a CLOSED union with exactly two cases.
///
/// STRUCTURAL ABSENCE ONE of the five the no-publish property rests on. There is no Effected case,
/// so no caller can obtain a value meaning uploaded and no code can be written that returns one.
/// Adding a third case is a breaking change to a closed union — it cannot be done by configuration,
/// and it cannot be done without the Review Gate seeing it.
///
/// The private constructor is what closes the union: no type outside this file can derive from it.
/// </summary>
public abstract record DispatchOutcome
{
    private DispatchOutcome()
    {
    }

    /// <summary>
    /// The dispatch composed, validated and came to rest. This is the furthest any caller gets.
    /// The descriptor is persisted with its queue entry, and nothing consumes it afterwards.
    /// </summary>
    public sealed record Composed(DispatchDescriptor Descriptor) : DispatchOutcome;

    /// <summary>The dispatch refused, naming the structural or gate reason.</summary>
    public sealed record Refused(DispatchRefusalReason Reason, string Detail) : DispatchOutcome;

    /// <summary>
    /// The case count of this union, read by the demonstration that asserts no effecting case was
    /// added. Two, and the demonstration fails at two-plus-one.
    /// </summary>
    public static IReadOnlyList<Type> Cases { get; } =
    [
        typeof(Composed),
        typeof(Refused),
    ];
}

