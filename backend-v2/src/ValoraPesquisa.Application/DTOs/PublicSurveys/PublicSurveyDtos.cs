namespace ValoraPesquisa.Application.DTOs;
public record PublicQuestionDto(Guid Id,string Text,string Type,int Position,bool Required,IReadOnlyList<QuestionOptionDto> Options);
public record PublicSurveyDto(Guid Id,string Title,string? Description,bool ShowResult,IReadOnlyList<PublicQuestionDto> Questions);
public record PublicSurveyValidationDto(bool Ok,Guid SurveyId,string OrganizationSlug,PublicSurveyDto Survey);
