using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;
public sealed class ModuleRepository(IDbConnectionFactory connections) : IModuleRepository
{
    public async Task<IReadOnlyList<ModuleDto>> ListAsync()
    {
        using var connection = connections.Create();
        return (await connection.QueryAsync<ModuleDto>("SELECT id AS Id,code AS Code,name AS Name,NULL::text AS Description,category AS Category,status AS Status,0 AS DisplayOrder,NULL::text AS MinimumPlanCode FROM valorapesquisa.modules WHERE deleted_at IS NULL ORDER BY name")).ToList();
    }
    public async Task<IReadOnlyList<ModuleDto>> ListForOrganizationAsync(Guid organizationId)
    {
        using var connection = connections.Create();
        return (await connection.QueryAsync<ModuleDto>("""SELECT m.id AS Id,m.code AS Code,m.name AS Name,NULL::text AS Description,m.category AS Category,CASE WHEN COALESCE(om.enabled,true) THEN m.status ELSE 'disabled' END AS Status,0 AS DisplayOrder,NULL::text AS MinimumPlanCode FROM valorapesquisa.modules m LEFT JOIN valorapesquisa.organization_modules om ON om.module_id=m.id AND om.organization_id=@organizationId WHERE m.deleted_at IS NULL ORDER BY m.name""",new { organizationId })).ToList();
    }
    public async Task SetOrganizationModuleAsync(Guid organizationId,string moduleCode,bool enabled)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync("""INSERT INTO valorapesquisa.organization_modules(organization_id,module_id,module_code,enabled) SELECT @organizationId,id,code,@enabled FROM valorapesquisa.modules WHERE code=@moduleCode ON CONFLICT (organization_id,module_code) DO UPDATE SET module_id=EXCLUDED.module_id,enabled=EXCLUDED.enabled,updated_at=now()""",new { organizationId,moduleCode,enabled });
    }
}
