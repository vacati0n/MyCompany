using MediaCompany.Domain.Accounting;
using MediaCompany.Domain.Capabilities;
using MediaCompany.Domain.Production;
using MediaCompany.Domain.Registry;
using MediaCompany.Domain.Work;

namespace MediaCompany.Application.Ports;

// ---------------------------------------------------------------------------
// The production change: the ports the produce and prepare paths read and write through
// ---------------------------------------------------------------------------

/// <summary>
/// The store's own designation and identity (the production change, decision D-005 of its design), read
/// without opening a transaction. Read members only.
/// </summary>
public interface IStoreDesignationReader
{
    /// <summary>The recorded designation, or null where none is recorded, with the connected database's name.</summary>
    Task<StoreIdentity> ReadAsync(CancellationToken cancellationToken);
}

/// <summary>What a store says it is, and what the connection says it is.</summary>
public sealed record StoreIdentity(StoreDesignation? Designation, string DatabaseName, DateTimeOffset? DesignatedAt);

/// <summary>
/// The reads the produce path takes before it calls anything (the production change, decisions D-010, D-015
/// and D-017 of its design). Read members only; every figure is the datastore's.
/// </summary>
public interface IProductionReader
{
    /// <summary>
    /// The item as the preparation recorded it: its title and channel, the outcomes its recorded package's version
    /// records, the highest version recorded, and its cap with the counted total, or null where the item is absent.
    /// </summary>
    Task<RecordedPackage?> PackageAsync(ItemId item, ItemVersion packageVersion, CancellationToken cancellationToken);

    /// <summary>
    /// What a plan is estimated with, at the datastore's instant: the admitted routes of a capability, the prices
    /// in force for their models, the billed kinds and the accounts.
    /// </summary>
    Task<PlanPricing> PricingAsync(CapabilityClass capability, CancellationToken cancellationToken);

    /// <summary>The stage outcomes recorded for one item version, in the closed stage order.</summary>
    Task<IReadOnlyList<ProducedStageOutcome>> StagesAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>The artifacts recorded for one item version, in recording order.</summary>
    Task<IReadOnlyList<ArtifactRecord>> ArtifactsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken);

    /// <summary>
    /// The latest recording registration of an item's package version, with every registered beat file, or null where none
    /// is recorded (the own-voice change, decision D-004 of its design). Default-implemented so every delivered double
    /// compiles unchanged; a realization without the tenth schema resource refuses by name.
    /// </summary>
    Task<RecordingRegistration?> LatestRegistrationAsync(ItemId item, ItemVersion packageVersion, CancellationToken cancellationToken) =>
        throw new NotSupportedException("This reader holds no recording registration; the tenth schema resource adds it.");

    /// <summary>The narration measurements recorded for one item version, in recording order (the own-voice change, decision D-007).</summary>
    Task<IReadOnlyList<NarrationMeasurement>> MeasurementsAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken) =>
        throw new NotSupportedException("This reader holds no narration measurement; the tenth schema resource adds it.");

    /// <summary>The narration provenance recorded for one item version, in recording order (the own-voice change, decision D-008).</summary>
    Task<IReadOnlyList<NarrationProvenance>> ProvenanceAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken) =>
        throw new NotSupportedException("This reader holds no narration provenance; the tenth schema resource adds it.");

    /// <summary>The narration source a production version recorded, or null where it was recorded before the tenth resource.</summary>
    Task<NarrationSource?> VersionSourceAsync(ItemId item, ItemVersion version, CancellationToken cancellationToken) =>
        throw new NotSupportedException("This reader holds no narration source; the tenth schema resource adds it.");
}

/// <summary>The item a production reads, as recorded.</summary>
public sealed record RecordedPackage
{
    public required ItemId Item { get; init; }
    public required ChannelId Channel { get; init; }
    public required string Title { get; init; }

    /// <summary>The version the recorded package holds its outcomes on.</summary>
    public required ItemVersion PackageVersion { get; init; }

    /// <summary>The package version's recorded stage outcomes.</summary>
    public required IReadOnlyList<ProducedStageOutcome> PackageStages { get; init; }

