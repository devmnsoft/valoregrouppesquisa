using Dapper;
using Valora.Application.Contracts;
using Valora.Application.DTOs;
using Valora.Application.Exceptions;
namespace Valora.Infrastructure.Repositories;

public sealed class UserAdministrationRepository(IDbConnectionFactory factory) : IUserAdministrationRepository {
    // Colunas text[] chegam ao Dapper como System.Array (nunca casam com string[] no construtor posicional) e
    // timestamptz chega como DateTime — materializamos RoleCodes como CSV e datas como DateTime, convertendo no ToResponse.
    private const string Projection = """u.id Id,u.name Name,u.email Email,u.phone Phone,u.status Status,COALESCE((SELECT string_agg(r.code, ',' ORDER BY r.code) FROM valorapesquisa.user_roles ur JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE ur.user_id=u.id),'') RoleCodesCsv,u.last_login_at LastLoginAt,u.created_at CreatedAt,u.updated_at UpdatedAt""";
    public async Task<(IReadOnlyList<UserAdministrationResponse> Items, long Total)> ListAsync(Guid org, UserListQuery q, CancellationToken ct = default) { using var c = factory.Create(); var scopeType = q.DepartmentId.HasValue ? "department" : q.UnitId.HasValue ? "unit" : q.LegalEntityId.HasValue ? "legal_entity" : q.BusinessGroupId.HasValue ? "business_group" : null; var scopeId = q.DepartmentId ?? q.UnitId ?? q.LegalEntityId ?? q.BusinessGroupId; var where = """u.organization_id=@org AND u.deleted_at IS NULL AND (@Search::text IS NULL OR u.name ILIKE '%'||@Search::text||'%' OR u.email ILIKE '%'||@Search::text||'%') AND (@Status::text IS NULL OR u.status=@Status::text) AND (@RoleCode::text IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.user_roles x JOIN valorapesquisa.roles r ON r.id=x.role_id WHERE x.user_id=u.id AND r.code=@RoleCode::text)) AND (@scopeId::uuid IS NULL OR EXISTS(SELECT 1 FROM valorapesquisa.user_scopes s WHERE s.organization_id=@org AND s.user_id=u.id AND s.scope_type=@scopeType::text AND s.scope_id=@scopeId::uuid AND s.deleted_at IS NULL))"""; var order = q.SortBy.ToLowerInvariant() switch { "email" => "u.email", "status" => "u.status", "createdat" => "u.created_at", "lastloginat" => "u.last_login_at", _ => "u.name" }; var direction = q.SortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase) ? "DESC" : "ASC"; var p = new { org, q.Search, q.Status, q.RoleCode, scopeId, scopeType, offset = (q.Page - 1) * q.PageSize, q.PageSize }; var rows = (await c.QueryAsync<UserRow>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.users u WHERE {where} ORDER BY {order} {direction},u.id LIMIT @PageSize OFFSET @offset", p, cancellationToken: ct))).AsList(); var total = await c.ExecuteScalarAsync<long>(new CommandDefinition($"SELECT count(*) FROM valorapesquisa.users u WHERE {where}", p, cancellationToken: ct)); var items = new List<UserAdministrationResponse>(); foreach (var row in rows) items.Add(ToResponse(row, await Scopes(c, org, row.Id, ct))); return (items, total); }
    public async Task<UserAdministrationResponse?> GetAsync(Guid org, Guid id, CancellationToken ct = default) { using var c = factory.Create(); var r = await c.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.users u WHERE u.organization_id=@org AND u.id=@id AND u.deleted_at IS NULL", new { org, id }, cancellationToken: ct)); return r is null ? null : ToResponse(r, await Scopes(c, org, id, ct)); }
    public async Task<UserAdministrationResponse?> CreateAdministrativeAsync(Guid org, Guid actor, CreateAdministrativeUserRequest r, CancellationToken ct = default) {
        using var c = factory.Create();
        c.Open();
        using var tx = c.BeginTransaction();
        var roleId = await c.ExecuteScalarAsync<Guid?>(new CommandDefinition("SELECT id FROM valorapesquisa.roles WHERE code=@RoleCode AND status='active' AND deleted_at IS NULL AND (organization_id IS NULL OR organization_id=@org) ORDER BY (organization_id IS NULL), name LIMIT 1", new { org, r.RoleCode }, tx, cancellationToken: ct));
        if (roleId is null) return null;
        var userId = await c.ExecuteScalarAsync<Guid>(new CommandDefinition("INSERT INTO valorapesquisa.users(organization_id,name,email,password_hash) VALUES(@org,trim(@Name),@Email,@PasswordHash) RETURNING id", new { org, r.Name, r.Email, r.PasswordHash }, tx, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.user_roles(user_id,role_id) VALUES(@userId,@roleId)", new { userId, roleId }, tx, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json,correlation_id,module) VALUES(@org,@actor,'admin.user.created','user',CAST(@userId AS text),@message,jsonb_build_object('role',@RoleCode),current_setting('valora.correlation_id',true),'admin')", new { org, actor, userId, message = $"Usuário {r.Email} criado com papel {r.RoleCode}.", r.RoleCode }, tx, cancellationToken: ct));
        tx.Commit();
        return await GetAsync(org, userId, ct);
    }
    public async Task<bool> UpdateAsync(Guid org, Guid id, UpdateUserRequest r, CancellationToken ct = default) { using var c = factory.Create(); return await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.users SET name=trim(@Name),phone=@Phone,updated_at=now() WHERE organization_id=@org AND id=@id AND deleted_at IS NULL", new { org, id, r.Name, r.Phone }, cancellationToken: ct)) == 1; }
    public async Task<bool> UpdateStatusAsync(Guid org, Guid id, string status, CancellationToken ct = default) {
        using var c = factory.Create();
        c.Open();
        using var tx = c.BeginTransaction();
        await LockAdministratorSetAsync(c, tx, org, ct);
        var current = await c.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.users u WHERE u.organization_id=@org AND u.id=@id AND u.deleted_at IS NULL FOR UPDATE", new { org, id }, tx, cancellationToken: ct));
        if (current is null) { tx.Rollback(); return false; }
        if (!status.Equals("active", StringComparison.OrdinalIgnoreCase) && IsActiveAdministrator(current)) {
            var adminCount = await CountAdministratorsAsync(c, tx, org, ct);
            if (adminCount <= 1) throw LastAdministrator();
        }
        var statusChanged = !current.Status.Equals(status, StringComparison.OrdinalIgnoreCase);
        await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.users SET status=@status,access_version=access_version+CASE WHEN @statusChanged THEN 1 ELSE 0 END,updated_at=now() WHERE organization_id=@org AND id=@id AND deleted_at IS NULL", new { org, id, status, statusChanged }, tx, cancellationToken: ct));
        if (statusChanged)
            await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.user_sessions SET revoked_at=COALESCE(revoked_at,now()) WHERE organization_id=@org AND user_id=@id AND revoked_at IS NULL", new { org, id }, tx, cancellationToken: ct));
        tx.Commit();
        return true;
    }
    public async Task<UserAdministrationResponse?> UpdateAdministrativeAsync(Guid org, Guid actor, Guid id, UpdateAdministrativeUserRequest r, CancellationToken ct = default) {
        using var c = factory.Create();
        c.Open();
        using var tx = c.BeginTransaction();
        await LockAdministratorSetAsync(c, tx, org, ct);
        var current = await c.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.users u WHERE u.organization_id=@org AND u.id=@id AND u.deleted_at IS NULL FOR UPDATE", new { org, id }, tx, cancellationToken: ct));
        if (current is null) return null;
        var normalized = r.RoleCodes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var roleRows = (await c.QueryAsync<RoleAssignmentRow>(new CommandDefinition("SELECT DISTINCT ON (code) id Id,code Code FROM valorapesquisa.roles WHERE code=ANY(@codes) AND status='active' AND deleted_at IS NULL AND (organization_id IS NULL OR organization_id=@org) ORDER BY code,(organization_id IS NULL),name", new { org, codes = normalized }, tx, cancellationToken: ct))).AsList();
        if (roleRows.Count != normalized.Length) throw new InvalidOperationException("Uma ou mais roles são inválidas.");

        var beforeRoles = current.RoleCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var wasAdministrator = beforeRoles.Any(IsAdministrativeRole);
        var remainsAdministrator = r.Status == "active" && normalized.Any(IsAdministrativeRole);
        if (wasAdministrator && !remainsAdministrator) {
            var adminCount = await CountAdministratorsAsync(c, tx, org, ct);
            if (adminCount <= 1) throw LastAdministrator();
        }

        var afterRoles = normalized.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesChanged = !beforeRoles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).SequenceEqual(afterRoles, StringComparer.OrdinalIgnoreCase);
        var statusChanged = !current.Status.Equals(r.Status, StringComparison.OrdinalIgnoreCase);
        var accessChanged = rolesChanged || statusChanged;
        await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.users SET name=trim(@Name),phone=@Phone,status=@Status,access_version=access_version+CASE WHEN @accessChanged THEN 1 ELSE 0 END,updated_at=now() WHERE organization_id=@org AND id=@id AND deleted_at IS NULL", new { org, id, r.Name, r.Phone, r.Status, accessChanged }, tx, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM valorapesquisa.user_roles WHERE user_id=@id; INSERT INTO valorapesquisa.user_roles(user_id,role_id) SELECT @id,unnest(@roleIds::uuid[])", new { id, roleIds = roleRows.Select(role => role.Id).ToArray() }, tx, cancellationToken: ct));
        if (accessChanged)
            await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.user_sessions SET revoked_at=COALESCE(revoked_at,now()) WHERE organization_id=@org AND user_id=@id AND revoked_at IS NULL", new { org, id }, tx, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition("INSERT INTO valorapesquisa.audit_logs(organization_id,user_id,action,entity_type,entity_id,message,metadata_json,correlation_id,module) VALUES(@org,@actor,'admin.user.updated','user',CAST(@id AS text),@message,jsonb_build_object('before_roles',@beforeRoles,'after_roles',@afterRoles,'before_status',@beforeStatus,'after_status',@afterStatus,'access_changed',@accessChanged),current_setting('valora.correlation_id',true),'admin')", new { org, actor, id, message = $"Usuário {current.Email} atualizado em operação administrativa atômica.", beforeRoles, afterRoles, beforeStatus = current.Status, afterStatus = r.Status, accessChanged }, tx, cancellationToken: ct));
        tx.Commit();
        return await GetAsync(org, id, ct);
    }
    public async Task<int> CountAdministratorsAsync(Guid org, CancellationToken ct = default) { using var c = factory.Create(); return await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(DISTINCT u.id)::int FROM valorapesquisa.users u JOIN valorapesquisa.user_roles ur ON ur.user_id=u.id JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE u.organization_id=@org AND u.status='active' AND u.deleted_at IS NULL AND r.status='active' AND r.deleted_at IS NULL AND r.code IN('admin_valora','admin_grupo','empresa_admin')", new { org }, cancellationToken: ct)); }
    public async Task<bool> IsAdministratorAsync(Guid org, Guid id, CancellationToken ct = default) { using var c = factory.Create(); return await c.ExecuteScalarAsync<bool>(new CommandDefinition("SELECT EXISTS(SELECT 1 FROM valorapesquisa.users u JOIN valorapesquisa.user_roles ur ON ur.user_id=u.id JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE u.organization_id=@org AND u.id=@id AND r.status='active' AND r.deleted_at IS NULL AND r.code IN('admin_valora','admin_grupo','empresa_admin'))", new { org, id }, cancellationToken: ct)); }
    public async Task ReplaceRolesAsync(Guid org, Guid id, IReadOnlyList<string> codes, CancellationToken ct = default) {
        using var c = factory.Create();
        c.Open();
        using var tx = c.BeginTransaction();
        await LockAdministratorSetAsync(c, tx, org, ct);
        var current = await c.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition($"SELECT {Projection} FROM valorapesquisa.users u WHERE u.organization_id=@org AND u.id=@id AND u.deleted_at IS NULL FOR UPDATE", new { org, id }, tx, cancellationToken: ct));
        if (current is null) throw new InvalidOperationException("Usuário não encontrado.");
        var normalized = codes.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var roleRows = (await c.QueryAsync<RoleAssignmentRow>(new CommandDefinition("SELECT DISTINCT ON (code) id Id,code Code FROM valorapesquisa.roles WHERE code=ANY(@codes) AND status='active' AND deleted_at IS NULL AND (organization_id IS NULL OR organization_id=@org) ORDER BY code,(organization_id IS NULL),name", new { org, codes = normalized }, tx, cancellationToken: ct))).AsList();
        if (roleRows.Count != normalized.Length) throw new InvalidOperationException("Uma ou mais roles são inválidas.");
        var beforeRoles = current.RoleCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var wasAdministrator = current.Status == "active" && beforeRoles.Any(IsAdministrativeRole);
        var remainsAdministrator = current.Status == "active" && normalized.Any(IsAdministrativeRole);
        if (wasAdministrator && !remainsAdministrator && await CountAdministratorsAsync(c, tx, org, ct) <= 1)
            throw LastAdministrator();
        var afterRoles = normalized.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).ToArray();
        var rolesChanged = !beforeRoles.OrderBy(role => role, StringComparer.OrdinalIgnoreCase).SequenceEqual(afterRoles, StringComparer.OrdinalIgnoreCase);
        await c.ExecuteAsync(new CommandDefinition("DELETE FROM valorapesquisa.user_roles WHERE user_id=@id; INSERT INTO valorapesquisa.user_roles(user_id,role_id) SELECT @id,unnest(@found)", new { id, found = roleRows.Select(role => role.Id).ToArray() }, tx, cancellationToken: ct));
        if (rolesChanged) {
            await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.users SET access_version=access_version+1,updated_at=now() WHERE organization_id=@org AND id=@id AND deleted_at IS NULL", new { org, id }, tx, cancellationToken: ct));
            await c.ExecuteAsync(new CommandDefinition("UPDATE valorapesquisa.user_sessions SET revoked_at=COALESCE(revoked_at,now()) WHERE organization_id=@org AND user_id=@id AND revoked_at IS NULL", new { org, id }, tx, cancellationToken: ct));
        }
        tx.Commit();
    }
    public async Task ReplaceScopesAsync(Guid org, Guid id, IReadOnlyList<UserScopeRequest> scopes, CancellationToken ct = default) { using var c = factory.Create(); c.Open(); using var tx = c.BeginTransaction(); foreach (var s in scopes) { var table = s.Type switch { "business_group" => "business_groups", "legal_entity" => "legal_entities", "unit" => "units", "department" => "departments", "organization" => "organizations", _ => throw new InvalidOperationException("Tipo de escopo inválido.") }; if (!await c.ExecuteScalarAsync<bool>($"SELECT EXISTS(SELECT 1 FROM valorapesquisa.{table} WHERE id=@Id AND {(table == "organizations" ? "id" : "organization_id")}=@org AND deleted_at IS NULL)", new { s.Id, org }, tx)) throw new InvalidOperationException("Escopo não pertence à organização."); } await c.ExecuteAsync("UPDATE valorapesquisa.user_scopes SET deleted_at=now() WHERE organization_id=@org AND user_id=@id AND deleted_at IS NULL", new { org, id }, tx); foreach (var s in scopes) await c.ExecuteAsync("INSERT INTO valorapesquisa.user_scopes(organization_id,user_id,scope_type,scope_id) VALUES(@org,@id,@Type,@Id)", new { org, id, s.Type, s.Id }, tx); tx.Commit(); }
    private static async Task<IReadOnlyList<UserScopeResponse>> Scopes(System.Data.IDbConnection c, Guid org, Guid id, CancellationToken ct) { const string sql = """SELECT s.scope_type Type,s.scope_id Id,CASE s.scope_type WHEN 'business_group' THEN bg.name WHEN 'legal_entity' THEN le.legal_name WHEN 'unit' THEN un.name WHEN 'department' THEN d.name ELSE o.public_name END Label,concat_ws(' › ',bg.name,le.trade_name,un.name,d.name) Breadcrumb FROM valorapesquisa.user_scopes s LEFT JOIN valorapesquisa.business_groups bg ON s.scope_type='business_group' AND bg.id=s.scope_id LEFT JOIN valorapesquisa.legal_entities le ON s.scope_type='legal_entity' AND le.id=s.scope_id LEFT JOIN valorapesquisa.units un ON s.scope_type='unit' AND un.id=s.scope_id LEFT JOIN valorapesquisa.departments d ON s.scope_type='department' AND d.id=s.scope_id LEFT JOIN valorapesquisa.organizations o ON s.scope_type='organization' AND o.id=s.scope_id WHERE s.organization_id=@org AND s.user_id=@id AND s.deleted_at IS NULL ORDER BY s.scope_type,Label"""; return (await c.QueryAsync<UserScopeResponse>(new CommandDefinition(sql, new { org, id }, cancellationToken: ct))).AsList(); }
    private static UserAdministrationResponse ToResponse(UserRow x, IReadOnlyList<UserScopeResponse> scopes) => new(x.Id, x.Name, x.Email, x.Phone, x.Status, x.RoleCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries), scopes, ToUtc(x.LastLoginAt), ToUtc(x.CreatedAt), ToUtc(x.UpdatedAt));
    // Npgsql retorna timestamptz como DateTime (Kind=Utc); preservamos o offset zero na resposta pública.
    private static DateTimeOffset ToUtc(DateTime d) => new(d, TimeSpan.Zero);
    private static DateTimeOffset? ToUtc(DateTime? d) => d is null ? null : new DateTimeOffset(d.Value, TimeSpan.Zero);
    private static bool IsAdministrativeRole(string code) => code is "admin_valora" or "admin_grupo" or "empresa_admin";
    private static bool IsActiveAdministrator(UserRow row) => row.Status == "active" && row.RoleCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Any(IsAdministrativeRole);
    private static BusinessRuleAppException LastAdministrator() => new("LAST_ADMINISTRATOR: O último administrador não pode ser desativado, suspenso ou perder papel administrativo.");
    private static Task LockAdministratorSetAsync(System.Data.IDbConnection c, System.Data.IDbTransaction tx, Guid org, CancellationToken ct) =>
        c.ExecuteAsync(new CommandDefinition("SELECT pg_advisory_xact_lock(hashtext(CAST(@org AS text)))", new { org }, tx, cancellationToken: ct));
    private static Task<int> CountAdministratorsAsync(System.Data.IDbConnection c, System.Data.IDbTransaction tx, Guid org, CancellationToken ct) =>
        c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT count(DISTINCT u.id)::int FROM valorapesquisa.users u JOIN valorapesquisa.user_roles ur ON ur.user_id=u.id JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE u.organization_id=@org AND u.status='active' AND u.deleted_at IS NULL AND r.status='active' AND r.deleted_at IS NULL AND r.code IN('admin_valora','admin_grupo','empresa_admin')", new { org }, tx, cancellationToken: ct));
    private sealed record UserRow(Guid Id, string Name, string Email, string? Phone, string Status, string RoleCodesCsv, DateTime? LastLoginAt, DateTime CreatedAt, DateTime? UpdatedAt);
    private sealed record RoleAssignmentRow(Guid Id, string Code);
}
