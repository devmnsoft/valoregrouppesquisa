using Microsoft.Extensions.Logging.Abstractions;
using Valora.Application.Results;
using Xunit;

namespace Valora.Tests;

[Trait("Category", "Unit")]
public sealed class ResultLifecycleServicesTests
{
    [Fact]
    public void LowSampleProducesInsufficientResultWithoutInventingScore()
    {
        var result = CreateService().Calculate(Answers(), 2, 3, true, "valora-2.0", "test-correlation",
            DateTimeOffset.Parse("2026-09-03T12:00:00Z"));

        Assert.Equal(ResultLifecycleStatus.Insufficient, result.Status);
        Assert.Null(result.OverallScore);
        Assert.Empty(result.Recommendations);
        Assert.True(result.Evidence.IsAnonymous);
        Assert.Equal("Ainda não há respostas suficientes para calcular este resultado.", result.Limitation);
    }

    [Fact]
    public void CalculationIsDeterministicAndKeepsTraceableSnapshot()
    {
        var service = CreateService();
        var at = DateTimeOffset.Parse("2026-09-03T12:00:00Z");
        var first = service.Calculate(Answers(), 3, 3, false, "valora-2.0", "c-1", at);
        var second = service.Calculate(Answers().Reverse(), 3, 3, false, "valora-2.0", "c-2", at);

        Assert.Equal(50m, first.OverallScore);
        Assert.Equal(first.OverallScore, second.OverallScore);
        Assert.Equal(first.Snapshot.Sha256, second.Snapshot.Sha256);
        Assert.Equal("valora-2.0", first.Snapshot.RuleVersion);
        Assert.NotEmpty(first.Evidence.Items);
        Assert.All(first.Recommendations, recommendation => Assert.NotEmpty(recommendation.EvidenceCodes));
    }

    [Fact]
    public void PublishedResultRequiresANewVersionToRecalculate()
    {
        var publication = new ResultPublicationService();
        var calculated = CreateService().Calculate(Answers(), 3, 3, false, "valora-2.0", "c-1");
        var published = publication.Publish(calculated);

        Assert.Equal(ResultLifecycleStatus.Published, published.Status);
        var error = Assert.Throws<InvalidOperationException>(() => publication.EnsureCanRecalculate(published, false));
        Assert.Equal("Este resultado publicado preserva rastreabilidade e não pode ser alterado diretamente.", error.Message);
        publication.EnsureCanRecalculate(published, true);
    }

    [Fact]
    public void InsufficientResultCannotBePublished()
    {
        var result = CreateService().Calculate(Answers(), 0, 3, true, "valora-2.0", "c-1");
        Assert.Throws<InvalidOperationException>(() => new ResultPublicationService().Publish(result));
    }

    private static ResultCalculationService CreateService() => new(
        new EvidenceAggregationService(), new ValoraIndexScoreService(), new ResultSnapshotService(),
        new ResultRecommendationService(), NullLogger<ResultCalculationService>.Instance);

    private static MethodologicalAnswer[] Answers() =>
    [
        new(Guid.Parse("00000000-0000-0000-0000-000000000001"), "Q1", "people", "culture", 1m, 0m, 4m),
        new(Guid.Parse("00000000-0000-0000-0000-000000000002"), "Q2", "people", "culture", 3m, 0m, 4m)
    ];
}
