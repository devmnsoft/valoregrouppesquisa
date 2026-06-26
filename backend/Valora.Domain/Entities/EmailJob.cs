namespace Valora.Domain.Entities;

public sealed record EmailJob
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
