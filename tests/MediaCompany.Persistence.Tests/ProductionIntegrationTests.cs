using System.Net;
using System.Security.Cryptography;
using System.Text.Json;
using MediaCompany.Application.Ports;
using MediaCompany.Capability;
using MediaCompany.Capability.Providers;
using MediaCompany.Credentials;
using MediaCompany.Deterministic.Analytics;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Analytics;
using MediaCompany.Domain.Authority;
using MediaCompany.Domain.Management;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;
using MediaCompany.Host;
using MediaCompany.Production;
using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// The production capability against the record store (the ninth schema resource and the change that comes with it).
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, in the separate demonstration
/// database, PREPARED BY THE PREPARATION COMMAND'S OWN WRITER FROM THE RECORDED CONFIGURATION and designated
/// demonstration. Every price, cap and route below is that recorded configuration or a fixture of this store alone.
///
/// NO METERED CALL IS MADE AND NO REAL ENDPOINT IS REACHED. Fake mode composes the fakes; the metered path below the
/// host command is exercised through the produce service with the vendor adapter over a STUB HANDLER answering every
/// request itself, under a fake secret this test sets. The company store is never connected to.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class ProductionIntegrationTests : IAsyncLifetime
{
    private const string FakeSecret = "fake-test-secret-7c1e-never-a-credential";
    private static readonly ProviderAccountId Speech = new("openai");

    private NpgsqlDataSource? _dataSource;
    private readonly string _output = Path.Combine(Path.GetTempPath(), "mediacompany-production-" + Guid.NewGuid().ToString("N"));

    private NpgsqlDataSource Source => _dataSource ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    public async Task InitializeAsync()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            return;
        }

        _dataSource = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString);
        try
        {
            await ThrowawayStore.DropAsync(_dataSource);
            await SchemaInstaller.InstallAsync(_dataSource, CancellationToken.None);
        }
        catch
        {
            await ThrowawayStore.DropAsync(_dataSource);
            throw;
        }
    }

    public async Task DisposeAsync()
    {
        if (Directory.Exists(_output))
        {
            Directory.Delete(_output, recursive: true);
        }

        if (_dataSource is not null)
        {
            try
            {
                await ThrowawayStore.DropAsync(_dataSource);
            }
            finally
            {
                await _dataSource.DisposeAsync();
            }
        }
    }

    // -----------------------------------------------------------------------
    // The ninth schema resource
    // -----------------------------------------------------------------------

    /// <summary>
    /// The ninth resource installs last, creates every record EMPTY and records nothing — no designation, cap,
    /// reservation, production, artifact, budget or configuration value — records the owner's five answers as decided
    /// entries superseding the five questions, leaves price capture and the register writer open, costs all five kinds
    /// in the stored cost, and applies a second time changing nothing.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheNinthResourceRecordsNothingAnswersFiveQuestionsAndReappliesCleanly()
    {
        Assert.Equal("MediaCompany.Persistence.Schema.009-production.sql", SchemaInstaller.ResourceNames[^2]);
        Assert.Equal("MediaCompany.Persistence.Schema.009-production.sql", SchemaInstaller.ResourceNames[8]);
        foreach (var table in new[] { "store_designation", "item_caps", "admission_reservations", "production_versions", "production_artifacts", "budgets", "configuration" })
        {
            Assert.Equal(0L, await ScalarAsync<long>($"SELECT count(*) FROM {table}"));
        }

        var answers = new Dictionary<string, string> { ["REG-022"] = "REG-004", ["REG-023"] = "REG-007", ["REG-024"] = "REG-017", ["REG-025"] = "REG-018", ["REG-026"] = "REG-019" };
        var read = await new NpgsqlCompanyRecordReader(Source).ReadAsync(null, CancellationToken.None);
        foreach (var (answer, question) in answers)
        {
            var entry = Assert.Single(read.RegisterEntries, e => e.Identifier == answer);
            Assert.Equal(RegisterEntryStatus.Decided, entry.Status);
            Assert.Equal(question, entry.Supersedes);
            Assert.Contains("research/ceo-decision-record.md", entry.Statement, StringComparison.Ordinal);
            Assert.Equal("the owner", entry.DecidedBy);
        }

        var open = read.RegisterEntries.Where(e => RuleCatalogue.IsOpen(e, read.RegisterEntries)).Select(e => e.Identifier).Order(StringComparer.Ordinal);
        Assert.Equal(new[] { 5, 6, 8, 9, 10, 11, 12, 13, 20, 21 }.Select(n => $"REG-{n:D3}"), open);

        Assert.Contains("character_units", await ScalarAsync<string>(
            "SELECT pg_get_expr(d.adbin, d.adrelid) FROM pg_attrdef d JOIN pg_attribute a ON a.attrelid = d.adrelid AND a.attnum = d.adnum "
            + "WHERE d.adrelid = 'agent_costs'::regclass AND a.attname = 'computed_cost'"), StringComparison.Ordinal);

        var instants = read.RegisterEntries.OrderBy(e => e.Identifier, StringComparer.Ordinal).Select(e => e.EnteredAt).ToArray();
        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.009-production.sql"));
        var again = await new NpgsqlCompanyRecordReader(Source).ReadAsync(null, CancellationToken.None);
        Assert.Equal(instants, again.RegisterEntries.OrderBy(e => e.Identifier, StringComparer.Ordinal).Select(e => e.EnteredAt));
    }

    /// <summary>
    /// A row recorded BEFORE the ninth resource reads as delivered: its cost is the value it held, its added units zero,
    /// though the stored cost's expression changed under it.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnOperationRecordedBeforeTheNinthResourceKeepsItsCost()
    {
        await ThrowawayStore.DropAsync(Source);
        foreach (var resource in SchemaInstaller.ResourceNames.Take(8))
        {
            await ExecuteAsync(SchemaInstaller.ReadResource(resource));
        }

        await ExecuteAsync(
            """
            INSERT INTO agent_costs (operation_id, run_id, occurred_at, attempt, item_id, channel_id, department_id, agent_id, capability_class,
                                     period, input_units, output_units, applied_input_price, applied_output_price, cost_basis, duration_ms,
                                     outcome, cost_stated)
            SELECT gen_random_uuid(), gen_random_uuid(), now_at, 1, gen_random_uuid(), gen_random_uuid(), gen_random_uuid(), gen_random_uuid(),
                   'EditorialReasoning', (date_trunc('month', now_at AT TIME ZONE 'UTC'))::date, 1000, 500, 0.000003, 0.000015,
                   'Measurement', 10, 'Succeeded', true
            FROM (SELECT GREATEST(clock_timestamp(), horizon) AS now_at FROM audit_record_horizon) h
            """);
        var before = await ScalarAsync<decimal>("SELECT computed_cost FROM agent_costs");

        await ExecuteAsync(SchemaInstaller.ReadResource("MediaCompany.Persistence.Schema.009-production.sql"));

        Assert.Equal(before, await ScalarAsync<decimal>("SELECT computed_cost FROM agent_costs"));
        Assert.Equal(0.0105m, before);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT character_units + image_units FROM agent_costs"));
    }

    // -----------------------------------------------------------------------
    // Costing in every billed kind
    // -----------------------------------------------------------------------

    /// <summary>
    /// A character-billed operation books a STATED cost from the price in force, carrying its source and date. THE
    /// OWNER'S UNIT-KIND RULE ON THE BOOKING SIDE (correction CR-002): a consumed kind the model is NOT billed by needs no
    /// price and costs nothing, so the cost stays stated; a consumed kind the model IS billed by with no price in force
    /// leaves the cost not stated under its named reason.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ACharacterBilledOperationIsBookedAtAStatedCostAnUnbilledKindNeedsNoPriceAndAnUnpricedBilledKindIsNotStated()
    {
        await PrepareAsync();
        await FixtureModelAsync("image-unpriced-fixture", ["ImageUnit"]);
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());

        OperationRecord priced;
        OperationRecord unbilled;
        OperationRecord unpriced;
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            priced = await transaction.Operations.RecordAsync(Draft(new UnitCounts(0, 0, 0, 0) { CharacterUnits = 1_000 }), CancellationToken.None);
            unbilled = await transaction.Operations.RecordAsync(Draft(new UnitCounts(0, 0, 0, 0) { CharacterUnits = 1_000, ImageUnits = 1 }), CancellationToken.None);
            unpriced = await transaction.Operations.RecordAsync(
                Draft(new UnitCounts(0, 0, 0, 0) { ImageUnits = 1 }) with { Model = new ModelId("image-unpriced-fixture") }, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.True(priced.CostStated);
        Assert.Equal(1_000 * 0.000015m, priced.ComputedCost.Amount);
        Assert.Equal(0.000015m, await ScalarAsync<decimal>($"SELECT applied_character_price FROM agent_costs WHERE operation_id = '{priced.Id}'"));
        // The applied price carries its recorded source; the orchestrator's first-hand re-fetch of 2026-10-10 added a
        // newer row at the same price, so the source is asserted by its stated price, not by its ESTIMATE label.
        Assert.Contains("USD 15.00 per", await ScalarAsync<string>(
            $"SELECT p.source FROM agent_costs a JOIN model_prices p ON p.model_price_id = a.applied_price_id WHERE a.operation_id = '{priced.Id}'"), StringComparison.Ordinal);
        Assert.True(unbilled.CostStated);
        Assert.Equal(1_000 * 0.000015m, unbilled.ComputedCost.Amount);
        Assert.False(unpriced.CostStated);
        Assert.Equal(CostUnstatedReason.PriceNotInForce, unpriced.CostUnstatedReason);
    }

    /// <summary>
    /// A MEASURED IMAGE RESPONSE (correction CR-002): the response reports input, output and image units, and the model
    /// is recorded as billed by input and output units only. The booking states its cost from the input and output
    /// prices in force, applies no image price, and needs none.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AMeasuredImageResponseIsBookedByTheKindsItsModelIsBilledBy()
    {
        await PrepareAsync();
        await FixtureModelAsync("image-measured-fixture", ["InputUnit", "OutputUnit"]);
        await FixturePriceAsync("image-measured-fixture", "InputUnit", 0.000005m);
        await FixturePriceAsync("image-measured-fixture", "OutputUnit", 0.00004m);
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());

        OperationRecord measured;
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            measured = await transaction.Operations.RecordAsync(
                Draft(new UnitCounts(100, 200, 0, 0) { ImageUnits = 1 }) with { Model = new ModelId("image-measured-fixture") }, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.True(measured.CostStated);
        Assert.Equal((100 * 0.000005m) + (200 * 0.00004m), measured.ComputedCost.Amount);
        Assert.Equal(0m, await ScalarAsync<decimal>($"SELECT applied_image_price FROM agent_costs WHERE operation_id = '{measured.Id}'"));
        Assert.Equal(1L, await ScalarAsync<long>($"SELECT image_units FROM agent_costs WHERE operation_id = '{measured.Id}'"));
        Assert.Equal("InputUnit", await ScalarAsync<string>(
            $"SELECT p.unit_kind FROM agent_costs a JOIN model_prices p ON p.model_price_id = a.applied_price_id WHERE a.operation_id = '{measured.Id}'"));
    }

    /// <summary>
    /// A COST-UNSTATED BOOKING DOES NOT CLEAR ITS RESERVATION (corrections CR-002 and CR-004): the vendor answers with a
    /// measured response reporting a unit no price row can cost, so the booking's cost is not stated. Its reservation
    /// keeps counting at its worst case in the item's counted total, and the next admission of the item is refused before
    /// any call: the company and channel readings cannot state the booked spend, and the cap counts the worst case.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ACostUnstatedBookingKeepsItsReservationCountingAtItsWorstCase()
    {
        var record = await PrepareAsync();
        var item = await SecondItemAsync(record, cap: 0.05m);
        var stand = new StandIn(() => new ProviderAttempt(true, new UnitCounts(0, 0, 0, 1) { CharacterUnits = 2_000 }, CostBasis.Measurement, TimeSpan.FromMilliseconds(5), null, null)
        {
            Content = new ProducedContent(ContentKind.Audio, "audio/wav", FakeSpeechAudio.Wave("stand-in")),
            ChargeKnown = true,
        });

        var booked = Assert.IsType<CapabilityOutcome.Completed>(await Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), stand)
            .ExecuteAsync(Request(record, 2_000, item), Context(), CancellationToken.None));
        Assert.False(booked.Operation.CostStated);
        Assert.Equal(CostUnstatedReason.UnpricedUnitConsumed, booked.Operation.CostUnstatedReason);

        // The production reader and the admission ledger each count the reservation.
        var cap = (await new NpgsqlProductionReader(Source).PackageAsync(item, new ItemVersion(1), CancellationToken.None))!.Cap!;
        Assert.Equal(1, cap.OpenReservationCount);
        Assert.Equal(0L, cap.UnstatedOperations);
        Assert.Equal(2_000 * 0.000015m, cap.CountedTotal.Amount);
        await using (var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock()).BeginAsync(CancellationToken.None))
        {
            await transaction.Admission.ReserveAsync(CancellationToken.None);
            var snapshot = await transaction.Admission.ReadAsync(
                CapabilityClass.Narration, new Attribution(item, record.Channel, record.Department, record.Agent), ApprovedEnvelope.Metered, null, CancellationToken.None);
            Assert.Equal(1, snapshot.ItemCap!.OpenReservationCount);
            Assert.Equal(2_000 * 0.000015m, snapshot.ItemCap.CountedTotal.Amount);
        }

        var second = await Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), stand)
            .ExecuteAsync(Request(record, 2_000, item), Context(), CancellationToken.None);
        Assert.IsNotType<CapabilityOutcome.Completed>(second);
        Assert.Equal(1, stand.Calls);
    }

    /// <summary>
    /// THE CAP FAILS CLOSED (correction CR-004): an operation of the item with a cost not stated and NO reservation
    /// bounding it leaves the counted total unstatable. The store's cap reading names it, and the next admission is
    /// refused before any call (the resolver's own refusal under the cap is proved in memory, where no budget reading
    /// refuses first).
    /// </summary>
    [RequiresPostgresFact]
    public async Task AnUnstatedOperationWithNoReservationClosesTheCap()
    {
        var record = await PrepareAsync();
        await FixtureModelAsync("image-unpriced-fixture", ["ImageUnit"]);
        await using (var transaction = await new NpgsqlUnitOfWork(Source, new FixedClock()).BeginAsync(CancellationToken.None))
        {
            var unstated = await transaction.Operations.RecordAsync(
                Draft(new UnitCounts(0, 0, 0, 0) { ImageUnits = 1 }) with { Model = new ModelId("image-unpriced-fixture") }, CancellationToken.None);
            Assert.False(unstated.CostStated);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var cap = (await new NpgsqlProductionReader(Source).PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!.Cap!;
        Assert.Equal(1L, cap.UnstatedOperations);
        Assert.Contains("1 of them with a cost not stated and no reservation bounding it", cap.Describe(), StringComparison.Ordinal);

        var stand = new StandIn(Succeeded);
        var refused = await Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), stand)
            .ExecuteAsync(Request(record, 100), Context(), CancellationToken.None);
        Assert.IsNotType<CapabilityOutcome.Completed>(refused);
        Assert.Equal(0, stand.Calls);
    }

    // -----------------------------------------------------------------------
    // The designation and the preparation
    // -----------------------------------------------------------------------

    /// <summary>The designation is written once, and only naming the database it is written in.</summary>
    [RequiresPostgresFact]
    public async Task TheDesignationIsWrittenOnceAndOnlyInItsOwnDatabase()
    {
        await AssertRefusedAsync("INSERT INTO store_designation (designation, database_name, recorded_at) VALUES ('Company', 'mediacompany', now())", "names database mediacompany");
        await ExecuteAsync("INSERT INTO store_designation (designation, database_name, recorded_at) VALUES ('Demonstration', current_database(), now())");
        await AssertRefusedAsync("UPDATE store_designation SET designation = 'Company'", "written once");

        var identity = await new NpgsqlStoreDesignationReader(Source).ReadAsync(CancellationToken.None);
        Assert.Equal(StoreDesignation.Demonstration, identity.Designation);
        Assert.Equal("mediacompany_demo", identity.DatabaseName);
    }

    /// <summary>
    /// The preparation loads every record a run reads into a fresh demonstration store, records NO budget amount and
    /// NO configuration value, is idempotent, refuses a differing row by name without writing anything, and refuses a
    /// store designated otherwise.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ThePreparationLoadsEveryRecordOnceRefusesADifferenceAndRecordsNoBudget()
    {
        var record = Preparation();
        var writer = new NpgsqlPreparationWriter(Source);

        var first = await writer.PrepareAsync(record, StoreDesignation.Demonstration, CancellationToken.None);
        var second = await writer.PrepareAsync(record, StoreDesignation.Demonstration, CancellationToken.None);

        Assert.False(first.Refused);
        Assert.Equal(0, first.AlreadyPresent);
        Assert.Equal(0, second.Inserted);
        Assert.Equal(first.Inserted, second.AlreadyPresent);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM budgets"));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM configuration"));
        Assert.Equal(12L, await ScalarAsync<long>($"SELECT count(*) FROM dossier_stage_evidence WHERE item_id = '{record.Item}' AND item_version = 1"));
        Assert.Equal(5.95m, await ScalarAsync<decimal>($"SELECT amount FROM item_caps WHERE item_id = '{record.Item}'"));

        // The company record reader reads each channel's cap coverage in its one snapshot (the unbudgeted-channel ruling).
        var coverage = Assert.Single((await new NpgsqlCompanyRecordReader(Source).ReadAsync(null, CancellationToken.None)).ItemCapCoverage!);
        Assert.Equal(record.Channel, coverage.Channel);
        Assert.Equal(1, coverage.Items);
        Assert.Equal(1, coverage.CappedItems);

        var differing = await writer.PrepareAsync(record with { ItemTitle = "a different title" }, StoreDesignation.Demonstration, CancellationToken.None);
        Assert.True(differing.Refused);
        Assert.Contains(differing.Refusals, r => r.Contains($"item {record.Item}", StringComparison.Ordinal) && r.Contains("title", StringComparison.Ordinal));

        var company = await writer.PrepareAsync(record, StoreDesignation.Company, CancellationToken.None);
        Assert.Contains("designated Demonstration", Assert.Single(company.Refusals), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The cap's round trip against the store
    // -----------------------------------------------------------------------

    /// <summary>
    /// A TIMEOUT AFTER THE VENDOR MAY HAVE CHARGED is booked at the reserved worst case, a stated cost labelled
    /// estimate; its reservation, committed before the call, is reconciled by the booking under the one identifier.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ATimeoutAfterTheVendorMayHaveChargedIsBookedAtTheReservedWorstCase()
    {
        var record = await PrepareAsync();
        var gateway = Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), new StandIn(() => VendorCalls.Unknown(TimeSpan.FromSeconds(60), "timed out", ProviderFailureSignal.Outage)));

        var booked = Assert.IsType<CapabilityOutcome.Completed>(await gateway.ExecuteAsync(Request(record, 1_200), Context(), CancellationToken.None));

        Assert.Equal(OperationOutcome.Failed, booked.Operation.Outcome);
        Assert.Equal(CostBasis.Estimate, booked.Operation.CostBasis);
        Assert.Equal(1_200, booked.Operation.Units.CharacterUnits);
        Assert.Equal(1_200 * 0.000015m, booked.Operation.ComputedCost.Amount);
        Assert.True(booked.Operation.CostStated);
        Assert.Equal(1L, await ScalarAsync<long>($"SELECT count(*) FROM admission_reservations WHERE operation_id = '{booked.Operation.Id}'"));
        var cap = (await new NpgsqlProductionReader(Source).PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!.Cap!;
        Assert.Equal(0, cap.OpenReservationCount);
        Assert.Equal(1_200 * 0.000015m, cap.CountedTotal.Amount);
    }

    /// <summary>
    /// A RESERVATION COMMITTED AND THE BOOKING NEVER WRITTEN: the admission transaction is lost after the call and the
    /// recording cannot be made either. The reservation is in the store, open; the next admission of the item counts
    /// it at its worst case and refuses a call that would pass the cap with it — before the call.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AReservationWhoseBookingWasNeverWrittenKeepsCountingInTheStore()
    {
        var record = await PrepareAsync();
        var item = await SecondItemAsync(record, cap: 0.05m);

        var stand = new StandIn(Succeeded);
        var lost = Gateway(new LosingUnitOfWork(new NpgsqlUnitOfWork(Source, new FixedClock())), stand);
        await Assert.ThrowsAsync<IncurredAttemptNotRecordedException>(() =>
            lost.ExecuteAsync(Request(record, 2_000, item), Context(), CancellationToken.None));

        Assert.Equal(1L, await ScalarAsync<long>($"SELECT count(*) FROM admission_reservations WHERE item_id = '{item}'"));
        Assert.Equal(0L, await ScalarAsync<long>($"SELECT count(*) FROM agent_costs WHERE item_id = '{item}'"));

        var refused = Assert.IsType<CapabilityOutcome.Refused>(await Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), stand)
            .ExecuteAsync(Request(record, 2_000, item), Context(), CancellationToken.None));
        Assert.Equal(RefusalReason.ItemCapExceeded, refused.Reason);
        Assert.Contains("1 open reservation(s)", refused.Detail, StringComparison.Ordinal);
        Assert.Equal(1, stand.Calls);
    }

    /// <summary>
    /// A CONCURRENT PAIR of admissions of one item: while the first holds the company scope across its call, the second
    /// is deferred without waiting and without a call, so the two together can never pass the cap.
    /// </summary>
    [RequiresPostgresFact]
    public async Task AConcurrentPairCannotTogetherPassTheCap()
    {
        var record = await PrepareAsync();
        var release = new TaskCompletionSource();
        var entered = new TaskCompletionSource();
        var first = new StandIn(() =>
        {
            entered.TrySetResult();
            release.Task.Wait(TimeSpan.FromSeconds(30));
            return Succeeded();
        });
        var second = new StandIn(Succeeded);

        var running = Task.Run(() => Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), first).ExecuteAsync(Request(record, 1_000), Context(), CancellationToken.None));
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(30));
        var deferred = await Gateway(new NpgsqlUnitOfWork(Source, new FixedClock()), second).ExecuteAsync(Request(record, 1_000), Context(), CancellationToken.None);
        release.SetResult();
        var completed = await running;

        Assert.Equal(RefusalReason.MeteredAdmissionInProgress, Assert.IsType<CapabilityOutcome.Held>(deferred).Reason);
        Assert.Equal(0, second.Calls);
        Assert.IsType<CapabilityOutcome.Completed>(completed);
        Assert.Equal(1L, await ScalarAsync<long>($"SELECT count(*) FROM admission_reservations WHERE item_id = '{record.Item}'"));
    }

    // -----------------------------------------------------------------------
    // The zero-spend production, end to end
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE ZERO-SPEND PRODUCTION OF ITEM 001, END TO END, in fake mode against a fresh demonstration store prepared from
    /// the recorded configuration, with the process clock SKEWED three years ahead of the datastore's:
    ///   * one rendered file under the output root, decoding end to end with zero errors, exactly one video and one audio
    ///     stream, its recorded runtime the probed duration within one frame, within one frame of the narration's;
    ///   * twelve stage outcomes on the new item version, the four produced ones in execution order, every instant the
    ///     datastore's and none the process clock's; every artifact's recorded hash the stored file's;
    ///   * fourteen operations, the whole narration exactly, booked at zero and labelled demonstration in every reader;
    ///     no benchmark observation; the publish-ready predicate refusing on the held stages;
    ///   * ZERO DATASTORE TRANSACTIONS OPEN while any external process runs;
    ///   * then plan-only mode, which previews the OWN mode (the own-voice change, decision D-002 of its design): it calls
    ///     nothing, starts nothing and writes nothing, and states that no metered spend is planned; the vendor ESTIMATE it
    ///     printed before is asserted on a direct composition of the vendor plan;
    ///   * then METERED MODE THROUGH THE RULE, refused before any request naming the owner's decision of 2026-10-10;
    ///   * then THE VENDOR PATH UNDER TEST, BELOW THE RULE, through the same service's internal entry, with the vendor adapter
    ///     over a stub handler and a fake secret: the narration requests reach the stub, the narration repeats byte for byte,
    ///     and a render past its bound ends the Production stage failed by name with no rendered file recorded.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task TheZeroSpendProductionOfItemOneRendersOneDecodableFileAndRecordsEverything()
    {
        var record = await PrepareAsync();
        var loaded = await ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);
        var settings = MediaToolOnPath.Settings(_output, Repository());
        var observer = new TransactionObserver(Source);
        var tool = new ObservedTool(new ExternalMediaTool(settings), observer);
        var store = new ArtifactWriter(_output, Repository());
        var output = new StringWriter();
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());
        var reader = new NpgsqlProductionReader(Source);
        var service = new ProduceItemService(unitOfWork, reader, tool, store, settings, output);
        var skewed = () => DateTimeOffset.UtcNow.AddYears(3);
        var before = await ScalarAsync<DateTimeOffset>("SELECT clock_timestamp()");

        var fake = await service.RunAsync(Request(record, loaded, ProductionMode.Fake),
            CapabilityGatewayFactory.CreateDemonstration(unitOfWork, [new DemonstrationProvider(Speech, VendorContract.SpeechAudio)], skewed), CancellationToken.None);

        var after = await ScalarAsync<DateTimeOffset>("SELECT clock_timestamp()");
        Assert.True(fake.ExitCode == ProduceResult.Succeeded, output.ToString());
        var version = fake.Version!.Value;
        Assert.Equal(2, version.Value);

        // One rendered file, decodable end to end, one video and one audio stream, its runtime measured from the file.
        var rendered = fake.Rendered!;
        Assert.StartsWith(Path.GetFullPath(_output), rendered.FullPath, StringComparison.OrdinalIgnoreCase);
        var probe = await new ExternalMediaTool(settings).ProbeAsync(rendered.FullPath, CancellationToken.None);
        Assert.Equal(1, probe.Count("video"));
        Assert.Equal(1, probe.Count("audio"));
        Assert.Equal(2, probe.StreamKinds.Count);
        Assert.Equal(0, (await new ExternalMediaTool(settings).DecodeAsync(rendered.FullPath, CancellationToken.None)).Errors);
        var artifacts = await reader.ArtifactsAsync(record.Item, version, CancellationToken.None);
        var video = Assert.Single(artifacts, a => a.Role == ArtifactRole.RenderedVideo);
        var frame = TimeSpan.FromSeconds(1.0 / settings.FramesPerSecond);
        Assert.True((video.MeasuredDuration!.Value - probe.Duration).Duration() <= frame);
        var narration = Assert.Single(artifacts, a => a.Role == ArtifactRole.Narration);
        Assert.True((video.MeasuredDuration!.Value - narration.MeasuredDuration!.Value).Duration() <= frame,
            $"the video runs {video.MeasuredDuration} and the narration {narration.MeasuredDuration}");

        // Every recorded hash is the stored file's.
        foreach (var artifact in artifacts)
        {
            Assert.Equal(artifact.Sha256, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(store.FullPath(artifact.RelativePath)))));
        }

        Assert.Equal(22, artifacts.Count(a => a.Role == ArtifactRole.GraphicStill));
        Assert.Equal(18, artifacts.Count(a => a.Role == ArtifactRole.ClipPlaceholder));
        Assert.Equal(5, artifacts.Count(a => a.Role == ArtifactRole.ThumbnailCandidate));
        Assert.Equal("THUMB-03", Assert.Single(artifacts, a => a.Role == ArtifactRole.ThumbnailPlaceholder).Implements);

        // Twelve stage outcomes, the produced four in execution order, every instant the datastore's.
        var stages = await reader.StagesAsync(record.Item, version, CancellationToken.None);
        Assert.Equal(ProductionStageSet.All, stages.Select(s => s.Stage));
        Assert.All(stages, s => Assert.InRange(s.RecordedAt, before, after));
        var produced = new[] { ProductionStage.Design, ProductionStage.Audio, ProductionStage.Thumbnail, ProductionStage.Production }
            .Select(p => stages.Single(s => s.Stage == p).RecordedAt).ToArray();
        Assert.Equal(produced.Order(), produced);
        Assert.Equal(StageOutcome.Held, stages.Single(s => s.Stage == ProductionStage.Production).Outcome);
        Assert.Contains("CLIP-05, CLIP-06, CLIP-07", stages.Single(s => s.Stage == ProductionStage.Production).Summary, StringComparison.Ordinal);
        Assert.Equal(StageOutcome.Held, stages.Single(s => s.Stage == ProductionStage.Thumbnail).Outcome);
        Assert.Equal(StageOutcome.Held, stages.Single(s => s.Stage == ProductionStage.CopyrightCheck).Outcome);
        Assert.Contains("MEASURED", stages.Single(s => s.Stage == ProductionStage.Audio).Summary, StringComparison.Ordinal);

        // Fourteen operations: the whole narration, at zero, every instant the datastore's.
        Assert.Equal(14, fake.Operations.Count);
        Assert.All(fake.Operations, o => Assert.InRange(o.OccurredAt, before, after));
        Assert.All(fake.Operations, o => Assert.Equal(0m, o.ComputedCost.Amount));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM benchmark_observations"));

        // No transaction was open while the external tool ran.
        Assert.True(observer.Observations > 50);
        Assert.True(observer.MostOpen == 0, string.Join(" ## ", observer.Seen));

        // The readers label the demonstration: the produced item's lines are never observed.
        var lines = ManagementComposers.Compose(await new NpgsqlCompanyRecordReader(Source).ReadAsync(null, CancellationToken.None)).Lines
            .Where(l => l.Key is ReportLineKey.ProducedItemOperations or ReportLineKey.ProducedItemRuntime or ReportLineKey.ProducedItemStages).ToArray();
        Assert.Equal(3, lines.Length);
        Assert.All(lines, l => Assert.Equal(FigureCase.Demonstration, l.Figure.Case));
        Assert.All(lines, l => Assert.Contains("DEMONSTRATION", l.Figure.Describe(), StringComparison.Ordinal));

        // The delivered weekly cost lines, every brief line and every dashboard tile of the demonstration store read the
        // fake operations as demonstration, never as observed spend.
        var managed = ManagementReportService.Compose(await new NpgsqlCompanyRecordReader(Source).ReadAsync(null, CancellationToken.None));
        Assert.DoesNotContain(managed.Reports.Lines, l => l.Figure.Case is FigureCase.Observed or FigureCase.ObservedZero);
        Assert.Equal(FigureCase.Demonstration, managed.Reports.Lines.First(l => l.Key == ReportLineKey.WeekCost).Figure.Case);
        Assert.DoesNotContain(managed.Tiles, t => t.Case is FigureCase.Observed or FigureCase.ObservedZero);
        Assert.DoesNotContain(managed.Tiles, t => t.Rendering.Contains("[observed", StringComparison.Ordinal));

        // The publish-ready predicate refuses, naming the held stages.
        var dossier = (await new NpgsqlItemDossierReader(Source).DossierAsync(record.Item, version, CancellationToken.None))!;
        var verdict = PublishReadyPredicate.Evaluate(dossier.Dossier, GateState.AwaitingRightsCheck, [], dossier.Channel, null);
        Assert.Contains(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.StageHeld && r.Detail.Contains("Production", StringComparison.Ordinal));
        Assert.Contains(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.StageHeld && r.Detail.Contains("CopyrightCheck", StringComparison.Ordinal));

        // Plan-only (REWRITTEN under the Planning Gate's ruling): it previews the own mode over a fixture installation of the
        // in-house model — files read, no process started — and prints the cap and that no metered spend is planned; nothing
        // is called, no version is opened, no file is written.
        var files = Directory.GetFiles(_output, "*", SearchOption.AllDirectories).Length;
        var planOutput = new StringWriter();
        using (var installation = new MediaCompany.OwnVoice.Fixtures.FixtureInstallation("deterministic", TimeSpan.FromSeconds(60)))
        {
            var plan = await new ProduceItemService(unitOfWork, reader, new StartsNothing(), store, settings with { InHouseModel = installation.Settings() }, planOutput)
                .RunAsync(Request(record, loaded, ProductionMode.PlanOnly), null, CancellationToken.None);
            Assert.True(plan.ExitCode == ProduceResult.Succeeded, planOutput.ToString());
        }

        Assert.Contains("total: no metered spend is planned, USD 0.00; local compute unmeasured", planOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("source: InHouseModel, for every part", planOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("item cap 5.95", planOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(files, Directory.GetFiles(_output, "*", SearchOption.AllDirectories).Length);
        Assert.Equal(14L, await ScalarAsync<long>("SELECT count(*) FROM agent_costs"));
        Assert.Equal(2, (await reader.PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!.HighestVersion.Value);

        // The vendor ESTIMATE the plan-only section asserted before, on a direct composition of the vendor plan.
        var vendorPlan = ProductionPlan.Compose(
            (await reader.PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!,
            NarrationSplitter.Split(loaded.Narration, loaded.Material.Beats, settings.NarrationCharacterMaximum!.Value),
            await reader.PricingAsync(CapabilityClass.Narration, CancellationToken.None)) with
        {
            Selection = new NarrationSelection(NarrationSource.Vendor, "named explicitly below the narration source rule: the vendor path under test", [], []),
        };
        var vendorOutput = new StringWriter();
        vendorPlan.Print(vendorOutput, ProductionMode.Metered, StoreDesignation.Demonstration, settings, loaded.Material);
        Assert.Contains("ESTIMATE", vendorOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("item cap 5.95", vendorOutput.ToString(), StringComparison.Ordinal);

        // THE VENDOR PATH UNDER TEST, BELOW THE RULE (rewritten under the Planning Gate's ruling): the vendor adapter over a
        // stub handler, a fake secret, the same service through its internal entry naming the vendor source; a render bound
        // of one millisecond ends the Production stage failed by name. Through the rule, metered mode is refused first.
        var variable = CredentialBrokerFactory.VariableName(Speech, null);
        // The variable's existing value is NEVER READ: it could hold a real secret. The fake value is set in this
        // process only, and removed from this process afterwards; nothing outside the process changes.
        Environment.SetEnvironmentVariable(variable, FakeSecret);
        try
        {
            var stub = new SpeechStub();
            var broker = CredentialBrokerFactory.Create(new SecretStoreOptions(), () => DateTimeOffset.UtcNow);
            var metered = CapabilityGatewayFactory.Create(unitOfWork, broker, broker, new FixedClock(),
                [new ProviderEndpoint(Speech, new Uri("https://speech.stub.invalid/"), VendorContract.SpeechAudio)], new HttpClient(stub));
            var bounded = settings with { RenderBound = TimeSpan.FromMilliseconds(1) };
            var meteredOutput = new StringWriter();
            var barred = await new ProduceItemService(unitOfWork, reader, new ExternalMediaTool(bounded), store, bounded, meteredOutput)
                .RunAsync(Request(record, loaded, ProductionMode.Metered), metered, CancellationToken.None);
            Assert.Equal(ProduceResult.RefusedBeforeAnyCall, barred.ExitCode);
            Assert.Null(barred.Version);
            Assert.Equal(0, stub.Requests);
            Assert.Contains("2026-10-10", meteredOutput.ToString(), StringComparison.Ordinal);

            var second = await new ProduceItemService(unitOfWork, reader, new ExternalMediaTool(bounded), store, bounded, meteredOutput)
                .RunBelowTheRuleAsync(Request(record, loaded, ProductionMode.Metered), NarrationSource.Vendor, metered, CancellationToken.None);

            Assert.Equal(ProduceResult.StageFailed, second.ExitCode);
            Assert.Equal(14, stub.Requests);
            var secondArtifacts = await reader.ArtifactsAsync(record.Item, second.Version!.Value, CancellationToken.None);
            Assert.DoesNotContain(secondArtifacts, a => a.Role == ArtifactRole.RenderedVideo);
            var failed = (await reader.StagesAsync(record.Item, second.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Production);
            Assert.Equal(StageOutcome.Failed, failed.Outcome);
            Assert.Contains("passed its configured bound", failed.Summary, StringComparison.Ordinal);

            // One input, one byte sequence: the narration and every still repeat.
            Assert.Equal(narration.Sha256, Assert.Single(secondArtifacts, a => a.Role == ArtifactRole.Narration).Sha256);
            foreach (var still in artifacts.Where(a => a.Role is ArtifactRole.GraphicStill or ArtifactRole.ThumbnailCandidate or ArtifactRole.ClipPlaceholder))
            {
                Assert.Equal(still.Sha256, Assert.Single(secondArtifacts, a => a.Implements == still.Implements && a.Role == still.Role).Sha256);
            }

            // The metered operations book the character units at the configured price, labelled estimate, never zero.
            Assert.All(second.Operations, o => Assert.Equal(CostBasis.Estimate, o.CostBasis));
            Assert.Equal(11_096, second.Operations.Sum(o => o.Units.CharacterUnits));
            Assert.Equal(11_096 * 0.000015m, second.Operations.Sum(o => o.ComputedCost.Amount));

            var everything = output + meteredOutput.ToString() + string.Join("\n", second.Operations.Select(o => o.ToString()));
            Assert.DoesNotContain(FakeSecret, everything, StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    /// <summary>
    /// AN INJECTED INTERRUPTION between the promotion of a file and its record leaves ZERO RECORDS for the file: the
    /// file is complete on disk, and nothing claims it. THE RUN STILL ENDS IN A NAMED, RECORDED OUTCOME (correction
    /// CR-005): the stage in progress is recorded failed, naming the failure, and the run ends with exit code 3, never
    /// an unhandled exception.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AnInterruptionBeforeTheRecordLeavesNoRecordForTheFile()
    {
        var record = await PrepareAsync();
        var loaded = await ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);
        var settings = MediaToolOnPath.Settings(_output, Repository());
        var store = new InterruptedStore(new ArtifactWriter(_output, Repository()), "gfx-05.png");
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());
        var reader = new NpgsqlProductionReader(Source);
        var service = new ProduceItemService(unitOfWork, reader, new ExternalMediaTool(settings), store, settings, new StringWriter());

        var ended = await service.RunAsync(Request(record, loaded, ProductionMode.Fake),
            CapabilityGatewayFactory.CreateDemonstration(unitOfWork, [new DemonstrationProvider(Speech, VendorContract.SpeechAudio)], () => DateTimeOffset.UtcNow),
            CancellationToken.None);

        Assert.Equal(ProduceResult.StageFailed, ended.ExitCode);
        Assert.True(File.Exists(store.Interrupted));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM production_artifacts WHERE relative_path LIKE '%gfx-05.png'"));
        Assert.Equal(4L, await ScalarAsync<long>("SELECT count(*) FROM production_artifacts WHERE role = 'GraphicStill'"));
        var design = (await reader.StagesAsync(record.Item, ended.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Design);
        Assert.Equal(StageOutcome.Failed, design.Outcome);
        Assert.Contains("IOException", design.Summary, StringComparison.Ordinal);
        Assert.Contains("induced interruption", design.Summary, StringComparison.Ordinal);
    }

    /// <summary>
    /// AN OPERATOR'S CANCELLATION DURING A VENDOR CALL, end to end through the produce service (corrections CR-003 and
    /// CR-009), on THE VENDOR PATH UNDER TEST, BELOW THE RULE (rewritten under the Planning Gate's ruling: the service's
    /// internal entry names the vendor source, since the rule refuses metered mode): the stub vendor receives the first
    /// narration request and the operator cancels while it is unanswered.
    /// The attempt is booked at its worst case, labelled estimate, under its reservation; the Audio stage is recorded
    /// failed naming the cancellation; nothing after it runs; the run ends with exit code 6.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task ACancellationDuringAVendorCallBooksTheAttemptRecordsTheStageAndEndsWithItsCode()
    {
        var record = await PrepareAsync();
        var loaded = await ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);
        var settings = MediaToolOnPath.Settings(_output, Repository());
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());
        var reader = new NpgsqlProductionReader(Source);
        var variable = CredentialBrokerFactory.VariableName(Speech, null);

        // The variable's existing value is NEVER READ: the fake value is set in this process only, and removed after.
        Environment.SetEnvironmentVariable(variable, FakeSecret);
        try
        {
            using var cancellation = new CancellationTokenSource();
            var stub = new CancellingStub(cancellation);
            var broker = CredentialBrokerFactory.Create(new SecretStoreOptions(), () => DateTimeOffset.UtcNow);
            var metered = CapabilityGatewayFactory.Create(unitOfWork, broker, broker, new FixedClock(),
                [new ProviderEndpoint(Speech, new Uri("https://speech.stub.invalid/"), VendorContract.SpeechAudio)], new HttpClient(stub));
            var output = new StringWriter();

            var ended = await new ProduceItemService(unitOfWork, reader, new ExternalMediaTool(settings), new ArtifactWriter(_output, Repository()), settings, output)
                .RunBelowTheRuleAsync(Request(record, loaded, ProductionMode.Metered), NarrationSource.Vendor, metered, cancellation.Token);

            Assert.Equal(ProduceResult.Cancelled, ended.ExitCode);
            Assert.Equal(1, stub.Requests);
            var operation = Assert.Single(await ScalarsAsync($"SELECT operation_id::text FROM agent_costs WHERE item_id = '{record.Item}'"));
            Assert.Equal("Estimate", await ScalarAsync<string>($"SELECT cost_basis FROM agent_costs WHERE operation_id = '{operation}'"));
            Assert.Equal("Failed", await ScalarAsync<string>($"SELECT outcome FROM agent_costs WHERE operation_id = '{operation}'"));
            Assert.Contains("cancelled by the operator", await ScalarAsync<string>($"SELECT failure_reason FROM agent_costs WHERE operation_id = '{operation}'"), StringComparison.Ordinal);
            Assert.Equal(await ScalarAsync<long>($"SELECT character_units FROM admission_reservations WHERE operation_id = '{operation}'"),
                await ScalarAsync<long>($"SELECT character_units FROM agent_costs WHERE operation_id = '{operation}'"));
            var stages = await reader.StagesAsync(record.Item, ended.Version!.Value, CancellationToken.None);
            var audio = stages.Single(s => s.Stage == ProductionStage.Audio);
            Assert.Equal(StageOutcome.Failed, audio.Outcome);
            Assert.Contains("cancelled by the operator", audio.Summary, StringComparison.Ordinal);
            Assert.DoesNotContain(stages, s => s.Stage is ProductionStage.Thumbnail or ProductionStage.Production);
            Assert.DoesNotContain(FakeSecret, output.ToString(), StringComparison.Ordinal);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    /// <summary>
    /// THE PROCESS CLOCK ONE DAY AHEAD AND ONE DAY BEHIND the datastore's (correction CR-009): in fake mode, with the
    /// render bound set to fail fast, every stage instant and every operation instant of each run is the datastore's,
    /// never the process clock's, whichever way the process clock is wrong.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task EveryInstantIsTheDatastoresWithTheProcessClockADayAheadOrBehind()
    {
        var record = await PrepareAsync();
        var loaded = await ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);
        var settings = MediaToolOnPath.Settings(_output, Repository()) with { RenderBound = TimeSpan.FromMilliseconds(1) };
        var reader = new NpgsqlProductionReader(Source);

        foreach (var skew in new[] { TimeSpan.FromDays(1), TimeSpan.FromDays(-1) })
        {
            var unitOfWork = new NpgsqlUnitOfWork(Source, new SkewedClock(skew));
            var before = await ScalarAsync<DateTimeOffset>("SELECT clock_timestamp()");
            var ended = await new ProduceItemService(unitOfWork, reader, new ExternalMediaTool(settings), new ArtifactWriter(_output, Repository()), settings, new StringWriter())
                .RunAsync(Request(record, loaded, ProductionMode.Fake),
                    CapabilityGatewayFactory.CreateDemonstration(unitOfWork, [new DemonstrationProvider(Speech, VendorContract.SpeechAudio)], () => DateTimeOffset.UtcNow + skew),
                    CancellationToken.None);
            var after = await ScalarAsync<DateTimeOffset>("SELECT clock_timestamp()");

            Assert.Equal(ProduceResult.StageFailed, ended.ExitCode);
            Assert.Equal(14, ended.Operations.Count);
            Assert.All(ended.Operations, o => Assert.InRange(o.OccurredAt, before, after));
            var stages = await reader.StagesAsync(record.Item, ended.Version!.Value, CancellationToken.None);
            Assert.Equal(12, stages.Count);
            Assert.All(stages, s => Assert.InRange(s.RecordedAt, before, after));
            Assert.All(await reader.ArtifactsAsync(record.Item, ended.Version!.Value, CancellationToken.None), a => Assert.InRange(a.RecordedAt!.Value, before, after));
        }
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    private static PreparationRecord Preparation() =>
        ProductionCommands.ReadPreparation(Path.Combine(Repository(), "config", "preparation-item-001.json"));

    private async Task<PreparationRecord> PrepareAsync()
    {
        var record = Preparation();
        var outcome = await new NpgsqlPreparationWriter(Source).PrepareAsync(record, StoreDesignation.Demonstration, CancellationToken.None);
        Assert.False(outcome.Refused, string.Join("; ", outcome.Refusals));
        return record;
    }

    /// <summary>A second item of the prepared channel with a small cap: a fixture of this self-dropping store only.</summary>
    private async Task<ItemId> SecondItemAsync(PreparationRecord record, decimal cap)
    {
        var item = ItemId.New();
        await ExecuteAsync($"INSERT INTO items (item_id, channel_id, item_version, title) VALUES ('{item}', '{record.Channel}', 1, 'a fixture item')");
        await ExecuteAsync($"INSERT INTO item_caps (item_id, amount, currency, source, recorded_at) VALUES ('{item}', {cap.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 'USD', 'a demonstration fixture cap', now())");
        return item;
    }

    private static ProduceRequest Request(PreparationRecord record, LoadedItem loaded, ProductionMode mode) => new()
    {
        Item = record.Item,
        Mode = mode,
        Loaded = loaded,
        Department = record.Department,
        Agent = record.Agent,
        Designation = StoreDesignation.Demonstration,
    };

    private static CapabilityRequest Request(PreparationRecord record, int characters, ItemId? item = null) => new(
        CapabilityClass.Narration, ReasoningTier.Light, new QualityRating(0), new ContextCapacity(0), new Money(5.95m), Criticality.Routine,
        new Attribution(item ?? record.Item, record.Channel, record.Department, record.Agent),
        new EstimatedUnits(0, 0, 0) { CharacterUnits = characters }, TimeSpan.Zero, ReducedFloorPolicy.Forbidden)
    {
        Payload = new CapabilityPayload.Narration(new string('a', characters), "voice", "wav"),
    };

    private static CapabilityInvocationContext Context() => new()
    {
        Run = RunId.New(),
        Job = JobId.New(),
        Stage = LifecyclePosition.Assembly,
        Actor = WorkforceRole.Producer,
        Attempt = 1,
    };

    private static CapabilityGateway Gateway(IUnitOfWork unitOfWork, IProviderAdapter adapter) =>
        new(unitOfWork, new OpenBroker(), [adapter], TimeSpan.FromSeconds(60));

    private static ProviderAttempt Succeeded() =>
        new(true, UnitCounts.None, CostBasis.Estimate, TimeSpan.FromMilliseconds(5), null, null)
        {
            Content = new ProducedContent(ContentKind.Audio, "audio/wav", FakeSpeechAudio.Wave("stand-in")),
            ChargeKnown = false,
        };

    private OperationDraft Draft(UnitCounts units) => new()
    {
        Id = OperationId.New(),
        Run = RunId.New(),
        Attribution = new Attribution(Preparation().Item, Preparation().Channel, Preparation().Department, Preparation().Agent),
        Capability = CapabilityClass.Narration,
        Route = Preparation().Routes[0].Id,
        Model = Preparation().Models[0].Id,
        Units = units,
        CostBasis = CostBasis.Measurement,
        Duration = TimeSpan.FromSeconds(1),
        Outcome = OperationOutcome.Succeeded,
        OccurredAt = DateTimeOffset.UnixEpoch,
        Attempt = 1,
    };

    /// <summary>A model of this self-dropping store only, recorded as billed by the kinds named; it has no price until one is added.</summary>
    private Task FixtureModelAsync(string model, string[] billed) =>
        ExecuteAsync("INSERT INTO models (model_id, provider_account_id, rated_quality, context_capacity, modality, billed_kinds) "
            + $"VALUES ('{model}', 'openai', 0, 0, 'image', ARRAY[{string.Join(", ", billed.Select(k => $"'{k}'"))}]::text[])");

    /// <summary>A price row of this self-dropping store only, in force from a day before the datastore's instant.</summary>
    private Task FixturePriceAsync(string model, string kind, decimal price) =>
        ExecuteAsync("INSERT INTO model_prices (model_price_id, model_id, unit_kind, unit_price, currency, source, verified_on, valid_from) "
            + $"VALUES ('{Guid.NewGuid()}', '{model}', '{kind}', {price.ToString(System.Globalization.CultureInfo.InvariantCulture)}, 'USD', "
            + "'a demonstration fixture price', current_date, now() - interval '1 day')");

    private async Task ExecuteAsync(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
    }

    private async Task<IReadOnlyList<string>> ScalarsAsync(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();
        var values = new List<string>();
        while (await reader.ReadAsync())
        {
            values.Add(reader.GetString(0));
        }

        return values;
    }

    private async Task<T> ScalarAsync<T>(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        return reader.GetFieldValue<T>(0);
    }

    private async Task AssertRefusedAsync(string sql, string fragment)
    {
        var refused = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(sql));
        Assert.Contains(fragment, refused.MessageText, StringComparison.Ordinal);
    }

    private sealed class FixedClock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
    }

    /// <summary>A process clock set wrong by a fixed offset; the datastore's clock is the only one any record reads.</summary>
    private sealed class SkewedClock(TimeSpan skew) : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow + skew;
    }

    /// <summary>
    /// A stub vendor that receives the request and never answers: on the first request it cancels the operator's token
    /// and waits on it, standing in for an operator pressing cancel while a vendor call is in flight.
    /// </summary>
    private sealed class CancellingStub(CancellationTokenSource operatorCancellation) : HttpMessageHandler
    {
        private int _requests;

        public int Requests => _requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            Assert.Equal($"Bearer {FakeSecret}", request.Headers.Authorization?.ToString());
            await operatorCancellation.CancelAsync();
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("unreachable: the wait ends only by cancellation");
        }
    }

    /// <summary>A stand-in provider declared here: it reaches nothing and answers from the function it is given.</summary>
    private sealed class StandIn(Func<ProviderAttempt> answer) : IProviderAdapter
    {
        private int _calls;

        public int Calls => _calls;

        public ProviderAccountId ProviderAccount => Speech;

        public Task<ProviderAttempt> InvokeAsync(RouteTarget.ProviderRoute route, CapabilityRequest request, ScopedHandle handle, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _calls);
            return Task.FromResult(answer());
        }
    }

    /// <summary>A broker double that issues a handle and holds no secret.</summary>
    private sealed class OpenBroker : ICredentialBroker
    {
        public Task<CredentialOutcome> IssueAsync(CredentialRequest request, CancellationToken cancellationToken)
        {
            var constructor = typeof(ScopedHandle).GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Single(c => c.GetParameters().Length == 10);
            var handle = (ScopedHandle)constructor.Invoke(
            [
                Guid.NewGuid(), request.CredentialClass, request.ProviderAccount, request.Channel, request.Capability,
                request.Job, request.Stage, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddMinutes(5), 1,
            ]);
            return Task.FromResult<CredentialOutcome>(new CredentialOutcome.Issued(handle));
        }
    }

    /// <summary>
    /// A unit of work whose ADMISSION transaction is lost at the operation write after the call, and whose recording
    /// transaction cannot be opened: the attempt is incurred and never recorded. The companion transaction is real.
    /// </summary>
    private sealed class LosingUnitOfWork(NpgsqlUnitOfWork inner) : IUnitOfWork
    {
        private int _begun;

        public async Task<IWorkTransaction> BeginAsync(CancellationToken cancellationToken)
        {
            var transaction = await inner.BeginAsync(cancellationToken);
            return Interlocked.Increment(ref _begun) == 1 ? new Losing(transaction) : transaction;
        }

        public Task<IWorkTransaction> BeginAsync(TimeSpan commandTimeout, CancellationToken cancellationToken) =>
            throw new TimeoutException("induced: the recording transaction could not be opened");

        private sealed class Losing(IWorkTransaction inner) : IWorkTransaction
        {
            public IAuditAppender Audit => inner.Audit;
            public IOperationRecorder Operations { get; } = new Lost();
            public IJobWriter Jobs => inner.Jobs;
            public IBudgetEvaluator Budgets => inner.Budgets;
            public IGateWriter Gates => inner.Gates;
            public IRouteAvailabilityWriter Availability => inner.Availability;
            public IDispatchWriter Dispatches => inner.Dispatches;
            public IDossierWriter Dossiers => inner.Dossiers;
            public IAdmissionLedger Admission => inner.Admission;
            public IBenchmarkWriter Benchmarks => inner.Benchmarks;

            public Task CommitAsync(CancellationToken cancellationToken) => inner.CommitAsync(cancellationToken);

            public ValueTask DisposeAsync() => inner.DisposeAsync();

            private sealed class Lost : IOperationRecorder
            {
                public Task<OperationRecord> RecordAsync(OperationDraft draft, CancellationToken cancellationToken) =>
                    throw new NpgsqlException("induced: the admission transaction was lost after the provider call");
            }
        }
    }

    /// <summary>A stub handler for the speech path: it answers every request itself with the fake wave of the request's text.</summary>
    private sealed class SpeechStub : HttpMessageHandler
    {
        private int _requests;

        public int Requests => _requests;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requests);
            Assert.Equal($"Bearer {FakeSecret}", request.Headers.Authorization?.ToString());
            using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(FakeSpeechAudio.Wave(body.RootElement.GetProperty("input").GetString()!)) };
            response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");
            return response;
        }
    }

    /// <summary>
    /// Counts the CLIENT transactions open in this database, from a connection of its own, each time the tool is about to
    /// run. The datastore's own autovacuum workers also carry a transaction start and are not the company's transactions,
    /// so they are not counted; a failure names every transaction it saw.
    /// </summary>
    private sealed class TransactionObserver(NpgsqlDataSource source)
    {
        public int MostOpen { get; private set; }

        public int Observations { get; private set; }

        /// <summary>What each open transaction was, where one was seen, so a failure names it.</summary>
        public List<string> Seen { get; } = [];

        public async Task ObserveAsync()
        {
            await using var command = source.CreateCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid() AND xact_start IS NOT NULL AND backend_type = 'client backend'");
            var open = (int)(long)(await command.ExecuteScalarAsync())!;
            if (open > 0)
            {
                await using var detail = source.CreateCommand(
                    "SELECT string_agg(backend_type || ':' || state || ':' || left(query, 200), ' || ') FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid() AND xact_start IS NOT NULL AND backend_type = 'client backend'");
                Seen.Add(await detail.ExecuteScalarAsync() as string ?? "?");
            }

            MostOpen = Math.Max(MostOpen, open);
            Observations++;
        }
    }

    /// <summary>The media tool, observed: before each invocation, the transactions open in the store are counted.</summary>
    private sealed class ObservedTool(IMediaTool inner, TransactionObserver observer) : IMediaTool
    {
        public async Task<string> VersionLineAsync(CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            return await inner.VersionLineAsync(cancellationToken);
        }

        public async Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            await inner.DrawStillAsync(still, cancellationToken);
        }

        public async Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            await inner.ConcatenateAudioAsync(stagingFolder, listFile, outputFile, sampleFormat, cancellationToken);
        }

        public async Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            return await inner.MeasureAudioAsync(file, cancellationToken);
        }

        public async Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            var rendering = inner.RenderAsync(render, cancellationToken);
            await Task.Delay(500, CancellationToken.None);
            await observer.ObserveAsync();
            await rendering;
        }

        public async Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            return await inner.ProbeAsync(file, cancellationToken);
        }

        public async Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            return await inner.DecodeAsync(file, cancellationToken);
        }
    }

    /// <summary>A process starter that starts nothing and fails the test if a plan ever asks it to.</summary>
    private sealed class StartsNothing : IMediaTool
    {
        private static InvalidOperationException Refused() => new("plan-only started a process");

        public Task<string> VersionLineAsync(CancellationToken cancellationToken) => throw Refused();

        public Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken) => throw Refused();

        public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken) => throw Refused();

        public Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken) => throw Refused();

        public Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken) => throw Refused();

        public Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken) => throw Refused();

        public Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken) => throw Refused();
    }

    /// <summary>The writer, interrupted once: after a named file is promoted, and before it can be recorded.</summary>
    private sealed class InterruptedStore(IArtifactStore inner, string interruptAfter) : IArtifactStore
    {
        public string? Interrupted { get; private set; }

        public string OutputRoot => inner.OutputRoot;

        public string BeginRun(string runFolder) => inner.BeginRun(runFolder);

        public Task WriteStagingTextAsync(string stagingFolder, string name, string text, CancellationToken cancellationToken) =>
            inner.WriteStagingTextAsync(stagingFolder, name, text, cancellationToken);

        public Task CopyIntoStagingAsync(string stagingFolder, string sourceFile, string name, CancellationToken cancellationToken) =>
            inner.CopyIntoStagingAsync(stagingFolder, sourceFile, name, cancellationToken);

        public Task<StoredFile> StoreAsync(string stagingFolder, ReadOnlyMemory<byte> bytes, string relativePath, CancellationToken cancellationToken) =>
            inner.StoreAsync(stagingFolder, bytes, relativePath, cancellationToken);

        public async Task<StoredFile> PromoteAsync(string stagingFolder, string stagingName, string relativePath, CancellationToken cancellationToken)
        {
            var stored = await inner.PromoteAsync(stagingFolder, stagingName, relativePath, cancellationToken);
            if (relativePath.EndsWith(interruptAfter, StringComparison.Ordinal))
            {
                Interrupted = stored.FullPath;
                throw new IOException("induced interruption after the file was promoted and before it was recorded");
            }

            return stored;
        }

        public string FullPath(string relativePath) => inner.FullPath(relativePath);
    }
}

