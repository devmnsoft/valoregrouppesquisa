namespace Valora.Application.DTOs;

public sealed record EmailJobDto(Guid Id,Guid? OrganizationId,Guid? ResponseId,Guid? CertificateId,string TemplateCode,string FromEmail,string FromName,string ToEmailMasked,string Subject,string Status,int Attempts,DateTimeOffset? NextAttemptAt,DateTimeOffset? LastAttemptAt,DateTimeOffset? SentAt,string? DeadLetterReason,DateTimeOffset CreatedAt);
