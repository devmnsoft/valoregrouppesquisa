using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;
using Valora.Application.Security;

namespace Valora.Infrastructure.Repositories;

public sealed class UserRepository(IDbConnectionFactory factory, ILogger<UserRepository> logger) : IUserRepository
{
    private sealed class AuthUserRow
    {
        public Guid Id { get; set; }
        public Guid? OrganizationId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? RoleCodesCsv { get; set; }
    }

    private const string UserProjection = """
        u.id AS Id, u.organization_id AS OrganizationId, u.name AS Name, u.email AS Email,
        u.status AS Status, u.phone AS Phone, u.password_reset_required AS PasswordResetRequired,
        u.last_login_at AS LastLoginAt, u.created_at AS CreatedAt, u.updated_at AS UpdatedAt,
        COALESCE((SELECT array_agg(r.code ORDER BY r.code) FROM valorapesquisa.user_roles ur
          JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE ur.user_id=u.id), ARRAY[]::text[]) AS RoleCodes
        """;

    public async Task<UserAuthenticationRecord?> GetByEmailAsync(string email)
    {
        try
        {
            using var connection = factory.Create();
            const string sql = """
                SELECT u.id AS Id, u.organization_id AS OrganizationId, u.name AS Name, u.email AS Email,
                       u.password_hash AS PasswordHash, u.status AS Status, u.phone AS Phone,
                       COALESCE((SELECT string_agg(r.code, ',' ORDER BY r.code)
                         FROM valorapesquisa.user_roles ur JOIN valorapesquisa.roles r ON r.id=ur.role_id
                         WHERE ur.user_id=u.id), '') AS RoleCodesCsv
                FROM valorapesquisa.users u
                WHERE lower(u.email)=lower(@email) AND u.deleted_at IS NULL
                ORDER BY u.updated_at DESC NULLS LAST
                LIMIT 1
                """;
            var row = await connection.QuerySingleOrDefaultAsync<AuthUserRow>(sql, new { email });
            return row is null
                ? null
                : new UserAuthenticationRecord(
                    row.Id,
                    row.OrganizationId,
                    row.Name,
                    row.Email,
                    row.PasswordHash,
                    row.Status,
                    row.Phone,
                    row.RoleCodesCsv ?? string.Empty);
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao buscar usuário por e-mail. Email={Email}", LogSanitizer.MaskEmail(email)); throw; }
    }

    public async Task<UserRecord?> GetAsync(Guid id)
    {
        using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync<UserRecord>($"SELECT {UserProjection} FROM valorapesquisa.users u WHERE u.id=@id AND u.deleted_at IS NULL", new { id });
    }

    public async Task<IReadOnlyList<UserRecord>> ListByOrganizationAsync(Guid organizationId, bool includeGlobal = false)
    {
        using var connection = factory.Create();
        var rows = await connection.QueryAsync<UserRecord>($"SELECT {UserProjection} FROM valorapesquisa.users u WHERE u.deleted_at IS NULL AND (u.organization_id=@organizationId OR @includeGlobal) ORDER BY u.created_at DESC", new { organizationId, includeGlobal });
        return rows.AsList();
    }

    public async Task<Guid> CreateAsync(Guid organizationId, string name, string email, string passwordHash, string role)
    {
        using var connection = factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var userId = await connection.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.users(organization_id,name,email,password_hash) VALUES (@organizationId,@name,@email,@passwordHash) RETURNING id", new { organizationId, name, email, passwordHash }, transaction);
            var roleId = await connection.ExecuteScalarAsync<Guid?>("SELECT id FROM valorapesquisa.roles WHERE code=@role AND deleted_at IS NULL AND (organization_id IS NULL OR organization_id=@organizationId) ORDER BY organization_id NULLS FIRST LIMIT 1", new { role, organizationId }, transaction);
            if (roleId is null) throw new InvalidOperationException("Role de cadastro não configurada.");
            await connection.ExecuteAsync("INSERT INTO valorapesquisa.user_roles(user_id,role_id) VALUES (@userId,@roleId) ON CONFLICT DO NOTHING", new { userId, roleId }, transaction);
            transaction.Commit();
            return userId;
        }
        catch { transaction.Rollback(); throw; }
    }

    public async Task TouchLoginAsync(Guid id) { using var c=factory.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.users SET last_login_at=now(), updated_at=now() WHERE id=@id AND deleted_at IS NULL",new{id}); }

    public async Task CreatePasswordResetTokenAsync(Guid userId,string tokenHash,DateTimeOffset expiresAt,string? requestIpHash,string? userAgent)
    {
        using var c=factory.Create();
        const string sql="""
          UPDATE valorapesquisa.password_reset_tokens SET used_at=now(),updated_at=now() WHERE user_id=@userId AND used_at IS NULL;
          INSERT INTO valorapesquisa.password_reset_tokens(organization_id,user_id,token_hash,expires_at,request_ip_hash,user_agent)
          SELECT organization_id,id,@tokenHash,@expiresAt,@requestIpHash,@userAgent FROM valorapesquisa.users WHERE id=@userId AND deleted_at IS NULL
          """;
        await c.ExecuteAsync(sql,new{userId,tokenHash,expiresAt,requestIpHash,userAgent});
    }

    public async Task<PasswordResetTokenRecord?> GetValidPasswordResetTokenAsync(string tokenHash)
    {
        using var c=factory.Create();
        return await c.QuerySingleOrDefaultAsync<PasswordResetTokenRecord>("SELECT id AS Id,user_id AS UserId,expires_at AS ExpiresAt,used_at AS UsedAt FROM valorapesquisa.password_reset_tokens WHERE token_hash=@tokenHash AND used_at IS NULL AND expires_at>now()",new{tokenHash});
    }

    public async Task MarkPasswordResetTokenUsedAsync(Guid tokenId) { using var c=factory.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.password_reset_tokens SET used_at=now(),updated_at=now() WHERE id=@tokenId AND used_at IS NULL",new{tokenId}); }
    public async Task UpdatePasswordHashAsync(Guid userId,string passwordHash) { using var c=factory.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.users SET password_hash=@passwordHash,updated_at=now() WHERE id=@userId AND deleted_at IS NULL",new{userId,passwordHash}); }

    public async Task UpdateAsync(Guid organizationId,Guid id,string? name,string? email,string? role,string? phone)
    {
        if (role is not null) throw new InvalidOperationException("Roles devem ser alteradas pelo fluxo RBAC dedicado.");
        using var c=factory.Create();
        await c.ExecuteAsync("UPDATE valorapesquisa.users SET name=COALESCE(@name,name),email=COALESCE(@email,email),phone=COALESCE(@phone,phone),updated_at=now() WHERE id=@id AND organization_id=@organizationId AND deleted_at IS NULL",new{organizationId,id,name,email,phone});
    }

    public async Task UpdateStatusAsync(Guid organizationId,Guid id,string status) { using var c=factory.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.users SET status=@status,updated_at=now() WHERE id=@id AND organization_id=@organizationId AND deleted_at IS NULL",new{organizationId,id,status}); }
}
