namespace Valora.Domain.Entities;

public sealed record ImportBatch
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
