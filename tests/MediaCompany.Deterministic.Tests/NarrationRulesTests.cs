using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>
/// The own-voice change's pure rules (decisions D-001, D-002, D-003, D-004 and D-007 of its design): the narration source
/// rule over the FULL PRODUCT of its finite inputs, the vendor bar, the store guard's own mode, the recording set check and
/// the duration expectation. Nothing here reads a file, a store or a setting.
/// </summary>
public sealed class NarrationRulesTests
{
    private static readonly StoreDesignation?[] Designations = [null, StoreDesignation.Demonstration, StoreDesignation.Company];

    private static IEnumerable<RegistrationReading> Registrations()
    {
        yield return RegistrationReading.None;
        yield return new RegistrationReading(RegistrationState.ReleaseIncomplete, Guid.Parse("11111111-1111-1111-1111-111111111111"), ["release document reference", "model-training term"]);
        yield return new RegistrationReading(RegistrationState.Verified, Guid.Parse("22222222-2222-2222-2222-222222222222"), []);
        yield return new RegistrationReading(RegistrationState.FilesChanged, Guid.Parse("33333333-3333-3333-3333-333333333333"),
            ["beat 04's registered file changed after registration: sha256 aaaa registered and bbbb now"]);
    }

    private static IEnumerable<ModelReading> Models()
    {
        yield return ModelReading.NotConfigured;
        yield return new ModelReading(ModelState.Verified, []);
        yield return new ModelReading(ModelState.Refused, ["the voice model changed: sha256 cccc computed and dddd configured"]);
    }

    /// <summary>
    /// THE RULE NEVER YIELDS THE VENDOR, over the full product of every mode, every designation, every registration state
    /// and every model state, and it reads nothing else; two calls over equal inputs return equal selections.
    /// </summary>
    [Fact]
    public void TheRuleNeverYieldsTheVendorOverTheFullProductOfItsInputs()
    {
        var count = 0;
        foreach (var mode in Enum.GetValues<ProductionMode>())
        {
            foreach (var designation in Designations)
            {
                foreach (var registration in Registrations())
                {
                    foreach (var model in Models())
                    {
                        var selection = NarrationSourceRule.Select(mode, designation, registration, model);
                        var again = NarrationSourceRule.Select(mode, designation, registration, model);
                        Assert.NotEqual(NarrationSource.Vendor, selection.Source);
                        Assert.Equal(selection.Source, again.Source);
                        Assert.Equal(selection.Reason, again.Reason);
                        Assert.Equal(selection.PassedOver, again.PassedOver);
                        Assert.Equal(selection.Refusals, again.Refusals);
                        Assert.Equal(selection.Chosen, selection.Refusals.Count == 0);
                        count++;
                    }
                }
            }
        }

        Assert.Equal(Enum.GetValues<ProductionMode>().Length * 3 * 4 * 3, count);
    }

    /// <summary>Metered mode is refused on every store, naming the owner's decision of 2026-10-10 that the narration is the company's own.</summary>
    [Fact]
    public void MeteredModeIsRefusedNamingTheOwnersDecision()
    {
        foreach (var designation in Designations.Where(d => d is not null))
        {
            var selection = NarrationSourceRule.Select(ProductionMode.Metered, designation, RegistrationReading.None, new ModelReading(ModelState.Verified, []));
            Assert.Null(selection.Source);
            var refusal = Assert.Single(selection.Refusals);
            Assert.Contains("2026-10-10", refusal, StringComparison.Ordinal);
            Assert.Contains("narration is its own", refusal, StringComparison.Ordinal);
        }
    }

    /// <summary>The fake only in a demonstration store, whatever the registration or the model; the own mode never yields it.</summary>
    [Fact]
    public void TheFakeNarratesOnlyInADemonstrationStoreAndTheOwnModeNeverYieldsIt()
    {
        foreach (var registration in Registrations())
        {
            foreach (var model in Models())
            {
                Assert.Equal(NarrationSource.Fake, NarrationSourceRule.Select(ProductionMode.Fake, StoreDesignation.Demonstration, registration, model).Source);
                var company = NarrationSourceRule.Select(ProductionMode.Fake, StoreDesignation.Company, registration, model);
                Assert.Null(company.Source);
                Assert.Contains("demonstration store", Assert.Single(company.Refusals), StringComparison.Ordinal);
                foreach (var designation in new[] { StoreDesignation.Demonstration, StoreDesignation.Company })
                {
                    Assert.NotEqual(NarrationSource.Fake, NarrationSourceRule.Select(ProductionMode.Own, designation, registration, model).Source);
                    Assert.NotEqual(NarrationSource.Fake, NarrationSourceRule.Select(ProductionMode.PlanOnly, designation, registration, model).Source);
                }
            }
        }
    }

