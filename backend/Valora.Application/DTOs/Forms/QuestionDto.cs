namespace Valora.Application.DTOs;

public sealed record QuestionDto(Guid Id,Guid FormId,string Text,string Type,int Position,bool Required,decimal Weight,IReadOnlyList<QuestionOptionDto> Options);
