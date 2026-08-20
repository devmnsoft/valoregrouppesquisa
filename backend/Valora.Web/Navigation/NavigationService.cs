using System.Security.Claims;
using Valora.Web.Services.Bff;

namespace Valora.Web.Navigation;

public sealed class NavigationService(
    NavigationCatalog catalog,
    BffAuthenticationService authentication,
    INavigationRouteResolver routes)
{
    public async Task<NavigationViewModel> BuildAsync(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var session = await authentication.GetAsync(httpContext, cancellationToken);
        var roles = Set(session?.SafeSession.AccessContext.Roles, Claims(httpContext, ClaimTypes.Role));
        if (!string.IsNullOrWhiteSpace(session?.SafeSession.User.Role))
            roles.Add(session.SafeSession.User.Role);

        var context = new NavigationContext(
            session?.SafeSession.User.Name ?? httpContext.User.Identity?.Name,
            session?.SafeSession.Organization?.TradeName ?? session?.SafeSession.Organization?.Name,
            session?.SafeSession.Plan?.Id,
            httpContext.User.FindFirstValue("subscription_status") ?? (session?.SafeSession.Plan is null ? "missing" : "active"),
            roles,
            Set(session?.SafeSession.AccessContext.Permissions, Claims(httpContext, "permission")),
            Set(session?.SafeSession.AccessContext.Capabilities, Claims(httpContext, "capability")),
            Set(session?.SafeSession.AccessContext.Scopes, Claims(httpContext, "scope")),
            Set(session?.SafeSession.AccessContext.EnabledModules, Claims(httpContext, "module")));

        var currentPath = NormalizePath(httpContext.Request.Path.Value);
        var visible = catalog.Sections
            .OrderBy(navigationSection => navigationSection.Order)
            .Select(navigationSection => new
            {
                Section = navigationSection,
                Items = navigationSection.Items.OrderBy(item => item.Order)
                    .Select(item => (Item: item, Url: routes.Resolve(item.Destination)))
                    .Where(resolved => resolved.Url is not null && IsVisible(resolved.Item, context))
                    .ToArray()
            })
            .Where(resolvedSection => resolvedSection.Items.Length > 0)
            .ToArray();

        // Prefer the longest matching URL, ensuring a single active item for nested routes.
        var activeCode = visible.SelectMany(resolvedSection => resolvedSection.Items)
            .Where(resolved => Matches(currentPath, resolved.Url!))
            .OrderByDescending(resolved => NormalizePath(resolved.Url).Length)
            .Select(resolved => resolved.Item.Code)
            .FirstOrDefault();

        var sections = visible.Select(resolvedSection =>
        {
            var items = resolvedSection.Items.Select(resolved => new NavigationItemViewModel(
                resolved.Item.Code, resolved.Item.Label, resolved.Item.Description, resolved.Url!, resolved.Item.Icon,
                resolved.Item.Badge, resolved.Item.Code == activeCode)).ToArray();
            return new NavigationSectionViewModel(resolvedSection.Section.Code, resolvedSection.Section.Label,
                items.Any(item => item.IsActive), items);
        }).ToArray();

        var correlationId = httpContext.TraceIdentifier;
        return new NavigationViewModel(sections, context.OrganizationName, session?.SafeSession.Plan?.Name,
            session is not null && context.OrganizationName is not null, correlationId);
    }

    public static bool Matches(string? requestPath, string destination)
    {
        var current = NormalizePath(requestPath);
        var target = NormalizePath(destination);
        return current.Equals(target, StringComparison.OrdinalIgnoreCase)
            || (target != "/" && current.StartsWith(target + "/", StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizePath(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "/";
        var path = value.Split('?', '#')[0].TrimEnd('/');
        return string.IsNullOrEmpty(path) ? "/" : path.StartsWith('/') ? path : "/" + path;
    }

    private static bool IsVisible(NavigationItem item, NavigationContext context)
    {
        if (!item.Roles.Overlaps(context.Roles)) return false;
        if (context.EnabledModules.Count == 0 || !context.EnabledModules.Contains(item.ModuleCode)) return false;
        if (!context.HasValidSubscription && item.Capability is not null) return false;
        if (item.Permission is not null && !context.Permissions.Contains(item.Permission)) return false;
        if (item.Capability is not null && !context.Capabilities.Contains(item.Capability)) return false;
        if (item.ScopeRequirement is not null && !context.Scopes.Contains(item.ScopeRequirement)) return false;
        return true;
    }

    private static HashSet<string> Claims(HttpContext context, string type) =>
        context.User.FindAll(type)
            .SelectMany(claim => claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static HashSet<string> Set(IEnumerable<string>? authoritative, HashSet<string> claims) =>
        authoritative is null ? claims : authoritative.Concat(claims).ToHashSet(StringComparer.OrdinalIgnoreCase);
}
