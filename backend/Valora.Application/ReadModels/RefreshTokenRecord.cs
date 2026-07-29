namespace Valora.Application.ReadModels;

public sealed record RefreshTokenRecord(Guid Id, Guid FamilyId, Guid SessionId, Guid UserId,
    Guid OrganizationId, string Email, string Role, string Locale,
    DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt, DateTimeOffset? RevokedAt);
