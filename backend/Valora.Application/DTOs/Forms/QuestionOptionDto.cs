namespace Valora.Application.DTOs;

public sealed record QuestionOptionDto(Guid Id,Guid QuestionId,string Text,decimal Score,int Position);
