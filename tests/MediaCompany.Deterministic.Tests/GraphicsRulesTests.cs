using MediaCompany.Deterministic.Production;
using MediaCompany.Domain.Production;
using Xunit;

namespace MediaCompany.Deterministic.Tests;

/// <summary>The pure rules the graphics change adds beside the store guard and the timeline (decisions D-004 and D-007 of its design).</summary>
public sealed class GraphicsRulesTests
{
    /// <summary>The review is admitted on a demonstration store only; the company store and an undesignated store are refused by name.</summary>
    [Fact]
    public void TheReviewIsAdmittedOnADemonstrationStoreOnly()
    {
        Assert.True(StoreGuard.JudgeReview(StoreDesignation.Demonstration, "mediacompany_demo").Admitted);

        var company = StoreGuard.JudgeReview(StoreDesignation.Company, "mediacompany");
        Assert.False(company.Admitted);
        Assert.Equal("review-graphics runs only against a store designated demonstration, and mediacompany is designated Company", Assert.Single(company.Refusals));

        var undesignated = StoreGuard.JudgeReview(null, "a-store");
        Assert.False(undesignated.Admitted);
        Assert.Contains("is designated nothing", Assert.Single(undesignated.Refusals), StringComparison.Ordinal);
    }

    /// <summary>
    /// The timeline the assembly reuses is the delivered rule: equal measured beat durations give identical segment boundaries,
    /// whatever the visuals' paths are (a still or a frame manifest at a graphic position).
    /// </summary>
    [Fact]
    public void TheTimelineGivesIdenticalBoundariesForEqualBeatDurationsWhateverTheGraphicsPaths()
    {
        (int, TimeSpan)[] beats = [(1, TimeSpan.FromSeconds(6.1333)), (2, TimeSpan.FromSeconds(31.6))];
        (string, int, string)[] stills = [("GFX-01", 1, "design/gfx-01.png"), ("CLIP-01", 1, "production/clip-01-placeholder.png"), ("GFX-02", 2, "design/gfx-02.png")];
        (string, int, string)[] mixed = [("GFX-01", 1, "design/gfx-01.png"), ("CLIP-01", 1, "production/clip-01-placeholder.png"), ("GFX-02", 2, "design/gfx-02-frames.txt")];

        var a = BeatTimeline.Compose(beats, stills, 30);
        var b = BeatTimeline.Compose(beats, mixed, 30);
        Assert.Equal(a.Select(s => (s.Implements, s.StartFrame, s.Frames)), b.Select(s => (s.Implements, s.StartFrame, s.Frames)));
        Assert.Equal(BeatTimeline.Frame(TimeSpan.FromSeconds(6.1333 + 31.6), 30), b.Sum(s => s.Frames));
    }

    /// <summary>The figure tokens of a text: digits and the listed number words, nothing else.</summary>
    [Fact]
    public void TheNumberRuleFindsDigitsAndNumberWordsOnly()
    {
        Assert.Equal(new[] { "13", "15" }, GraphicSpecificationCheck.FigureTokens("13–15 Hz"));
        Assert.Equal(new[] { "nine", "o'clock" }, GraphicSpecificationCheck.FigureTokens("nine o'clock"));
        Assert.Equal(new[] { "noon" }, GraphicSpecificationCheck.FigureTokens("noon"));
        Assert.Empty(GraphicSpecificationCheck.FigureTokens("a region, not a point"));
        Assert.Equal(new[] { "eight" }, GraphicSpecificationCheck.FigureTokens("a rough figure of eight"));
    }
}
