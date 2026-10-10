using System.Globalization;
using MediaCompany.Domain.Work;

namespace MediaCompany.Domain.Production;

/// <summary>
/// Where a production's narration comes from (the own-voice change, decision D-001 of its design). A CLOSED set of four:
/// exactly one narrates a whole production, chosen by the narration source rule, and the tenth schema resource checks the
/// same four. No other value is constructible or printable; an architecture assertion holds the count at four.
/// </summary>
public enum NarrationSource
{
    /// <summary>A human recording the company holds, registered one file per beat with a recorded release.</summary>
    Recording = 1,

    /// <summary>The owner-approved open-licensed speech model, run on the company's own machine after its files verify.</summary>
    InHouseModel = 2,

    /// <summary>The demonstration composition's fake provider; a demonstration store only.</summary>
    Fake = 3,

    /// <summary>
    /// The speech vendor through the capability boundary. It stays in the build and is NEVER chosen by the rule while the
    /// owner's decision of 2026-10-10 (the company's narration is its own) stands; re-admitting it needs a new recorded
    /// owner decision and a code change.
    /// </summary>
    Vendor = 4,
}

/// <summary>
/// The release a performer gave for a recording (the own-voice change, decision D-004 of its design). Every field is
/// recorded as given, or ABSENT when it was not given: nothing here is ever filled in, so no rights position is invented.
/// </summary>
public sealed record ReleaseRecord
{
    /// <summary>The release document's reference, as the operator gave it.</summary>
    public string? DocumentReference { get; init; }

    /// <summary>The date the release document carries.</summary>
    public DateOnly? DocumentDate { get; init; }

    /// <summary>The performer's name, as the operator gave it; the registration and its release record the same name.</summary>
    public string? PerformerName { get; init; }

    /// <summary>The SHA-256 of the release document, read from outside the repository; the document is copied nowhere.</summary>
    public string? DocumentSha256 { get; init; }

    /// <summary>The model-training term of the release, as given (for example that model training is not permitted).</summary>
    public string? TrainingTerm { get; init; }

    /// <summary>The names of the release fields that were not given, in a fixed order; empty when the release is complete.</summary>
    public IReadOnlyList<string> AbsentFields =>
        new (string Name, bool Absent)[]
        {
            ("release document reference", string.IsNullOrWhiteSpace(DocumentReference)),
            ("release document date", DocumentDate is null),
            ("performer's name", string.IsNullOrWhiteSpace(PerformerName)),
            ("release document SHA-256", string.IsNullOrWhiteSpace(DocumentSha256)),
            ("model-training term", string.IsNullOrWhiteSpace(TrainingTerm)),
        }.Where(f => f.Absent).Select(f => f.Name).ToArray();

    /// <summary>Whether every release field was given.</summary>
    public bool Complete => AbsentFields.Count == 0;

    /// <summary>The release as recorded, every absent field printed ABSENT.</summary>
    public string Describe() =>
        $"release document reference {Shown(DocumentReference)}; date {DocumentDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "ABSENT"}; "
        + $"performer's name {Shown(PerformerName)}; document SHA-256 {Shown(DocumentSha256)}; model-training term {Shown(TrainingTerm)}";

    private static string Shown(string? value) => string.IsNullOrWhiteSpace(value) ? "ABSENT" : value;
}

/// <summary>
/// What one decode of an audio stream measured (the own-voice change, decision D-007 of its design). The DURATION is the
/// decoded sample count over the decoded sample rate, never a container's header; the LOUDNESS is the media tool's EBU R128
/// integrated loudness, or not measurable with the tool's reason, never zero. Nothing here is ever acted on.
/// </summary>
public sealed record AudioMeasurement
{
    /// <summary>The basis every narration duration is recorded with.</summary>
    public const string DecodedBasis = "decoded sample count over sample rate";

    /// <summary>The loudness measure, named.</summary>
    public const string LoudnessMeasure = "EBU R128 integrated loudness (ITU-R BS.1770 gating), the media tool's R128 meter";

    public const string LoudnessUnit = "LUFS";

    /// <summary>The container the probe named.</summary>
    public required string Container { get; init; }

    /// <summary>The audio codec the probe named.</summary>
    public required string Codec { get; init; }

    /// <summary>The decoder's sample rate.</summary>
    public required int SampleRate { get; init; }

    public required int Channels { get; init; }

    /// <summary>The decoder's sample format.</summary>
    public required string SampleFormat { get; init; }

    /// <summary>Samples per channel, counted by decoding the whole stream.</summary>
    public required long DecodedSamples { get; init; }

    /// <summary>The integrated loudness in LUFS, or null where the meter could not state one.</summary>
    public decimal? IntegratedLoudness { get; init; }

    /// <summary>Why no loudness is recorded, where none is; null where one is.</summary>
    public string? LoudnessNotMeasurable { get; init; }

    /// <summary>Each line the decode reported naming an error or corrupt input; empty for a clean decode. Not stored.</summary>
    public IReadOnlyList<string> DecodeFindings { get; init; } = [];

