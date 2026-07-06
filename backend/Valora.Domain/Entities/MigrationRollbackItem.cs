namespace Valora.Domain.Entities;

public sealed record MigrationRollbackItem
{
    public Guid Id { get; init; }
    public Guid BatchId { get; init; }
    public string TargetEntity { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string Operation { get; init; } = string.Empty;
    public string? BeforeJson { get; init; }
    public string? AfterJson { get; init; }
    public string? RollbackSql { get; init; }
    public string Status { get; init; } = "planned";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? RolledBackAt { get; init; }
}
