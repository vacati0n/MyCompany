using System.Reflection;
using MediaCompany.Domain.Authority;
using Npgsql;

namespace MediaCompany.Persistence;

/// <summary>
/// Creates the record store from the embedded schema (module M-017).
///
/// Direction is forward from an empty store; there is nothing to migrate (constraint C-012). The
/// backup and restore position that sequencing constraint P-012 requires must be established
/// before the first append-only entry is written, because an append-only store has no in-place
/// correction: this installer therefore creates the store and stops, and provisioning the
/// safeguard is an operational step outside it.
/// </summary>
public static class SchemaInstaller
{
    /// <summary>The schema files, applied in order.</summary>
    public static IReadOnlyList<string> ResourceNames { get; } =
    [
        "MediaCompany.Persistence.Schema.001-schema.sql",
        "MediaCompany.Persistence.Schema.002-rollups.sql",

        // Wave 3, the publishing capability. Additive throughout: three new tables, two
        // nullable approval marks and two nullable tier columns, applied after the two
        // delivered files so every table it extends already exists.
        "MediaCompany.Persistence.Schema.003-publication.sql",

        // Wave 4, the analytics surface. Additive: one register recording an observed revenue
        // parameter with its source observation and that observation's date, applied after every
        // resource it depends on. No delivered table, column, constraint or view is altered, and
        // the register is created empty, so the reversal is lossless in the state this change
        // delivers.
        "MediaCompany.Persistence.Schema.004-analytics.sql",

        // Wave 5, the sustained-rate capability. Additive: the stated reasoning tier on the route
        // register, the item dossier register of five write-once tables, and one index over the
        // append-only record by action and instant. Every object is created only where absent,
        // and nothing here records a first-publication condition, an approval or a route.
        "MediaCompany.Persistence.Schema.005-sustained-rate.sql",

        // Wave 6, the multi-channel capability. Additive: the company-level payment-account record,
        // created empty, with new per-channel payment-account rows refused; the audit chain head and
        // the check binding every appender to it; the booking check on the operation record; and the
        // recorded-from-state check on the gate-transition record. Every check binds new rows only,
        // and nothing here records a channel, a budget, a configuration value or a condition.
        "MediaCompany.Persistence.Schema.006-multi-channel.sql",

        // Wave 7, the AI-economics capability. Additive: the benchmark record (a content-free corpus-entry
        // register and write-once observations), the admission decision record, whether an operation's
        // cost is stated, the two keys an observation's reference names, and on the gate-transition
        // record a mandatory position, write-once rows and the named same-instant refusal. Every table is
        // created empty, and nothing here records an observation, an entry, a decision, a budget amount,
        // a configuration value, a route or a gate transition.
        "MediaCompany.Persistence.Schema.007-ai-economics.sql",

        // Wave 8, the AI-management capability. Additive: the open-decisions register, write-once with
        // supersession and seeded with the transcription of the recorded owner decisions and open questions;
        // the platform-policy statement register and the re-verification result record, created empty; the
        // held-outcome record, created empty; and the admission decision readings' amount, spend and
        // utilisation columns widened to the unbounded decimal, as the seventh resource widened the alert
        // columns. Nothing here records a threshold, a cadence, a budget amount, a statement or a result.
        "MediaCompany.Persistence.Schema.008-management.sql",

        // Wave 9, the production capability. Additive: the store designation, the item cap register, the
        // admission reservations, the production versions and artifacts, all created empty and write-once; the
        // character and image unit kinds with the operation record's cost covering them; the recorded billed
        // kinds, account scope and scheme and route terms positions; the customer-content-licence kind and the
        // item-cap reason; and five decided register entries superseding the questions the owner answered.
        // Nothing here records a designation, a cap, a channel, a budget amount, a price or a route.
        "MediaCompany.Persistence.Schema.009-production.sql",
    ];


    public static async Task InstallAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dataSource);

        foreach (var resource in ResourceNames)
        {
            var sql = ReadResource(resource);
            await using var command = dataSource.CreateCommand(sql);
            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        await SeedClosedActionSetAsync(dataSource, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Mirrors the closed action set of <see cref="ActionSet"/> into the roles and permissions
    /// tables, so the operating register of "each role's permitted actions" is producible from the
    /// company's own records (acceptance criterion AC-020).
    ///
    /// The code is the authority and the tables are the mirror, never the other way round: a test
    /// asserts the two are equal, so a row added directly to the database is a failing check
    /// rather than a silently widened permission.
    /// </summary>
    public static async Task SeedClosedActionSetAsync(NpgsqlDataSource dataSource, CancellationToken cancellationToken)
    {
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

        await using (var clear = new NpgsqlCommand("DELETE FROM role_permissions", connection, transaction))
        {
            await clear.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var role in ActionSet.Roles)
        {
            await using (var insertRole = new NpgsqlCommand(
                "INSERT INTO roles (role) VALUES (@role) ON CONFLICT DO NOTHING", connection, transaction))
            {
                insertRole.Parameters.AddWithValue("role", role.ToString());
                await insertRole.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            foreach (var action in ActionSet.For(role))
            {
                await using var insertAction = new NpgsqlCommand(
                    "INSERT INTO role_permissions (role, action) VALUES (@role, @action) ON CONFLICT DO NOTHING",
                    connection,
                    transaction);
                insertAction.Parameters.AddWithValue("role", role.ToString());
                insertAction.Parameters.AddWithValue("action", action.ToString());
                await insertAction.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
    }

    internal static string ReadResource(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                $"Embedded schema resource '{name}' is missing. Available: {string.Join(", ", assembly.GetManifestResourceNames())}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
