namespace Valora.Application.DTOs;

public sealed record EmailTemplateDto(Guid Id,Guid? OrganizationId,string Code,string Name,string Subject,string BodyHtml,string? BodyText,string Status);
