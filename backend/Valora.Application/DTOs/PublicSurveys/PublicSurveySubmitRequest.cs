namespace Valora.Application.DTOs;

public sealed record PublicSurveyParticipantRequest(
    string? Name,
    string? Email,
    string? Phone,
    bool Anonymous,
    string? ConsentVersion,
    string? Website = null,
    DateTimeOffset? FormStartedAt = null);

public sealed record PublicSurveyAnswerRequest(
    Guid QuestionId,
    string Type,
    decimal? ScaleValue = null,
    Guid? OptionId = null,
    IReadOnlyList<Guid>? OptionIds = null,
    decimal? NumberValue = null,
    string? TextValue = null,
    bool? BooleanValue = null);

public sealed record PublicSurveySubmitRequest(
    string Token,
    PublicSurveyParticipantRequest Participant,
    IReadOnlyList<PublicSurveyAnswerRequest> Answers,
    bool LgpdConsent,
    bool CommunicationConsent);
