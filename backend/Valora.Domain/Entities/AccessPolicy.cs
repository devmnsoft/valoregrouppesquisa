namespace Valora.Domain.Entities;

public sealed record AccessPolicy
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string RoleCode { get; init; } = string.Empty;
    public string ModuleCode { get; init; } = string.Empty;
    public string PermissionCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
