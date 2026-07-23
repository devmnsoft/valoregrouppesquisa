using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Module : AuditableEntity
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = "core";
    public TenantStatus Status { get; init; } = TenantStatus.Active;
    public int DisplayOrder { get; init; }
    public string? MinimumPlanCode { get; init; }
}
