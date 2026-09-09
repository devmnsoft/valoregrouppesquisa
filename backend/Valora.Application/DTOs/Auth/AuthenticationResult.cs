namespace Valora.Application.DTOs;

public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    AuthenticatedUserDto User,
    AuthenticatedOrganizationDto? Organization,
    AuthenticatedPlanDto? Plan,
    AuthenticatedAccessContextDto AccessContext)
{
    public EffectiveAccessSnapshot EffectiveAccessSnapshot => new(
        User,
        Organization,
        AccessContext.SelectedOrganizationId,
        Plan,
        AccessContext.SubscriptionStatus,
        AccessContext.Roles,
        AccessContext.Permissions,
        AccessContext.EnabledModules,
        AccessContext.Capabilities,
        AccessContext.Scopes,
        AccessContext.AccessVersion,
        AccessContext.GeneratedAt);
}

public sealed record AuthenticatedAccessContextDto(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> EnabledModules,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Scopes,
    string SubscriptionStatus,
    Guid? OrganizationId,
    string? PlanCode)
{
    public Guid? SelectedOrganizationId { get; init; }
    public bool IsGlobalAdministrator { get; init; }
    public long AccessVersion { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
}

public sealed record EffectiveAccessSnapshot(
    AuthenticatedUserDto User,
    AuthenticatedOrganizationDto? Organization,
    Guid? SelectedOrganizationId,
    AuthenticatedPlanDto? Plan,
    string SubscriptionStatus,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Modules,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Scopes,
    long AccessVersion,
    DateTimeOffset GeneratedAt);
