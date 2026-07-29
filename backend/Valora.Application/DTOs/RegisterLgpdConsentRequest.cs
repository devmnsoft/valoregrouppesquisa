namespace Valora.Application.DTOs;

public sealed record RegisterLgpdConsentRequest(Guid? OrganizationId,Guid? SurveyId,Guid? ResponseId,string? ParticipantEmail,string ConsentText,string ConsentVersion,bool Accepted);
