using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class UsageService(IUsageRepository repo) : IUsageService { public Task<UsageDto> GetMonthlyAsync(Guid organizationId,DateTime month)=>repo.GetMonthlyAsync(organizationId,month); }
