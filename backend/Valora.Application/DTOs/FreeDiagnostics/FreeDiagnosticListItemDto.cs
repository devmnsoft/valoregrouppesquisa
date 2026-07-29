namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record FreeDiagnosticListItemDto(Guid ResponseId,Guid? OrganizationId,string? OrganizationName,string? SurveyTitle,string? ParticipantName,string? ParticipantEmail,string? EmailStatus,string? CertificateStatus,string? MaturityLevel,DateTime? CreatedAt,DateTime? CompletedAt,int EmailAttempts,int ResendCount,DateTime? LastResendAt,DateTime? ReviewedAt,string? LastError,string? ResultUrl,string? CertificateUrl,string? CorrelationId);
