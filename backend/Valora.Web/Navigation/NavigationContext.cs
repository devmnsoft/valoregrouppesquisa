namespace Valora.Web.Navigation;

public sealed record NavigationContext(
    string Role,
    IReadOnlySet<string> Permissions,
    IReadOnlySet<string> Capabilities,
    IReadOnlySet<string> Scopes,
    IReadOnlySet<string> EnabledModules,
    IReadOnlySet<string> AvailableRoutes,
    string SubscriptionStatus,
    string? PlanCode)
{
    public bool HasValidSubscription =>
        PlanCode is not null && SubscriptionStatus is "active" or "trialing";
}
