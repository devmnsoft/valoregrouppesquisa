using Dapper;
using Valora.Application.Access;

namespace Valora.Infrastructure.Repositories;

public sealed class AccessAdministrationRepository(Application.Contracts.IDbConnectionFactory factory) : IAccessAdministrationRepository
{
    private const string RoleProjection = """r.id Id,r.code Code,r.name Name,r.description Description,r.is_system IsSystem,r.status Status,r.version Version,(SELECT count(*)::int FROM valorapesquisa.user_roles ur WHERE ur.role_id=r.id) UserCount,COALESCE((SELECT array_agg(p.code ORDER BY p.code) FROM valorapesquisa.role_permissions rp JOIN valorapesquisa.permissions p ON p.id=rp.permission_id WHERE rp.role_id=r.id),ARRAY[]::text[]) Permissions""";

    public async Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(Guid org, CancellationToken ct) { using var c=factory.Create(); return (await c.QueryAsync<AccessRoleDto>(new CommandDefinition($"SELECT {RoleProjection} FROM valorapesquisa.roles r WHERE r.deleted_at IS NULL AND (r.organization_id IS NULL OR r.organization_id=@org) ORDER BY r.is_system DESC,r.name",new{org},cancellationToken:ct))).AsList(); }
    public async Task<AccessRoleDto?> GetRoleAsync(Guid org, Guid id, CancellationToken ct) { using var c=factory.Create(); return await c.QuerySingleOrDefaultAsync<AccessRoleDto>(new CommandDefinition($"SELECT {RoleProjection} FROM valorapesquisa.roles r WHERE r.id=@id AND r.deleted_at IS NULL AND (r.organization_id IS NULL OR r.organization_id=@org)",new{org,id},cancellationToken:ct)); }
    public async Task<IReadOnlyList<AccessPermissionDto>> ListPermissionsAsync(CancellationToken ct) { using var c=factory.Create(); return (await c.QueryAsync<AccessPermissionDto>(new CommandDefinition("SELECT code Code,name Name,description Description,module_code ModuleCode,functional_group FunctionalGroup,risk_level RiskLevel,assignable_to_custom_roles AssignableToCustomRoles,display_order DisplayOrder,status Status FROM valorapesquisa.permissions WHERE status='active' ORDER BY module_code,display_order,name",cancellationToken:ct))).AsList(); }

    public async Task<AccessRoleDto> CreateRoleAsync(Guid org, Guid actor, CreateAccessRoleRequest request, CancellationToken ct)
    {
        using var c=factory.Create(); c.Open(); using var tx=c.BeginTransaction();
        var ids=await ResolvePermissions(c,tx,request.Permissions,ct);
        var id=await c.ExecuteScalarAsync<Guid>(new CommandDefinition("INSERT INTO valorapesquisa.roles(organization_id,code,name,description,status,is_system,version) VALUES(@org,lower(trim(@Code)),trim(@Name),@Description,'active',false,1) RETURNING id",new{org,request.Code,request.Name,request.Description},tx,cancellationToken:ct));
        await InsertPermissions(c,tx,id,ids,ct); await Audit(c,tx,org,actor,"access.role.created",id,null,request.Permissions,null,ct); tx.Commit();
        return (await GetRoleAsync(org,id,ct))!;
    }

    public async Task<AccessRoleDto?> UpdateRoleAsync(Guid org, Guid actor, Guid id, UpdateAccessRoleRequest request, CancellationToken ct)
    {
        using var c=factory.Create(); c.Open(); using var tx=c.BeginTransaction();
        var changed=await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.roles SET name=trim(@Name),description=@Description,status=@Status,version=version+1,updated_at=now() WHERE id=@id AND organization_id=@org AND is_system=false AND deleted_at IS NULL AND version=@ExpectedVersion",new{org,id,request.Name,request.Description,request.Status,request.ExpectedVersion},tx,cancellationToken:ct));
        if(changed!=1)return null; await Audit(c,tx,org,actor,"access.role.updated",id,null,null,null,ct); tx.Commit(); return await GetRoleAsync(org,id,ct);
    }

