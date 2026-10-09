using MediaCompany.Application.Ports;
using MediaCompany.Domain.Configuration;
using MediaCompany.Domain.Registry;

namespace MediaCompany.Deterministic.Services;

/// <summary>
/// The one composing site of a channel's profile (the multi-channel change, decision D-001 of its
/// design), and the ONLY component that reads a channel configuration key.
///
/// A profile is composed from recorded state alone: the channel register row, which holds the
/// channel's company, platform and language; the library registration register, which holds the
/// channel's registrations; and the channel's values in the temporal configuration register, each
/// under its key and the channel's own scope. Every configured attribute reads RECORDED, with its
/// value, version and validity start, or NOT RECORDED, naming the key and the scope looked for, and
/// never a default. The risk profile carries the required shared-payee statement, which no key
/// reaches.
///
/// Values are returned verbatim and interpreted by nothing: no profile member is read as a due
/// time, a trigger, a routing target, a gate state, a refusal, a precondition or a payee, and a
/// channel brought into being by register and configuration rows is therefore neither live nor
/// routed to. The service writes nothing and records no value.
/// </summary>
public sealed class ChannelProfileService
{
    private readonly IOperatingRegisters _registers;
    private readonly IAssetLedger _assets;
    private readonly IConfigurationStore _configuration;
    private readonly IClock _clock;

    public ChannelProfileService(
        IOperatingRegisters registers,
        IAssetLedger assets,
        IConfigurationStore configuration,
        IClock clock)
    {
        _registers = registers;
        _assets = assets;
        _configuration = configuration;
        _clock = clock;
    }

    /// <summary>The profile of every channel the register holds, in the register's order.</summary>
    public async Task<IReadOnlyList<ChannelProfile>> ProfilesAsync(CancellationToken cancellationToken)
    {
        var channels = await _registers.ChannelsAsync(cancellationToken).ConfigureAwait(false);
        var profiles = new List<ChannelProfile>(channels.Count);

        foreach (var channel in channels)
        {
            profiles.Add(await ComposeAsync(channel, channels, cancellationToken).ConfigureAwait(false));
        }

        return profiles;
    }

    /// <summary>One channel's profile, or null where the register holds no such channel.</summary>
    public async Task<ChannelProfile?> ProfileAsync(ChannelId channel, CancellationToken cancellationToken)
    {
        var channels = await _registers.ChannelsAsync(cancellationToken).ConfigureAwait(false);
        var row = channels.FirstOrDefault(c => c.Id.Equals(channel));

        return row is null ? null : await ComposeAsync(row, channels, cancellationToken).ConfigureAwait(false);
    }

    private async Task<ChannelProfile> ComposeAsync(
        Channel channel,
        IReadOnlyList<Channel> register,
        CancellationToken cancellationToken)
    {
        var asOf = _clock.UtcNow;
        var scope = ChannelConfigurationKeys.ScopeFor(channel.Id);
        var attributes = new Dictionary<string, ChannelAttribute>(StringComparer.Ordinal);

        foreach (var key in ChannelConfigurationKeys.Ordered)
        {
            var value = await _configuration.InForceAsync(key, scope, asOf, cancellationToken).ConfigureAwait(false);
            attributes[key] = value is null
                ? ChannelAttribute.Absent(key, scope)
                : ChannelAttribute.RecordedValue(key, scope, value.Value, value.Version, value.ValidFrom);
        }

        var registrations = await _assets.RegistrationsAsync(channel.Id, cancellationToken).ConfigureAwait(false);
        var channelsOfCompany = register.Count(c => c.Company.Equals(channel.Company));

        return new ChannelProfile
        {
            Channel = channel.Id,
            Company = channel.Company,
            Platform = channel.Platform,
            Language = channel.Language,
            Registered = channel.Registered,
            LibraryRegistrations = registrations.OrderBy(r => r.Library, StringComparer.Ordinal).ToArray(),
            Audience = attributes[ChannelConfigurationKeys.Audience],
            Brand = attributes[ChannelConfigurationKeys.Brand],
            Voice = attributes[ChannelConfigurationKeys.Voice],
            Tone = attributes[ChannelConfigurationKeys.Tone],
            VisualIdentity = attributes[ChannelConfigurationKeys.VisualIdentity],
            ContentStrategy = attributes[ChannelConfigurationKeys.ContentStrategy],
            Schedule = attributes[ChannelConfigurationKeys.Schedule],
            Risk = ChannelRiskProfile.For(attributes[ChannelConfigurationKeys.RiskNotes], channel.Company, channelsOfCompany),
        };
    }
}
