using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record LegalEntity : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public Guid? BusinessGroupId { get; init; }
    public string LegalName { get; init; } = string.Empty;
    public string TradeName { get; init; } = string.Empty;
    public string Cnpj { get; init; } = string.Empty;
    public TenantStatus Status { get; init; } = TenantStatus.Active;
}
