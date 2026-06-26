namespace Valora.Application.DTOs;

public record PublicSurveySubmitRequest(string Token,Dictionary<string,object> Participant,Dictionary<string,object> Answers,bool LgpdConsent,bool CommunicationConsent);
