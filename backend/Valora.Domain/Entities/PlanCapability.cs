using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record PlanCapability : AuditableEntity
{
    public Guid PlanId { get; init; }
    public string CapabilityKey { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public string? MetadataJson { get; init; }
}
