namespace Valora.Domain.Entities;

public sealed record OrganizationSettings { public Guid Id { get; init; } public Guid OrganizationId { get; init; } public string SettingsJson { get; init; } = "{}"; public DateTime CreatedAt { get; init; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; init; } }
