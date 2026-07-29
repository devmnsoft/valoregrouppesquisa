namespace Valora.Application.DTOs;

public sealed record ResponseDto(Guid Id,Guid OrganizationId,Guid SurveyId,Guid FormId,string? ParticipantName,string? ParticipantEmail,string Status,DateTimeOffset? CompletedAt);
