namespace Valora.Application.DTOs;

public sealed record AuditEntry
{
    public AuditEntry(
        Guid? organizationId,
        Guid? userId,
        string action,
        string? entityType,
        string? entityId,
        string? message,
        string? metadataJson = "{}",
        string? correlationId = null,
        DateTimeOffset? createdAt = null)
    {
        OrganizationId = organizationId;
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;
        Message = message;
        MetadataJson = string.IsNullOrWhiteSpace(metadataJson) ? "{}" : metadataJson;
        CorrelationId = correlationId;
        CreatedAt = createdAt;
    }

    public Guid? OrganizationId { get; }
    public Guid? UserId { get; }
    public string Action { get; }
    public string? EntityType { get; }
    public string? EntityId { get; }
    public string? Message { get; }
    public string MetadataJson { get; }
    public string? CorrelationId { get; }
    public DateTimeOffset? CreatedAt { get; }
}
