using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record OrganizationModule : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public Guid? ModuleId { get; init; }
    public string ModuleCode { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string Source { get; init; } = "plan";
    public string? BlockReason { get; init; }
}
