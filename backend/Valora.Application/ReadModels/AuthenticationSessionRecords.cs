namespace Valora.Application.ReadModels;

public sealed record AuthenticationSessionRecord(Guid Id, Guid UserId, Guid OrganizationId,
    DateTimeOffset CreatedAt, DateTimeOffset LastUsedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);

public sealed record RefreshTokenRecord(Guid Id, Guid FamilyId, Guid SessionId, Guid UserId,
    Guid OrganizationId, string Email, string Role, string Locale,
    DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt, DateTimeOffset? RevokedAt);

public enum RefreshTokenUseStatus { Rotated, Invalid, Reused }
public sealed record RefreshTokenUseResult(RefreshTokenUseStatus Status, RefreshTokenRecord? Current);
