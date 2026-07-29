namespace Valora.Application.DTOs;

public sealed record LgpdConsentDto(Guid Id,Guid? OrganizationId,Guid? SurveyId,Guid? ResponseId,string ConsentVersion,bool Accepted,DateTimeOffset? AcceptedAt,DateTimeOffset CreatedAt);
