namespace Valora.Application.Contracts;
public interface IPermissionService { Task<bool> HasPermissionAsync(Guid userId,string permissionCode,Guid? organizationId=null); }
