using System.Globalization;
using MediaCompany.Application.Ports;

namespace MediaCompany.Production;

/// <summary>
/// Everything the production path is configured with (the production change, decisions D-008, D-013 and D-014 of
/// its design). No member has a default in code: the bounds, the profile and the character maximum are configured
/// amounts, ruled at the Design Gate, and every printed figure from them is labelled configured. The executables, the
/// font, the output root and the repository root are located by configuration and never by a path written in source.
///
/// THE VENDOR MEMBERS ARE OPTIONAL (the own-voice change, decision D-011 of its design): the provider-call bound, the
/// character maximum and the voice are read only by the boundary sources, and each mode that needs one refuses by name
/// without it. The in-house model's section is read only by the own mode and plan-only.
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

    /// <summary>The bound on a full decode of a produced file; the narration join and every audio measurement borrow it too (correction CR-008).</summary>
    public required TimeSpan DecodeBound { get; init; }

    /// <summary>The bound on one probe of a file; the tool's version check borrows it too (correction CR-008).</summary>
    public required TimeSpan ProbeBound { get; init; }

    public required TimeSpan StillBound { get; init; }

    /// <summary>The provider-call bound the composition gives the boundary, printed with the plan; read by the boundary sources only.</summary>
    public TimeSpan? ProviderCallBound { get; init; }

    /// <summary>The most characters one narration request carries; read by the boundary sources only.</summary>
    public int? NarrationCharacterMaximum { get; init; }

    /// <summary>
    /// The vendor voice a narration request names. The code carries NO default, so a boundary source refuses without it;
    /// the own sources never read it.
    /// </summary>
    public string? NarrationVoice { get; init; }

    /// <summary>The in-house model's section, or null where none is configured.</summary>
    public InHouseModelSettings? InHouseModel { get; init; }

    /// <summary>States the configured figures, each labelled configured; a member not configured is said so.</summary>
    public string DescribeBounds() =>
        string.Create(CultureInfo.InvariantCulture,
            $"provider call {(ProviderCallBound is { } call ? $"{call.TotalSeconds:0} s" : "not configured")}; render {RenderBound.TotalMinutes:0} min; ")
        + string.Create(CultureInfo.InvariantCulture,
            $"decode {DecodeBound.TotalMinutes:0} min (the narration join and every audio measurement too); probe {ProbeBound.TotalSeconds:0} s (the version check too); ")
        + string.Create(CultureInfo.InvariantCulture,
            $"still {StillBound.TotalSeconds:0} s; profile {Width}x{Height} at {FramesPerSecond} frames a second; ")
        + $"narration character maximum {(NarrationCharacterMaximum is { } maximum ? maximum.ToString(CultureInfo.InvariantCulture) : "not configured")}"
        + (InHouseModel is { } model ? string.Create(CultureInfo.InvariantCulture, $"; in-house model part bound {model.PartBound.TotalSeconds:0} s") : string.Empty)
        + " (all configured)";
}

/// <summary>One file and the SHA-256 it is expected to hash to, both configured outside the repository.</summary>
public sealed record ExpectedFile(string Path, string Sha256);

/// <summary>A licence value taken from CONFIGURATION, with the source the configuration names; never read from an installed file.</summary>
public sealed record ConfiguredLicence(string Value, string Source)
{
    public string Describe() => $"{Value} (CONFIGURED, not read from an installed file: {Source})";
}

/// <summary>
/// The in-house model's section (the own-voice change, decisions D-005, D-006 and D-011 of its design), written by the
/// orchestrator into the run's settings file OUTSIDE the repository. Every path and every expected SHA-256 is configured,
/// none has a default in code, and the expected values are never written to a store. The part bound is a configured amount
/// (350 s at the Design Gate), labelled configured wherever it is printed.
/// </summary>
public sealed record InHouseModelSettings
{
    /// <summary>The environment's interpreter, started in isolated mode.</summary>
    public required ExpectedFile Interpreter { get; init; }

    /// <summary>The environment's configuration file, which names the base interpreter the environment's launchers start.</summary>
    public required ExpectedFile EnvironmentConfiguration { get; init; }

    /// <summary>The expected SHA-256 of the base interpreter the environment's configuration names; its path is read from that verified file.</summary>
    public required string BaseInterpreterSha256 { get; init; }

    /// <summary>The runtime's module name, run with the interpreter's module option.</summary>
    public required string RuntimeModule { get; init; }

    /// <summary>The voice model file.</summary>
    public required ExpectedFile Model { get; init; }

    /// <summary>The voice configuration file; it must be the model path plus ".json", because the runtime loads that one.</summary>
    public required ExpectedFile Configuration { get; init; }

    /// <summary>The voice's model card.</summary>
    public required ExpectedFile ModelCard { get; init; }

    /// <summary>The runtime package's installed-files record, whose every hashed entry is verified.</summary>
    public required ExpectedFile RuntimeRecord { get; init; }

    /// <summary>The installed-files record of every dependency the runtime runs with, each verified with its entries.</summary>
    public required IReadOnlyList<ExpectedFile> DependencyRecords { get; init; }

    /// <summary>The six generation settings, passed explicitly.</summary>
    public required VoiceGenerationSettings Generation { get; init; }

    /// <summary>The bound on one generation, on the monotonic clock; past it the process tree is terminated.</summary>
    public required TimeSpan PartBound { get; init; }

    /// <summary>The voice weights' licence where the configuration records one with its source; otherwise not stated in the installed files.</summary>
    public ConfiguredLicence? WeightsLicence { get; init; }

    /// <summary>The voice's licence where the configuration records one with its source; otherwise not stated in the installed files.</summary>
    public ConfiguredLicence? VoiceLicence { get; init; }

    /// <summary>The interpreter's path, as configured.</summary>
    public string InterpreterPath => Interpreter.Path;
}

/// <summary>A required setting is absent or names nothing; refused before any capability call, naming the setting.</summary>
public sealed class ProductionSettingRefusedException(string setting, string reason)
    : Exception($"the setting {setting} is refused: {reason}")
{
    public string Setting { get; } = setting;
}
