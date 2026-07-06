namespace Valora.Domain.Entities;

public sealed record SupportTicketMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid TicketId { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool Internal { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
