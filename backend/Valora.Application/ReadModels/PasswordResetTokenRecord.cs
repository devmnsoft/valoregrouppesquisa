namespace Valora.Application.ReadModels;

public sealed record PasswordResetTokenRecord(Guid Id, Guid UserId, DateTimeOffset ExpiresAt, DateTimeOffset? UsedAt);
