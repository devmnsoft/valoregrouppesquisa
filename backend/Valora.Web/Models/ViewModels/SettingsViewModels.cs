namespace Valora.Web.Models.ViewModels;

public sealed record UserProfileSettingsViewModel(
    Guid UserId,
    string Name,
    string Email,
    string Role,
    string Initials);

public sealed record OrganizationSettingsViewModel(
    Guid OrganizationId,
    string OrganizationName,
    string PlanName,
    string SubscriptionStatus,
    IReadOnlyList<string> EnabledModules);

public sealed record SecuritySettingsViewModel(
    int SessionTimeoutMinutes,
    string ProtectionScheme,
    bool CsrfProtectionActive,
    bool MultiTenantIsolationActive);

public sealed class SettingsIndexViewModel {
    public SettingsIndexViewModel(
        UserProfileSettingsViewModel profile,
        OrganizationSettingsViewModel? organization,
        SecuritySettingsViewModel security) {
        Profile = profile;
        Organization = organization;
        Security = security;
    }

    public UserProfileSettingsViewModel Profile { get; }
    public OrganizationSettingsViewModel? Organization { get; }
    public SecuritySettingsViewModel Security { get; }
}
