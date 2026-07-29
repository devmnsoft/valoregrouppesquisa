namespace Valora.Application.ReadModels;

public sealed record AuthenticationSessionRecord(Guid Id, Guid UserId, Guid OrganizationId,
    DateTimeOffset CreatedAt, DateTimeOffset LastUsedAt, DateTimeOffset ExpiresAt, DateTimeOffset? RevokedAt);
