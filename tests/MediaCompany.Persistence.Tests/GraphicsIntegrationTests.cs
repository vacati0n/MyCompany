using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using MediaCompany.Application.Ports;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Work;
using MediaCompany.Graphics.Fixtures;
using MediaCompany.OwnVoice.Fixtures;
using MediaCompany.Production;
using Npgsql;
using Xunit;

namespace MediaCompany.Persistence.Tests;

/// <summary>
/// GRAPHICS DRAWN BY CODE against the record store (the eleventh schema resource and the change that comes with it).
///
/// EVERY DEMONSTRATION RUNS AGAINST A THROWAWAY STORE THAT IT DROPS AND RECREATES, in the separate demonstration database only.
/// The renderer is the in-test graphics stand-in over a FIXTURE installation whose manifests and expected hashes the test makes
/// itself; the narration is the in-house model's in-test stand-in at a short length, so a production is quick. No test runs the
/// installed renderer or the real model, reaches a network, books a cost or connects to the company store.
/// </summary>
[Collection(DatastoreCollection.Name)]
public sealed class GraphicsIntegrationTests : IAsyncLifetime
{
    private NpgsqlDataSource? _dataSource;
    private readonly string _root = Path.Combine(Path.GetTempPath(), "mediacompany-graphics-" + Guid.NewGuid().ToString("N"));

    private NpgsqlDataSource Source => _dataSource ?? throw new InvalidOperationException("No datastore; this test should have been skipped.");

    private string Output => Path.Combine(_root, "output");

