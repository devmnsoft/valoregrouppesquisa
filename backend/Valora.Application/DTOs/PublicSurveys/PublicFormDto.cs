namespace Valora.Application.DTOs;
public sealed record PublicFormDto(Guid Id,string Name,string? Description,int? TimeMin,IReadOnlyList<PublicQuestionDto> Questions);
