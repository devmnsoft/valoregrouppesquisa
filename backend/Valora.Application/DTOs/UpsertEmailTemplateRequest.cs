namespace Valora.Application.DTOs;

public sealed record UpsertEmailTemplateRequest(Guid? OrganizationId,string Code,string Name,string Subject,string BodyHtml,string? BodyText,string Status = "active");
