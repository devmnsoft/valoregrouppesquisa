namespace Valora.Application.DTOs;

public sealed record SubmitResponseRequest(
    string? ParticipantName,
    string? ParticipantEmail,
    IReadOnlyList<PublicSurveyAnswerRequest> Answers);
