using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IEmailTemplateService { Task<IReadOnlyList<EmailTemplateDto>> ListAsync(Guid? organizationId); Task<EmailTemplateDto> UpsertAsync(Guid? id,UpsertEmailTemplateRequest request); }
