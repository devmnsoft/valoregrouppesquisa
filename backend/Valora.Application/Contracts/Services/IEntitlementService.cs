namespace Valora.Application.Contracts;
public interface IEntitlementService { Task<Valora.Application.DTOs.EntitlementDto> ResolveAsync(Guid organizationId); Task<bool> CanUseAsync(Guid organizationId,string moduleCode); }
