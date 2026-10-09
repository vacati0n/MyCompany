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
        Assert.Equal("MediaCompany.Persistence.Schema.009-production.sql", SchemaInstaller.ResourceNames[^1]);
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
    /// A character-billed operation books a STATED cost from the price in force, carrying its source and date; a
    /// consumed kind with no price in force leaves the cost not stated under its named reason.
    /// </summary>
    [RequiresPostgresFact]
    public async Task ACharacterBilledOperationIsBookedAtAStatedCostAndAnUnpricedKindIsNot()
    {
        await PrepareAsync();
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());

        OperationRecord priced;
        OperationRecord unpriced;
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            priced = await transaction.Operations.RecordAsync(Draft(new UnitCounts(0, 0, 0, 0) { CharacterUnits = 1_000 }), CancellationToken.None);
            unpriced = await transaction.Operations.RecordAsync(Draft(new UnitCounts(0, 0, 0, 0) { ImageUnits = 1 }), CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.True(priced.CostStated);
        Assert.Equal(1_000 * 0.000015m, priced.ComputedCost.Amount);
        Assert.Equal(0.000015m, await ScalarAsync<decimal>($"SELECT applied_character_price FROM agent_costs WHERE operation_id = '{priced.Id}'"));
        Assert.Contains("ESTIMATE", await ScalarAsync<string>(
            $"SELECT p.source FROM agent_costs a JOIN model_prices p ON p.model_price_id = a.applied_price_id WHERE a.operation_id = '{priced.Id}'"), StringComparison.Ordinal);
        Assert.False(unpriced.CostStated);
        Assert.Equal(CostUnstatedReason.PriceNotInForce, unpriced.CostUnstatedReason);
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
    ///   * then plan-only mode, which calls nothing and writes nothing;
    ///   * then the METERED PATH below the host command, through the same service, with the vendor adapter over a stub
    ///     handler and a fake secret: the narration requests reach the stub, the narration repeats byte for byte, and a
    ///     render past its bound ends the Production stage failed by name with no rendered file recorded.
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
        Assert.Equal(0, observer.MostOpen);

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

        // Plan-only: the plan is printed, nothing is called, no version is opened, no file is written.
        var files = Directory.GetFiles(_output, "*", SearchOption.AllDirectories).Length;
        var planOutput = new StringWriter();
        var plan = await new ProduceItemService(unitOfWork, reader, tool, store, settings, planOutput)
            .RunAsync(Request(record, loaded, ProductionMode.PlanOnly), null, CancellationToken.None);
        Assert.Equal(ProduceResult.Succeeded, plan.ExitCode);
        Assert.Contains("ESTIMATE", planOutput.ToString(), StringComparison.Ordinal);
        Assert.Contains("item cap 5.95", planOutput.ToString(), StringComparison.Ordinal);
        Assert.Equal(files, Directory.GetFiles(_output, "*", SearchOption.AllDirectories).Length);
        Assert.Equal(14L, await ScalarAsync<long>("SELECT count(*) FROM agent_costs"));
        Assert.Equal(2, (await reader.PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!.HighestVersion.Value);

        // The metered path below the host command: the vendor adapter over a stub handler, a fake secret, the same
        // service; a render bound of one millisecond ends the Production stage failed by name.
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
            var second = await new ProduceItemService(unitOfWork, reader, new ExternalMediaTool(bounded), store, bounded, meteredOutput)
                .RunAsync(Request(record, loaded, ProductionMode.Metered), metered, CancellationToken.None);

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
    /// file is complete on disk, and nothing claims it.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AnInterruptionBeforeTheRecordLeavesNoRecordForTheFile()
    {
        var record = await PrepareAsync();
        var loaded = await ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);
        var settings = MediaToolOnPath.Settings(_output, Repository());
        var store = new InterruptedStore(new ArtifactWriter(_output, Repository()), "gfx-05.png");
        var unitOfWork = new NpgsqlUnitOfWork(Source, new FixedClock());
        var service = new ProduceItemService(unitOfWork, new NpgsqlProductionReader(Source), new ExternalMediaTool(settings), store, settings, new StringWriter());

        await Assert.ThrowsAsync<IOException>(() => service.RunAsync(Request(record, loaded, ProductionMode.Fake),
            CapabilityGatewayFactory.CreateDemonstration(unitOfWork, [new DemonstrationProvider(Speech, VendorContract.SpeechAudio)], () => DateTimeOffset.UtcNow),
            CancellationToken.None));

        Assert.True(File.Exists(store.Interrupted));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM production_artifacts WHERE relative_path LIKE '%gfx-05.png'"));
        Assert.Equal(4L, await ScalarAsync<long>("SELECT count(*) FROM production_artifacts WHERE role = 'GraphicStill'"));
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

    private async Task ExecuteAsync(string sql)
    {
        await using var command = Source.CreateCommand(sql);
        await command.ExecuteNonQueryAsync();
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

    /// <summary>Counts the transactions open in this database, from a connection of its own, each time the tool is about to run.</summary>
    private sealed class TransactionObserver(NpgsqlDataSource source)
    {
        public int MostOpen { get; private set; }

        public int Observations { get; private set; }

        public async Task ObserveAsync()
        {
            await using var command = source.CreateCommand(
                "SELECT count(*) FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid() AND xact_start IS NOT NULL");
            var open = (int)(long)(await command.ExecuteScalarAsync())!;
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

        public async Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            await inner.ConcatenateAudioAsync(stagingFolder, listFile, outputFile, cancellationToken);
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
