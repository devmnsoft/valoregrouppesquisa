namespace Valora.Application.DTOs;

public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    AuthenticatedUserDto User,
    AuthenticatedOrganizationDto? Organization,
    AuthenticatedPlanDto? Plan);
