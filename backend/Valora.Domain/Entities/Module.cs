namespace Valora.Domain.Entities;

public sealed record Module { public Guid Id { get; init; } public string Code { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public string? Description { get; init; } public string Category { get; init; } = "core"; public string Status { get; init; } = "active"; public int DisplayOrder { get; init; } public string? MinimumPlanCode { get; init; } public DateTime CreatedAt { get; init; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; init; } public bool IsDeleted { get; init; } }
