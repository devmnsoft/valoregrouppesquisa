namespace Valora.Application.Contracts;

public interface IPermissionRepository
{
    Task<bool> HasAsync(Guid userId, string permissionCode, Guid? organizationId);
}
