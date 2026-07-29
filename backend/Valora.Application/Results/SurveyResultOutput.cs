namespace Valora.Application.Results;

public sealed record SurveyResultOutput(decimal RawScore, decimal MaxScore, decimal Percentage, decimal Normalized5, string Band, string Recommendation, IReadOnlyList<DimensionScoreOutput> Dimensions);
