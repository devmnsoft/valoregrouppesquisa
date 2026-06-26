namespace Valora.Application.DTOs;
public sealed record SubmitSurveyResponseRequest(string? Token, Dictionary<string,object>? Participant, Dictionary<string,object>? Answers, bool LgpdConsent, bool CommunicationConsent);
