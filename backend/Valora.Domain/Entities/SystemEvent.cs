namespace Valora.Domain.Entities;

public sealed record SystemEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Code { get; init; } = string.Empty;
    public string Severity { get; init; } = "info";
    public string PayloadJson { get; init; } = "{}";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
