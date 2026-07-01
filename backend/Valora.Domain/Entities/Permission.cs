namespace Valora.Domain.Entities;

public sealed record Permission { public Guid Id { get; init; } public string Code { get; init; } = string.Empty; public string Name { get; init; } = string.Empty; public string? ModuleCode { get; init; } public DateTime CreatedAt { get; init; } = DateTime.UtcNow; }
