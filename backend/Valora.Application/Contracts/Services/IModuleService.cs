using Valora.Application.DTOs;
namespace Valora.Application.Contracts;

public interface IModuleService { Task<IReadOnlyList<ModuleDto>> ListAsync(Guid? organizationId=null); }
