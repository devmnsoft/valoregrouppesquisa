namespace Valora.Application.DTOs;
public sealed record PublicQuestionDto(Guid Id,string Text,string Type,bool Required,decimal MaxScore,string? DimensionName,int DisplayOrder,IReadOnlyList<PublicQuestionOptionDto> Options);
