using Dapper;
using Microsoft.Extensions.Logging;
using Valora.Application.Contracts;
using Valora.Application.Security;

namespace Valora.Infrastructure.Repositories;

public sealed class UserRepository(IDbConnectionFactory f, ILogger<UserRepository> logger) : IUserRepository
{
    public async Task<dynamic?> GetByEmailAsync(string email)
    {
        try
        {
            using var c = f.Create();
            return await c.QuerySingleOrDefaultAsync(
                "SELECT * FROM valorapesquisa.users WHERE lower(email)=lower(@email) AND is_deleted=false",
                new { email });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao buscar usuário por e-mail. Email={Email}", LogSanitizer.MaskEmail(email));
            throw;
        }
    }

    public async Task<dynamic?> GetAsync(Guid id)
    {
        try
        {
            using var c = f.Create();
            return await c.QuerySingleOrDefaultAsync(
                "SELECT id,organization_id,name,email,role,status FROM valorapesquisa.users WHERE id=@id",
                new { id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao buscar usuário. UserId={UserId}", id);
            throw;
        }
    }

    public async Task<Guid> CreateAsync(Guid organizationId, string name, string email, string passwordHash, string role)
    {
        try
        {
            using var c = f.Create();
            const string sql = "INSERT INTO valorapesquisa.users(organization_id,name,email,password_hash,role) VALUES (@organizationId,@name,@email,@passwordHash,@role) RETURNING id";
            return await c.ExecuteScalarAsync<Guid>(sql, new { organizationId, name, email, passwordHash, role });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar usuário. OrganizationId={OrganizationId} Email={Email} Role={Role}", organizationId, LogSanitizer.MaskEmail(email), role);
            throw;
        }
    }

    public async Task TouchLoginAsync(Guid id)
    {
        try
        {
            using var c = f.Create();
            await c.ExecuteAsync("UPDATE valorapesquisa.users SET last_login_at=now() WHERE id=@id", new { id });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar último login. UserId={UserId}", id);
            throw;
        }
    }

    public async Task CreatePasswordResetTokenAsync(Guid userId, string tokenHash, DateTimeOffset expiresAt, string? requestIpHash, string? userAgent)
    {
        try
        {
            using var c = f.Create();
            const string sql = "UPDATE valorapesquisa.password_reset_tokens SET used_at=now(), updated_at=now() WHERE user_id=@userId AND used_at IS NULL AND is_deleted=false; INSERT INTO valorapesquisa.password_reset_tokens(user_id,token_hash,expires_at,request_ip_hash,user_agent) VALUES (@userId,@tokenHash,@expiresAt,@requestIpHash,@userAgent)";
            await c.ExecuteAsync(sql, new { userId, tokenHash, expiresAt, requestIpHash, userAgent });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar credencial temporária de acesso. UserId={UserId}", userId);
            throw;
        }
    }

    public async Task<dynamic?> GetValidPasswordResetTokenAsync(string tokenHash)
    {
        try
        {
            using var c = f.Create();
            const string sql = "SELECT id,user_id,expires_at,used_at FROM valorapesquisa.password_reset_tokens WHERE token_hash=@tokenHash AND used_at IS NULL AND expires_at>now() AND is_deleted=false";
            return await c.QuerySingleOrDefaultAsync(sql, new { tokenHash });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao validar credencial temporária de acesso.");
            throw;
        }
    }

    public async Task MarkPasswordResetTokenUsedAsync(Guid tokenId)
    {
        try
        {
            using var c = f.Create();
            await c.ExecuteAsync(
                "UPDATE valorapesquisa.password_reset_tokens SET used_at=now(), updated_at=now() WHERE id=@tokenId",
                new { tokenId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao marcar token de recuperação como usado. TokenId={TokenId}", tokenId);
            throw;
        }
    }

    public async Task UpdatePasswordHashAsync(Guid userId, string passwordHash)
    {
        try
        {
            using var c = f.Create();
            const string sql = "UPDATE valorapesquisa.users SET password_hash=@passwordHash, updated_at=now() WHERE id=@userId";
            await c.ExecuteAsync(sql, new { userId, passwordHash });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao atualizar credencial de acesso. UserId={UserId}", userId);
            throw;
        }
    }


    public async Task<IReadOnlyList<dynamic>> ListByOrganizationAsync(Guid organizationId, bool includeGlobal = false)
    {
        try
        {
            using var c = f.Create();
            var sql = "SELECT u.id,u.organization_id,u.name,u.email,COALESCE(u.role,r.code) AS role,u.status,u.phone,u.last_login_at,u.created_at FROM valorapesquisa.users u LEFT JOIN valorapesquisa.roles r ON r.id=u.role_id WHERE u.is_deleted=false AND (u.organization_id=@organizationId OR @includeGlobal=true) ORDER BY u.created_at DESC";
            return (await c.QueryAsync(sql, new { organizationId, includeGlobal })).ToList();
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao listar usuários. OrganizationId={OrganizationId}", organizationId); throw; }
    }

    public async Task UpdateAsync(Guid organizationId, Guid id, string? name, string? email, string? role, string? phone)
    {
        try
        {
            using var c = f.Create();
            await c.ExecuteAsync("UPDATE valorapesquisa.users SET name=COALESCE(@name,name), email=COALESCE(@email,email), role=COALESCE(@role,role), phone=COALESCE(@phone,phone), updated_at=now() WHERE id=@id AND organization_id=@organizationId AND is_deleted=false", new { organizationId, id, name, email, role, phone });
        }
        catch (Exception ex) { logger.LogError(ex, "Erro ao atualizar usuário. UserId={UserId} OrganizationId={OrganizationId} Email={Email}", id, organizationId, LogSanitizer.MaskEmail(email)); throw; }
    }

    public async Task UpdateStatusAsync(Guid organizationId, Guid id, string status)
    {
        try { using var c = f.Create(); await c.ExecuteAsync("UPDATE valorapesquisa.users SET status=@status, updated_at=now() WHERE id=@id AND organization_id=@organizationId AND is_deleted=false", new { organizationId, id, status }); }
        catch (Exception ex) { logger.LogError(ex, "Erro ao atualizar status de usuário. UserId={UserId} OrganizationId={OrganizationId}", id, organizationId); throw; }
    }

}
