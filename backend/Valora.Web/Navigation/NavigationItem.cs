namespace Valora.Web.Navigation;

public sealed record NavigationItem(
    string Code,
    string Label,
    string Description,
    string Url,
    string Icon,
    string? Permission,
    string? Capability,
    string ModuleCode,
    string? ScopeRequirement,
    int Order,
    string? Badge,
    IReadOnlySet<string> Roles);