    /// <summary>
    /// The own mode's table: a verified recording narrates whatever the model's state; a released recording whose file
    /// changed refuses, naming the file and both hashes, and is never replaced by the model; an incomplete release is passed
    /// over naming each absent field; then the verified model narrates; otherwise the run is refused naming why.
    /// </summary>
    [Theory]
    [InlineData(RegistrationState.Verified, ModelState.NotConfigured, NarrationSource.Recording)]
    [InlineData(RegistrationState.Verified, ModelState.Verified, NarrationSource.Recording)]
    [InlineData(RegistrationState.Verified, ModelState.Refused, NarrationSource.Recording)]
    [InlineData(RegistrationState.FilesChanged, ModelState.Verified, null)]
    [InlineData(RegistrationState.FilesChanged, ModelState.NotConfigured, null)]
    [InlineData(RegistrationState.ReleaseIncomplete, ModelState.Verified, NarrationSource.InHouseModel)]
    [InlineData(RegistrationState.ReleaseIncomplete, ModelState.NotConfigured, null)]
    [InlineData(RegistrationState.ReleaseIncomplete, ModelState.Refused, null)]
    [InlineData(RegistrationState.Absent, ModelState.Verified, NarrationSource.InHouseModel)]
    [InlineData(RegistrationState.Absent, ModelState.NotConfigured, null)]
    [InlineData(RegistrationState.Absent, ModelState.Refused, null)]
    public void TheOwnModeFollowsItsTableAndNamesEveryReason(RegistrationState registrationState, ModelState modelState, NarrationSource? expected)
    {
        var registration = Registrations().Single(r => r.State == registrationState);
        var model = Models().Single(m => m.State == modelState);
        foreach (var mode in new[] { ProductionMode.Own, ProductionMode.PlanOnly })
        {
            foreach (var designation in new[] { StoreDesignation.Demonstration, StoreDesignation.Company })
            {
                var selection = NarrationSourceRule.Select(mode, designation, registration, model);
                Assert.Equal(expected, selection.Source);
                if (registrationState == RegistrationState.ReleaseIncomplete)
                {
                    var passed = Assert.Single(selection.PassedOver);
                    Assert.Contains("release document reference, model-training term", passed, StringComparison.Ordinal);
                }

                if (expected is null)
                {
                    Assert.NotEmpty(selection.Refusals);
                    var all = string.Join(" ", selection.Refusals);
                    switch (registrationState, modelState)
                    {
                        case (RegistrationState.FilesChanged, _):
                            Assert.Contains("sha256 aaaa registered and bbbb now", all, StringComparison.Ordinal);
                            break;
                        case (_, ModelState.Refused):
                            Assert.Contains("sha256 cccc computed and dddd configured", all, StringComparison.Ordinal);
                            break;
                        default:
                            Assert.Contains("no in-house model is configured", all, StringComparison.Ordinal);
                            break;
                    }
                }
            }
        }
    }

    /// <summary>The store guard admits the own mode on a demonstration store and on the company store matching the configured identity only.</summary>
    [Theory]
    [InlineData(StoreDesignation.Demonstration, "mediacompany_demo", "mediacompany", true)]
    [InlineData(StoreDesignation.Company, "mediacompany", "mediacompany", true)]
    [InlineData(StoreDesignation.Company, "another_store", "mediacompany", false)]
    [InlineData(StoreDesignation.Company, "mediacompany", null, false)]
    public void TheStoreGuardAdmitsTheOwnModeOnADemonstrationStoreAndTheMatchingCompanyStore(StoreDesignation designation, string database, string? identity, bool admitted)
    {
        var verdict = StoreGuard.Judge(ProductionMode.Own, designation, database, identity);
        Assert.Equal(admitted, verdict.Admitted);
        Assert.Equal(!admitted, verdict.Refusals.Count == 1);
        Assert.False(StoreGuard.Judge(ProductionMode.Own, null, database, identity).Admitted);
    }

    // -----------------------------------------------------------------------
    // The recording set check
    // -----------------------------------------------------------------------

    private static readonly int[] Beats = Enumerable.Range(1, 13).ToArray();

    [Fact]
    public void AFullSetOfOneFilePerBeatIsAcceptedWhateverItsExtension()
    {
        var names = Beats.Select(b => b % 2 == 0 ? $"{b:00}.wav" : $"{b:00}.FLAC").ToArray();
        var verdict = RecordingSetCheck.Check(names, Beats);
        Assert.False(verdict.Refused, string.Join("; ", verdict.Refusals));
        Assert.Equal(Beats, verdict.Accepted.Keys.Order());
        Assert.Equal("01.FLAC", verdict.Accepted[1]);
    }

