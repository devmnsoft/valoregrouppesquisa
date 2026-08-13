namespace Valora.Application.ReadModels;

public sealed class PasswordResetTokenRecord
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public DateTime ExpiresAt { get; init; }
    public DateTime? UsedAt { get; init; }
}
