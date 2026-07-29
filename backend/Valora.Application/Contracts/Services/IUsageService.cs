using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IUsageService { Task<UsageDto> GetMonthlyAsync(Guid organizationId,DateTime month); }
