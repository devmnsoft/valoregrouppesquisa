using Dapper;
using Valora.Application.Contracts;
using Valora.Application.FormalDeliverables;

namespace Valora.Infrastructure.Repositories;

public sealed class ShareLinkRepository(IDbConnectionFactory connections) : IShareLinkRepository
{
    public async Task SaveAsync(ShareLink link, Guid? createdBy, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        await connection.ExecuteAsync(new CommandDefinition("""
            INSERT INTO valorapesquisa.share_links
                (id, organization_id, entity_type, entity_id, token_hash, scope, status, expires_at, created_by, metadata_json)
            VALUES (@Id, @OrganizationId, 'diagnosis', @DiagnosisId, @TokenHash, @Scope, 'active', @ExpiresAt, @CreatedBy,
                    jsonb_build_object('allowDownload', @AllowDownload))
            """, new { link.Id, link.OrganizationId, link.DiagnosisId, link.TokenHash,
                Scope = link.AllowDownload ? "read download" : "read", link.ExpiresAt, CreatedBy = createdBy, link.AllowDownload },
            cancellationToken: cancellationToken));
    }

    public async Task<ShareLink?> FindByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        return await connection.QuerySingleOrDefaultAsync<ShareLink>(new CommandDefinition("""
            SELECT id, organization_id AS OrganizationId, entity_id AS DiagnosisId, token_hash AS TokenHash,
                   expires_at AS ExpiresAt, COALESCE((metadata_json->>'allowDownload')::boolean, scope LIKE '%download%') AS AllowDownload,
                   revoked_at AS RevokedAt
            FROM valorapesquisa.share_links
            WHERE token_hash=@TokenHash AND entity_type='diagnosis' AND deleted_at IS NULL
            """, new { TokenHash = tokenHash }, cancellationToken: cancellationToken));
    }

    public async Task<bool> RevokeAsync(Guid organizationId, Guid linkId, CancellationToken cancellationToken = default)
    {
        using var connection = connections.Create();
        return await connection.ExecuteAsync(new CommandDefinition("""
            UPDATE valorapesquisa.share_links SET status='revoked', revoked_at=now(), updated_at=now()
            WHERE id=@LinkId AND organization_id=@OrganizationId AND deleted_at IS NULL AND revoked_at IS NULL
            """, new { LinkId = linkId, OrganizationId = organizationId }, cancellationToken: cancellationToken)) == 1;
    }
}
