using Valora.Domain.Common;
using Valora.Domain.Enums;

namespace Valora.Domain.Entities;

public sealed record OrganizationBranding : AuditableEntity
{
    public Guid OrganizationId { get; init; }
    public string PrimaryColor { get; init; } = "#0b3d4d";
    public string SecondaryColor { get; init; } = "#d7a94b";
    public string? LogoUrl { get; init; }
    public string? PublicSlug { get; init; }
    public bool WhiteLabelEnabled { get; init; }
}
