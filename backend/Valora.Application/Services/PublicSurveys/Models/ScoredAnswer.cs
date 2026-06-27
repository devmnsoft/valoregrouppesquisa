namespace Valora.Application.Services;
public sealed record ScoredAnswer(Guid QuestionId,string DimensionName,string? AnswerText,string AnswerJson,decimal Score,decimal MaxScore);
