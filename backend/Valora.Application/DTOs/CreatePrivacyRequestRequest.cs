namespace Valora.Application.DTOs;

public sealed record CreatePrivacyRequestRequest(Guid? OrganizationId,string RequesterEmail,string RequestType,string? Description,Guid? ResponseId);
