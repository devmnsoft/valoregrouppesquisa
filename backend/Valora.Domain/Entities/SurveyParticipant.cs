namespace Valora.Domain.Entities;

public sealed record SurveyParticipant { public Guid Id { get; init; } public Guid SurveyId { get; init; } public string? Name { get; init; } public string? Email { get; init; } public string Status { get; init; } = "active"; public DateTime CreatedAt { get; init; } = DateTime.UtcNow; }
