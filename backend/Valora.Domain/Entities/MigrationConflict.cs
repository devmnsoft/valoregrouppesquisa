namespace Valora.Domain.Entities;

public sealed record MigrationConflict
{
    public Guid Id { get; init; }
    public Guid BatchId { get; init; }
    public string LegacyCollection { get; init; } = string.Empty;
    public string? LegacyId { get; init; }
    public string TargetEntity { get; init; } = string.Empty;
    public Guid? TargetId { get; init; }
    public string ConflictType { get; init; } = string.Empty;
    public string Severity { get; init; } = "warning";
    public string LegacyValueJson { get; init; } = "{}";
    public string CurrentValueJson { get; init; } = "{}";
    public string? Resolution { get; init; }
    public string? ResolvedBy { get; init; }
    public DateTime? ResolvedAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
