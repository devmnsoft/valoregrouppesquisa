namespace Valora.Application.ReadModels;
public sealed record SurveyLinkReadModel(Guid Id,Guid SurveyId,string? PublicUrl,string Status,DateTime? StartsAt,DateTime? ExpiresAt);
