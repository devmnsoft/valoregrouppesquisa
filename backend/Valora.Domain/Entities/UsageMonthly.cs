using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record UsageMonthly : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public string UsageKey { get; init; } = string.Empty;
    public int Year { get; init; }
    public int Month { get; init; }
    public long Quantity { get; init; }

    public UsageMonthly Add(long quantity) => this with { Quantity = Quantity + quantity, UpdatedAt = DateTime.UtcNow };
}
