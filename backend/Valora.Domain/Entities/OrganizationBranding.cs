namespace Valora.Domain.Entities;

public sealed record OrganizationBranding { public Guid Id { get; init; } public Guid OrganizationId { get; init; } public string? LogoUrl { get; init; } public string? PrimaryColor { get; init; } public string? SecondaryColor { get; init; } public DateTime CreatedAt { get; init; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; init; } }