    /// <summary>The highest item version recorded, the package's or a production's.</summary>
    public required ItemVersion HighestVersion { get; init; }

    /// <summary>The item's cap reading, or null where no cap is recorded.</summary>
    public ItemCapReading? Cap { get; init; }

}

/// <summary>What a plan is estimated with, at one datastore instant.</summary>
public sealed record PlanPricing(
    DateTimeOffset At,
    IReadOnlyList<Route> Routes,
    IReadOnlyList<ModelPrice> Prices,
    IReadOnlyDictionary<ModelId, IReadOnlyList<PriceUnitKind>> BilledKinds,
    IReadOnlyList<ProviderAccount> Accounts);

/// <summary>
/// The preparation (the production change, decision D-016 of its design): loads recorded configuration and a
/// recorded package into a store, inserting where absent and refusing, by name, any differing row.
/// </summary>
public interface IPreparationWriter
{
    Task<PreparationOutcome> PrepareAsync(PreparationRecord record, StoreDesignation designation, CancellationToken cancellationToken);
}

/// <summary>What one preparation did.</summary>
public sealed record PreparationOutcome(int Inserted, int AlreadyPresent, IReadOnlyList<string> Refusals)
{
    public bool Refused => Refusals.Count > 0;
}

/// <summary>
/// Everything a preparation records, read from the recorded configuration file and the package folder. Every
/// amount here is a recorded configured amount with its source; none is an observation.
/// </summary>
public sealed record PreparationRecord
{
    public required CompanyId Company { get; init; }
    public required string CompanyName { get; init; }
    public required string CompanyOperatingState { get; init; }
    public required ChannelId Channel { get; init; }
    public required string ChannelPlatform { get; init; }
    public required string ChannelLanguage { get; init; }
    public required DepartmentId Department { get; init; }
    public required string DepartmentName { get; init; }
    public required string DepartmentBudgetHolder { get; init; }
    public required AgentId Agent { get; init; }
    public required string AgentName { get; init; }
    public required string AgentRole { get; init; }
    public required IReadOnlyList<PreparedAccount> Accounts { get; init; }
    public required IReadOnlyList<PreparedModel> Models { get; init; }
    public required IReadOnlyList<PreparedRoute> Routes { get; init; }
    public required IReadOnlyList<PreparedPrice> Prices { get; init; }
    public required ItemId Item { get; init; }
    public required string ItemTitle { get; init; }
    public required ItemVersion PackageVersion { get; init; }
    public required IReadOnlyList<PreparedStage> PackageStages { get; init; }
    public required Money Cap { get; init; }
    public required string CapSource { get; init; }
}

public sealed record PreparedAccount(
    ProviderAccountId Id, string Provider, string TermsBasis, DateOnly VerifiedOn, CredentialScope Scope, AuthenticationScheme Scheme);

public sealed record PreparedModel(
    ModelId Id, ProviderAccountId Account, QualityRating RatedQuality, ContextCapacity Context, string Modality,
    IReadOnlyList<PriceUnitKind> BilledKinds);

public sealed record PreparedRoute(
    RouteId Id, CapabilityClass Capability, RouteTier Tier, ProviderAccountId Account, ModelId Model,
    QualityRating RatedQuality, ContextCapacity Context, string TermsBasis, DateOnly TermsVerifiedOn,
    RouteTermsPositions Positions);

public sealed record PreparedPrice(
    ModelPriceId Id, ModelId Model, PriceUnitKind Kind, decimal UnitPrice, string Currency, string Source,
    DateOnly VerifiedOn, DateTimeOffset ValidFrom);

public sealed record PreparedStage(MediaCompany.Domain.Production.ProductionStage Stage, StageOutcome Outcome, string Summary);

// ---------------------------------------------------------------------------
// The two operating-system boundaries: the one process starter and the one writer
// ---------------------------------------------------------------------------

