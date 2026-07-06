namespace Valora.Domain.Entities;

public sealed record ImportLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid MigrationBatchId { get; init; }
    public string EntityType { get; init; } = string.Empty;
    public string LegacyId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? Error { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
