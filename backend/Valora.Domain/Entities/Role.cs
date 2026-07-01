namespace Valora.Domain.Entities;

public sealed record Role { public Guid Id { get; init; } public Guid? OrganizationId { get; init; } public string Code { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public bool IsSystem { get; init; } public DateTime CreatedAt { get; init; } = DateTime.UtcNow; }
