using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Department : AuditableEntity {
    public Guid OrganizationId { get; init; }
    public Guid? UnitId { get; init; }
    public string Name { get; init; } = string.Empty;
    public TenantStatus Status { get; init; } = TenantStatus.Active;
}
