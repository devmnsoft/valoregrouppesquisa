namespace Valora.Web.Navigation;

public sealed record NavigationItemViewModel(
    string Code, string Label, string Description, string Url, string Icon, string? Badge, bool IsActive);

public sealed record NavigationSectionViewModel(
    string Code, string Label, bool IsExpanded, IReadOnlyList<NavigationItemViewModel> Items);

public sealed record NavigationViewModel(
    IReadOnlyList<NavigationSectionViewModel> Sections,
    string? OrganizationName,
    string? PlanCode,
    bool IsContextAvailable,
    string CorrelationId);

public sealed record NavigationGroupsViewModel(
    IReadOnlyList<NavigationSectionViewModel> Sections,
    string Context,
    string IdPrefix);
