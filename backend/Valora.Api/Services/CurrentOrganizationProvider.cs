using System.Security.Claims;
using Valora.Application.Common;

namespace Valora.Api.Services;

public sealed class CurrentOrganizationProvider(
    IHttpContextAccessor httpContextAccessor,
    ILogger<CurrentOrganizationProvider> logger) : ICurrentOrganizationProvider
{
    private static readonly string[] ClaimNames = ["organization_id", "organizationId", "tenant_id", "tenantId"];

    public CurrentOrganizationContext GetCurrent()
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return CurrentOrganizationContext.Unresolved();

        foreach (var claimName in ClaimNames)
            if (TryResolve(context.User.FindFirstValue(claimName), $"claim:{claimName}", out var result)) return result;

        if (TryResolve(context.Request.Headers["X-Organization-Id"].FirstOrDefault(), "header:X-Organization-Id", out var header)) return header;
        if (TryResolve(context.Request.Query["organizationId"].FirstOrDefault(), "query:organizationId", out var query)) return query;
        if (TryResolve(context.Request.RouteValues["organizationId"]?.ToString(), "route:organizationId", out var route)) return route;

        logger.LogWarning("Contexto de organização ausente. UserId={UserId} Path={Path}",
            context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub"),
            context.Request.Path.Value);
        return CurrentOrganizationContext.Unresolved();
    }

    private static bool TryResolve(string? value, string source, out CurrentOrganizationContext result)
    {
        if (Guid.TryParse(value, out var id) && id != Guid.Empty)
        {
            result = CurrentOrganizationContext.Resolved(id, source);
            return true;
        }

        result = CurrentOrganizationContext.Unresolved();
        return false;
    }
}
