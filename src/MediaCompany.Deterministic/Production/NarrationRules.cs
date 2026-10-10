using System.Globalization;
using System.Text.RegularExpressions;
using MediaCompany.Domain.Production;

namespace MediaCompany.Deterministic.Production;

/// <summary>The state of the latest recording registration of an item's package version, as the plan or the run read it.</summary>
public enum RegistrationState
{
    /// <summary>No registration is recorded for the item's package version.</summary>
    Absent = 1,

    /// <summary>A registration is recorded and at least one release field is absent; it is passed over, never used.</summary>
    ReleaseIncomplete = 2,

    /// <summary>A registration with a complete release whose every stored copy re-hashed to its registered SHA-256.</summary>
    Verified = 3,

    /// <summary>A registration with a complete release whose stored copy is missing or no longer hashes to its registered SHA-256.</summary>
    FilesChanged = 4,
}

/// <summary>A registration reading: its state, its identifier where one is recorded, and every finding named.</summary>
public sealed record RegistrationReading(RegistrationState State, Guid? Registration, IReadOnlyList<string> Findings)
{
    public static RegistrationReading None { get; } = new(RegistrationState.Absent, null, []);
}

/// <summary>The in-house model's verification state, as the plan or the run computed it from the installed files.</summary>
public enum ModelState
{
    /// <summary>No in-house model section is configured.</summary>
    NotConfigured = 1,

    /// <summary>Every configured file exists outside the repository and hashes to its expected value.</summary>
    Verified = 2,

    /// <summary>At least one finding refuses the model; each is named.</summary>
    Refused = 3,
}

/// <summary>A model reading: its state and every finding named.</summary>
public sealed record ModelReading(ModelState State, IReadOnlyList<string> Findings)
{
    public static ModelReading NotConfigured { get; } = new(ModelState.NotConfigured, []);
}

/// <summary>What the rule chose, or why it refused, and every source it passed over with the reason.</summary>
public sealed record NarrationSelection(NarrationSource? Source, string Reason, IReadOnlyList<string> PassedOver, IReadOnlyList<string> Refusals)
{
    public bool Chosen => Source is not null;
}

/// <summary>
/// THE NARRATION SOURCE RULE (the own-voice change, decisions D-001 and D-002 of its design), a pure function over the mode,
/// the store's designation, the latest registration's reading and the model's verification reading, and nothing else: two
/// calls over equal inputs return equal selections. It reads NO setting, so no settings value can re-admit the vendor.
///
/// Order: fake mode yields the fake on a demonstration store only; metered mode is refused naming the owner's decision of
/// 2026-10-10; the own mode and plan-only yield the recording when its release is complete and every stored copy verified,
/// refuse when a released registration's file changed or is missing, pass an incomplete release over naming each absent
/// field, then yield the in-house model when it is verified, and otherwise refuse naming why. NO BRANCH YIELDS THE VENDOR.
/// </summary>
public static class NarrationSourceRule
{
    /// <summary>The refusal every request for the vendor narration route meets while the owner's decision stands.</summary>
    public const string VendorBar =
        "the owner's decision of 2026-10-10 is that the company's narration is its own (a recording it holds, or the in-house model run on its own machine); "
        + "the speech vendor's narration route is not attempted again, so metered narration is refused; re-admitting it needs a new recorded owner decision and a code change";

    public static NarrationSelection Select(ProductionMode mode, StoreDesignation? designation, RegistrationReading registration, ModelReading model)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentNullException.ThrowIfNull(model);

        if (designation is null)
        {
            return Refused("the store records no designation, so no narration source is admitted");
        }

        switch (mode)
        {
            case ProductionMode.Fake:
                return designation == StoreDesignation.Demonstration
                    ? new NarrationSelection(NarrationSource.Fake, "fake mode in a store designated demonstration: the demonstration composition's fake provider", [], [])
                    : Refused($"the fake source narrates only in a demonstration store, and this store is designated {designation}");

            case ProductionMode.Metered:
                return Refused(VendorBar);

            case ProductionMode.Own:
            case ProductionMode.PlanOnly:
                break;

            default:
                return Refused($"no narration source is defined for the mode {mode}");
        }

