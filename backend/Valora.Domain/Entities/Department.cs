namespace Valora.Domain.Entities;

public sealed record Department
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? UnitId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