    /// <summary>A missing, an extra, a duplicate, a whole-narration and any other file are each refused, ALL AT ONCE, naming each beat or file.</summary>
    [Fact]
    public void MissingExtraDuplicateWholeAndOtherFilesAreEachRefusedNamingThem()
    {
        var names = Beats.Where(b => b != 7).Select(b => $"{b:00}.wav")
            .Concat(["14.wav", "03.flac", "narration.wav", "notes.txt", "1.wav"])
            .ToArray();
        var verdict = RecordingSetCheck.Check(names, Beats);

        Assert.True(verdict.Refused);
        Assert.Empty(verdict.Accepted);
        var all = string.Join("\n", verdict.Refusals);
        Assert.Contains("beat 07 has no file", all, StringComparison.Ordinal);
        Assert.Contains("the file 14.wav names beat 14", all, StringComparison.Ordinal);
        Assert.Contains("beat 03 has 2 files (03.flac, 03.wav)", all, StringComparison.Ordinal);
        Assert.Contains("the file narration.wav is not named by a two-digit beat number", all, StringComparison.Ordinal);
        Assert.Contains("whole-narration file is not accepted", all, StringComparison.Ordinal);
        Assert.Contains("the file notes.txt", all, StringComparison.Ordinal);
        Assert.Contains("the file 1.wav", all, StringComparison.Ordinal);
        Assert.Equal(6, verdict.Refusals.Count);
    }

    /// <summary>A set of mixed formats is refused naming each beat's format; nothing is converted.</summary>
    [Fact]
    public void AMixedFormatSetIsRefusedNamingEachBeatsFormat()
    {
        AudioMeasurement Of(int rate, int channels, string format) => new()
        {
            Container = "wav", Codec = "pcm", SampleRate = rate, Channels = channels, SampleFormat = format, DecodedSamples = rate,
            LoudnessNotMeasurable = "fixture",
        };

        Assert.Empty(RecordingSetCheck.MixedFormats([(1, Of(48_000, 1, "s16")), (2, Of(48_000, 1, "s16"))]));
        var refusals = RecordingSetCheck.MixedFormats([(1, Of(48_000, 1, "s16")), (2, Of(44_100, 1, "s16")), (3, Of(48_000, 2, "s32"))]);
        Assert.Equal(4, refusals.Count);
        Assert.Contains("nothing is converted", refusals[0], StringComparison.Ordinal);
        Assert.Equal("beat 02 is 44100 Hz, 1 channel(s), s16", refusals[2]);
        Assert.Equal("beat 03 is 48000 Hz, 2 channel(s), s32", refusals[3]);
    }

    // -----------------------------------------------------------------------
    // The duration expectation: a reported reference, never a target
    // -----------------------------------------------------------------------

    private const string Script = "| Measure | Value |\n|---|---|\n| Delivery rate assumed | 150 words per minute, documentary register |\n| Narration duration at that rate | 12 min 51 s |\n";

    [Fact]
    public void TheScriptsOwnRateRowIsReadAndAScriptWithoutOneReadsNotStated()
    {
        var rate = DurationExpectation.ReadScript(Script, "script.md");
        Assert.Equal(150, rate.WordsPerMinute);
        Assert.Equal("12 min 51 s", rate.DurationQuote);
        Assert.Equal(771.6m, DurationExpectation.ExpectedSeconds(1_929, rate.WordsPerMinute));
        Assert.Equal(109.6m, DurationExpectation.ExpectedSeconds(274, rate.WordsPerMinute));

        var none = DurationExpectation.ReadScript("| Measure | Value |\n", "other.md");
        Assert.Null(none.WordsPerMinute);
        Assert.Null(DurationExpectation.ExpectedSeconds(274, none.WordsPerMinute));
        Assert.Contains("not stated", DurationExpectation.Report("beat 01", 100m, 274, null, none), StringComparison.Ordinal);
    }

    /// <summary>
    /// Audio at HALF and at DOUBLE the expectation is reported with its signed difference and never refused or judged: the
    /// report carries no pass, fail or tolerance.
    /// </summary>
    [Fact]
    public void HalfAndDoubleTheExpectationAreReportedWithTheSignedDifferenceAndNeverJudged()
    {
        var rate = DurationExpectation.ReadScript(Script, "script.md");
        var half = DurationExpectation.Report("the whole narration", 385.8m, 1_929, 771.6m, rate);
        var twice = DurationExpectation.Report("the whole narration", 1_543.2m, 1_929, 771.6m, rate);

        Assert.Contains("measured 385.800 s from decoded audio; expected 771.6 s (1,929 words at 150 words a minute", half, StringComparison.Ordinal);
        Assert.Contains("difference -385.800 s (measured minus expected; reported, never acted on)", half, StringComparison.Ordinal);
        Assert.Contains("difference +771.600 s", twice, StringComparison.Ordinal);
        foreach (var line in new[] { half, twice })
        {
            Assert.DoesNotContain("pass", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("fail", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("tolerance", line, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("target", line, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Words are the whitespace split, whatever the white space.</summary>
    [Fact]
    public void WordsAreTheWhitespaceSplit()
    {
        Assert.Equal(5, DurationExpectation.Words("  one two\nthree\t four\r\n\nfive "));
        Assert.Equal(0, DurationExpectation.Words(" \n "));
    }
}
