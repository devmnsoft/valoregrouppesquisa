using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IUserAdministrationService
{
 Task<PagedResponse<UserAdministrationResponse>> ListAsync(Guid organizationId,UserListQuery query,CancellationToken ct=default);
 Task<UserAdministrationResponse> GetAsync(Guid organizationId,Guid userId,CancellationToken ct=default);
 Task<UserAdministrationResponse> UpdateAsync(Guid organizationId,Guid userId,UpdateUserRequest request,CancellationToken ct=default);
 Task UpdateStatusAsync(Guid organizationId,Guid actorId,Guid userId,UpdateUserStatusRequest request,CancellationToken ct=default);
 Task SetRolesAsync(Guid organizationId,Guid actorId,Guid userId,UpdateUserRolesRequest request,CancellationToken ct=default);
 Task SetScopesAsync(Guid organizationId,Guid actorId,Guid userId,UpdateUserScopesRequest request,CancellationToken ct=default);
}
