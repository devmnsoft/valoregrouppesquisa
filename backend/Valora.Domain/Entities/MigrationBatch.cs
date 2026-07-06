namespace Valora.Domain.Entities;

public sealed record MigrationBatch
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string SourceType { get; init; } = string.Empty;
    public string SourceName { get; init; } = string.Empty;
    public string Mode { get; init; } = "dry_run";
    public string Status { get; init; } = "created";
    public string? RequestedBy { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? FinishedAt { get; init; }
    public int TotalRecords { get; init; }
    public int ValidRecords { get; init; }
    public int InvalidRecords { get; init; }
    public int ImportedRecords { get; init; }
    public int SkippedRecords { get; init; }
    public int ConflictRecords { get; init; }
    public int ErrorRecords { get; init; }
    public string SummaryJson { get; init; } = "{}";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
