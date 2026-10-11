namespace Valora.Application.Access;

public static class RoleAssignmentPolicy {
    public static readonly IReadOnlySet<string> PlatformReservedRoleCodes =
        new HashSet<string>([ValoraAccessCatalog.PlatformRole, "platform_admin", "admin_grupo", "consultor_valora"], StringComparer.OrdinalIgnoreCase);

    public static bool CanAssignRole(string roleCode, bool actorIsGlobalAdministrator) =>
        actorIsGlobalAdministrator || !PlatformReservedRoleCodes.Contains(NormalizeRoleCode(roleCode));

    public static string NormalizeRoleCode(string roleCode) {
        if (string.IsNullOrWhiteSpace(roleCode)) throw new ArgumentException("Role obrigatória.", nameof(roleCode));
        return roleCode.Trim().ToLowerInvariant();
    }

    public static IReadOnlyList<string> NormalizeRoleCodes(IEnumerable<string> roleCodes) {
        var normalized = roleCodes.Select(NormalizeRoleCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (normalized.Length == 0) throw new ArgumentException("Informe ao menos um papel.", nameof(roleCodes));
        return normalized;
    }

    public static IReadOnlyList<AccessRoleDto> FilterAssignable(IEnumerable<AccessRoleDto> roles, bool actorIsGlobalAdministrator) =>
        roles.Where(role => role.Status.Equals("active", StringComparison.OrdinalIgnoreCase)
            && CanAssignRole(role.Code, actorIsGlobalAdministrator)).ToArray();
}
