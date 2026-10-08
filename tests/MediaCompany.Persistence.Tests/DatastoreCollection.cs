using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The datastore demonstrations share one PostgreSQL instance and each one drops and recreates the
/// schema, so they run SEQUENTIALLY. Run in parallel they clobber one another's schema, and the
/// resulting failure looks like a broken property rather than a broken harness.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class DatastoreCollection
{
    public const string Name = "datastore";
}

/// <summary>
/// The throwaway store every datastore demonstration runs against.
///
/// THE DEMONSTRATIONS DESTROY THE SCHEMA OF WHATEVER DATABASE THE TEST CONNECTION STRING NAMES:
/// each drops it when it starts and drops it again when it completes, failed or not, so no row a
/// demonstration wrote — a first-publication condition recorded satisfied, an owner approval, a
/// gate-state fixture or a dispatch record — outlives the demonstration that wrote it. A
/// development schema therefore lives in a different database from the one the demonstrations
/// are pointed at, and db/README.md says so.
/// </summary>
internal static class ThrowawayStore
{
    internal const string DropStatement = "DROP SCHEMA public CASCADE; CREATE SCHEMA public;";

    internal static async Task DropAsync(NpgsqlDataSource dataSource)
    {
        await using var command = dataSource.CreateCommand(DropStatement);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>How many tables the public schema holds, which is zero once the schema is dropped.</summary>
    internal static async Task<long> TablesAsync(NpgsqlDataSource dataSource)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema = 'public'");
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
