namespace Valora.Application.DTOs;

public sealed record CertificateDto(Guid Id,Guid OrganizationId,Guid? SurveyId,Guid ResponseId,string? ParticipantName,string? ParticipantEmailMasked,string? CompanyName,decimal? TotalScore,string? Level,string ValidationCode,string? ValidationUrl,string Status,DateTimeOffset? IssuedAt,string PayloadJson);
