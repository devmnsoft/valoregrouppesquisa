namespace Valora.Application.DTOs;

public sealed record SurveyLinkDto(Guid Id,Guid SurveyId,string PublicUrl,string Status,DateTimeOffset? ExpiresAt,DateTimeOffset? RevokedAt);
