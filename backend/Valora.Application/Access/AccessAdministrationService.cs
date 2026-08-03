using Valora.Application.Exceptions;

namespace Valora.Application.Access;

public sealed class AccessAdministrationService(IAccessAdministrationRepository repository) : IAccessAdministrationService
{
    public Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(Guid org, CancellationToken ct) => repository.ListRolesAsync(Required(org), ct);
    public async Task<AccessRoleDto> GetRoleAsync(Guid org, Guid id, CancellationToken ct) => await repository.GetRoleAsync(Required(org), id, ct) ?? throw new NotFoundAppException("Papel não encontrado.");
    public Task<IReadOnlyList<AccessPermissionDto>> ListPermissionsAsync(CancellationToken ct) => repository.ListPermissionsAsync(ct);
    public async Task<IReadOnlyList<AccessModuleDto>> ListModulesAsync(CancellationToken ct) { var permissions=await repository.ListPermissionsAsync(ct); return ValoraModules.All.Select(code=>new AccessModuleDto(code, ModuleName(code), "active", permissions.Where(p=>p.ModuleCode==code).ToArray())).ToArray(); }
    public async Task<AccessRoleDto> CreateRoleAsync(Guid org, Guid actor, CreateAccessRoleRequest request, CancellationToken ct) { Validate(request.Code, request.Name, request.Permissions); return await repository.CreateRoleAsync(Required(org), actor, request, ct); }
    public async Task<AccessRoleDto> UpdateRoleAsync(Guid org, Guid actor, Guid id, UpdateAccessRoleRequest request, CancellationToken ct) => await repository.UpdateRoleAsync(Required(org), actor, id, request, ct) ?? throw new ConcurrencyConflictException("O papel foi alterado por outro administrador.");
    public async Task DeleteRoleAsync(Guid org, Guid actor, Guid id, CancellationToken ct) { if(!await repository.DeleteRoleAsync(Required(org), actor, id, ct)) throw new ConflictAppException("Papel de sistema, em uso ou inexistente não pode ser excluído."); }
    public async Task<AccessRoleDto> ReplacePermissionsAsync(Guid org, Guid actor, Guid id, ReplaceRolePermissionsRequest request, CancellationToken ct) { Validate("role", "role", request.Permissions); return await repository.ReplacePermissionsAsync(Required(org), actor, id, request, ct) ?? throw new ConcurrencyConflictException("Papel inexistente, protegido ou com versão desatualizada."); }
    public async Task<EffectiveAccessDto> GetEffectiveAccessAsync(Guid org, Guid user, CancellationToken ct) => await repository.GetEffectiveAccessAsync(Required(org), user, ct) ?? throw new NotFoundAppException("Usuário não encontrado nesta organização.");
    private static Guid Required(Guid id) => id != Guid.Empty ? id : throw new ForbiddenAppException("Organização autenticada é obrigatória.");
    private static void Validate(string code,string name,IReadOnlyList<string> permissions) { if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(name)) throw new ValidationAppException("Código e nome são obrigatórios."); if(permissions.Count!=permissions.Distinct(StringComparer.Ordinal).Count()) throw new ValidationAppException("Permissões duplicadas não são permitidas."); }
    private static string ModuleName(string code)=>code switch { "identity"=>"Identidade", "organization"=>"Organização", "forms"=>"Diagnósticos", "surveys"=>"Pesquisas", "distribution"=>"Distribuição", "responses"=>"Respostas", "results"=>"Resultados", "certificates"=>"Certificados", "communications"=>"Comunicações", "audit"=>"Auditoria", "settings"=>"Configurações", _=>"Operações" };
}
