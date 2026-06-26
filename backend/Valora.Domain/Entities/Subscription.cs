namespace Valora.Domain.Entities;

public sealed record Subscription
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
