using System.Security.Claims;
using Valora.Application.Access;
using Valora.Application.Common;

namespace Valora.Api.Services;

public sealed class CurrentRequestContextProvider(IHttpContextAccessor accessor) : ICurrentRequestContext {
    private static readonly string[] OrganizationAliases = ["organizationId", "tenant_id", "tenantId"];
    private static readonly string[] SessionAliases = ["sessionId"];

    public CurrentRequestContext GetCurrent() {
        var http = accessor.HttpContext;
        if (http is null) return Empty();

        var roles = Values(http.User, ClaimTypes.Role, "role");
        var global = roles.Contains(ValoraAccessCatalog.PlatformRole, StringComparer.OrdinalIgnoreCase);
        var organizationId = ReadGuid(http.User, "organization_id", OrganizationAliases);
        var selected = ReadGuid(http.User, "selected_organization_id");

        if (TryGuid(http.Request.Headers["X-Organization-Id"].FirstOrDefault(), out var requested)) {
            if (global)
                selected = requested;
            else if (organizationId != requested)
                selected = null;
        }

        if (!global) selected = organizationId;

        return new(
            ReadGuid(http.User, ClaimTypes.NameIdentifier, ["sub"]),
            ReadGuid(http.User, "session_id", SessionAliases),
            organizationId,
            selected,
            global,
            roles,
            Values(http.User, "permission"),
            Values(http.User, "module"),
            Values(http.User, "capability"),
            Values(http.User, "scope"),
            http.User.FindFirstValue("subscription_status") ?? "missing",
            ReadLong(http.User.FindFirstValue("access_version")));
    }

    private static CurrentRequestContext Empty() => new(null, null, null, null, false, [], [], [], [], [], "missing", 0);

    private static IReadOnlyList<string> Values(ClaimsPrincipal principal, params string[] types) => types
        .SelectMany(principal.FindAll)
        .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static Guid? ReadGuid(ClaimsPrincipal principal, string canonical, IReadOnlyList<string>? aliases = null) {
        if (TryGuid(principal.FindFirstValue(canonical), out var value)) return value;
        if (aliases is not null)
            foreach (var alias in aliases)
                if (TryGuid(principal.FindFirstValue(alias), out value)) return value;
        return null;
    }

    private static bool TryGuid(string? value, out Guid result) =>
        Guid.TryParse(value, out result) && result != Guid.Empty;

    private static long ReadLong(string? value) => long.TryParse(value, out var parsed) && parsed > 0 ? parsed : 0;
}
