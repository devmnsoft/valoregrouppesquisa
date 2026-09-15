namespace Valora.Application.ReadModels;

public sealed record ResponseReadModel(Guid Id, Guid OrganizationId, Guid SurveyId, Guid FormId, Guid FormVersionId, string? ParticipantName, string? ParticipantEmail, string Status, DateTime? CompletedAt, string? ResultTokenHash, bool IsDeleted);
