using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IPlanRepository { Task<IReadOnlyList<PlanDto>> GetPublicPlansAsync(); Task<PlanDto?> GetByIdAsync(string id); Task<string?> GetCurrentPlanIdAsync(Guid organizationId); Task CreateSubscriptionAsync(Guid organizationId,string planId); }
