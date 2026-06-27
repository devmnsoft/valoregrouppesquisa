using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IPlanEntitlementService
{
    Task<LimitCheckResult> CheckLimitAsync(Guid organizationId,string limitKey,int requestedAmount=1);
    Task<PlanEntitlements> ResolveAsync(Guid organizationId);
    Task<UsageDto> GetUsageAsync(Guid organizationId);
}
