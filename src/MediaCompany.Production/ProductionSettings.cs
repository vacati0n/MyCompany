namespace MediaCompany.Production;

/// <summary>
/// Everything the production path is configured with (the production change, decisions D-008, D-013 and D-014 of
/// its design). Every member is REQUIRED and has no default in code: the bounds, the profile and the character
/// maximum are configured amounts, ruled at the Design Gate, and every printed figure from them is labelled
/// configured. The executables, the font, the output root and the repository root are located by configuration
/// and never by a path written in source.
/// </summary>
public sealed record ProductionSettings
{
    /// <summary>The one folder every produced file is written under; refused inside the repository.</summary>
    public required string OutputRoot { get; init; }

    /// <summary>The repository's root, against which the output root is refused by containment and the package is read.</summary>
    public required string RepositoryRoot { get; init; }

    /// <summary>The renderer executable (the external media tool).</summary>
    public required string RendererPath { get; init; }

    /// <summary>The probe executable of the same tool.</summary>
    public required string ProbePath { get; init; }

    /// <summary>The fixed font file every still is drawn with.</summary>
    public required string FontFile { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    public required int FramesPerSecond { get; init; }

    public required TimeSpan RenderBound { get; init; }

    public required TimeSpan DecodeBound { get; init; }

    public required TimeSpan ProbeBound { get; init; }

    public required TimeSpan StillBound { get; init; }

    /// <summary>The provider-call bound the composition gives the boundary, printed with the plan.</summary>
    public required TimeSpan ProviderCallBound { get; init; }

    /// <summary>The most characters one narration request carries.</summary>
    public required int NarrationCharacterMaximum { get; init; }

    /// <summary>
    /// The vendor voice a narration request names. It is a channel voice choice the owner has not made (the open
    /// question on channel one's configuration values), so it has no default: metered mode refuses without it.
    /// </summary>
    public string? NarrationVoice { get; init; }

    /// <summary>States the configured figures, each labelled configured.</summary>
    public string DescribeBounds() =>
        $"provider call {ProviderCallBound.TotalSeconds:0} s; render {RenderBound.TotalMinutes:0} min; decode {DecodeBound.TotalMinutes:0} min; "
        + $"probe {ProbeBound.TotalSeconds:0} s; still {StillBound.TotalSeconds:0} s; profile {Width}x{Height} at {FramesPerSecond} frames a second; "
        + $"narration character maximum {NarrationCharacterMaximum} (all configured)";
}

/// <summary>A required setting is absent or names nothing; refused before any capability call, naming the setting.</summary>
public sealed class ProductionSettingRefusedException(string setting, string reason)
    : Exception($"the setting {setting} is refused: {reason}")
{
    public string Setting { get; } = setting;
}
