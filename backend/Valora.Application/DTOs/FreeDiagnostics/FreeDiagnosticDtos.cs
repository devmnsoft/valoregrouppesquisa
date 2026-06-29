namespace Valora.Application.DTOs.FreeDiagnostics;

public sealed record FreeDiagnosticSummaryDto(int TotalResponses,int TodayResponses,int MonthResponses,int EmailsSent,int EmailsPending,int EmailsProcessing,int EmailsFailed,int CertificatesGenerated,int SharedLinks);
public sealed record FreeDiagnosticListItemDto(Guid ResponseId,Guid? OrganizationId,string? OrganizationName,string? SurveyTitle,string? ParticipantName,string? ParticipantEmail,string? EmailStatus,string? CertificateStatus,string? MaturityLevel,DateTime? CreatedAt,DateTime? CompletedAt,int EmailAttempts,int ResendCount,DateTime? LastResendAt,DateTime? ReviewedAt,string? LastError,string? ResultUrl,string? CertificateUrl,string? CorrelationId);
public sealed record FreeDiagnosticDetailDto(Guid ResponseId,Guid? OrganizationId,string? OrganizationName,string? SurveyTitle,string? ParticipantName,string? ParticipantEmail,string? EmailStatus,string? CertificateStatus,string? MaturityLevel,DateTime? CreatedAt,DateTime? CompletedAt,int EmailAttempts,int ResendCount,DateTime? LastResendAt,DateTime? ReviewedAt,string? ReviewNote,string? LastError,string? ResultUrl,string? CertificateUrl,string? ValidationCode,string? ValidationUrl,IReadOnlyList<object> RecentEmails,IReadOnlyList<object> RecentAuditEvents);
public sealed record FreeDiagnosticFilter(DateTime? StartDate,DateTime? EndDate,string? Name,string? Email,string? EmailStatus,string? MaturityLevel,string? CertificateStatus,int Page=1,int PageSize=50);
public sealed record ResendResultEmailRequest(string? Justification,bool Force=false);
public sealed record RegenerateCertificateRequest(string? Justification,string? LayoutVersion=null);
public sealed record MarkCommunicationReviewedRequest(string ReviewNote);
public sealed record FreeDiagnosticActionResult(bool Ok,string Status,string Message,string? CorrelationId=null);
