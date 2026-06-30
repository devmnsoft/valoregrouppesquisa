namespace ValoraPesquisa.Application.DTOs;
public record SubmitSurveyResponseRequest(string Token,string Org,string? ParticipantName,string? ParticipantEmail,IReadOnlyList<SubmitAnswerDto> Answers);
public record SubmitAnswerDto(Guid QuestionId,string? AnswerValue,string? AnswerText);
public record SubmitSurveyResponseResult(Guid ResponseId,string ResultToken,decimal TotalScore,decimal Percentage,decimal Normalized5,string Level);
public record ResponseDto(Guid Id,Guid OrganizationId,Guid SurveyId,Guid FormId,string? ParticipantName,string? ParticipantEmail,string Status,DateTimeOffset CompletedAt,DateTimeOffset CreatedAt);
