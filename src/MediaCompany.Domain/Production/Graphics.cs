namespace MediaCompany.Domain.Production;

/// <summary>Whether a graphic position holds still or moves (the graphics change, decision D-001 of its design).</summary>
public enum GraphicKind
{
    /// <summary>One frame: the specification's final state, independent of its span.</summary>
    Still = 1,

    /// <summary>Exactly its segment's frames, moving over the style file's motion-end fraction of them, then holding.</summary>
    Motion = 2,
}

/// <summary>
/// What a graphic's sameness statement rests on (the graphics change, decision D-006 of its design). A statement is made
/// only from bytes compared in the run that records it; nothing here is configured or assumed.
/// </summary>
public enum SamenessKind
{
    /// <summary>Rendered twice in separate processes in a demonstration store; every frame byte-identical.</summary>
    RepeatsTwoRenders = 1,

    /// <summary>Rendered twice in separate processes in a demonstration store; the renders differ, both digests recorded.</summary>
    DifferedTwoRenders = 2,

    /// <summary>A still rendered once in the company store, its bytes equal to the owner's approved still.</summary>
    EqualsApprovedStill = 3,

    /// <summary>A motion graphic rendered once in the company store, re-timed from that run's narration; not compared.</summary>
    RetimedRenderedOnce = 4,
}

/// <summary>
/// One graphic artifact's provenance (the graphics change, decision D-006 of its design), recorded on the artifact's own
/// short transaction. Every hash names the bytes the next reader took: the specification and style as staged, the drawing
/// program and render script as staged, the bundle as served, the installed files as held, the frames as promoted. Every
/// value is read from a verified file or observed in this run; none is copied from configuration.
/// </summary>
public sealed record GraphicProvenance
{
    public required ItemId Item { get; init; }

    public required ItemVersion Version { get; init; }

    /// <summary>The artifact this provenance belongs to: the still's PNG or the motion graphic's frame manifest.</summary>
    public required string RelativePath { get; init; }

    /// <summary>The graphic position the material names, as GFX-01.</summary>
    public required string Position { get; init; }

    public required GraphicKind Kind { get; init; }

    public required string SpecificationPath { get; init; }

    public required string SpecificationSha256 { get; init; }

    public required string StyleSha256 { get; init; }

    /// <summary>The digest over the staged drawing program's files, each named with its SHA-256.</summary>
    public required string DrawingProgramDigest { get; init; }

    public required string RenderScriptSha256 { get; init; }

    /// <summary>The digest over every bundle file, hashed after bundling and held while it was served.</summary>
    public required string BundleDigest { get; init; }

    public required string RendererName { get; init; }

    public required string RendererVersion { get; init; }

    /// <summary>Each manifest's SHA-256 and verified file count, as the verifier read them.</summary>
    public required string Manifests { get; init; }

    public required string NodeSha256 { get; init; }

    public required string NodeVersion { get; init; }

    public required string BrowserSha256 { get; init; }

    public required string BrowserVersion { get; init; }

    /// <summary>Each declared face's family, version and SHA-256, read from its own verified bytes.</summary>
    public required string Faces { get; init; }

    public required long SegmentStartFrame { get; init; }

    public required long SegmentFrames { get; init; }

    public required long FramesRendered { get; init; }

    /// <summary>The first and last frame that differ from the frame before them, or null for a still or a motion that never changes.</summary>
    public long? FirstChangingFrame { get; init; }

    public long? LastChangingFrame { get; init; }

    public required string FinalFrameSha256 { get; init; }

    /// <summary>The SHA-256 over the frame manifest's bytes (motion) or the still's own SHA-256 (still).</summary>
    public required string FrameSetDigest { get; init; }

    public required int Concurrency { get; init; }

    /// <summary>The exact argument list the renderer's process was started with, executable first.</summary>
    public required string Arguments { get; init; }

    public required string RequestSha256 { get; init; }

    /// <summary>Seconds observed with a monotonic stopwatch on this machine, for the renders recorded here.</summary>
    public required decimal RenderSeconds { get; init; }

    /// <summary>Processes still in the render's job after it exited, read before the job was closed.</summary>
    public required int LeftoverProcesses { get; init; }

    public required SamenessKind Sameness { get; init; }

    public required string SamenessStatement { get; init; }

    /// <summary>The datastore's instant; null on a draft.</summary>
    public DateTimeOffset? RecordedAt { get; init; }
}
