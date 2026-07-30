using Valora.Application.Contracts;
using Valora.Application.DTOs;

namespace Valora.Application.Services;


public sealed class PermissionService(IPermissionRepository permissions) : IPermissionService
{
    public Task<bool> HasPermissionAsync(Guid userId, string permissionCode, Guid? organizationId = null) =>
        permissions.HasAsync(userId, permissionCode, organizationId);
}
