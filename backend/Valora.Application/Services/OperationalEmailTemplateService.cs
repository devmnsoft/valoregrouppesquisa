using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class OperationalEmailTemplateService(IEmailOperationalRepository repo):IEmailTemplateService{ public Task<IReadOnlyList<EmailTemplateDto>> ListAsync(Guid? o)=>repo.ListTemplatesAsync(o); public Task<EmailTemplateDto> UpsertAsync(Guid? id,UpsertEmailTemplateRequest r)=>repo.UpsertTemplateAsync(id,r); }
