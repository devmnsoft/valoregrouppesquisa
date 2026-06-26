namespace Valora.Application.DTOs;

public record AuditEntry(Guid? OrganizationId,Guid? UserId,string Action,string? EntityType,string? EntityId,string? Message,string MetadataJson="{}");
