using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Organization : AuditableEntity
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public TenantStatus Status { get; init; } = TenantStatus.Active;
    public Guid? BusinessGroupId { get; init; }

    public void EnsureValid()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new InvalidOperationException("Organization name is required.");
        }
    }
}
