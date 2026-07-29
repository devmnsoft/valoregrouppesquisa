using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IEmailStatusService { Task<EmailStatusDto> GetAsync(Guid? organizationId); }
