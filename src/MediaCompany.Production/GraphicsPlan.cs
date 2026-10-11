using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;

namespace MediaCompany.Production;

/// <summary>One file of the drawing program or the render script, its bytes read once and their SHA-256.</summary>
public sealed record ProgramFile(string Name, byte[] Bytes, string Sha256);

/// <summary>One graphic the owner approved at the contact-sheet review, as the approval file records it (decision D-006).</summary>
public sealed record ApprovedGraphic(string Position, string SpecificationSha256, GraphicKind Kind, string FinalFrameSha256, string? StillSha256);

/// <summary>The owner's approval file, parsed from the very bytes whose SHA-256 matched its configured value (decision D-006).</summary>
public sealed record GraphicsApproval(string Sha256, string Date, string Approver, string StyleSha256, IReadOnlyList<ApprovedGraphic> Graphics)
{
    public ApprovedGraphic? For(string position) => Graphics.FirstOrDefault(g => g.Position == position);
}

/// <summary>
/// What a production's graphics are planned from (the graphics change, decisions D-001, D-003, D-009 and D-014 of its
/// design): every specification checked with the bytes read once, the style file, the drawing program and the render script
/// read once and hashed, the installation verified from its manifests WITHOUT holding it, every drawn character checked
/// against the verified faces, and in the company store the owner's approval read and compared. Composing it starts no
/// process and writes no file; a refusal names every finding before any call.
/// </summary>
public sealed record GraphicsPlan
{
    public required bool Configured { get; init; }

    public required IReadOnlyList<string> Findings { get; init; }

    public GraphicCheckResult? Check { get; init; }

    public ProgramFile? Style { get; init; }

    public IReadOnlyList<ProgramFile> Drawing { get; init; } = [];

    public ProgramFile? RenderScript { get; init; }

    public RendererReport? Installation { get; init; }

    public GraphicsApproval? Approval { get; init; }

    public bool Refused => Configured && Findings.Count > 0;

    /// <summary>The digest over the drawing program's files, each named with its SHA-256, in name order.</summary>
    public string DrawingDigest => Digest(Drawing.Select(f => (f.Name, f.Sha256)));

    public static string Digest(IEnumerable<(string Name, string Sha256)> files) =>
        Convert.ToHexStringLower(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            string.Concat(files.OrderBy(f => f.Name, StringComparer.Ordinal).Select(f => $"{f.Sha256}  {f.Name}\n")))));

    public static readonly GraphicsPlan NotConfigured = new() { Configured = false, Findings = [] };

    /// <summary>Prints the graphics section of the plan; every figure named for what it is.</summary>
    public void Print(TextWriter output, GraphicsSettings? settings)
    {
        ArgumentNullException.ThrowIfNull(output);
        var c = CultureInfo.InvariantCulture;
        if (!Configured || settings is null)
        {
            output.WriteLine("  graphics: NOT CONFIGURED (no graphics section in the settings file); a producing mode refuses before any call, and no text card is drawn instead");
            return;
        }

        var specs = Check?.Specifications ?? [];
        output.WriteLine(string.Create(c,
            $"  graphics: {specs.Count} specification(s) checked, {specs.Count(s => s.Kind == GraphicKind.Still)} still and {specs.Count(s => s.Kind == GraphicKind.Motion)} motion, drawn after Audio from the measured beat durations"));
        foreach (var spec in specs)
        {
            output.WriteLine($"    {spec.Position}: {spec.FileName}, {spec.Kind}, sha256 {spec.Sha256} (the bytes read once and staged)");
        }

        if (Installation is { } installed)
        {
            output.WriteLine($"    renderer: {installed.RendererName ?? "not read"} {installed.RendererVersion ?? "not read"} ({installed.RendererPackages}), read from its verified package files; "
                + string.Create(c, $"{installed.FilesVerified:N0} installed files verified from their manifests ({installed.DescribeManifests()})"));
            output.WriteLine($"    browser: version {installed.BrowserVersion ?? "not read"} read from its verified executable, sha256 {installed.BrowserSha256 ?? "not verified"}");
            output.WriteLine($"    node: version {installed.NodeVersion ?? "not read"} read from its verified executable, sha256 {installed.NodeSha256 ?? "not verified"}");
            output.WriteLine($"    font: {installed.DescribeFaces()}; every drawn character checked against these faces' character maps");
        }

        output.WriteLine($"    licence: {settings.Licence.Describe()}; no licence key is passed, so no usage event is sent");
        output.WriteLine($"    style {settings.StyleFile} sha256 {Style?.Sha256 ?? "not read"}; drawing program {settings.DrawingFolder} digest {DrawingDigest}; render script {settings.RenderScript} sha256 {RenderScript?.Sha256 ?? "not read"}");
        output.WriteLine($"    bounds (configured): {settings.DescribeBounds()}");
        if (Approval is { } approval)
        {
            output.WriteLine($"    owner's approval: {approval.Approver} on {approval.Date}, file sha256 {approval.Sha256} (equals its configured value); {approval.Graphics.Count} graphics approved; every still must equal its approved bytes and every motion graphic is re-timed from this run's narration");
        }

        output.WriteLine("    graphics cost: no metered spend is planned, USD 0.00; no model call; local compute unmeasured");
        foreach (var finding in Findings)
        {
            output.WriteLine($"    refused: {finding}");
        }
    }
}

