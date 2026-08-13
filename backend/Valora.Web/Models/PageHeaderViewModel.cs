namespace Valora.Web.Models;

public sealed record PageHeaderAction(
    string Label,
    string? Href = null,
    string? DataAction = null,
    string Style = "secondary",
    string? Icon = null);

public sealed record PageHeaderViewModel(
    string Title,
    string Subtitle,
    string Eyebrow = "Valora Insight™",
    string? Badge = null,
    IReadOnlyList<PageHeaderAction>? Actions = null,
    IReadOnlyList<string>? Breadcrumb = null,
    string? FiltersPartial = null);
