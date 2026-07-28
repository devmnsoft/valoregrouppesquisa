using Dapper;
using Valora.Application.Contracts;
using Valora.Application.ReadModels;

namespace Valora.Infrastructure.Repositories;

public sealed class SessionRepository(IDbConnectionFactory factory) : ISessionRepository
{
    public async Task<Guid> CreateAsync(Guid userId, Guid organizationId, DateTimeOffset expiresAt, string? ipHash, string? userAgent)
    {
        using var connection = factory.Create();
        return await connection.ExecuteScalarAsync<Guid>("""
            INSERT INTO valorapesquisa.user_sessions(organization_id,user_id,expires_at,last_used_at,ip_hash,user_agent)
            VALUES (@organizationId,@userId,@expiresAt,now(),@ipHash,@userAgent) RETURNING id
            """, new { userId, organizationId, expiresAt, ipHash, userAgent });
    }

    public async Task<AuthenticationSessionRecord?> GetAsync(Guid sessionId)
    {
        using var connection = factory.Create();
        return await connection.QuerySingleOrDefaultAsync<AuthenticationSessionRecord>("""
            SELECT id,user_id AS UserId,organization_id AS OrganizationId,created_at AS CreatedAt,
                   COALESCE(last_used_at,created_at) AS LastUsedAt,expires_at AS ExpiresAt,revoked_at AS RevokedAt
            FROM valorapesquisa.user_sessions WHERE id=@sessionId
            """, new { sessionId });
    }

    public async Task<IReadOnlyList<AuthenticationSessionRecord>> ListAsync(Guid userId)
    {
        using var connection = factory.Create();
        var rows = await connection.QueryAsync<AuthenticationSessionRecord>("""
            SELECT id,user_id AS UserId,organization_id AS OrganizationId,created_at AS CreatedAt,
                   COALESCE(last_used_at,created_at) AS LastUsedAt,expires_at AS ExpiresAt,revoked_at AS RevokedAt
            FROM valorapesquisa.user_sessions
            WHERE user_id=@userId AND revoked_at IS NULL AND expires_at>now() ORDER BY last_used_at DESC NULLS LAST
            """, new { userId });
        return rows.AsList();
    }

    public async Task RevokeAsync(Guid sessionId, Guid userId, string reason)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync("UPDATE valorapesquisa.user_sessions SET revoked_at=now(),status='revoked',revocation_reason=@reason WHERE id=@sessionId AND user_id=@userId AND revoked_at IS NULL", new { sessionId, userId, reason });
    }

    public async Task RevokeAllAsync(Guid userId, string reason)
    {
        using var connection = factory.Create();
        await connection.ExecuteAsync("UPDATE valorapesquisa.user_sessions SET revoked_at=now(),status='revoked',revocation_reason=@reason WHERE user_id=@userId AND revoked_at IS NULL", new { userId, reason });
    }
}
