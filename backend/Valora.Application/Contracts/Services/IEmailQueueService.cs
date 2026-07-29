using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IEmailQueueService { Task<EmailJobDto> QueueResultAsync(Guid organizationId,Guid responseId,string toEmail); Task<EmailJobDto> QueueCertificateAsync(Guid organizationId,Guid certificateId,string toEmail); Task<EmailJobDto> QueueInviteAsync(Guid organizationId,Guid surveyId,string toEmail); Task<EmailJobDto?> RetryAsync(Guid id); }
