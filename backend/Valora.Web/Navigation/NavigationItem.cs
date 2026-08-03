namespace Valora.Web.Navigation;

public sealed record NavigationDestination(string Controller, string Action)
{
    public static NavigationDestination Mvc(string controller, string action = "Index") => new(controller, action);
}

public sealed record NavigationItem(
    string Code,
    string Label,
    string Description,
    NavigationDestination Destination,
    string Icon,
    string? Permission,
    string? Capability,
    string ModuleCode,
    string? ScopeRequirement,
    int Order,
    string? Badge,
    IReadOnlySet<string> Roles);