/// <summary>Composes a production's <see cref="GraphicsPlan"/>; reads files only and starts no process.</summary>
public static class GraphicsPlanner
{
    public static async Task<GraphicsPlan> ComposeAsync(ProductionSettings settings, LoadedItem loaded, StoreDesignation designation, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(loaded);
        if (settings.Graphics is not { } graphics)
        {
            return GraphicsPlan.NotConfigured;
        }

        var findings = new List<string>();
        var repository = settings.RepositoryRoot;

        // The specifications: every file of the item's graphics folder, each read once.
        var folder = Path.Combine(Path.GetDirectoryName(loaded.MaterialPath)!, "graphics");
        var specifications = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        if (Directory.Exists(folder))
        {
            foreach (var file in Directory.EnumerateFiles(folder).Order(StringComparer.Ordinal))
            {
                specifications[Path.GetFileName(file)] = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
            }
        }
        else
        {
            findings.Add($"the item's graphics folder {folder} is missing");
        }

        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in GraphicSpecificationCheck.CitedSources(specifications).Order(StringComparer.Ordinal))
        {
            var full = Path.Combine(repository, path);
            if (Path.IsPathRooted(path) || path.Contains("..", StringComparison.Ordinal) || !File.Exists(full))
            {
                continue;
            }

            sources[path] = ItemPackageLoader.Normalised(await File.ReadAllBytesAsync(full, cancellationToken).ConfigureAwait(false));
        }

        var check = GraphicSpecificationCheck.Check(loaded.Material, specifications, sources);
        findings.AddRange(check.Findings);

        async Task<ProgramFile?> ReadAsync(string relative, string setting)
        {
            var full = Path.Combine(repository, relative);
            if (Path.IsPathRooted(relative) || !File.Exists(full))
            {
                findings.Add($"{setting}: {relative} is not a file in the repository");
                return null;
            }

            var bytes = await File.ReadAllBytesAsync(full, cancellationToken).ConfigureAwait(false);
            return new ProgramFile(Path.GetFileName(full), bytes, Convert.ToHexStringLower(SHA256.HashData(bytes)));
        }

        var style = await ReadAsync(graphics.StyleFile, "graphics.styleFile").ConfigureAwait(false);
        var script = await ReadAsync(graphics.RenderScript, "graphics.renderScript").ConfigureAwait(false);
        var drawing = new List<ProgramFile>();
        var drawingFolder = Path.Combine(repository, graphics.DrawingFolder);
        if (Directory.Exists(drawingFolder) && !Path.IsPathRooted(graphics.DrawingFolder))
        {
            foreach (var file in Directory.EnumerateFiles(drawingFolder).Order(StringComparer.Ordinal))
            {
                var bytes = await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false);
                drawing.Add(new ProgramFile(Path.GetFileName(file), bytes, Convert.ToHexStringLower(SHA256.HashData(bytes))));
            }

