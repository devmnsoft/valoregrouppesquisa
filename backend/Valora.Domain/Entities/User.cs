using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record User : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public TenantStatus Status { get; init; } = TenantStatus.Active;
    public DateTime? LastLoginAt { get; init; }
}
