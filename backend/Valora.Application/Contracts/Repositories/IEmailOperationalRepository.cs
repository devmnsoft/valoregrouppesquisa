using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IEmailOperationalRepository { Task<IReadOnlyList<EmailTemplateDto>> ListTemplatesAsync(Guid? organizationId); Task<EmailTemplateDto> UpsertTemplateAsync(Guid? id,UpsertEmailTemplateRequest request); Task<EmailJobDto> QueueAsync(Guid? organizationId,Guid? responseId,Guid? certificateId,string templateCode,string toEmail,string subject,string bodyHtml,string? bodyText); Task<IReadOnlyList<EmailJobDto>> ListJobsAsync(Guid? organizationId,string? status=null); Task<EmailJobDto?> GetJobAsync(Guid id); Task MarkProcessingAsync(Guid id); Task MarkSentAsync(Guid id); Task MarkFailedAsync(Guid id,string reason,bool deadLetter); Task<EmailStatusDto> StatusAsync(Guid? organizationId,bool developmentOutbox); }
