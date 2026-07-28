using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;

namespace Valora.Infrastructure.Repositories;

public sealed class RefreshTokenRepository(IDbConnectionFactory factory) : IRefreshTokenRepository
{
    public async Task<Guid> CreateFamilyAsync(Guid sessionId)
    {
        using var connection = factory.Create();
        return await connection.ExecuteScalarAsync<Guid>("INSERT INTO valorapesquisa.refresh_token_families(session_id) VALUES (@sessionId) RETURNING id", new { sessionId });
    }

    public async Task CreateAsync(Guid id, Guid familyId, Guid sessionId, Guid userId, Guid organizationId, string tokenHash, DateTimeOffset expiresAt)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync("""
            INSERT INTO valorapesquisa.refresh_tokens(id,family_id,session_id,user_id,organization_id,token_hash,expires_at)
            VALUES (@id,@familyId,@sessionId,@userId,@organizationId,@tokenHash,@expiresAt)
            """, new { id, familyId, sessionId, userId, organizationId, tokenHash, expiresAt });
    }

    public async Task<RefreshTokenUseResult> RotateAsync(string currentHash, Guid replacementId, string replacementHash, DateTimeOffset replacementExpiresAt)
    {
        using var connection = factory.Create();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        var current = await connection.QuerySingleOrDefaultAsync<RefreshTokenRecord>("""
            SELECT rt.id,rt.family_id AS FamilyId,rt.session_id AS SessionId,rt.user_id AS UserId,
                   rt.organization_id AS OrganizationId,u.email AS Email,
                   COALESCE((SELECT r.code FROM valorapesquisa.user_roles ur JOIN valorapesquisa.roles r ON r.id=ur.role_id WHERE ur.user_id=u.id ORDER BY r.code LIMIT 1),'empresa_admin') AS Role,
                   o.default_language_code AS Locale,rt.expires_at AS ExpiresAt,rt.used_at AS UsedAt,rt.revoked_at AS RevokedAt
            FROM valorapesquisa.refresh_tokens rt JOIN valorapesquisa.users u ON u.id=rt.user_id
            JOIN valorapesquisa.organizations o ON o.id=rt.organization_id WHERE rt.token_hash=@currentHash FOR UPDATE OF rt
            """, new { currentHash }, transaction);
        if (current is null || current.ExpiresAt <= DateTimeOffset.UtcNow || current.RevokedAt is not null)
        {
            transaction.Rollback();
            return new(RefreshTokenUseStatus.Invalid, current);
        }
        if (current.UsedAt is not null)
        {
            await RevokeFamilyAsync(connection, transaction, current.FamilyId, current.SessionId, "refresh_token_reuse");
            transaction.Commit();
            return new(RefreshTokenUseStatus.Reused, current);
        }
        var sessionActive = await connection.ExecuteScalarAsync<bool>("SELECT EXISTS(SELECT 1 FROM valorapesquisa.user_sessions WHERE id=@id AND revoked_at IS NULL AND expires_at>now())", new { id = current.SessionId }, transaction);
        if (!sessionActive) { transaction.Rollback(); return new(RefreshTokenUseStatus.Invalid, current); }
        await connection.ExecuteAsync("""
            INSERT INTO valorapesquisa.refresh_tokens(id,family_id,session_id,user_id,organization_id,token_hash,expires_at)
            VALUES (@replacementId,@FamilyId,@SessionId,@UserId,@OrganizationId,@replacementHash,@replacementExpiresAt);
            UPDATE valorapesquisa.refresh_tokens SET used_at=now(),replaced_by_id=@replacementId WHERE id=@Id;
            UPDATE valorapesquisa.user_sessions SET last_used_at=now() WHERE id=@SessionId;
            """, new { replacementId, replacementHash, replacementExpiresAt, current.FamilyId, current.SessionId, current.UserId, current.OrganizationId, current.Id }, transaction);
        transaction.Commit();
        return new(RefreshTokenUseStatus.Rotated, current);
    }

    public async Task RevokeByHashAsync(string tokenHash, string reason)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync("""
            WITH revoked AS (UPDATE valorapesquisa.refresh_tokens SET revoked_at=now(),revocation_reason=@reason WHERE token_hash=@tokenHash AND revoked_at IS NULL RETURNING session_id)
            UPDATE valorapesquisa.user_sessions SET revoked_at=now(),status='revoked',revocation_reason=@reason WHERE id IN (SELECT session_id FROM revoked)
            """, new { tokenHash, reason });
    }

    private static async Task RevokeFamilyAsync(System.Data.IDbConnection connection, System.Data.IDbTransaction transaction, Guid familyId, Guid sessionId, string reason)
    {
        await connection.ExecuteAsync("UPDATE valorapesquisa.refresh_token_families SET revoked_at=now(),revocation_reason=@reason WHERE id=@familyId; UPDATE valorapesquisa.refresh_tokens SET revoked_at=COALESCE(revoked_at,now()),revocation_reason=@reason WHERE family_id=@familyId; UPDATE valorapesquisa.user_sessions SET revoked_at=now(),status='revoked',revocation_reason=@reason WHERE id=@sessionId", new { familyId, sessionId, reason }, transaction);
    }
}
