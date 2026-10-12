using System.Diagnostics;
using MediaCompany.Application.Ports;
using MediaCompany.Graphics.Fixtures;
using Xunit;

namespace MediaCompany.Production.Tests;

/// <summary>
/// The installation verifier and the render invocation through the one process starter (the graphics change, decisions D-002
/// and D-003 of its design), over a fixture installation made in the test with the graphics stand-in as its Node. No test here
/// runs the installed renderer.
/// </summary>
public sealed class GraphicsRenderTests : IDisposable
{
    private readonly string _output = Path.Combine(Path.GetTempPath(), "mediacompany-graphics-test-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try
        {
            Directory.Delete(_output, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    // -----------------------------------------------------------------------
    // The installation verifier
    // -----------------------------------------------------------------------

    [Fact]
    public async Task AFixtureInstallationVerifiesAndItsIdentitiesAreReadFromItsVerifiedBytes()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var settings = fixture.Settings() with { Licence = new ConfiguredLicence("a configured licence that is not an identity", "the test") };
        var report = await new RendererInstallationVerifier(settings).VerifyAsync(CancellationToken.None);

        Assert.True(report.Verified, string.Join("\n", report.Findings));
        Assert.Equal("remotion", report.RendererName);
        Assert.Equal("0.0.0-fixture", report.RendererVersion);
        Assert.Equal("1.2.3.4", report.BrowserVersion);
        Assert.Equal("Fixture Sans", report.Italic!.Reading.Family);
        Assert.Equal("Italic", report.Italic.Reading.Subfamily);
        Assert.Equal(GraphicsFixture.Sha256(fixture.RegularFace), report.Regular!.Sha256);
        Assert.Equal(GraphicsFixture.Sha256(fixture.NodeExecutable), report.NodeSha256);
        Assert.Equal(4, report.Manifests.Count);
        Assert.DoesNotContain("a configured licence", report.DescribeManifests() + report.DescribeFaces() + report.RendererPackages, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AChangedByteIsRefusedNamingBothHashes()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var before = GraphicsFixture.Sha256(fixture.BrowserExecutable);
        await File.AppendAllTextAsync(fixture.BrowserExecutable, "x");
        var after = GraphicsFixture.Sha256(fixture.BrowserExecutable);

        var report = await new RendererInstallationVerifier(fixture.Settings()).VerifyAsync(CancellationToken.None);
        Assert.False(report.Verified);
        Assert.Contains(report.Findings, f => f.Contains(after, StringComparison.Ordinal) && f.Contains(before, StringComparison.Ordinal) && f.Contains("fixture-browser.bin", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AnUnlistedFileUnderTheRenderersPackagesIsRefused()
    {
        using var fixture = new GraphicsFixture("deterministic");
        Directory.CreateDirectory(Path.Combine(fixture.Renderer, "node_modules", ".cache", "webpack"));
        await File.WriteAllTextAsync(Path.Combine(fixture.Renderer, "node_modules", ".cache", "webpack", "planted.pack"), "a re-created cache");

        var report = await new RendererInstallationVerifier(fixture.Settings()).VerifyAsync(CancellationToken.None);
        Assert.Contains(report.Findings, f => f.Contains("planted.pack", StringComparison.Ordinal) && f.Contains("does not list", StringComparison.Ordinal));
    }

    /// <summary>
    /// A NODE_MODULES IN A PARENT FOLDER of the installed renderer folder (the review's correction CR-002), from which Node and
    /// the bundler would resolve a module no manifest covers, refuses the installation by name; removed, it verifies again.
    /// </summary>
    [Fact]
    public async Task ANodeModulesFolderInAParentOfTheRendererFolderIsRefused()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var planted = Path.Combine(fixture.Root, "node_modules", "bufferutil");
        Directory.CreateDirectory(planted);
        await File.WriteAllTextAsync(Path.Combine(planted, "index.js"), "module.exports = 'a planted module';");

        var report = await new RendererInstallationVerifier(fixture.Settings()).VerifyAsync(CancellationToken.None);
        Assert.False(report.Verified);
        Assert.Contains(report.Findings, f => f.StartsWith(Path.Combine(fixture.Root, "node_modules") + ":", StringComparison.OrdinalIgnoreCase)
            && f.Contains("a node_modules in an ancestor folder of the installed renderer folder", StringComparison.Ordinal));

        Directory.Delete(Path.Combine(fixture.Root, "node_modules"), recursive: true);
        Assert.True((await new RendererInstallationVerifier(fixture.Settings()).VerifyAsync(CancellationToken.None)).Verified);
    }

    /// <summary>
    /// THE RENDER SCRIPT REFUSES A MODULE RESOLVING OUTSIDE THE INSTALLED RENDERER FOLDER (the review's correction CR-002), run by
    /// the real Node through the one starter: with the installed bundler package absent and a planted one in the renderer
    /// folder's parent node_modules, the script exits 3 naming the planted file, and the planted module never ran. Needs Node
    /// installed; not-run, saying why, where it is not.
    /// </summary>
    [RequiresNodeFact]
    public async Task TheRenderScriptRefusesAModuleResolvingOutsideTheInstalledRendererFolderAndNeverRunsIt()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var planted = Path.Combine(fixture.Root, "node_modules", "@remotion", "bundler");
        Directory.CreateDirectory(planted);
        var marker = Path.Combine(fixture.Root, "planted-module-ran.txt");
        await File.WriteAllTextAsync(Path.Combine(planted, "package.json"), """{"name":"@remotion/bundler","main":"index.js"}""");
        await File.WriteAllTextAsync(Path.Combine(planted, "index.js"),
            $"require('fs').writeFileSync({System.Text.Json.JsonSerializer.Serialize(marker)}, 'ran'); module.exports = {{ bundle: async () => {{}} }};");
        Directory.Delete(Path.Combine(fixture.Renderer, "node_modules", "@remotion", "bundler"), recursive: true);

        var settings = MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with
        {
            Graphics = fixture.Settings() with { NodeExecutable = RequiresNodeFactAttribute.Node },
            RendererPath = fixture.NodeExecutable, ProbePath = fixture.NodeExecutable, FontFile = fixture.ItalicFace,
        };
        var store = new ArtifactWriter(_output, MediaTool.RepositoryRoot());
        var folder = store.StagingSubfolder(store.BeginRun("item/v2"), "graphics");
        var temporary = store.StagingSubfolder(folder, "tmp");
        await StageAsync(store, folder, fixture);

        var refused = await Assert.ThrowsAsync<MediaToolFailedException>(() => new ExternalMediaTool(settings).InvokeAsync(
            new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Bundle, "bundle-request.json", temporary, TimeSpan.FromSeconds(60)), CancellationToken.None));
        Assert.Contains("exited with code 3", refused.Message, StringComparison.Ordinal);
        Assert.Contains("refused: the module " + Path.Combine(planted, "index.js"), refused.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(File.Exists(marker), "the planted module ran");
    }

    [Fact]
    public async Task AnAbsoluteOrClimbingManifestEntryIsRefused()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var node = fixture.NodeManifest;
        var absolute = $"{GraphicsFixture.Sha256(fixture.NodeExecutable)} */c/Program Files/nodejs/node.exe\n";
        await File.WriteAllTextAsync(node.Manifest, absolute);
        var settings = fixture.Settings() with { Node = node with { Sha256 = GraphicsFixture.Sha256(node.Manifest) } };

        var report = await new RendererInstallationVerifier(settings).VerifyAsync(CancellationToken.None);
        Assert.Contains(report.Findings, f => f.Contains("is absolute or climbs out of its folder", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AManifestThatDiffersFromItsConfiguredHashIsRefused()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var settings = fixture.Settings() with { Font = fixture.FontManifest with { Sha256 = new string('0', 64) } };
        var report = await new RendererInstallationVerifier(settings).VerifyAsync(CancellationToken.None);
        Assert.Contains(report.Findings, f => f.Contains("the font manifest", StringComparison.Ordinal) && f.Contains(new string('0', 64), StringComparison.Ordinal));
    }

    /// <summary>The hold: while held, a listed file can be neither replaced nor deleted; released, it can.</summary>
    [Fact]
    public async Task AHeldInstallationCannotBeChangedUntilItIsReleased()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var (report, held) = await new RendererInstallationVerifier(fixture.Settings()).HoldAsync(CancellationToken.None);
        Assert.True(report.Verified);
        await using (held)
        {
            Assert.ThrowsAny<IOException>(() => File.WriteAllText(fixture.BrowserExecutable, "replaced"));
            Assert.ThrowsAny<IOException>(() => File.Delete(fixture.RegularFace));
        }

        File.WriteAllText(fixture.BrowserExecutable, "replaced after the hold was released");
    }

    /// <summary>Verification reads files only: it writes nothing and starts no process.</summary>
    [Fact]
    public async Task VerificationWritesNothingAndStartsNoProcess()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var files = Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories).Select(f => (f, File.GetLastWriteTimeUtc(f))).ToArray();
        var standIns = Process.GetProcessesByName("MediaCompany.GraphicsStandIn").Length;

        await new RendererInstallationVerifier(fixture.Settings()).VerifyAsync(CancellationToken.None);

        Assert.Equal(files, Directory.GetFiles(fixture.Root, "*", SearchOption.AllDirectories).Select(f => (f, File.GetLastWriteTimeUtc(f))).ToArray());
        Assert.Equal(standIns, Process.GetProcessesByName("MediaCompany.GraphicsStandIn").Length);
    }

    // -----------------------------------------------------------------------
    // The plan: the check, the installation and the coverage, read only
    // -----------------------------------------------------------------------

    /// <summary>
    /// THE PLAN REFUSES A CHARACTER THE FACE LACKS, before any process: over a fixture installation whose regular face lacks the
    /// theta, the graphics plan names the character, its code point and every graphic drawing it; over a complete face it passes.
    /// </summary>
    [Fact]
    public async Task ThePlanRefusesACharacterTheFaceLacksNamingItAndPassesWithACompleteFace()
    {
        var loaded = await ItemPackageLoader.LoadAsync(MediaTool.RepositoryRoot(), "wave-2/item-001/item-material.json", CancellationToken.None);
        using var lacking = new GraphicsFixture("deterministic", excludedFromRegular: new HashSet<int> { 0x03B8 });
        var refused = await GraphicsPlanner.ComposeAsync(
            MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with { Graphics = lacking.Settings() }, loaded, MediaCompany.Domain.Production.StoreDesignation.Demonstration, CancellationToken.None);
        Assert.True(refused.Refused);
        Assert.Contains(refused.Findings, f => f.Contains("gfx-05.json element thetaLabel", StringComparison.Ordinal) && f.Contains("U+03B8", StringComparison.Ordinal));
        Assert.All(refused.Findings, f => Assert.Contains("U+03B8", f, StringComparison.Ordinal));

        using var complete = new GraphicsFixture("deterministic");
        var passed = await GraphicsPlanner.ComposeAsync(
            MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with { Graphics = complete.Settings() }, loaded, MediaCompany.Domain.Production.StoreDesignation.Demonstration, CancellationToken.None);
        Assert.False(passed.Refused, string.Join("\n", passed.Findings));
        Assert.Equal(22, passed.Check!.Specifications.Count);
    }

    /// <summary>In the company store the plan refuses before any call without the owner's approval file.</summary>
    [Fact]
    public async Task InTheCompanyStoreThePlanRefusesWithoutTheOwnersApproval()
    {
        var loaded = await ItemPackageLoader.LoadAsync(MediaTool.RepositoryRoot(), "wave-2/item-001/item-material.json", CancellationToken.None);
        using var fixture = new GraphicsFixture("deterministic");
        var plan = await GraphicsPlanner.ComposeAsync(
            MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with { Graphics = fixture.Settings() }, loaded, MediaCompany.Domain.Production.StoreDesignation.Company, CancellationToken.None);
        Assert.True(plan.Refused);
        Assert.Contains(plan.Findings, f => f.Contains("no owner's approval file is configured", StringComparison.Ordinal));
    }

    /// <summary>
    /// THE APPROVAL FILE'S SHAPE AND COMPARISONS (the review's ruling on its question 2): an approval written from the plan's own
    /// hashes passes in the company store; a drawing program digest, a render script hash, a specification hash or a kind other
    /// than the approved one is refused by name before any call, and a file with an unknown member, an upper-case kind, a still
    /// whose still hash is not its final frame or a motion graphic carrying a still hash is refused as not of the recorded shape.
    /// </summary>
    [Theory]
    [InlineData("none", null)]
    [InlineData("drawing", "the owner approved the drawing program digest")]
    [InlineData("script", "the owner approved the render script sha256")]
    [InlineData("specification", "GFX-04: the owner approved its specification sha256")]
    [InlineData("kind", "GFX-04: the owner approved it as Motion, and its specification is Still")]
    [InlineData("unknown", "is not an approval file of the recorded shape (unknown member approvedBy)")]
    [InlineData("upperKind", "is not an approval file of the recorded shape (kind Still is neither still nor motion)")]
    [InlineData("stillHash", "is not an approval file of the recorded shape (GFX-01: a still's stillSha256 is its finalFrameSha256)")]
    [InlineData("motionStill", "is not an approval file of the recorded shape (GFX-02: a motion graphic has no stillSha256)")]
    public async Task TheApprovalFileIsReadInItsRecordedShapeAndComparedBeforeAnyCall(string fault, string? expected)
    {
        var loaded = await ItemPackageLoader.LoadAsync(MediaTool.RepositoryRoot(), "wave-2/item-001/item-material.json", CancellationToken.None);
        using var fixture = new GraphicsFixture("deterministic");
        var settings = MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with { Graphics = fixture.Settings() };
        var shown = await GraphicsPlanner.ComposeAsync(settings, loaded, MediaCompany.Domain.Production.StoreDesignation.Demonstration, CancellationToken.None);
        Assert.False(shown.Refused, string.Join("\n", shown.Findings));

        string Other(string hash) => hash[..^4] + (hash.EndsWith("0000", StringComparison.Ordinal) ? "1111" : "0000");
        var frame = new string('f', 64);
        var json = new System.Text.Json.Nodes.JsonObject
        {
            ["date"] = "2026-10-11",
            ["approver"] = "a fixture approver",
            ["styleSha256"] = shown.Style!.Sha256,
            ["drawingProgramDigest"] = fault == "drawing" ? Other(shown.DrawingDigest) : shown.DrawingDigest,
            ["renderScriptSha256"] = fault == "script" ? Other(shown.RenderScript!.Sha256) : shown.RenderScript!.Sha256,
            ["graphics"] = new System.Text.Json.Nodes.JsonArray(shown.Check!.Specifications.Select(s => (System.Text.Json.Nodes.JsonNode)new System.Text.Json.Nodes.JsonObject
            {
                ["position"] = s.Position,
                ["specificationSha256"] = fault == "specification" && s.Position == "GFX-04" ? Other(s.Sha256) : s.Sha256,
                ["kind"] = fault == "upperKind" ? s.Kind.ToString() : (fault == "kind" && s.Position == "GFX-04") || s.Kind == MediaCompany.Domain.Production.GraphicKind.Motion ? "motion" : "still",
                ["finalFrameSha256"] = frame,
                ["stillSha256"] = (s.Kind == MediaCompany.Domain.Production.GraphicKind.Still && !(fault == "kind" && s.Position == "GFX-04")) || (fault == "motionStill" && s.Position == "GFX-02")
                    ? (fault == "stillHash" && s.Position == "GFX-01" ? Other(frame) : frame)
                    : null,
            }).ToArray()),
        };
        if (fault == "unknown")
        {
            json["approvedBy"] = "an unrecorded member";
        }

        Directory.CreateDirectory(_output);
        var path = Path.Combine(_output, "approval.json");
        await File.WriteAllTextAsync(path, json.ToJsonString());
        var sha = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path)));
        var company = await GraphicsPlanner.ComposeAsync(settings with { Graphics = fixture.Settings() with { ApprovalFile = new ExpectedFile(path, sha) } },
            loaded, MediaCompany.Domain.Production.StoreDesignation.Company, CancellationToken.None);

