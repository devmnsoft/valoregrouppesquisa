using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class EmailStatusService(IEmailOperationalRepository repo):IEmailStatusService{ public Task<EmailStatusDto> GetAsync(Guid? o)=>repo.StatusAsync(o,true); }