/// <summary>
/// The external media tool (the production change, decision D-013 of its design), realised by the ONE type that
/// starts a process. Every member runs one invocation under its own configured bound, with no transaction open,
/// reads its inputs from files under the staging folder and writes its output only into that folder; a member
/// that passes its bound ends by terminating the process tree under <see cref="MediaToolBoundExceededException"/>.
/// </summary>
public interface IMediaTool
{
    /// <summary>The first line the renderer prints for its version, recorded with each run.</summary>
    Task<string> VersionLineAsync(CancellationToken cancellationToken);

    /// <summary>Draws one still from a text file into a PNG, both named relative to the staging folder.</summary>
    Task DrawStillAsync(StillSpecification still, CancellationToken cancellationToken);

    /// <summary>
    /// Concatenates the audio files a list file names into one PCM file in the parts' COMMON sample format, named in the
    /// tool's own sample-format vocabulary, with no inserted silence and no filter: it neither resamples nor requantises
    /// (the own-voice change, decision D-007 of its design).
    /// </summary>
    Task ConcatenateAudioAsync(string stagingFolder, string listFile, string outputFile, string sampleFormat, CancellationToken cancellationToken);

    /// <summary>Renders a segment list over an audio file into one video file in the staging folder.</summary>
    Task RenderAsync(RenderSpecification render, CancellationToken cancellationToken);

    /// <summary>Probes a file's streams and duration.</summary>
    Task<MediaProbe> ProbeAsync(string file, CancellationToken cancellationToken);

    /// <summary>Decodes a file end to end to a null sink and counts the errors reported.</summary>
    Task<DecodeCheck> DecodeAsync(string file, CancellationToken cancellationToken);

    /// <summary>
    /// Measures a file's one audio stream from DECODED audio (the own-voice change, decision D-007 of its design): the
    /// container, codec, sample rate, channel count and sample format the decoder reports, the samples counted by decoding
    /// the whole stream, and the EBU R128 integrated loudness of the same decode pass, or why the meter stated none. The
    /// duration is decoded samples over sample rate; no container duration field is read. Runs under the decode bound.
    /// </summary>
    Task<AudioMeasurement> MeasureAudioAsync(string file, CancellationToken cancellationToken) =>
        throw new NotSupportedException("This media tool measures no decoded audio.");
}

/// <summary>
/// The in-house speech model (the own-voice change, decision D-005 of its design), realised by the ONE type that starts a
/// process. One generation starts the environment's interpreter located by setting, in isolated mode with bytecode writing
/// off, running the runtime as a module with an EXPLICIT argument list: the model file's and the configuration file's full
/// paths, the part's text file and output file (plain names in the staging folder) and all six generation settings; never a
/// voice name, a data directory, a device flag, a speaker number or raw output. The text reaches it only through the text
/// file. Its working directory is the staging folder; it runs under the model part bound on the monotonic clock with no
/// transaction held; past the bound its process tree is terminated under <see cref="MediaToolBoundExceededException"/>.
/// </summary>
public interface IVoiceModel
{
    /// <summary>Runs one generation and returns the exact argument list the process was started with, executable first.</summary>
    Task<IReadOnlyList<string>> GenerateAsync(VoiceGeneration generation, CancellationToken cancellationToken);
}

/// <summary>One generation: the staging folder, the text and output file names in it, the verified model files and the settings.</summary>
public sealed record VoiceGeneration(
    string StagingFolder,
    string TextFile,
    string OutputFile,
    string ModelFile,
    string ConfigurationFile,
    VoiceGenerationSettings Settings,
    TimeSpan Bound);

