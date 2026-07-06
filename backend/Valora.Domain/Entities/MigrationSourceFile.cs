namespace Valora.Domain.Entities;

public sealed record MigrationSourceFile
{
    public Guid Id { get; init; }
    public Guid? BatchId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string? ContentType { get; init; }
    public long SizeBytes { get; init; }
    public string Sha256 { get; init; } = string.Empty;
    public string? StoredPath { get; init; }
    public string Status { get; init; } = "registered";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
