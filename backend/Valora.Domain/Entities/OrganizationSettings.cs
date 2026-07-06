namespace Valora.Domain.Entities;

public sealed record OrganizationSettings
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string SettingsJson { get; init; } = "{}";
    public bool LgpdConsentRequired { get; init; } = true;
    public bool AllowPublicResults { get; init; }
    public string TimeZone { get; init; } = "America/Sao_Paulo";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
