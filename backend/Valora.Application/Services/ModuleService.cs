using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class ModuleService(IModuleRepository repo) : IModuleService { public Task<IReadOnlyList<ModuleDto>> ListAsync(Guid? organizationId=null)=>organizationId.HasValue?repo.ListForOrganizationAsync(organizationId.Value):repo.ListAsync(); }
