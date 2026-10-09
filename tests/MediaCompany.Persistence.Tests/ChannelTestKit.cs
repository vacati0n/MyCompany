using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Services;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Rights;
using Npgsql;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The demonstration harness the multi-channel change adds: the composed services built from the
/// delivered adapters over a throwaway store, and the FIXTURE ROWS a demonstration records. Every
/// fixture here exists only in the throwaway store the demonstration drops; nothing in production
/// code, configuration, seed data or migration records any of them.
/// </summary>
internal static class ChannelTestKit
{
    /// <summary>The demonstration library a fixture asset comes from and a fixture channel is registered on.</summary>
    internal const string Library = "library-one";

    /// <summary>
    /// The month the datastore books an operation into now: the month of the later of its clock and
    /// the record horizon. A demonstration reads its period from here, or from the stored instant the
    /// recorder returns, because no operation is booked at an instant a caller supplies.
    /// </summary>
    internal static async Task<DateOnly> BookingMonthAsync(NpgsqlDataSource source)
    {
        await using var command = source.CreateCommand(
            "SELECT (date_trunc('month', GREATEST(clock_timestamp(), horizon) AT TIME ZONE 'UTC'))::date FROM audit_record_horizon");
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<DateOnly>(0);
    }

    internal static PublicationGateService Gate(NpgsqlDataSource source, IUnitOfWork unitOfWork, IClock clock) =>
        new(new NpgsqlGateLedger(source), new NpgsqlAssetLedger(source), new NpgsqlItemRegister(source), unitOfWork, clock);

    internal static AnalyticsReportService Analytics(NpgsqlDataSource source, IClock clock)
    {
        var costs = new NpgsqlCostReader(source);
        return new AnalyticsReportService(
            costs, costs, new NpgsqlRevenueParameterRegister(source), new NpgsqlThroughputReader(source),
            new NpgsqlItemDossierReader(source), new NpgsqlGateLedger(source), new NpgsqlAssetLedger(source),
            new NpgsqlChannelPartitionReader(source), new NpgsqlApprovalQueueReader(source),
            new NpgsqlOperatingRegisters(source), new NpgsqlBenchmarkReader(source), clock);
    }

    /// <summary>A complete, verified asset decision for one item, naming itself a demonstration fixture.</summary>
    internal static Asset CompleteAsset(ItemId item) => new()
    {
        Id = AssetId.New(),
        Item = item,
        Library = Library,
        Source = "demonstration fixture catalogue",
        Creator = "demonstration fixture creator",
        LicenceType = LicenceType.RoyaltyFreeStock,
        LicenceReference = "demonstration-fixture-licence",
        CommercialUsePermitted = true,
        ModificationPermitted = true,
        AttributionRequirement = "none under the fixture licence",
        PlatformRestrictions = "none",
        ProofOfLicenceReference = "demonstration-fixture-proof",
        AssessedRisk = AssessedRisk.Low,
        VerifiedBy = WorkforceRole.Copyright,
        VerifiedOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1),
    };

    /// <summary>
    /// The rights fixtures a presentation needs: one complete asset decision for the item and the
    /// channel's registration on the asset's library, both demonstration rows in the throwaway store.
    /// </summary>
    internal static async Task RecordRightsFixtureAsync(NpgsqlDataSource source, ItemId item, ChannelId channel)
    {
        await new NpgsqlAssetLedger(source).RecordAsync(CompleteAsset(item), CancellationToken.None);
        await using var command = source.CreateCommand(
            """
            INSERT INTO library_registrations (channel_id, library, registered_on)
            VALUES (@channel, @library, CURRENT_DATE - 1)
            ON CONFLICT (channel_id, library) DO NOTHING
            """);
        command.Parameters.AddWithValue("channel", channel.Value);
        command.Parameters.AddWithValue("library", Library);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// One fixture first-publication observation. The payment account is a COMPANY-LEVEL fact, so its
    /// fixture row goes to the company-level record through the channel's company; the other two are
    /// the channel's own. Each carries the evidence the schema requires and names itself a fixture.
    /// </summary>
    internal static async Task RecordConditionFixtureAsync(
        NpgsqlDataSource source,
        ChannelId channel,
        FirstPublicationCondition condition,
        ConditionState state)
    {
        await using var command = source.CreateCommand(condition == FirstPublicationCondition.PaymentAccount
            ? """
              INSERT INTO company_payment_account_observations (company_id, state, evidence, observed_on)
              SELECT company_id, @state, 'demonstration fixture row in a throwaway store; discharges nothing', CURRENT_DATE
              FROM channels WHERE channel_id = @channel
              """
            : """
              INSERT INTO first_publication_conditions (channel_id, condition, state, evidence, observed_on)
              VALUES (@channel, @condition, @state, 'demonstration fixture row in a throwaway store; discharges nothing', CURRENT_DATE)
              """);
        command.Parameters.AddWithValue("channel", channel.Value);
        command.Parameters.AddWithValue("condition", condition.ToString());
        command.Parameters.AddWithValue("state", state.ToString());
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Brings an item version from Draft to an owner verdict through the DELIVERED gate path on real
    /// recorded state: the submission writes awaiting rights check, the presentation reads it and the
    /// recorded rights check, and the verdict is recorded. Returns the presentation instant.
    /// </summary>
    internal static async Task<DateTimeOffset> PresentAsync(PublicationGateService gate, ItemId item, ItemVersion version)
    {
        var submitted = await gate.SubmitForRightsCheckAsync(item, version, WorkforceRole.Producer, CancellationToken.None);
        if (submitted is not GateStepOutcome.Moved)
        {
            throw new InvalidOperationException($"the fixture submission was refused: {submitted}");
        }

        var presented = await gate.PresentForOwnerApprovalAsync(item, version, CancellationToken.None);
        return presented is GateStepOutcome.Moved moved
            ? moved.At
            : throw new InvalidOperationException($"the fixture presentation was refused: {presented}");
    }
}
