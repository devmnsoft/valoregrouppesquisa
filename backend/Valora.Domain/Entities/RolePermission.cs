namespace Valora.Domain.Entities;

public sealed record RolePermission
{
    public Guid Id { get; init; }
    public Guid RoleId { get; init; }
    public Guid PermissionId { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}
