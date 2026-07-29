namespace Valora.Application.Results;

public sealed record SurveyQuestionInput(string Id, string Type, string? Dimension, decimal Weight, decimal MaxScore, decimal? ScoreWhenFilled, IReadOnlyList<SurveyOptionInput> Options);
