namespace Valora.Application.DTOs;

public sealed record SubmitSurveyResponseRequest(
    string? Token,
    PublicSurveyParticipantRequest Participant,
    IReadOnlyList<PublicSurveyAnswerRequest> Answers,
    bool LgpdConsent,
    bool CommunicationConsent,
    string? IdempotencyKey = null);
