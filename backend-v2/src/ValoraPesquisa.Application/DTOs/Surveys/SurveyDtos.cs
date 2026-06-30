namespace ValoraPesquisa.Application.DTOs;
public record SurveyDto(Guid Id,Guid OrganizationId,Guid FormId,string Title,string? Description,string Status,DateTimeOffset? StartsAt,DateTimeOffset? ExpiresAt,bool ShowResult,bool AllowRepeat,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record SurveyLinkDto(Guid Id,Guid SurveyId,Guid OrganizationId,string PublicUrl,string Status,DateTimeOffset? ExpiresAt,DateTimeOffset? RevokedAt,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public record CreatedSurveyLinkDto(Guid Id,Guid SurveyId,Guid OrganizationId,string PublicUrl,string Token,string Status,DateTimeOffset? ExpiresAt);
