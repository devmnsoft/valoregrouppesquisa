namespace ValoraPesquisa.Application.DTOs;
public record QuestionOptionDto(Guid Id,Guid QuestionId,string Text,decimal Score,int Position);
public record QuestionDto(Guid Id,string Text,string Type,int Position,bool Required,decimal Weight,decimal MaxScore,IReadOnlyList<QuestionOptionDto> Options);
public record FormDto(Guid Id,Guid OrganizationId,string Title,string? Description,string Status,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt,IReadOnlyList<QuestionDto> Questions);
