namespace Valora.Application.DTOs;
public sealed record PublicSurveyDto(Guid Id,string Title,string? Description,string Status,bool LgpdRequired);