    /// <summary>Decoded samples over the decoded sample rate, in seconds, exact to the sample.</summary>
    public decimal DurationSeconds => SampleRate > 0 ? (decimal)DecodedSamples / SampleRate : 0m;

    /// <summary>The decoded duration as a time span, to the tick.</summary>
    public TimeSpan Duration => TimeSpan.FromTicks((long)Math.Round(DurationSeconds * TimeSpan.TicksPerSecond));

    /// <summary>The format an exact join needs shared: rate, channels and sample format.</summary>
    public string Format => string.Create(CultureInfo.InvariantCulture, $"{SampleRate} Hz, {Channels} channel(s), {SampleFormat}");

    public string DescribeLoudness() => IntegratedLoudness is { } lufs
        ? string.Create(CultureInfo.InvariantCulture, $"{lufs:0.0} {LoudnessUnit} MEASURED ({LoudnessMeasure})")
        : $"loudness NOT MEASURABLE ({LoudnessNotMeasurable})";

    public string DescribeDuration() =>
        string.Create(CultureInfo.InvariantCulture, $"{DurationSeconds:0.000} s MEASURED ({DecodedSamples:N0} decoded samples at {SampleRate} Hz; {DecodedBasis})");
}

/// <summary>One registered beat file: its stored copy under the output root, the SHA-256 of those stored bytes and what a decode of them measured.</summary>
public sealed record RegisteredBeatFile
{
    public required int Beat { get; init; }

    /// <summary>The stored copy's path relative to the configured output root, with forward slashes.</summary>
    public required string StoredPath { get; init; }

    public required long Length { get; init; }

    public required string Sha256 { get; init; }

    public required AudioMeasurement Measured { get; init; }
}

/// <summary>
/// One recording registration (the own-voice change, decision D-004 of its design): one stored copy per beat of one item's
/// package version, the performer and the release, recorded once on the datastore's instant.
/// </summary>
public sealed record RecordingRegistration
{
    public required Guid Id { get; init; }

    public required ItemId Item { get; init; }

    public required ItemVersion PackageVersion { get; init; }

    public required ReleaseRecord Release { get; init; }

    public required IReadOnlyList<RegisteredBeatFile> Beats { get; init; }

    /// <summary>The datastore's instant; null on a draft.</summary>
    public DateTimeOffset? RecordedAt { get; init; }
}

/// <summary>
/// One narration file's measurement as recorded (the own-voice change, decision D-007 of its design): the decoded
/// measurement, and for a beat the words counted in its text and the duration expected at the script's own rate. The
/// expectation is a reported reference, never a target: nothing is passed, failed or adjusted against it.
/// </summary>
public sealed record NarrationMeasurement
{
    public required ItemId Item { get; init; }

    public required ItemVersion Version { get; init; }

    /// <summary>The narration artifact's path relative to the output root.</summary>
    public required string RelativePath { get; init; }

    public required AudioMeasurement Measured { get; init; }

    /// <summary>The beat the file narrates, or null for a part of no single beat and for the joined narration.</summary>
    public int? Beat { get; init; }

    /// <summary>The whitespace-split words of the text the file narrates.</summary>
    public int? Words { get; init; }

    /// <summary>Words times 60 over the script's assumed rate, to one decimal, or null where the script states no rate.</summary>
    public decimal? ExpectedSeconds { get; init; }

    /// <summary>Where the expectation came from, or why none is stated.</summary>
    public required string ExpectationBasis { get; init; }

    public DateTimeOffset? RecordedAt { get; init; }
}

/// <summary>
/// One own-source narration file's provenance (the own-voice change, decision D-008 of its design). For the in-house model
/// every value is read from the files that were hashed and loaded in that run, or from the run itself; a value taken from
/// configuration says so. For a recording: the registration, the beat, the performer and the release.
/// </summary>
public sealed record NarrationProvenance
{
    public required ItemId Item { get; init; }

    public required ItemVersion Version { get; init; }

    public required string RelativePath { get; init; }

    public required NarrationSource Source { get; init; }

    public string? ModelName { get; init; }

    public string? ModelVersion { get; init; }

    public string? CodeLicence { get; init; }

    public string? VoiceName { get; init; }

    public string? DatasetLicence { get; init; }

    public string? WeightsLicence { get; init; }

    public string? VoiceLicence { get; init; }

    public string? GenerationSettings { get; init; }

    /// <summary>Every hashed file with its SHA-256 as computed through the held handle in this run.</summary>
    public string? FileHashes { get; init; }

    public int? RecordEntriesVerified { get; init; }

    /// <summary>The exact argument list the process was started with.</summary>
    public string? Arguments { get; init; }

    /// <summary>"repeats", "not observed to repeat" with both hashes, or "not observed to repeat: generated once".</summary>
    public string? Repeatability { get; init; }

    public Guid? Registration { get; init; }

    public int? Beat { get; init; }

    public string? PerformerName { get; init; }

    public string? Release { get; init; }

    public DateTimeOffset? RecordedAt { get; init; }
}
