using MediaCompany.Domain.Rights;

namespace MediaCompany.Domain.Registry;

/// <summary>
/// One configured attribute of one channel, in exactly one of two shapes (decision D-001 of the
/// multi-channel design).
///
/// <see cref="Recorded"/> carries the value as it was recorded, verbatim, with its version and the
/// start of its validity. <see cref="NotRecorded"/> carries NO VALUE FIELD of any kind: it names the
/// key and the scope that were looked for, so an attribute nobody recorded can never be read as a
/// default, a blank or anything a reader could mistake for a value. The union is closed by a private
/// constructor, so a third shape is a code change a reviewer sees.
/// </summary>
public abstract record ChannelAttribute
{
    private ChannelAttribute(string key, string scope)
    {
        Key = key;
        Scope = scope;
    }

    /// <summary>The configuration key the attribute is held under.</summary>
    public string Key { get; }

    /// <summary>The channel scope the key was looked for under.</summary>
    public string Scope { get; }

    /// <summary>A value recorded in the temporal configuration register and in force at the read.</summary>
    public sealed record Recorded : ChannelAttribute
    {
        internal Recorded(string key, string scope, string value, int version, DateTimeOffset validFrom)
            : base(key, scope)
        {
            Value = value;
            Version = version;
            ValidFrom = validFrom;
        }

        /// <summary>The value exactly as recorded. No component interprets it.</summary>
        public string Value { get; }

        public int Version { get; }

        public DateTimeOffset ValidFrom { get; }
    }

    /// <summary>No value is recorded in force for this key under this channel's scope.</summary>
    public sealed record NotRecorded : ChannelAttribute
    {
        internal NotRecorded(string key, string scope)
            : base(key, scope)
        {
        }

        /// <summary>What was looked for and where, stated rather than defaulted.</summary>
        public string Statement =>
            $"not recorded: no value is in force for {Key} under scope {Scope}, and none stands in its place";
    }

    public static ChannelAttribute RecordedValue(string key, string scope, string value, int version, DateTimeOffset validFrom)
    {
        Require(key, scope);
        ArgumentNullException.ThrowIfNull(value);
        return new Recorded(key, scope, value, version, validFrom);
    }

    public static ChannelAttribute Absent(string key, string scope)
    {
        Require(key, scope);
        return new NotRecorded(key, scope);
    }

    /// <summary>The rendering. The two shapes render apart by construction.</summary>
    public string Describe() => this switch
    {
        Recorded recorded => $"{recorded.Value} (version {recorded.Version}, in force from {recorded.ValidFrom:O})",
        NotRecorded absent => absent.Statement,
        _ => throw new InvalidOperationException("Unreachable: a channel attribute has two shapes."),
    };

    private static void Require(string key, string scope)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(scope))
        {
            throw new ArgumentException("A channel attribute names the key and the scope it was looked for under.");
        }
    }
}

/// <summary>
/// A channel's risk profile: the channel's own recorded risk notes, and the REQUIRED statement that
/// the channel shares the company's single payee and payment account with every other channel of
/// the company.
///
/// The statement is a construction invariant composed here, never a value: no configuration key
/// reaches it, no record shape holds a per-channel payee, and the constructor is private, so it
/// cannot be configured away, blanked or replaced by a caller.
/// </summary>
public sealed record ChannelRiskProfile
{
    private ChannelRiskProfile(ChannelAttribute riskNotes, string sharedPayeeStatement)
    {
        RiskNotes = riskNotes;
        SharedPayeeStatement = sharedPayeeStatement;
    }

    /// <summary>The channel's own risk notes, recorded or not recorded.</summary>
    public ChannelAttribute RiskNotes { get; }

    /// <summary>That the channel shares one payee and one payment account with every channel of its company.</summary>
    public string SharedPayeeStatement { get; }

    /// <summary>
    /// The risk profile of a channel whose company the channel register holds
    /// <paramref name="channelsOfCompany"/> channels for, the channel itself included.
    /// </summary>
    public static ChannelRiskProfile For(ChannelAttribute riskNotes, CompanyId company, int channelsOfCompany)
    {
        ArgumentNullException.ThrowIfNull(riskNotes);

        if (channelsOfCompany < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(channelsOfCompany), "A channel's company holds at least that channel in the register.");
        }

        return new ChannelRiskProfile(riskNotes, SharedPayeeStatementFor(company, channelsOfCompany));
    }

    /// <summary>The statement, authored once.</summary>
    public static string SharedPayeeStatementFor(CompanyId company, int channelsOfCompany) =>
        $"this channel shares the single payee and the single payment account of company {company} with every "
        + $"other channel of that company; the channel register holds {channelsOfCompany} channel(s) for it, and "
        + "no channel holds a payee or a payment account of its own, so the payment-account condition reads "
        + "identically for every one of them";
}

/// <summary>
/// One channel's profile, composed from recorded state alone: the register row, the library
/// registration register and the channel's values in the temporal configuration register.
///
/// Nothing on a profile is a default. A configured attribute nobody recorded reads not recorded,
/// naming the key and the scope looked for; a channel with no library registration holds an empty
/// list, which the rights precondition reads as unregistered and refuses. No member of a profile
/// is read by any gate state, refusal, precondition, payee, trigger or routing decision.
/// </summary>
public sealed record ChannelProfile
{
    public required ChannelId Channel { get; init; }
    public required CompanyId Company { get; init; }
    public required string Platform { get; init; }

    /// <summary>The channel's language, read from the channel register row, where it is held.</summary>
    public required string Language { get; init; }

    /// <summary>The register row's registered flag, as recorded.</summary>
    public required bool Registered { get; init; }

    /// <summary>The channel's library registrations, read from the library registration register.</summary>
    public required IReadOnlyList<LibraryRegistration> LibraryRegistrations { get; init; }

    public required ChannelAttribute Audience { get; init; }
    public required ChannelAttribute Brand { get; init; }
    public required ChannelAttribute Voice { get; init; }
    public required ChannelAttribute Tone { get; init; }
    public required ChannelAttribute VisualIdentity { get; init; }
    public required ChannelAttribute ContentStrategy { get; init; }
    public required ChannelAttribute Schedule { get; init; }

    public required ChannelRiskProfile Risk { get; init; }

    /// <summary>Every configured attribute, in the order the closed key set declares them.</summary>
    public IReadOnlyList<ChannelAttribute> ConfiguredAttributes =>
        [Audience, Brand, Voice, Tone, VisualIdentity, ContentStrategy, Schedule, Risk.RiskNotes];
}
