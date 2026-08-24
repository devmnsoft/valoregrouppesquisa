using Valora.Application.Results;

namespace Valora.Tests;

public sealed class MethodologicalScoringServiceTests
{
    [Fact]
    public void Calculates_weighted_overall_concept_and_dimension_scores_with_decimal_math()
    {
        var result = new MethodologicalScoringService().Calculate([
            Answer("Q1", "governance", "decision", 5m, 2m),
            Answer("Q2", "governance", "accountability", 1m, 1m),
            Answer("Q3", "culture", "leadership", 3m, 1m)
        ]);

        Assert.Equal(62.50m, result.OverallScore);
        Assert.Equal(66.67m, result.Dimensions.Single(x => x.Code == "governance").Score);
        Assert.Equal("integrated", result.MaturityLevel);
        Assert.Equal(3, result.Evidence.Count);
    }

    [Fact]
    public void Ignores_zero_weight_invalid_range_and_unmapped_answers_without_dividing_by_zero()
    {
        var result = new MethodologicalScoringService().Calculate([
            Answer("ZERO", "governance", "decision", 3m, 0m),
            new(Guid.NewGuid(), "RANGE", "governance", "decision", 6m, 1m, 5m),
            Answer("UNMAPPED", "", "", 2m, 1m)
        ]);

        Assert.Null(result.OverallScore);
        Assert.Equal("insufficient_evidence", result.MaturityLevel);
        Assert.Equal(3, result.IgnoredQuestions.Count);
    }

    [Fact]
    public void Qualitative_answer_requires_an_explicit_normalization_rule()
    {
        var invalid = new MethodologicalAnswer(Guid.NewGuid(), "Q1", "culture", "communication", null, 0m, 1m, IsQualitative: true);
        var valid = invalid with { QuestionCode = "Q2", QualitativeNormalizedValue = 80m };
        var result = new MethodologicalScoringService().Calculate([invalid, valid]);

        Assert.Equal(80m, result.OverallScore);
        Assert.Equal(.70m, Assert.Single(result.Evidence).Confidence);
        Assert.Contains("Q1", result.IgnoredQuestions);
    }

    private static MethodologicalAnswer Answer(string code, string dimension, string concept, decimal value, decimal weight)
        => new(Guid.NewGuid(), code, dimension, concept, value, 1m, 5m, weight);
}