        var passedOver = new List<string>();
        switch (registration.State)
        {
            case RegistrationState.Verified:
                return new NarrationSelection(NarrationSource.Recording,
                    $"registration {registration.Registration} has a complete release and every stored beat file re-hashed to its registered SHA-256",
                    passedOver, []);

            case RegistrationState.FilesChanged:
                return Refused([$"registration {registration.Registration} has a complete release, and its stored files no longer match what was registered; "
                    + "the recording is not replaced by another source", .. registration.Findings]);

            case RegistrationState.ReleaseIncomplete:
                passedOver.Add($"recording passed over: registration {registration.Registration} has no complete release; absent: "
                    + string.Join(", ", registration.Findings));
                break;

            default:
                passedOver.Add("recording passed over: no recording is registered for the item's package version");
                break;
        }

        return model.State switch
        {
            ModelState.Verified => new NarrationSelection(NarrationSource.InHouseModel,
                "no usable recording, and every configured in-house model file is outside the repository and hashed to its expected SHA-256", passedOver, []),
            ModelState.NotConfigured => new NarrationSelection(null, "no source", passedOver,
                ["no usable recording, and no in-house model is configured (the settings' inHouseModel section)"]),
            _ => new NarrationSelection(null, "no source", passedOver,
                ["no usable recording, and the in-house model is refused", .. model.Findings]),
        };
    }

    private static NarrationSelection Refused(params string[] refusals) => new(null, "no source", [], refusals);
}

/// <summary>
/// THE RECORDING SET CHECK (the own-voice change, decision D-004 of its design), a pure function over a folder's file names
/// and the material's beat numbers: exactly one file per beat, named by its two-digit beat number with any extension. It
/// refuses, naming each beat or file, a missing beat, an extra beat number, a duplicate beat, a file not named by a beat
/// (a whole-narration file among them) and any other file. Every finding is named at once.
/// </summary>
public static partial class RecordingSetCheck
{
    public sealed record Verdict(IReadOnlyDictionary<int, string> Accepted, IReadOnlyList<string> Refusals)
    {
        public bool Refused => Refusals.Count > 0;
    }

    public static Verdict Check(IReadOnlyList<string> fileNames, IReadOnlyList<int> beats)
    {
        ArgumentNullException.ThrowIfNull(fileNames);
        ArgumentNullException.ThrowIfNull(beats);

        var refusals = new List<string>();
        var byBeat = new Dictionary<int, List<string>>();
        foreach (var name in fileNames.Order(StringComparer.Ordinal))
        {
            var match = BeatName().Match(name);
            if (!match.Success)
            {
                refusals.Add($"the file {name} is not named by a two-digit beat number; one file per beat is registered, and a whole-narration file is not accepted");
                continue;
            }

            var beat = int.Parse(match.Groups["beat"].Value, CultureInfo.InvariantCulture);
            if (!beats.Contains(beat))
            {
                refusals.Add($"the file {name} names beat {beat:00}, and the item material has no beat {beat}");
                continue;
            }

            (byBeat.TryGetValue(beat, out var files) ? files : byBeat[beat] = []).Add(name);
        }

        foreach (var beat in beats.Order())
        {
            if (!byBeat.TryGetValue(beat, out var files))
            {
                refusals.Add($"beat {beat:00} has no file");
            }
            else if (files.Count > 1)
            {
                refusals.Add($"beat {beat:00} has {files.Count} files ({string.Join(", ", files)}); exactly one file per beat is registered");
            }
        }

        var accepted = refusals.Count == 0 ? byBeat.ToDictionary(b => b.Key, b => b.Value[0]) : new Dictionary<int, string>();
        return new Verdict(accepted, refusals);
    }

    /// <summary>
    /// A set whose measured formats differ is refused before narration, naming each beat's format (the Design Gate's ruling):
    /// no format target exists, so nothing is converted, and an exact join needs one rate, one channel count and one sample format.
    /// </summary>
    public static IReadOnlyList<string> MixedFormats(IReadOnlyList<(int Beat, AudioMeasurement Measured)> files)
    {
        ArgumentNullException.ThrowIfNull(files);
        if (files.Select(f => f.Measured.Format).Distinct(StringComparer.Ordinal).Count() <= 1)
        {
            return [];
        }

        return files.OrderBy(f => f.Beat)
            .Select(f => $"beat {f.Beat:00} is {f.Measured.Format}")
            .Prepend("the beat files do not share one sample rate, channel count and sample format, so they cannot be joined exactly; nothing is converted (no format target is set); record every beat with the same settings")
            .ToArray();
    }

