namespace Valora.Web.Navigation;

public sealed record NavigationContext(
    string? UserName,
    string? OrganizationName,
    string? PlanCode,
    string SubscriptionStatus,
    IReadOnlySet<string> Roles,
    IReadOnlySet<string> Permissions,
    IReadOnlySet<string> Capabilities,
    IReadOnlySet<string> Scopes,
    IReadOnlySet<string> EnabledModules)
{
    public bool HasValidSubscription =>
        PlanCode is not null && SubscriptionStatus is "active" or "trialing";
}
