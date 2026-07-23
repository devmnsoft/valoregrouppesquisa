using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Unit : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public Guid LegalEntityId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Code { get; init; }
    public TenantStatus Status { get; init; } = TenantStatus.Active;
}
