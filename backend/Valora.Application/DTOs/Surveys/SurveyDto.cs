namespace Valora.Application.DTOs;

public sealed record SurveyDto(Guid Id,Guid OrganizationId,Guid FormId,string Title,string? Description,string Status,DateTimeOffset? StartsAt,DateTimeOffset? ExpiresAt);
