using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record Plan : AuditableEntity
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public PlanPublicationStatus PublicationStatus { get; init; } = PlanPublicationStatus.PublicActive;
    public bool IsPublic { get; init; }
    public bool IsActive { get; init; } = true;
}
