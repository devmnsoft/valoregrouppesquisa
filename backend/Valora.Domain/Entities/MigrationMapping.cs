namespace Valora.Domain.Entities;

public sealed record MigrationMapping
{
    public Guid Id { get; init; }
    public Guid BatchId { get; init; }
    public string LegacyCollection { get; init; } = string.Empty;
    public string LegacyId { get; init; } = string.Empty;
    public string TargetEntity { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string MappingKey { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
