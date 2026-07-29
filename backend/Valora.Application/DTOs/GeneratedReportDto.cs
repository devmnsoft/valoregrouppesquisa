namespace Valora.Application.DTOs;

public sealed record GeneratedReportDto(Guid Id,Guid OrganizationId,Guid? SurveyId,Guid? ResponseId,string Title,string Format,string Status,string PayloadJson,string? FileName,string? MimeType,DateTimeOffset CreatedAt);