            if (Directory.EnumerateDirectories(drawingFolder).Any())
            {
                findings.Add($"graphics.drawingFolder: {graphics.DrawingFolder} holds a folder; the drawing program is one flat folder");
            }
        }
        else
        {
            findings.Add($"graphics.drawingFolder: {graphics.DrawingFolder} is not a folder in the repository");
        }

        // The installation, verified from its manifests and read only.
        var installation = await new RendererInstallationVerifier(graphics).VerifyAsync(cancellationToken).ConfigureAwait(false);
        findings.AddRange(installation.Findings);

        // Every drawn character, in the face it is set in, against that face's character map read from its verified bytes.
        if (installation.Regular is { } regular && installation.Italic is { } italic)
        {
            var drawnRegular = check.Specifications.SelectMany(s => s.Texts.Where(t => !t.Italic).Select(t => ($"{s.FileName} element {t.Element}", t.Text))).ToList();
            var drawnItalic = check.Specifications.SelectMany(s => s.Texts.Where(t => t.Italic).Select(t => ($"{s.FileName} element {t.Element}", t.Text)));
            foreach (var spec in check.Specifications)
            {
                using var document = JsonDocument.Parse(spec.Bytes);
                if (document.RootElement.TryGetProperty("disclosure", out var disclosure) && disclosure.TryGetProperty("text", out var text))
                {
                    drawnRegular.Add(($"{spec.FileName} disclosure", text.GetString() ?? string.Empty));
                }
            }

            findings.AddRange(FontCoverage.Uncovered(regular.Reading, drawnRegular));
            findings.AddRange(FontCoverage.Uncovered(italic.Reading, drawnItalic));
        }

        // The owner's approval, in the company store only.
        GraphicsApproval? approval = null;
        if (designation == StoreDesignation.Company)
        {
            approval = await ReadApprovalAsync(graphics, findings, cancellationToken).ConfigureAwait(false);
            if (approval is not null)
            {
                if (style is not null && !string.Equals(approval.StyleSha256, style.Sha256, StringComparison.Ordinal))
                {
                    findings.Add($"the owner approved the style sha256 {approval.StyleSha256}, and the style file now hashes {style.Sha256}");
                }

                foreach (var spec in check.Specifications)
                {
                    var approved = approval.For(spec.Position);
                    if (approved is null)
                    {
                        findings.Add($"{spec.Position}: the owner's approval names no approved graphic for it");
                    }
                    else if (!string.Equals(approved.SpecificationSha256, spec.Sha256, StringComparison.Ordinal))
                    {
                        findings.Add($"{spec.Position}: the owner approved its specification sha256 {approved.SpecificationSha256}, and it now hashes {spec.Sha256}");
                    }
                }
            }
        }

        return new GraphicsPlan
        {
            Configured = true,
            Findings = findings,
            Check = check,
            Style = style,
            Drawing = drawing,
            RenderScript = script,
            Installation = installation,
            Approval = approval,
        };
    }

    /// <summary>The approval file, hashed against its configured value and parsed from that one buffer; a finding where it cannot be.</summary>
    private static async Task<GraphicsApproval?> ReadApprovalAsync(GraphicsSettings graphics, List<string> findings, CancellationToken cancellationToken)
    {
        if (graphics.ApprovalFile is not { } configured)
        {
            findings.Add("graphics.approvalFile: no owner's approval file is configured; a company-store production needs the owner's contact-sheet approval first");
            return null;
        }

        if (!File.Exists(configured.Path))
        {
            findings.Add($"graphics.approvalFile: {configured.Path} does not exist; the owner's contact-sheet approval is recorded first");
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(configured.Path, cancellationToken).ConfigureAwait(false);
        var sha = Convert.ToHexStringLower(SHA256.HashData(bytes));
        if (!string.Equals(sha, configured.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            findings.Add($"graphics.approvalFile: {configured.Path} hashes {sha}, and {configured.Sha256} is configured as expected");
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(bytes);
            var root = document.RootElement;
            string S(JsonElement e, string key) => e.GetProperty(key).GetString() ?? throw new KeyNotFoundException(key);
            var graphicsApproved = root.GetProperty("graphics").EnumerateArray().Select(g => new ApprovedGraphic(
                S(g, "position"),
                S(g, "specificationSha256"),
                Enum.Parse<GraphicKind>(S(g, "kind"), ignoreCase: true),
                S(g, "finalFrameSha256"),
                g.TryGetProperty("stillSha256", out var still) && still.ValueKind == JsonValueKind.String ? still.GetString() : null)).ToArray();
            return new GraphicsApproval(sha, S(root, "date"), S(root, "approver"), S(root, "styleSha256"), graphicsApproved);
        }
        catch (Exception unreadable) when (unreadable is JsonException or KeyNotFoundException or InvalidOperationException or ArgumentException)
        {
            findings.Add($"graphics.approvalFile: {configured.Path} is not an approval file of the recorded shape ({unreadable.Message})");
            return null;
        }
    }
}
