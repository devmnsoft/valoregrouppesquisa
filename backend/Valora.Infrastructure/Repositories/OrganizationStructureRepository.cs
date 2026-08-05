using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Repositories;

public sealed class OrganizationStructureRepository(IDbConnectionFactory factory) : IOrganizationStructureRepository
{
    public async Task<IReadOnlyList<UnitResponse>> ListUnitsAsync(Guid organizationId, string? status = null, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        const string sql = "SELECT id Id,organization_id OrganizationId,legal_entity_id LegalEntityId,name Name,code Code,type Type,region Region,state State,city City,status Status,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.units WHERE organization_id=@organizationId AND deleted_at IS NULL AND (@status IS NULL OR status=@status) ORDER BY status,name";
        return (await c.QueryAsync<UnitResponse>(new CommandDefinition(sql, new { organizationId, status }, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<UnitResponse?> GetUnitAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        const string sql = "SELECT id Id,organization_id OrganizationId,legal_entity_id LegalEntityId,name Name,code Code,type Type,region Region,state State,city City,status Status,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.units WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL";
        return await c.QuerySingleOrDefaultAsync<UnitResponse>(new CommandDefinition(sql, new { organizationId, id }, cancellationToken: cancellationToken));
    }
    public async Task<UnitResponse> CreateUnitAsync(Guid organizationId, UpsertUnitRequest request, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        var legalEntityId = request.LegalEntityId ?? await c.ExecuteScalarAsync<Guid>(new CommandDefinition("SELECT id FROM valorapesquisa.legal_entities WHERE organization_id=@organizationId AND deleted_at IS NULL ORDER BY created_at LIMIT 1", new { organizationId }, cancellationToken: cancellationToken));
        var id = await c.ExecuteScalarAsync<Guid>(new CommandDefinition("INSERT INTO valorapesquisa.units(organization_id,legal_entity_id,name,code,type,region,state,city,status) VALUES(@organizationId,@legalEntityId,@Name,@Code,@Type,@Region,@State,@City,'active') RETURNING id", new { organizationId, legalEntityId, request.Name, request.Code, request.Type, request.Region, request.State, request.City }, cancellationToken: cancellationToken));
        return (await GetUnitAsync(organizationId, id, cancellationToken))!;
    }
    public async Task<UnitResponse?> UpdateUnitAsync(Guid organizationId, Guid id, UpsertUnitRequest request, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        var changed = await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.units SET legal_entity_id=COALESCE(@LegalEntityId,legal_entity_id),name=@Name,code=@Code,type=@Type,region=@Region,state=@State,city=@City,updated_at=now() WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL", new { organizationId, id, request.LegalEntityId, request.Name, request.Code, request.Type, request.Region, request.State, request.City }, cancellationToken: cancellationToken));
        return changed == 0 ? null : await GetUnitAsync(organizationId, id, cancellationToken);
    }
    public async Task<bool> SetUnitStatusAsync(Guid organizationId, Guid id, string status, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        return await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.units SET status=@status,updated_at=now() WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL", new { organizationId, id, status }, cancellationToken: cancellationToken)) > 0;
    }
    public async Task<IReadOnlyList<DepartmentResponse>> ListDepartmentsAsync(Guid organizationId, Guid? unitId = null, string? status = null, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        const string sql = "SELECT id Id,organization_id OrganizationId,unit_id UnitId,name Name,code Code,type Type,status Status,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.departments WHERE organization_id=@organizationId AND deleted_at IS NULL AND (@unitId IS NULL OR unit_id=@unitId) AND (@status IS NULL OR status=@status) ORDER BY status,name";
        return (await c.QueryAsync<DepartmentResponse>(new CommandDefinition(sql, new { organizationId, unitId, status }, cancellationToken: cancellationToken))).AsList();
    }
    public async Task<DepartmentResponse?> GetDepartmentAsync(Guid organizationId, Guid id, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        const string sql = "SELECT id Id,organization_id OrganizationId,unit_id UnitId,name Name,code Code,type Type,status Status,created_at CreatedAt,updated_at UpdatedAt FROM valorapesquisa.departments WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL";
        return await c.QuerySingleOrDefaultAsync<DepartmentResponse>(new CommandDefinition(sql, new { organizationId, id }, cancellationToken: cancellationToken));
    }
    public async Task<DepartmentResponse> CreateDepartmentAsync(Guid organizationId, UpsertDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        var id = await c.ExecuteScalarAsync<Guid>(new CommandDefinition("INSERT INTO valorapesquisa.departments(organization_id,unit_id,name,code,type,status) VALUES(@organizationId,@UnitId,@Name,@Code,@Type,'active') RETURNING id", new { organizationId, request.UnitId, request.Name, request.Code, request.Type }, cancellationToken: cancellationToken));
        return (await GetDepartmentAsync(organizationId, id, cancellationToken))!;
    }
    public async Task<DepartmentResponse?> UpdateDepartmentAsync(Guid organizationId, Guid id, UpsertDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        var changed = await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.departments SET unit_id=@UnitId,name=@Name,code=@Code,type=@Type,updated_at=now() WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL", new { organizationId, id, request.UnitId, request.Name, request.Code, request.Type }, cancellationToken: cancellationToken));
        return changed == 0 ? null : await GetDepartmentAsync(organizationId, id, cancellationToken);
    }
    public async Task<bool> SetDepartmentStatusAsync(Guid organizationId, Guid id, string status, CancellationToken cancellationToken = default)
    {
        using var c = factory.Create();
        return await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.departments SET status=@status,updated_at=now() WHERE organization_id=@organizationId AND id=@id AND deleted_at IS NULL", new { organizationId, id, status }, cancellationToken: cancellationToken)) > 0;
    }
}