    [GeneratedRegex(@"^(?<beat>\d{2})(\.[^.\\/]+)?$", RegexOptions.CultureInvariant)]
    private static partial Regex BeatName();
}

/// <summary>The script's own statement of the delivery rate it assumed, quoted, or not stated.</summary>
public sealed record ScriptRate(int? WordsPerMinute, string Source, string? RateQuote, string? DurationQuote)
{
    public string Describe() => WordsPerMinute is { } rate
        ? $"{rate} words a minute, the delivery rate the script {Source} records (\"{RateQuote}\")"
        : $"not stated: the script {Source} records no delivery rate";
}

/// <summary>
/// THE DURATION EXPECTATION (the own-voice change, decision D-007 of its design), a pure function. It is a REPORTED REFERENCE,
/// never a target: words are the whitespace split of a beat's narration text, expected seconds are words times 60 over the
/// script's own assumed rate to one decimal, and the report states measured, expected and the signed difference with no pass,
/// fail or tolerance. A script with no rate row reads not stated. No rate is written into production source or the material.
/// </summary>
public static partial class DurationExpectation
{
    /// <summary>Reads the script's rows labelled for the assumed delivery rate and the narration duration at that rate.</summary>
    public static ScriptRate ReadScript(string scriptText, string source)
    {
        ArgumentNullException.ThrowIfNull(scriptText);
        string? Row(Regex label) => scriptText.Split('\n').Select(l => label.Match(l)).FirstOrDefault(m => m.Success)?.Groups["value"].Value.Trim();

        var rate = Row(RateRow());
        var duration = Row(DurationRow());
        var words = rate is not null && LeadingRate().Match(rate) is { Success: true } m
            ? int.Parse(m.Groups["rate"].Value, CultureInfo.InvariantCulture)
            : (int?)null;
        return new ScriptRate(words, source, rate, duration);
    }

    /// <summary>The whitespace-split words of a text.</summary>
    public static int Words(string text) =>
        (text ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    /// <summary>Words times 60 over the rate, to one decimal; null where no rate is stated.</summary>
    public static decimal? ExpectedSeconds(int words, int? wordsPerMinute) =>
        wordsPerMinute is { } rate and > 0 ? Math.Round(words * 60m / rate, 1, MidpointRounding.AwayFromZero) : null;

    /// <summary>One line of the duration report: measured, expected and the signed difference, never a verdict.</summary>
    public static string Report(string what, decimal measuredSeconds, int words, decimal? expectedSeconds, ScriptRate rate)
    {
        ArgumentNullException.ThrowIfNull(rate);
        var c = CultureInfo.InvariantCulture;
        var measured = string.Create(c, $"{what}: measured {measuredSeconds:0.000} s from decoded audio");
        return expectedSeconds is { } expected
            ? measured + string.Create(c, $"; expected {expected:0.0} s ({words:N0} words at {rate.WordsPerMinute} words a minute, the script's assumed rate); ")
                + string.Create(c, $"difference {measuredSeconds - expected:+0.000;-0.000;0.000} s (measured minus expected; reported, never acted on)")
            : measured + $"; expected duration not stated ({rate.Describe()})";
    }

    [GeneratedRegex(@"^\|\s*Delivery rate assumed\s*\|(?<value>[^|]*)\|", RegexOptions.CultureInvariant)]
    private static partial Regex RateRow();

    [GeneratedRegex(@"^\|\s*Narration duration at that rate\s*\|(?<value>[^|]*)\|", RegexOptions.CultureInvariant)]
    private static partial Regex DurationRow();

    [GeneratedRegex(@"^(?<rate>\d+)\s+words per minute", RegexOptions.CultureInvariant)]
    private static partial Regex LeadingRate();
}
