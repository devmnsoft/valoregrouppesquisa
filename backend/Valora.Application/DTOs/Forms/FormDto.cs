namespace Valora.Application.DTOs;

public sealed record FormDto(Guid Id,Guid OrganizationId,string Title,string? Description,string Status,IReadOnlyList<QuestionDto> Questions);
