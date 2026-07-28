using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public sealed record PlanRecord(Guid Id,string Code,string Name,bool IsPublic,bool IsActive);
public sealed record PlanLimitRecord(string LimitKey,int? LimitValue,string Period);
public sealed record PlanCapabilityRecord(string CapabilityKey,bool Enabled);
public interface IPlanRepository { Task<IReadOnlyList<PlanDto>> GetPublicPlansAsync(); Task<PlanDto?> GetByIdAsync(string id); Task<string?> GetCurrentPlanIdAsync(Guid organizationId); Task CreateSubscriptionAsync(Guid organizationId,string planId); }
