using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MediaCompany.Capability")]
[assembly: InternalsVisibleTo("MediaCompany.Capability.Tests")]

namespace MediaCompany.Credentials;

/// <summary>
/// How the broker reaches the dedicated secret store.
///
/// Open question Q-005 — which dedicated store is provisioned — is a Design Gate decision taken
/// together with the stack, and is still open; risk R-018 carries the consequence. This wave
/// therefore defines the port and ships one adapter that satisfies the two properties constraint
/// C-008 makes non-negotiable: the store is outside the application process, and the secret is
/// resolved per use rather than at start-up. Substituting the adapter when the store is chosen
/// changes this file and nothing else, because no other module holds the dependency.
/// </summary>
public sealed record SecretStoreOptions
{
    /// <summary>
    /// The prefix of the process-external variable each holder's secret is published under. The
    /// value is read on every resolution, never cached, which is what makes a rotation take effect
    /// on the next issuance and a revocation on the next presentation.
    /// </summary>
    public string VariablePrefix { get; init; } = "MEDIACOMPANY_SECRET_";

    /// <summary>The ttl of an issued handle. Shorter than the rotation interval, per risk R-007.</summary>
    public TimeSpan HandleTtl { get; init; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// The composition root's only way to obtain a broker. It supplies configuration, never an
/// implementation, so no caller can substitute a store that returns secret values into its own
/// scope.
/// </summary>
public static class CredentialBrokerFactory
{
    public static CredentialBroker Create(SecretStoreOptions options, Func<DateTimeOffset> clock)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(clock);

        return new CredentialBroker(new ProcessExternalSecretStore(options), clock, options.HandleTtl);
    }

    /// <summary>
    /// The broker of the DEMONSTRATION composition (the production change, decision D-004 of its design). Its
    /// store reads NO variable and holds no secret: it answers every holder with one fixed, non-secret placeholder,
    /// so a fake provider's handle is issued and presented exactly as a real one is, and nothing is read from the
    /// process environment whatever variables are set.
    /// </summary>
    public static CredentialBroker CreateDemonstration(Func<DateTimeOffset> clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        return new CredentialBroker(new DemonstrationSecretStore(), clock, new SecretStoreOptions().HandleTtl);
    }

    /// <summary>
    /// The exact name of the variable a holder's secret is published under (the production change, decision D-016
    /// of its design), so the owner guide, the metered refusal and the store all name one variable. It reads
    /// nothing and returns a name, never a value.
    /// </summary>
    public static string VariableName(ProviderAccountId account, ChannelId? channel, SecretStoreOptions? options = null) =>
        ProcessExternalSecretStore.VariableNameFor(
            new CredentialHolderKey(account, channel), (options ?? new SecretStoreOptions()).VariablePrefix);

    /// <summary>
    /// Whether the variable a holder's secret would be published under is set and non-empty. It answers yes or
    /// no and never returns, logs or compares the value; the metered mode's refusal reads it to name each missing
    /// variable before any call.
    /// </summary>
    public static bool IsPublished(ProviderAccountId account, ChannelId? channel, SecretStoreOptions? options = null) =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(VariableName(account, channel, options)));
}

/// <summary>
/// The demonstration composition's store (the production change, decision D-004 of its design): it reads no
/// environment variable and answers every holder with the same placeholder, which is not a secret of anything.
/// Internal, as every store is.
/// </summary>
internal sealed class DemonstrationSecretStore : ISecretStore
{
    /// <summary>The placeholder a fake provider's handle is presented with; it opens nothing.</summary>
    internal const string Placeholder = "demonstration-placeholder-not-a-secret";

    public Task<string?> ResolveAsync(CredentialHolderKey holder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<string?>(Placeholder);
    }
}

/// <summary>
/// Resolves a holder's secret from the process-external environment on every call.
///
/// The adapter is internal, so the secret value it returns is reachable from no other assembly.
/// It never caches: a rotation written to the store is visible at the next issuance and a
/// revocation at the next presentation, with no redeployment and no restart.
/// </summary>
internal sealed class ProcessExternalSecretStore : ISecretStore
{
    private readonly SecretStoreOptions _options;

    public ProcessExternalSecretStore(SecretStoreOptions options) => _options = options;

    public Task<string?> ResolveAsync(CredentialHolderKey holder, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = VariableNameFor(holder, _options.VariablePrefix);
        var value = Environment.GetEnvironmentVariable(name);
        return Task.FromResult(string.IsNullOrEmpty(value) ? null : value);
    }

    /// <summary>
    /// The variable name is derived from the holder key, which is the pair of provider account and
    /// channel. Because the key is the name, one secret cannot be published for two holders.
    /// </summary>
    internal static string VariableNameFor(CredentialHolderKey holder, string prefix)
    {
        var channel = holder.Channel?.Value.ToString("N") ?? "global";
        var account = holder.ProviderAccount.Value
            .Replace('-', '_')
            .Replace('.', '_')
            .ToUpperInvariant();
        return $"{prefix}{account}__{channel.ToUpperInvariant()}";
    }
}
