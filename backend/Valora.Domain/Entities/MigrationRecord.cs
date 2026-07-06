namespace Valora.Domain.Entities;

public sealed record MigrationRecord
{
    public Guid Id { get; init; }
    public Guid BatchId { get; init; }
    public Guid? SourceFileId { get; init; }
    public string LegacyCollection { get; init; } = string.Empty;
    public string? LegacyId { get; init; }
    public string TargetEntity { get; init; } = string.Empty;
    public Guid? TargetId { get; init; }
    public string Action { get; init; } = "insert";
    public string Status { get; init; } = "planned";
    public string InputJson { get; init; } = "{}";
    public string NormalizedJson { get; init; } = "{}";
    public string? ErrorCode { get; init; }
    public string? ErrorMessage { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; init; } = DateTime.UtcNow;
}