    public async Task<bool> DeleteRoleAsync(Guid org, Guid actor, Guid id, CancellationToken ct)
    {
        using var c=factory.Create(); c.Open(); using var tx=c.BeginTransaction();
        var changed=await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.roles r SET deleted_at=now(),status='inactive',version=version+1 WHERE r.id=@id AND r.organization_id=@org AND r.is_system=false AND r.deleted_at IS NULL AND NOT EXISTS(SELECT 1 FROM valorapesquisa.user_roles ur WHERE ur.role_id=r.id)",new{org,id},tx,cancellationToken:ct));
        if(changed!=1)return false; await Audit(c,tx,org,actor,"access.role.deleted",id,null,null,null,ct); tx.Commit(); return true;
    }

    public async Task<AccessRoleDto?> ReplacePermissionsAsync(Guid org, Guid actor, Guid id, ReplaceRolePermissionsRequest request, CancellationToken ct)
    {
        using var c=factory.Create(); c.Open(); using var tx=c.BeginTransaction();
        var role=await c.QuerySingleOrDefaultAsync<RoleLock>(new CommandDefinition("SELECT id Id,is_system IsSystem,version Version FROM valorapesquisa.roles WHERE id=@id AND organization_id=@org AND deleted_at IS NULL FOR UPDATE",new{org,id},tx,cancellationToken:ct));
        if(role is null||role.IsSystem||role.Version!=request.ExpectedVersion)return null;
        var before=(await c.QueryAsync<string>(new CommandDefinition("SELECT p.code FROM valorapesquisa.role_permissions rp JOIN valorapesquisa.permissions p ON p.id=rp.permission_id WHERE rp.role_id=@id ORDER BY p.code",new{id},tx,cancellationToken:ct))).AsList();
        var ids=await ResolvePermissions(c,tx,request.Permissions,ct);
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM valorapesquisa.role_permissions WHERE role_id=@id",new{id},tx,cancellationToken:ct)); await InsertPermissions(c,tx,id,ids,ct);
        await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.roles SET version=version+1,updated_at=now() WHERE id=@id",new{id},tx,cancellationToken:ct));
        await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.users u SET access_version=access_version+1,updated_at=now() WHERE organization_id=@org AND EXISTS(SELECT 1 FROM valorapesquisa.user_roles ur WHERE ur.user_id=u.id AND ur.role_id=@id); UPDATE valorapesquisa.user_sessions s SET revoked_at=COALESCE(revoked_at,now()) WHERE organization_id=@org AND EXISTS(SELECT 1 FROM valorapesquisa.user_roles ur WHERE ur.user_id=s.user_id AND ur.role_id=@id)",new{org,id},tx,cancellationToken:ct));
        await Audit(c,tx,org,actor,"access.role.permissions_replaced",id,before,request.Permissions,request.Reason,ct); tx.Commit(); return await GetRoleAsync(org,id,ct);
    }

