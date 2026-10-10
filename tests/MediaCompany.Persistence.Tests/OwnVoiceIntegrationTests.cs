using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Publication;
using MediaCompany.Domain.Work;
using MediaCompany.Host;
using MediaCompany.OwnVoice.Fixtures;
using MediaCompany.Production;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// THE COMPANY'S OWN VOICE against the record store (the tenth schema resource and the change that comes with it).
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, in the separate demonstration database
/// only, prepared by the preparation command's own writer. The recordings are tones the test renders with the installed
/// media tool; the in-house model is the in-test stand-in process over a FIXTURE installation whose expected hashes the test
/// computes itself. No test runs the real model, reaches a network, books a cost or connects to the company store.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class OwnVoiceIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mediacompany-own-voice-" + Guid.NewGuid().ToString("N"));

    private NpgsqlDataSource Source => _dataSource ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private string Output => Path.Combine(_root, "output");

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
        try
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, recursive: true);
            }
        }
        catch (IOException)
        {
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
    // The tenth schema resource
    // -----------------------------------------------------------------------

    /// <summary>
    /// The tenth resource installs last and records nothing; it declares no binary column and every duration it holds is a
    /// generated column; over a store holding the nine resources AND ROWS OF THE COMPANY STORE'S SHAPE — designated company, a
    /// metered production version failed at Audio, one estimated operation of USD 0.007695 — it applies alone, changing no
    /// delivered row, and re-applies changing nothing; afterwards the version's source reads null, the own mode is admitted
    /// with a recording or the model and refused with another source; a full install over that store is refused, naming the
    /// option that applies from an ordinal.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheTenthResourceAppliesAloneOverACompanyShapedStoreChangingNoRowAndReappliesCleanly()
    {
        Assert.Equal("MediaCompany.Persistence.Schema.010-own-voice.sql", SchemaInstaller.ResourceNames[^1]);
        Assert.Equal(10, SchemaInstaller.ResourceNames.Count);
        foreach (var table in new[] { "recording_registrations", "registered_beat_files", "narration_measurements", "narration_provenance", "store_designation" })
        {
            Assert.Equal(0L, await ScalarAsync<long>($"SELECT count(*) FROM {table}"));
        }

        Assert.Equal(0L, await ScalarAsync<long>(
            "SELECT count(*) FROM information_schema.columns WHERE table_schema = current_schema() AND data_type IN ('bytea', 'oid') "
            + "AND table_name IN ('recording_registrations', 'registered_beat_files', 'narration_measurements', 'narration_provenance')"));
        Assert.Equal(2L, await ScalarAsync<long>(
            "SELECT count(*) FROM information_schema.columns WHERE table_schema = current_schema() AND column_name = 'duration_seconds' AND is_generated = 'ALWAYS'"));

        // A store of the company store's recorded shape, holding the nine resources only.
        await ThrowawayStore.DropAsync(Source);
        foreach (var resource in SchemaInstaller.ResourceNames.Take(9))
        {
            await ExecuteAsync(SchemaInstaller.ReadResource(resource));
        }

        await SchemaInstaller.SeedClosedActionSetAsync(Source, CancellationToken.None);
        var record = Preparation();
        var prepared = await new NpgsqlPreparationWriter(Source).PrepareAsync(record, StoreDesignation.Company, CancellationToken.None);
        Assert.False(prepared.Refused, string.Join("; ", prepared.Refusals));
        var unitOfWork = new NpgsqlUnitOfWork(Source, new Clock());
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            var opened = await transaction.Production.OpenProductionVersionAsync(record.Item, ProductionMode.Metered, CancellationToken.None);
            await transaction.Dossiers.RecordStageStampedAsync(record.Item, opened.Version, ProductionStage.Audio, StageOutcome.Failed, "a fixture of the recorded failure at Audio", null, CancellationToken.None);
            await transaction.Operations.RecordAsync(new OperationDraft
            {
                Id = OperationId.New(),
                Run = RunId.New(),
                Attribution = new Attribution(record.Item, record.Channel, record.Department, record.Agent),
                Capability = CapabilityClass.Narration,
                Route = record.Routes[0].Id,
                Model = record.Models[0].Id,
                Units = new UnitCounts(0, 0, 0, 0) { CharacterUnits = 513 },
                CostBasis = CostBasis.Estimate,
                Duration = TimeSpan.FromSeconds(1),
                Outcome = OperationOutcome.Failed,
                FailureReason = "a fixture of the recorded failed attempt",
                OccurredAt = DateTimeOffset.UnixEpoch,
                Attempt = 1,
            }, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        Assert.Equal(0.007695m, await ScalarAsync<decimal>("SELECT sum(computed_cost) FROM agent_costs"));
        var before = await DeliveredRowsAsync();
        Assert.True(before.Count > 40);

        await SchemaInstaller.InstallFromAsync(Source, 10, CancellationToken.None);
        Assert.Equal(before, await DeliveredRowsAsync(before));
        await SchemaInstaller.InstallFromAsync(Source, 10, CancellationToken.None);
        Assert.Equal(before, await DeliveredRowsAsync(before));
        Assert.Equal(0.007695m, await ScalarAsync<decimal>("SELECT sum(computed_cost) FROM agent_costs"));
        Assert.True(await ScalarAsync<bool>("SELECT narration_source IS NULL FROM production_versions"));

        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            await transaction.Production.OpenProductionVersionAsync(record.Item, ProductionMode.Own, NarrationSource.InHouseModel, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var refused = await Assert.ThrowsAsync<PostgresException>(async () =>
        {
            await using var transaction = await unitOfWork.BeginAsync(CancellationToken.None);
            await transaction.Production.OpenProductionVersionAsync(record.Item, ProductionMode.Own, NarrationSource.Fake, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        });
        Assert.Contains("production_versions_source_matches_mode", refused.ConstraintName ?? refused.MessageText, StringComparison.Ordinal);

        var full = await Assert.ThrowsAsync<SchemaInstallRefusedException>(() => SchemaInstaller.InstallAsync(Source, CancellationToken.None));
        Assert.Contains("install --from 10", full.Message, StringComparison.Ordinal);
        await Assert.ThrowsAsync<SchemaInstallRefusedException>(() => SchemaInstaller.InstallFromAsync(Source, 1, CancellationToken.None));
    }

    // -----------------------------------------------------------------------
    // Recording registration
    // -----------------------------------------------------------------------

    /// <summary>
    /// A COMPLETE THIRTEEN-FILE SET FOR ITEM 001 REGISTERS: each beat's STORED COPY is recorded with the SHA-256 of the bytes
    /// stored, its container, codec, rate, channels, sample format, decoded samples and loudness; the duration is the store's
    /// generated decoded samples over rate. ABSENT performer and release fields are recorded NULL and printed ABSENT, so a
    /// production passes the recording over, naming each absent field.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task ACompleteSetRegistersEveryBeatsStoredCopyAndAbsentFieldsReadAbsent()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        var folder = Beats(1.0, rendered: true);
        var output = new StringWriter();

        var result = await Registrar(output).RegisterAsync(new RegistrationRequest { Item = record.Item, Loaded = loaded, RecordingsFolder = folder }, CancellationToken.None);

        Assert.True(result.ExitCode == RegistrationResult.Registered, output.ToString());
        var registration = (await new NpgsqlProductionReader(Source).LatestRegistrationAsync(record.Item, new ItemVersion(1), CancellationToken.None))!;
        Assert.Equal(result.Registration!.Id, registration.Id);
        Assert.Equal(13, registration.Beats.Count);
        foreach (var beat in registration.Beats)
        {
            var stored = ArtifactWriterPath(beat.StoredPath);
            Assert.Equal(AudioFixtures.Sha256(stored), beat.Sha256);
            Assert.Equal(AudioFixtures.Sha256(Path.Combine(folder, $"{beat.Beat:00}.wav")), beat.Sha256);
            Assert.StartsWith(new ArtifactWriter(Output, Repository()).OutputRoot, stored, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(("wav", "pcm_s16le", 22_050, 1, "s16"), (beat.Measured.Container, beat.Measured.Codec, beat.Measured.SampleRate, beat.Measured.Channels, beat.Measured.SampleFormat));
            Assert.Equal((long)Math.Round(Seconds(1.0)(beat.Beat) * 22_050), beat.Measured.DecodedSamples);
            Assert.NotNull(beat.Measured.IntegratedLoudness);
        }

        Assert.Equal(13L, await ScalarAsync<long>(
            "SELECT count(*) FROM registered_beat_files WHERE duration_seconds = decoded_samples::numeric / sample_rate AND duration_basis = 'decoded sample count over sample rate'"));
        Assert.True(await ScalarAsync<bool>(
            "SELECT performer_name IS NULL AND release_document_reference IS NULL AND release_document_date IS NULL AND release_document_sha256 IS NULL AND release_training_term IS NULL FROM recording_registrations"));
        Assert.Contains("release document reference ABSENT", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("performer's name ABSENT", output.ToString(), StringComparison.Ordinal);
        Assert.Contains("NOT complete", output.ToString(), StringComparison.Ordinal);

        // The plan passes the incomplete recording over, naming every absent field; with no model configured it refuses.
        var planOutput = new StringWriter();
        var plan = await Service(planOutput, new NothingStarts()).RunAsync(Request(record, loaded, ProductionMode.PlanOnly), null, CancellationToken.None);
        Assert.Equal(ProduceResult.RefusedBeforeAnyCall, plan.ExitCode);
        Assert.Contains("recording passed over: registration " + registration.Id + " has no complete release; absent: release document reference, release document date, performer's name, release document SHA-256, model-training term",
            planOutput.ToString(), StringComparison.Ordinal);
    }

    /// <summary>
    /// MISSING, EXTRA, DUPLICATE AND WHOLE-NARRATION SETS, A TRUNCATED FILE, A FILE CARRYING A VIDEO STREAM, A SET OF MIXED
    /// FORMATS and A RELEASE DOCUMENT INSIDE THE REPOSITORY are each refused before anything is recorded, naming each finding;
    /// a release document outside the repository is hashed and copied nowhere.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task EveryUnregistrableSetIsRefusedNamingEachFindingAndRecordsNothing()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();

        async Task<string> Refused(string folder, string? document = null)
        {
            var output = new StringWriter();
            var result = await Registrar(output).RegisterAsync(
                new RegistrationRequest { Item = record.Item, Loaded = loaded, RecordingsFolder = folder, ReleaseDocumentPath = document }, CancellationToken.None);
            Assert.True(result.ExitCode == RegistrationResult.Refused, output.ToString());
            Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM recording_registrations"));
            Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM registered_beat_files"));
            return output.ToString();
        }

        var set = Beats(0.6);
        File.Delete(Path.Combine(set, "07.wav"));
        File.Copy(Path.Combine(set, "01.wav"), Path.Combine(set, "14.wav"));
        File.Copy(Path.Combine(set, "03.wav"), Path.Combine(set, "03.flac"));
        File.Copy(Path.Combine(set, "02.wav"), Path.Combine(set, "narration.wav"));
        var named = await Refused(set);
        Assert.Contains("beat 07 has no file", named, StringComparison.Ordinal);
        Assert.Contains("names beat 14", named, StringComparison.Ordinal);
        Assert.Contains("beat 03 has 2 files", named, StringComparison.Ordinal);
        Assert.Contains("narration.wav is not named by a two-digit beat number", named, StringComparison.Ordinal);

        var damaged = Beats(0.6);
        var truncated = Path.Combine(damaged, "05.wav");
        var bytes = await File.ReadAllBytesAsync(truncated);
        await File.WriteAllBytesAsync(truncated, bytes[..(bytes.Length / 2)]);
        File.Delete(Path.Combine(damaged, "09.wav"));
        AudioFixtures.Render(MediaToolOnPath.Renderer!, "-f", "lavfi", "-i", "color=c=black:s=64x64:r=5:d=1", "-f", "lavfi", "-i", "sine=frequency=500:sample_rate=22050:duration=1",
            "-c:v", "libx264", "-c:a", "aac", "-shortest", Path.Combine(damaged, "09.mp4"));
        var findings = await Refused(damaged);
        Assert.Contains("beat 05 (05.wav) does not decode end to end cleanly", findings, StringComparison.Ordinal);
        Assert.Contains("beat 09 (09.mp4) carries streams [video, audio]", findings, StringComparison.Ordinal);

        var mixed = Beats(0.6);
        await File.WriteAllBytesAsync(Path.Combine(mixed, "04.wav"), AudioFixtures.Wave(44_100, 2, 16, 0.6, 400, 0.2));
        var formats = await Refused(mixed);
        Assert.Contains("do not share one sample rate, channel count and sample format", formats, StringComparison.Ordinal);
        Assert.Contains("beat 04 is 44100 Hz, 2 channel(s), s16", formats, StringComparison.Ordinal);

        var inside = await Refused(Beats(0.6), Path.Combine(Repository(), "MediaCompany.slnx"));
        Assert.Contains("lies inside the repository", inside, StringComparison.Ordinal);

        // A document outside the repository is hashed and copied nowhere.
        Directory.CreateDirectory(_root);
        var release = Path.Combine(_root, "signed-release-fixture.txt");
        await File.WriteAllTextAsync(release, "a fixture release document");
        var output = new StringWriter();
        var registered = await Registrar(output).RegisterAsync(new RegistrationRequest
        {
            Item = record.Item, Loaded = loaded, RecordingsFolder = Beats(0.6), PerformerName = "A Fixture Performer", ReleaseDocumentReference = "fixture release 1",
            ReleaseDocumentDate = new DateOnly(2026, 10, 10), ReleaseDocumentPath = release, TrainingTerm = "model training not permitted",
        }, CancellationToken.None);
        Assert.True(registered.ExitCode == RegistrationResult.Registered, output.ToString());
        Assert.Equal(AudioFixtures.Sha256(release), await ScalarAsync<string>("SELECT release_document_sha256 FROM recording_registrations"));
        Assert.Empty(Directory.GetFiles(Output, "signed-release-fixture*", SearchOption.AllDirectories));
        Assert.Contains("the release is complete", output.ToString(), StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The zero-spend production from a registered recording, end to end
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE ZERO-SPEND PRODUCTION FROM A REGISTERED RECORDING, END TO END, in the own mode, against a fresh demonstration store:
    /// the plan names the recording for every part with its reason before anything runs; every part is the REGISTERED bytes,
    /// copied into the run by the one writer and recorded with the registered hash; every part and the joined narration are
    /// measured from decoded audio, the joined samples exactly the parts'; each beat's measurement sits beside its expectation;
    /// one rendered file decodes end to end with one video and one audio stream, its runtime within one frame of the
    /// narration's; no operation is booked and the cap's counted total is unchanged; no transaction is open while the tool
    /// runs; the publish-ready predicate refuses on the held stages.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AProductionFromARegisteredRecordingRendersOneDecodableFileAtZeroSpend()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        var registration = await RegisterCompleteAsync(record, loaded, 1.0, rendered: true);
        var reader = new NpgsqlProductionReader(Source);
        var capBefore = (await reader.PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!.Cap!.Counted();
        var observer = new TransactionObserver(Source);
        var output = new StringWriter();

        var produced = await Service(output, new ObservedTool(new ExternalMediaTool(Settings()), observer)).RunAsync(Request(record, loaded, ProductionMode.Own), null, CancellationToken.None);

        Assert.True(produced.ExitCode == ProduceResult.Succeeded, output.ToString());
        var text = output.ToString();
        Assert.True(text.IndexOf("source: Recording, for every part", StringComparison.Ordinal) < text.IndexOf("Opened item version", StringComparison.Ordinal));
        Assert.Equal(13, Enumerable.Range(1, 13).Count(n => text.Contains($"part {n} (beat {n}): Recording - registration {registration.Id}", StringComparison.Ordinal)));
        Assert.Contains("total: no metered spend is planned, USD 0.00; recording time unmeasured; local compute unmeasured", text, StringComparison.Ordinal);
        Assert.Equal(NarrationSource.Recording, await reader.VersionSourceAsync(record.Item, produced.Version!.Value, CancellationToken.None));

        var version = produced.Version!.Value;
        var artifacts = await reader.ArtifactsAsync(record.Item, version, CancellationToken.None);
        var parts = artifacts.Where(a => a.Role == ArtifactRole.NarrationPart).OrderBy(a => a.RelativePath, StringComparer.Ordinal).ToArray();
        Assert.Equal(13, parts.Length);
        Assert.Equal(registration.Beats.OrderBy(b => b.Beat).Select(b => b.Sha256), parts.Select(p => p.Sha256));
        foreach (var artifact in artifacts)
        {
            Assert.Equal(artifact.Sha256, AudioFixtures.Sha256(ArtifactWriterPath(artifact.RelativePath)));
        }

        var measurements = await reader.MeasurementsAsync(record.Item, version, CancellationToken.None);
        Assert.Equal(14, measurements.Count);
        var joined = Assert.Single(measurements, m => m.Beat is null);
        Assert.Equal(measurements.Where(m => m.Beat is not null).Sum(m => m.Measured.DecodedSamples), joined.Measured.DecodedSamples);
        Assert.Equal(registration.Beats.Sum(b => b.Measured.DecodedSamples), joined.Measured.DecodedSamples);
        Assert.Equal(1_929, joined.Words);
        Assert.Equal(771.6m, joined.ExpectedSeconds);
        Assert.All(measurements.Where(m => m.Beat is not null), m => Assert.Equal(Math.Round(m.Words!.Value * 0.4m, 1), m.ExpectedSeconds));
        Assert.All(await reader.ProvenanceAsync(record.Item, version, CancellationToken.None), p =>
        {
            Assert.Equal(NarrationSource.Recording, p.Source);
            Assert.Equal(registration.Id, p.Registration);
            Assert.Contains("model-training term model training not permitted", p.Release, StringComparison.Ordinal);
        });

        var rendered = produced.Rendered!;
        var probe = await new ExternalMediaTool(Settings()).ProbeAsync(rendered.FullPath, CancellationToken.None);
        Assert.Equal((1, 1, 2), (probe.Count("video"), probe.Count("audio"), probe.StreamKinds.Count));
        Assert.Equal(0, (await new ExternalMediaTool(Settings()).DecodeAsync(rendered.FullPath, CancellationToken.None)).Errors);
        var video = Assert.Single(artifacts, a => a.Role == ArtifactRole.RenderedVideo);
        Assert.True((video.MeasuredDuration!.Value - joined.Measured.Duration).Duration() <= TimeSpan.FromSeconds(1.0 / 30), $"{video.MeasuredDuration} against {joined.Measured.Duration}");

        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM agent_costs"));
        Assert.Empty(produced.Operations);
        Assert.Equal(capBefore, (await reader.PackageAsync(record.Item, new ItemVersion(1), CancellationToken.None))!.Cap!.Counted());
        var audio = (await reader.StagesAsync(record.Item, version, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Audio);
        Assert.Contains("MEASURED from decoded audio", audio.Summary, StringComparison.Ordinal);
        Assert.Contains("no operation is booked and no metered spend is planned or made, USD 0.00; recording time unmeasured", audio.Summary, StringComparison.Ordinal);
        Assert.True(observer.Observations > 50);
        Assert.True(observer.MostOpen == 0, string.Join(" ## ", observer.Seen));

        var dossier = (await new NpgsqlItemDossierReader(Source).DossierAsync(record.Item, version, CancellationToken.None))!;
        var verdict = PublishReadyPredicate.Evaluate(dossier.Dossier, GateState.AwaitingRightsCheck, [], dossier.Channel, null);
        Assert.Contains(verdict.Refusals, r => r.Refusal == PublishReadyRefusal.StageHeld && r.Detail.Contains("Production", StringComparison.Ordinal));
    }

    // -----------------------------------------------------------------------
    // The zero-spend production from the in-house model (the in-test stand-in), end to end
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE ZERO-SPEND PRODUCTION FROM THE IN-HOUSE MODEL, END TO END, with the in-test stand-in over a fixture installation and
    /// the process clock a day ahead of the datastore's: the plan names the model for every part with the verified files; each
    /// part's provenance is complete, its file hashes those of the files given to the process in that run, its argument list
    /// recorded, the first part generated twice and recorded NOT OBSERVED TO REPEAT for a non-deterministic stand-in (both
    /// hashes) and REPEATS for a deterministic one; the parts concatenate exactly to the narration; the planned source is the
    /// recorded source of every part; no operation is booked; every instant is the datastore's; no transaction is open while
    /// the stand-in runs.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AProductionFromTheInHouseModelRecordsCompleteProvenanceAtZeroSpend()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        var reader = new NpgsqlProductionReader(Source);

        foreach (var behaviour in new[] { "random", "deterministic" })
        {
            using var installation = new FixtureInstallation(behaviour, TimeSpan.FromSeconds(120));
            var model = installation.Settings() with { Generation = new VoiceGenerationSettings(0.1m, 0.667m, 0.8m, 0m, 1m, true) };
            var settings = Settings() with { InHouseModel = model };
            var observer = new TransactionObserver(Source);
            var tool = new ExternalMediaTool(settings);
            var output = new StringWriter();
            var before = await ScalarAsync<DateTimeOffset>("SELECT clock_timestamp()");

            var produced = await new ProduceItemService(new NpgsqlUnitOfWork(Source, new Clock(TimeSpan.FromDays(1))), reader, tool, new ArtifactWriter(Output, Repository()), settings, output,
                new ObservedVoice(tool, observer)).RunAsync(Request(record, loaded, ProductionMode.Own), null, CancellationToken.None);

            var after = await ScalarAsync<DateTimeOffset>("SELECT clock_timestamp()");
            Assert.True(produced.ExitCode == ProduceResult.Succeeded, output.ToString());
            var text = output.ToString();
            Assert.Contains("source: InHouseModel, for every part", text, StringComparison.Ordinal);
            Assert.Contains($"voice model {installation.Model}: sha256 {model.Model.Sha256}", text, StringComparison.Ordinal);
            Assert.Contains("total: no metered spend is planned, USD 0.00; local compute unmeasured", text, StringComparison.Ordinal);

            var version = produced.Version!.Value;
            Assert.Equal(NarrationSource.InHouseModel, await reader.VersionSourceAsync(record.Item, version, CancellationToken.None));
            var provenance = await reader.ProvenanceAsync(record.Item, version, CancellationToken.None);
            Assert.Equal(13, provenance.Count);
            foreach (var row in provenance)
            {
                Assert.Equal(NarrationSource.InHouseModel, row.Source);
                Assert.Equal("runtime-fixture", row.ModelName);
                Assert.Equal("1.0", row.ModelVersion);
                Assert.StartsWith("a-fixture-code-licence (read from the runtime's installed package metadata)", row.CodeLicence, StringComparison.Ordinal);
                Assert.StartsWith("xx_XX-fixture-test", row.VoiceName, StringComparison.Ordinal);
                Assert.StartsWith("a fixture dataset licence", row.DatasetLicence, StringComparison.Ordinal);
                Assert.Equal("a configured weights licence (CONFIGURED, not read from an installed file: a fixture source)", row.WeightsLicence);
                Assert.Equal("not stated in the installed files", row.VoiceLicence);
                Assert.Contains("noise scale 0.667 (the voice configuration states 0.667)", row.GenerationSettings, StringComparison.Ordinal);
                Assert.Contains("length scale 0.1 (the voice configuration states 1)", row.GenerationSettings, StringComparison.Ordinal);
                foreach (var file in new[] { installation.Interpreter, installation.Model, installation.Configuration, installation.ModelCard, installation.RuntimeRecord, installation.DependencyRecord })
                {
                    Assert.Contains($"{file} sha256 {AudioFixtures.Sha256(file)}", row.FileHashes, StringComparison.Ordinal);
                }

                Assert.Equal(4, row.RecordEntriesVerified);
                Assert.StartsWith($"{installation.Interpreter} -I -B -m runtime_fixture -m {installation.Model} -c {installation.Configuration} -i part-", row.Arguments, StringComparison.Ordinal);
                Assert.DoesNotContain("--data-dir", row.Arguments, StringComparison.Ordinal);
                Assert.InRange(row.RecordedAt!.Value, before, after);
            }

            var first = provenance.Single(p => p.RelativePath.EndsWith("part-001.wav", StringComparison.Ordinal));
            if (behaviour == "random")
            {
                Assert.StartsWith("not observed to repeat: part 1 generated twice with identical settings gave sha256 ", first.Repeatability, StringComparison.Ordinal);
            }
            else
            {
                Assert.StartsWith("repeats: part 1 generated twice", first.Repeatability, StringComparison.Ordinal);
            }

            Assert.All(provenance.Where(p => p != first), p => Assert.Equal("not observed to repeat: generated once", p.Repeatability));

            var measurements = await reader.MeasurementsAsync(record.Item, version, CancellationToken.None);
            Assert.Equal(1_929, measurements.Where(m => m.Beat is not null).Sum(m => m.Words));
            Assert.All(await reader.StagesAsync(record.Item, version, CancellationToken.None), s => Assert.InRange(s.RecordedAt, before, after));
            Assert.All(measurements, m => Assert.InRange(m.RecordedAt!.Value, before, after));
            Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM agent_costs"));
            var probe = await tool.ProbeAsync(produced.Rendered!.FullPath, CancellationToken.None);
            Assert.Equal((1, 1), (probe.Count("video"), probe.Count("audio")));
            Assert.True(observer.Observations >= 14);
            Assert.True(observer.MostOpen == 0, string.Join(" ## ", observer.Seen));
        }
    }

    // -----------------------------------------------------------------------
    // The round trip and the one clock: each reader of a registered or verified byte
    // -----------------------------------------------------------------------

    /// <summary>A REGISTERED RECORDING REPLACED AFTER REGISTRATION stops the production before anything is recorded, naming the file and both hashes.</summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task ARecordingReplacedAfterRegistrationStopsTheProductionBeforeNarrationNamingBothHashes()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        var registration = await RegisterCompleteAsync(record, loaded, 0.6);
        var beat = registration.Beats.Single(b => b.Beat == 6);
        await File.WriteAllBytesAsync(ArtifactWriterPath(beat.StoredPath), AudioFixtures.Wave(22_050, 1, 16, 0.6, 999, 0.3));
        var output = new StringWriter();

        var produced = await Service(output, new ExternalMediaTool(Settings())).RunAsync(Request(record, loaded, ProductionMode.Own), null, CancellationToken.None);

        Assert.Equal(ProduceResult.RefusedBeforeAnyCall, produced.ExitCode);
        Assert.Null(produced.Version);
        Assert.Contains($"beat 06's registered file {ArtifactWriterPath(beat.StoredPath)} changed after registration: sha256 {beat.Sha256} registered and {AudioFixtures.Sha256(ArtifactWriterPath(beat.StoredPath))} now",
            output.ToString(), StringComparison.Ordinal);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM production_versions"));
    }

    /// <summary>
    /// A STAND-IN THAT OUTLASTS THE MODEL PART BOUND: its process tree is terminated, the Audio stage is recorded failed
    /// naming the part and the configured bound, nothing of the part is recorded, the run ends with exit 3, and no
    /// transaction was open at any observation while it ran.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AStandInPastTheModelPartBoundIsTerminatedTheStageFailsAndNothingIsHeld()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        using var installation = new FixtureInstallation("slow", TimeSpan.FromSeconds(3));
        var settings = Settings() with { InHouseModel = installation.Settings() };
        var observer = new TransactionObserver(Source);
        var tool = new ExternalMediaTool(settings);
        var output = new StringWriter();

        var produced = await new ProduceItemService(new NpgsqlUnitOfWork(Source, new Clock()), new NpgsqlProductionReader(Source), tool, new ArtifactWriter(Output, Repository()), settings, output,
            new ObservedVoice(tool, observer, whileRunning: 5)).RunAsync(Request(record, loaded, ProductionMode.Own), null, CancellationToken.None);

        Assert.Equal(ProduceResult.StageFailed, produced.ExitCode);
        var audio = (await new NpgsqlProductionReader(Source).StagesAsync(record.Item, produced.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Audio);
        Assert.Equal(StageOutcome.Failed, audio.Outcome);
        Assert.Contains("part 1 (beat 1) passed the configured model part bound of 3 s on the monotonic clock; its process tree was terminated", audio.Summary, StringComparison.Ordinal);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM production_artifacts WHERE role IN ('NarrationPart', 'Narration')"));
        Assert.True(observer.Observations >= 5);
        Assert.True(observer.MostOpen == 0, string.Join(" ## ", observer.Seen));
    }

    /// <summary>
    /// A MODEL FILE SWAPPED BETWEEN THE PLAN'S VERIFICATION AND THE LOAD, and another BETWEEN TWO PARTS: each run stops before
    /// the part it would load, recording the Audio stage failed with the file and both hashes, exit 3.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AModelFileSwappedAfterVerificationOrBetweenPartsStopsTheRunNamingBothHashes()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        var reader = new NpgsqlProductionReader(Source);
        foreach (var swapAt in new[] { "after-plan", "between-parts" })
        {
            using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
            var model = installation.Settings() with { Generation = new VoiceGenerationSettings(0.1m, 0.667m, 0.8m, 0m, 1m, true) };
            var settings = Settings() with { InHouseModel = model };
            void Swap() => File.WriteAllText(installation.Model, "deterministic\na swapped model");
            var tool = new ExternalMediaTool(settings);
            IMediaTool media = swapAt == "after-plan" ? new SwapOnFirstStill(tool, Swap) : tool;
            IArtifactStore store = swapAt == "between-parts" ? new SwapBeforeText(new ArtifactWriter(Output, Repository()), "part-002.txt", Swap) : new ArtifactWriter(Output, Repository());
            var output = new StringWriter();

            var produced = await new ProduceItemService(new NpgsqlUnitOfWork(Source, new Clock()), reader, media, store, settings, output, tool)
                .RunAsync(Request(record, loaded, ProductionMode.Own), null, CancellationToken.None);

            Assert.True(produced.ExitCode == ProduceResult.StageFailed, output.ToString());
            var audio = (await reader.StagesAsync(record.Item, produced.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Audio);
            Assert.Contains("the narration source changed between the plan and part", audio.Summary, StringComparison.Ordinal);
            Assert.Contains(model.Model.Sha256, audio.Summary, StringComparison.Ordinal);
            Assert.Contains(AudioFixtures.Sha256(installation.Model), audio.Summary, StringComparison.Ordinal);
            var parts = (await reader.ArtifactsAsync(record.Item, produced.Version!.Value, CancellationToken.None)).Count(a => a.Role == ArtifactRole.NarrationPart);
            Assert.Equal(swapAt == "after-plan" ? 0 : 1, parts);
        }
    }

    /// <summary>
    /// THE PLAN'S SOURCE AGAINST THE PRODUCED SOURCE: a recording registered after the plan named the model (the store changed
    /// between plan and run) stops the run before the first part, naming the planned and the derived source; nothing mixes.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task ARecordingRegisteredBetweenThePlanAndTheFirstPartStopsTheRunNamingBothSources()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        var loaded = await LoadAsync();
        await RegisterCompleteAsync(record, loaded, 0.6);
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        var settings = Settings() with { InHouseModel = installation.Settings() };
        var tool = new ExternalMediaTool(settings);
        var output = new StringWriter();

        // The plan's read sees no registration; every later read sees the one recorded "meanwhile".
        var produced = await new ProduceItemService(new NpgsqlUnitOfWork(Source, new Clock()), new RegistrationAppearsAfterThePlan(new NpgsqlProductionReader(Source)), tool,
            new ArtifactWriter(Output, Repository()), settings, output, tool).RunAsync(Request(record, loaded, ProductionMode.Own), null, CancellationToken.None);

        Assert.True(produced.ExitCode == ProduceResult.StageFailed, output.ToString());
        Assert.Contains("source: InHouseModel, for every part", output.ToString(), StringComparison.Ordinal);
        var audio = (await new NpgsqlProductionReader(Source).StagesAsync(record.Item, produced.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Audio);
        Assert.Contains("the narration source changed between the plan and part 1: the plan named InHouseModel, and the store and files now derive Recording", audio.Summary, StringComparison.Ordinal);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM production_artifacts WHERE role = 'NarrationPart'"));
    }

    // -----------------------------------------------------------------------
    // The commands
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE COMMANDS IN THE DEMONSTRATION STORE, with their documented exit codes and no secret: verify-model 0 over a matching
    /// fixture installation and 2 over a changed one, reading files only; plan-only 0 naming the model for every part with the
    /// hashes verified; metered 2 naming the owner's decision; register-recording 0 for a set and 2 for a refused one; fake
    /// mode refused on a store designated company, where the own mode is admitted.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task EveryCommandRunsInTheDemonstrationStoreWithItsDocumentedExitCode()
    {
        await PrepareAsync(StoreDesignation.Demonstration);
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        await using var provider = CompositionRoot.Build(new HostOptions { ConnectionString = PostgresIntegrationTests.ConnectionString!, ProviderEndpoints = [] });
        var database = new NpgsqlConnectionStringBuilder(PostgresIntegrationTests.ConnectionString).Database!;
        var settings = SettingsFile(installation, database);

        async Task<(int Exit, string Output, string Errors)> Run(Func<TextWriter, TextWriter, Task<int>> command)
        {
            var output = new StringWriter();
            var errors = new StringWriter();
            var exit = await command(output, errors);
            return (exit, output.ToString(), errors.ToString());
        }

        var verified = await Run((o, e) => ProductionCommands.VerifyModelAsync(["verify-model", "--settings", settings], o, e, CancellationToken.None));
        Assert.True(verified.Exit == 0, verified.Errors);
        Assert.Contains("Verified:", verified.Output, StringComparison.Ordinal);
        Assert.Contains("no process was started", verified.Output, StringComparison.Ordinal);

        var plan = await Run((o, e) => ProductionCommands.ProduceAsync(provider, ["produce", "--mode", "plan-only", "--settings", settings], o, e, CancellationToken.None));
        Assert.True(plan.Exit == 0, plan.Output + plan.Errors);
        Assert.Contains("source: InHouseModel, for every part", plan.Output, StringComparison.Ordinal);
        Assert.Contains("equals the configured expected value", plan.Output, StringComparison.Ordinal);
        Assert.Contains("verification read files only and started no process", plan.Output, StringComparison.Ordinal);

        var metered = await Run((o, e) => ProductionCommands.ProduceAsync(provider, ["produce", "--mode", "metered", "--settings", settings], o, e, CancellationToken.None));
        Assert.Equal(2, metered.Exit);
        Assert.Contains("2026-10-10", metered.Errors, StringComparison.Ordinal);

        var registered = await Run((o, e) => ProductionCommands.RegisterRecordingAsync(provider,
            ["register-recording", "--settings", settings, "--recordings", Beats(0.6), "--performer", "A Fixture Performer", "--release-date", "2026-10-10"], o, e, CancellationToken.None));
        Assert.True(registered.Exit == 0, registered.Output + registered.Errors);
        Assert.Contains("release document reference ABSENT", registered.Output, StringComparison.Ordinal);
        var bad = Beats(0.6);
        File.Delete(Path.Combine(bad, "13.wav"));
        var refused = await Run((o, e) => ProductionCommands.RegisterRecordingAsync(provider, ["register-recording", "--settings", settings, "--recordings", bad], o, e, CancellationToken.None));
        Assert.Equal(2, refused.Exit);
        Assert.Contains("beat 13 has no file", refused.Output, StringComparison.Ordinal);

        await File.AppendAllTextAsync(installation.ModelCard, "changed");
        var changed = await Run((o, e) => ProductionCommands.VerifyModelAsync(["verify-model", "--settings", settings], o, e, CancellationToken.None));
        Assert.Equal(2, changed.Exit);
        Assert.Contains("the model card changed", changed.Errors, StringComparison.Ordinal);

        Assert.DoesNotContain("secret", File.ReadAllText(settings), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>In a store designated COMPANY, fake mode is refused by the guard and the rule, and the own mode is admitted under the matching identity.</summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task InAStoreDesignatedCompanyTheFakeIsRefusedAndTheOwnModeIsAdmitted()
    {
        await PrepareAsync(StoreDesignation.Company);
        using var installation = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(60));
        await using var provider = CompositionRoot.Build(new HostOptions { ConnectionString = PostgresIntegrationTests.ConnectionString!, ProviderEndpoints = [] });
        var database = new NpgsqlConnectionStringBuilder(PostgresIntegrationTests.ConnectionString).Database!;
        var settings = SettingsFile(installation, database);

        var errors = new StringWriter();
        Assert.Equal(2, await ProductionCommands.ProduceAsync(provider, ["produce", "--mode", "fake", "--settings", settings], new StringWriter(), errors, CancellationToken.None));
        Assert.Contains("fake mode runs only against a demonstration store", errors.ToString(), StringComparison.Ordinal);

        var output = new StringWriter();
        var exit = await ProductionCommands.ProduceAsync(provider, ["produce", "--mode", "plan-only", "--settings", settings], output, new StringWriter(), CancellationToken.None);
        Assert.True(exit == 0, output.ToString());
        Assert.Contains("store designated Company", output.ToString(), StringComparison.Ordinal);
        Assert.True(StoreGuard.Judge(ProductionMode.Own, StoreDesignation.Company, database, database).Admitted);
        Assert.Null(NarrationSourceRule.Select(ProductionMode.Fake, StoreDesignation.Company, RegistrationReading.None, new ModelReading(ModelState.Verified, [])).Source);
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

    private async Task<PreparationRecord> PrepareAsync(StoreDesignation designation)
    {
        var record = Preparation();
        var outcome = await new NpgsqlPreparationWriter(Source).PrepareAsync(record, designation, CancellationToken.None);
        Assert.False(outcome.Refused, string.Join("; ", outcome.Refusals));
        return record;
    }

    private static Task<LoadedItem> LoadAsync() => ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);

    private ProductionSettings Settings() => MediaToolOnPath.Settings(Output, Repository());

    /// <summary>A stored file's full path, as the one writer resolves it (long names, links resolved).</summary>
    private string ArtifactWriterPath(string relative) => new ArtifactWriter(Output, Repository()).FullPath(relative);

    private static Func<int, double> Seconds(double scale) => beat => Math.Round(scale * (1.0 + (beat * 0.1)), 2);

    /// <summary>
    /// Thirteen tone recordings, one per beat, in a new folder: RENDERED BY THE INSTALLED MEDIA TOOL where a test narrates from
    /// them, and written by the test itself (the same tones, faster) where a test only needs a set to refuse or to register.
    /// </summary>
    private string Beats(double scale, bool rendered = false)
    {
        var folder = Path.Combine(_root, "recordings-" + Guid.NewGuid().ToString("N")[..8]);
        if (rendered)
        {
            AudioFixtures.RenderBeats(MediaToolOnPath.Renderer!, folder, Enumerable.Range(1, 13).ToArray(), Seconds(scale));
            return folder;
        }

        Directory.CreateDirectory(folder);
        foreach (var beat in Enumerable.Range(1, 13))
        {
            File.WriteAllBytes(Path.Combine(folder, $"{beat:00}.wav"), AudioFixtures.Wave(22_050, 1, 16, Seconds(scale)(beat), 200 + (beat * 20), 0.125));
        }

        return folder;
    }

    private RecordingRegistrar Registrar(TextWriter output) =>
        new(new NpgsqlUnitOfWork(Source, new Clock()), new NpgsqlProductionReader(Source), new ExternalMediaTool(Settings()), new ArtifactWriter(Output, Repository()), Settings(), output);

    private async Task<RecordingRegistration> RegisterCompleteAsync(PreparationRecord record, LoadedItem loaded, double scale, bool rendered = false)
    {
        Directory.CreateDirectory(_root);
        var release = Path.Combine(_root, $"release-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(release, "a fixture release document, signed by a fixture performer");
        var output = new StringWriter();
        var result = await Registrar(output).RegisterAsync(new RegistrationRequest
        {
            Item = record.Item, Loaded = loaded, RecordingsFolder = Beats(scale, rendered), PerformerName = "A Fixture Performer", ReleaseDocumentReference = "fixture release",
            ReleaseDocumentDate = new DateOnly(2026, 10, 10), ReleaseDocumentPath = release, TrainingTerm = "model training not permitted",
        }, CancellationToken.None);
        Assert.True(result.ExitCode == RegistrationResult.Registered, output.ToString());
        return result.Registration!;
    }

    private ProduceItemService Service(TextWriter output, IMediaTool tool) =>
        new(new NpgsqlUnitOfWork(Source, new Clock()), new NpgsqlProductionReader(Source), tool, new ArtifactWriter(Output, Repository()), Settings(), output);

    private static ProduceRequest Request(PreparationRecord record, LoadedItem loaded, ProductionMode mode) => new()
    {
        Item = record.Item,
        Mode = mode,
        Loaded = loaded,
        Department = record.Department,
        Agent = record.Agent,
        Designation = StoreDesignation.Demonstration,
    };

    /// <summary>A settings file outside the repository, as the orchestrator writes one: the fixture installation's paths and the hashes the test computed.</summary>
    private string SettingsFile(FixtureInstallation installation, string database)
    {
        var model = installation.Settings();
        JsonObject File(ExpectedFile file) => new() { ["path"] = file.Path, ["sha256"] = file.Sha256 };
        var json = new JsonObject
        {
            ["outputRoot"] = Output,
            ["repositoryRoot"] = Repository(),
            ["rendererPath"] = MediaToolOnPath.Renderer,
            ["probePath"] = MediaToolOnPath.Probe,
            ["fontFile"] = MediaToolOnPath.Font,
            ["width"] = 1920,
            ["height"] = 1080,
            ["framesPerSecond"] = 30,
            ["renderBoundMinutes"] = 60,
            ["decodeBoundMinutes"] = 60,
            ["probeBoundSeconds"] = 60,
            ["stillBoundSeconds"] = 60,
            ["companyStoreIdentity"] = database,
            ["materialPath"] = "wave-2/item-001/item-material.json",
            ["preparationPath"] = "config/preparation-item-001.json",
            ["inHouseModel"] = new JsonObject
            {
                ["interpreter"] = File(model.Interpreter),
                ["environmentConfiguration"] = File(model.EnvironmentConfiguration),
                ["baseInterpreterSha256"] = model.BaseInterpreterSha256,
                ["runtimeModule"] = model.RuntimeModule,
                ["model"] = File(model.Model),
                ["configuration"] = File(model.Configuration),
                ["modelCard"] = File(model.ModelCard),
                ["runtimeRecord"] = File(model.RuntimeRecord),
                ["dependencyRecords"] = new JsonArray(model.DependencyRecords.Select(d => (JsonNode)File(d)).ToArray()),
                ["generation"] = new JsonObject
                {
                    ["lengthScale"] = 0.1m, ["noiseScale"] = 0.667m, ["noiseWidthScale"] = 0.8m, ["sentenceSilenceSeconds"] = 0m, ["volume"] = 1m, ["normalize"] = true,
                },
                ["partBoundSeconds"] = 60,
            },
        };
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, $"settings-{Guid.NewGuid():N}.json");
        System.IO.File.WriteAllText(path, json.ToJsonString());
        return path;
    }

    /// <summary>Every delivered table's rows, hashed over the columns it held before (so an added column cannot hide a changed value).</summary>
    private async Task<Dictionary<string, string>> DeliveredRowsAsync(Dictionary<string, string>? columnsFrom = null)
    {
        var tables = new Dictionary<string, string>();
        await using var list = Source.CreateCommand(
            "SELECT table_name, string_agg(quote_ident(column_name), ', ' ORDER BY ordinal_position) FROM information_schema.columns "
            + "WHERE table_schema = current_schema() AND table_name IN (SELECT table_name FROM information_schema.tables WHERE table_schema = current_schema() AND table_type = 'BASE TABLE') "
            + "GROUP BY table_name ORDER BY table_name");
        var columns = new List<(string Table, string Columns)>();
        await using (var reader = await list.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                columns.Add((reader.GetString(0), reader.GetString(1)));
            }
        }

        foreach (var (table, all) in columns)
        {
            var key = columnsFrom?.Keys.FirstOrDefault(k => k.StartsWith(table + "(", StringComparison.Ordinal));
            if (columnsFrom is not null && key is null)
            {
                continue;
            }

            var used = key is null ? all : key[(table.Length + 1)..^1];
            await using var hash = Source.CreateCommand($"SELECT count(*)::text || ':' || COALESCE(md5(string_agg(r::text, '|' ORDER BY r::text)), '') FROM (SELECT {used} FROM {table}) r");
            tables[$"{table}({used})"] = (string)(await hash.ExecuteScalarAsync())!;
        }

        return tables;
    }

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

    /// <summary>The process clock, optionally set wrong by a fixed offset; the datastore's clock is the only one any record reads.</summary>
    private sealed class Clock(TimeSpan skew = default) : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow + skew;
    }

    /// <summary>A process starter that starts nothing and fails the test if a plan ever asks it to.</summary>
    private sealed class NothingStarts : IMediaTool
    {
        private static InvalidOperationException Refused() => new("a plan started a process");

        public Task<string> VersionLineAsync(CancellationToken cancellationToken) => throw Refused();

        public Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken) => throw Refused();

        public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken) => throw Refused();

        public Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken) => throw Refused();

        public Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken) => throw Refused();

        public Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken) => throw Refused();

        public Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken) => throw Refused();
    }

    /// <summary>Counts the CLIENT transactions open in this database from a connection of its own, naming any it sees.</summary>
    private sealed class TransactionObserver(NpgsqlDataSource source)
    {
        public int MostOpen { get; private set; }

        public int Observations { get; private set; }

        public List<string> Seen { get; } = [];

        public async Task ObserveAsync()
        {
            await using var command = source.CreateCommand(
                "SELECT count(*), string_agg(state || ':' || left(query, 120), ' || ') FROM pg_stat_activity WHERE datname = current_database() AND pid <> pg_backend_pid() "
                + "AND xact_start IS NOT NULL AND backend_type = 'client backend'");
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            var open = (int)reader.GetInt64(0);
            if (open > 0)
            {
                Seen.Add(reader.IsDBNull(1) ? "?" : reader.GetString(1));
            }

            MostOpen = Math.Max(MostOpen, open);
            Observations++;
        }
    }

    /// <summary>The media tool, observed before each invocation.</summary>
    private sealed class ObservedTool(IMediaTool inner, TransactionObserver observer) : IMediaTool
    {
        public async Task<string> VersionLineAsync(CancellationToken cancellationToken) { await observer.ObserveAsync(); return await inner.VersionLineAsync(cancellationToken); }

        public async Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken) { await observer.ObserveAsync(); await inner.DrawStillAsync(still, cancellationToken); }

        public async Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            await inner.ConcatenateAudioAsync(stagingFolder, listFile, outputFile, sampleFormat, cancellationToken);
        }

        public async Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken) { await observer.ObserveAsync(); await inner.RenderAsync(render, cancellationToken); }

        public async Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken) { await observer.ObserveAsync(); return await inner.ProbeAsync(file, cancellationToken); }

        public async Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken) { await observer.ObserveAsync(); return await inner.DecodeAsync(file, cancellationToken); }

        public async Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken) { await observer.ObserveAsync(); return await inner.MeasureAudioAsync(file, cancellationToken); }
    }

    /// <summary>The model, observed: before the generation starts and, while it is still running, every half second up to the count named.</summary>
    private sealed class ObservedVoice(IVoiceModel inner, TransactionObserver observer, int whileRunning = 1) : IVoiceModel
    {
        public async Task<IReadOnlyList<string>> GenerateAsync(VoiceGeneration generation, CancellationToken cancellationToken)
        {
            await observer.ObserveAsync();
            var running = inner.GenerateAsync(generation, cancellationToken);
            for (var i = 0; i < whileRunning && !running.IsCompleted; i++)
            {
                await Task.Delay(500, CancellationToken.None);
                await observer.ObserveAsync();
            }

            return await running;
        }
    }

    /// <summary>The media tool that swaps a file when the run draws its first still, after the plan verified the installation.</summary>
    private sealed class SwapOnFirstStill(IMediaTool inner, Action swap) : IMediaTool
    {
        private bool _swapped;

        public Task<string> VersionLineAsync(CancellationToken cancellationToken) => inner.VersionLineAsync(cancellationToken);

        public Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken)
        {
            if (!_swapped)
            {
                _swapped = true;
                swap();
            }

            return inner.DrawStillAsync(still, cancellationToken);
        }

        public Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken) =>
            inner.ConcatenateAudioAsync(stagingFolder, listFile, outputFile, sampleFormat, cancellationToken);

        public Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken) => inner.RenderAsync(render, cancellationToken);

        public Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken) => inner.ProbeAsync(file, cancellationToken);

        public Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken) => inner.DecodeAsync(file, cancellationToken);

        public Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken) => inner.MeasureAudioAsync(file, cancellationToken);
    }

    /// <summary>The writer that swaps a file just before it stages a named text: between two parts, when nothing is held.</summary>
    private sealed class SwapBeforeText(IArtifactStore inner, string name, Action swap) : IArtifactStore
    {
        public string OutputRoot => inner.OutputRoot;

        public string BeginRun(string runFolder) => inner.BeginRun(runFolder);

        public Task WriteStagingTextAsync(string stagingFolder, string staged, string text, CancellationToken cancellationToken)
        {
            if (staged == name)
            {
                swap();
            }

            return inner.WriteStagingTextAsync(stagingFolder, staged, text, cancellationToken);
        }

        public Task CopyIntoStagingAsync(string stagingFolder, string sourceFile, string staged, CancellationToken cancellationToken) =>
            inner.CopyIntoStagingAsync(stagingFolder, sourceFile, staged, cancellationToken);

        public Task<StoredFile> StoreAsync(string stagingFolder, ReadOnlyMemory<byte> bytes, string relativePath, CancellationToken cancellationToken) =>
            inner.StoreAsync(stagingFolder, bytes, relativePath, cancellationToken);

        public Task<StoredFile> PromoteAsync(string stagingFolder, string stagingName, string relativePath, CancellationToken cancellationToken) =>
            inner.PromoteAsync(stagingFolder, stagingName, relativePath, cancellationToken);

        public string FullPath(string relativePath) => inner.FullPath(relativePath);
    }

    /// <summary>A reader whose FIRST registration read (the plan's) sees none, and every later read the store's: a registration recorded between plan and run.</summary>
    private sealed class RegistrationAppearsAfterThePlan(IProductionReader inner) : IProductionReader
    {
        private int _reads;

        public Task<RecordedPackage?> PackageAsync(ItemId item, ItemVersion packageVersion, CancellationToken cancellationToken) => inner.PackageAsync(item, packageVersion, cancellationToken);

        public Task<PlanPricing> PricingAsync(CapabilityClass capability, CancellationToken cancellationToken) => inner.PricingAsync(capability, cancellationToken);

        public Task<IReadOnlyList<ProducedStageOutcome>> StagesAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken) => inner.StagesAsync(item, version, cancellationToken);

        public Task<IReadOnlyList<ArtifactRecord>> ArtifactsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken) => inner.ArtifactsAsync(item, version, cancellationToken);

        public Task<RecordingRegistration?> LatestRegistrationAsync(ItemId item, ItemVersion packageVersion, CancellationToken cancellationToken) =>
            Interlocked.Increment(ref _reads) == 1 ? Task.FromResult<RecordingRegistration?>(null) : inner.LatestRegistrationAsync(item, packageVersion, cancellationToken);
    }
}

/// <summary>The counted total a cap reading states, for comparing before and after a run.</summary>
internal static class CapReadingExtensions
{
    public static string Counted(this ItemCapReading reading) => reading.Describe();
}
