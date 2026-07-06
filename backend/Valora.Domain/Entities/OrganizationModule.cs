namespace Valora.Domain.Entities;

public sealed record OrganizationModule
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public Guid? ModuleId { get; init; }
    public string ModuleCode { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string Source { get; init; } = "plan";
    public string? BlockReason { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
