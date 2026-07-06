namespace Valora.Domain.Entities;

public sealed record DataExportRequest
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Status { get; init; } = "queued";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
