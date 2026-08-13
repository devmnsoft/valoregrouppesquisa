using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IPlanRepository
{
    Task<IReadOnlyList<PlanDto>> GetPublicPlansAsync(CancellationToken cancellationToken = default);
    Task<PlanDto?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<string?> GetCurrentPlanIdAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task CreateSubscriptionAsync(Guid organizationId, string planId, CancellationToken cancellationToken = default);
}
