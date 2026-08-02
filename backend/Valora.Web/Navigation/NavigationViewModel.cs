namespace Valora.Web.Navigation;

public sealed record NavigationViewModel(IReadOnlyList<NavigationSection> Sections, string CurrentPath);
