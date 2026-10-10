using MediaCompany.Domain.Accounting;

namespace MediaCompany.Domain.Capabilities;

/// <summary>
/// What a caller asks for. Per decision D-012 the request declares what is needed and never who
/// provides it: there is no provider field and no model field, so a caller cannot express a
/// provider preference and the routing position owns the choice.
/// </summary>
public sealed record CapabilityRequest
{
    public CapabilityRequest(
        CapabilityClass capability,
        ReasoningTier reasoningTier,
        QualityRating qualityFloor,
        ContextCapacity contextRequirement,
        Money costCeiling,
        Criticality criticality,
        Attribution attribution,
        EstimatedUnits estimatedUnits,
        TimeSpan holdTimeout,
        ReducedFloorPolicy reducedFloorPolicy)
    {
        ArgumentNullException.ThrowIfNull(attribution);
        ArgumentNullException.ThrowIfNull(estimatedUnits);
        ArgumentNullException.ThrowIfNull(reducedFloorPolicy);

        if (criticality == Criticality.Critical && reducedFloorPolicy.Permitted)
        {
            throw new ArgumentException(
                "A critical request may not carry a reduced-floor policy; resolution step 6 holds it instead.",
                nameof(reducedFloorPolicy));
        }

        Capability = capability;
        ReasoningTier = reasoningTier;
        QualityFloor = qualityFloor;
        ContextRequirement = contextRequirement;
        CostCeiling = costCeiling;
        Criticality = criticality;
        Attribution = attribution;
        EstimatedUnits = estimatedUnits;
        HoldTimeout = holdTimeout;
        ReducedFloorPolicy = reducedFloorPolicy;
    }

    public CapabilityClass Capability { get; }
    public ReasoningTier ReasoningTier { get; }
    public QualityRating QualityFloor { get; }
    public ContextCapacity ContextRequirement { get; }
    public Money CostCeiling { get; }
    public Criticality Criticality { get; }
    public Attribution Attribution { get; }
    public EstimatedUnits EstimatedUnits { get; }
    public TimeSpan HoldTimeout { get; }
    public ReducedFloorPolicy ReducedFloorPolicy { get; }

    /// <summary>
    /// The representative task this request performs, or none (the AI-economics change, decision D-003
    /// of its design). Optional, and absent by default, so every delivered construction compiles and
    /// behaves as before: with no task class there is nothing to compare evidence by, and selection
    /// returns the configured ordering, labelled as such and naming the absent task class.
    /// </summary>
    public TaskClass? TaskClass { get; init; }

    /// <summary>
    /// What the vendor is asked to produce (the production change, decision D-002 of its design): the
    /// narration text, the prompt or the image prompt, typed, so no adapter invents a request body. Optional,
    /// and absent by default, so every delivered construction compiles and behaves as before. It names no
    /// provider and no model: routing still owns the choice.
    /// </summary>
    public CapabilityPayload? Payload { get; init; }
}

/// <summary>
/// The unit counts a request expects to consume, used by the pre-flight ceiling filter of resolution step 4
/// and, under a recorded item cap, as the WORST CASE the admission reserves (the production change, decision
/// D-006 of its design). The recorded consumption is what the operation actually returned, or this worst case
/// where the vendor's charge is not known.
/// </summary>
public sealed record EstimatedUnits(long InputUnits, long OutputUnits, long CachedUnits)
{
    public static EstimatedUnits None { get; } = new(0, 0, 0);

    /// <summary>Characters the vendor bills the request by (the production change, decision D-007). Zero by default.</summary>
    public long CharacterUnits { get; init; }

    /// <summary>Images the vendor bills the request by (the production change, decision D-007). Zero by default.</summary>
    public long ImageUnits { get; init; }

    /// <summary>The same counts as recorded units, which is what a booking at the worst case records.</summary>
    public UnitCounts AsUnits() =>
        new(InputUnits, OutputUnits, CachedUnits, 0) { CharacterUnits = CharacterUnits, ImageUnits = ImageUnits };
}

/// <summary>
/// The typed body of a capability request (the production change, decision D-002 of its design). A closed
/// union: each case carries exactly what one vendor contract needs, and nothing a caller could fill with a
/// provider or a model.
/// </summary>
public abstract record CapabilityPayload
{
    private CapabilityPayload()
    {
    }

    /// <summary>Narration: the exact text to speak, the voice the account names and the audio format asked for.</summary>
    public sealed record Narration(string Text, string Voice, string Format) : CapabilityPayload
    {
        /// <summary>The characters the request carries, which is what a character-billed vendor bills.</summary>
        public long Characters => Text.Length;

        /// <summary>States the request without its text.</summary>
        public override string ToString() => $"narration of {Characters} characters, voice {Voice}, format {Format}";
    }

    /// <summary>A messages request: one prompt and the bound on the units the reply may take.</summary>
    public sealed record Messages(string Prompt, long MaxOutputUnits) : CapabilityPayload
    {
        /// <summary>States the request without its prompt.</summary>
        public override string ToString() => $"messages request of {Prompt.Length} characters, at most {MaxOutputUnits} output units";
    }

    /// <summary>A still image: one prompt and the size asked for.</summary>
    public sealed record StillImage(string Prompt, string Size) : CapabilityPayload
    {
        /// <summary>States the request without its prompt.</summary>
        public override string ToString() => $"still image request of {Prompt.Length} characters at {Size}";
    }
}

/// <summary>What kind of content a completed call produced (the production change, decision D-001).</summary>
public enum ContentKind
{
    Text = 1,
    Audio = 2,
    Image = 3,
}

/// <summary>
/// The content a completed capability call produced (the production change, decision D-001 of its design):
/// its kind, the media type the vendor declared, the bytes as ONE read-only buffer exactly as received, their
/// length and the SHA-256 hash of that same buffer. No second copy is made and no other buffer is hashed, so
/// the recorded length and hash are the received bytes'.
/// </summary>
public sealed record ProducedContent
{
    public ProducedContent(ContentKind kind, string mediaType, ReadOnlyMemory<byte> bytes)
    {
        if (string.IsNullOrWhiteSpace(mediaType))
        {
            throw new ArgumentException("Produced content carries the media type its vendor declared.", nameof(mediaType));
        }

        Kind = kind;
        MediaType = mediaType;
        Bytes = bytes;
        Length = bytes.Length;
        Sha256 = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(bytes.Span));
    }

    public ContentKind Kind { get; }

    public string MediaType { get; }

    public ReadOnlyMemory<byte> Bytes { get; }

    public long Length { get; }

    /// <summary>Lowercase hexadecimal SHA-256 of <see cref="Bytes"/>.</summary>
    public string Sha256 { get; }

    /// <summary>States the content without its bytes, so no log line ever carries what was produced.</summary>
    public override string ToString() => $"{Kind} {MediaType}, {Length} bytes, sha256 {Sha256}";
}

/// <summary>
/// Resolution step 6 never downgrades. A non-critical request may carry its own declared policy
/// permitting a reduced floor; when it is used the reduction is recorded and the item is barred
/// from a releasable state until a role re-verifies it.
/// </summary>
public sealed record ReducedFloorPolicy
{
    private ReducedFloorPolicy(bool permitted, QualityRating? reducedFloor)
    {
        Permitted = permitted;
        ReducedFloor = reducedFloor;
    }

    public static ReducedFloorPolicy Forbidden { get; } = new(false, null);

    public static ReducedFloorPolicy AllowDownTo(QualityRating reducedFloor) => new(true, reducedFloor);

    public bool Permitted { get; }
    public QualityRating? ReducedFloor { get; }
}
