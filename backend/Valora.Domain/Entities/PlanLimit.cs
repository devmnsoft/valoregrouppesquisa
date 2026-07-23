using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record PlanLimit : AuditableEntity
{
    public Guid PlanId { get; init; }
    public string LimitKey { get; init; } = string.Empty;
    public int? LimitValue { get; init; }
    public string Period { get; init; } = "lifetime";

    public bool IsUnlimited => LimitValue is null;
}
