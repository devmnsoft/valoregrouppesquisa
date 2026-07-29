namespace Valora.Application.DTOs;

public sealed record TokenPair(
    Guid SessionId,
    Guid UserId,
    Guid OrganizationId,
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
