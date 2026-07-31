namespace Valora.Application.DTOs;

public sealed record UserScopeResponse(string Type, Guid Id, string Label, string Breadcrumb);
public sealed record UserAdministrationResponse(Guid Id,string Name,string Email,string? Phone,string Status,IReadOnlyList<string> RoleCodes,IReadOnlyList<UserScopeResponse> Scopes,DateTimeOffset? LastLoginAt,DateTimeOffset CreatedAt,DateTimeOffset? UpdatedAt);
public sealed record UserListQuery(int Page=1,int PageSize=20,string? Search=null,string? Status=null,string? RoleCode=null,Guid? BusinessGroupId=null,Guid? LegalEntityId=null,Guid? UnitId=null,Guid? DepartmentId=null,string SortBy="name",string SortDirection="asc");
public sealed record PagedResponse<T>(IReadOnlyList<T> Items,int Page,int PageSize,long TotalItems,int TotalPages);
public sealed record UpdateUserRequest(string Name,string? Phone);
public sealed record UpdateUserStatusRequest(string Status);
public sealed record UpdateUserRolesRequest(IReadOnlyList<string> RoleCodes);
public sealed record UpdateUserScopesRequest(IReadOnlyList<UserScopeRequest> Scopes);
public sealed record UserScopeRequest(string Type,Guid Id);
