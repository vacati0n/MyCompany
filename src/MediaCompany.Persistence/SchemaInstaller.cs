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
