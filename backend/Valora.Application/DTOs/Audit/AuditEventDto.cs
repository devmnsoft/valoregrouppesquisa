namespace Valora.Application.DTOs;

public sealed record AuditEventDto(Guid Id,Guid? OrganizationId,Guid? UserId,string Action,string Entity,Guid? EntityId,string CorrelationId,DateTimeOffset CreatedAt);