    public async Task InitializeAsync()
    {
        if (PostgresIntegrationTests.ConnectionString is null)
        {
            return;
        }

        _dataSource = NpgsqlDataSource.Create(PostgresIntegrationTests.ConnectionString);
        await ThrowawayStore.DropAsync(_dataSource);
        await SchemaInstaller.InstallAsync(_dataSource, CancellationToken.None);
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
        catch (UnauthorizedAccessException)
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
    // The eleventh schema resource
    // -----------------------------------------------------------------------

    /// <summary>
    /// The eleventh resource installs last and records nothing; it declares no binary column; over a store holding the ten
    /// resources AND ROWS OF THE COMPANY STORE'S SHAPE (designated company, a production version with a delivered artifact) it
    /// applies alone, changing no delivered row, and re-applies changing nothing; afterwards the four new roles are admitted
    /// and an unknown one is still refused.
    /// </summary>
    [RequiresPostgresFact]
    public async Task TheEleventhResourceAppliesAloneOverACompanyShapedStoreChangingNoRowAndReappliesCleanly()
    {
        Assert.Equal("MediaCompany.Persistence.Schema.011-graphics.sql", SchemaInstaller.ResourceNames[^1]);
        Assert.Equal(11, SchemaInstaller.ResourceNames.Count);
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM graphic_provenance"));
        Assert.Equal(0L, await ScalarAsync<long>(
            "SELECT count(*) FROM information_schema.columns WHERE table_schema = current_schema() AND table_name = 'graphic_provenance' AND data_type IN ('bytea', 'oid')"));

        // A store of the company store's shape holding the ten resources only, with a production version and a delivered artifact.
        await ThrowawayStore.DropAsync(Source);
        foreach (var resource in SchemaInstaller.ResourceNames.Take(10))
        {
            await ExecuteAsync(SchemaInstaller.ReadResource(resource));
        }

        await SchemaInstaller.SeedClosedActionSetAsync(Source, CancellationToken.None);
        var record = await PrepareAsync(StoreDesignation.Company);
        var unitOfWork = new NpgsqlUnitOfWork(Source, new Clock());
        await using (var transaction = await unitOfWork.BeginAsync(CancellationToken.None))
        {
            var opened = await transaction.Production.OpenProductionVersionAsync(record.Item, ProductionMode.Own, NarrationSource.InHouseModel, CancellationToken.None);
            await transaction.Production.RecordArtifactAsync(new ArtifactRecord
            {
                Item = record.Item, Version = opened.Version, Stage = ProductionStage.Design, Role = ArtifactRole.GraphicStill, Implements = "a delivered still",
                RelativePath = "item/v2/design/still.png", Length = 1, Sha256 = new string('a', 64),
            }, CancellationToken.None);
            await transaction.CommitAsync(CancellationToken.None);
        }

        var before = await RowsAsync();
        await SchemaInstaller.InstallFromAsync(Source, 11, CancellationToken.None);
        Assert.Equal(before, await RowsAsync());
        await SchemaInstaller.InstallFromAsync(Source, 11, CancellationToken.None);
        Assert.Equal(before, await RowsAsync());
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM graphic_provenance"));

        foreach (var role in new[] { "GraphicMotion", "ContactSheet", "ContactSheetFrame", "GraphicPreview" })
        {
            await ExecuteAsync(
                $"INSERT INTO production_artifacts (item_id, item_version, relative_path, stage, role, implements, length_bytes, sha256, recorded_at) "
                + $"SELECT item_id, item_version, 'item/v2/{role}', 'Design', '{role}', 'a new role', 1, '{new string('b', 64)}', clock_timestamp() FROM production_versions");
        }

        var refused = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(
            $"INSERT INTO production_artifacts (item_id, item_version, relative_path, stage, role, implements, length_bytes, sha256, recorded_at) "
            + $"SELECT item_id, item_version, 'item/v2/x', 'Design', 'TextCard', 'an unknown role', 1, '{new string('b', 64)}', clock_timestamp() FROM production_versions"));
        Assert.Contains("production_artifacts_role_check", refused.ConstraintName ?? refused.MessageText, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // A production with graphics drawn by the stand-in
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE 22 GRAPHICS DRAWN AFTER AUDIO, in a demonstration store: ten stills and twelve motion graphics, each recorded with
    /// complete provenance read back from the store; every recorded hash the stored file's; every still and frame twice rendered
    /// and the repeat recorded with its digest; each motion graphic's frames exactly its segment, moving inside it and holding
    /// after the motion window; the identities read from the fixture's verified bytes; no transaction open while any render ran.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task TheTwentyTwoGraphicsAreDrawnAfterAudioWithCompleteProvenance()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        var observer = new TransactionObserver(Source);
        var (produced, output, settings) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared, observer: observer);
        Assert.True(produced.ExitCode == ProduceResult.Succeeded, output);

        var reader = new NpgsqlProductionReader(Source);
        var version = produced.Version!.Value;
        var artifacts = await reader.ArtifactsAsync(record.Item, version, CancellationToken.None);
        var provenance = await reader.GraphicProvenanceAsync(record.Item, version, CancellationToken.None);
        Assert.Equal(10, artifacts.Count(a => a.Role == ArtifactRole.GraphicStill));
        Assert.Equal(12, artifacts.Count(a => a.Role == ArtifactRole.GraphicMotion));
        Assert.Equal(22, provenance.Count);
        var store = new ArtifactWriter(Output, Repository());
        foreach (var artifact in artifacts)
        {
            Assert.Equal(artifact.Sha256, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(store.FullPath(artifact.RelativePath)))));
        }

        var stages = await reader.StagesAsync(record.Item, version, CancellationToken.None);
        Assert.True(stages.Single(s => s.Stage == ProductionStage.Audio).RecordedAt < stages.Single(s => s.Stage == ProductionStage.Design).RecordedAt);
        var style = Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Repository(), "config", "graphics-style.json"))));
        foreach (var row in provenance)
        {
            var artifact = Assert.Single(artifacts, a => a.RelativePath == row.RelativePath);
            Assert.Equal(row.Position, artifact.Implements);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Repository(), row.SpecificationPath)))), row.SpecificationSha256);
            Assert.Equal(style, row.StyleSha256);
            Assert.Equal("remotion", row.RendererName);
            Assert.Equal("0.0.0-fixture", row.RendererVersion);
            Assert.Equal("1.2.3.4", row.BrowserVersion);
            Assert.Equal(GraphicsFixture.Sha256(GraphicsFixture.Shared.NodeExecutable), row.NodeSha256);
            Assert.Contains($"sha256 {GraphicsFixture.Sha256(GraphicsFixture.Shared.RegularFace)}", row.Faces, StringComparison.Ordinal);
            Assert.Contains(GraphicsFixture.Shared.RendererManifest.Sha256, row.Manifests, StringComparison.Ordinal);
            Assert.Equal(1, row.Concurrency);
            Assert.StartsWith($"{GraphicsFixture.Shared.NodeExecutable} render.mjs render render-", row.Arguments, StringComparison.Ordinal);
            Assert.Equal(SamenessKind.RepeatsTwoRenders, row.Sameness);
            Assert.StartsWith("repeats: rendered twice in separate processes", row.SamenessStatement, StringComparison.Ordinal);
            Assert.Equal(0, row.LeftoverProcesses);
            Assert.True(row.RenderSeconds > 0);
            if (row.Kind == GraphicKind.Still)
            {
                Assert.Equal(1, row.FramesRendered);
                Assert.Equal(artifact.Sha256, row.FinalFrameSha256);
                Assert.Null(row.FirstChangingFrame);
                continue;
            }

            // A motion graphic: exactly its segment's frames, changing inside its motion window, then holding its final state.
            Assert.Equal(row.SegmentFrames, row.FramesRendered);
            var window = (long)Math.Floor(row.SegmentFrames * 0.8m);
            Assert.InRange(row.FirstChangingFrame!.Value, 1, window - 1);
            Assert.InRange(row.LastChangingFrame!.Value, 1, window - 1);
            var lines = (await File.ReadAllTextAsync(store.FullPath(artifact.RelativePath))).Split('\n', StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(row.SegmentFrames, lines.Length);
            Assert.Equal(row.FinalFrameSha256, lines[^1].Split(' ')[3]);
        }

        Assert.Contains("graphics: 22 drawn by code with the approved renderer; no model call, no capability call; booked metered spend USD 0.00; local compute unmeasured", output, StringComparison.Ordinal);
        Assert.Contains("graphics cost: no metered spend is planned, USD 0.00; no model call; local compute unmeasured", output, StringComparison.Ordinal);
        Assert.True(observer.Observations >= 44, observer.Observations.ToString(System.Globalization.CultureInfo.InvariantCulture));
        Assert.True(observer.MostOpen == 0, string.Join(" ## ", observer.Seen));
        Assert.Equal(0L, await ScalarAsync<long>("SELECT count(*) FROM agent_costs"));
        _ = settings;
    }

    /// <summary>
    /// MOTION IS TIMED FROM THE NARRATION ACTUALLY USED: two narrations of different measured durations give each motion graphic
    /// a different frame count, each inside its own segment, while every still is byte-identical between them.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task TwoNarrationsOfDifferentDurationsTimeTheMotionDifferentlyAndLeaveEveryStillIdentical()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        var reader = new NpgsqlProductionReader(Source);
        var runs = new List<IReadOnlyList<GraphicProvenance>>();
        foreach (var scale in new[] { 0.1m, 0.15m })
        {
            var (produced, output, _) = await ProduceAsync(record, voice, scale, GraphicsFixture.Shared);
            Assert.True(produced.ExitCode == ProduceResult.Succeeded, output);
            runs.Add(await reader.GraphicProvenanceAsync(record.Item, produced.Version!.Value, CancellationToken.None));
        }

        foreach (var (shorter, longer) in runs[0].Zip(runs[1]))
        {
            Assert.Equal(shorter.Position, longer.Position);
            if (shorter.Kind == GraphicKind.Still)
            {
                Assert.Equal(shorter.FinalFrameSha256, longer.FinalFrameSha256);
                continue;
            }

            Assert.True(longer.SegmentFrames > shorter.SegmentFrames, $"{shorter.Position}: {shorter.SegmentFrames} and {longer.SegmentFrames} frames");
            Assert.NotEqual(shorter.LastChangingFrame, longer.LastChangingFrame);
            Assert.InRange(shorter.LastChangingFrame!.Value, 1, shorter.SegmentFrames - 1);
            Assert.InRange(longer.LastChangingFrame!.Value, 1, longer.SegmentFrames - 1);
        }
    }

    /// <summary>
    /// NOTHING BUT THE MEASURED NARRATION TIMES A GRAPHIC: the script's delivery rate and the specified runtime, changed in the
    /// loaded item, leave every graphic's output byte-identical.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task TheScriptsRateAndTheSpecifiedRuntimeLeaveEveryGraphicIdentical()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        var reader = new NpgsqlProductionReader(Source);
        var loaded = await LoadAsync();
        var altered = loaded with
        {
            Rate = new ScriptRate(99, "a fixture source", "99 words a minute, set by the test", "a fixture duration"),
            Material = loaded.Material with { SpecifiedRuntime = loaded.Material.SpecifiedRuntime with { Seconds = 1 } },
        };

        var hashes = new List<string[]>();
        foreach (var item in new[] { loaded, altered })
        {
            var (produced, output, _) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared, item);
            Assert.True(produced.ExitCode == ProduceResult.Succeeded, output);
            var artifacts = await reader.ArtifactsAsync(record.Item, produced.Version!.Value, CancellationToken.None);
            hashes.Add(artifacts.Where(a => a.Role is ArtifactRole.GraphicStill or ArtifactRole.GraphicMotion).OrderBy(a => a.Implements, StringComparer.Ordinal).Select(a => a.Sha256).ToArray());
        }

        Assert.Equal(22, hashes[0].Length);
        Assert.Equal(hashes[0], hashes[1]);
    }

    /// <summary>A renderer whose two renders differ is recorded not observed to repeat, with both digests and the first differing frame.</summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task ARendererThatDoesNotRepeatIsRecordedNotObservedToRepeatWithBothDigests()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        using var graphics = new GraphicsFixture("vary");
        var (produced, output, _) = await ProduceAsync(record, voice, 0.1m, graphics);
        Assert.True(produced.ExitCode == ProduceResult.Succeeded, output);

        var provenance = await new NpgsqlProductionReader(Source).GraphicProvenanceAsync(record.Item, produced.Version!.Value, CancellationToken.None);
        Assert.All(provenance, p =>
        {
            Assert.Equal(SamenessKind.DifferedTwoRenders, p.Sameness);
            Assert.Matches("^not observed to repeat: rendered twice in separate processes with identical inputs gave frame-set sha256 [0-9a-f]{64} and [0-9a-f]{64}; the first differing frame is \\d+$", p.SamenessStatement);
        });
    }

    /// <summary>
    /// THE BYTES ASSEMBLED ARE THE BYTES HASHED: a motion graphic's frame changed after Design recorded it, before the assembly
    /// read it, fails the Production stage naming the frame and both hashes; nothing is encoded from it.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AFrameChangedAfterItWasRecordedFailsTheAssemblyNamingBothHashes()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        string? changed = null;
        var (produced, output, _) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared, beforeThumbnail: run =>
        {
            changed ??= Directory.GetFiles(run, "frame-000003.png", SearchOption.AllDirectories).Order(StringComparer.Ordinal).First();
            File.AppendAllText(changed, "changed after it was recorded");
        });

        Assert.Equal(ProduceResult.StageFailed, produced.ExitCode);
        var production = (await new NpgsqlProductionReader(Source).StagesAsync(record.Item, produced.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Production);
        Assert.Equal(StageOutcome.Failed, production.Outcome);
        Assert.Contains("changed after it was recorded: sha256 ", production.Summary, StringComparison.Ordinal);
        Assert.Contains("frame-000003.png", production.Summary, StringComparison.Ordinal);
        Assert.DoesNotContain(await new NpgsqlProductionReader(Source).ArtifactsAsync(record.Item, produced.Version!.Value, CancellationToken.None), a => a.Role == ArtifactRole.RenderedVideo);
        _ = output;
    }

    /// <summary>
    /// A FILE A RENDER LEFT UNDER THE INSTALLED FOLDER (as a bundler cache would) fails the Design stage by name at the
    /// verification after the last render; a bundle holding an excluded speech client's module is refused by name.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AFileLeftUnderTheInstalledFolderOrAnExcludedModuleFailsTheDesignStage()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        var reader = new NpgsqlProductionReader(Source);
        foreach (var (behaviour, expected) in new[]
                 {
                     ("cache", "the graphics installation changed while the graphics were rendered"),
                     ("excluded", "module(s) of an excluded speech client package"),
                 })
        {
            using var graphics = new GraphicsFixture(behaviour);
            var (produced, output, _) = await ProduceAsync(record, voice, 0.1m, graphics);
            Assert.True(produced.ExitCode == ProduceResult.StageFailed, output);
            var design = (await reader.StagesAsync(record.Item, produced.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Design);
            Assert.Equal(StageOutcome.Failed, design.Outcome);
            Assert.Contains(expected, design.Summary, StringComparison.Ordinal);
        }
    }

    /// <summary>An installed file changed after the manifests were written refuses the production before any call, naming both hashes.</summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task AnInstalledFileChangedRefusesTheProductionBeforeAnyCall()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        using var graphics = new GraphicsFixture("deterministic");
        var listed = GraphicsFixture.Sha256(graphics.BrowserExecutable);
        await File.AppendAllTextAsync(graphics.BrowserExecutable, "a changed byte");
        var (produced, output, _) = await ProduceAsync(record, voice, 0.1m, graphics);

        Assert.Equal(ProduceResult.RefusedBeforeAnyCall, produced.ExitCode);
        Assert.Null(produced.Version);
        Assert.Contains($"and {listed} listed by the browser manifest", output, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The company store: stills compared with the owner's approval, motion re-timed
    // -----------------------------------------------------------------------

    /// <summary>
    /// IN A STORE DESIGNATED COMPANY: without the owner's approval file the production is refused before any call; with an
    /// approval file holding the demonstration's still hashes every still is recorded equal to its approved bytes and every
    /// motion graphic re-timed and rendered once; an approval naming a still the render does not equal fails the Design stage
    /// naming both hashes.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task InTheCompanyStoreStillsEqualTheApprovedRendersAndMotionIsRetimed()
    {
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        var demonstration = await PrepareAsync(StoreDesignation.Demonstration);
        var (shown, shownOutput, _) = await ProduceAsync(demonstration, voice, 0.1m, GraphicsFixture.Shared);
        Assert.True(shown.ExitCode == ProduceResult.Succeeded, shownOutput);
        var approved = await new NpgsqlProductionReader(Source).GraphicProvenanceAsync(demonstration.Item, shown.Version!.Value, CancellationToken.None);

        // A new store numbers its versions from 2 again, so the demonstration's produced files are moved out of the way first.
        await ThrowawayStore.DropAsync(Source);
        await SchemaInstaller.InstallAsync(Source, CancellationToken.None);
        Directory.Delete(Output, recursive: true);
        var record = await PrepareAsync(StoreDesignation.Company);

        var (refused, refusedOutput, _) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared, designation: StoreDesignation.Company);
        Assert.Equal(ProduceResult.RefusedBeforeAnyCall, refused.ExitCode);
        Assert.Contains("no owner's approval file is configured", refusedOutput, StringComparison.Ordinal);

        var approval = ApprovalFile(approved, still => still);
        var (produced, output, _) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared, designation: StoreDesignation.Company, approval: approval);
        Assert.True(produced.ExitCode == ProduceResult.Succeeded, output);
        Assert.Contains("owner's approval: a fixture approver on 2026-10-11", output, StringComparison.Ordinal);
        var provenance = await new NpgsqlProductionReader(Source).GraphicProvenanceAsync(record.Item, produced.Version!.Value, CancellationToken.None);
        Assert.All(provenance.Where(p => p.Kind == GraphicKind.Still), p => Assert.Equal(SamenessKind.EqualsApprovedStill, p.Sameness));
        Assert.All(provenance.Where(p => p.Kind == GraphicKind.Motion), p =>
        {
            Assert.Equal(SamenessKind.RetimedRenderedOnce, p.Sameness);
            Assert.StartsWith("re-timed from this run's narration", p.SamenessStatement, StringComparison.Ordinal);
        });

        var wrong = ApprovalFile(approved, still => still[..^4] + "0000");
        var (failed, failedOutput, _) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared, designation: StoreDesignation.Company, approval: wrong);
        Assert.True(failed.ExitCode == ProduceResult.StageFailed, failedOutput);
        var design = (await new NpgsqlProductionReader(Source).StagesAsync(record.Item, failed.Version!.Value, CancellationToken.None)).Single(s => s.Stage == ProductionStage.Design);
        Assert.Contains("a still must equal its approved render", design.Summary, StringComparison.Ordinal);
    }

    // -----------------------------------------------------------------------
    // The owner's contact sheet and preview
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE REVIEW, in a demonstration store: a contact sheet and a video-only preview recorded with their hashes; 22 final-frame
    /// copies beside the sheet, each byte-identical to its graphic's recorded final frame; the preview one video stream, decoded
    /// with zero errors, as long as the graphics' production spans; a second review of the version refused; and in a store not
    /// designated demonstration the review refused before any process.
    /// </summary>
    [RequiresPostgresAndMediaToolFact]
    public async Task TheReviewBuildsTheSheetAndThePreviewFromTheProductionsOwnFilesInADemonstrationStoreOnly()
    {
        var record = await PrepareAsync(StoreDesignation.Demonstration);
        using var voice = new FixtureInstallation("deterministic", TimeSpan.FromSeconds(120));
        var (produced, output, settings) = await ProduceAsync(record, voice, 0.1m, GraphicsFixture.Shared);
        Assert.True(produced.ExitCode == ProduceResult.Succeeded, output);
        var version = produced.Version!.Value;
        var loaded = await LoadAsync();
        var reader = new NpgsqlProductionReader(Source);
        var tool = new ExternalMediaTool(settings);
        var store = new ArtifactWriter(Output, Repository());
        var review = new ReviewGraphicsService(new NpgsqlUnitOfWork(Source, new Clock()), reader, tool, store, settings, new StringWriter());
        var printed = new StringWriter();
        var exit = await new ReviewGraphicsService(new NpgsqlUnitOfWork(Source, new Clock()), reader, tool, store, settings, printed)
            .RunAsync(new ReviewRequest(record.Item, version, loaded, StoreDesignation.Demonstration, "a demonstration store"), CancellationToken.None);
        Assert.True(exit == ReviewGraphicsService.Reviewed, printed.ToString());

        var artifacts = await reader.ArtifactsAsync(record.Item, version, CancellationToken.None);
        var provenance = await reader.GraphicProvenanceAsync(record.Item, version, CancellationToken.None);
        var sheet = Assert.Single(artifacts, a => a.Role == ArtifactRole.ContactSheet);
        var preview = Assert.Single(artifacts, a => a.Role == ArtifactRole.GraphicPreview);
        var copies = artifacts.Where(a => a.Role == ArtifactRole.ContactSheetFrame).ToArray();
        Assert.Equal(22, copies.Length);
        foreach (var copy in copies)
        {
            Assert.Equal(provenance.Single(p => p.Position == copy.Implements).FinalFrameSha256, copy.Sha256);
            Assert.Equal(copy.Sha256, Convert.ToHexStringLower(SHA256.HashData(await File.ReadAllBytesAsync(store.FullPath(copy.RelativePath)))));
        }

        Assert.Contains($"sha256 {sheet.Sha256}", printed.ToString(), StringComparison.Ordinal);
        Assert.Contains($"sha256 {preview.Sha256}", printed.ToString(), StringComparison.Ordinal);
        var probe = await tool.ProbeAsync(store.FullPath(preview.RelativePath), CancellationToken.None);
        Assert.Equal(new[] { "video" }, probe.StreamKinds);
        Assert.Equal(0, (await tool.DecodeAsync(store.FullPath(preview.RelativePath), CancellationToken.None)).Errors);
        var spans = provenance.Sum(p => p.SegmentFrames);
        Assert.True(Math.Abs(probe.Duration.TotalSeconds - (spans / 30.0)) <= 1.0 / 30, $"{probe.Duration} against {spans} frames");

        Assert.Equal(ReviewGraphicsService.Refused, await review.RunAsync(new ReviewRequest(record.Item, version, loaded, StoreDesignation.Demonstration, "a demonstration store"), CancellationToken.None));
        var company = new StringWriter();
        var standIns = System.Diagnostics.Process.GetProcesses().Length;
        Assert.Equal(ReviewGraphicsService.Refused, await new ReviewGraphicsService(new NpgsqlUnitOfWork(Source, new Clock()), reader, tool, store, settings, company)
            .RunAsync(new ReviewRequest(record.Item, version, loaded, StoreDesignation.Company, "mediacompany"), CancellationToken.None));
        Assert.Contains("review-graphics runs only against a store designated demonstration, and mediacompany is designated Company", company.ToString(), StringComparison.Ordinal);
        Assert.Single(artifacts, a => a.Role == ArtifactRole.ContactSheet);
        _ = standIns;
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private async Task<(ProduceResult Result, string Output, ProductionSettings Settings)> ProduceAsync(
        PreparationRecord record, FixtureInstallation voice, decimal lengthScale, GraphicsFixture graphics, LoadedItem? loaded = null,
        TransactionObserver? observer = null, Action<string>? beforeThumbnail = null, StoreDesignation designation = StoreDesignation.Demonstration, string? approval = null)
    {
        loaded ??= await LoadAsync();
        var model = voice.Settings() with { Generation = new VoiceGenerationSettings(lengthScale, 0.667m, 0.8m, 0m, 1m, true) };
        var graphicsSettings = graphics.Settings() with { ApprovalFile = approval is null ? null : new ExpectedFile(approval, GraphicsFixture.Sha256(approval)) };
        var settings = MediaToolOnPath.Settings(Output, Repository()) with { InHouseModel = model, Graphics = graphicsSettings };
        var inner = new ExternalMediaTool(settings);
        var tool = new HookedTool(inner, observer, beforeThumbnail is null ? null : () => beforeThumbnail(Output));
        var output = new StringWriter();
        var produced = await new ProduceItemService(new NpgsqlUnitOfWork(Source, new Clock()), new NpgsqlProductionReader(Source), tool, new ArtifactWriter(Output, Repository()), settings, output, inner)
            .RunAsync(new ProduceRequest
            {
                Item = record.Item, Mode = ProductionMode.Own, Loaded = loaded, Department = record.Department, Agent = record.Agent, Designation = designation,
            }, null, CancellationToken.None);
        return (produced, output.ToString(), settings);
    }

    /// <summary>The owner's approval file as the orchestrator writes one, from a demonstration production's provenance.</summary>
    private string ApprovalFile(IReadOnlyList<GraphicProvenance> approved, Func<string, string> still)
    {
        var json = new JsonObject
        {
            ["date"] = "2026-10-11",
            ["approver"] = "a fixture approver",
            ["decisionRecordEntry"] = "a fixture decision",
            ["demonstrationStore"] = "a demonstration store",
            ["demonstrationVersion"] = 2,
            ["contactSheetSha256"] = new string('c', 64),
            ["previewSha256"] = new string('d', 64),
            ["styleSha256"] = approved[0].StyleSha256,
            ["manifests"] = approved[0].Manifests,
            ["graphics"] = new JsonArray(approved.Select(p => (JsonNode)new JsonObject
            {
                ["position"] = p.Position,
                ["specificationSha256"] = p.SpecificationSha256,
                ["kind"] = p.Kind.ToString(),
                ["finalFrameSha256"] = p.FinalFrameSha256,
                ["stillSha256"] = p.Kind == GraphicKind.Still ? still(p.FinalFrameSha256) : null,
            }).ToArray()),
        };
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, $"approval-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json.ToJsonString());
        return path;
    }

    private static string Repository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "MediaCompany.slnx")))
        {
            directory = directory.Parent;
        }

        return directory!.FullName;
    }

    private static Task<LoadedItem> LoadAsync() => ItemPackageLoader.LoadAsync(Repository(), "wave-2/item-001/item-material.json", CancellationToken.None);

    private async Task<PreparationRecord> PrepareAsync(StoreDesignation designation)
    {
        var record = MediaCompany.Host.ProductionCommands.ReadPreparation(Path.Combine(Repository(), "config", "preparation-item-001.json"));
        var outcome = await new NpgsqlPreparationWriter(Source).PrepareAsync(record, designation, CancellationToken.None);
        Assert.False(outcome.Refused, string.Join("; ", outcome.Refusals));
        return record;
    }

    private async Task<string> RowsAsync()
    {
        var parts = new List<string>();
        foreach (var table in new[] { "production_versions", "production_artifacts", "items", "item_dossiers", "store_designation", "item_caps" })
        {
            await using var hash = Source.CreateCommand($"SELECT count(*)::text || ':' || COALESCE(md5(string_agg(r::text, '|' ORDER BY r::text)), '') FROM {table} r");
            parts.Add($"{table} {(string)(await hash.ExecuteScalarAsync())!}");
        }

        return string.Join("; ", parts);
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

    private sealed class Clock : IClock
    {
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
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

    /// <summary>
    /// The media tool and the renderer, observed where an observer is given (before each render and while it runs), with an
    /// optional hook run before the first thumbnail is drawn, which is after Design and before the assembly.
    /// </summary>
    private sealed class HookedTool(ExternalMediaTool inner, TransactionObserver? observer, Action? beforeThumbnail) : IMediaTool, IGraphicRenderer
    {
        private Action? _hook = beforeThumbnail;

        public async Task<GraphicInvocation> InvokeAsync(GraphicInvocationRequest request, CancellationToken cancellationToken)
        {
            if (observer is null)
            {
                return await inner.InvokeAsync(request, cancellationToken);
            }

            await observer.ObserveAsync();
            var running = inner.InvokeAsync(request, cancellationToken);
            while (!running.IsCompleted)
            {
                await Task.WhenAny(running, Task.Delay(200, CancellationToken.None));
                if (!running.IsCompleted)
                {
                    await observer.ObserveAsync();
                }
            }

            return await running;
        }

        public Task<string> VersionLineAsync(CancellationToken cancellationToken) => inner.VersionLineAsync(cancellationToken);

        public Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken)
        {
            if (still.Style == StillStyle.Thumbnail || still.Style == StillStyle.Placeholder)
            {
                Interlocked.Exchange(ref _hook, null)?.Invoke();
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
}