    public async Task<EffectiveAccessDto?> GetEffectiveAccessAsync(Guid org, Guid user, CancellationToken ct)
    {
        using var c=factory.Create();
        var status=await c.QuerySingleOrDefaultAsync<UserState>(new CommandDefinition("SELECT u.status UserStatus,o.status OrganizationStatus,COALESCE((SELECT s.status FROM valorapesquisa.subscriptions s WHERE s.organization_id=@org AND s.deleted_at IS NULL ORDER BY s.created_at DESC LIMIT 1),'inactive') SubscriptionStatus FROM valorapesquisa.users u JOIN valorapesquisa.organizations o ON o.id=u.organization_id WHERE u.id=@user AND u.organization_id=@org AND u.deleted_at IS NULL",new{org,user},cancellationToken:ct)); if(status is null)return null;
        var modules=(await c.QueryAsync<string>(new CommandDefinition("SELECT m.code FROM valorapesquisa.modules m JOIN valorapesquisa.organization_modules om ON (om.module_id=m.id OR om.module_code=m.code) WHERE om.organization_id=@org AND om.enabled AND m.status='active' AND m.deleted_at IS NULL ORDER BY m.code",new{org},cancellationToken:ct))).AsList();
        var candidate=(await c.QueryAsync<PermissionState>(new CommandDefinition("SELECT DISTINCT p.code Code,p.module_code ModuleCode FROM valorapesquisa.user_roles ur JOIN valorapesquisa.roles r ON r.id=ur.role_id AND r.deleted_at IS NULL AND r.status='active' JOIN valorapesquisa.role_permissions rp ON rp.role_id=r.id JOIN valorapesquisa.permissions p ON p.id=rp.permission_id AND p.status='active' WHERE ur.user_id=@user AND (r.organization_id IS NULL OR r.organization_id=@org)",new{org,user},cancellationToken:ct))).AsList();
        var enabled=status.UserStatus=="active"&&status.OrganizationStatus=="active"&&status.SubscriptionStatus is "active" or "trialing";
        var granted=candidate.Where(p=>enabled&&p.ModuleCode is not null&&modules.Contains(p.ModuleCode)).Select(p=>p.Code).Order().ToArray();
        var blocked=candidate.Where(p=>!granted.Contains(p.Code)).Select(p=>new BlockedAccessDto(p.Code,!enabled?"Conta, organização ou assinatura inativa.":p.ModuleCode is null?"Permissão legada aguardando classificação.":"Módulo indisponível no plano ou na organização.")).ToArray();
        var scopes=(await c.QueryAsync<AccessScopeDto>(new CommandDefinition("SELECT scope_type Type,scope_id Id,scope_type||' · '||scope_id::text Label FROM valorapesquisa.user_scopes WHERE organization_id=@org AND user_id=@user AND deleted_at IS NULL ORDER BY scope_type",new{org,user},cancellationToken:ct))).AsList(); return new(user,granted,blocked,scopes,modules);
    }

    private static async Task<Guid[]> ResolvePermissions(System.Data.IDbConnection c,System.Data.IDbTransaction tx,IReadOnlyList<string> codes,CancellationToken ct) { var normalized=codes.Distinct(StringComparer.Ordinal).ToArray(); var ids=(await c.QueryAsync<Guid>(new CommandDefinition("SELECT id FROM valorapesquisa.permissions WHERE code=ANY(@normalized) AND status='active' AND assignable_to_custom_roles",new{normalized},tx,cancellationToken:ct))).ToArray(); if(ids.Length!=normalized.Length)throw new InvalidOperationException("Permissão inexistente, inativa ou não atribuível."); return ids; }
    private static Task InsertPermissions(System.Data.IDbConnection c,System.Data.IDbTransaction tx,Guid role,Guid[] ids,CancellationToken ct)=>c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.role_permissions(role_id,permission_id) SELECT @role,unnest(@ids::uuid[])",new{role,ids},tx,cancellationToken:ct));
    private static Task Audit(System.Data.IDbConnection c,System.Data.IDbTransaction tx,Guid org,Guid actor,string action,Guid role,IReadOnlyList<string>? before,IReadOnlyList<string>? after,string? reason,CancellationToken ct)=>c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json,correlation_id) VALUES(@org,@actor,@action,'role',CAST(@role AS text),'Permissões de perfil alteradas',jsonb_build_object('before',@before,'after',@after,'reason',@reason),current_setting('valora.correlation_id',true))",new{org,actor,action,role,before=before?.ToArray(),after=after?.ToArray(),reason},tx,cancellationToken:ct));
    private sealed record RoleLock(Guid Id,bool IsSystem,long Version); private sealed record PermissionState(string Code,string? ModuleCode); private sealed record UserState(string UserStatus,string OrganizationStatus,string SubscriptionStatus);
}
