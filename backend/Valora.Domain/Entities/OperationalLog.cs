namespace Valora.Domain.Entities;

public sealed record OperationalLog
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? OrganizationId { get; init; }
    public string Level { get; init; } = "Information";
    public string Message { get; init; } = string.Empty;
    public string CorrelationId { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
