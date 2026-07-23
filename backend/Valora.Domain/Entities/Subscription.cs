using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Subscription : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public Guid PlanId { get; init; }
    public SubscriptionStatus Status { get; init; } = SubscriptionStatus.Active;
    public DateTime StartsAt { get; init; } = DateTime.UtcNow;
    public DateTime? EndsAt { get; init; }
}
