namespace Valora.Domain.Entities;

public sealed record SurveyInvite { public Guid Id { get; init; } public Guid SurveyId { get; init; } public string Email { get; init; } = string.Empty; public string Status { get; init; } = "pending"; public DateTime CreatedAt { get; init; } = DateTime.UtcNow; public DateTime? UpdatedAt { get; init; } }
