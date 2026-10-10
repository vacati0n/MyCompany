using System.Text.Json;
using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;

namespace MediaCompany.Production;

/// <summary>A loaded item: its material, its narration text with line endings normalised, and the material's own hash.</summary>
public sealed record LoadedItem(ItemMaterial Material, string Narration, string MaterialPath);

/// <summary>
/// Reads an item's material and the records it quotes (the production change, decision D-011 of its design). Every
/// source is read with its line endings normalised to line feeds and nothing else, the package verifier's rule, and
/// the material check refuses the load naming the first mismatch. It reads files and writes none.
/// </summary>
public static class ItemPackageLoader
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static async Task<LoadedItem> LoadAsync(string repositoryRoot, string materialPath, CancellationToken cancellationToken)
    {
        var full = Path.IsPathRooted(materialPath) ? materialPath : Path.Combine(repositoryRoot, materialPath);
        var material = JsonSerializer.Deserialize<ItemMaterial>(await File.ReadAllBytesAsync(full, cancellationToken).ConfigureAwait(false), Options)
            ?? throw new ItemMaterialRefusedException($"the item material {materialPath} read as nothing");

        var paths = new HashSet<string>(StringComparer.Ordinal) { material.NarrationFile, material.SpecifiedRuntime.Source, material.UnsourcedClips.Source, material.ClipPlaceholders.Source };
        paths.UnionWith(material.Beats.SelectMany(b => new[] { b.StructureSource, b.OpeningSource, b.CueSource }));
        paths.UnionWith(material.Graphics.Select(g => g.Source));
        paths.UnionWith(material.Clips.Select(c => c.Source));
        paths.UnionWith(material.Thumbnails.Select(t => t.Source));
        paths.UnionWith(material.Thumbnails.Where(t => t.PlaceholderSource is not null).Select(t => t.PlaceholderSource!));

        var sources = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var path in paths)
        {
            var file = Path.Combine(repositoryRoot, path);
            if (!File.Exists(file))
            {
                throw new ItemMaterialRefusedException($"the source {path} the material quotes is not in the repository");
            }

            sources[path] = Normalised(await File.ReadAllBytesAsync(file, cancellationToken).ConfigureAwait(false));
        }

        if (MaterialCheck.FirstMismatch(material, sources) is { } mismatch)
        {
            throw new ItemMaterialRefusedException(mismatch);
        }

        return new LoadedItem(material, sources[material.NarrationFile], full);
    }

    /// <summary>UTF-8 text with every CRLF made a line feed, and nothing else changed.</summary>
    public static string Normalised(byte[] bytes) =>
        new System.Text.UTF8Encoding(false, true).GetString(bytes).TrimStart('﻿').Replace("\r\n", "\n", StringComparison.Ordinal);
}

/// <summary>The item material does not match the records it quotes; the load is refused naming the first mismatch.</summary>
public sealed class ItemMaterialRefusedException(string mismatch) : Exception($"the item material is refused: {mismatch}");