        if (expected is null)
        {
            Assert.False(company.Refused, string.Join("\n", company.Findings));
            Assert.Equal(shown.DrawingDigest, company.Approval!.DrawingProgramDigest);
        }
        else
        {
            Assert.True(company.Refused);
            Assert.Contains(company.Findings, f => f.Contains(expected, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// THE OWNER GUIDE'S APPROVAL FILE IS ACCEPTED AS WRITTEN (the review's correction CR-003): the complete example in
    /// wave-11/owner-guide-graphics.md, written to a file as it stands, passes the company plan's shape and every comparison
    /// against this repository's specifications, style file, drawing program and render script.
    /// </summary>
    [Fact]
    public async Task TheOwnerGuidesApprovalFileExampleIsAcceptedByTheCompanyPlanAsWritten()
    {
        var guide = await File.ReadAllTextAsync(Path.Combine(MediaTool.RepositoryRoot(), "wave-11", "owner-guide-graphics.md"));
        var start = guide.IndexOf("```json", StringComparison.Ordinal);
        Assert.True(start >= 0, "the guide holds no approval file example");
        var body = guide[(guide.IndexOf('\n', start) + 1)..guide.IndexOf("```", start + 7, StringComparison.Ordinal)];

        Directory.CreateDirectory(_output);
        var path = Path.Combine(_output, "approval-from-the-guide.json");
        await File.WriteAllTextAsync(path, body);
        var sha = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(await File.ReadAllBytesAsync(path)));
        var loaded = await ItemPackageLoader.LoadAsync(MediaTool.RepositoryRoot(), "wave-2/item-001/item-material.json", CancellationToken.None);
        using var fixture = new GraphicsFixture("deterministic");
        var plan = await GraphicsPlanner.ComposeAsync(
            MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with { Graphics = fixture.Settings() with { ApprovalFile = new ExpectedFile(path, sha) } },
            loaded, MediaCompany.Domain.Production.StoreDesignation.Company, CancellationToken.None);

        Assert.False(plan.Refused, string.Join("\n", plan.Findings));
        Assert.Equal(22, plan.Approval!.Graphics.Count);
        Assert.Equal(10, plan.Approval.Graphics.Count(g => g.StillSha256 is not null));
    }

    // -----------------------------------------------------------------------
    // The render invocation through the one starter
    // -----------------------------------------------------------------------

    /// <summary>
    /// A stand-in bundle and render through the boundary: the stand-in accepts only the three arguments and only the cleared
    /// environment pointing into staging (it exits 9 naming any breach), so success is the evidence; the frames land in staging
    /// and the one writer promotes them.
    /// </summary>
    [Fact]
    public async Task AStandInRenderStartedThroughTheBoundaryWritesItsFramesInStagingAndIsPromoted()
    {
        using var fixture = new GraphicsFixture("deterministic");
        var (tool, store, folder, temporary) = Compose(fixture);
        await StageAsync(store, folder, fixture);

        var bundled = await tool.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Bundle, "bundle-request.json", temporary, TimeSpan.FromSeconds(60)), CancellationToken.None);
        Assert.Equal(new[] { fixture.NodeExecutable, "render.mjs", "bundle", "bundle-request.json" }, bundled.Arguments);
        Assert.True(File.Exists(Path.Combine(folder, "bundle", "index.html")));

        var rendered = await tool.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Render, "render-request.json", temporary, TimeSpan.FromSeconds(60)), CancellationToken.None);
        Assert.Equal(0, rendered.LeftoverProcesses);
        Assert.Equal(12, Directory.GetFiles(Path.Combine(folder, "frames")).Length);
        var promoted = await store.PromoteAsync(Path.Combine(folder, "frames"), "element-0.png", "item/v2/design/frame-000000.png", CancellationToken.None);
        Assert.StartsWith(store.OutputRoot, promoted.FullPath, StringComparison.OrdinalIgnoreCase);

        // No licence key and no telemetry option is passed: the whole argument list is the four values above.
        Assert.DoesNotContain(rendered.Arguments, a => a.Contains("licen", StringComparison.OrdinalIgnoreCase) || a.Contains("telemetry", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(4, rendered.Arguments.Count);
    }

    /// <summary>
    /// PAST ITS BOUND THE WHOLE JOB ENDS: a stand-in that started a grandchild and then waits outlasts a short bound; the
    /// invocation ends under the bound outcome and no stand-in process started by this test remains.
    /// </summary>
    [Fact]
    public async Task AStandInOutlastingAShortBoundEndsUnderTheBoundAndLeavesNoProcess()
    {
        using var fixture = new GraphicsFixture("grandchild");
        var (tool, store, folder, temporary) = Compose(fixture);
        await StageAsync(store, folder, fixture);
        var started = DateTime.Now;

        var exceeded = await Assert.ThrowsAsync<MediaToolBoundExceededException>(() =>
            tool.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Render, "render-request.json", temporary, TimeSpan.FromSeconds(3)), CancellationToken.None));
        Assert.Equal("graphic render", exceeded.Invocation);
        await Task.Delay(500);
        Assert.DoesNotContain(Process.GetProcessesByName("MediaCompany.GraphicsStandIn"), p => Started(p) >= started.AddSeconds(-1));
    }

    /// <summary>An orphan the stand-in left behind is counted in the job before it is closed, and closing the job ends it.</summary>
    [Fact]
    public async Task AnOrphanLeftInTheJobIsCountedAndEnded()
    {
        using var fixture = new GraphicsFixture("orphan");
        var (tool, store, folder, temporary) = Compose(fixture);
        await StageAsync(store, folder, fixture);
        var started = DateTime.Now;

        var rendered = await tool.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Render, "render-request.json", temporary, TimeSpan.FromSeconds(60)), CancellationToken.None);
        Assert.True(rendered.LeftoverProcesses >= 1);
        await Task.Delay(500);
        Assert.DoesNotContain(Process.GetProcessesByName("MediaCompany.GraphicsStandIn"), p => Started(p) >= started.AddSeconds(-1));
    }

    [Fact]
    public async Task ARendererFailureIsNamedWithItsExitCode()
    {
        using var fixture = new GraphicsFixture("fail");
        var (tool, store, folder, temporary) = Compose(fixture);
        await StageAsync(store, folder, fixture);
        var failed = await Assert.ThrowsAsync<MediaToolFailedException>(() =>
            tool.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Render, "render-request.json", temporary, TimeSpan.FromSeconds(60)), CancellationToken.None));
        Assert.Equal(4, failed.ExitCode);
        Assert.Contains("the renderer failed", failed.Message, StringComparison.Ordinal);
    }

    /// <summary>An absent Node, browser, face or staged render script refuses by name before any process starts.</summary>
    [Theory]
    [InlineData("node", "graphics.nodeExecutable")]
    [InlineData("browser", "graphics.browserExecutable")]
    [InlineData("face", "graphics.regularFace")]
    [InlineData("script", "graphics.renderScript")]
    public async Task ARefusedSettingStopsBeforeAnyProcessNamingIt(string absent, string setting)
    {
        using var fixture = new GraphicsFixture("deterministic");
        var graphics = fixture.Settings();
        graphics = absent switch
        {
            "node" => graphics with { NodeExecutable = Path.Combine(fixture.Node, "absent.bin") },
            "browser" => graphics with { BrowserExecutable = Path.Combine(fixture.Browser, "absent.bin") },
            "face" => graphics with { RegularFace = Path.Combine(fixture.Font, "absent.ttf") },
            _ => graphics,
        };
        var settings = MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with
        {
            Graphics = graphics, RendererPath = fixture.NodeExecutable, ProbePath = fixture.NodeExecutable, FontFile = fixture.ItalicFace,
        };
        var store = new ArtifactWriter(_output, MediaTool.RepositoryRoot());
        var folder = store.StagingSubfolder(store.BeginRun("item/v2"), "graphics");
        if (absent != "script")
        {
            await store.WriteStagingTextAsync(folder, "render.mjs", "staged", CancellationToken.None);
        }

        await store.WriteStagingTextAsync(folder, "render-request.json", "{}", CancellationToken.None);
        var standIns = Process.GetProcessesByName("MediaCompany.GraphicsStandIn").Length;
        var refused = await Assert.ThrowsAsync<ProductionSettingRefusedException>(async () =>
        {
            var tool = new ExternalMediaTool(settings);
            await tool.InvokeAsync(new GraphicInvocationRequest(folder, "render.mjs", GraphicMode.Render, "render-request.json", folder, TimeSpan.FromSeconds(10)), CancellationToken.None);
        });
        Assert.StartsWith(setting, refused.Setting, StringComparison.Ordinal);
        Assert.Equal(standIns, Process.GetProcessesByName("MediaCompany.GraphicsStandIn").Length);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private (ExternalMediaTool Tool, ArtifactWriter Store, string Folder, string Temporary) Compose(GraphicsFixture fixture)
    {
        var settings = MediaTool.Settings(_output, MediaTool.RepositoryRoot()) with
        {
            Graphics = fixture.Settings(), RendererPath = fixture.NodeExecutable, ProbePath = fixture.NodeExecutable, FontFile = fixture.ItalicFace,
        };
        var store = new ArtifactWriter(_output, MediaTool.RepositoryRoot());
        var staging = store.BeginRun("item/v2");
        var folder = store.StagingSubfolder(staging, "graphics");
        var temporary = store.StagingSubfolder(folder, "tmp");
        return (new ExternalMediaTool(settings), store, folder, temporary);
    }

    private static async Task StageAsync(ArtifactWriter store, string folder, GraphicsFixture fixture)
    {
        var repository = MediaTool.RepositoryRoot();
        await store.WriteStagingBytesAsync(folder, "render.mjs", await File.ReadAllBytesAsync(Path.Combine(repository, "graphics", "render.mjs")), CancellationToken.None);
        var drawing = store.StagingSubfolder(folder, "drawing");
        await store.WriteStagingBytesAsync(drawing, "index.jsx", await File.ReadAllBytesAsync(Path.Combine(repository, "graphics", "drawing", "index.jsx")), CancellationToken.None);
        var publicFolder = store.StagingSubfolder(folder, "public");
        await store.CopyIntoStagingAsync(publicFolder, fixture.RegularFace, "face-regular.ttf", CancellationToken.None);
        await store.WriteStagingBytesAsync(folder, "style.json", await File.ReadAllBytesAsync(Path.Combine(repository, "config", "graphics-style.json")), CancellationToken.None);
        await store.WriteStagingBytesAsync(folder, "gfx-02.json", await File.ReadAllBytesAsync(Path.Combine(repository, "wave-2", "item-001", "graphics", "gfx-02.json")), CancellationToken.None);
        var anchor = Path.Combine(fixture.Renderer, "package.json");
        await store.WriteStagingBytesAsync(folder, "bundle-request.json", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["anchor"] = anchor, ["modules"] = Path.Combine(fixture.Renderer, "node_modules"), ["entry"] = "drawing/index.jsx",
            ["publicDir"] = "public", ["outDir"] = "bundle", ["moduleList"] = "bundle-modules.json",
        }), CancellationToken.None);
        await store.WriteStagingBytesAsync(folder, "render-request.json", System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new Dictionary<string, object>
        {
            ["anchor"] = anchor, ["bundle"] = "bundle", ["browser"] = fixture.BrowserExecutable, ["specification"] = "gfx-02.json", ["style"] = "style.json",
            ["timing"] = new Dictionary<string, object> { ["kind"] = "motion", ["frames"] = 12, ["fps"] = 30, ["width"] = 64, ["height"] = 36 },
            ["faces"] = new Dictionary<string, object> { ["regular"] = "face-regular.ttf", ["italic"] = "face-italic.ttf" },
            ["output"] = "frames", ["concurrency"] = 1, ["frameTimeoutMs"] = 30000,
        }), CancellationToken.None);
    }

    private static DateTime Started(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (Exception unreadable) when (unreadable is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return DateTime.MinValue;
        }
    }
}

/// <summary>A fact that needs Node installed in its usual folder; not-run, naming why, where it is not.</summary>
public sealed class RequiresNodeFactAttribute : FactAttribute
{
    internal static string Node { get; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "nodejs", "node.exe");

    public RequiresNodeFactAttribute()
    {
        if (!File.Exists(Node))
        {
            Skip = $"not-run: needs Node at {Node}.";
        }
    }
}
