using Valora.Application.Access;
using Valora.Application.OrganizationalIntelligence;

namespace Valora.Tests;

public sealed class IntelligentDeliverablesEngineTests
{
    private static readonly Guid Organization = Guid.NewGuid();
    private readonly ValoraIntelligenceEngine _engine = new();

    [Fact]
    public void Analyze_CalculatesWeightedOverallAndDimensionScores()
    {
        var result = _engine.Analyze(Organization, null,
        [
            E("Governança Organizacional", 20, 1), E("Governança Organizacional", 80, 3),
            E("Pessoas", 50, 2)
        ], DateTime.UnixEpoch);

        Assert.Equal(57.5m, result.Score);
        Assert.Equal(65m, result.Dimensions.Single(x => x.Dimension == "Governança Organizacional").Score);
        Assert.Equal(50m, result.Dimensions.Single(x => x.Dimension == "Pessoas").Score);
    }

    [Theory]
    [InlineData(3, "low")]
    [InlineData(4, "medium")]
    [InlineData(6, "medium")]
    [InlineData(7, "high")]
    public void Analyze_UsesOfficialConfidenceThresholds(int count, string expected)
    {
        var result = _engine.Analyze(Organization, null, Enumerable.Range(0, count).Select(_ => E("Liderança", 30, 1)));
        Assert.Equal(expected, result.ConfidenceLevel);
        Assert.All(result.Insights, insight => Assert.NotEmpty(insight.Evidence));
    }

    [Fact]
    public void Analyze_DoesNotCreateRecommendationWithoutEvidence()
    {
        var result = _engine.Analyze(Organization, null, []);
        Assert.Null(result.Score);
        Assert.Empty(result.Insights); Assert.Empty(result.Actions); Assert.Empty(result.Priorities);
        Assert.All(result.Radar, item => Assert.Null(item.Score));
    }

    [Fact]
    public void Analyze_BuildsEveryMandatoryReportSectionAndOfficialRadarDimension()
    {
        var result = _engine.Analyze(Organization, null, [E("Cultura Organizacional", 30, 1)]);
        Assert.Equal(15, result.Report.Sections.Count);
        Assert.Equal(ValoraOfficialDimensions.All, result.Radar.Select(x => x.Dimension));
        Assert.All(result.Actions, action => Assert.NotEmpty(action.Evidence));
    }

    [Fact]
    public void InvalidScoresAndWeightsNeverParticipateInCalculation()
    {
        var result = _engine.Analyze(Organization, null,
            [E("Sistemas", 60, 1), E("Sistemas", 200, 1), E("Sistemas", 10, 0)]);
        Assert.Equal(60m, result.Score);
        Assert.Single(result.Evidence);
    }

    [Fact]
    public void CanonicalCatalogContainsEveryDeliverablePermission()
    {
        string[] permissions = ["dashboard.read", "radar.read", "reports.read", "reports.generate", "action.read",
            "action.manage", "heatmap.read", "evolution.read", "journey.read", "benchmark.read", "insights.read"];
        Assert.All(permissions, permission => Assert.True(ValoraAccessCatalog.IsCanonicalPermission(permission)));
        Assert.All(ValoraAccessCatalog.CapabilitiesForStrict(permissions), capability => Assert.Equal("organizational_intelligence", capability));
    }

    private static DiagnosisEvidence E(string dimension, decimal score, decimal weight) =>
        new(Guid.NewGuid(), Organization, null, dimension, "mapped", score, weight, "Evidência observada e rastreável.", DateTime.UtcNow);
}
