namespace Valora.Application.ReadModels;
public sealed record SurveyPublicReadModel(Guid Id,Guid OrganizationId,Guid FormId,string Title,string? Description,string Status,bool LgpdRequired,string? OrganizationName,string? PublicName,string? OrganizationSlug,DateTime? StartsAt,DateTime? ExpiresAt);
