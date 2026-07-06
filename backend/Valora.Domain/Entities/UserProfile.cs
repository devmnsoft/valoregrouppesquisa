namespace Valora.Domain.Entities;

public sealed record UserProfile
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid? OrganizationId { get; init; }
    public string RoleCode { get; init; } = string.Empty;
    public Guid? DepartmentId { get; init; }
    public string? DisplayName { get; init; }
    public string? Department { get; init; }
    public string? Phone { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
