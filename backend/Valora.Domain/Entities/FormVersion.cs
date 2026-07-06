namespace Valora.Domain.Entities;

public sealed record FormVersion
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid FormId { get; init; }
    public int VersionNumber { get; init; }
    public string SnapshotJson { get; init; } = "{}";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
