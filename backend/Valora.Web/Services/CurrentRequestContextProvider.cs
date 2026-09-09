using System.Security.Claims;
using Valora.Application.Access;
using Valora.Application.Common;

namespace Valora.Web.Services;

public sealed class CurrentRequestContextProvider(IHttpContextAccessor accessor) : ICurrentRequestContext
{
    public CurrentRequestContext GetCurrent()
    {
        var http = accessor.HttpContext;
        if (http is null) return new(null, null, null, null, false, [], [], [], [], [], "missing", 0);

        var roles = Values(http.User, ClaimTypes.Role, "role");
        var global = roles.Contains(ValoraAccessCatalog.PlatformRole, StringComparer.OrdinalIgnoreCase);
        var organization = ReadGuid(http.User, "organization_id", "organizationId", "tenant_id", "tenantId");
        var selected = ReadGuid(http.User, "selected_organization_id");
        if (!global) selected = organization;

        return new(
            ReadGuid(http.User, ClaimTypes.NameIdentifier, "sub"),
            ReadGuid(http.User, "session_id", "sessionId"),
            organization,
            selected,
            global,
            roles,
            Values(http.User, "permission"),
            Values(http.User, "module"),
            Values(http.User, "capability"),
            Values(http.User, "scope"),
            http.User.FindFirstValue("subscription_status") ?? "missing",
            long.TryParse(http.User.FindFirstValue("access_version"), out var version) ? version : 0);
    }

    private static IReadOnlyList<string> Values(ClaimsPrincipal principal, params string[] types) => types
        .SelectMany(principal.FindAll)
        .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();

    private static Guid? ReadGuid(ClaimsPrincipal principal, params string[] types)
    {
        foreach (var type in types)
            if (Guid.TryParse(principal.FindFirstValue(type), out var id) && id != Guid.Empty) return id;
        return null;
    }
}
