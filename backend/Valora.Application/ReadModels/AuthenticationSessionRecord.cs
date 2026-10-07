namespace Valora.Application.ReadModels;

public sealed record AuthenticationSessionRecord(Guid Id, Guid UserId, Guid OrganizationId,
    DateTime CreatedAt, DateTime LastUsedAt, DateTime ExpiresAt, DateTime? RevokedAt);
