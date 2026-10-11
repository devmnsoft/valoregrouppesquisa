namespace Valora.Application.DTOs;

public sealed record AuditEventDto(Guid Id, Guid? OrganizationId, Guid? UserId, string Action, string Entity, Guid? EntityId, string CorrelationId, DateTimeOffset CreatedAt);
public sealed record AuditListQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtcExclusive = null,
    string? Action = null,
    string? EntityType = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 50);
