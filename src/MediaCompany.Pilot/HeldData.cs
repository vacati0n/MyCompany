using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MediaCompany.Pilot;

/// <summary>One banned class (a) to (g): whole-word terms and the patterns for money amounts and ratings.</summary>
public sealed record BannedClass(string Id, string Name, IReadOnlyList<string> Terms, IReadOnlyList<string> Patterns);

/// <summary>One kit 04 checklist item, its identifier and its wording exactly as the kit table holds it.</summary>
public sealed record ChecklistItem(string Id, string Wording);

/// <summary>
/// The versioned data the toolchain holds (design D-004 and D-007): banned classes, exemptions, the sentence frames, the
/// call-to-action list, the kit 04 checklist, and the CEO's fixed limits. One embedded resource; tests keep the call-to-action
/// list and the checklist equal to the kit files.
/// </summary>
public sealed record HeldData
{
    public required int FormVersion { get; init; }
    public required string Source { get; init; }
    public required string DisclosureSentence { get; init; }
    public required string OverlayText { get; init; }
    public required string IntroductionFrame { get; init; }
    public required string FactFrame { get; init; }
    public required IReadOnlyList<string> CallToAction { get; init; }
    public required IReadOnlyList<BannedClass> BannedClasses { get; init; }
    public required IReadOnlyList<string> Exemptions { get; init; }
    public required int MinimumCaptionFontSize { get; init; }
    public required int MinimumOverlayFontSize { get; init; }
    public required double RecordingMaximumSeconds { get; init; }
    public required double RecordingUploadCeilingSeconds { get; init; }
    public required int PilotVideoLimit { get; init; }
    public required int VideosPerProductLimit { get; init; }
    public required IReadOnlyList<ChecklistItem> Checklist { get; init; }
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> ChecklistTicked { get; init; }

    public const int KnownFormVersion = 1;

    private static readonly Lazy<HeldData> Loaded = new(Load);

    public static HeldData Current => Loaded.Value;

    internal static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectRequiredConstructorParameters = true,
    };

    private static HeldData Load()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("MediaCompany.Pilot.held-data.json")
            ?? throw new InvalidOperationException("the held data resource is missing from the toolchain assembly");
        var held = JsonSerializer.Deserialize<HeldData>(stream, Json)
            ?? throw new InvalidOperationException("the held data resource is empty");
        if (held.FormVersion != KnownFormVersion)
        {
            throw new InvalidOperationException($"the held data form version {held.FormVersion} is not known to this toolchain");
        }

        return held;
    }
}
