namespace Valora.Application.Contracts;
public interface IModuleRepository { Task<IReadOnlyList<Valora.Application.DTOs.ModuleDto>> ListAsync(); Task<IReadOnlyList<Valora.Application.DTOs.ModuleDto>> ListForOrganizationAsync(Guid organizationId); Task SetOrganizationModuleAsync(Guid organizationId,string moduleCode,bool enabled); }
