namespace Valora.Domain.Entities;

public sealed record CompareReport
{
    public Guid Id { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
