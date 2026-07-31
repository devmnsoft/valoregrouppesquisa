using Valora.Application.DTOs;
namespace Valora.Application.Contracts;
public interface IUserAdministrationRepository
{
 Task<(IReadOnlyList<UserAdministrationResponse> Items,long Total)> ListAsync(Guid organizationId,UserListQuery query,CancellationToken ct=default);
 Task<UserAdministrationResponse?> GetAsync(Guid organizationId,Guid userId,CancellationToken ct=default);
 Task<bool> UpdateAsync(Guid organizationId,Guid userId,UpdateUserRequest request,CancellationToken ct=default);
 Task<bool> UpdateStatusAsync(Guid organizationId,Guid userId,string status,CancellationToken ct=default);
 Task<int> CountAdministratorsAsync(Guid organizationId,CancellationToken ct=default);
 Task<bool> IsAdministratorAsync(Guid organizationId,Guid userId,CancellationToken ct=default);
 Task ReplaceRolesAsync(Guid organizationId,Guid userId,IReadOnlyList<string> roleCodes,CancellationToken ct=default);
 Task ReplaceScopesAsync(Guid organizationId,Guid userId,IReadOnlyList<UserScopeRequest> scopes,CancellationToken ct=default);
}
