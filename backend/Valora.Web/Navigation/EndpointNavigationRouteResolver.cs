using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;

namespace Valora.Web.Navigation;

public sealed class EndpointNavigationRouteResolver(
    IActionDescriptorCollectionProvider actions,
    LinkGenerator links,
    IHttpContextAccessor accessor,
    ILogger<EndpointNavigationRouteResolver> logger) : INavigationRouteResolver
{
    public string? Resolve(NavigationDestination destination)
    {
        var exists = actions.ActionDescriptors.Items.Any(action =>
            string.Equals(action.RouteValues["controller"], destination.Controller, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(action.RouteValues["action"], destination.Action, StringComparison.OrdinalIgnoreCase));

        if (!exists)
        {
            logger.LogWarning("Navigation destination {Controller}.{Action} has no MVC action.", destination.Controller, destination.Action);
            return null;
        }

        var path = links.GetPathByAction(accessor.HttpContext, destination.Action, destination.Controller);
        if (string.IsNullOrWhiteSpace(path))
            logger.LogWarning("Navigation destination {Controller}.{Action} cannot generate a URL.", destination.Controller, destination.Action);
        return path;
    }
}
