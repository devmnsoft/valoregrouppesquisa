using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record BusinessGroup : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? TaxId { get; init; }
    public TenantStatus Status { get; init; } = TenantStatus.Active;
}
