namespace Valora.Domain.Entities;

public sealed record SupportTicket
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Status { get; init; } = "open";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
