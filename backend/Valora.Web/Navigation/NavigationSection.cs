namespace Valora.Web.Navigation;

public sealed record NavigationSection(string Code, string Label, int Order, IReadOnlyList<NavigationItem> Items);
