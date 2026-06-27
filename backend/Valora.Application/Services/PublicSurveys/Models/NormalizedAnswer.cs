namespace Valora.Application.Services;
public sealed record NormalizedAnswer(Guid QuestionId,string? AnswerText,string AnswerJson,decimal? NumericValue);
