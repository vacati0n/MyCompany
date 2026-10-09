using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// That a demonstration's store is DROPPED BY THE DEMONSTRATION ITSELF, on completion as well as at
/// start, and that this holds when the demonstration aborts.
///
/// The demonstration driven here is the one that writes every row the Planning Gate names: the
/// three first-publication conditions recorded satisfied as fixture rows, a gate-state fixture, an
/// owner approval and a dispatch record. It is run through its own lifecycle, the rows are observed
/// while it holds the store, and the store is inspected after it completes.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class ThrowawayStoreTests
{
    /// <summary>
    /// After the composition demonstration completes, no table remains: none of its satisfied
    /// conditions, its approval, its gate states or its dispatch record outlives it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task NoFixtureRowOutlivesTheDemonstrationThatWroteIt()
    {
        var demonstration = new DossierAndSequenceIntegrationTests();
        await demonstration.InitializeAsync();

        try
        {
            await demonstration.OverTheFixtureStoreTheDriveComposesAndStopsAtTheTerminalPosition();

            await using var observer = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString!);
            // Two per-channel conditions, and the payment account in the company-level record.
            Assert.Equal(2L, await CountAsync(observer, "first_publication_conditions WHERE state = 'Satisfied'"));
            Assert.Equal(1L, await CountAsync(observer, "company_payment_account_observations WHERE state = 'Satisfied'"));
            Assert.Equal(1L, await CountAsync(observer, "approvals"));
            Assert.Equal(1L, await CountAsync(observer, "publication_dispatches"));
            Assert.True(await CountAsync(observer, "gate_transitions") > 0);
        }
        finally
        {
            await demonstration.DisposeAsync();
        }

        await using var after = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString!);
        Assert.Equal(0L, await ThrowawayStore.TablesAsync(after));
    }

    /// <summary>
    /// A demonstration that ABORTS part-way — here the composition demonstration run a second time
    /// over the same store, whose fixture submission the gate path refuses because the first run left
    /// the item approved — still leaves no table behind once it completes.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnAbortedDemonstrationStillLeavesNoRowBehind()
    {
        var demonstration = new DossierAndSequenceIntegrationTests();
        await demonstration.InitializeAsync();

        try
        {
            await demonstration.OverTheFixtureStoreTheDriveComposesAndStopsAtTheTerminalPosition();
            var aborted = await Assert.ThrowsAsync<InvalidOperationException>(
                demonstration.OverTheFixtureStoreTheDriveComposesAndStopsAtTheTerminalPosition);
            Assert.Contains(nameof(Deterministic.Services.GateStepRefusal.NotSubmittableFromRecordedState), aborted.Message, StringComparison.Ordinal);
        }
        finally
        {
            await demonstration.DisposeAsync();
        }

        await using var after = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString!);
        Assert.Equal(0L, await ThrowawayStore.TablesAsync(after));
    }

    private static async Task<long> CountAsync(NpgsqlDataSource source, string from)
    {
        await using var command = source.CreateCommand($"SELECT COUNT(*) FROM {from}");
        return (long)(await command.ExecuteScalarAsync())!;
    }
}
