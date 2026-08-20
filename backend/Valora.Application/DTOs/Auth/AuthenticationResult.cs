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
    AuthenticatedAccessContextDto AccessContext);

public sealed record AuthenticatedAccessContextDto(
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> EnabledModules,
    IReadOnlyList<string> Capabilities,
    IReadOnlyList<string> Scopes,
    string SubscriptionStatus,
    Guid? OrganizationId,
    string? PlanCode);
