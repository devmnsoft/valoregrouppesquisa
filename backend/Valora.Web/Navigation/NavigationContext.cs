namespace Valora.Web.Navigation;

public sealed record NavigationContext(
    string Role,
    IReadOnlySet<string> Permissions,
    IReadOnlySet<string> Capabilities,
    IReadOnlySet<string> Scopes,
    bool HasValidSubscription);
