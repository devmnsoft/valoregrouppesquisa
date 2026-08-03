namespace Valora.Application.Access;

public sealed record AccessRoleDto(Guid Id, string Code, string Name, string? Description, bool IsSystem, string Status, long Version, int UserCount, IReadOnlyList<string> Permissions);
public sealed record AccessPermissionDto(string Code, string Name, string? Description, string? ModuleCode, string FunctionalGroup, string RiskLevel, bool AssignableToCustomRoles, int DisplayOrder, string Status);
public sealed record AccessModuleDto(string Code, string Name, string Status, IReadOnlyList<AccessPermissionDto> Permissions);
public sealed record CreateAccessRoleRequest(string Code, string Name, string? Description, IReadOnlyList<string> Permissions);
public sealed record UpdateAccessRoleRequest(string Name, string? Description, string Status, long ExpectedVersion);
public sealed record ReplaceRolePermissionsRequest(IReadOnlyList<string> Permissions, long ExpectedVersion, string? Reason);
public sealed record EffectiveAccessDto(Guid UserId, IReadOnlyList<string> GrantedPermissions, IReadOnlyList<BlockedAccessDto> BlockedPermissions, IReadOnlyList<AccessScopeDto> Scopes, IReadOnlyList<string> AvailableModules);
public sealed record BlockedAccessDto(string Permission, string Reason);
public sealed record AccessScopeDto(string Type, Guid Id, string Label);

public interface IAccessAdministrationService
{
    Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(Guid organizationId, CancellationToken ct);
    Task<AccessRoleDto> GetRoleAsync(Guid organizationId, Guid roleId, CancellationToken ct);
    Task<AccessRoleDto> CreateRoleAsync(Guid organizationId, Guid actorId, CreateAccessRoleRequest request, CancellationToken ct);
    Task<AccessRoleDto> UpdateRoleAsync(Guid organizationId, Guid actorId, Guid roleId, UpdateAccessRoleRequest request, CancellationToken ct);
    Task DeleteRoleAsync(Guid organizationId, Guid actorId, Guid roleId, CancellationToken ct);
    Task<AccessRoleDto> ReplacePermissionsAsync(Guid organizationId, Guid actorId, Guid roleId, ReplaceRolePermissionsRequest request, CancellationToken ct);
    Task<IReadOnlyList<AccessPermissionDto>> ListPermissionsAsync(CancellationToken ct);
    Task<IReadOnlyList<AccessModuleDto>> ListModulesAsync(CancellationToken ct);
    Task<EffectiveAccessDto> GetEffectiveAccessAsync(Guid organizationId, Guid userId, CancellationToken ct);
}

public interface IAccessAdministrationRepository
{
    Task<IReadOnlyList<AccessRoleDto>> ListRolesAsync(Guid organizationId, CancellationToken ct);
    Task<AccessRoleDto?> GetRoleAsync(Guid organizationId, Guid roleId, CancellationToken ct);
    Task<AccessRoleDto> CreateRoleAsync(Guid organizationId, Guid actorId, CreateAccessRoleRequest request, CancellationToken ct);
    Task<AccessRoleDto?> UpdateRoleAsync(Guid organizationId, Guid actorId, Guid roleId, UpdateAccessRoleRequest request, CancellationToken ct);
    Task<bool> DeleteRoleAsync(Guid organizationId, Guid actorId, Guid roleId, CancellationToken ct);
    Task<AccessRoleDto?> ReplacePermissionsAsync(Guid organizationId, Guid actorId, Guid roleId, ReplaceRolePermissionsRequest request, CancellationToken ct);
    Task<IReadOnlyList<AccessPermissionDto>> ListPermissionsAsync(CancellationToken ct);
    Task<EffectiveAccessDto?> GetEffectiveAccessAsync(Guid organizationId, Guid userId, CancellationToken ct);
}
