namespace ValoraPesquisa.Application.DTOs;
public record AuditEventDto(Guid Id,Guid? OrganizationId,Guid? UserId,string Action,string Entity,Guid? EntityId,string CorrelationId,string Metadata,DateTimeOffset CreatedAt);