/// <summary>
/// The six generation settings, every one passed explicitly and recorded with each artifact (the owner's decision of
/// 2026-10-10: the voice's default settings, passed explicitly). Every value is configured; none has a default in code.
/// </summary>
public sealed record VoiceGenerationSettings(
    decimal LengthScale,
    decimal NoiseScale,
    decimal NoiseWidthScale,
    decimal SentenceSilenceSeconds,
    decimal Volume,
    bool Normalise)
{
    public string Describe() => string.Create(System.Globalization.CultureInfo.InvariantCulture,
        $"length scale {LengthScale}; noise scale {NoiseScale}; noise width scale {NoiseWidthScale}; sentence silence {SentenceSilenceSeconds} s; volume {Volume}; "
        + $"output normalisation {(Normalise ? "on (the runtime's per-sentence peak normalisation)" : "off")}");
}

/// <summary>One still: its text file and output file under the staging folder, and the profile it is drawn at.</summary>
public sealed record StillSpecification(string StagingFolder, string TextFile, string OutputFile, StillStyle Style);

/// <summary>The fixed looks a still is drawn in.</summary>
public enum StillStyle
{
    Graphic = 1,
    Thumbnail = 2,
    Placeholder = 3,
}

/// <summary>A render: the segment list file, the audio file and the output, all under the staging folder.</summary>
public sealed record RenderSpecification(string StagingFolder, string SegmentListFile, string AudioFile, string OutputFile, long TotalFrames);

/// <summary>What a probe measured from a file: its stream kinds in order and its duration.</summary>
public sealed record MediaProbe(IReadOnlyList<string> StreamKinds, TimeSpan Duration)
{
    public int Count(string kind) => StreamKinds.Count(k => string.Equals(k, kind, StringComparison.Ordinal));
}

/// <summary>What a full decode reported: the error lines counted, and the first of them where any.</summary>
public sealed record DecodeCheck(int Errors, string? FirstError);

/// <summary>A media tool invocation passed its configured bound and its process tree was terminated.</summary>
public sealed class MediaToolBoundExceededException : Exception
{
    public MediaToolBoundExceededException(string invocation, TimeSpan bound)
        : base($"the {invocation} invocation passed its configured bound of {bound} and its process tree was terminated")
    {
        Invocation = invocation;
        Bound = bound;
    }

    public string Invocation { get; }

    public TimeSpan Bound { get; }
}

/// <summary>A media tool invocation exited unsuccessfully.</summary>
public sealed class MediaToolFailedException : Exception
{
    public MediaToolFailedException(string invocation, int exitCode, string? firstErrorLine)
        : base($"the {invocation} invocation exited with code {exitCode}{(firstErrorLine is null ? string.Empty : ": " + firstErrorLine)}")
    {
        Invocation = invocation;
        ExitCode = exitCode;
    }

    public string Invocation { get; }

    public int ExitCode { get; }
}

/// <summary>
/// The artifact store (the production change, decision D-014 of its design), realised by the ONE type that
/// writes files, under one configured output root outside the repository. A file is written to staging, flushed
/// to disk, closed, moved into place and RE-READ to hash, and only then may it be recorded.
/// </summary>
public interface IArtifactStore
{
    /// <summary>The configured output root, as a full path.</summary>
    string OutputRoot { get; }

    /// <summary>
    /// Begins one production's folder under the output root and returns its staging folder, clearing any staging
    /// an interrupted earlier run left; a promoted file is never touched.
    /// </summary>
    string BeginRun(string runFolder);

    /// <summary>Writes a tool input (a text file, a list file) into the run's staging folder.</summary>
    Task WriteStagingTextAsync(string stagingFolder, string name, string text, CancellationToken cancellationToken);

    /// <summary>Copies a configured read-only input, such as the font file, into the run's staging folder.</summary>
    Task CopyIntoStagingAsync(string stagingFolder, string sourceFile, string name, CancellationToken cancellationToken);

    /// <summary>Stores bytes received from the boundary: staging, flush, close, promote, re-read and hash.</summary>
    Task<StoredFile> StoreAsync(string stagingFolder, ReadOnlyMemory<byte> bytes, string relativePath, CancellationToken cancellationToken);

    /// <summary>Promotes a file the tool wrote into staging after the tool exited, then re-reads and hashes it.</summary>
    Task<StoredFile> PromoteAsync(string stagingFolder, string stagingName, string relativePath, CancellationToken cancellationToken);

    /// <summary>The full path of a promoted file.</summary>
    string FullPath(string relativePath);
}

/// <summary>A promoted file: its relative path, and the length and hash recomputed from the stored bytes.</summary>
public sealed record StoredFile(string RelativePath, string FullPath, long Length, string Sha256);
