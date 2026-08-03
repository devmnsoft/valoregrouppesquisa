using System.Security.Claims;
using Valora.Web.Services.Bff;

namespace Valora.Web.Navigation;

public sealed class NavigationService(NavigationCatalog catalog, BffAuthenticationService authentication)
{
    public async Task<NavigationViewModel> BuildAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var session = await authentication.GetAsync(httpContext, cancellationToken);
        var role = session?.SafeSession.User.Role ?? httpContext.User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var permissions = Claims(httpContext, "permission");
        var capabilities = Claims(httpContext, "capability");
        var scopes = Claims(httpContext, "scope");
        var modules = Claims(httpContext, "module");
        var routes = catalog.Sections.SelectMany(section => section.Items).Select(item => item.Url)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var subscriptionStatus = httpContext.User.FindFirstValue("subscription_status")
            ?? (session?.SafeSession.Plan is null ? "missing" : "active");
        var context = new NavigationContext(role, permissions, capabilities, scopes, modules, routes,
            subscriptionStatus, session?.SafeSession.Plan?.Id);

        var sections = catalog.Sections
            .OrderBy(section => section.Order)
            .Select(section => section with { Items = section.Items.Where(item => IsVisible(item, context)).OrderBy(item => item.Order).ToArray() })
            .Where(section => section.Items.Count > 0)
            .ToArray();

        return new NavigationViewModel(sections, httpContext.Request.Path.Value ?? "/");
    }

    private static bool IsVisible(NavigationItem item, NavigationContext context)
    {
        if (!item.Roles.Contains(context.Role)) return false;
        if (!context.AvailableRoutes.Contains(item.Url)) return false;
        if (context.EnabledModules.Count == 0 || !context.EnabledModules.Contains(item.ModuleCode)) return false;
        if (!context.HasValidSubscription && item.Capability is not null) return false;
        if (item.Permission is not null && !context.Permissions.Contains(item.Permission)) return false;
        if (item.Capability is not null && !context.Capabilities.Contains(item.Capability)) return false;
        if (item.ScopeRequirement is not null && !context.Scopes.Contains(item.ScopeRequirement)) return false;
        return true;
    }

    private static IReadOnlySet<string> Claims(HttpContext context, string type) =>
        context.User.FindAll(type).SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
