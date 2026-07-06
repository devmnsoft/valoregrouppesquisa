namespace Valora.Domain.Entities;

public sealed record OrganizationBranding
{
    public Guid Id { get; init; }
    public Guid OrganizationId { get; init; }
    public string PrimaryColor { get; init; } = "#0b3d4d";
    public string SecondaryColor { get; init; } = "#d7a94b";
    public string? LogoUrl { get; init; }
    public string? PublicSlug { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; init; }
}
