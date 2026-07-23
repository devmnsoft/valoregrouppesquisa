using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record OrganizationSettings : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public string SettingsJson { get; init; } = "{}";
    public bool LgpdConsentRequired { get; init; } = true;
    public bool AllowPublicResults { get; init; }
    public string TimeZone { get; init; } = "America/Sao_Paulo";
}