/// <summary>A datastore demonstration that also needs the media tool and the font: not-run, saying why, where either is absent.</summary>
public sealed class RequiresPostgresAndMediaToolFactAttribute : FactAttribute
{
    public RequiresPostgresAndMediaToolFactAttribute()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            Skip = "not-run: no PostgreSQL datastore is reachable. Set MEDIACOMPANY_TEST_CONNECTION_STRING to run.";
        }
        else if (!MediaToolOnPath.Present)
        {
            Skip = "not-run: the media tool or the fixed font is not installed on this machine.";
        }
    }
}

/// <summary>Where the media tool and the fixed font are, for these demonstrations only; found on the search path.</summary>
internal static class MediaToolOnPath
{
    internal static string? Renderer { get; } = OnPath(OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg");

    internal static string? Probe { get; } = OnPath(OperatingSystem.IsWindows() ? "ffprobe.exe" : "ffprobe");

    internal static string? Font { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "arial.ttf") is var f && File.Exists(f) ? f : null;

    internal static bool Present => Renderer is not null && Probe is not null && Font is not null;

    internal static ProductionSettings Settings(string outputRoot, string repositoryRoot) => new()
    {
        OutputRoot = outputRoot,
        RepositoryRoot = repositoryRoot,
        RendererPath = Renderer!,
        ProbePath = Probe!,
        FontFile = Font!,
        Width = 1920,
        Height = 1080,
        FramesPerSecond = 30,
        RenderBound = TimeSpan.FromMinutes(60),
        DecodeBound = TimeSpan.FromMinutes(60),
        ProbeBound = TimeSpan.FromSeconds(60),
        StillBound = TimeSpan.FromSeconds(60),
        ProviderCallBound = TimeSpan.FromSeconds(60),
        NarrationCharacterMaximum = 1_500,
        NarrationVoice = "a-demonstration-voice-the-fakes-ignore",
    };

    private static string? OnPath(string name) =>
        (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(d => Path.Combine(d.Trim(), name))
            .FirstOrDefault(File.Exists);
}
