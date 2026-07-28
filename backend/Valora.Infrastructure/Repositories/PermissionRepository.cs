using Dapper;
using Valora.Application.Contracts;

namespace Valora.Infrastructure.Repositories;

public sealed class PermissionRepository(IDbConnectionFactory factory) : IPermissionRepository
{
    public async Task<bool> HasAsync(Guid userId, string permissionCode, Guid? organizationId)
    {
        using var connection = factory.Create();
        return await connection.ExecuteScalarAsync<bool>("""
            SELECT EXISTS (
              SELECT 1 FROM valorapesquisa.user_roles ur
              JOIN valorapesquisa.roles r ON r.id=ur.role_id AND r.deleted_at IS NULL
              JOIN valorapesquisa.role_permissions rp ON rp.role_id=r.id
              JOIN valorapesquisa.permissions p ON p.id=rp.permission_id
              JOIN valorapesquisa.users u ON u.id=ur.user_id AND u.deleted_at IS NULL
              WHERE ur.user_id=@userId AND p.code=@permissionCode
                AND (@organizationId IS NULL OR u.organization_id=@organizationId)
                AND (r.organization_id IS NULL OR r.organization_id=u.organization_id))
            """, new { userId, permissionCode, organizationId });
    }
}
