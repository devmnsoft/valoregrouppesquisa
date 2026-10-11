namespace Valora.Application.Access;

public static class RoleAssignmentPolicy {
    public static readonly IReadOnlySet<string> PlatformReservedRoleCodes =
        new HashSet<string>([ValoraAccessCatalog.PlatformRole, "consultor_valora"], StringComparer.OrdinalIgnoreCase);

    public static bool CanAssignRole(string roleCode, bool actorIsGlobalAdministrator) =>
        actorIsGlobalAdministrator || !PlatformReservedRoleCodes.Contains(roleCode);

    public static IReadOnlyList<AccessRoleDto> FilterAssignable(IEnumerable<AccessRoleDto> roles, bool actorIsGlobalAdministrator) =>
        roles.Where(role => role.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
            && CanAssignRole(role.Code, actorIsGlobalAdministrator)).ToArray();
}
