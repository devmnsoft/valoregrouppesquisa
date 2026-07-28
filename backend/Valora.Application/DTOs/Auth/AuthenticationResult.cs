namespace Valora.Application.DTOs;

public sealed record AuthenticatedUserDto(Guid Id, string Name, string Email, string Role);

public sealed record AuthenticatedOrganizationDto(Guid Id, string Name, string? TradeName, string Slug);

public sealed record AuthenticatedPlanDto(string Id, string Name);

public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);

public sealed record SessionDto(Guid Id, DateTimeOffset CreatedAt, DateTimeOffset LastSeenAt, DateTimeOffset ExpiresAt);

public sealed record AuthenticationResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    Guid SessionId,
    AuthenticatedUserDto User,
    AuthenticatedOrganizationDto? Organization,
    AuthenticatedPlanDto? Plan);
