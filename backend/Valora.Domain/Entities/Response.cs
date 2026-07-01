namespace Valora.Domain.Entities;

public sealed record Response { public Guid Id { get; init; } public Guid OrganizationId { get; init; } public Guid SurveyId { get; init; } public Guid FormId { get; init; } public string? ParticipantName { get; init; } public string? ParticipantEmail { get; init; } public string Status { get; init; } = "completed"; public DateTime CreatedAt { get; init; } = DateTime.UtcNow; }
